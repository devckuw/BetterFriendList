using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using KamiToolKit.Nodes;
using KamiToolKit.Extensions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Dalamud.Game.ClientState;
using SamplePlugin;
using BetterFriendList;

namespace SamplePlugin.GameAddon;
public class InfoProxyManager : IDisposable
{
    private InfoProxyManager(Plugin p)
    {
        plugin = p;
        SetupHook();

        Plugin.ClientState.Login += OnLogin;
        Plugin.ClientState.Logout += OnLogout;
        Plugin.ClientState.InstanceChanged += OnInstanceChanged;
        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        Plugin.ClientState.ZoneInit += OnZoneInit;

        if (Plugin.ClientState.IsLoggedIn)
        {
            wasAllowed = Plugin.IsRequestDataAllowed();
            lastWorld = Plugin.PlayerState.CurrentWorld.RowId;
#if DEBUG
            Plugin.Log.Debug($"Loading plugin but Logged In => wasAllowed = true worldid:{lastWorld}");
#endif
        }
        else
        {
#if DEBUG
            Plugin.Log.Debug($"Loading plugin but Logged Out => wasAllowed = false worldid:random");
#endif
            wasAllowed = false;
            lastWorld = uint.MaxValue;
        }
    }

    public static void Initialize(Plugin p) { Instance = new InfoProxyManager(p); }

    public static InfoProxyManager Instance { get; private set; } = null!;

