using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terraria_Beatiful_Looks.Content.Items.Vanity
{
	/// <summary>
	/// Звёздная корона (украшение на голову). Шаблон для костюмов:
	/// голова — [AutoloadEquip(EquipType.Head)] + _Head.png (40x1120, 20 кадров);
	/// тело — EquipType.Body + _Body.png; ноги — EquipType.Legs + _Legs.png.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class StarlightCrown : ModItem
	{
		public override void SetStaticDefaults()
		{
			// Корона не прячет волосы
			ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 18;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.vanity = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.GoldBar, 4)
				.AddIngredient(ItemID.FallenStar, 2)
				.AddTile(TileID.Anvils)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.PlatinumBar, 4)
				.AddIngredient(ItemID.FallenStar, 2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
