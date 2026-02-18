# ModKit TML

A tModLoader port of [KindRedSand/Terraria.ModKit](https://github.com/KindRedSand/Terraria.ModKit) — a cheat/utility mod for Terraria.

The original project is a standalone executable that patches the vanilla game. This fork is a **native tModLoader mod** rewritten from scratch to work with tModLoader's API (1.4.4+).

## Features

### Cheat Panel (bottom hotbar)
A panel with 10 toggle buttons (visible when entering a world). All keybinds are **unbound by default** — you can assign them in **Settings → Controls → Keybindings → Mod Controls**.

| Button | Description |
|--------|-------------|
| Journey Mode | Unlock Journey Mode powers in any world |
| Speed Hack | 2× movement speed |
| Bestiary Unlock | Unlock all bestiary entries |
| Noclip | Fly through blocks freely |
| Unlock Items | Unlock all items for Journey Mode duplication |
| Full Bright | Remove darkness, see everything |
| Bestiary Lock | Reset bestiary to default |
| God Mode | Invincible (damage numbers still show, but HP is instantly restored) |
| Map Reveal | Reveal the entire world map |
| Freeze Enemies | Freeze all NPCs in place |

### Additional Features
All keybinds below are also **unbound by default** and can be configured in **Settings → Controls → Keybindings → Mod Controls**.

| Feature | Description |
|---------|-------------|
| Full Heal | HP + Mana + remove debuffs |
| REPL Console | Toggle C# REPL console |
| Extra Accessory Slots | Cycle extra accessory slots (up to 6 extra) |
| Instant Revive | Revive instantly |
| Noclip Speed +/- | Increase / Decrease noclip speed |

### REPL Console
A full C# REPL console with:
- Direct access to `Terraria.Main`, `Player`, `NPC`, `Item` classes via reflection
- Template commands: give items, spawn NPCs, set time, teleport, change difficulty, and more
- Clickable template commands with inline hints
- Draggable window with scrollable output

### Extra Accessory Slots
Cycle between 0–6 extra accessory slots rendered alongside the vanilla inventory.

## Installation

### Option A: Pre-built mod (recommended)
1. Download `ModKitTML.tmod` from the [`Releases/`](Releases/) folder
2. Place it in: `Documents\My Games\Terraria\tModLoader\Mods\`
3. Launch tModLoader and enable the mod in **Workshop → Manage Mods**

### Option B: Build from source
1. Copy the [`ModKitTML/`](ModKitTML/) folder to: `Documents\My Games\Terraria\tModLoader\ModSources\ModKitTML\`
2. Launch tModLoader → **Workshop → Develop Mods → ModKit TML → Build**

## Requirements
- **Terraria** 1.4.4.9+
- **tModLoader** 2025.x+ (.NET 8)

## Note: `side = client`
The `build.txt` file includes `side = client`. This tells tModLoader that the mod runs **only on the client side** and is not required on the server.

- **With `side = client`:** The mod works in singleplayer and on multiplayer servers without the server needing the mod installed. However, some cheats (god mode, speed hack, etc.) may be partially limited by server-side validation.
- **Without `side = client`:** The mod would need to be installed on both client and server to join. Since this is a cheat mod meant for personal/singleplayer use, `side = client` is the correct choice.

If you build from source and want to remove it, simply delete the `side = client` line from `ModKitTML/build.txt`.

## Credits
- Original project: [KindRedSand/Terraria.ModKit](https://github.com/KindRedSand/Terraria.ModKit)
- REPL concept from [ModdersToolkit](https://github.com/JavidPack/ModdersToolkit)
- Map reveal from [HERO's Mod](https://github.com/JavidPack/HEROsMod)
- Extra accessory slots from [CheatSheet](https://github.com/JavidPack/CheatSheet)
