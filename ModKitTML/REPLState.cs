using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ModKitTML
{
    public class REPLState : UIState
    {
        // Panel dimensions — sized to fit all templates with hints
        private const float PanelWidth = 870f;
        private const float PanelHeight = 440f;

        private UIPanel _panel;
        private UIPanel _contentPanel;

        // Text input — follows Terraria.ModKit NewUITextBox pattern
        private string _inputText = "";
        private bool _inputFocused;
        private UIPanel _inputPanel;
        private int _frameCount;

        // Track previous Esc/Enter/Mouse state for reliable edge detection
        private bool _prevEscDown;
        private bool _prevEnterDown;
        private bool _prevMouseLeft;

        // Dragging
        private bool _dragging;
        private Vector2 _dragOffset;
        private Vector2 _panelPosition;
        private bool _positionInitialized;

        // Longest command for alignment: "Main.LocalPlayer.HeldItem.consumable = false;" (46 chars)
        // All hints will be drawn at a fixed X offset from xBase for alignment

        // Template commands with hints
        private static readonly TemplateEntry[] Templates = new TemplateEntry[]
        {
            new("Main.LocalPlayer.HeldItem.damage = 69;",        "// Item damage.", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.stack = 77;",         "// Item stack count.", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.defense = 42;",       "// Item defense.", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.potion = false;",     "// Remove potion cooldown.", false),
            new("Main.LocalPlayer.HeldItem.consumable = false;", "// Make potion infinite (not consumed on use).", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.useTime = 17;",       "// Speed up use animation (allows spamming).", false),
            new("Main.LocalPlayer.HeldItem.useAnimation = 2;",   "// Speed up use animation (allows spamming).", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.scale = 1.5f;",       "// Change item size (visual and hitbox).", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.prefix = 81;",        "// Item prefix (modifier).", false),
            new(null, null, true),
            new("Main.LocalPlayer.HeldItem.type = 4;",           "// Change held item type.", false),
        };

        private REPLBackend _backend;

        public bool IsFocused => _inputFocused;
        public bool IsMouseOver => _panel != null && _panel.ContainsPoint(Main.MouseScreen);

        private struct TemplateEntry
        {
            public string Command;
            public string Hint;
            public bool IsSpacer;
            public TemplateEntry(string cmd, string hint, bool spacer) { Command = cmd; Hint = hint; IsSpacer = spacer; }
        }

        public override void OnInitialize()
        {
            _backend = new REPLBackend();

            _panel = new UIPanel();
            _panel.Width.Set(PanelWidth, 0f);
            _panel.Height.Set(PanelHeight, 0f);
            _panel.BackgroundColor = new Color(20, 20, 35, 230);
            _panel.BorderColor = new Color(80, 80, 120, 200);
            Append(_panel);

            // Title bar area (used for dragging)
            var titleBar = new UIPanel();
            titleBar.Width.Set(0, 1f);
            titleBar.Height.Set(34, 0f);
            titleBar.Top.Set(0, 0f);
            titleBar.Left.Set(0, 0f);
            titleBar.BackgroundColor = new Color(30, 30, 55, 240);
            titleBar.BorderColor = new Color(60, 60, 100, 150);
            _panel.Append(titleBar);

            // Title text
            var title = new UIText("REPL Console", 0.5f, true);
            title.Top.Set(-2, 0f);
            title.Left.Set(10, 0f);
            titleBar.Append(title);

            // Content area for templates
            _contentPanel = new UIPanel();
            _contentPanel.Width.Set(-20, 1f);
            _contentPanel.Height.Set(-90, 1f);
            _contentPanel.Top.Set(40, 0f);
            _contentPanel.Left.Set(10, 0f);
            _contentPanel.BackgroundColor = new Color(10, 10, 20, 200);
            _contentPanel.BorderColor = new Color(50, 50, 80, 150);
            _panel.Append(_contentPanel);

            // Input panel
            _inputPanel = new UIPanel();
            _inputPanel.Width.Set(-80, 1f);
            _inputPanel.Height.Set(30, 0f);
            _inputPanel.Top.Set(-40, 1f);
            _inputPanel.Left.Set(10, 0f);
            _inputPanel.BackgroundColor = new Color(15, 15, 30, 220);
            _inputPanel.BorderColor = new Color(60, 60, 100, 180);
            _panel.Append(_inputPanel);

            // Send button
            var sendBtn = new UITextPanel<string>("Run", 0.8f);
            sendBtn.Width.Set(55, 0f);
            sendBtn.Height.Set(30, 0f);
            sendBtn.Top.Set(-40, 1f);
            sendBtn.Left.Set(-65, 1f);
            sendBtn.BackgroundColor = new Color(60, 140, 60, 220);
            sendBtn.OnLeftClick += (evt, el) => ExecuteInput();
            sendBtn.OnMouseOver += (evt, el) =>
            {
                ((UITextPanel<string>)el).BackgroundColor = new Color(90, 190, 90, 240);
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            sendBtn.OnMouseOut += (evt, el) =>
            {
                ((UITextPanel<string>)el).BackgroundColor = new Color(60, 140, 60, 220);
            };
            _panel.Append(sendBtn);
        }

        public void ResetPosition()
        {
            _panelPosition = new Vector2(
                Main.screenWidth / 2f - PanelWidth / 2f,
                Main.screenHeight / 2f - PanelHeight / 2f);
            _positionInitialized = true;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (_panel == null) return;
            _panel.HAlign = 0f;
            _panel.VAlign = 0f;
            _panel.Left.Set(_panelPosition.X, 0f);
            _panel.Top.Set(_panelPosition.Y, 0f);
            _panel.Recalculate();
        }

        public void FocusInput()
        {
            if (!_inputFocused)
            {
                Main.clrInput();
                _inputFocused = true;
            }
        }

        public void UnfocusInput()
        {
            if (_inputFocused)
            {
                _inputFocused = false;
            }
        }

        private void ExecuteInput()
        {
            string code = _inputText?.Trim();
            if (string.IsNullOrEmpty(code)) return;
            _inputText = "";

            var result = _backend.Execute(code);
            if (!string.IsNullOrEmpty(result.output))
            {
                Color resultColor = result.isError ? new Color(255, 100, 100) : new Color(180, 255, 180);
                Main.NewText(result.output, resultColor.R, resultColor.G, resultColor.B);
            }
            else
            {
                Main.NewText("> " + code, 130, 200, 255);
            }
        }

        /// <summary>
        /// Template click pastes code into the input field for editing, does NOT execute.
        /// </summary>
        private void PasteTemplate(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            _inputText = code;
            FocusInput();
        }

        private static bool JustPressed(Keys key)
        {
            return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_positionInitialized)
                ResetPosition();

            // Prevent game mouse input and tooltips when over REPL
            if (_panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = new Item();
                Main.hoverItemName = "";
            }

            // Visual focus indicator on input field
            _inputPanel.BorderColor = _inputFocused
                ? new Color(100, 140, 255, 220)
                : new Color(60, 60, 100, 180);

            // --- Dragging logic (title bar only) ---
            CalculatedStyle panelDims = _panel.GetDimensions();
            Rectangle titleBarRect = new(
                (int)panelDims.X, (int)panelDims.Y,
                (int)panelDims.Width, 34);

            if (Main.mouseLeft)
            {
                if (!_dragging && Main.mouseLeftRelease && titleBarRect.Contains(Main.mouseX, Main.mouseY))
                {
                    _dragging = true;
                    _dragOffset = new Vector2(Main.mouseX - _panelPosition.X, Main.mouseY - _panelPosition.Y);
                }
                if (_dragging)
                {
                    _panelPosition = new Vector2(Main.mouseX - _dragOffset.X, Main.mouseY - _dragOffset.Y);
                    _panelPosition.X = MathHelper.Clamp(_panelPosition.X, 0, Main.screenWidth - PanelWidth);
                    _panelPosition.Y = MathHelper.Clamp(_panelPosition.Y, 0, Main.screenHeight - PanelHeight);
                    ApplyPosition();
                }
            }
            else
            {
                _dragging = false;
            }

            // --- Suppress chat while REPL input is focused ---
            if (_inputFocused)
            {
                // Continuously suppress chat opening while focused
                Main.drawingPlayerChat = false;
                Main.chatText = "";
                Main.chatRelease = false;

                // Esc = toggle inventory manually
                // Use raw Keyboard.GetState() for reliable detection (Main.inputText can be consumed by WritingText)
                bool escNow = Keyboard.GetState().IsKeyDown(Keys.Escape);
                if (escNow && !_prevEscDown)
                {
                    Main.playerInventory = !Main.playerInventory;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
                _prevEscDown = escNow;

                // Enter = execute
                // Use raw Keyboard.GetState() for reliable detection (Main.inputText can be consumed by WritingText)
                bool enterNow = Keyboard.GetState().IsKeyDown(Keys.Enter);
                if (enterNow && !_prevEnterDown)
                {
                    ExecuteInput();
                }
                _prevEnterDown = enterNow;

                // Ctrl+A = clear input text
                if ((Main.inputText.IsKeyDown(Keys.LeftControl) || Main.inputText.IsKeyDown(Keys.RightControl))
                    && JustPressed(Keys.A))
                {
                    _inputText = "";
                }

                // Ctrl+C = copy input text to clipboard (SDL2 via FNA)
                if ((Main.inputText.IsKeyDown(Keys.LeftControl) || Main.inputText.IsKeyDown(Keys.RightControl))
                    && JustPressed(Keys.C))
                {
                    if (!string.IsNullOrEmpty(_inputText))
                    {
                        try { SDL2.SDL.SDL_SetClipboardText(_inputText); }
                        catch { }
                    }
                }
            }
            else
            {
                // Reset Esc/Enter tracking when unfocused
                _prevEscDown = Keyboard.GetState().IsKeyDown(Keys.Escape);
                _prevEnterDown = Keyboard.GetState().IsKeyDown(Keys.Enter);
            }

            // --- Focus/unfocus by mouse click (track manually, Main.mouseLeftRelease gets consumed by UI) ---
            bool mouseNow = Main.mouseLeft;
            if (mouseNow && !_prevMouseLeft && !_dragging)
            {
                CalculatedStyle ipDim = _inputPanel.GetDimensions();
                bool clickedInput = Main.MouseScreen.X >= ipDim.X && Main.MouseScreen.X < ipDim.X + ipDim.Width
                                 && Main.MouseScreen.Y >= ipDim.Y && Main.MouseScreen.Y < ipDim.Y + ipDim.Height;
                if (clickedInput)
                    FocusInput();
                else
                    UnfocusInput();
            }
            _prevMouseLeft = mouseNow;
        }

        private Vector2 MeasureString(DynamicSpriteFont font, string text)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;
            return ChatManager.GetStringSize(font, text, Vector2.One);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // ===== CRITICAL: Handle text input in Draw pass =====
            if (_inputFocused)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newText = Main.GetInputText(_inputText ?? "");

                // Ctrl+A override: clear text
                if ((Main.inputText.IsKeyDown(Keys.LeftControl) || Main.inputText.IsKeyDown(Keys.RightControl))
                    && Main.inputText.IsKeyDown(Keys.A))
                {
                    newText = "";
                }

                _inputText = newText;
                _frameCount++;
            }

            // Draw the input field text
            DrawInputField(spriteBatch);

            // Draw template commands
            DrawTemplates(spriteBatch);
        }

        private void DrawTemplates(SpriteBatch spriteBatch)
        {
            CalculatedStyle contentDims = _contentPanel.GetInnerDimensions();

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float lineHeight = MeasureString(font, "A").Y;
            float scale = 0.85f;
            float lineH = lineHeight * scale;
            float spacerHeight = 8f;

            float y = contentDims.Y;
            float xBase = contentDims.X + 6f;

            Color cmdColor = new Color(255, 220, 100);
            Color cmdHoverColor = new Color(255, 255, 180);
            Color hintColor = new Color(120, 120, 150);
            string clickedTemplate = null;

            // Calculate aligned hint X position based on longest command
            string longestCmd = "Main.LocalPlayer.HeldItem.consumable = false;";
            float hintAlignX = xBase + MeasureString(font, longestCmd).X * scale + 10f;

            foreach (var t in Templates)
            {
                if (t.IsSpacer)
                {
                    y += spacerHeight;
                    continue;
                }

                float cmdW = MeasureString(font, t.Command).X * scale;
                float entryH = lineH + 2f;

                // Click hitbox covers only the command text
                Rectangle cmdRect = new((int)xBase - 2, (int)y, (int)(cmdW + 12), (int)entryH);

                bool hovered = cmdRect.Contains(Main.mouseX, Main.mouseY);

                if (hovered)
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, cmdRect, new Color(70, 70, 30, 100));

                // Draw command (clickable)
                Color drawColor = hovered ? cmdHoverColor : cmdColor;
                spriteBatch.DrawString(font, t.Command, new Vector2(xBase, y), drawColor,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // Draw hint text (not clickable, same font, grey) — aligned to same column
                if (!string.IsNullOrEmpty(t.Hint))
                {
                    spriteBatch.DrawString(font, t.Hint, new Vector2(hintAlignX, y), hintColor,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }

                if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
                    clickedTemplate = t.Command;

                y += entryH;
            }

            // Template click = paste into input field (not execute)
            if (clickedTemplate != null)
                PasteTemplate(clickedTemplate);
        }

        private void DrawInputField(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = _inputPanel.GetInnerDimensions();
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float scale = 0.9f;

            string displayText;
            Color textColor;

            if (string.IsNullOrEmpty(_inputText) && !_inputFocused)
            {
                displayText = "Type C# code here...";
                textColor = new Color(120, 120, 150, 150);
            }
            else
            {
                displayText = _inputText ?? "";
                textColor = Color.White;

                // Blinking cursor
                if (_inputFocused && (_frameCount / 20) % 2 == 0)
                    displayText += "|";
            }

            Vector2 pos = new(dims.X + 4, dims.Y + (dims.Height - MeasureString(font, "A").Y * scale) / 2f);
            spriteBatch.DrawString(font, displayText, pos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}