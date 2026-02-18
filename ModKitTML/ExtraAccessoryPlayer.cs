using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ModKitTML
{
    /// <summary>
    /// Extra accessory slots — ported from JavidPack/CheatSheet.
    /// Adds up to 6 extra functional accessory slots.
    /// Items provide full buffs (armor, prefixes, equip effects).
    /// Items save/load with the player character.
    /// </summary>
    public class ExtraAccessoryPlayer : ModPlayer
    {
        public const int MaxExtraSlots = 6;
        public Item[] ExtraAccessories = new Item[MaxExtraSlots];
        public int EnabledSlots = 0; // How many are active (0-6)

        public override void Initialize()
        {
            ExtraAccessories = new Item[MaxExtraSlots];
            for (int i = 0; i < MaxExtraSlots; i++)
            {
                ExtraAccessories[i] = new Item();
                ExtraAccessories[i].SetDefaults(0, true);
            }
        }

        public override void UpdateEquips()
        {
            // Apply prefix/armor benefits
            for (int i = 0; i < EnabledSlots; i++)
            {
                Item item = ExtraAccessories[i];
                if (item.IsAir) continue;
                if (item.expertOnly && !Main.expertMode) continue;

                if (item.accessory)
                    Player.GrantPrefixBenefits(item);

                Player.GrantArmorBenefits(item);
            }

            // Apply equip functional effects (the actual accessory powers)
            for (int i = 0; i < EnabledSlots; i++)
            {
                Item item = ExtraAccessories[i];
                if (item.IsAir) continue;
                if (item.expertOnly && !Main.expertMode) continue;

                Player.ApplyEquipFunctional(item, false);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["ExtraAccessories"] = ExtraAccessories.Select(ItemIO.Save).ToList();
            tag["EnabledSlots"] = EnabledSlots;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("ExtraAccessories"))
            {
                var list = tag.GetList<TagCompound>("ExtraAccessories");
                var items = list.Select(ItemIO.Load).ToArray();
                for (int i = 0; i < MaxExtraSlots && i < items.Length; i++)
                    ExtraAccessories[i] = items[i];
            }
            if (tag.ContainsKey("EnabledSlots"))
                EnabledSlots = tag.GetInt("EnabledSlots");
        }

        public void CycleSlots()
        {
            EnabledSlots = (EnabledSlots + 1) % (MaxExtraSlots + 1);
            Main.NewText($"Extra accessory slots: {EnabledSlots}");
        }

        public void DecrementSlots()
        {
            EnabledSlots = (EnabledSlots - 1 + MaxExtraSlots + 1) % (MaxExtraSlots + 1);
            Main.NewText($"Extra accessory slots: {EnabledSlots}");
        }
    }
}