    ~InfoProxyManager()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);

        Plugin.ClientState.Login -= OnLogin;
        Plugin.ClientState.Logout -= OnLogout;
        Plugin.ClientState.InstanceChanged -= OnInstanceChanged;
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Plugin.ClientState.ZoneInit -= OnZoneInit;

        applyFiltersHook.Disable();
        applyFiltersHook.Dispose();

        requestDataHook.Disable();
        requestDataHook.Dispose();

        endRequestHook.Disable();
        endRequestHook.Dispose();

        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Instance = null!;
    }

    public Hook<InfoProxyCommonList.Delegates.ApplyFilters> applyFiltersHook;
    public Hook<InfoProxyCommonList.Delegates.RequestData> requestDataHook;
    private Hook<InfoProxyCommonList.Delegates.EndRequest> endRequestHook;
    private Plugin plugin;
    private bool wasAllowed;
    private uint lastWorld;

    public unsafe void SetupHook()
    {
        applyFiltersHook = Plugin.GameInteropProvider.HookFromAddress<InfoProxyCommonList.Delegates.ApplyFilters>(InfoProxyCommonList.MemberFunctionPointers.ApplyFilters, ApplyFildersDetour);
        applyFiltersHook.Enable();

        requestDataHook = Plugin.GameInteropProvider.HookFromAddress<InfoProxyCommonList.Delegates.RequestData>(InfoProxyFriendList.Instance()->VirtualTable->RequestData, RequestDataDetour);
        requestDataHook.Enable();

        endRequestHook = Plugin.GameInteropProvider.HookFromAddress<InfoProxyCommonList.Delegates.EndRequest>(InfoProxyFriendList.Instance()->VirtualTable->EndRequest, EndRequestDetour);
        endRequestHook.Enable();
    }

    public void OnLogin()
    {
#if DEBUG
        Plugin.Log.Debug($"Login");
#endif
    }

    public void OnLogout(int type, int code)
    {
#if DEBUG
        Plugin.Log.Debug($"Logout");
#endif
        wasAllowed = false;
        lastWorld = uint.MaxValue;
    }

    public void OnInstanceChanged(uint id)
    {
#if DEBUG
        Plugin.Log.Debug($"Instance Changed : {id}");
#endif
    }

    public unsafe void OnTerritoryChanged(ushort id)
    {
#if DEBUG
        Plugin.Log.Debug($"Territory Changed : {id}");
#endif

        bool isAllowed = Plugin.IsRequestDataAllowed();

        if (wasAllowed == false && isAllowed)
        {
            if (!plugin.Configuration.RefreshFriendOnOpenNative)
            {
                requestDataHook.Original((InfoProxyCommonList*)InfoProxyFriendList.Instance());
            }
#if DEBUG
            Plugin.Log.Debug("trigger refresh");
#endif
        }
        wasAllowed = isAllowed;
    }

    public unsafe void OnZoneInit(ZoneInitEventArgs args)
    {
#if DEBUG
        Plugin.Log.Debug($"Zone Init : {args.TerritoryType.RowId} {Plugin.ClientState.IsLoggedIn} {Plugin.PlayerState.CurrentWorld.Value.Name}");
#endif

        if (Plugin.PlayerState.CurrentWorld.RowId != lastWorld)
        {
            lastWorld = Plugin.PlayerState.CurrentWorld.RowId;
            wasAllowed = false;
            if (wasAllowed == false && Plugin.IsRequestDataAllowed())
            {
                wasAllowed = true;
                if (!plugin.Configuration.RefreshFriendOnOpenNative)
                {
                    requestDataHook.Original((InfoProxyCommonList*)InfoProxyFriendList.Instance());
                }
#if DEBUG
                Plugin.Log.Debug("trigger refresh");
#endif
            }
        }
    }

    public unsafe static bool checkFriendInvite()
    {   
        bool ret = false;
        var proxy = InfoProxyFriendList.Instance();
        for (uint i = 0; i < proxy->EntryCount; i++)
        {
            uint flag = proxy->GetEntry(i)->ExtraFlags & 0x30;
            if (flag == 0x20)
            {
                Plugin.Log.Debug($"{proxy->GetEntry(i)->NameString}");
                ret = true;
            }

            if (flag == 0x30)
            {
                Plugin.Log.Debug($"{proxy->GetEntry(i)->NameString}");
                ret = true;
            }
        }
        return ret;
    }

    public unsafe void ApplyFildersDetour(InfoProxyCommonList* infoProxyCommonList)
    {
#if DEBUG
        if (infoProxyCommonList == InfoProxyFriendList.Instance())
        {
            Plugin.Log.Debug("friendlist sorting");
        }
#endif
        applyFiltersHook.Original(infoProxyCommonList);

        if (plugin.Configuration.UsesNotes == true && infoProxyCommonList == InfoProxyFriendList.Instance())
        {
            ApplyNotesOnTooltip();
        }
        plugin.MainWindow.SortFriends();
    }

    public unsafe bool RequestDataDetour(InfoProxyCommonList* infoProxyCommonList)
    {   

        if (infoProxyCommonList == InfoProxyFriendList.Instance() && !plugin.Configuration.RefreshFriendOnOpenNative)
        {
            if (checkFriendInvite())
            {
                return requestDataHook.Original(infoProxyCommonList);
            }
#if DEBUG
            Plugin.Log.Debug("RequestDataDetour friendlist");
#endif
            return true;
        }
#if DEBUG
        Plugin.Log.Debug("RequestDataDetour");
#endif
        return requestDataHook.Original(infoProxyCommonList);
    }

    public unsafe void EndRequestDetour(InfoProxyCommonList* infoProxyCommonList)
    {
#if DEBUG
        if (infoProxyCommonList == InfoProxyFriendList.Instance())
        {
            Plugin.Log.Debug("EndRequestDetour friendlist");
        }
#endif
        //Plugin.Log.Debug("EndRequestDetour");
        endRequestHook.Original(infoProxyCommonList);
    }

    public unsafe void ApplyNotesOnTooltip()
    {
        var proxy = InfoProxyFriendList.Instance();
        var atkStage = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance();
        if (atkStage == null) return;
        var array = atkStage->GetStringArrayData(FFXIVClientStructs.FFXIV.Component.GUI.StringArrayType.FriendList);
        if (array == null) return;

        for (int i=0; i < proxy->EntryCount; i++)
        {
            string? note;
            plugin.Configuration.FriendNotes.TryGetValue(proxy->GetEntry((uint)i)->ContentId, out note);
            if (note != null && note != string.Empty)
            {
                array->SetValue(i*5+3, note);
            }
        }
    }

    public unsafe void ApplyColor()
    {
        
    }
}