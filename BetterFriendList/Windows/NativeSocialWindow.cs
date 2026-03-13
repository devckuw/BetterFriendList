using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using KamiToolKit.Nodes;
using KamiToolKit.Extensions;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SamplePlugin.GameAddon;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using System.Linq;

namespace BetterFriendList.Windows;

public class NativeSocialWindow : IDisposable
{

    private TextureButtonNode? minimizeButton = null;
    private TextureButtonNode? refreshButton = null;
    public bool isMinimized = false;
    private Vector2 lastPos = new Vector2(-1, -1);
    private bool isPlaced = false;
    private int needSubAddonHide = -1;
    private int needResetFirstVisibleItemIndex = 0;
    public int oldFirstVisibleItemIndex = -1;
    Plugin plugin;

    public NativeSocialWindow(Plugin p)
    {
        Plugin.Framework.RunOnFrameworkThread(EnableCollapse);
        Plugin.Framework.RunOnFrameworkThread(EnableRefresh);

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "Social", OnPostSetupSocial);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, "Social", OnPreDrawSocial);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreFinalize, "Social", OnPreFinilizeSocial);

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "PartyMemberList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "FriendList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PostSetup, "SocialList", OnPostSetupSubAddon);

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, "PartyMemberList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, "FriendList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreDraw, "SocialList", OnPreDrawSubAddon);

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PostDraw, "FriendList", OnPostDrawFriendList);

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreFinalize, "FriendList", OnPreFinilizeFriendList);

        Plugin.Framework.Update += OnFrameWorkUpdate;

        plugin = p;
    }

    public void Dispose()
    {
        ResetColor();

        Plugin.Framework.Update -= OnFrameWorkUpdate;

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "Social", OnPostSetupSocial);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "Social", OnPreDrawSocial);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreFinalize, "Social", OnPreFinilizeSocial);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "PartyMemberList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "FriendList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "SocialList", OnPostSetupSubAddon);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "PartyMemberList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "FriendList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "SocialList", OnPreDrawSubAddon);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostDraw, "FriendList", OnPostDrawFriendList);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreFinalize, "FriendList", OnPreFinilizeFriendList);

        minimizeButton?.Dispose();
        minimizeButton = null;

        refreshButton?.Dispose();
        refreshButton = null;
    }

    public unsafe void OnFrameWorkUpdate(IFramework framework)
    {
        if (!plugin.Configuration.UsesColorNative) return;
        var friendList = (AddonSocial*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null) return;
        if (!friendList->IsFullyLoaded()) return;

        var listComponentNode = (AtkComponentNode*) friendList->GetNodeById(14);
        if (listComponentNode == null) return;
        
        var listComponent = (AtkComponentList*) listComponentNode->Component;
        if (listComponent == null) return;

        var newFirstVisibleItemIndex = listComponent->FirstVisibleItemIndex;

        //check for scroll or requested color
        if (newFirstVisibleItemIndex != oldFirstVisibleItemIndex)
        {
            oldFirstVisibleItemIndex = newFirstVisibleItemIndex;
            Plugin.Log.Debug("scroll");
            ApplyColor();
        }
    }

    public unsafe void buildRefreshButton()
    {
#if DEBUG
        Plugin.Log.Debug("try create refresh button");
#endif
        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null) return;
        if (!friendList->IsFullyLoaded()) return;

        refreshButton = new TextureButtonNode()
        {
            Size =  new Vector2(28.0f, 28.0f),
            TexturePath = "ui/uld/CircleButtons.tex",
            TextureCoordinates = new Vector2(112.0f, 0.0f),
            TextureSize = new Vector2(28.0f, 28.0f),
            Position = new Vector2(548.0f, 72.0f),
            TextTooltip = "Refresh",

            OnClick = () =>
            {
#if DEBUG
                Plugin.Log.Debug("refresh clicked");
#endif
                var agent = AgentFriendlist.Instance();
                if (agent == null)
                {
                    return;
                }
                var proxy = agent->InfoProxy;
                if (proxy == null)
                {
                    return;
                }

                InfoProxyManager.Instance.requestDataHook.Original((InfoProxyCommonList*)agent->InfoProxy);
            }
        };
    }

    public unsafe void Maximize()
    {
#if DEBUG
        Plugin.Log.Debug($"Maximize() isMinimized:{isMinimized}");
#endif
        if (!isMinimized)
        {
            return;
        }

        var social = (AddonSocial*)Plugin.GameGui.GetAddonByName("Social").Address;
        if (social == null) return;
        if (!social->IsFullyLoaded()) return;

        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        var partyMembers = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("PartyMemberList").Address;
        var playerSearch = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("SocialList").Address;

        AtkUnitBase* loaded = null;

        if (friendList != null)
        {
            loaded = (AtkUnitBase*)friendList;
        }
        if (partyMembers != null)
        {
            loaded = partyMembers;
        }
        if (playerSearch != null)
        {
            loaded = playerSearch;
        }

        AtkResNode* btnPM = social->GetNodeById(3);
        AtkResNode* btnFL = social->GetNodeById(4);
        AtkResNode* btnPS = social->GetNodeById(5);
        AtkResNode* btnCM = social->GetNodeById(2);

        var windowsResNode = social->GetNodeById(6);
        if (windowsResNode == null) return;
        var windowsResNodeComp = windowsResNode->GetComponent();
        if (windowsResNodeComp == null) return;

        var crossButton = (AtkComponentNode*)windowsResNodeComp->GetNodeById(7);
        if (crossButton == null) return;

        var title = windowsResNodeComp->GetTextNodeById(3);
        if (title == null) return;

        windowsResNode->SetPositionShort(0, 0);
        windowsResNode->SetRotationDegrees(0);
        social->SetSize(662, 548);

        crossButton->SetPositionShort(629,6);

        minimizeButton?.Position = new Vector2(600, 7);

        title->SetPositionShort(12, 7);

        btnPM->AddNodeFlag(NodeFlags.Visible);
        btnFL->AddNodeFlag(NodeFlags.Visible);
        btnPS->AddNodeFlag(NodeFlags.Visible);
        //btnCM->AddNodeFlag(NodeFlags.Visible);

        loaded->RootNode->AddNodeFlag(NodeFlags.Visible);

        for (var i=0; i < loaded->CollisionNodeListCount; i++)
        {
            loaded->CollisionNodeList[i]->AddNodeFlag(NodeFlags.Visible);
        }
    
        isMinimized = false;
    }

    public unsafe void Minimize()
    {
#if DEBUG
        Plugin.Log.Debug($"Minimize() isMinimized:{isMinimized}");
#endif
        if (isMinimized)
        {
            return;
        }

        var social = (AddonSocial*)Plugin.GameGui.GetAddonByName("Social").Address;
        if (social == null) return;
        if (!social->IsFullyLoaded()) return;

        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        var partyMembers = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("PartyMemberList").Address;
        var playerSearch = (AtkUnitBase*)Plugin.GameGui.GetAddonByName("SocialList").Address;

        AtkUnitBase* loaded = null;

        if (friendList != null)
        {
            loaded = (AtkUnitBase*)friendList;
        }
        if (partyMembers != null)
        {
            loaded = partyMembers;
        }
        if (playerSearch != null)
        {
            loaded = playerSearch;
        }

        AtkResNode* btnPM = social->GetNodeById(3);
        AtkResNode* btnFL = social->GetNodeById(4);
        AtkResNode* btnPS = social->GetNodeById(5);
        AtkResNode* btnCM = social->GetNodeById(2);

        var windowsResNode = social->GetNodeById(6);
        if (windowsResNode == null) return;
        var windowsResNodeComp = windowsResNode->GetComponent();
        if (windowsResNodeComp == null) return;

        var crossButton = (AtkComponentNode*)windowsResNodeComp->GetNodeById(7);
        if (crossButton == null) return;

        var title = windowsResNodeComp->GetTextNodeById(3);
        if (title == null) return;

        social->SetSize(40, 126);
        windowsResNode->SetPositionShort(544, 40);
        windowsResNode->SetRotationDegrees(-90);

        crossButton->SetPositionShort(7,85);

        minimizeButton?.Position = new Vector2(600, 7);

        title->SetPositionShort(35, 7);

        btnPM->RemoveNodeFlag(NodeFlags.Visible);
        btnFL->RemoveNodeFlag(NodeFlags.Visible);
        btnPS->RemoveNodeFlag(NodeFlags.Visible);
        //btnCM->RemoveNodeFlag(NodeFlags.Visible);

        loaded->RootNode->RemoveNodeFlag(NodeFlags.Visible);

        for (var i=0; i < loaded->CollisionNodeListCount; i++)
        {
            loaded->CollisionNodeList[i]->RemoveNodeFlag(NodeFlags.Visible);
        }

        isMinimized = true;
    }

    public unsafe void buildMinimizeButton()
    {
        Plugin.Log.Debug($"try create minimize button");
        var social = (AddonSocial*)Plugin.GameGui.GetAddonByName("Social").Address;
        if (social == null) return;
        minimizeButton = new TextureButtonNode() //ui/uld/ListA.tex ui/uld/IconA_Frame.tex
        {
            Size = new Vector2(26.0f, 26.0f),
			TexturePath = "ui/uld/IconA_Frame.tex",
			TextureCoordinates = new Vector2(410.0f, 56.0f),
			TextureSize = new Vector2(14.0f, 14.0f),

			OnClick = () =>
            {
                var social = (AddonSocial*)Plugin.GameGui.GetAddonByName("Social").Address;
                if (social == null) return;

                if (isMinimized) // maximize
                {
                    Maximize();
                }
                else // minimize
                {
                    Minimize();
                }
            },
        };
    }

    public void EnableRefresh()
    {
        if (refreshButton == null)
        {
            buildRefreshButton();
        }
        SetupRefresh();
    }

    public void EnableCollapse()
    {
        if (minimizeButton == null)
        {
            buildMinimizeButton();
        }
        SetupCollapse();
    }

    public void DisableRefresh()
    {
        refreshButton?.DetachNode();
    }

    public void DisableCollapse()
    {
        minimizeButton?.DetachNode();
    }

    public void OnPostDrawFriendList(AddonEvent type, AddonArgs args)
    {
        if (needResetFirstVisibleItemIndex == 1)
        {
            oldFirstVisibleItemIndex = -1;
            needResetFirstVisibleItemIndex = 0;
            return;
        }
        if (needResetFirstVisibleItemIndex == 2)
            needResetFirstVisibleItemIndex = 1;
    }

    public unsafe void OnPreDrawSubAddon(AddonEvent type, AddonArgs args)
    {
        if (!isMinimized)
        {
            return;
        }
        if (needSubAddonHide == -1)
        {
            return;
        }
        else if (needSubAddonHide == 1)
        {
            Maximize();
            needSubAddonHide = -1;
            return;
        }
        else if (needSubAddonHide == 0)
        {
            Minimize();
            needSubAddonHide = -1;
            return;
        }
    }
    
    public unsafe void OnPostSetupSubAddon(AddonEvent type, AddonArgs args)
    {   
#if DEBUG
        Plugin.Log.Debug($"OnPostSetupSubAddon {args.AddonName}");
#endif
        if (args.AddonName == "FriendList")
        {
            needResetFirstVisibleItemIndex = 2;
        }
        if (!isMinimized)
        {
            if (args.AddonName == "FriendList")
            {
                EnableRefresh();
            }
            return;
        }
        
        if (plugin.Configuration.KeepSubAddonHidden)
        {
            needSubAddonHide = 0;
        }
        else
        {
            needSubAddonHide = 1;
        }
    }

    public void OnPostSetupSocial(AddonEvent type, AddonArgs args)
    {
        EnableCollapse();
    }

    public unsafe void RepositionSocial(nint ptr)
    {
        var social = (AddonSocial*)ptr;
        isPlaced = true;
        if (lastPos.X <= 0) return;
        social->SetPosition((short)lastPos.X, (short)lastPos.Y);
    }

    public void OnPreDrawSocial(AddonEvent type, AddonArgs args)
    {
        if (isPlaced)
        {
            return;
        }
        RepositionSocial(args.Addon.Address);
    }

    public void OnPreFinilizeSocial(AddonEvent type, AddonArgs args)
    {
        lastPos = new Vector2(args.Addon.Position.X, args.Addon.Position.Y);
        minimizeButton?.DetachNode();
        isMinimized = false;
        isPlaced = false;
    }

    public void OnPreFinilizeFriendList(AddonEvent type, AddonArgs args)
    {
#if DEBUG
        Plugin.Log.Debug("detach refresh");
#endif
        refreshButton?.DetachNode();
    }

    public unsafe void SetupCollapse()
    {
        if (!plugin.Configuration.UsesCollapseButton)
        {
            return;
        }
        var social = (AddonSocial*)Plugin.GameGui.GetAddonByName("Social").Address;
        if (social == null) return;
        if (!social->IsFullyLoaded()) return;

        minimizeButton?.AttachNode(social->RootNode);
        minimizeButton?.Position = new Vector2(600, 7);
        
        var btnCM = social->GetNodeById(2);
        btnCM->RemoveNodeFlag(NodeFlags.Visible);
    }

    public unsafe void SetupRefresh()
    {
        if (!plugin.Configuration.UsesRefreshButton)
        {
            return;
        }
        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null) return;
        if (!friendList->IsFullyLoaded()) return;
