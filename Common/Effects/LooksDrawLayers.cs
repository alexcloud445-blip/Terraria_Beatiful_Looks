using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Общие помощники для слоёв отрисовки. Всё, что рисуется слоями, видно и в превью меню.
	/// </summary>
	internal static class LayerUtils
	{
		public static LooksPlayer Mp(PlayerDrawSet info) => info.drawPlayer.GetModPlayer<LooksPlayer>();

		/// <summary>Рисовать ли эффекты для этого прохода отрисовки (не для теней и копий).</summary>
		public static bool Visible(PlayerDrawSet info)
		{
			if (info.shadow != 0f || info.drawPlayer.dead)
				return false;
			VfxAssets.Prepare();
			LooksPlayer mp = Mp(info);
			return mp.EffectsVisibleInDraw;
		}

		/// <summary>Центр персонажа в экранных координатах.</summary>
		public static Vector2 Center(PlayerDrawSet info) =>
			(info.Position - Main.screenPosition + info.drawPlayer.Size / 2f).Floor();

		public static float Pulse(float speed = 2f, float amount = 0.15f) =>
			1f - amount + amount * (float)Math.Sin(Main.GlobalTimeWrappedHourly * speed);

		/// <summary>Добавляет спрайт в кэш отрисовки игрока с краской эффектов.</summary>
		public static void Add(ref PlayerDrawSet info, int shader, Texture2D tex, Vector2 pos, Rectangle? frame, Color color,
			float rotation, Vector2 origin, Vector2 scale)
		{
			var data = new DrawData(tex, pos, frame, color, rotation, origin, scale, SpriteEffects.None, 0) { shader = shader };
			info.DrawDataCache.Add(data);
		}

		public static void Add(ref PlayerDrawSet info, int shader, Texture2D tex, Vector2 pos, Color color, float scale) =>
			Add(ref info, shader, tex, pos, null, color, 0f, tex.Size() / 2f, new Vector2(scale));

		/// <summary>
		/// Линия между двумя точками (текстура Line). Длина точная: цепочки из коротких отрезков
		/// (кольца, рога, ленты) не дают светлых «бусин» на стыках.
		/// </summary>
		public static void Line(ref PlayerDrawSet info, int shader, Vector2 a, Vector2 b, Color color, float thickness)
		{
			Texture2D line = VfxAssets.Line;
			Vector2 diff = b - a;
			Add(ref info, shader, line, a, null, color, diff.ToRotation(), new Vector2(0f, line.Height / 2f),
				new Vector2(diff.Length() / line.Width, thickness / line.Height));
		}
	}

	/// <summary>
	/// Эффекты позади персонажа: лучи, сияние, свечение ауры, магический круг, кольца под ногами.
	/// </summary>
	public class BackEffectsLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.JimsCloak, PlayerDrawLayers.MountBack);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (!LayerUtils.Visible(drawInfo))
				return false;
			LookSettings l = LayerUtils.Mp(drawInfo).Look;
			return l.BackGlow || l.GodRays || l.GroundRing || l.Sigil || l.PulseRing || l.Aura != AuraStyle.None || l.Halo == HaloStyle.Eclipse;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LookSettings look = LayerUtils.Mp(drawInfo).Look;
			Player p = drawInfo.drawPlayer;
			Vector2 center = LayerUtils.Center(drawInfo);
			Vector2 feet = center + new Vector2(0f, p.height / 2f * p.gravDir);
			int shader = EffectDyes.ShaderId(look.EffectDye);
			float time = Main.GlobalTimeWrappedHourly;

			if (look.GodRays)
				GodRays(ref drawInfo, look, center, shader, time);

			if (look.BackGlow)
			{
				Texture2D bloom = VfxAssets.Bloom;
				float pulse = LayerUtils.Pulse();
				LayerUtils.Add(ref drawInfo, shader, bloom, center, null, VfxMath.Additive(look.EffectColor2.Get(), 0.32f * pulse), 0f,
					bloom.Size() / 2f, new Vector2(0.7f, 0.85f) * pulse);
				LayerUtils.Add(ref drawInfo, shader, bloom, center, null, VfxMath.Additive(look.EffectColor.Get(), 0.3f), 0f,
					bloom.Size() / 2f, new Vector2(0.38f, 0.5f));
			}

			if (look.Aura != AuraStyle.None)
				AuraBackdrop(ref drawInfo, look, center, feet, time, p);

			if (look.Sigil)
				Sigil(ref drawInfo, look, feet, shader, time, p.gravDir);

			if (look.GroundRing)
			{
				Texture2D ring = VfxAssets.GroundRing;
				float pulse = LayerUtils.Pulse(3f);
				LayerUtils.Add(ref drawInfo, shader, ring, feet, null, VfxMath.Additive(look.EffectColor.Get(0.5f), 0.85f * pulse), 0f,
					ring.Size() / 2f, new Vector2(74f / 120f * pulse));
				Texture2D bloom = VfxAssets.Bloom;
				LayerUtils.Add(ref drawInfo, shader, bloom, feet, null, VfxMath.Additive(look.EffectColor2.Get(), 0.3f), 0f,
					bloom.Size() / 2f, new Vector2(0.6f, 0.16f));
			}

			CelestialHalos.DrawBehindBody(ref drawInfo, look);

			if (look.PulseRing)
			{
				Texture2D ring = VfxAssets.GroundRing;
				for (int i = 0; i < 3; i++)
				{
					float phase = VfxMath.Frac(time * 0.55f + i / 3f);
					float size = MathHelper.Lerp(40f, 130f, phase);
					float alpha = (float)Math.Pow(1f - phase, 1.6f) * VfxMath.Ease(phase * 6f);
					LayerUtils.Add(ref drawInfo, shader, ring, feet, null, VfxMath.Additive(look.EffectColor.Mix(look.EffectColor2, phase), alpha), 0f,
						ring.Size() / 2f, new Vector2(size / 120f));
				}
			}
		}

		/// <summary>Вращающиеся тонкие лучи света из-за спины: разная длина и ширина, цвета чередуются.</summary>
		private static void GodRays(ref PlayerDrawSet info, LookSettings look, Vector2 center, int shader, float time)
		{
			Texture2D ray = VfxAssets.GodRay;
			var origin = new Vector2(ray.Width / 2f, ray.Height);
			Color hi = Color.Lerp(look.EffectColor.Get(), Color.White, 0.55f);
			const int count = 14;
			for (int i = 0; i < count; i++)
			{
				float angle = time * (i % 2 == 0 ? 0.22f : -0.15f) + i * MathHelper.TwoPi / count + (i % 3) * 0.2f;
				float flicker = 0.7f + 0.3f * (float)Math.Sin(time * 1.9f + i * 2.3f);
				float length = (52f + 34f * (float)Math.Sin(time * 0.9f + i * 1.7f) * (float)Math.Sin(i * 0.9f)) * (0.85f + 0.15f * flicker);
				float width = i % 3 == 0 ? 0.55f : 0.32f;
				Color c = i % 3 == 0 ? hi : look.EffectColor.Mix(look.EffectColor2, (i % 5) / 4f);
				LayerUtils.Add(ref info, shader, ray, center, null, VfxMath.Additive(c, (i % 3 == 0 ? 0.45f : 0.55f) * flicker), angle + MathHelper.PiOver2, origin,
					new Vector2(width, length / ray.Height));
			}
		}

		/// <summary>Подсветка, которая показывает ауру и в превью меню, плюс постоянные детали некоторых аур.</summary>
		private static void AuraBackdrop(ref PlayerDrawSet info, LookSettings look, Vector2 center, Vector2 feet, float time, Player p)
		{
			Texture2D bloom = VfxAssets.Bloom;
			float s = look.AuraSize;
			Color c1 = look.AuraColor.Get();
			Color c2 = look.AuraColor2.Get();

			// мягкое свечение вокруг тела
			LayerUtils.Add(ref info, 0, bloom, center, null, VfxMath.Additive(c2, 0.15f * LayerUtils.Pulse(1.5f, 0.3f)), 0f,
				bloom.Size() / 2f, new Vector2(0.6f, 0.75f) * s);

			switch (look.Aura)
			{
				case AuraStyle.CelestialRing:
				{
					Texture2D ring = VfxAssets.GroundRing;
					LayerUtils.Add(ref info, 0, ring, center, null, VfxMath.Additive(c1, 0.55f), 0f, ring.Size() / 2f,
						new Vector2(92f / 120f * s));
					LayerUtils.Add(ref info, 0, ring, center, null, VfxMath.Additive(c2, 0.3f), 0f, ring.Size() / 2f,
						new Vector2(80f / 120f * s));
					break;
				}
				case AuraStyle.Divine:
				{
					// столб света сверху
					Texture2D ray = VfxAssets.GodRay;
					float pulse = LayerUtils.Pulse(2f, 0.2f);
					LayerUtils.Add(ref info, 0, ray, feet, null, VfxMath.Additive(c2, 0.35f * pulse), 0f,
						new Vector2(ray.Width / 2f, ray.Height), new Vector2(2.2f * s, 1.6f));
					LayerUtils.Add(ref info, 0, ray, feet, null, VfxMath.Additive(c1, 0.3f * pulse), 0f,
						new Vector2(ray.Width / 2f, ray.Height), new Vector2(1.1f * s, 1.3f));
					break;
				}
				case AuraStyle.Void:
				{
					// тёмное ядро и светящийся ободок
					LayerUtils.Add(ref info, 0, bloom, center, null, Color.Black * 0.45f, 0f, bloom.Size() / 2f, new Vector2(0.6f, 0.75f) * s);
					Texture2D ring = VfxAssets.RingWave;
					float spin = time * 0.8f;
					LayerUtils.Add(ref info, 0, ring, center, null, VfxMath.Additive(c1, 0.45f), spin, ring.Size() / 2f,
						new Vector2(0.62f, 0.62f) * s);
					break;
				}
				case AuraStyle.Flames:
				{
					LayerUtils.Add(ref info, 0, bloom, feet, null, VfxMath.Additive(c1, 0.45f * LayerUtils.Pulse(6f, 0.2f)), 0f,
						bloom.Size() / 2f, new Vector2(0.5f, 0.3f) * s);
					break;
				}
				case AuraStyle.Astrolabe:
					CelestialDraw.Astrolabe(ref info, look, center, false, p.gravDir);
					break;
				case AuraStyle.Aurora:
					CelestialDraw.Aurora(ref info, look, center, p.gravDir, p.height);
					break;
				case AuraStyle.Galaxy:
					CelestialDraw.Galaxy(ref info, look, center, false, p.gravDir);
					break;
				case AuraStyle.Eclipse:
					CelestialDraw.Eclipse(ref info, look, center, p.gravDir);
					break;
				case AuraStyle.Mandala:
					CelestialDraw.Mandala(ref info, look, center);
					break;
			}
		}

		/// <summary>Магический круг под ногами: кольца, бегущие руны и вращающаяся звезда (в перспективе).</summary>
		private static void Sigil(ref PlayerDrawSet info, LookSettings look, Vector2 feet, int shader, float time, float grav)
		{
			const float width = 116f;
			const float squash = 0.3f;
			float pulse = LayerUtils.Pulse(2.2f, 0.15f);
			Color c1 = look.EffectColor.Get();
			Color c2 = look.EffectColor2.Get();

			Texture2D bloom = VfxAssets.Bloom;
			LayerUtils.Add(ref info, shader, bloom, feet, null, VfxMath.Additive(c1, 0.35f * pulse), 0f, bloom.Size() / 2f,
				new Vector2(width, width * squash) / bloom.Width * 1.1f);

			Texture2D rings = VfxAssets.SigilRings;
			LayerUtils.Add(ref info, shader, rings, feet, null, VfxMath.Additive(c2, 0.45f * pulse), 0f, rings.Size() / 2f,
				new Vector2(width / 124f * 1.03f, width / 124f * 1.08f));
			LayerUtils.Add(ref info, shader, rings, feet, null, VfxMath.Additive(c1, 0.85f * pulse), 0f, rings.Size() / 2f,
				new Vector2(width / 124f));

			// внешний эллипс текстуры: 124x37 при размере 128x40
			float rx = width / 2f;
			float ry = rx * squash;

			// руны в поясе между кольцами
			const int runes = 10;
			for (int i = 0; i < runes; i++)
			{
				float a = time * 0.45f + i * MathHelper.TwoPi / runes;
				Vector2 pos = feet + VfxMath.OnEllipse(a, rx * 0.755f, ry * 0.755f);
				float depth = 0.65f + 0.35f * (float)Math.Sin(a) * grav;
				Color runeColor = VfxMath.LerpVivid(c2, Color.Lerp(c1, Color.White, 0.4f), 0.5f + 0.5f * (float)Math.Sin(a * 2f));
				LayerUtils.Add(ref info, shader, VfxAssets.Runes, pos, VfxAssets.RuneFrame(i), VfxMath.Additive(runeColor, depth * pulse), 0f,
					new Vector2(16f, 16f), new Vector2(0.42f, 0.28f));
			}

			// гексаграмма (два треугольника), вращается в другую сторону
			Vector2[] pts = new Vector2[6];
			for (int k = 0; k < 6; k++)
				pts[k] = feet + VfxMath.OnEllipse(-time * 0.3f + k * MathHelper.TwoPi / 6f, rx * 0.56f, ry * 0.56f);
			Color lineColor = VfxMath.Additive(VfxMath.LerpVivid(c1, c2, 0.5f), 0.85f * pulse);
			for (int k = 0; k < 6; k++)
				LayerUtils.Line(ref info, shader, pts[k], pts[(k + 2) % 6], lineColor, 2.2f);
		}
	}

	/// <summary>
	/// Эффекты ПЕРЕД персонажем (ближняя половина колец астролябии и диска галактики).
	/// Рисуется самым последним слоем игрока.
	/// </summary>
	public class FrontEffectsLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.BeetleBuff, PlayerDrawLayers.EyebrellaCloud);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (!LayerUtils.Visible(drawInfo))
				return false;
			AuraStyle aura = LayerUtils.Mp(drawInfo).Look.Aura;
			return aura == AuraStyle.Astrolabe || aura == AuraStyle.Galaxy;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LookSettings look = LayerUtils.Mp(drawInfo).Look;
			Player p = drawInfo.drawPlayer;
			Vector2 center = LayerUtils.Center(drawInfo);
			if (look.Aura == AuraStyle.Astrolabe)
				CelestialDraw.Astrolabe(ref drawInfo, look, center, true, p.gravDir);
			else if (look.Aura == AuraStyle.Galaxy)
				CelestialDraw.Galaxy(ref drawInfo, look, center, true, p.gravDir);
		}
	}

	/// <summary>Нимб над головой (9 стилей). Дальняя часть некоторых нимбов v0.6 — в HaloBackLayer (за головой).</summary>
	public class HaloLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
			LayerUtils.Visible(drawInfo) && LayerUtils.Mp(drawInfo).Look.Halo != HaloStyle.None;

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LookSettings look = LayerUtils.Mp(drawInfo).Look;
			Player p = drawInfo.drawPlayer;
			int shader = EffectDyes.ShaderId(look.EffectDye);
			float time = Main.GlobalTimeWrappedHourly;

			float bob = (float)Math.Sin(time * 2.5f) * 1.5f;
			Vector2 pos = LayerUtils.Center(drawInfo) + new Vector2(0f, (-p.height / 2f - 7f + bob) * p.gravDir);
			Color c1 = look.EffectColor.Get();
			Color c2 = look.EffectColor2.Get();
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D flare = VfxAssets.Flare;

			if (look.Halo >= HaloStyle.RuneCrown)
			{
				// нимбы v0.6 (часть из них ещё и за головой — HaloBackLayer)
				CelestialHalos.Draw(ref drawInfo, look, true);
				return;
			}

			// общий ореол
			LayerUtils.Add(ref drawInfo, shader, bloom, pos, null, VfxMath.Additive(c2, 0.32f), 0f, bloom.Size() / 2f, new Vector2(0.34f, 0.13f));

			switch (look.Halo)
			{
				case HaloStyle.Ring:
					HaloRing(ref drawInfo, shader, pos, c1, 28f);
					break;

				case HaloStyle.Double:
				{
					HaloRing(ref drawInfo, shader, pos, c1, 30f);
					Vector2 upper = pos + new Vector2(0f, -5f * p.gravDir);
					HaloRing(ref drawInfo, shader, upper, c2, 19f);
					for (int i = 0; i < 2; i++)
					{
						float a = time * 2f + i * MathHelper.Pi;
						LayerUtils.Add(ref drawInfo, shader, flare, pos + VfxMath.OnEllipse(a, 15f, 4.5f), null, VfxMath.Additive(Color.White, 0.8f),
							0f, flare.Size() / 2f, new Vector2(0.1f));
						LayerUtils.Add(ref drawInfo, shader, flare, upper + VfxMath.OnEllipse(-a, 9.5f, 3f), null, VfxMath.Additive(c2, 0.8f),
							0f, flare.Size() / 2f, new Vector2(0.08f));
					}
					break;
				}

				case HaloStyle.Radiant:
				{
					// корона света: веер лучей вверх
					Texture2D ray = VfxAssets.GodRay;
					const int rays = 9;
					for (int i = 0; i < rays; i++)
					{
						float spread = (i - (rays - 1) / 2f) / ((rays - 1) / 2f); // -1..1
						float angle = spread * 1.1f;
						float len = (16f + 6f * (float)Math.Sin(time * 3f + i * 1.3f)) * (1f - 0.35f * Math.Abs(spread));
						Vector2 basePos = pos + new Vector2(spread * 12f, 0f);
						LayerUtils.Add(ref drawInfo, shader, ray, basePos, null, VfxMath.Additive(Color.Lerp(c1, c2, Math.Abs(spread)), 0.7f),
							angle * p.gravDir + (p.gravDir < 0 ? MathHelper.Pi : 0f), new Vector2(ray.Width / 2f, ray.Height),
							new Vector2(0.22f, len / ray.Height));
					}
					HaloRing(ref drawInfo, shader, pos, c1, 28f);
					break;
				}

				case HaloStyle.Thorns:
				{
					Texture2D thorns = VfxAssets.ThornHalo;
					var origin = new Vector2(20f, 11f);
					SpriteEffects flip = p.gravDir < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
					LayerUtils.Add(ref drawInfo, shader, bloom, pos, null, VfxMath.Additive(c1, 0.45f), 0f, bloom.Size() / 2f, new Vector2(0.3f, 0.12f));
					drawInfo.DrawDataCache.Add(new DrawData(thorns, pos, null, Color.Lerp(c2, Color.Black, 0.35f), 0f, origin, 1.05f, flip, 0) { shader = shader });
					drawInfo.DrawDataCache.Add(new DrawData(thorns, pos, null, VfxMath.Additive(c1, 0.55f), 0f, origin, 1.05f, flip, 0) { shader = shader });
					break;
				}

				case HaloStyle.Starry:
				{
					HaloRing(ref drawInfo, shader, pos, c1, 26f, 0.55f);
					const int stars = 5;
					for (int i = 0; i < stars; i++)
					{
						float a = time * 1.2f + i * MathHelper.TwoPi / stars;
						float depth = 0.6f + 0.4f * (float)Math.Sin(a);
						float twinkle = 0.75f + 0.25f * (float)Math.Sin(time * 5f + i * 2f);
						LayerUtils.Add(ref drawInfo, shader, flare, pos + VfxMath.OnEllipse(a, 15f, 4.5f), null,
							VfxMath.Additive(Color.Lerp(c2, Color.White, 0.4f), depth * twinkle), a * 0.5f, flare.Size() / 2f,
							new Vector2(0.11f * depth * twinkle));
					}
					break;
				}
			}
		}

		private static void HaloRing(ref PlayerDrawSet info, int shader, Vector2 pos, Color color, float width, float opacity = 1f)
		{
			Texture2D ring = VfxAssets.HaloRing;
			Vector2 scale = new Vector2(width / 27f);
			LayerUtils.Add(ref info, shader, ring, pos, null, VfxMath.Additive(color, 0.95f * opacity), 0f, ring.Size() / 2f, scale);
			LayerUtils.Add(ref info, shader, ring, pos, null, VfxMath.Additive(Color.Lerp(color, Color.White, 0.6f), 0.4f * opacity), 0f, ring.Size() / 2f, scale * 0.97f);
		}
	}
}
