using Terraria.ModLoader;

namespace ModKitTML
{
    public class ModKitTML : Mod
    {
        public static ModKitTML Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            ModKitKeybinds.Load(this);
        }

        public override void Unload()
        {
            Instance = null;
            ModKitKeybinds.Unload();
        }
    }
}