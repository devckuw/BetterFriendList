using Dalamud.Configuration;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;

namespace BetterFriendList;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // settings
    public bool RefreshFriendOnOpen { get; set; } = false;
    public bool SortOnDifferentTab { get; set; } = true;
    public bool FixedColumnSize { get; set; } = true;
    public VirtualKey VirtualKey { get; set; } = VirtualKey.NO_KEY;

    // data
    public Dictionary<ulong, Vector4> FriendsColors { get; set; } = new Dictionary<ulong, Vector4>();
    public Dictionary<ulong, string> FriendNotes { get; set; } = new Dictionary<ulong, string>();

    // options in main window
    public Sorting Sorting { get; set; } = Sorting.Oldest;
    public bool OnlineFirst { get; set; } = true; 

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
