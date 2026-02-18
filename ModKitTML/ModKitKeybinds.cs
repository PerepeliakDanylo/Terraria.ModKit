using Terraria.ModLoader;

namespace ModKitTML
{
    public static class ModKitKeybinds
    {
        public static ModKeybind TogglePanelKey { get; private set; }
        public static ModKeybind FlyModeKey { get; private set; }
        public static ModKeybind UnlockAllItemsKey { get; private set; }
        public static ModKeybind UnlockBestiaryKey { get; private set; }
        public static ModKeybind LockBestiaryKey { get; private set; }
        public static ModKeybind IncreaseFlySpeedKey { get; private set; }
        public static ModKeybind DecreaseFlySpeedKey { get; private set; }
        public static ModKeybind InstantReviveKey { get; private set; }
        public static ModKeybind ToggleREPLKey { get; private set; }
        public static ModKeybind JourneyModeKey { get; private set; }
        public static ModKeybind SpeedHackKey { get; private set; }
        public static ModKeybind FullBrightKey { get; private set; }
        public static ModKeybind GodModeKey { get; private set; }
        public static ModKeybind FullHealKey { get; private set; }
        public static ModKeybind ExtraAccKey { get; private set; }
        public static ModKeybind FreezeEnemiesKey { get; private set; }

        public static void Load(Mod mod)
        {
            // Display names here become the labels in Settings > Controls
            TogglePanelKey = KeybindLoader.RegisterKeybind(mod, "Cheat Panel", "NumPad0");
            FlyModeKey = KeybindLoader.RegisterKeybind(mod, "Noclip", "NumPad4");
            UnlockAllItemsKey = KeybindLoader.RegisterKeybind(mod, "Unlock Items", "NumPad5");
            UnlockBestiaryKey = KeybindLoader.RegisterKeybind(mod, "Unlock Bestiary", "NumPad3");
            LockBestiaryKey = KeybindLoader.RegisterKeybind(mod, "Lock Bestiary", "NumPad7");
            IncreaseFlySpeedKey = KeybindLoader.RegisterKeybind(mod, "Noclip Speed+", "OemOpenBrackets");
            DecreaseFlySpeedKey = KeybindLoader.RegisterKeybind(mod, "Noclip Speed-", "OemCloseBrackets");
            InstantReviveKey = KeybindLoader.RegisterKeybind(mod, "Instant Revive", "NumPad9");
            ToggleREPLKey = KeybindLoader.RegisterKeybind(mod, "REPL Console", "F10");
            JourneyModeKey = KeybindLoader.RegisterKeybind(mod, "Journey Mode", "NumPad1");
            SpeedHackKey = KeybindLoader.RegisterKeybind(mod, "Speed Hack", "NumPad2");
            FullBrightKey = KeybindLoader.RegisterKeybind(mod, "Full Bright", "NumPad6");
            GodModeKey = KeybindLoader.RegisterKeybind(mod, "God Mode", "NumPad8");
            FullHealKey = KeybindLoader.RegisterKeybind(mod, "Full Heal", "F9");
            ExtraAccKey = KeybindLoader.RegisterKeybind(mod, "Extra Acc", "F11");
            FreezeEnemiesKey = KeybindLoader.RegisterKeybind(mod, "Freeze Enemies", "F12");
        }

        public static void Unload()
        {
            TogglePanelKey = null;
            FlyModeKey = null;
            UnlockAllItemsKey = null;
            UnlockBestiaryKey = null;
            LockBestiaryKey = null;
            IncreaseFlySpeedKey = null;
            DecreaseFlySpeedKey = null;
            InstantReviveKey = null;
            ToggleREPLKey = null;
            JourneyModeKey = null;
            SpeedHackKey = null;
            FullBrightKey = null;
            GodModeKey = null;
            FullHealKey = null;
            ExtraAccKey = null;
            FreezeEnemiesKey = null;
        }
    }
}