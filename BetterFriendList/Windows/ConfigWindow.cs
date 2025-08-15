using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Common.Lua;
using Serilog;
using System;
using System.Linq;
using System.Numerics;
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

        Size = new Vector2(390, 230);
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

        ImGui.NewLine();
        DrawExplication();
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
