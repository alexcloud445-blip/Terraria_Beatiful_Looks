using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Effects;

namespace Terraria_Beatiful_Looks.Content.Items.Wings
{
	/// <summary>
	/// Основа для всех крыльев мода. Новые крылья: наследник + [AutoloadEquip(EquipType.Wings)]
	/// + 2 PNG: иконка и лист ИмяКласса_Wings.png (4 кадра 86x56 сверху вниз).
	/// </summary>
	public abstract class CosmeticWings : ModItem
	{
		/// <summary>Цвет света от крыльев.</summary>
		protected abstract Color GlowColor { get; }

		/// <summary>Частица, которая сыплется с крыльев в полёте.</summary>
		protected abstract void SpawnParticle(Vector2 position, Vector2 velocity);

		protected abstract void AddIngredients(Recipe recipe);

		protected virtual int CraftingStation => TileID.Anvils;

		public override void SetStaticDefaults()
		{
			// время полёта, макс. скорость, ускорение
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(100, 6f, 1.5f);
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 28;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
			ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
		{
			ascentWhenFalling = 0.85f;
			ascentWhenRising = 0.15f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 2.5f;
			constantAscend = 0.135f;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			if (!hideVisual)
				Effects(player);
		}

		public override void UpdateVanity(Player player)
		{
			Effects(player);
		}

		private void Effects(Player player)
		{
			if (Main.dedServ)
				return;

			Lighting.AddLight(player.Center, GlowColor.ToVector3() * 0.45f);

			if (player.velocity.Y != 0f && Main.rand.NextFloat() < 0.5f * Fx.GlobalIntensity)
			{
				Vector2 pos = player.Center + new Vector2(-player.direction * 16f, -4f) + Main.rand.NextVector2Circular(14f, 10f);
				SpawnParticle(pos, new Vector2(-player.direction * 0.4f, 0.7f));
			}
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			AddIngredients(recipe);
			recipe.AddTile(CraftingStation);
			recipe.Register();
		}
	}

	[AutoloadEquip(EquipType.Wings)]
	public class StarlightWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(120, 160, 255);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Spark(pos, vel, new Color(150, 200, 255), 0.7f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.FallenStar, 5);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class EmberWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(255, 130, 40);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) =>
			Fx.Spark(pos, vel + new Vector2(0f, -1.5f), Color.Lerp(new Color(255, 230, 120), new Color(255, 80, 30), Main.rand.NextFloat()), 1f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.Torch, 25).AddIngredient(ItemID.Gel, 15);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class FrostWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(140, 220, 255);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Snow(pos, vel, new Color(200, 240, 255), 0.8f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.IceBlock, 30).AddIngredient(ItemID.FallenStar, 3);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class SakuraWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(255, 150, 200);
		protected override int CraftingStation => TileID.Loom;
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Petal(pos, vel, new Color(255, 170, 210), 0.9f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.PinkGel, 5).AddIngredient(ItemID.FallenStar, 3);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class VoidWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(130, 60, 200);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Star(pos, vel, new Color(170, 100, 255), 0.9f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.Obsidian, 15).AddIngredient(ItemID.FallenStar, 5);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class FairyWings : CosmeticWings
	{
		protected override Color GlowColor => new Color(120, 255, 180);
		protected override int CraftingStation => TileID.Loom;
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Star(pos, vel, new Color(170, 255, 200), 0.8f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.Daybloom, 5).AddIngredient(ItemID.FallenStar, 3);
	}
}