#if DEBUG
        Plugin.Log.Debug("setup refresh");
#endif
        refreshButton?.AttachNode(friendList->RootNode);
    }

    public ByteColor RGBAToByteColor(byte r, byte g, byte b, byte a)
    {
        return new ByteColor() {RGBA = (uint)(a << 24 | b << 16 | g << 8 | r << 0)};
    }

    public ByteColor RGBAToByteColor(Vector4 col)
    {
        return RGBAToByteColor((byte)(col.X * 255), (byte)(col.Y * 255), (byte)(col.Z * 255), (byte)(col.W * 255));
    }

    public unsafe AtkTextNode* GetTextNode(AtkComponentListItemRenderer* item)
    {
        var node1 = item->GetTextNodeById(10);
        if ((node1->NodeFlags & NodeFlags.Visible) == NodeFlags.Visible)
            return node1;
        var node2 = item->GetTextNodeById(8);
        if ((node2->NodeFlags & NodeFlags.Visible) == NodeFlags.Visible)
            return node2;
        return null;
    }

    public unsafe void ApplyColor()//int newFirstVisibleItemIndex)
    {
        var friendList = (AddonSocial*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null) return;
        if (!friendList->IsFullyLoaded()) return;

        var proxy = InfoProxyFriendList.Instance();
        if (proxy == null)
        {
            return;
        }

        var listComponentNode = (AtkComponentNode*) friendList->GetNodeById(14);
        if (listComponentNode is null) return;
        
        var listComponent = (AtkComponentList*) listComponentNode->Component;
        if (listComponent is null) return;

        foreach(uint nodeId in Enumerable.Range(41001, 17).Prepend(4))
        {
            var listItemRenderer = listComponent->UldManager.SearchNodeById<AtkComponentNode>(nodeId);
            if (listItemRenderer is null) return;
            var listItemRendererComponent =  (AtkComponentListItemRenderer*) listItemRenderer->Component;
            if (listItemRendererComponent is null) return;

            var textNodeptr = GetTextNode(listItemRendererComponent);
            if (textNodeptr != null){
                var proxyEntry = proxy->GetEntry((uint)listItemRendererComponent->ListItemIndex);
                if (proxyEntry == null) return;
                //var col = plugin.Configuration.FriendsColors[proxyEntry->ContentId];
                Vector4 col;
                if(!plugin.Configuration.FriendsColors.TryGetValue(proxyEntry->ContentId, out col))
                {
                    col = new Vector4(1,1,1,1);
                    plugin.Configuration.FriendsColors.Add(proxyEntry->ContentId, col);
                    plugin.Configuration.Save();
                }

                if (col.X + col.Y + col.Z == 3)
                {
                    col = new Vector4(238f/255f, 225f/255f, 197f/255f, 1);
                }
                if (!proxyEntry->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online) ||
                 proxyEntry->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AnotherWorld))
                {
                    textNodeptr->TextColor = RGBAToByteColor(new Vector4(col.X, col.Y, col.Z, 0.5f));
                }
                else
                {
                    textNodeptr->TextColor = RGBAToByteColor(col);
                }
            }
        }
    }

    public unsafe void ResetColor()
    {
        var friendList = (AddonSocial*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null) return;
        if (!friendList->IsFullyLoaded()) return;

        var proxy = InfoProxyFriendList.Instance();
        if (proxy == null)
        {
            return;
        }

        var listComponentNode = (AtkComponentNode*) friendList->GetNodeById(14);
        if (listComponentNode is null) return;
        
        var listComponent = (AtkComponentList*) listComponentNode->Component;
        if (listComponent is null) return;

        foreach(uint nodeId in Enumerable.Range(41001, 17).Prepend(4))
        {
            var listItemRenderer = listComponent->UldManager.SearchNodeById<AtkComponentNode>(nodeId);
            if (listItemRenderer is null) return;
            var listItemRendererComponent =  (AtkComponentListItemRenderer*) listItemRenderer->Component;
            if (listItemRendererComponent is null) return;

            var textNodeptr = GetTextNode(listItemRendererComponent);
            if (textNodeptr != null){
                var proxyEntry = proxy->GetEntry((uint)listItemRendererComponent->ListItemIndex);
                if (proxyEntry == null) return;
                if (!proxyEntry->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online) ||
                 proxyEntry->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AnotherWorld))
                {
                    textNodeptr->TextColor = RGBAToByteColor(128, 128, 128, 255);
                }
                else
                {
                    textNodeptr->TextColor = RGBAToByteColor(238, 225, 197, 255);
                }
            }
        }
    }

}
