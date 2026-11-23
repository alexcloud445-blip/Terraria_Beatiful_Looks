using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// «Небесный механизм» ауры Астролябия: три наклонных кольца в 3D, которые медленно прецессируют
	/// вокруг вертикальной оси. Точки колец: X вправо, Y вверх, Z — к зрителю (Z &gt; 0 — перед персонажем).
	/// </summary>
	internal static class AstroRig
	{
		public const int Rings = 3;
		public static readonly float[] Radius = { 46f, 38f, 30f };
		private static readonly float[] Tilt = { 0.45f, 1.3f, 0.9f };
		private static readonly float[] Precess = { 0.32f, -0.26f, 0.45f };
		private static readonly float[] Phase = { 0f, 1.4f, 2.7f };

		/// <summary>Два единичных вектора в плоскости кольца.</summary>
		public static void Basis(int ring, float time, out Vector3 e1, out Vector3 e2)
		{
			float phi = time * Precess[ring] + Phase[ring];
			float st = (float)Math.Sin(Tilt[ring]);
			var normal = new Vector3(st * (float)Math.Cos(phi), (float)Math.Cos(Tilt[ring]), st * (float)Math.Sin(phi));
			e1 = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, normal));
			e2 = Vector3.Cross(normal, e1);
		}

		public static Vector3 Point(Vector3 e1, Vector3 e2, float theta, float radius) =>
			(e1 * (float)Math.Cos(theta) + e2 * (float)Math.Sin(theta)) * radius;

		/// <summary>Точка кольца на экране (смещение от центра персонажа).</summary>
		public static Vector2 ToScreen(Vector3 p, float grav) => new Vector2(p.X, -p.Y * grav);
	}

	/// <summary>Частицы новых аур v0.6 (отрисовка постоянных частей — в CelestialDraw).</summary>
	public static partial class AuraEffects
	{
		/// <summary>Астролябия: искры вспыхивают на кольцах, с «планет» срывается звёздная пыль.</summary>
		private static void Astrolabe(Ctx c)
		{
			float time = Main.GlobalTimeWrappedHourly * c.V;
			for (int i = Fx.Rolls(0.22f * c.D); i > 0; i--)
			{
				int ring = Main.rand.Next(AstroRig.Rings);
				AstroRig.Basis(ring, time, out Vector3 e1, out Vector3 e2);
				Vector3 p = AstroRig.Point(e1, e2, R(0f, MathHelper.TwoPi), AstroRig.Radius[ring] * c.S);
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + AstroRig.ToScreen(p, c.G);
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new FlareParticle(pos, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), R(0.22f, 0.38f) * c.S, 26) { Front = p.Z >= 0f });
				else
					ParticleSystem.Spawn(new GlowParticle(pos, Main.rand.NextVector2Circular(0.25f, 0.25f), pal.Glow, R(0.14f, 0.24f) * c.S, 36) { Front = p.Z >= 0f, Drag = 0.97f });
			}
		}

		/// <summary>Северное сияние: с кромки занавеса медленно опускаются огоньки, над ним мерцают звёзды.</summary>
		private static void Aurora(Ctx c)
		{
			float top = -(c.P.height * 0.5f + 24f) * c.G;
			for (int i = Fx.Rolls(0.16f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + new Vector2(R(-64f, 64f) * c.S, top + R(-6f, 6f));
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.15f, 0.15f), R(0.15f, 0.4f) * c.G * c.V), new Ramp(pal.Hi, pal.A, pal.B), R(0.16f, 0.28f) * c.S, Main.rand.Next(60, 90))
				{
					Drag = 0.99f,
					Wander = 0.015f,
					Front = false
				});
			}

			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + new Vector2(R(-70f, 70f) * c.S, top - R(40f, 90f) * c.S * c.G);
				ParticleSystem.Spawn(new FlareParticle(pos, Vector2.Zero, new Ramp(Color.White, pal.B), R(0.2f, 0.32f), Main.rand.Next(30, 50)) { Front = false });
			}
		}

		/// <summary>Галактика: звёзды по спирали затягиваются в центр диска, в центре вспыхивают блики.</summary>
		private static void Galaxy(Ctx c)
		{
			for (int i = Fx.Rolls(0.3f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ShapeParticle p = Orbiter(c, Main.rand.NextBool(4) ? Shape.Star4 : Shape.Wisp, new Ramp(pal.B, pal.A, pal.Hi), R(58f, 72f) * c.S, -0.75f * c.V, 0.035f * c.V,
					R(0.22f, 0.38f) * c.S, 80);
				p.Glowing = true;
				p.GlowStrength = 0.3f;
				p.OrbitSquash = 0.4f;
				p.FaceVelocity = p.Shape == Shape.Wisp;
				p.HomeOffset = new Vector2(0f, 6f * c.G);
				ParticleSystem.Spawn(p);
			}

			for (int i = Fx.Rolls(0.04f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new FlareParticle(c.P.Center + new Vector2(R(-8f, 8f), 6f * c.G + R(-3f, 3f)), Vector2.Zero, new Ramp(Color.White, pal.Hi, pal.A), R(0.35f, 0.55f) * c.S, 30) { Front = Coin });
			}
		}

		/// <summary>Затмение: с короны срываются огненные искры, иногда — длинный протуберанец из угольков.</summary>
		private static void Eclipse(Ctx c)
		{
			Vector2 center = c.P.Center + new Vector2(0f, -8f * c.G) * c.S;
			float radius = 26f * c.S;
			for (int i = Fx.Rolls(0.28f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 dir = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new GlowParticle(center + dir * radius * R(1f, 1.15f), dir * R(0.4f, 1.1f) * c.V, pal.Hot, R(0.16f, 0.3f) * c.S, Main.rand.Next(30, 50))
				{
					Drag = 0.97f,
					Front = false
				});
			}

			if (Main.rand.NextFloat() < 0.012f * c.D)
			{
				Palette pal = c.Pal;
				Vector2 dir = Main.rand.NextVector2Unit();
				Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
				for (int k = 0; k < 9; k++)
				{
					float t = k / 8f;
					// дуга протуберанца: угольки летят наружу и загибаются
					Vector2 vel = (dir * (1.6f - t * 0.8f) + side * (t - 0.5f) * 1.4f) * c.V;
					ParticleSystem.Spawn(new GlowParticle(center + dir * radius, vel, pal.Hot, R(0.22f, 0.34f) * c.S, 36 + k * 2)
					{
						Drag = 0.95f,
						Gravity = -dir * 0.03f,
						Front = false
					});
				}
			}
		}

		/// <summary>Мандала: по кольцам вспыхивают блики, изредка выплывает цветок лотоса.</summary>
		private static void Mandala(Ctx c)
		{
			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				float r = (Main.rand.NextBool() ? 75f : 46f) * c.S;
				Vector2 pos = c.P.Center + R(0f, MathHelper.TwoPi).ToRotationVector2() * r;
				ParticleSystem.Spawn(new FlareParticle(pos, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), R(0.22f, 0.36f) * c.S, 28) { Front = false });
			}

			if (Main.rand.NextFloat() < 0.006f * c.D)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Lotus, SpriteMotion.Float, c.P.Center + new Vector2(R(-30f, 30f), R(-20f, 20f)) * c.S, new Vector2(0f, 0.35f * c.Up),
					new Ramp(pal.Hi, pal.A, pal.B), 0.9f * c.S, 100) { GlowStrength = 0.7f, GlowRamp = pal.B, Front = false });
			}

			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.6f), new Vector2(0f, 0.25f * c.Up), c.Pal.Soft, R(0.15f, 0.25f) * c.S, 50) { Front = false, Blink = 0.2f });
		}
	}

	/// <summary>Постоянные части новых аур, которые рисуются слоями игрока (видны и в превью меню).</summary>
	internal static class CelestialDraw
	{
		private static void Add(ref PlayerDrawSet info, Texture2D tex, Vector2 pos, Rectangle? frame, Color color, float rotation, Vector2 origin, Vector2 scale,
			SpriteEffects fx = SpriteEffects.None) =>
			info.DrawDataCache.Add(new DrawData(tex, pos, frame, color, rotation, origin, scale, fx, 0));

		// ======================= Астролябия =======================

		/// <summary>Кольца, деления, руны и «планеты». front — только ближняя к зрителю половина.</summary>
		public static void Astrolabe(ref PlayerDrawSet info, LookSettings look, Vector2 center, bool front, float grav)
		{
			float s = look.AuraSize;
			float t = Main.GlobalTimeWrappedHourly * look.AuraSpeed;
			Palette pal = Palette.Of(look.AuraColor, look.AuraColor2);
			const int segs = 40;

			// ось «механизма» и полюса (позади)
			if (!front)
			{
				Vector2 top = center + new Vector2(0f, -58f * s * grav);
				Vector2 bottom = center + new Vector2(0f, 58f * s * grav);
				LayerUtils.Line(ref info, 0, top, bottom, VfxMath.Additive(pal.A, 0.1f), 7f * s);
				Texture2D glint = VfxAssets.Glint;
				LayerUtils.Add(ref info, 0, glint, top, VfxMath.Additive(pal.Hi, 0.55f), 0.22f * s);
				LayerUtils.Add(ref info, 0, glint, bottom, VfxMath.Additive(pal.Hi, 0.4f), 0.18f * s);
			}

			for (int r = 0; r < AstroRig.Rings; r++)
			{
				AstroRig.Basis(r, t, out Vector3 e1, out Vector3 e2);
				float radius = AstroRig.Radius[r] * s;
				Color ringColor = r == 0 ? pal.A : r == 1 ? pal.Hi : pal.B;

				for (int k = 0; k < segs; k++)
				{
					float a0 = k * MathHelper.TwoPi / segs;
					float a1 = (k + 1) * MathHelper.TwoPi / segs;
					Vector3 p0 = AstroRig.Point(e1, e2, a0, radius);
					Vector3 p1 = AstroRig.Point(e1, e2, a1, radius);
					float z = (p0.Z + p1.Z) * 0.5f / radius;
					if (z >= 0f != front)
						continue;

					float depth = 0.5f + 0.5f * z;
					// по кольцу бежит блик
					float shine = (float)Math.Pow(Math.Max(0.0, Math.Sin(a0 * 0.5f - t * 0.8f - r * 1.3f)), 12.0);
					Color c = VfxMath.LerpVivid(ringColor, pal.Hi, 0.15f + 0.6f * shine);
					Vector2 sa = center + AstroRig.ToScreen(p0, grav);
					Vector2 sb = center + AstroRig.ToScreen(p1, grav);
					LayerUtils.Line(ref info, 0, sa, sb, VfxMath.Additive(ringColor, 0.1f + 0.14f * depth), 10f * s);
					LayerUtils.Line(ref info, 0, sa, sb, VfxMath.Additive(c, 0.35f + 0.5f * depth + 0.4f * shine), 3.2f);

					if (r == 0)
					{
						// внутренняя тонкая дорожка и деления, как на шкале астролябии
						Vector2 ia = center + AstroRig.ToScreen(p0 * 0.9f, grav);
						Vector2 ib = center + AstroRig.ToScreen(p1 * 0.9f, grav);
						LayerUtils.Line(ref info, 0, ia, ib, VfxMath.Additive(ringColor, 0.18f + 0.3f * depth), 2.4f);
						if (k % 2 == 0)
						{
							Vector2 outward = AstroRig.ToScreen(p0, grav).SafeNormalize(Vector2.UnitX);
							float len = (k % 10 == 0 ? 5f : 2.5f) * s;
							LayerUtils.Line(ref info, 0, ia, ia + outward * len, VfxMath.Additive(c, 0.3f + 0.4f * depth), 2.6f);
						}
					}
				}

				// украшения колец
				if (r == 0)
				{
					Vector3 p = AstroRig.Point(e1, e2, t * 0.7f, radius);
					if (p.Z >= 0f == front)
					{
						float depth = 0.5f + 0.5f * p.Z / radius;
						Vector2 pos = center + AstroRig.ToScreen(p, grav);
						Texture2D flare = VfxAssets.Flare;
						LayerUtils.Add(ref info, 0, VfxAssets.Bloom, pos, VfxMath.Additive(pal.B, 0.3f * depth), 0.18f * s);
						LayerUtils.Add(ref info, 0, flare, pos, null, VfxMath.Additive(pal.Hi, 0.6f + 0.4f * depth), t, flare.Size() / 2f, new Vector2(0.16f * s * (0.8f + 0.2f * depth)));
					}
				}
				else if (r == 1)
				{
					const int runes = 6;
					for (int k = 0; k < runes; k++)
					{
						Vector3 p = AstroRig.Point(e1, e2, t * 0.9f + k * MathHelper.TwoPi / runes, radius);
						if (p.Z >= 0f != front)
							continue;
						float depth = 0.5f + 0.5f * p.Z / radius;
						Vector2 pos = center + AstroRig.ToScreen(p, grav);
						Color rc = VfxMath.LerpVivid(ringColor, pal.Hi, 0.35f);
						LayerUtils.Add(ref info, 0, VfxAssets.Bloom, pos, VfxMath.Additive(ringColor, 0.18f * depth), 0.14f * s);
						LayerUtils.Add(ref info, 0, VfxAssets.Runes, pos, VfxAssets.RuneFrame(k), VfxMath.Additive(rc, 0.45f + 0.5f * depth), 0f,
							new Vector2(16f, 16f), new Vector2(0.38f * s * (0.85f + 0.15f * depth)));
					}
				}
				else
				{
					for (int k = 0; k < 2; k++)
					{
						Vector3 p = AstroRig.Point(e1, e2, -t * 1.2f + k * MathHelper.Pi, radius);
						if (p.Z >= 0f != front)
							continue;
						float depth = 0.5f + 0.5f * p.Z / radius;
						Vector2 pos = center + AstroRig.ToScreen(p, grav);
						Texture2D orb = VfxAssets.Orb;
						Color pc = k == 0 ? pal.Hi : pal.A;
						LayerUtils.Add(ref info, 0, VfxAssets.Bloom, pos, VfxMath.Additive(pal.B, 0.3f * depth), 0.16f * s);
						LayerUtils.Add(ref info, 0, orb, pos, VfxMath.Additive(pc, 0.6f + 0.4f * depth), 0.15f * s * (0.8f + 0.2f * depth));
						LayerUtils.Add(ref info, 0, VfxAssets.HaloRing, pos, null, VfxMath.Additive(pc, 0.5f * depth + 0.2f), -0.35f,
							VfxAssets.HaloRing.Size() / 2f, new Vector2(0.45f * s));
					}
				}
			}
		}

		// ======================= Северное сияние =======================

		/// <summary>Два занавеса над головой (дальний бледнее и выше): складки бегут волной, цвет переливается.</summary>
		public static void Aurora(ref PlayerDrawSet info, LookSettings look, Vector2 center, float grav, float playerHeight)
		{
			float s = look.AuraSize;
			float t = Main.GlobalTimeWrappedHourly * look.AuraSpeed;
			Palette pal = Palette.Of(look.AuraColor, look.AuraColor2);
			Texture2D tex = VfxAssets.AuroraStrip;
			SpriteEffects fx = grav < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;
			var origin = new Vector2(16f, grav < 0f ? 10f : 118f);
			const int strips = 30;

			for (int layer = 0; layer < 2; layer++)
			{
				bool far = layer == 0;
				float width = 150f * s * (far ? 0.85f : 1f);
				float baseY = -(playerHeight * 0.5f + (far ? 28f : 16f) * s);
				for (int i = 0; i < strips; i++)
				{
					float u = i / (float)(strips - 1);
					float x = (u - 0.5f) * width;
					float env = (float)Math.Pow(Math.Sin(u * MathHelper.Pi), 0.7);
					float fold = (float)Math.Sin(x * 0.045f + t * 1.1f + layer * 1.7f) * 7f + (float)Math.Sin(x * 0.11f - t * 1.6f + layer) * 3.5f;
					float sway = (float)Math.Sin(t * 0.7f + x * 0.03f + layer) * 5f;
					float h = (54f + 22f * (float)Math.Sin(x * 0.06f + t * 0.5f + layer * 2f)) * s * (far ? 0.8f : 1f);
					Vector2 pos = center + new Vector2(x + sway, (baseY + fold * s) * grav);
					float hue = 0.5f + 0.5f * (float)Math.Sin(x * 0.025f + t * 0.6f + layer * 2.1f);
					Color lower = VfxMath.LerpVivid(pal.A, pal.B, hue * 0.35f);
					Color upper = VfxMath.LerpVivid(pal.B, pal.A, hue * 0.3f);
					// отдельные «лучи» занавеса: яркость и высота медленно гуляют от складки к складке
					float ray = 0.5f + 0.5f * (float)(Math.Sin(i * 1.93f + t * 0.9f + layer) * Math.Sin(i * 0.71f - t * 0.6f));
					float alpha = env * (far ? 0.22f : 0.34f) * (0.45f + 0.55f * ray);
					var scale = new Vector2(width / strips * 2.6f / 32f, h * (0.8f + 0.35f * ray) / 128f);
					Add(ref info, tex, pos, null, VfxMath.Additive(lower, alpha), 0f, origin, scale, fx);
					Add(ref info, tex, pos, null, VfxMath.Additive(upper, alpha * 0.55f), 0f, origin, scale * new Vector2(1.15f, 1.5f), fx);
				}
			}
		}

		// ======================= Галактика =======================

		/// <summary>
		/// Диск галактики в перспективе: дальняя половина позади персонажа, ближняя — перед ним (полупрозрачно).
		/// 32 кадра вращения плавно смешиваются.
		/// </summary>
		public static void Galaxy(ref PlayerDrawSet info, LookSettings look, Vector2 center, bool front, float grav)
		{
			float s = look.AuraSize;
			Palette pal = Palette.Of(look.AuraColor, look.AuraColor2);
			float f = Main.GlobalTimeWrappedHourly * look.AuraSpeed * 2f;
			int frames = VfxAssets.GalaxyFrames;
			int i0 = ((int)Math.Floor(f) % frames + frames) % frames;
			float blend = f - (float)Math.Floor(f);
			Vector2 pos = center + new Vector2(0f, 6f * grav * s);
			float scale = s;
			float alpha = front ? 0.55f : 1f;
			Texture2D tex = VfxAssets.Galaxy;

			for (int k = 0; k < 2; k++)
			{
				Rectangle frame = VfxAssets.GalaxyFrame((i0 + k) % frames);
				float w = k == 0 ? 1f - blend : blend;
				if (w <= 0.01f)
					continue;
				Rectangle half = front ? new Rectangle(frame.X, frame.Y + 32, 160, 32) : new Rectangle(frame.X, frame.Y, 160, 32);
				var origin = new Vector2(80f, front ? 0f : 32f);
				Add(ref info, tex, pos, half, VfxMath.Additive(pal.B, alpha * w * 0.45f), 0f, origin, new Vector2(scale * 1.08f));
				Add(ref info, tex, pos, half, VfxMath.Additive(pal.A, alpha * w), 0f, origin, new Vector2(scale));
			}

			if (!front)
			{
				Texture2D bloom = VfxAssets.Bloom;
				LayerUtils.Add(ref info, 0, bloom, pos, null, VfxMath.Additive(pal.B, 0.25f), 0f, bloom.Size() / 2f, new Vector2(1.1f, 0.42f) * s);
				LayerUtils.Add(ref info, 0, bloom, pos, VfxMath.Additive(pal.Hi, 0.4f * LayerUtils.Pulse(1.3f, 0.2f)), 0.22f * s);
			}
		}

		// ======================= Затмение =======================

		/// <summary>Тёмный диск за спиной с пылающей короной, протуберанцами и «бриллиантовым кольцом».</summary>
		public static void Eclipse(ref PlayerDrawSet info, LookSettings look, Vector2 center, float grav)
		{
			float s = look.AuraSize;
			float t = Main.GlobalTimeWrappedHourly * look.AuraSpeed;
			Palette pal = Palette.Of(look.AuraColor, look.AuraColor2);
			Vector2 pos = center + new Vector2(0f, -8f * grav) * s;
			const float radius = 26f;
			float pulse = LayerUtils.Pulse(1.7f, 0.12f);

			Texture2D bloom = VfxAssets.Bloom;
			LayerUtils.Add(ref info, 0, bloom, pos, VfxMath.Additive(pal.B, 0.3f * pulse), 1.15f * s);
			Texture2D corona = VfxAssets.Corona;
			LayerUtils.Add(ref info, 0, corona, pos, null, VfxMath.Additive(pal.B, 0.55f), -t * 0.07f, corona.Size() / 2f, new Vector2(radius / 23f * 1.25f * s * pulse));
			LayerUtils.Add(ref info, 0, corona, pos, null, VfxMath.Additive(pal.A, 0.95f), t * 0.05f, corona.Size() / 2f, new Vector2(radius / 23f * s));

			// протуберанцы: языки огня с края диска
			Texture2D flame = VfxAssets.Flame;
			for (int k = 0; k < 5; k++)
			{
				float a = t * 0.12f + k * MathHelper.TwoPi / 5f + (float)Math.Sin(t * 0.4f + k) * 0.3f;
				Vector2 dir = a.ToRotationVector2();
				float flicker = 0.75f + 0.25f * (float)Math.Sin(t * 5f + k * 2.1f);
				Rectangle frame = VfxAssets.FlameFrame((int)(t * 9f) + k);
				LayerUtils.Add(ref info, 0, flame, pos + dir * (radius - 2f) * s, frame, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.B, 0.4f), 0.55f * flicker), a + MathHelper.PiOver2,
					new Vector2(16f, 46f), new Vector2(0.38f, 0.32f * flicker) * s);
			}

			// тёмный диск (обычное смешивание) и тонкий светлый ободок
			Texture2D disc = VfxAssets.Disc;
			LayerUtils.Add(ref info, 0, disc, pos, null, new Color(4, 2, 10) * 0.96f, 0f, disc.Size() / 2f, new Vector2(radius / 30f * s));
			Texture2D ring = VfxAssets.RingWave;
			LayerUtils.Add(ref info, 0, ring, pos, null, VfxMath.Additive(pal.Hi, 0.55f), 0f, ring.Size() / 2f, new Vector2(radius / 53.8f * s));

			// «бриллиантовое кольцо»: медленно обходящий край яркий блик
			float da = t * 0.3f;
			Vector2 diamond = pos + da.ToRotationVector2() * radius * s;
			float sparkle = 0.6f + 0.4f * (float)Math.Sin(t * 3.1f);
			LayerUtils.Add(ref info, 0, bloom, diamond, VfxMath.Additive(pal.Hi, 0.4f * sparkle), 0.2f * s);
			LayerUtils.Add(ref info, 0, VfxAssets.Glint, diamond, null, VfxMath.Additive(Color.White, 0.9f * sparkle), 0f, new Vector2(32f, 32f), new Vector2(0.55f * s * sparkle));
		}

		// ======================= Мандала =======================

		/// <summary>Огромный тонкий узор за спиной: части вращаются в разные стороны и медленно «дышат».</summary>
		public static void Mandala(ref PlayerDrawSet info, LookSettings look, Vector2 center)
		{
			float s = look.AuraSize;
			float t = Main.GlobalTimeWrappedHourly * look.AuraSpeed;
			Palette pal = Palette.Of(look.AuraColor, look.AuraColor2);
			float breathe = 1f + 0.025f * (float)Math.Sin(t * 1.4f);
			float glow = LayerUtils.Pulse(1.4f, 0.15f);

			Texture2D bloom = VfxAssets.Bloom;
			LayerUtils.Add(ref info, 0, bloom, center, VfxMath.Additive(pal.B, 0.18f), 1.25f * s);

			Texture2D outer = VfxAssets.MandalaOuter;
			Texture2D inner = VfxAssets.MandalaInner;
			Vector2 o = outer.Size() / 2f;
			LayerUtils.Add(ref info, 0, outer, center, null, VfxMath.Additive(pal.B, 0.22f), t * 0.08f, o, new Vector2(0.58f * s * breathe * 1.015f));
			LayerUtils.Add(ref info, 0, outer, center, null, VfxMath.Additive(pal.A, 0.5f * glow), t * 0.08f, o, new Vector2(0.58f * s * breathe));
			LayerUtils.Add(ref info, 0, inner, center, null, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.B, 0.6f), 0.5f * glow), -t * 0.13f, o, new Vector2(0.55f * s * breathe));
			LayerUtils.Add(ref info, 0, inner, center, null, VfxMath.Additive(pal.Hi, 0.35f), t * 0.25f, o, new Vector2(0.22f * s * breathe));
		}
	}
}
