using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace BetterFriendList;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool RefreshFriendOnOpen { get; set; } = false;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
