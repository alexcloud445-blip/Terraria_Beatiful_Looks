using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>Общие помощники для слоёв отрисовки.</summary>
	internal static class LayerUtils
	{
		public const string EffectsPath = "Terraria_Beatiful_Looks/Assets/Effects/";

		public static Texture2D Tex(string name) =>
			ModContent.Request<Texture2D>(EffectsPath + name, AssetRequestMode.ImmediateLoad).Value;

		public static LookSettings Look(PlayerDrawSet info) => info.drawPlayer.GetModPlayer<LooksPlayer>().Look;

		/// <summary>Центр персонажа в экранных координатах.</summary>
		public static Vector2 Center(PlayerDrawSet info) =>
			(info.Position - Main.screenPosition + info.drawPlayer.Size / 2f).Floor();

		/// <summary>Цвет «как свет» (аддитивно), с прозрачностью.</summary>
		public static Color Glow(Color c, float opacity)
		{
			Color r = c * opacity;
			r.A = 0;
			return r;
		}

		public static float Pulse(float speed = 2f) => 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * speed);
	}

	/// <summary>Мягкое свечение за спиной и сияющий круг под ногами (рисуются позади персонажа).</summary>
	public class BackEffectsLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.JimsCloak, PlayerDrawLayers.MountBack);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead || drawInfo.drawPlayer.invis)
				return false;
			LookSettings look = LayerUtils.Look(drawInfo);
			return look.BackGlow || look.GroundRing;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LookSettings look = LayerUtils.Look(drawInfo);
			Vector2 center = LayerUtils.Center(drawInfo);
			float grav = drawInfo.drawPlayer.gravDir;

			if (look.BackGlow)
			{
				Texture2D glow = LayerUtils.Tex("Glow");
				Color c = LayerUtils.Glow(look.EffectColor.Get(), 0.55f * LayerUtils.Pulse());
				drawInfo.DrawDataCache.Add(new DrawData(glow, center, null, c, 0f, glow.Size() / 2f,
					new Vector2(1.1f, 1.4f), SpriteEffects.None, 0));
			}

			if (look.GroundRing)
			{
				Texture2D ring = LayerUtils.Tex("Ring");
				Vector2 feet = center + new Vector2(0f, drawInfo.drawPlayer.height / 2f * grav);
				float pulse = LayerUtils.Pulse(3f);
				Color c = LayerUtils.Glow(look.EffectColor.Get(0.5f), 0.8f * pulse);
				drawInfo.DrawDataCache.Add(new DrawData(ring, feet, null, c, 0f, ring.Size() / 2f,
					0.7f + 0.08f * pulse, SpriteEffects.None, 0));
			}
		}
	}

	/// <summary>Нимб над головой.</summary>
	public class HaloLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			return drawInfo.shadow == 0f && !drawInfo.drawPlayer.dead && !drawInfo.drawPlayer.invis
				&& LayerUtils.Look(drawInfo).Halo;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LookSettings look = LayerUtils.Look(drawInfo);
			Texture2D halo = LayerUtils.Tex("Halo");
			Player p = drawInfo.drawPlayer;

			float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f) * 1.5f;
			Vector2 pos = LayerUtils.Center(drawInfo) + new Vector2(0f, (-p.height / 2f - 6f + bob) * p.gravDir);
			Color color = look.EffectColor.Get();

			// Мягкий ореол + сам нимб
			drawInfo.DrawDataCache.Add(new DrawData(halo, pos, null, LayerUtils.Glow(color, 0.35f), 0f,
				halo.Size() / 2f, 1.35f, SpriteEffects.None, 0));
			drawInfo.DrawDataCache.Add(new DrawData(halo, pos, null, LayerUtils.Glow(color, 0.95f), 0f,
				halo.Size() / 2f, 1f, SpriteEffects.None, 0));
		}
	}
}
