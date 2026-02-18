using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ModKitTML
{
    internal class ExtraAccessoryUI
    {
        private static Item[] singleSlotArray = new Item[1];

        public static void DrawExtraSlots(SpriteBatch spriteBatch)
        {
            if (!Main.playerInventory || Main.EquipPage != 0)
                return;

            var eap = Main.LocalPlayer.GetModPlayer<ExtraAccessoryPlayer>();
            if (eap.EnabledSlots <= 0) return;

            Point mousePoint = new Point(Main.mouseX, Main.mouseY);
            Main.inventoryScale = 0.85f;

            int slotW = (int)(TextureAssets.InventoryBack.Value.Width * Main.inventoryScale);
            int slotH = (int)(TextureAssets.InventoryBack.Value.Height * Main.inventoryScale);

            int mH = 0;
            if (Main.mapEnabled && !Main.mapFullscreen && Main.mapStyle == 1)
            {
                mH = 256;
                if (mH + 600 > Main.screenHeight) mH = Main.screenHeight - 600;
            }

            // Calculate the base coordinates of the right column of the inventory
            int baseX = Main.screenWidth - 92 - (47 * 3);
            if (Main.netMode == 1) baseX -= 47;

            // Placement: slightly ABOVE the first standard accessory,
            // so as not to overlap the right icons. Let's raise it to safetyOffset.
            int safetyOffset = 24;
            int baseY = mH + 174 - safetyOffset;

            for (int i = 0; i < eap.EnabledSlots; i++)
            {
                Item accItem = eap.ExtraAccessories[i];
                Rectangle r = new Rectangle(baseX, baseY + i * 47, slotW, slotH);

                if (r.Contains(mousePoint))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.armorHide = true;
                    singleSlotArray[0] = accItem;
                    ItemSlot.Handle(singleSlotArray, ItemSlot.Context.EquipAccessory, 0);
                    accItem = singleSlotArray[0];
                }

                singleSlotArray[0] = accItem;
                ItemSlot.Draw(spriteBatch, singleSlotArray, ItemSlot.Context.EquipAccessory, 0, new Vector2(r.X, r.Y));
                eap.ExtraAccessories[i] = singleSlotArray[0];
            }
        }
    }
}
