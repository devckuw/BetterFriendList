using BetterFriendList.GameAddon;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace BetterFriendList
{
    internal class KeyboardHelper
    {
        private bool wasActivte = false;
        private VirtualKey virtualKey;
        private List<VirtualKey> keyState;
        private Plugin plugin;

        #region singleton
        private KeyboardHelper(Plugin p, VirtualKey key)
        {
            virtualKey = key;
            Plugin.Framework.Update += OnUpdate;
            plugin = p;
        }

        public static void Initialize(Plugin p, VirtualKey key) { Instance = new KeyboardHelper(p, key); }

        public static KeyboardHelper Instance { get; private set; } = null!;

        ~KeyboardHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            Plugin.Framework.Update -= OnUpdate;
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
        #endregion

        private List<VirtualKey> GetPressedKeys()
        {
            List<VirtualKey> pressed = new List<VirtualKey>();

            foreach (var key in Plugin.KeyState.GetValidVirtualKeys())
            {
                if((int)key > 4 && Plugin.KeyState[key])
                {
                    pressed.Add(key);
                }
            }

            return pressed;
        }

        public static bool IsKeyPressed(VirtualKey key)
        {
            if (Instance == null)
            {
                return false;
            }
            return Instance.keyState.Contains(key);
        }

        public static List<VirtualKey> PressedKeys()
        {
            if (Instance == null)
            {
                return new List<VirtualKey>();
            }
            return Instance.keyState;
        }

        public static void SetKey(VirtualKey key)
        {
            if (Instance == null)
            {
                Plugin.Log.Debug("Instance not init");
                return ;
            }
            Instance.virtualKey = key;
        }

        private void OnUpdate(IFramework framework)
        {
            keyState = GetPressedKeys();
            if (virtualKey == VirtualKey.NO_KEY)
            {
                return;
            }
            if (!Plugin.KeyState[virtualKey])
            {
                wasActivte = false;
                return;
            }
            if (!ChatHelper.IsInputTextActive() && !wasActivte)
            {
                wasActivte = true;
                plugin.ToggleMainUI();
            }
        }

    }
}
