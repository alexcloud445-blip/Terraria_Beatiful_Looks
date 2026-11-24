using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using BeautifulLooks.Common.Looks;
using BeautifulLooks.Common.Players;
using BeautifulLooks.Common.VFX;

namespace BeautifulLooks.Common.Effects
{
	/// <summary>
	/// Всё, что остаётся за персонажем при движении:
	/// частицы (TrailStyle), светящаяся лента (RibbonStyle) и призрачные копии (EchoStyle).
	/// </summary>
	public static class TrailEffects
	{
		// ======================= частицы =======================

		public static void Update(Player player, LookSettings look)
		{
			if (look.Trail == TrailStyle.None)
				return;

			float speed = player.velocity.Length();
			if (speed < 1.5f)
				return;

			// Чем быстрее движемся — тем гуще след
			float chance = MathHelper.Clamp(speed / 6f, 0.25f, 1.5f) * Fx.GlobalIntensity;

			for (int i = Fx.Rolls(chance); i > 0; i--)
			{
				Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-14f, 18f))
					- player.velocity * Main.rand.NextFloat(0.5f, 1.5f);
				Vector2 vel = -player.velocity * 0.08f;
				Palette pal = Palette.Of(look.TrailColor, look.TrailColor2, Main.rand.NextFloat());
				bool front = Main.rand.NextBool();
				float g = player.gravDir;

