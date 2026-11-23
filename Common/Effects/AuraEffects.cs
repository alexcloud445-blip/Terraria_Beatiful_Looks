using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Ауры на собственной системе частиц. Каждый стиль — отдельный метод.
	/// d — плотность (сколько частиц), s — размер, v — скорость; c1/c2 — основной и второй цвет.
	/// </summary>
	public static class AuraEffects
	{
		private struct Ctx
		{
			public Player P;
			public LookSettings L;
			public float D, S, V;
			public ColorSetting C1, C2;

			/// <summary>Случайный цвет между основным и вторым.</summary>
			public Color Mix => C1.Mix(C2, Main.rand.NextFloat(), Main.rand.NextFloat());

			/// <summary>«Вверх» с учётом перевёрнутой гравитации.</summary>
			public float Up => -P.gravDir;
		}

		public static void Update(Player player, LookSettings look)
		{
			if (look.Aura == AuraStyle.None)
				return;

			var c = new Ctx
			{
				P = player,
				L = look,
				D = look.AuraDensity * Fx.GlobalIntensity,
				S = look.AuraSize,
				V = look.AuraSpeed,
				C1 = look.AuraColor,
				C2 = look.AuraColor2
			};

			Lighting.AddLight(player.Center, look.AuraColor.Get().ToVector3() * 0.5f * Fx.GlobalIntensity);

			switch (look.Aura)
			{
				case AuraStyle.Glow: Glow(c); break;
				case AuraStyle.Sparkles: Sparkles(c); break;
				case AuraStyle.Comets: Comets(c); break;
				case AuraStyle.Flames: Flames(c); break;
				case AuraStyle.Starfall: Starfall(c); break;
				case AuraStyle.Frost: Frost(c); break;
				case AuraStyle.Sakura: Sakura(c); break;
				case AuraStyle.Hearts: Hearts(c); break;
				case AuraStyle.Vortex: Vortex(c); break;
				case AuraStyle.CelestialRing: CelestialRing(c); break;
				case AuraStyle.Storm: Storm(c); break;
				case AuraStyle.Void: Void(c); break;
				case AuraStyle.Divine: Divine(c); break;
				case AuraStyle.Prism: Prism(c); break;
				case AuraStyle.Butterflies: Butterflies(c); break;
				case AuraStyle.Runes: Runes(c); break;
				case AuraStyle.Fireflies: Fireflies(c); break;
			}
		}

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		private static bool Coin => Main.rand.NextBool();

		/// <summary>Случайная точка вокруг тела (эллипс по размеру игрока).</summary>
		private static Vector2 AroundBody(Ctx c, float extra = 1f) =>
			c.P.Center + Main.rand.NextVector2Circular(c.P.width * 0.9f * c.S * extra, c.P.height * 0.6f * c.S * extra);

		private static Vector2 Feet(Ctx c) => c.P.Center + new Vector2(0f, c.P.height / 2f * c.P.gravDir);

		// ======================= стили =======================

		private static void Glow(Ctx c)
		{
			for (int i = Fx.Rolls(0.4f * c.D); i > 0; i--)
			{
				Vector2 pos = Feet(c) + new Vector2(R(-14f, 14f) * c.S, -R(0f, 12f) * c.P.gravDir);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.2f, 0.2f), R(0.5f, 1.3f) * c.Up * c.V), c.Mix, R(0.45f, 0.85f) * c.S, Main.rand.Next(50, 80))
				{
					Drag = 0.985f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.04f * c.D); i > 0; i--)
			{
				ParticleSystem.Spawn(new GlowParticle(c.P.Center + Main.rand.NextVector2Circular(8f, 8f), new Vector2(0f, 0.3f * c.Up), c.C1.Get(), 1.8f * c.S, 60)
				{
					Core = false,
					EndScale = 1.2f,
					Front = false,
					Opacity = 0.5f
				});
			}
		}

		private static void Sparkles(Ctx c)
		{
			for (int i = Fx.Rolls(0.35f * c.D); i > 0; i--)
			{
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.3f), Main.rand.NextVector2Circular(0.4f, 0.4f) * c.V, c.Mix, R(0.45f, 0.9f) * c.S, Main.rand.Next(22, 40))
				{
					Spin = R(-0.03f, 0.03f),
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.3f * c.D); i > 0; i--)
			{
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.2f), new Vector2(0f, R(0.2f, 0.6f) * c.Up * c.V), c.Mix, R(0.25f, 0.45f) * c.S, Main.rand.Next(30, 50))
				{
					Front = Coin
				});
			}
		}

		/// <summary>Добирает орбитальные огоньки до нужного количества.</summary>
		private static void EnsureOrbiters(Ctx c, int count, Func<int, OrbitParticle> create)
		{
			int have = ParticleSystem.OrbitCounts[c.P.whoAmI];
			for (int i = have; i < count; i++)
			{
				ParticleSystem.Spawn(create(i));
				ParticleSystem.OrbitCounts[c.P.whoAmI]++;
			}
		}

		/// <summary>Огонёк живёт, пока у владельца выбран этот стиль ауры и эффекты видны.</summary>
		private static Func<Player, bool> AliveWhile(AuraStyle style) =>
			p => p.TryGetModPlayer(out LooksPlayer mp) && mp.EffectsVisible && mp.Look.Aura == style;

		private static void Comets(Ctx c)
		{
			int count = c.D < 0.6f ? 2 : c.D < 1.4f ? 3 : 4;
			EnsureOrbiters(c, count, i => new OrbitParticle(c.P.whoAmI, MathHelper.TwoPi * i / count + R(-0.3f, 0.3f), 20)
			{
				RadiusX = R(38f, 46f),
				RadiusY = R(12f, 18f),
				Tilt = R(-0.35f, 0.35f),
				AngularSpeed = R(0.05f, 0.07f),
				Ribbon = RibbonStyle.Soft,
				TrailWidth = 9f,
				Alive = AliveWhile(AuraStyle.Comets),
				Refresh = (o, p) =>
				{
					LookSettings l = p.GetModPlayer<LooksPlayer>().Look;
					o.SizeMul = l.AuraSize;
					o.SpeedMul = l.AuraSpeed;
					o.Scale = l.AuraSize;
					o.Color = l.AuraColor.Get(o.Angle * 0.05f);
					o.TrailColor = o.Color;
					o.TrailColor2 = l.AuraColor2.Get(0.3f);
					if (Main.rand.NextBool(4))
						ParticleSystem.Spawn(new GlowParticle(o.Position, Main.rand.NextVector2Circular(0.5f, 0.5f), o.TrailColor2, 0.3f * o.Scale, 25) { Front = o.Front });
				}
			});
		}

		private static void Flames(Ctx c)
		{
			Color hot = Color.Lerp(Color.White, c.C2.Get(), 0.6f);
			for (int i = Fx.Rolls(0.7f * c.D); i > 0; i--)
			{
				Vector2 pos = Feet(c) + new Vector2(R(-12f, 12f) * c.S, -R(0f, 6f) * c.P.gravDir);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.3f, 0.3f), R(1.2f, 2.4f) * c.Up * c.V), c.C1.Get(R(0f, 1f)), R(0.45f, 0.7f) * c.S, Main.rand.Next(25, 40))
				{
					Glowing = true,
					Grow = 0.35f,
					MaxOpacity = 0.8f,
					Front = Main.rand.NextBool(3)
				});
			}

			for (int i = Fx.Rolls(0.5f * c.D); i > 0; i--)
			{
				Vector2 pos = Feet(c) + new Vector2(R(-14f, 14f) * c.S, -R(0f, 20f) * c.P.gravDir);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.6f, 0.6f), R(1.5f, 3f) * c.Up * c.V), hot, R(0.3f, 0.55f) * c.S, Main.rand.Next(35, 60))
				{
					EndColor = c.C1.Get(),
					Gravity = new Vector2(0f, 0.02f * c.Up),
					Wander = 0.08f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.06f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Center + new Vector2(R(-10f, 10f), -c.P.height * 0.6f * c.P.gravDir);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.2f, 0.2f), 1.2f * c.Up), new Color(40, 34, 34), R(0.7f, 1f) * c.S, 70)
				{
					MaxOpacity = 0.35f,
					Front = false
				});
			}
		}

		private static void Starfall(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Vector2 start = c.P.Center + new Vector2(R(-90f, 60f), -R(100f, 150f) * c.P.gravDir) * c.S;
				Vector2 vel = new Vector2(1.3f, 3.4f * c.P.gravDir) * c.V * R(0.85f, 1.15f);
				ParticleSystem.Spawn(new ShootingStarParticle(start, vel, c.C1.Get(R(0f, 1f)), c.C2.Get(), R(0.8f, 1.1f) * c.S, Main.rand.Next(38, 50))
				{
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.15f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.8f), Vector2.Zero, c.Mix, R(0.3f, 0.6f) * c.S, Main.rand.Next(25, 45)) { Front = Coin });
		}

		private static void Frost(Ctx c)
		{
			for (int i = Fx.Rolls(0.22f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Top + new Vector2(R(-40f, 40f), -R(6f, 24f) * c.P.gravDir) * c.S;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Snowflake, SpriteMotion.Drift, pos, new Vector2(0f, 0.3f * c.P.gravDir * c.V), c.Mix, R(0.6f, 1f) * c.S, Main.rand.Next(90, 140))
				{
					Glowing = true,
					GlowStrength = 0.3f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
			{
				Vector2 pos = Feet(c) + new Vector2(R(-20f, 20f) * c.S, -4f * c.P.gravDir);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.6f, 0.6f), -0.15f * c.P.gravDir), c.C2.Get(), R(0.8f, 1.1f) * c.S, 80)
				{
					Glowing = true,
					MaxOpacity = 0.22f,
					Grow = 2.2f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.4f), Vector2.Zero, Color.Lerp(c.C1.Get(), Color.White, 0.5f), R(0.3f, 0.55f) * c.S, 30) { Front = Coin });

			for (int i = Fx.Rolls(0.06f * c.D); i > 0; i--)
			{
				Vector2 dir = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new SparkParticle(c.P.Center + dir * 10f * c.S, dir * R(1.5f, 2.5f) * c.V, c.C1.Get(), 0.7f * c.S, 20) { Front = Coin });
			}
		}

		private static void Sakura(Ctx c)
		{
			for (int i = Fx.Rolls(0.28f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Top + new Vector2(R(-50f, 50f), -R(6f, 28f) * c.P.gravDir) * c.S;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Petal, SpriteMotion.Fall, pos, new Vector2(R(-0.5f, 0.5f), 0.3f * c.P.gravDir * c.V), c.Mix, R(0.7f, 1.05f) * c.S, Main.rand.Next(110, 160))
				{
					GlowStrength = 0.2f,
					Front = Coin
				});
			}

			// лепестки, подхваченные ветром вокруг тела
			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
			{
				float angle = R(0f, MathHelper.TwoPi);
				Vector2 offset = VfxMath.OnEllipse(angle, 34f, 14f) * c.S;
				Vector2 vel = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle) * 0.4f) * 1.6f * c.V;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Petal, SpriteMotion.Spin, c.P.Center + offset, vel, c.Mix, R(0.6f, 0.9f) * c.S, 50)
				{
					GlowStrength = 0.2f,
					Spin = R(-0.12f, 0.12f),
					Front = Math.Sin(angle) > 0
				});
			}

			for (int i = Fx.Rolls(0.15f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.3f), new Vector2(0f, 0.3f * c.Up), c.C2.Get(), R(0.25f, 0.4f) * c.S, 40) { Front = Coin });
		}

		private static void Hearts(Ctx c)
		{
			for (int i = Fx.Rolls(0.09f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Center + new Vector2(R(-22f, 22f), R(-8f, 18f)) * c.S;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Heart, SpriteMotion.Float, pos, new Vector2(0f, R(0.6f, 1.1f) * c.Up * c.V), c.Mix, R(0.8f, 1.25f) * c.S, Main.rand.Next(70, 100))
				{
					GlowStrength = 0.45f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.14f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.3f), new Vector2(0f, -0.3f * c.P.gravDir), c.Mix, R(0.3f, 0.55f) * c.S, 30) { Front = Coin });
		}

		private static void Vortex(Ctx c)
		{
			for (int i = Fx.Rolls(0.9f * c.D); i > 0; i--)
			{
				float angle = R(0f, MathHelper.TwoPi);
				Vector2 offset = angle.ToRotationVector2() * R(36f, 50f) * c.S;
				Vector2 vel = (-offset).SafeNormalize(Vector2.Zero).RotatedBy(1.15f) * 2.4f * c.V;
				ParticleSystem.Spawn(new SparkParticle(c.P.Center + offset, vel, c.C1.Mix(c.C2, R(0f, 1f), angle / MathHelper.TwoPi), 0.7f * c.S, 22)
				{
					Drag = 0.99f,
					Stretch = 2.5f,
					Front = offset.Y * c.P.gravDir > 0f
				});
			}

			if (Main.GameUpdateCount % (uint)Math.Max(8, (int)(30 / c.V)) == 0)
				ParticleSystem.Spawn(new RingParticle(c.P.Center, c.C2.Get(), 110f * c.S, 20f * c.S, 30) { Front = false, Opacity = 0.6f });
		}

		private static void CelestialRing(Ctx c)
		{
			const int runes = 8;
			EnsureOrbiters(c, runes, i => new OrbitParticle(c.P.whoAmI, MathHelper.TwoPi * i / runes, 8)
			{
				RadiusX = 44f,
				RadiusY = 11f,
				Bob = 0f,
				AngularSpeed = 0.02f,
				RuneHead = i % 8,
				Ribbon = RibbonStyle.None,
				Alive = AliveWhile(AuraStyle.CelestialRing),
				Refresh = (o, p) =>
				{
					LookSettings l = p.GetModPlayer<LooksPlayer>().Look;
					o.SizeMul = l.AuraSize;
					o.SpeedMul = l.AuraSpeed;
					o.Scale = l.AuraSize;
					o.Color = l.AuraColor.Mix(l.AuraColor2, 0.5f + 0.5f * (float)Math.Sin(o.Angle), o.Angle * 0.1f);
				}
			});

			for (int i = Fx.Rolls(0.25f * c.D); i > 0; i--)
			{
				float angle = R(0f, MathHelper.TwoPi);
				Vector2 pos = c.P.Center + VfxMath.OnEllipse(angle, 44f, 11f) * c.S;
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(0f, R(0.4f, 0.9f) * c.Up * c.V), c.Mix, R(0.25f, 0.4f) * c.S, 40)
				{
					Front = Math.Sin(angle) > 0
				});
			}
		}

		private static void Storm(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Vector2 start = c.P.Center + Main.rand.NextVector2Circular(24f, 30f) * c.S;
				Vector2 end = start + Main.rand.NextVector2Unit() * R(36f, 64f) * c.S;
				ParticleSystem.Spawn(new LightningParticle(start, end, c.C1.Get(R(0f, 1f)), 0.7f, Main.rand.Next(10, 16)) { Front = Coin, CoreColor = c.C2.Get() });
			}

			for (int i = Fx.Rolls(0.25f * c.D); i > 0; i--)
			{
				Vector2 dir = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new SparkParticle(c.P.Center + dir * R(6f, 20f) * c.S, dir * R(3f, 6f) * c.V, c.Mix, 0.6f * c.S, 12)
				{
					Drag = 0.88f,
					Front = Coin
				});
			}

			// редкий удар молнии сверху
			if (Main.rand.NextFloat() < 0.006f * c.D)
			{
				Vector2 end = c.P.Bottom;
				Vector2 start = end + new Vector2(R(-40f, 40f), -R(260f, 340f) * c.P.gravDir);
				ParticleSystem.Spawn(new LightningParticle(start, end, c.C1.Get(), 1.4f, 18, 0.18f) { Front = false, CoreColor = Color.White });
				ParticleSystem.Spawn(new RingParticle(end, c.C2.Get(), 20f, 120f * c.S, 28) { Squash = new Vector2(1f, 0.3f), Front = true });
				Lighting.AddLight(end, c.C1.Get().ToVector3() * 2f);
			}
		}

		private static void Void(Ctx c)
		{
			Color dark = Color.Lerp(Color.Black, c.C1.Get(), 0.2f);
			for (int i = Fx.Rolls(0.22f * c.D); i > 0; i--)
			{
				ParticleSystem.Spawn(new SmokeParticle(AroundBody(c, 1.2f), new Vector2(R(-0.3f, 0.3f), 0.5f * c.Up * c.V), dark, R(0.7f, 1f) * c.S, Main.rand.Next(50, 70))
				{
					MaxOpacity = 0.5f,
					Grow = 1.6f,
					Front = Main.rand.NextBool(3)
				});
			}

			for (int i = Fx.Rolls(0.45f * c.D); i > 0; i--)
			{
				float dist = R(45f, 70f) * c.S;
				Vector2 dir = Main.rand.NextVector2Unit();
				float speed = 2.2f * c.V;
				int life = Math.Max(8, (int)(dist / speed) - 2);
				ParticleSystem.Spawn(new GlowParticle(c.P.Center + dir * dist, -dir * speed, c.Mix, R(0.3f, 0.5f) * c.S, life)
				{
					Drag = 1f,
					EndScale = 0.1f,
					Front = dir.Y * c.P.gravDir > 0f
				});
			}

			if (Main.GameUpdateCount % (uint)Math.Max(10, (int)(45 / c.V)) == 0)
				ParticleSystem.Spawn(new RingParticle(c.P.Center, c.C2.Get(), 150f * c.S, 16f * c.S, 40) { Front = false, Opacity = 0.7f });
		}

		private static void Divine(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Top + new Vector2(R(-46f, 46f), -R(20f, 50f) * c.P.gravDir) * c.S;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Feather, SpriteMotion.Sway, pos, new Vector2(0f, 0.2f * c.P.gravDir), Color.Lerp(c.C2.Get(), Color.White, 0.4f), R(0.8f, 1.1f) * c.S, Main.rand.Next(130, 180))
				{
					GlowStrength = 0.3f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.35f * c.D); i > 0; i--)
			{
				Vector2 pos = Feet(c) + new Vector2(R(-22f, 22f) * c.S, -R(0f, 30f) * c.P.gravDir);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(0f, R(0.6f, 1.4f) * c.Up * c.V), c.Mix, R(0.25f, 0.5f) * c.S, Main.rand.Next(45, 70))
				{
					Drag = 0.99f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.5f), Vector2.Zero, Color.Lerp(c.C1.Get(), Color.White, 0.4f), R(0.5f, 0.8f) * c.S, 35) { Front = Coin });
		}

		private static void Prism(Ctx c)
		{
			const int shards = 5;
			EnsureOrbiters(c, shards, i => new OrbitParticle(c.P.whoAmI, MathHelper.TwoPi * i / shards, 22)
			{
				RadiusX = R(40f, 50f),
				RadiusY = R(14f, 22f),
				Tilt = R(-0.5f, 0.5f),
				AngularSpeed = R(0.045f, 0.06f),
				ShardHead = true,
				Ribbon = RibbonStyle.Rainbow,
				TrailWidth = 8f,
				Alive = AliveWhile(AuraStyle.Prism),
				Refresh = (o, p) =>
				{
					LookSettings l = p.GetModPlayer<LooksPlayer>().Look;
					o.SizeMul = l.AuraSize;
					o.SpeedMul = l.AuraSpeed;
					o.Scale = l.AuraSize;
					float hue = VfxMath.Frac(Main.GlobalTimeWrappedHourly * 0.3f + o.Angle / MathHelper.TwoPi);
					Color rainbow = Main.hslToRgb(hue, 1f, 0.7f);
					o.Color = Color.Lerp(rainbow, l.AuraColor.Get(), 0.3f);
					o.TrailColor = Color.White;
					o.TrailColor2 = rainbow;
				}
			});

			for (int i = Fx.Rolls(0.2f * c.D); i > 0; i--)
			{
				Color rainbow = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.75f);
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.5f), Vector2.Zero, rainbow, R(0.3f, 0.6f) * c.S, 30) { Front = Coin });
			}
		}

		private static void Butterflies(Ctx c)
		{
			for (int i = Fx.Rolls(0.025f * c.D); i > 0; i--)
			{
				Vector2 home = new Vector2(R(-46f, 46f), R(-40f, 10f) * c.P.gravDir) * c.S;
				ParticleSystem.Spawn(new SpriteParticle(SpriteKind.Butterfly, SpriteMotion.Flutter, c.P.Center + home + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(1f, 1f), c.Mix, R(0.8f, 1.1f) * c.S, Main.rand.Next(240, 360))
				{
					Owner = c.P.whoAmI,
					HomeOffset = home,
					GlowStrength = 0.35f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.6f), Main.rand.NextVector2Circular(0.3f, 0.3f), c.C2.Get(), R(0.2f, 0.35f) * c.S, 50) { Front = Coin, Blink = 0.2f });
		}

		private static void Runes(Ctx c)
		{
			for (int i = Fx.Rolls(0.07f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Center + new Vector2(R(-30f, 30f), R(-6f, 26f) * c.P.gravDir) * c.S;
				ParticleSystem.Spawn(new RuneParticle(pos, new Vector2(0f, 0.6f * c.Up * c.V), c.Mix, R(0.8f, 1.1f) * c.S, Main.rand.Next(60, 80)) { Front = Coin });
			}

			for (int i = Fx.Rolls(0.25f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.3f), new Vector2(0f, 0.5f * c.Up * c.V), c.Mix, R(0.2f, 0.35f) * c.S, 40) { Front = Coin });

			if (Main.GameUpdateCount % (uint)Math.Max(15, (int)(70 / c.V)) == 0)
				ParticleSystem.Spawn(new RingParticle(Feet(c), c.C1.Get(), 20f, 110f * c.S, 40) { Squash = new Vector2(1f, 0.3f), Front = false });
		}

		private static void Fireflies(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Vector2 pos = c.P.Center + Main.rand.NextVector2Circular(70f, 50f) * c.S;
				ParticleSystem.Spawn(new GlowParticle(pos, Main.rand.NextVector2Circular(0.5f, 0.5f) * c.V, c.Mix, R(0.45f, 0.7f) * c.S, Main.rand.Next(180, 260))
				{
					Blink = 0.12f,
					Wander = 0.05f * c.V,
					Drag = 0.96f,
					EndScale = 0.8f,
					Front = Coin
				});
			}
		}
	}
}
