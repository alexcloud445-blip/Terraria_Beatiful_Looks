using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Следы шагов, прыжок и приземление (StepStyle). Цвета — из вкладки «Движение».
	/// Шаг — каждые ~20 пикселей пути по земле, попеременно чуть впереди и позади.
	/// Прыжок — отрыв от земли или резкий толчок вверх в воздухе (двойной прыжок, ракетные ботинки).
	/// Приземление — касание земли после быстрого падения; чем сильнее удар, тем шире волна.
	/// </summary>
	public static class StepEffects
	{
		private const float StepDistance = 20f;

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		private static Palette Pal(LookSettings look) => Palette.Of(look.MotionColor, look.MotionColor2, Main.rand.NextFloat(0.4f));

		public static void Update(LooksPlayer mp, bool visible)
		{
			Player p = mp.Player;
			StepStyle style = mp.Look.Steps;
			bool active = visible && style != StepStyle.None && !p.mount.Active && p.grapCount == 0 && !p.pulley;
			bool onGround = p.velocity.Y == 0f;
			float vy = p.velocity.Y * p.gravDir;
			Vector2 feet = p.gravDir > 0f ? p.Bottom : p.Top;
			float g = p.gravDir;

			if (mp.JumpCooldown > 0)
				mp.JumpCooldown--;

			if (active)
			{
				if (onGround && !mp.WasOnGround && mp.PrevVelocityY > 7f)
				{
					Land(mp, style, feet, g, MathHelper.Clamp((mp.PrevVelocityY - 7f) / 9f, 0f, 1f));
				}
				else if (mp.JumpCooldown == 0 && vy < -2f && (mp.WasOnGround || (vy < mp.PrevVelocityY - 4f && vy < -3f)))
				{
					// отрыв от земли или резкий толчок вверх в воздухе (двойной прыжок)
					Jump(mp, style, mp.WasOnGround ? feet : p.Center + new Vector2(0f, p.height * 0.5f * g), g);
					mp.JumpCooldown = 10;
				}

				if (onGround && Math.Abs(p.velocity.X) > 0.8f)
				{
					mp.StepProgress += Math.Abs(p.velocity.X);
					if (mp.StepProgress >= StepDistance)
					{
						mp.StepProgress = 0f;
						mp.StepSide = -mp.StepSide;
						Step(mp, style, feet + new Vector2(mp.StepSide * 3f + p.velocity.X * 0.5f, -1f * g), g);
					}
				}
			}

			mp.WasOnGround = onGround;
			mp.PrevVelocityY = vy;
		}

		// ======================= шаг =======================

		private static void Step(LooksPlayer mp, StepStyle style, Vector2 pos, float g)
		{
			Palette pal = Pal(mp.Look);
			switch (style)
			{
				case StepStyle.Flowers:
				{
					// цветок вырастает на месте шага и лежит, пока не завянет; иногда вспархивает лепесток
					Shape shape = Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Lotus;
					ParticleSystem.Spawn(new ShapeParticle(shape, SpriteMotion.Stay, pos + new Vector2(0f, -2f * g), Vector2.Zero, new Ramp(pal.Hi, pal.A, pal.B), R(0.4f, 0.55f), Main.rand.Next(110, 160))
					{
						Rotation = R(-0.2f, 0.2f) + (g < 0f ? MathHelper.Pi : 0f),
						Spin = 0f,
						GlowStrength = 0.45f,
						GlowRamp = pal.B,
						Front = Main.rand.NextBool()
					});
					if (Main.rand.NextBool(2))
						ParticleSystem.Spawn(new ShapeParticle(Shape.Petal, SpriteMotion.Float, pos, new Vector2(R(-0.4f, 0.4f), -R(0.4f, 0.9f) * g), new Ramp(pal.Hi, pal.A), R(0.45f, 0.6f), 70)
						{ GlowStrength = 0.3f, GlowRamp = pal.B, Front = Main.rand.NextBool() });
					break;
				}
				case StepStyle.Frost:
					ParticleSystem.Spawn(new ShapeParticle(Shape.SnowCrystal, SpriteMotion.Stay, pos, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), R(0.55f, 0.75f), Main.rand.Next(90, 130))
					{
						Glowing = true,
						GlowRamp = pal.B,
						Stretch = new Vector2(1f, 0.4f),
						Rotation = 0f,
						Spin = 0f,
						Front = false
					});
					for (int i = 0; i < 2; i++)
						ParticleSystem.Spawn(new FlareParticle(pos + new Vector2(R(-6f, 6f), -R(0f, 6f) * g), new Vector2(0f, -0.2f * g), new Ramp(Color.White, pal.A), R(0.2f, 0.32f), 24) { Front = true });
					break;

				case StepStyle.Embers:
					ParticleSystem.Spawn(new GlowParticle(pos, Vector2.Zero, pal.Hot, R(0.28f, 0.38f), Main.rand.Next(50, 80)) { Drag = 0.9f, EndScale = 0.4f, Front = true });
					for (int i = 0; i < 3; i++)
						ParticleSystem.Spawn(new SparkParticle(pos, new Vector2(R(-1.6f, 1.6f), -R(1.5f, 3.2f) * g), pal.Hot, R(0.35f, 0.5f), Main.rand.Next(35, 55))
						{
							Gravity = new Vector2(0f, 0.16f * g),
							Drag = 0.98f,
							Bounce = true,
							Front = Main.rand.NextBool()
						});
					break;

				case StepStyle.Stardust:
					ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.Star5 : Shape.Star4, SpriteMotion.Stay, pos + new Vector2(0f, -2f * g), Vector2.Zero,
						new Ramp(Color.White, pal.A, pal.B), R(0.35f, 0.5f), Main.rand.Next(50, 80))
					{
						Glowing = true,
						GlowRamp = pal.B,
						Spin = R(-0.03f, 0.03f),
						Front = Main.rand.NextBool()
					});
					ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.3f, 0.3f), -R(0.5f, 1f) * g), pal.Glow, R(0.15f, 0.25f), 45) { Blink = 0.3f, Front = true });
					break;

				case StepStyle.Shadow:
					ParticleSystem.Spawn(new SmokeParticle(pos + new Vector2(0f, -3f * g), new Vector2(R(-0.6f, 0.6f), -R(0.1f, 0.4f) * g),
						new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.45f, 0.65f), Main.rand.Next(45, 60))
					{
						MaxOpacity = 0.5f,
						Grow = 1.5f,
						Front = Main.rand.NextBool()
					});
					ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(0f, -R(0.3f, 0.7f) * g), new Ramp(pal.A, pal.B), R(0.15f, 0.22f), 35) { Front = true });
					break;

				case StepStyle.Ripple:
					ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.Hi, pal.A, pal.B), 6f, R(30f, 38f), 30) { Squash = new Vector2(1f, 0.28f), Front = false, Opacity = 0.8f });
					break;
			}
		}

		// ======================= прыжок =======================

		private static void Jump(LooksPlayer mp, StepStyle style, Vector2 pos, float g)
		{
			Palette pal = Pal(mp.Look);
			ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.Hi, pal.A, pal.B), 8f, 46f, 22) { Squash = new Vector2(1f, 0.3f), Front = true, Opacity = 0.75f });

			for (int i = 0; i < 6; i++)
			{
				float side = i % 2 == 0 ? 1f : -1f;
				Vector2 vel = new Vector2(side * R(1f, 2.6f), -R(0.3f, 1.4f) * g);
				switch (style)
				{
					case StepStyle.Flowers:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Petal : Shape.PetalRound, SpriteMotion.Fall, pos, vel, new Ramp(pal.Hi, pal.A, pal.B), R(0.55f, 0.75f), 100)
						{ GlowStrength = 0.35f, GlowRamp = pal.B, Settle = true, Spin = R(-0.12f, 0.12f), Front = Main.rand.NextBool() });
						break;
					case StepStyle.Frost:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Snowflake : Shape.ShardB, SpriteMotion.Drift, pos, vel, new Ramp(Color.White, pal.A, pal.B), R(0.45f, 0.65f), 80)
						{ Glowing = true, GlowRamp = pal.B, Settle = true, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Embers:
						ParticleSystem.Spawn(new SparkParticle(pos, vel * 1.5f, pal.Hot, R(0.4f, 0.55f), 50) { Gravity = new Vector2(0f, 0.16f * g), Drag = 0.98f, Bounce = true, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Stardust:
						ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Spin, pos, vel, new Ramp(Color.White, pal.A, pal.B), R(0.35f, 0.5f), 40)
						{ Glowing = true, Spin = R(-0.15f, 0.15f), Front = Main.rand.NextBool() });
						break;
					case StepStyle.Shadow:
						ParticleSystem.Spawn(new SmokeParticle(pos, vel * 0.6f, new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.5f, 0.7f), 50)
						{ MaxOpacity = 0.5f, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Ripple:
						if (i < 2)
							ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.A, pal.B), 4f, 70f + i * 20f, 30 + i * 6) { Squash = new Vector2(1f, 0.28f), Front = false, Opacity = 0.6f });
						break;
				}
			}
		}

		// ======================= приземление =======================

		private static void Land(LooksPlayer mp, StepStyle style, Vector2 pos, float g, float power)
		{
			Palette pal = Pal(mp.Look);
			// волна по земле: эллипс + внутреннее кольцо
			ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.Hi, pal.A, pal.B), 16f, 90f + 90f * power, 30) { Squash = new Vector2(1f, 0.26f), Front = true });
			ParticleSystem.Spawn(new RingParticle(pos, new Ramp(Color.White, pal.A), 8f, 50f + 40f * power, 20) { Squash = new Vector2(1f, 0.26f), Front = false, Opacity = 0.8f });
			if (power > 0.5f)
			{
				ParticleSystem.Spawn(new FlareParticle(pos, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), 0.9f + 0.6f * power, 20) { Front = true });
				Lighting.AddLight(pos, pal.A.ToVector3() * (0.8f + power));
			}

			int count = 8 + (int)(8 * power);
			for (int i = 0; i < count; i++)
			{
				float side = i % 2 == 0 ? 1f : -1f;
				// разлёт вдоль земли
				Vector2 vel = new Vector2(side * R(2f, 5.5f) * (0.7f + 0.5f * power), -R(0.2f, 1.6f) * g);
				switch (style)
				{
					case StepStyle.Flowers:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Petal : Shape.PetalRound, SpriteMotion.Fall, pos, vel * 0.7f, new Ramp(pal.Hi, pal.A, pal.B), R(0.55f, 0.8f), 110)
						{ GlowStrength = 0.35f, GlowRamp = pal.B, Settle = true, Spin = R(-0.15f, 0.15f), Front = Main.rand.NextBool() });
						break;
					case StepStyle.Frost:
						ParticleSystem.Spawn(new ShapeParticle(Shape.ShardA, SpriteMotion.Spin, pos, vel, new Ramp(Color.White, pal.A, pal.B), R(0.4f, 0.6f), 30)
						{ Glowing = true, GlowRamp = pal.B, Rotation = vel.ToRotation() + MathHelper.PiOver2, Spin = 0f, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Embers:
						ParticleSystem.Spawn(new SparkParticle(pos, vel + new Vector2(0f, -1.5f * g), pal.Hot, R(0.45f, 0.6f), 55) { Gravity = new Vector2(0f, 0.18f * g), Drag = 0.98f, Bounce = true, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Stardust:
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Star4 : Shape.Star5, SpriteMotion.Spin, pos, vel, new Ramp(Color.White, pal.A, pal.B), R(0.4f, 0.6f), 45)
						{ Glowing = true, GlowRamp = pal.B, Spin = R(-0.2f, 0.2f), Front = Main.rand.NextBool() });
						break;
					case StepStyle.Shadow:
						ParticleSystem.Spawn(new SmokeParticle(pos + new Vector2(0f, -4f * g), vel * 0.5f, new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.6f, 0.9f), 55)
						{ MaxOpacity = 0.55f, Front = Main.rand.NextBool() });
						break;
					case StepStyle.Ripple:
						if (i < 3)
							ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.A, pal.B), 6f, 60f + i * 35f + 60f * power, 28 + i * 8) { Squash = new Vector2(1f, 0.26f), Front = false, Opacity = 0.7f });
						break;
				}
			}

			// у цветов и мороза при сильном ударе на земле вокруг вырастают цветы / ледяные шипы
			if (power > 0.25f && (style == StepStyle.Flowers || style == StepStyle.Frost))
			{
				int n = 3 + (int)(4 * power);
				for (int i = 0; i < n; i++)
				{
					float x = (i - (n - 1) / 2f) * R(7f, 10f);
					Vector2 at = pos + new Vector2(x, -2f * g);
					if (style == StepStyle.Flowers)
						ParticleSystem.Spawn(new ShapeParticle(Shape.Lotus, SpriteMotion.Stay, at, Vector2.Zero, new Ramp(pal.Hi, pal.A, pal.B), R(0.45f, 0.65f), Main.rand.Next(120, 170))
						{ GlowStrength = 0.5f, GlowRamp = pal.B, Rotation = g < 0f ? MathHelper.Pi : 0f, Spin = 0f, Front = Main.rand.NextBool() });
					else
						ParticleSystem.Spawn(new ShapeParticle(Shape.ShardA, SpriteMotion.Stay, at + new Vector2(0f, -6f * g), Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), R(0.5f, 0.8f) * (1f - Math.Abs(x) / 60f), Main.rand.Next(70, 100))
						{ Glowing = true, GlowRamp = pal.B, Rotation = x * 0.02f + (g < 0f ? MathHelper.Pi : 0f), Spin = 0f, Front = Main.rand.NextBool() });
				}
			}
		}
	}
}
