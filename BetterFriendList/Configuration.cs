using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BetterFriendList;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool RefreshFriendOnOpen { get; set; } = false;
    public bool SortOnDifferentTab { get; set; } = true;

    public Dictionary<ulong, Vector4> FriendsColors { get; set; } = new Dictionary<ulong, Vector4>();

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
