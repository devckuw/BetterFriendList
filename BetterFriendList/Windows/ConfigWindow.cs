using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Lumina.Excel.Sheets;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace BetterFriendList.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    VirtualKey key;
    string keyString;
    bool editKey = false;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Better Friend List Settings###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(390, 250);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var refreshFriendOnOpen = Configuration.RefreshFriendOnOpen;
        if (ImGui.Checkbox("Refresh friend list on opening", ref refreshFriendOnOpen))
        {
            Configuration.RefreshFriendOnOpen = refreshFriendOnOpen;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }

        var sortOnDifferentTab = Configuration.SortOnDifferentTab;
        if (ImGui.Checkbox("Open filter options with right click instead of showing above.", ref sortOnDifferentTab))
        {
            Configuration.SortOnDifferentTab = sortOnDifferentTab;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }

        key = Configuration.VirtualKey;
        keyString = key.ToString();
        //ImGui.InputText($"##Keybind", ref keyString, 200, ImGuiInputTextFlags.ReadOnly);
        ImGui.Text($"Keybind : {keyString}");
        ImGui.SameLine();
        if (editKey)
        {
            if (ImGui.Button("Save##editkeybutton"))
            {
                editKey = false;
            }
        }
        else
        {
            if (ImGui.Button("Edit keybind##editkeybutton"))
            {
                editKey = true;
            }
        }
        DrawCommon.IsHovered("Backspace to clear\nIf a key doesnt work it might used by another thing.");

        if (editKey)
        {
            if (KeyboardHelper.IsKeyPressed(VirtualKey.BACK))
            {
                key = VirtualKey.NO_KEY;
                keyString = key.ToString();
                KeyboardHelper.SetKey(key);
                Configuration.VirtualKey = key;
                Configuration.Save();
            }
            else
            {
                VirtualKey keyPressed = KeyboardHelper.PressedKeys().FirstOrDefault(VirtualKey.NO_KEY);
                if (keyPressed != VirtualKey.NO_KEY)
                {
                    key = keyPressed;
                    keyString = key.ToString();
                    KeyboardHelper.SetKey(key);
                    Configuration.VirtualKey = keyPressed;
                    Configuration.Save();
                }
            }
        }
        if (ImGui.Button("Import Colors"))
        {
            if (ImportColors())
            {
                Plugin.Log.Debug("Color Import Success");
            }
            else
            {
                Plugin.Log.Debug("Color Import Failed");
            }
            
        }

        DrawCommon.IsHovered("Import colors used in chat by Simple Tweaks");

        ImGui.NewLine();
        DrawExplication();

    }

    public unsafe bool ImportColors()
    {
        var agent = AgentFriendlist.Instance();
        if (agent == null) return false;

        if (agent->InfoProxy == null) return false;

        string simpleTweaksPath = Plugin.PluginInterface.ConfigDirectory.FullName.Replace("BetterFriendList", "SimpleTweaksPlugin");
        string nameColorConfig = simpleTweaksPath + "\\ChatTweaks@ChatNameColours.json";

        if (!File.Exists(nameColorConfig))
        {
            Plugin.Log.Information("config file not present");
            return false;
            
        }
        Plugin.Log.Information("config file present");

        Dictionary<string, ulong> friends = new Dictionary<string, ulong>();

        for (var i = 0U; i < agent->InfoProxy->EntryCount; i++)
        {
            var friend = agent->InfoProxy->GetEntry(i);
            Plugin.DataManager.GetExcelSheet<World>().TryGetRow(friend->HomeWorld, out var friendHomeWorld);
            friends.Add(friend->NameString + "@" + friendHomeWorld.Name.ExtractText(), friend->ContentId);
        }

        List<ForcedColour> forcedColours = ReadJsonColors(nameColorConfig);

        foreach (ForcedColour forcedColour in forcedColours)
        {
            if (friends.ContainsKey(forcedColour.PlayerName + "@" + forcedColour.WorldName))
            {
                Configuration.FriendsColors[friends[forcedColour.PlayerName + "@" + forcedColour.WorldName]] = new Vector4((float)forcedColour.Color["X"], (float)forcedColour.Color["Y"], (float)forcedColour.Color["Z"], 1.0f);
                Plugin.Log.Debug($"Adding {forcedColour.PlayerName + "@" + forcedColour.WorldName} with color {forcedColour.Color["X"]},{forcedColour.Color["Y"]},{forcedColour.Color["Y"]}");
            }
        }

        Configuration.Save();

        return true;
       
    }

    public List<ForcedColour> ReadJsonColors(string path)
    {
        string jsonString = File.ReadAllText(path);
        SimpleTweaksChatNameColor? simpleTweaksChatNameColor = JsonSerializer.Deserialize<SimpleTweaksChatNameColor>(jsonString);

        if (simpleTweaksChatNameColor != null)
        {
            return simpleTweaksChatNameColor.ForcedColours;
        }

        return new List<ForcedColour>();
    }

    public void DrawExplication()
    {
        string txt = "Click on friends name to change their color.\n" +
            "Right Click wherever to show the filter options.\n" +
            "Refresh buttons are to pull info from the server.\n" +
            "More sorting things will be added later.\n" +
            "You can give idea on discord :)";
        ImGui.Text(txt);
    }
}
