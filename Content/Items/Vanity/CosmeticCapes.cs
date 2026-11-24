using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BeautifulLooks.Content.Items.Vanity
{
	/// <summary>
	/// Основа для плащей (аксессуар на спину). Новый плащ: наследник + [AutoloadEquip(EquipType.Back)]
	/// + иконка + ИмяКласса_Back.png (20 кадров 40x56 сверху вниз).
	/// </summary>
	public abstract class CosmeticCape : ModItem
	{
		protected abstract void AddIngredients(Recipe recipe);

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 28;
			Item.value = Item.sellPrice(silver: 75);
			Item.rare = ItemRarityID.Green;
			Item.accessory = true;
			Item.vanity = true;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			AddIngredients(recipe);
			recipe.AddTile(TileID.Loom);
			recipe.Register();
		}
	}

	[AutoloadEquip(EquipType.Back)]
	public class StarlightCape : CosmeticCape
	{
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.FallenStar, 3);
	}

	[AutoloadEquip(EquipType.Back)]
	public class RoyalCape : CosmeticCape
	{
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Ruby, 2);
	}

	[AutoloadEquip(EquipType.Back)]
	public class ShadowCape : CosmeticCape
	{
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Obsidian, 5);
	}
}
