using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace ModKitTML
{
    public class ModKitPlayer : ModPlayer
    {
        public bool godMode;
        public bool noclip;
        public bool fullBright;
        public bool speedHack;
        public bool freezeEnemies;
        public bool instantRevive;

        // Noclip fly speed
        public float flyDelta = 5f;
        public int flySpeedIndex = 3;

        // Journey mode toggle state
        private int oldGameMode;
        private byte oldPlayerDifficulty;

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (ModKitUISystem.IsREPLFocused)
                return;

            if (ModKitKeybinds.TogglePanelKey?.JustPressed == true)
                ModKitUISystem.ToggleCheatPanel();

            if (ModKitKeybinds.FlyModeKey?.JustPressed == true)
            {
                noclip = !noclip;
                Main.NewText($"Noclip: {(noclip ? "ON" : "OFF")}");
            }

            if (ModKitKeybinds.UnlockAllItemsKey?.JustPressed == true)
                UnlockAllItems();

            if (ModKitKeybinds.UnlockBestiaryKey?.JustPressed == true)
                UnlockBestiary();

            if (ModKitKeybinds.LockBestiaryKey?.JustPressed == true)
                LockBestiary();

            if (ModKitKeybinds.IncreaseFlySpeedKey?.JustPressed == true)
            {
                if (flySpeedIndex < 10) { flySpeedIndex++; UpdateFlySpeed(); }
            }

            if (ModKitKeybinds.DecreaseFlySpeedKey?.JustPressed == true)
            {
                if (flySpeedIndex > 1) { flySpeedIndex--; UpdateFlySpeed(); }
            }

            if (ModKitKeybinds.InstantReviveKey?.JustPressed == true)
            {
                instantRevive = !instantRevive;
                Main.NewText($"Instant revive: {(instantRevive ? "ON" : "OFF")}");
            }

            if (ModKitKeybinds.ToggleREPLKey?.JustPressed == true)
                ModKitUISystem.ToggleREPL();

            if (ModKitKeybinds.JourneyModeKey?.JustPressed == true)
                CycleJourneyMode();

            if (ModKitKeybinds.SpeedHackKey?.JustPressed == true)
            {
                speedHack = !speedHack;
                Main.NewText($"Speed Hack: {(speedHack ? "ON" : "OFF")}");
            }

            if (ModKitKeybinds.FullBrightKey?.JustPressed == true)
            {
                fullBright = !fullBright;
                Main.NewText($"Full Bright: {(fullBright ? "ON" : "OFF")}");
            }

            if (ModKitKeybinds.GodModeKey?.JustPressed == true)
            {
                godMode = !godMode;
                Main.NewText($"God Mode: {(godMode ? "ON" : "OFF")}");
            }

            if (ModKitKeybinds.FullHealKey?.JustPressed == true)
            {
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;
                Player.HealEffect(Player.statLifeMax2);
                Main.NewText("Fully healed!");
            }

            if (ModKitKeybinds.ExtraAccKey?.JustPressed == true)
                Player.GetModPlayer<ExtraAccessoryPlayer>().CycleSlots();

            if (ModKitKeybinds.FreezeEnemiesKey?.JustPressed == true)
            {
                freezeEnemies = !freezeEnemies;
                Main.NewText($"Freeze Enemies: {(freezeEnemies ? "ON" : "OFF")}");
            }
        }

        public override void ResetEffects()
        {
            // Speed hack
            if (speedHack)
            {
                Player.moveSpeed += 3f;
                Player.maxRunSpeed += 10f;
                Player.runAcceleration += 1f;
            }

            // Block player controls when REPL is focused
            if (ModKitUISystem.IsREPLFocused)
            {
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlJump = false;
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.controlThrow = false;
                Player.controlHook = false;
                Player.controlMount = false;
            }
        }

        public override void PreUpdateMovement()
        {
            // Noclip fly mode — ported from Terraria.ModKit Entry.cs
            if (noclip)
            {
                Player.velocity = Vector2.Zero;
                Player.fallStart = (int)(Player.position.Y / 16f);
                Player.gravity = 0f;

                if (Player.controlUp) Player.position.Y -= flyDelta;
                if (Player.controlDown) Player.position.Y += flyDelta;
                if (Player.controlRight) Player.position.X += flyDelta;
                if (Player.controlLeft) Player.position.X -= flyDelta;
            }

            // Instant revive
            if (instantRevive && Player.respawnTimer > 0)
                Player.respawnTimer = 0;
        }

        // God Mode: allow hit animation, sound, and damage numbers, but heal back immediately
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Let damage happen normally for visuals
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (godMode)
            {
                // Immediately restore all HP after the damage visual/sound played
                Player.statLife = Player.statLifeMax2;
            }
        }

        // God Mode: prevent actual death
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (godMode)
            {
                Player.statLife = Player.statLifeMax2;
                return false; // block death
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
        }

        public override void PostUpdate()
        {
            // God mode: keep HP/mana full
            if (godMode)
            {
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;
                Player.breath = Player.breathMax;
            }

            // Full bright
            if (fullBright)
            {
                Lighting.AddLight(Player.Center, 1f, 1f, 1f);
                for (int x = -30; x <= 30; x++)
                {
                    for (int y = -30; y <= 30; y++)
                    {
                        int tileX = (int)(Player.Center.X / 16f) + x;
                        int tileY = (int)(Player.Center.Y / 16f) + y;
                        if (tileX >= 0 && tileX < Main.maxTilesX && tileY >= 0 && tileY < Main.maxTilesY)
                        {
                            Lighting.AddLight(tileX, tileY, 1f, 1f, 1f);
                        }
                    }
                }
            }

            // Freeze enemies
            if (freezeEnemies)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly)
                    {
                        npc.velocity = Vector2.Zero;
                        npc.position = npc.oldPosition;
                    }
                }
            }
        }

        public void CycleJourneyMode()
        {
            if (Main.LocalPlayer.difficulty == 3)
            {
                Main.GameMode = oldGameMode;
                Main.LocalPlayer.difficulty = oldPlayerDifficulty;
                Main.NewText("Journey mode: OFF");
            }
            else
            {
                oldGameMode = Main.GameMode;
                oldPlayerDifficulty = Main.LocalPlayer.difficulty;
                Main.GameMode = 3;
                Main.LocalPlayer.difficulty = 3;
                Main.NewText("Journey mode: ON");
            }
        }

        public void UpdateFlySpeed()
        {
            flyDelta = flySpeedIndex switch
            {
                1 => 1f, 2 => 2.5f, 3 => 5f, 4 => 10f, 5 => 15f,
                6 => 20f, 7 => 30f, 8 => 40f, 9 => 50f, 10 => 100f,
                _ => 5f
            };
            Main.NewText($"Fly speed: {flyDelta}");
        }

        public void UnlockAllItems()
        {
            int count = 0;
            for (int i = 0; i < ItemID.Count; i++)
            {
                try
                {
                    Main.LocalPlayer.creativeTracker.ItemSacrifices.RegisterItemSacrifice(i, 999);
                    count++;
                }
                catch { }
            }
            Main.NewText($"Unlocked {count} items for Journey mode!");
        }

        public void UnlockBestiary()
        {
            try
            {
                foreach (var entry in ContentSamples.NpcBestiaryCreditIdsByNpcNetIds)
                {
                    Main.BestiaryTracker.Kills.SetKillCountDirectly(entry.Value, 9999);
                    Main.BestiaryTracker.Chats.SetWasChatWithDirectly(entry.Value);
                    Main.BestiaryTracker.Sights.SetWasSeenDirectly(entry.Value);
                }
                Main.NewText("Bestiary fully unlocked!");
            }
            catch (Exception e) { Main.NewText($"Error: {e.Message}"); }
        }

        public void LockBestiary()
        {
            try
            {
                Main.BestiaryTracker.Kills.Reset();
                Main.BestiaryTracker.Chats.Reset();
                Main.BestiaryTracker.Sights.Reset();
                Main.NewText("Bestiary locked!");
            }
            catch (Exception e) { Main.NewText($"Error: {e.Message}"); }
        }

        public static void RevealWholeMap()
        {
            for (int i = 0; i < Main.maxTilesX; i++)
                for (int j = 0; j < Main.maxTilesY; j++)
                    if (WorldGen.InWorld(i, j))
                        try { Main.Map.Update(i, j, 255); } catch { break; }
            Main.refreshMap = true;
            Main.NewText("Map revealed!");
        }

        public static void RevealAroundPoint(int x, int y)
        {
            const int size = 300;
            for (int i = x - size / 2; i < x + size / 2; i++)
                for (int j = y - size / 2; j < y + size / 2; j++)
                    if (WorldGen.InWorld(i, j))
                        Main.Map.Update(i, j, 255);
            Main.refreshMap = true;
        }
    }
}