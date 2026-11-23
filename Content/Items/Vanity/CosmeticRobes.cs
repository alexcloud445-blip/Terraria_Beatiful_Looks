using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Effects;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Content.Items.Vanity
{
	/// <summary>
	/// Робы (декоративное тело + юбка). Новая роба: наследник + [AutoloadEquip(EquipType.Body)]
	/// + иконка ИмяКласса.png, ИмяКласса_Body.png (360x224), ИмяКласса_Legs.png (40x1120, юбка)
	/// и ИмяКласса_Glow.png (120x1120: три копии листа юбки с «бусинами» подола, они мерцают по очереди).
	/// Все PNG делает tools/make_robes.py из шаблона робы ExampleMod.
	/// </summary>
	public abstract class CosmeticRobe : ModItem
	{
		/// <summary>Робы по номеру слота тела: по нему слой свечения узнаёт, что надето.</summary>
		public static readonly Dictionary<int, CosmeticRobe> ByBody = new Dictionary<int, CosmeticRobe>();

		/// <summary>Два цвета свечения подола (переливаются между собой).</summary>
		public abstract Color GlowA { get; }
		public abstract Color GlowB { get; }

		public Asset<Texture2D> GlowTexture { get; private set; }

		protected abstract void AddIngredients(Recipe recipe);

		/// <summary>Частица, которая слетает с подола при ходьбе.</summary>
		public abstract void HemParticle(Vector2 position, Vector2 velocity, float gravDir);

		public override void Load()
		{
			if (Main.dedServ)
				return;
			// юбка — отдельная текстура ног, её подставляет SetMatch
			EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Legs}", EquipType.Legs, this);
		}

		public override void SetStaticDefaults()
		{
			ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false;
			ByBody[Item.bodySlot] = this;
			if (!Main.dedServ)
				GlowTexture = ModContent.Request<Texture2D>(Texture + "_Glow");
		}

		public override void Unload() => ByBody.Clear();

		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 14;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.LightRed;
			Item.vanity = true;
		}

		public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
		{
			robes = true;
			equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
		}

		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			AddIngredients(recipe);
			recipe.AddTile(TileID.Loom);
			recipe.Register();
		}

		/// <summary>Роба, которую сейчас видно на игроке (слот одежды важнее слота брони).</summary>
		public static CosmeticRobe Worn(Player player)
		{
			Item vanity = player.armor[11];
			Item visible = vanity != null && !vanity.IsAir ? vanity : player.armor[1];
			return visible?.ModItem as CosmeticRobe;
		}

		/// <summary>Частицы и свет от подола (из LooksPlayer.PostUpdate).</summary>
		public static void UpdateWorn(Player player)
		{
			CosmeticRobe robe = Worn(player);
			if (robe == null)
				return;
			Vector2 hem = player.gravDir > 0f ? player.Bottom + new Vector2(0f, -5f) : player.Top + new Vector2(0f, 5f);
			Lighting.AddLight(hem, robe.GlowA.ToVector3() * 0.25f);
			float moving = Math.Min(1f, Math.Abs(player.velocity.X) / 4f);
			if (Main.rand.NextFloat() < (0.04f + 0.2f * moving) * Fx.GlobalIntensity)
			{
				Vector2 pos = hem + new Vector2(Main.rand.NextFloat(-11f, 11f), 0f);
				robe.HemParticle(pos, new Vector2(-player.velocity.X * 0.1f + Main.rand.NextFloat(-0.3f, 0.3f), 0f), player.gravDir);
			}
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class SeraphRobe : CosmeticRobe
	{
		public override Color GlowA => new Color(255, 215, 120);
		public override Color GlowB => new Color(255, 160, 90);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Feather, 8).AddIngredient(ItemID.FallenStar, 3);

		public override void HemParticle(Vector2 pos, Vector2 vel, float g) =>
			ParticleSystem.Spawn(new GlowParticle(pos, vel + new Vector2(0f, -Main.rand.NextFloat(0.4f, 1f) * g), new Ramp(Color.White, GlowA, GlowB), Main.rand.NextFloat(0.16f, 0.26f), 45)
			{ Drag = 0.98f, Front = Main.rand.NextBool() });
	}

	[AutoloadEquip(EquipType.Body)]
	public class InfernoRobe : CosmeticRobe
	{
		public override Color GlowA => new Color(255, 150, 40);
		public override Color GlowB => new Color(255, 60, 30);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Fireblossom, 3).AddIngredient(ItemID.Torch, 20);

		public override void HemParticle(Vector2 pos, Vector2 vel, float g) =>
			ParticleSystem.Spawn(new GlowParticle(pos, vel + new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.8f, 1.6f) * g),
				new Ramp(new Color(255, 240, 180), GlowA, GlowB, new Color(90, 20, 10)), Main.rand.NextFloat(0.18f, 0.28f), 40)
			{ Wander = 0.05f, Front = Main.rand.NextBool() });
	}

	[AutoloadEquip(EquipType.Body)]
	public class FrostRobe : CosmeticRobe
	{
		public override Color GlowA => new Color(150, 225, 255);
		public override Color GlowB => new Color(170, 170, 255);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Shiverthorn, 3).AddIngredient(ItemID.IceBlock, 25);

		public override void HemParticle(Vector2 pos, Vector2 vel, float g) =>
			ParticleSystem.Spawn(new ShapeParticle(Shape.Snowflake, SpriteMotion.Drift, pos, vel + new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.8f) * g),
				new Ramp(Color.White, GlowA, GlowB), Main.rand.NextFloat(0.35f, 0.5f), 60)
			{ Glowing = true, GlowRamp = GlowB, Settle = true, Front = Main.rand.NextBool() });
	}

	[AutoloadEquip(EquipType.Body)]
	public class SakuraKimono : CosmeticRobe
	{
		public override Color GlowA => new Color(255, 160, 215);
		public override Color GlowB => new Color(200, 150, 255);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.PinkGel, 5).AddIngredient(ItemID.Daybloom, 3);

		public override void HemParticle(Vector2 pos, Vector2 vel, float g) =>
			ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Petal, SpriteMotion.Fall, pos, vel + new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.2f) * g),
				new Ramp(new Color(255, 225, 240), GlowA, GlowB), Main.rand.NextFloat(0.45f, 0.6f), 90)
			{ GlowStrength = 0.35f, GlowRamp = GlowB, Settle = true, Front = Main.rand.NextBool() });
	}

	[AutoloadEquip(EquipType.Body)]
	public class VoidRobe : CosmeticRobe
	{
		public override Color GlowA => new Color(190, 90, 255);
		public override Color GlowB => new Color(255, 90, 210);
		protected override void AddIngredients(Recipe r) => r.AddIngredient(ItemID.Silk, 15).AddIngredient(ItemID.Obsidian, 10).AddIngredient(ItemID.FallenStar, 3);

		public override void HemParticle(Vector2 pos, Vector2 vel, float g)
		{
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new SmokeParticle(pos, vel + new Vector2(0f, -0.3f * g), new Ramp(new Color(40, 16, 70), new Color(5, 2, 10)), Main.rand.NextFloat(0.4f, 0.55f), 40)
				{ MaxOpacity = 0.45f, Front = Main.rand.NextBool() });
			else
				ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Float, pos, vel + new Vector2(0f, -Main.rand.NextFloat(0.4f, 0.8f) * g), new Ramp(Color.White, GlowA, GlowB),
					Main.rand.NextFloat(0.28f, 0.38f), 50) { Glowing = true, GlowRamp = GlowB, Front = Main.rand.NextBool() });
		}
	}

	/// <summary>
	/// Свечение подола робы. Повторяет каждый кусок юбки, который нарисовал ванильный слой «Robe»,
	/// той же позицией и кадром, но из текстуры свечения: так узор совпадает с тканью в любой позе
	/// (ходьба, полёт, сидя). Три группы «бусин» мерцают по очереди — по подолу бегут огоньки.
	/// </summary>
	public class RobeGlowLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Robe);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.wearsRobe && CosmeticRobe.ByBody.ContainsKey(p.body);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (!CosmeticRobe.ByBody.TryGetValue(p.body, out CosmeticRobe robe) || robe.GlowTexture == null || p.legs <= 0 || p.legs >= TextureAssets.ArmorLeg.Length)
				return;
			Texture2D legs = TextureAssets.ArmorLeg[p.legs].Value;
			Texture2D glow = robe.GlowTexture.Value;
			float time = Main.GlobalTimeWrappedHourly;
			var cache = drawInfo.DrawDataCache;
			int count = cache.Count;
			for (int i = 0; i < count; i++)
			{
				DrawData data = cache[i];
				if (data.texture != legs)
					continue;
				Rectangle src = data.sourceRect ?? new Rectangle(0, 0, legs.Width, legs.Height);
				for (int g = 0; g < 3; g++)
				{
					float wave = 0.5f + 0.5f * (float)Math.Sin(time * 4f - g * MathHelper.TwoPi / 3f);
					Color c = VfxMath.LerpVivid(robe.GlowA, robe.GlowB, 0.5f + 0.5f * (float)Math.Sin(time * 1.3f + g * 2f));
					DrawData copy = data;
					copy.texture = glow;
					copy.sourceRect = new Rectangle(src.X + 40 * g, src.Y, src.Width, src.Height);
					copy.color = VfxMath.Additive(c, 0.45f + 0.55f * wave);
					cache.Add(copy);
				}
			}
		}
	}
}
