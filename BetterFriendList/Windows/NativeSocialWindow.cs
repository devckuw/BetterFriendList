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

namespace BetterFriendList.Windows;

public class NativeSocialWindow : IDisposable
{

    private TextureButtonNode? minimizeButton = null;
    private TextureButtonNode? refreshButton = null;
    private bool isMinimized = false;
    private Vector2 lastPos = new Vector2(-1, -1);
    private bool isPlaced = false;
    private int needSubAddonHide = -1;
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

        Plugin.AddonLifeCycle.RegisterListener(AddonEvent.PreFinalize, "FriendList", OnPreFinilizeFriendList);

        plugin = p;
    }

    public void Dispose()
    {
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "Social", OnPostSetupSocial);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "Social", OnPreDrawSocial);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreFinalize, "Social", OnPreFinilizeSocial);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "PartyMemberList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "FriendList", OnPostSetupSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PostSetup, "SocialList", OnPostSetupSubAddon);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "PartyMemberList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "FriendList", OnPreDrawSubAddon);
        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreDraw, "SocialList", OnPreDrawSubAddon);

        Plugin.AddonLifeCycle.UnregisterListener(AddonEvent.PreFinalize, "FriendList", OnPreFinilizeFriendList);

        minimizeButton?.DetachNode();
        minimizeButton?.Dispose();

        refreshButton?.DetachNode();
        refreshButton?.Dispose();
    }

    public unsafe void DisableFlagNode(AtkResNode* node, NodeFlags flag)
    {
        node->NodeFlags &= ~flag;
    }

    public unsafe void EnableFlagNode(AtkResNode* node, NodeFlags flag)
    {   
        if (node == null) return;
        node->NodeFlags |= flag;
    }

    public unsafe void buildRefreshButton()
    {
#if DEBUG
        Plugin.Log.Debug("try create refresh button");
#endif
        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null)
        {
            return;
        }
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
        var windowsResNodeList = windowsResNode->GetComponent()->UldManager.NodeList;

        var nineGridFocus = windowsResNodeList[3];
        var nineGridNoFocus = windowsResNodeList[2];

        var crossButton = windowsResNodeList[6];
        var crossButtonNodeList = crossButton->GetComponent()->UldManager.NodeList;
        
        var crossButtonCollision = crossButtonNodeList[0];
        var crossButtonImg = crossButtonNodeList[1];

        var title = windowsResNodeList[10];

        windowsResNode->SetPositionShort(0, 0);
        windowsResNode->SetRotationDegrees(0);
        social->SetSize(662, 548);
        

        crossButton->SetPositionShort(629,6);

        minimizeButton?.Position = new Vector2(600, 7);

        title->SetPositionShort(12, 7);

        EnableFlagNode(btnPM, NodeFlags.Visible);
        EnableFlagNode(btnFL, NodeFlags.Visible);
        EnableFlagNode(btnPS, NodeFlags.Visible);
        //EnableFlagNode(btnCM, NodeFlags.Visible);

        EnableFlagNode(loaded->RootNode, NodeFlags.Visible);

        for (var i=0; i < loaded->CollisionNodeListCount; i++)
        {
            EnableFlagNode(loaded->CollisionNodeList[i], NodeFlags.Visible);
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
        var windowsResNodeList = windowsResNode->GetComponent()->UldManager.NodeList;

        var nineGridFocus = windowsResNodeList[3];
        var nineGridNoFocus = windowsResNodeList[2];

        var crossButton = windowsResNodeList[6];
        var crossButtonNodeList = crossButton->GetComponent()->UldManager.NodeList;
        
        var crossButtonCollision = crossButtonNodeList[0];
        var crossButtonImg = crossButtonNodeList[1];

        var title = windowsResNodeList[10];

        social->SetSize(40, 126);
        windowsResNode->SetPositionShort(544, 40);
        windowsResNode->SetRotationDegrees(-90);

        crossButton->SetPositionShort(7,85);

        minimizeButton?.Position = new Vector2(600, 7);

        title->SetPositionShort(35, 7);

        DisableFlagNode(btnPM, NodeFlags.Visible);
        DisableFlagNode(btnFL, NodeFlags.Visible);
        DisableFlagNode(btnPS, NodeFlags.Visible);
        //DisableFlagNode(btnCM, NodeFlags.Visible);

        DisableFlagNode(loaded->RootNode, NodeFlags.Visible);

        for (var i=0; i < loaded->CollisionNodeListCount; i++)
        {
            DisableFlagNode(loaded->CollisionNodeList[i], NodeFlags.Visible);
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
            Plugin.Log.Debug($"hidding subaddon {args.AddonName}");
            var loaded = (AtkUnitBase*)args.Addon.Address;

            DisableFlagNode(loaded->RootNode, NodeFlags.Visible);

            for (var i=0; i < loaded->CollisionNodeListCount; i++)
            {
                DisableFlagNode(loaded->CollisionNodeList[i], NodeFlags.Visible);
            }
            Plugin.Log.Debug("needSubAddonHide = false");
            //DisableFlagNode(loaded->RootNode, NodeFlags.Visible);
            needSubAddonHide = -1;
        }
    }
    
    public unsafe void OnPostSetupSubAddon(AddonEvent type, AddonArgs args)
    {   
#if DEBUG
        Plugin.Log.Debug($"OnPostSetupSubAddon {args.AddonName}");
#endif
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
        minimizeButton?.AttachNode(social->RootNode);
        minimizeButton?.Position = new Vector2(600, 7);
        
        var btnCM = social->GetNodeById(2);
        DisableFlagNode(btnCM, NodeFlags.Visible);
    }

    public unsafe void SetupRefresh()
    {
        if (!plugin.Configuration.UsesRefreshButton)
        {
            return;
        }
        var friendList = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList").Address;
        if (friendList == null)
        {
            return;
        }
#if DEBUG
        Plugin.Log.Debug("setup refresh");
#endif
        refreshButton?.AttachNode(friendList->RootNode);
    }

}
