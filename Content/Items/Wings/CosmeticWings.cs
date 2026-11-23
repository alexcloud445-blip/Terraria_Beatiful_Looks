using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Effects;
using Terraria_Beatiful_Looks.Common.Looks;

namespace Terraria_Beatiful_Looks.Content.Items.Wings
{
	/// <summary>
	/// Основа для всех крыльев мода. Новые крылья: наследник + [AutoloadEquip(EquipType.Wings)]
	/// + 2 PNG: иконка и лист ИмяКласса_Wings.png (4 кадра 86x56 сверху вниз).
	/// </summary>
	public abstract class CosmeticWings : ModItem
	{
		/// <summary>Крылья мода по номеру слота крыльев: по нему игрок находит, какие крылья-эффекты рисовать.</summary>
		public static readonly Dictionary<int, CosmeticWings> BySlot = new Dictionary<int, CosmeticWings>();

		/// <summary>Цвет света от крыльев.</summary>
		protected abstract Color GlowColor { get; }

		/// <summary>Как выглядят эти крылья (рисуются эффектами вместо спрайта).</summary>
		public abstract WingStyle VfxStyle { get; }

		/// <summary>Основной и второй цвет крыльев-эффектов.</summary>
		public virtual Color WingPrimary => GlowColor;
		public abstract Color WingSecondary { get; }

		/// <summary>Частица, которая сыплется с крыльев в полёте.</summary>
		protected abstract void SpawnParticle(Vector2 position, Vector2 velocity);

		protected abstract void AddIngredients(Recipe recipe);

		protected virtual int CraftingStation => TileID.Anvils;

		public override void SetStaticDefaults()
		{
			// время полёта, макс. скорость, ускорение
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(100, 6f, 1.5f);
			BySlot[Item.wingSlot] = this;
		}

		public override void Unload() => BySlot.Clear();

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

			// Сами крылья и частицы рисует WingEffects (Common/Effects); здесь только свет
			Lighting.AddLight(player.Center, GlowColor.ToVector3() * 0.45f);
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
		public override WingStyle VfxStyle => WingStyle.Constellation;
		public override Color WingSecondary => new Color(200, 150, 255);
		protected override Color GlowColor => new Color(120, 160, 255);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Spark(pos, vel, new Color(150, 200, 255), 0.7f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.FallenStar, 5);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class EmberWings : CosmeticWings
	{
		public override WingStyle VfxStyle => WingStyle.Flame;
		public override Color WingSecondary => new Color(255, 70, 30);
		protected override Color GlowColor => new Color(255, 130, 40);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) =>
			Fx.Spark(pos, vel + new Vector2(0f, -1.5f), Color.Lerp(new Color(255, 230, 120), new Color(255, 80, 30), Main.rand.NextFloat()), 1f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.Torch, 25).AddIngredient(ItemID.Gel, 15);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class FrostWings : CosmeticWings
	{
		public override WingStyle VfxStyle => WingStyle.Crystal;
		public override Color WingSecondary => new Color(230, 250, 255);
		protected override Color GlowColor => new Color(140, 220, 255);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Snow(pos, vel, new Color(200, 240, 255), 0.8f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.IceBlock, 30).AddIngredient(ItemID.FallenStar, 3);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class SakuraWings : CosmeticWings
	{
		public override WingStyle VfxStyle => WingStyle.Butterfly;
		public override Color WingSecondary => new Color(255, 230, 245);
		protected override Color GlowColor => new Color(255, 150, 200);
		protected override int CraftingStation => TileID.Loom;
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Petal(pos, vel, new Color(255, 170, 210), 0.9f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.PinkGel, 5).AddIngredient(ItemID.FallenStar, 3);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class VoidWings : CosmeticWings
	{
		public override WingStyle VfxStyle => WingStyle.Demon;
		public override Color WingSecondary => new Color(255, 80, 200);
		protected override Color GlowColor => new Color(130, 60, 200);
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Star(pos, vel, new Color(170, 100, 255), 0.9f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Feather, 10).AddIngredient(ItemID.Obsidian, 15).AddIngredient(ItemID.FallenStar, 5);
	}

	[AutoloadEquip(EquipType.Wings)]
	public class FairyWings : CosmeticWings
	{
		public override WingStyle VfxStyle => WingStyle.Spirit;
		public override Color WingSecondary => new Color(255, 240, 140);
		protected override Color GlowColor => new Color(120, 255, 180);
		protected override int CraftingStation => TileID.Loom;
		protected override void SpawnParticle(Vector2 pos, Vector2 vel) => Fx.Star(pos, vel, new Color(170, 255, 200), 0.8f);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.Daybloom, 5).AddIngredient(ItemID.FallenStar, 3);
	}
}
