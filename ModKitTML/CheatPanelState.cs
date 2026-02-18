using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ModKitTML
{
    public class CheatPanelState : UIState
    {
        internal UIPanel _bar;
        private readonly List<CheatButton> _buttons = new();

        // Track which features are on
        private bool _noclip;
        private bool _speedHack;
        private bool _fullBright;
        private bool _godMode;
        private bool _enemyFreeze;

        public void ResetToggles()
        {
            _noclip = false;
            _speedHack = false;
            _fullBright = false;
            _godMode = false;
            _enemyFreeze = false;
        }

        public override void OnInitialize()
        {
            _bar = new UIPanel();
            // Width will be set dynamically after adding all buttons
            _bar.Height.Set(41, 0f);
            _bar.Top.Set(-51, 1f); // raised 10px higher
            _bar.BackgroundColor = new Color(60, 50, 40, 115); // ~55% transparency
            _bar.BorderColor = new Color(200, 180, 140, 60);
            _bar.SetPadding(0); // no internal padding so buttons get full height
            Append(_bar);

            float x = 10f;
            float spacing = 50f;

            // 1. Journey Mode — Hot Pink
            AddButton(ref x, spacing, "J", "Journey Mode", new Color(0xFF, 0x69, 0xB4), () =>
            {
                GetModPlayer().CycleJourneyMode();
            }, () => Main.LocalPlayer.difficulty == 3);

            // 2. Noclip — Aquamarine
            AddButton(ref x, spacing, "N", "Noclip", new Color(0x7F, 0xFF, 0xD4), () =>
            {
                _noclip = !_noclip;
                GetModPlayer().noclip = _noclip;
            }, () => _noclip);

            // 3. Speed Hack — Gold/Yellow
            AddButton(ref x, spacing, "S", "Speed Hack", new Color(0xFF, 0xD7, 0x00), () =>
            {
                _speedHack = !_speedHack;
                GetModPlayer().speedHack = _speedHack;
            }, () => _speedHack);

            // 4. Full Bright — Light Yellow
            AddButton(ref x, spacing, "B", "Full Bright", new Color(0xFF, 0xFF, 0xE0), () =>
            {
                _fullBright = !_fullBright;
                GetModPlayer().fullBright = _fullBright;
            }, () => _fullBright);

            // 5. God Mode — Gold
            AddButton(ref x, spacing, "G", "God Mode", new Color(0xD4, 0xAF, 0x37), () =>
            {
                _godMode = !_godMode;
                GetModPlayer().godMode = _godMode;
            }, () => _godMode);

            // 6. Full Heal — Crimson Red
            AddButton(ref x, spacing, "H", "Full Heal", new Color(0xDC, 0x14, 0x3C), () =>
            {
                Main.LocalPlayer.statLife = Main.LocalPlayer.statLifeMax2;
                Main.LocalPlayer.statMana = Main.LocalPlayer.statManaMax2;
                Main.LocalPlayer.HealEffect(Main.LocalPlayer.statLifeMax2);
                Main.NewText("Fully healed!");
            }, null);

            // 7. REPL — Matrix Green
            AddButton(ref x, spacing, "R", "REPL", new Color(0x00, 0xFF, 0x00), () =>
            {
                ModKitUISystem.ToggleREPL();
            }, () => ModKitUISystem.REPLVisible);

            // 8. Extra Acc — Medium Purple (LMB = add slot, RMB = remove slot)
            AddButton(ref x, spacing, "A", "Extra Acc", new Color(0x93, 0x70, 0xDB), () =>
            {
                Main.LocalPlayer.GetModPlayer<ExtraAccessoryPlayer>().CycleSlots();
            }, () => Main.LocalPlayer.GetModPlayer<ExtraAccessoryPlayer>().EnabledSlots > 0,
            () =>
            {
                Main.LocalPlayer.GetModPlayer<ExtraAccessoryPlayer>().DecrementSlots();
            });

            // 9. Freeze Enemies — Deep Sky Blue
            AddButton(ref x, spacing, "F", "Freeze Enemies", new Color(0x00, 0xBF, 0xFF), () =>
            {
                _enemyFreeze = !_enemyFreeze;
                GetModPlayer().freezeEnemies = _enemyFreeze;
            }, () => _enemyFreeze);

            // 10. Reveal Map — Wheat/Beige
            AddButton(ref x, spacing, "M", "Reveal Map", new Color(0xF5, 0xDE, 0xB3), () =>
            {
                RevealMap();
            }, null);

            // Set bar width to 505, centered
            _bar.Width.Set(505, 0f);
            _bar.Left.Set(-505 / 2f, 0.5f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Prevent weapon swing, item use, and game tooltips when mouse is over the cheat bar
            if (_bar != null)
            {
                var barDim = _bar.GetDimensions();
                if (Main.MouseScreen.X >= barDim.X && Main.MouseScreen.X <= barDim.X + barDim.Width
                 && Main.MouseScreen.Y >= barDim.Y && Main.MouseScreen.Y <= barDim.Y + barDim.Height)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = new Item();
                    Main.hoverItemName = "";
                }
            }
        }

        private void AddButton(ref float x, float spacing, string letter, string tooltip, Color letterColor, Action onClick, Func<bool> isOn, Action onRightClick = null)
        {
            var btn = new CheatButton(letter, tooltip, letterColor, onClick, isOn, onRightClick);
            btn.Left.Set(x, 0f);
            btn.Top.Set(9, 0f);
            btn.Width.Set(35, 0f);
            btn.Height.Set(23, 0f);
            _bar.Append(btn);
            _buttons.Add(btn);
            x += spacing;
        }

        private ModKitPlayer GetModPlayer() => Main.LocalPlayer.GetModPlayer<ModKitPlayer>();

        private void RevealMap()
        {
            if (Main.netMode == 0)
                ModKitPlayer.RevealWholeMap();
            else
            {
                var center = Main.player[Main.myPlayer].Center.ToTileCoordinates();
                ModKitPlayer.RevealAroundPoint(center.X, center.Y);
            }
        }
    }

    public class CheatButton : UIElement
    {
        private readonly string _letter;
        private readonly string _tooltip;
        private readonly Color _letterColor;
        private readonly Action _onClick;
        private readonly Action _onRightClick;
        private readonly Func<bool> _isOn; // null = always grey (fire-and-forget)
        private bool _hovered;

        public CheatButton(string letter, string tooltip, Color letterColor, Action onClick, Func<bool> isOn, Action onRightClick = null)
        {
            _letter = letter;
            _tooltip = tooltip;
            _letterColor = letterColor;
            _onClick = onClick;
            _isOn = isOn;
            _onRightClick = onRightClick;
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            _hovered = true;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            _hovered = false;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            _onClick?.Invoke();
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            _onRightClick?.Invoke();
            if (_onRightClick != null)
                SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            Rectangle rect = dims.ToRectangle();

            bool on = _isOn?.Invoke() ?? false;

            // Background: green if on, grey if off or fire-and-forget
            Color bg = on ? new Color(60, 180, 80, 220) : new Color(50, 50, 70, 200);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, bg);

            // Draw letter centered
            DynamicSpriteFont font = FontAssets.DeathText.Value;
            Vector2 size = ChatManager.GetStringSize(font, _letter, Vector2.One);
            float scale = 0.45f;
            Vector2 pos = new(rect.X + rect.Width / 2f - size.X * scale / 2f,
                              rect.Y + rect.Height / 2f - size.Y * scale / 2f);
            // Always use the assigned letter color, regardless of on/off or hover state
            spriteBatch.DrawString(font, _letter, pos, _letterColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // Tooltip on hover
            if (_hovered)
            {
                DynamicSpriteFont tooltipFont = FontAssets.MouseText.Value;
                Vector2 tooltipSize = ChatManager.GetStringSize(tooltipFont, _tooltip, Vector2.One);
                Vector2 tooltipPos = new(rect.X + rect.Width / 2f - tooltipSize.X / 2f, rect.Y - 28);
                Rectangle tooltipRect = new((int)tooltipPos.X - 4, (int)tooltipPos.Y - 2,
                    (int)tooltipSize.X + 8, (int)tooltipSize.Y + 4);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, tooltipRect, new Color(20, 20, 40, 220));
                spriteBatch.DrawString(tooltipFont, _tooltip, tooltipPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}