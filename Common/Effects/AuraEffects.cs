using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Ауры на собственной системе частиц. Каждый стиль — отдельный метод из нескольких «слоёв»:
	/// основа (то, что задаёт характер), акценты (блики, искры) и редкие события (вспышки, волны).
	/// Цвета берутся из палитры (блик → основной → второй → тёмный) и меняются за жизнь частицы.
	/// </summary>
	public static partial class AuraEffects
	{
		private struct Ctx
		{
			public Player P;
			public LookSettings L;
			public float D, S, V;

			/// <summary>Палитра; в режиме радуги каждая частица получает свой оттенок.</summary>
			public Palette Pal => Palette.Of(L.AuraColor, L.AuraColor2, Main.rand.NextFloat());

			public float Up => -P.gravDir;
			public float G => P.gravDir;
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
				V = look.AuraSpeed
			};

			Lighting.AddLight(player.Center, look.AuraColor.Get().ToVector3() * 0.45f * Fx.GlobalIntensity);

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
				case AuraStyle.Astrolabe: Astrolabe(c); break;
				case AuraStyle.Aurora: Aurora(c); break;
				case AuraStyle.Galaxy: Galaxy(c); break;
				case AuraStyle.Eclipse: Eclipse(c); break;
				case AuraStyle.Mandala: Mandala(c); break;
			}
		}

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		private static bool Coin => Main.rand.NextBool();

		/// <summary>Случайная точка вокруг тела (эллипс по размеру игрока).</summary>
		private static Vector2 AroundBody(Ctx c, float extra = 1f) =>
			c.P.Center + Main.rand.NextVector2Circular(c.P.width * 0.9f * c.S * extra, c.P.height * 0.6f * c.S * extra);

		private static Vector2 Feet(Ctx c) => c.P.Center + new Vector2(0f, c.P.height / 2f * c.G);

		private static int Every(float ticks, float speed) => Math.Max(4, (int)(ticks / Math.Max(0.2f, speed)));

		private static bool Tick(int period) => Main.GameUpdateCount % (uint)period == 0;

		/// <summary>Форма, которая кружит вокруг игрока по спирали.</summary>
		private static ShapeParticle Orbiter(Ctx c, Shape shape, Ramp ramp, float radius, float radiusSpeed, float speed, float scale, int life)
		{
			float angle = R(0f, MathHelper.TwoPi);
			return new ShapeParticle(shape, SpriteMotion.Orbit, c.P.Center, Vector2.Zero, ramp, scale, life)
			{
				Owner = c.P.whoAmI,
				OrbitAngle = angle,
				OrbitRadius = radius,
				RadiusSpeed = radiusSpeed,
				OrbitSpeed = speed,
				Spin = R(-0.08f, 0.08f)
			};
		}

		// ======================= стили =======================

		/// <summary>Сияние: тёплые огоньки поднимаются от ног, редкие блики-звёздочки.</summary>
		private static void Glow(Ctx c)
		{
			for (int i = Fx.Rolls(0.35f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = Feet(c) + new Vector2(R(-14f, 14f) * c.S, -R(0f, 12f) * c.G);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.15f, 0.15f), R(0.5f, 1.2f) * c.Up * c.V), pal.Glow, R(0.35f, 0.7f) * c.S, Main.rand.Next(55, 85))
				{
					Drag = 0.985f,
					Wander = 0.02f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.04f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.2f), new Vector2(0f, 0.2f * c.Up), new Ramp(Color.White, c.Pal.A), R(0.35f, 0.55f) * c.S, 34) { Front = Coin });
		}

		/// <summary>Искры: мерцающие четырёхлучевые звёздочки и звёздные формы вокруг тела.</summary>
		private static void Sparkles(Ctx c)
		{
			for (int i = Fx.Rolls(0.22f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.35f), Main.rand.NextVector2Circular(0.3f, 0.3f) * c.V, new Ramp(Color.White, pal.A, pal.B), R(0.4f, 0.8f) * c.S, Main.rand.Next(24, 40))
				{
					Spin = R(-0.03f, 0.03f),
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.1f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.Star8 : Shape.Star4, SpriteMotion.Spin, AroundBody(c, 1.4f), new Vector2(0f, 0.3f * c.Up), new Ramp(pal.Hi, pal.A, pal.B), R(0.35f, 0.6f) * c.S, 40)
				{
					Glowing = true,
					Spin = R(-0.06f, 0.06f),
					GlowRamp = pal.B,
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

		/// <summary>Кометы: огоньки с градиентными хвостами кружат по наклонным орбитам.</summary>
		private static void Comets(Ctx c)
		{
			int count = c.D < 0.6f ? 2 : c.D < 1.4f ? 3 : 4;
			EnsureOrbiters(c, count, i => new OrbitParticle(c.P.whoAmI, MathHelper.TwoPi * i / count + R(-0.3f, 0.3f), 22)
			{
				RadiusX = R(38f, 46f),
				RadiusY = R(12f, 18f),
				Tilt = R(-0.4f, 0.4f),
				AngularSpeed = R(0.05f, 0.07f),
				Ribbon = RibbonStyle.Soft,
				TrailWidth = 8f,
				Alive = AliveWhile(AuraStyle.Comets),
				Refresh = (o, p) =>
				{
					LookSettings l = p.GetModPlayer<LooksPlayer>().Look;
					Palette pal = Palette.Of(l.AuraColor, l.AuraColor2, o.Angle * 0.03f);
					o.SizeMul = l.AuraSize;
					o.SpeedMul = l.AuraSpeed;
					o.Scale = l.AuraSize;
					o.Color = pal.A;
					o.Color2 = pal.B;
					o.TrailColor = pal.Hi;
					o.TrailColor2 = pal.B;
					if (Main.rand.NextBool(3))
						ParticleSystem.Spawn(new GlowParticle(o.Position, Main.rand.NextVector2Circular(0.4f, 0.4f), new Ramp(pal.A, pal.B, pal.Lo), 0.25f * o.Scale, 28) { Front = o.Front });
				}
			});
		}

		/// <summary>Пламя: языки огня от белого к основному, второму и тёмному цвету; угольки, капли огня, дым.</summary>
		private static void Flames(Ctx c)
		{
			for (int i = Fx.Rolls(0.6f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = Feet(c) + new Vector2(R(-12f, 12f) * c.S, -R(0f, 6f) * c.G);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.3f, 0.3f), R(1.2f, 2.4f) * c.Up * c.V), pal.Hot, R(0.45f, 0.7f) * c.S, Main.rand.Next(26, 40))
				{
					Glowing = true,
					Grow = 0.3f,
					MaxOpacity = 0.75f,
					Front = Main.rand.NextBool(3)
				});
			}

			for (int i = Fx.Rolls(0.4f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = Feet(c) + new Vector2(R(-14f, 14f) * c.S, -R(0f, 20f) * c.G);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(R(-0.6f, 0.6f), R(1.5f, 3f) * c.Up * c.V), pal.Hot, R(0.25f, 0.45f) * c.S, Main.rand.Next(35, 60))
				{
					Gravity = new Vector2(0f, 0.02f * c.Up),
					Wander = 0.08f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.06f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Teardrop, SpriteMotion.Float, Feet(c) + new Vector2(R(-12f, 12f) * c.S, -R(4f, 16f) * c.G),
					new Vector2(0f, R(1f, 1.8f) * c.Up * c.V), pal.Hot, R(0.5f, 0.8f) * c.S, 40)
				{
					Glowing = true,
					GlowRamp = new Ramp(pal.A, pal.B),
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + new Vector2(R(-10f, 10f), -c.P.height * 0.6f * c.G);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.2f, 0.2f), 1.1f * c.Up), new Ramp(VfxMath.Shade(pal.Lo, 0.6f), new Color(20, 16, 18)), R(0.7f, 1f) * c.S, 70)
				{
					MaxOpacity = 0.35f,
					Front = false
				});
			}
		}

		/// <summary>Звездопад: падающие звёзды с хвостами, мерцание, редкий полумесяц.</summary>
		private static void Starfall(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 start = c.P.Center + new Vector2(R(-90f, 60f), -R(100f, 150f) * c.G) * c.S;
				Vector2 vel = new Vector2(1.3f, 3.4f * c.G) * c.V * R(0.85f, 1.15f);
				ParticleSystem.Spawn(new ShootingStarParticle(start, vel, pal.A, pal.B, R(0.8f, 1.1f) * c.S, Main.rand.Next(38, 50)) { Front = Coin });
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.9f), Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), R(0.3f, 0.6f) * c.S, Main.rand.Next(25, 45)) { Front = Coin });
			}

			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Star5, SpriteMotion.Spin, AroundBody(c, 1.6f), new Vector2(0f, 0.4f * c.G), new Ramp(pal.Hi, pal.A), R(0.4f, 0.6f) * c.S, 50)
				{
					Glowing = true,
					GlowRamp = pal.B,
					Spin = R(-0.05f, 0.05f),
					Front = Coin
				});
			}

			if (Main.rand.NextFloat() < 0.004f * c.D)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Crescent, SpriteMotion.Float, c.P.Center + new Vector2(R(-30f, 30f), -40f * c.G) * c.S, new Vector2(0f, 0.3f * c.Up),
					new Ramp(pal.Hi, pal.A), 1.2f * c.S, 120) { GlowRamp = pal.B, GlowStrength = 0.8f, Front = false });
			}
		}

		/// <summary>Мороз: снежинки двух видов, ледяные осколки, морозный туман и блики.</summary>
		private static void Frost(Ctx c)
		{
			for (int i = Fx.Rolls(0.2f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Top + new Vector2(R(-40f, 40f), -R(6f, 24f) * c.G) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.SnowCrystal : Shape.Snowflake, SpriteMotion.Drift, pos,
					new Vector2(0f, 0.3f * c.G * c.V), new Ramp(Color.White, pal.A, pal.B), R(0.55f, 0.95f) * c.S, Main.rand.Next(90, 140))
				{
					Glowing = true,
					GlowRamp = pal.B,
					GlowStrength = 0.45f,
					Settle = true,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.07f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = Feet(c) + new Vector2(R(-20f, 20f) * c.S, -4f * c.G);
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.6f, 0.6f), -0.15f * c.G), new Ramp(pal.B, pal.A), R(0.8f, 1.1f) * c.S, 80)
				{
					Glowing = true,
					MaxOpacity = 0.18f,
					Grow = 2.2f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.1f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.4f), Vector2.Zero, new Ramp(Color.White, c.Pal.A), R(0.3f, 0.5f) * c.S, 30) { Front = Coin });

			if (Tick(Every(70f, c.V)))
			{
				// всплеск ледяных осколков
				Palette pal = c.Pal;
				for (int k = 0; k < 6; k++)
				{
					Vector2 dir = (k * MathHelper.TwoPi / 6f + R(-0.2f, 0.2f)).ToRotationVector2();
					ParticleSystem.Spawn(new ShapeParticle(Shape.ShardA, SpriteMotion.Spin, c.P.Center + dir * 8f, dir * R(1.6f, 2.4f) * c.V, new Ramp(Color.White, pal.A, pal.B), 0.55f * c.S, 32)
					{
						Glowing = true,
						Rotation = dir.ToRotation() + MathHelper.PiOver2,
						Spin = 0f,
						Front = dir.Y * c.G > 0
					});
				}
			}
		}

		/// <summary>Сакура: два вида лепестков, вихрь лепестков вокруг тела, иногда цветок лотоса.</summary>
		private static void Sakura(Ctx c)
		{
			for (int i = Fx.Rolls(0.24f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Top + new Vector2(R(-50f, 50f), -R(6f, 28f) * c.G) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Petal, SpriteMotion.Fall, pos,
					new Vector2(R(-0.5f, 0.5f), 0.3f * c.G * c.V), new Ramp(pal.Hi, pal.A, pal.B), R(0.7f, 1.05f) * c.S, Main.rand.Next(110, 160))
				{
					GlowStrength = 0.35f,
					GlowRamp = pal.B,
					Settle = true,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.1f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(Orbiter(c, Shape.Petal, new Ramp(pal.Hi, pal.A), R(26f, 40f) * c.S, 0.35f, 0.06f * c.V, R(0.55f, 0.8f) * c.S, 60));
			}

			if (Main.rand.NextFloat() < 0.006f * c.D)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Lotus, SpriteMotion.Float, Feet(c) + new Vector2(R(-20f, 20f), -10f * c.G), new Vector2(0f, 0.4f * c.Up),
					new Ramp(pal.Hi, pal.A, pal.B), 1f * c.S, 90) { GlowStrength = 0.7f, GlowRamp = pal.B, Front = true });
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.3f), new Vector2(0f, 0.3f * c.Up), c.Pal.Soft, R(0.2f, 0.35f) * c.S, 40) { Front = Coin });
		}

		/// <summary>Сердечки: всплывающие сердца с бликом, искорки и редкая «волна любви».</summary>
		private static void Hearts(Ctx c)
		{
			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + new Vector2(R(-22f, 22f), R(-8f, 18f)) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Heart, SpriteMotion.Float, pos, new Vector2(0f, R(0.6f, 1.1f) * c.Up * c.V),
					new Ramp(pal.A, pal.B), R(0.8f, 1.25f) * c.S, Main.rand.Next(70, 100))
				{
					GlowStrength = 0.6f,
					GlowRamp = new Ramp(pal.Hi, pal.B),
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.3f), new Vector2(0f, -0.3f * c.G), new Ramp(Color.White, c.Pal.A), R(0.3f, 0.5f) * c.S, 30) { Front = Coin });

			if (Tick(Every(90f, c.V)))
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new RingParticle(c.P.Center, new Ramp(pal.A, pal.B), 20f, 90f * c.S, 30) { Front = false, Opacity = 0.5f });
			}
		}

		/// <summary>Вихрь: полумесяцы и огоньки затягиваются по спирали, кольцо сжимается к центру.</summary>
		private static void Vortex(Ctx c)
		{
			for (int i = Fx.Rolls(0.35f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ShapeParticle p = Orbiter(c, Main.rand.NextBool(3) ? Shape.Crescent : Shape.Wisp, new Ramp(pal.Hi, pal.A, pal.B), R(46f, 58f) * c.S, -0.9f * c.V, 0.11f * c.V, R(0.45f, 0.7f) * c.S, 50);
				p.Glowing = true;
				p.GlowRamp = pal.B;
				p.OrbitSquash = 0.5f;
				p.FaceVelocity = true;
				ParticleSystem.Spawn(p);
			}

			for (int i = Fx.Rolls(0.5f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				float angle = R(0f, MathHelper.TwoPi);
				Vector2 offset = angle.ToRotationVector2() * R(36f, 50f) * c.S;
				Vector2 vel = (-offset).SafeNormalize(Vector2.Zero).RotatedBy(1.15f) * 2.4f * c.V;
				ParticleSystem.Spawn(new SparkParticle(c.P.Center + offset, vel, pal.Glow, 0.6f * c.S, 22)
				{
					Drag = 0.99f,
					Stretch = 2.5f,
					Front = offset.Y * c.G > 0f
				});
			}

			if (Tick(Every(30f, c.V)))
				ParticleSystem.Spawn(new RingParticle(c.P.Center, new Ramp(c.Pal.B, c.Pal.A), 110f * c.S, 18f * c.S, 30) { Front = false, Opacity = 0.55f });
		}

		/// <summary>Кольцо рун: руны бегут по орбите, от кольца поднимаются блики.</summary>
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
					Palette pal = Palette.Of(l.AuraColor, l.AuraColor2, o.Angle * 0.05f);
					o.Color = pal.Mix(0.5f + 0.5f * (float)Math.Sin(o.Angle));
					o.Color2 = pal.B;
				}
			});

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				float angle = R(0f, MathHelper.TwoPi);
				Vector2 pos = c.P.Center + VfxMath.OnEllipse(angle, 44f, 11f) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Float, pos, new Vector2(0f, R(0.4f, 0.9f) * c.Up * c.V), new Ramp(Color.White, pal.A, pal.B), R(0.3f, 0.45f) * c.S, 45)
				{
					Glowing = true,
					Front = Math.Sin(angle) > 0
				});
			}
		}

		/// <summary>Гроза: молнии вокруг тела, искры, тёмные тучки над головой, редкий удар сверху.</summary>
		private static void Storm(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 start = c.P.Center + Main.rand.NextVector2Circular(24f, 30f) * c.S;
				Vector2 end = start + Main.rand.NextVector2Unit() * R(36f, 64f) * c.S;
				ParticleSystem.Spawn(new LightningParticle(start, end, new Ramp(pal.A, pal.B), 0.7f, Main.rand.Next(10, 16)) { Front = Coin, CoreColor = pal.Hi });
			}

			for (int i = Fx.Rolls(0.22f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 dir = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new SparkParticle(c.P.Center + dir * R(6f, 20f) * c.S, dir * R(3f, 6f) * c.V, pal.Glow, 0.55f * c.S, 12)
				{
					Drag = 0.88f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Top + new Vector2(R(-26f, 26f), -R(16f, 26f) * c.G) * c.S;
				ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.3f, 0.3f), 0f), new Ramp(VfxMath.Shade(pal.B, 0.25f), new Color(10, 12, 20)), R(0.8f, 1.1f) * c.S, 70)
				{
					MaxOpacity = 0.4f,
					Front = false
				});
			}

			if (Main.rand.NextFloat() < 0.006f * c.D)
			{
				Palette pal = c.Pal;
				Vector2 end = c.P.Bottom;
				Vector2 start = end + new Vector2(R(-40f, 40f), -R(260f, 340f) * c.G);
				ParticleSystem.Spawn(new LightningParticle(start, end, new Ramp(pal.Hi, pal.A, pal.B), 1.4f, 18, 0.18f) { Front = false, CoreColor = Color.White });
				ParticleSystem.Spawn(new RingParticle(end, new Ramp(pal.A, pal.B), 20f, 120f * c.S, 28) { Squash = new Vector2(1f, 0.3f), Front = true });
				for (int k = 0; k < 8; k++)
				{
					Vector2 dir = new Vector2(R(-1f, 1f), -R(0.3f, 1f) * c.G);
					ParticleSystem.Spawn(new SparkParticle(end, dir * R(3f, 6f), pal.Glow, 0.7f, 18) { Gravity = new Vector2(0f, 0.2f * c.G), Bounce = true });
				}
				Lighting.AddLight(end, pal.A.ToVector3() * 2f);
			}
		}

		/// <summary>Пустота: тёмный дым, огоньки и полумесяцы затягиваются по спирали, схлопывающиеся кольца.</summary>
		private static void Void(Ctx c)
		{
			for (int i = Fx.Rolls(0.2f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new SmokeParticle(AroundBody(c, 1.2f), new Vector2(R(-0.3f, 0.3f), 0.5f * c.Up * c.V),
					new Ramp(VfxMath.Shade(pal.A, 0.25f), new Color(6, 2, 12)), R(0.7f, 1f) * c.S, Main.rand.Next(50, 70))
				{
					MaxOpacity = 0.5f,
					Grow = 1.6f,
					Front = Main.rand.NextBool(3)
				});
			}

			for (int i = Fx.Rolls(0.25f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ShapeParticle p = Orbiter(c, Shape.Wisp, new Ramp(pal.B, pal.A, pal.Hi), R(50f, 64f) * c.S, -1.1f * c.V, 0.08f * c.V, R(0.4f, 0.6f) * c.S, 48);
				p.Glowing = true;
				p.GlowRamp = pal.A;
				p.OrbitSquash = 0.55f;
				p.FaceVelocity = true;
				ParticleSystem.Spawn(p);
			}

			for (int i = Fx.Rolls(0.3f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				float dist = R(45f, 70f) * c.S;
				Vector2 dir = Main.rand.NextVector2Unit();
				float speed = 2.2f * c.V;
				int life = Math.Max(8, (int)(dist / speed) - 2);
				ParticleSystem.Spawn(new GlowParticle(c.P.Center + dir * dist, -dir * speed, new Ramp(pal.B, pal.A, pal.Hi), R(0.25f, 0.45f) * c.S, life)
				{
					Drag = 1f,
					EndScale = 0.1f,
					Front = dir.Y * c.G > 0f
				});
			}

			if (Tick(Every(45f, c.V)))
				ParticleSystem.Spawn(new RingParticle(c.P.Center, new Ramp(c.Pal.B, c.Pal.A), 150f * c.S, 16f * c.S, 40) { Front = false, Opacity = 0.65f });
		}

		/// <summary>Божественный свет: перья, золотые искры, ромбы-блики; столб света рисует слой.</summary>
		private static void Divine(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Top + new Vector2(R(-46f, 46f), -R(20f, 50f) * c.G) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Feather, SpriteMotion.Sway, pos, new Vector2(0f, 0.2f * c.G), new Ramp(Color.White, pal.Hi, pal.A), R(0.8f, 1.1f) * c.S, Main.rand.Next(130, 180))
				{
					GlowStrength = 0.5f,
					GlowRamp = pal.B,
					Settle = true,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.3f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = Feet(c) + new Vector2(R(-22f, 22f) * c.S, -R(0f, 30f) * c.G);
				ParticleSystem.Spawn(new GlowParticle(pos, new Vector2(0f, R(0.6f, 1.4f) * c.Up * c.V), pal.Glow, R(0.25f, 0.45f) * c.S, Main.rand.Next(45, 70))
				{
					Drag = 0.99f,
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Diamond, SpriteMotion.Float, AroundBody(c, 1.5f), new Vector2(0f, 0.5f * c.Up), new Ramp(Color.White, pal.A), R(0.4f, 0.6f) * c.S, 50)
				{
					Glowing = true,
					GlowRamp = pal.B,
					Front = Coin
				});
			}
		}

		/// <summary>Призма: осколки с радужными хвостами на орбитах, вращающиеся грани и радужные блики.</summary>
		private static void Prism(Ctx c)
		{
			const int shards = 4;
			EnsureOrbiters(c, shards, i => new OrbitParticle(c.P.whoAmI, MathHelper.TwoPi * i / shards, 22)
			{
				RadiusX = R(40f, 50f),
				RadiusY = R(14f, 22f),
				Tilt = R(-0.5f, 0.5f),
				AngularSpeed = R(0.045f, 0.06f),
				HeadShape = Shape.ShardA,
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
					Color rainbow = Main.hslToRgb(hue, 0.8f, 0.75f);
					o.Color = VfxMath.LerpVivid(rainbow, l.AuraColor.Get(), 0.25f);
					o.Color2 = Main.hslToRgb(VfxMath.Frac(hue + 0.3f), 0.75f, 0.68f);
					o.TrailColor = Color.White;
					o.TrailColor2 = rainbow;
				}
			});

			for (int i = Fx.Rolls(0.08f * c.D); i > 0; i--)
			{
				float hue = Main.rand.NextFloat();
				Color a = Main.hslToRgb(hue, 0.8f, 0.78f);
				Color b = Main.hslToRgb(VfxMath.Frac(hue + 0.25f), 0.75f, 0.68f);
				ParticleSystem.Spawn(new FlareParticle(AroundBody(c, 1.5f), Vector2.Zero, new Ramp(Color.White, a, b), R(0.3f, 0.6f) * c.S, 30) { Front = Coin });
			}

			for (int i = Fx.Rolls(0.035f * c.D); i > 0; i--)
			{
				float hue = Main.rand.NextFloat();
				Color a = Main.hslToRgb(hue, 0.8f, 0.78f);
				Color b = Main.hslToRgb(VfxMath.Frac(hue + 0.35f), 0.75f, 0.68f);
				ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Diamond : Shape.ShardB, SpriteMotion.Spin, AroundBody(c, 1.4f), Main.rand.NextVector2Circular(0.6f, 0.6f),
					new Ramp(Color.White, a, b), R(0.4f, 0.6f) * c.S, 45)
				{
					Glowing = true,
					GlowRamp = b,
					Spin = R(-0.12f, 0.12f),
					Front = Coin
				});
			}
		}

		/// <summary>Бабочки: порхают вокруг, машут крыльями, за ними сыплется блестящая пыльца.</summary>
		private static void Butterflies(Ctx c)
		{
			for (int i = Fx.Rolls(0.025f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 home = new Vector2(R(-46f, 46f), R(-40f, 10f) * c.G) * c.S;
				ParticleSystem.Spawn(new ShapeParticle(Shape.Butterfly0, SpriteMotion.Flutter, c.P.Center + home + Main.rand.NextVector2Circular(20f, 20f),
					Main.rand.NextVector2Circular(1f, 1f), new Ramp(pal.A, pal.B), R(0.85f, 1.15f) * c.S, Main.rand.Next(240, 360))
				{
					Owner = c.P.whoAmI,
					HomeOffset = home,
					GlowStrength = 0.5f,
					GlowRamp = new Ramp(pal.B, pal.A),
					Front = Coin
				});
			}

			for (int i = Fx.Rolls(0.12f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.6f), Main.rand.NextVector2Circular(0.3f, 0.3f), c.Pal.Soft, R(0.15f, 0.3f) * c.S, 50) { Front = Coin, Blink = 0.25f });
		}

		/// <summary>Руны: руны всплывают, искры, кольца-волны по земле.</summary>
		private static void Runes(Ctx c)
		{
			for (int i = Fx.Rolls(0.07f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + new Vector2(R(-30f, 30f), R(-6f, 26f) * c.G) * c.S;
				ParticleSystem.Spawn(new RuneParticle(pos, new Vector2(0f, 0.6f * c.Up * c.V), pal.Glow, R(0.8f, 1.1f) * c.S, Main.rand.Next(60, 80)) { Front = Coin });
			}

			for (int i = Fx.Rolls(0.2f * c.D); i > 0; i--)
				ParticleSystem.Spawn(new GlowParticle(AroundBody(c, 1.3f), new Vector2(0f, 0.5f * c.Up * c.V), c.Pal.Glow, R(0.2f, 0.35f) * c.S, 40) { Front = Coin });

			if (Tick(Every(70f, c.V)))
				ParticleSystem.Spawn(new RingParticle(Feet(c), new Ramp(c.Pal.A, c.Pal.B), 20f, 110f * c.S, 40) { Squash = new Vector2(1f, 0.3f), Front = false });

			if (Main.rand.NextFloat() < 0.01f * c.D)
			{
				Palette pal = c.Pal;
				var swirl = Orbiter(c, Shape.Swirl, new Ramp(pal.Hi, pal.A, pal.B), 0f, 0f, 0f, 1.2f * c.S, 60);
				swirl.Glowing = true;
				swirl.Spin = 0.08f;
				swirl.HomeOffset = new Vector2(0f, -36f * c.G * c.S);
				swirl.Front = false;
				ParticleSystem.Spawn(swirl);
			}
		}

		/// <summary>Светлячки: мерцающие огоньки бродят вокруг.</summary>
		private static void Fireflies(Ctx c)
		{
			for (int i = Fx.Rolls(0.05f * c.D); i > 0; i--)
			{
				Palette pal = c.Pal;
				Vector2 pos = c.P.Center + Main.rand.NextVector2Circular(70f, 50f) * c.S;
				ParticleSystem.Spawn(new GlowParticle(pos, Main.rand.NextVector2Circular(0.5f, 0.5f) * c.V, new Ramp(pal.Hi, pal.A, pal.B), R(0.45f, 0.7f) * c.S, Main.rand.Next(180, 260))
				{
					Blink = 0.12f,
					Wander = 0.05f * c.V,
					Drag = 0.96f,
					EndScale = 0.8f,
					Halo = 0.45f,
					Front = Coin
				});
			}
		}
	}
}
