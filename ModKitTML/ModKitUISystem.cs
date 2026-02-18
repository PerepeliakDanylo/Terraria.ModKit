using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModKitTML
{
    public class ModKitUISystem : ModSystem
    {
        internal static UserInterface cheatInterface;
        internal static UserInterface replInterface;
        internal static CheatPanelState cheatState;
        internal static REPLState replState;

        private static bool _replVisible;
        public static bool REPLVisible => _replVisible;

        private static bool _cheatPanelVisible = true;
        public static bool CheatPanelVisible => _cheatPanelVisible;

        public static bool IsREPLFocused => _replVisible && replState != null && replState.IsFocused;
        public static bool IsREPLMouseOver => _replVisible && replState != null && replState.IsMouseOver;

        private GameTime _lastGameTime;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                cheatInterface = new UserInterface();
                replInterface = new UserInterface();

                cheatState = new CheatPanelState();
                cheatState.Activate();
                cheatInterface.SetState(cheatState);

                replState = new REPLState();
                replState.Activate();
            }
        }

        public override void Unload()
        {
            // Cleanup
            if (replState != null && replState.IsFocused)
                replState.UnfocusInput();

            cheatInterface = null;
            replInterface = null;
            cheatState = null;
            replState = null;
            _replVisible = false;
            _cheatPanelVisible = true;
        }

        public override void OnWorldLoad()
        {
            cheatState?.ResetToggles();
            if (_replVisible)
            {
                _replVisible = false;
                replState?.UnfocusInput();
                replInterface?.SetState(null);
            }
        }

        public static void ToggleREPL()
        {
            _replVisible = !_replVisible;
            if (_replVisible)
            {
                replInterface?.SetState(replState);
                replState?.ResetPosition();
            }
            else
            {
                replState?.UnfocusInput();
                replInterface?.SetState(null);
            }
        }

        public static void ToggleCheatPanel()
        {
            _cheatPanelVisible = !_cheatPanelVisible;
            if (_cheatPanelVisible)
            {
                cheatInterface?.SetState(cheatState);
            }
            else
            {
                cheatInterface?.SetState(null);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _lastGameTime = gameTime;

            if (_cheatPanelVisible)
                cheatInterface?.Update(gameTime);

            if (_replVisible)
            {
                replInterface?.Update(gameTime);

                if (replState != null && replState.IsFocused)
                    Main.LocalPlayer.mouseInterface = true;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                // Extra Accessory slots
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "ModKitTML: Extra Accessories",
                    delegate
                    {
                        ExtraAccessoryUI.DrawExtraSlots(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI));
                mouseTextIndex++;

                if (_cheatPanelVisible)
                {
                    layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                        "ModKitTML: Cheat Panel",
                        delegate
                        {
                            cheatInterface?.Draw(Main.spriteBatch, _lastGameTime ?? new GameTime());
                            return true;
                        },
                        InterfaceScaleType.UI));
                    mouseTextIndex++;
                }

                if (_replVisible)
                {
                    layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                        "ModKitTML: REPL",
                        delegate
                        {
                            replInterface?.Draw(Main.spriteBatch, _lastGameTime ?? new GameTime());
                            return true;
                        },
                        InterfaceScaleType.UI));
                }
            }
        }
    }
}