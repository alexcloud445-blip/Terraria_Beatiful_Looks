using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terraria_Beatiful_Looks.Content.Items.Vanity
{
	/// <summary>
	/// Основа для головных уборов. Новый убор: наследник + [AutoloadEquip(EquipType.Head)]
	/// + иконка ИмяКласса.png + ИмяКласса_Head.png (20 кадров 40x56 сверху вниз).
	/// </summary>
	public abstract class CosmeticHat : ModItem
	{
		/// <summary>true — волосы видны целиком (ушки, рога); false — как под шляпой.</summary>
		protected virtual bool FullHair => true;

		protected abstract void AddIngredients(Recipe recipe);

		protected virtual int CraftingStation => TileID.Anvils;

		public override void SetStaticDefaults()
		{
			if (FullHair)
				ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
			else
				ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 18;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.vanity = true;
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			AddIngredients(recipe);
			recipe.AddTile(CraftingStation);
			recipe.Register();
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class StarlightCrown : CosmeticHat
	{
		protected override bool FullHair => false;
		protected override void AddIngredients(Recipe r) => r.AddRecipeGroup(RecipeGroupID.IronBar, 4).AddIngredient(ItemID.FallenStar, 2);

		public override void AddRecipes()
		{
			// Золото или платина
			CreateRecipe().AddIngredient(ItemID.GoldBar, 4).AddIngredient(ItemID.FallenStar, 2).AddTile(TileID.Anvils).Register();
			CreateRecipe().AddIngredient(ItemID.PlatinumBar, 4).AddIngredient(ItemID.FallenStar, 2).AddTile(TileID.Anvils).Register();
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class CatEars : CosmeticHat
	{
		protected override int CraftingStation => TileID.Loom;
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 5).AddIngredient(ItemID.PinkGel, 1);
	}

	[AutoloadEquip(EquipType.Head)]
	public class DemonHorns : CosmeticHat
	{
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Obsidian, 8).AddIngredient(ItemID.Ruby, 1);
	}

	[AutoloadEquip(EquipType.Head)]
	public class FlowerWreath : CosmeticHat
	{
		protected override int CraftingStation => TileID.WorkBenches;
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Daybloom, 3).AddIngredient(ItemID.Sunflower, 1);
	}

	[AutoloadEquip(EquipType.Head)]
	public class FrostTiara : CosmeticHat
	{
		protected override bool FullHair => false;
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.IceBlock, 20).AddIngredient(ItemID.FallenStar, 3);
	}
}