				switch (look.Trail)
				{
					case TrailStyle.Sparkles:
						if (Main.rand.NextBool(3))
							ParticleSystem.Spawn(new FlareParticle(pos, vel, new Ramp(Color.White, pal.A, pal.B), Main.rand.NextFloat(0.35f, 0.6f), 26) { Front = front });
						else
							ParticleSystem.Spawn(new GlowParticle(pos, vel, pal.Glow, Main.rand.NextFloat(0.25f, 0.45f), 30) { Front = front });
						break;
					case TrailStyle.Stars:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.Star4 : Shape.Star5, SpriteMotion.Spin, pos, vel, new Ramp(pal.Hi, pal.A, pal.B), Main.rand.NextFloat(0.5f, 0.8f), 40)
						{
							Glowing = true,
							GlowRamp = pal.B,
							Spin = Main.rand.NextFloat(-0.1f, 0.1f),
							Front = front
						});
						break;
					case TrailStyle.Flames:
						ParticleSystem.Spawn(new SmokeParticle(pos, vel + new Vector2(0f, -1.2f * g), pal.Hot, Main.rand.NextFloat(0.4f, 0.6f), 28)
						{
							Glowing = true,
							Grow = 0.3f,
							MaxOpacity = 0.75f,
							Front = front
						});
						if (Main.rand.NextBool(2))
							ParticleSystem.Spawn(new GlowParticle(pos, vel + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -1.8f * g), pal.Hot, 0.32f, 40) { Wander = 0.06f, Front = front });
						break;
					case TrailStyle.Hearts:
						ParticleSystem.Spawn(new ShapeParticle(Shape.Heart, SpriteMotion.Float, pos, vel + new Vector2(0f, -0.5f * g), new Ramp(pal.A, pal.B), 0.8f, 60)
						{
							GlowStrength = 0.55f,
							GlowRamp = new Ramp(pal.Hi, pal.B),
							Front = front
						});
						break;
					case TrailStyle.Petals:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Petal, SpriteMotion.Fall, pos, vel, new Ramp(pal.Hi, pal.A, pal.B), Main.rand.NextFloat(0.7f, 0.95f), 90)
						{
							GlowStrength = 0.3f,
							GlowRamp = pal.B,
							Settle = true,
							Front = front
						});
						break;
					case TrailStyle.Snow:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.SnowCrystal : Shape.Snowflake, SpriteMotion.Drift, pos, vel, new Ramp(Color.White, pal.A, pal.B), Main.rand.NextFloat(0.55f, 0.85f), 80)
						{
							Glowing = true,
							GlowRamp = pal.B,
							Settle = true,
							Front = front
						});
						break;
					case TrailStyle.Feathers:
						if (Main.rand.NextBool(3))
							ParticleSystem.Spawn(new ShapeParticle(Shape.Feather, SpriteMotion.Sway, pos, vel * 0.5f, new Ramp(Color.White, pal.Hi, pal.A), Main.rand.NextFloat(0.8f, 1f), 110)
							{
								GlowStrength = 0.4f,
								GlowRamp = pal.B,
								Settle = true,
								Front = front
							});
						else
							ParticleSystem.Spawn(new GlowParticle(pos, vel, pal.Glow, 0.28f, 30) { Front = front });
						break;
					case TrailStyle.Smoke:
						ParticleSystem.Spawn(new SmokeParticle(pos, vel * 0.5f, new Ramp(VfxMath.Shade(pal.A, 0.3f), new Color(8, 6, 12)), Main.rand.NextFloat(0.6f, 0.9f), 50)
						{
							MaxOpacity = 0.5f,
							Front = front
						});
						if (Main.rand.NextBool(3))
							ParticleSystem.Spawn(new GlowParticle(pos, vel, new Ramp(pal.B, pal.A), 0.28f, 35) { Front = front });
						break;
					case TrailStyle.Runes:
						if (Main.rand.NextBool(3))
							ParticleSystem.Spawn(new RuneParticle(pos, new Vector2(0f, -0.4f * g), pal.Glow, 0.8f, 50) { Front = front });
						else
							ParticleSystem.Spawn(new GlowParticle(pos, vel, pal.Glow, 0.28f, 30) { Front = front });
						break;
					case TrailStyle.Prism:
						float hue = VfxMath.Frac(Main.GlobalTimeWrappedHourly * 0.5f + Main.rand.NextFloat(0.3f));
						Color a = Main.hslToRgb(hue, 0.8f, 0.78f);
						Color b = Main.hslToRgb(VfxMath.Frac(hue + 0.3f), 0.75f, 0.68f);
						if (Main.rand.NextBool(2))
							ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.ShardA : Shape.Diamond, SpriteMotion.Spin, pos, vel + Main.rand.NextVector2Circular(0.8f, 0.8f),
								new Ramp(Color.White, a, b), Main.rand.NextFloat(0.5f, 0.8f), 40)
							{
								Glowing = true,
								GlowRamp = b,
								Spin = Main.rand.NextFloat(-0.15f, 0.15f),
								Front = front
							});
						else
							ParticleSystem.Spawn(new FlareParticle(pos, vel, new Ramp(Color.White, a, b), 0.45f, 26) { Front = front });
						break;
					case TrailStyle.Butterflies:
						if (Main.rand.NextBool(4))
							ParticleSystem.Spawn(new ShapeParticle(Shape.Butterfly0, SpriteMotion.Flutter, pos, Main.rand.NextVector2Circular(1f, 1f), new Ramp(pal.A, pal.B), 0.8f, 120)
							{
								GlowStrength = 0.45f,
								GlowRamp = pal.B,
								Front = front
							});
						else
							ParticleSystem.Spawn(new GlowParticle(pos, vel, pal.Soft, 0.22f, 30) { Front = front, Blink = 0.25f });
						break;
				}
			}
		}

		// ======================= лента =======================

		/// <summary>Сколько точек истории берёт лента при длине 1x.</summary>
		private const int BaseRibbonPoints = 24;

		private static readonly Vector2[] RibbonPos = new Vector2[LooksPlayer.HistoryLength];
		private static readonly float[] RibbonRot = new float[LooksPlayer.HistoryLength];
		private static readonly Vector2[] TwinPos = new Vector2[LooksPlayer.HistoryLength];

		/// <summary>Ленты всех видимых игроков (вызывается из VfxRenderer, позади игроков).</summary>
		public static void DrawRibbons()
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!p.active || !p.TryGetModPlayer(out LooksPlayer mp) || !mp.EffectsVisible)
					continue;
				if (mp.Look.Ribbon != RibbonStyle.None)
					DrawRibbon(mp);
				DashEffects.DrawRibbon(mp);
			}
		}

		private static void DrawRibbon(LooksPlayer mp)
		{
			LookSettings look = mp.Look;
			int count = Math.Clamp((int)(BaseRibbonPoints * look.RibbonLength), 6, Math.Min(LooksPlayer.HistoryLength, mp.HistoryCount));
			if (count < 3)
				return;

			// Точки ленты и направление движения в каждой точке
			float pathLength = 0f;
			for (int i = 0; i < count; i++)
			{
				RibbonPos[i] = mp.History[i] + new Vector2(0f, 4f * mp.Player.gravDir);
				if (i > 0)
					pathLength += Vector2.Distance(RibbonPos[i], RibbonPos[i - 1]);
			}

			for (int i = 0; i < count; i++)
			{
				Vector2 dir = i == 0 ? RibbonPos[0] - RibbonPos[1] : RibbonPos[i - 1] - RibbonPos[i];
				RibbonRot[i] = dir.LengthSquared() > 0.01f ? dir.ToRotation() : (i > 0 ? RibbonRot[i - 1] : 0f);
			}

			// Стоим на месте — лента плавно исчезает
			float opacity = MathHelper.Clamp((pathLength - 12f) / 80f, 0f, 1f);
			if (opacity <= 0.01f)
				return;

			Vector2[] positions = Slice(RibbonPos, count);
			float[] rotations = Slice(RibbonRot, count);
			float width = 12f * look.RibbonWidth;
			ColorSetting c1 = look.TrailColor, c2 = look.TrailColor2;

			VertexStrip.StripHalfWidthFunction widthFn = p => width * VfxMath.Ease(p * 5f) * (1f - p * 0.65f);

			switch (look.Ribbon)
			{
				case RibbonStyle.Rainbow:
					Ribbons.Draw(positions, rotations,
						p => VfxMath.Additive(Color.Lerp(Color.White, Main.hslToRgb(VfxMath.Frac(p * 1.6f - Main.GlobalTimeWrappedHourly), 0.85f, 0.62f), VfxMath.Ease(p * 3f)),
							opacity * (1f - p)),
						widthFn, RibbonStyle.Rainbow);
					break;

				case RibbonStyle.Empress:
					Ribbons.Draw(positions, rotations,
						p =>
						{
							Color prism = Main.hslToRgb(VfxMath.Frac(Main.GlobalTimeWrappedHourly * 0.4f + p), 0.8f, 0.78f);
							Color baseColor = Color.Lerp(c1.Get(), prism, 0.5f);
							return VfxMath.Additive(Color.Lerp(Color.White, baseColor, VfxMath.Ease(p * 4f)), opacity * (1f - p));
						},
						p => width * 1.3f * VfxMath.Ease(p * 5f) * (1f - p * 0.5f), RibbonStyle.Empress);
					break;

				case RibbonStyle.Twin:
					DrawTwin(positions, rotations, count, look, opacity, width);
					break;

				default: // Soft и Flame: от светлой головы к основному и второму цвету
				{
					Ramp ramp = Palette.Of(c1, c2).Glow;
					Ribbons.Draw(positions, rotations,
						p => VfxMath.Additive(ramp.At(p), opacity * (1f - p)),
						widthFn, look.Ribbon);
					break;
				}
			}
		}

		/// <summary>Две тонкие ленты, закрученные спиралью.</summary>
		private static void DrawTwin(Vector2[] positions, float[] rotations, int count, LookSettings look, float opacity, float width)
		{
			float time = Main.GlobalTimeWrappedHourly * 6f;
			float amp = 8f * look.RibbonWidth;
			for (int strand = 0; strand < 2; strand++)
			{
				float phase = strand * MathHelper.Pi;
				for (int i = 0; i < count; i++)
				{
					Vector2 normal = (rotations[i] + MathHelper.PiOver2).ToRotationVector2();
					float envelope = VfxMath.Ease(i / 4f);
					TwinPos[i] = positions[i] + normal * (float)Math.Sin(i * 0.45f - time + phase) * amp * envelope;
				}

				Palette pal = Palette.Of(look.TrailColor, look.TrailColor2);
				Ramp ramp = strand == 0 ? new Ramp(pal.Hi, pal.A, pal.B) : new Ramp(pal.Hi, pal.B, pal.A);
				Ribbons.Draw(Slice(TwinPos, count), rotations,
					p => VfxMath.Additive(ramp.At(p), opacity * (1f - p)),
					p => width * 0.45f * VfxMath.Ease(p * 5f) * (1f - p * 0.5f), RibbonStyle.Soft);
			}
		}

		/// <summary>Копия начала массива нужной длины (VertexStrip рисует массив целиком).</summary>
		private static T[] Slice<T>(T[] source, int count)
		{
			var result = new T[count];
			Array.Copy(source, result, count);
			return result;
		}

		// ======================= призрачные копии =======================

		/// <summary>Сейчас рисуется копия: LooksPlayer.TransformDrawData перекрашивает её.</summary>
		public static bool DrawingEcho;
		public static Color EchoColor;
		public static float EchoOpacity;
		public static bool EchoTint;
		/// <summary>Копия рисуется тёмной полупрозрачной тенью (обычное смешивание), а не светом.</summary>
		public static bool EchoDark;

		/// <summary>Рисует копии персонажа по прошлым позициям (вызывается из ModPlayer.DrawPlayer, до самого игрока).</summary>
		public static void DrawEchoes(LooksPlayer mp, Camera camera)
		{
			LookSettings look = mp.Look;
			Player player = mp.Player;
			if (look.Echo == EchoStyle.None || mp.HistoryCount < 4)
				return;

			int count, step;
			float startOpacity;
			bool tint;
			switch (look.Echo)
			{
				case EchoStyle.Afterimage:
					count = 6; step = 2; startOpacity = 0.45f; tint = false;
					break;
				case EchoStyle.Phantom:
					count = 5; step = 4; startOpacity = 0.55f; tint = true;
					break;
				default: // Ghost
					count = 4; step = 3; startOpacity = 0.5f; tint = true;
					break;
			}

			// Копии видны только в движении
			float moved = Vector2.Distance(mp.History[0], mp.History[Math.Min(mp.HistoryCount - 1, step * 2)]);
			float motion = MathHelper.Clamp((moved - 6f) / 30f, 0f, 1f);
			if (motion <= 0.01f)
				return;

			try
			{
				DrawingEcho = true;
				for (int i = count; i >= 1; i--)
				{
					int index = i * step;
					if (index >= mp.HistoryCount)
						continue;

					float t = i / (float)count;
					EchoTint = tint;
					EchoOpacity = startOpacity * (1f - t * 0.8f) * motion;
					EchoColor = look.Echo == EchoStyle.Phantom
						? look.TrailColor.Mix(look.TrailColor2, t)
						: look.TrailColor.Get(t * 0.3f);

					Vector2 topLeft = mp.History[index] - player.Size / 2f;
					Main.PlayerRenderer.DrawPlayer(camera, player, topLeft, player.fullRotation, player.fullRotationOrigin, 0.3f + 0.1f * i);
				}
			}
			catch (Exception e)
			{
				VfxRenderer.LogOnce("echoes", e);
			}
			finally
			{
				DrawingEcho = false;
			}
		}
	}
}
