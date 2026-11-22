using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terraria_Beatiful_Looks.Content.Items.Wings
{
	/// <summary>
	/// Звёздные крылья. Шаблон для новых крыльев: скопировать класс + 2 PNG
	/// (иконка и лист _Wings.png из 4 кадров, сложенных вертикально).
	/// </summary>
	[AutoloadEquip(EquipType.Wings)]
	public class StarlightWings : ModItem
	{
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

		private static void Effects(Player player)
		{
			if (Main.dedServ)
				return;

			Lighting.AddLight(player.Center, 0.2f, 0.3f, 0.55f);

			// Звёздная пыль сыплется с крыльев в полёте
			if (player.velocity.Y != 0f && Main.rand.NextBool(2))
			{
				Vector2 pos = player.Center + new Vector2(-player.direction * 16f, -4f) + Main.rand.NextVector2Circular(14f, 10f);
				Dust d = Dust.NewDustPerfect(pos, DustID.RainbowMk2, new Vector2(-player.direction * 0.4f, 0.7f), 0,
					new Color(150, 200, 255), 0.7f);
				d.noGravity = true;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Feather, 10)
				.AddIngredient(ItemID.FallenStar, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
