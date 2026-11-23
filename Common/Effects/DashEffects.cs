using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Эффекты рывка. Рывок определяется двумя способами:
	/// - ванильные рывки (щит Ктулху, таби, солнечная броня): в первый кадр Player.dashDelay == -1;
	/// - любые другие (Calamity и прочие моды): резкий скачок горизонтальной скорости намного выше бега.
	/// Начало — вспышка, звуковая волна и искры; во время — линии скорости, росчерк-лента, послеобразы и
	/// частицы стиля; конец — небольшой всплеск. Цвета — как у эффектов на персонаже.
	/// </summary>
	public static class DashEffects
	{
		/// <summary>Самое долгое, сколько тиков считаем рывком.</summary>
		private const int MaxDuration = 24;
		/// <summary>За сколько тиков гаснут послеобразы и росчерк после рывка.</summary>
		private const int FadeTicks = 14;

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		private static Palette Pal(LookSettings look) => Palette.Of(look.EffectColor, look.EffectColor2, Main.rand.NextFloat(0.3f));

		private static Color Rainbow(float offset = 0f) =>
			Main.hslToRgb(VfxMath.Frac(Main.GlobalTimeWrappedHourly * 0.6f + offset + Main.rand.NextFloat(0.25f)), 0.8f, 0.75f);

		// ======================= определение рывка =======================

		public static void Update(LooksPlayer mp)
		{
			Player p = mp.Player;
			DashStyle style = mp.Look.Dash;
			bool visible = style != DashStyle.None && mp.EffectsVisible;
			float vx = p.velocity.X;
			float speed = p.velocity.Length();

			bool start = false;
			if (mp.DashCooldown > 0)
			{
				mp.DashCooldown--;
			}
			else if (!p.mount.Active && p.grapCount == 0)
			{
				bool vanilla = p.dashDelay == -1;
				float runCap = Math.Max(p.maxRunSpeed, p.accRunSpeed) + 4f;
				bool spike = Math.Abs(vx) > runCap && Math.Abs(vx) - Math.Abs(mp.PrevVelocityX) > 5f;
				start = vanilla || spike;
			}
			mp.PrevVelocityX = vx;

			if (start)
			{
				mp.DashActive = true;
				mp.DashTimer = 0;
				mp.DashFade = 1f;
				mp.DashCooldown = 15;
				mp.DashSpeed = Math.Max(speed, 8f);
				mp.DashDir = p.velocity.SafeNormalize(new Vector2(p.direction, 0f));
				if (visible)
					Burst(mp, style, true);
			}
			else if (mp.DashActive)
			{
				mp.DashTimer++;
				bool still = speed > Math.Max(5f, mp.DashSpeed * 0.45f) && mp.DashTimer < MaxDuration;
				if (still)
				{
					if (speed > 1f)
						mp.DashDir = p.velocity / speed;
				}
				else
				{
					mp.DashActive = false;
					if (visible)
						Burst(mp, style, false);
				}
			}

			if (!mp.DashActive && mp.DashFade > 0f)
				mp.DashFade = Math.Max(0f, mp.DashFade - 1f / FadeTicks);

			if (mp.DashActive && visible)
				During(mp, style);
		}

		// ======================= начало и конец =======================

		private static void Burst(LooksPlayer mp, DashStyle style, bool start)
		{
			Player p = mp.Player;
			Palette pal = Pal(mp.Look);
			Vector2 c = p.Center;
			Vector2 d = mp.DashDir;
			Vector2 perp = d.RotatedBy(MathHelper.PiOver2);
			float rot = d.ToRotation();
			float power = start ? 1f : 0.6f;
			float g = p.gravDir;

			// звуковая волна: эллипс поперёк движения + внутреннее кольцо
			ParticleSystem.Spawn(new RingParticle(c - d * 6f, new Ramp(pal.Hi, pal.A, pal.B), 12f, 80f * power, start ? 24 : 16)
			{
				Squash = new Vector2(0.32f, 1f),
				Rotation = rot,
				Front = true
			});
			ParticleSystem.Spawn(new RingParticle(c, new Ramp(Color.White, pal.A), 6f, 44f * power, 14)
			{
				Squash = new Vector2(0.28f, 1f),
				Rotation = rot,
				Front = true
			});
			ParticleSystem.Spawn(new FlareParticle(c, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), 1.25f * power, 18) { Front = true });

			// искры веером назад
			int sparks = start ? 10 : 5;
			for (int i = 0; i < sparks; i++)
			{
				Vector2 dir = (-d).RotatedBy(R(-0.65f, 0.65f));
				ParticleSystem.Spawn(new SparkParticle(c + perp * R(-10f, 10f), dir * R(3f, 8f) * power, pal.Glow, 0.7f, 18) { Drag = 0.9f, Front = Main.rand.NextBool() });
			}

			switch (style)
			{
				case DashStyle.Flash:
					for (int i = 0; i < 3; i++)
						ParticleSystem.Spawn(new FlareParticle(c + perp * R(-16f, 16f) - d * R(0f, 20f), -d * R(0.5f, 1.5f), new Ramp(Color.White, pal.A), R(0.5f, 0.9f) * power, 24) { Front = true });
					break;

				case DashStyle.Shadow:
					for (int i = 0; i < (start ? 7 : 4); i++)
						ParticleSystem.Spawn(new SmokeParticle(c + Main.rand.NextVector2Circular(12f, 18f), -d * R(0.5f, 2f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
							new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.7f, 1.1f), Main.rand.Next(45, 60))
						{
							MaxOpacity = 0.6f,
							Front = Main.rand.NextBool()
						});
					break;

				case DashStyle.Flame:
					for (int i = 0; i < (start ? 8 : 4); i++)
					{
						ParticleSystem.Spawn(new SmokeParticle(c + Main.rand.NextVector2Circular(10f, 16f), -d * R(1f, 3f) + new Vector2(0f, -0.8f * g), pal.Hot, R(0.5f, 0.8f), 30)
						{
							Glowing = true,
							Grow = 0.4f,
							MaxOpacity = 0.8f,
							Front = Main.rand.NextBool()
						});
						ParticleSystem.Spawn(new GlowParticle(c, Main.rand.NextVector2Circular(4f, 4f), pal.Hot, R(0.25f, 0.45f), 40) { Wander = 0.08f, Gravity = new Vector2(0f, -0.04f * g) });
					}
					break;

				case DashStyle.Thunder:
					for (int i = 0; i < (start ? 2 : 1); i++)
					{
						Vector2 end = c + d * R(50f, 90f) + perp * R(-20f, 20f);
						ParticleSystem.Spawn(new LightningParticle(c - d * 10f, end, new Ramp(pal.Hi, pal.A, pal.B), 1f, 14, 0.28f) { CoreColor = Color.White, Front = true });
					}
					for (int i = 0; i < 6; i++)
					{
						Vector2 dir = Main.rand.NextVector2Unit();
						ParticleSystem.Spawn(new SparkParticle(c, dir * R(4f, 7f), pal.Glow, 0.5f, 10) { Drag = 0.85f });
					}
					break;

				case DashStyle.Prism:
					for (int i = 0; i < (start ? 9 : 5); i++)
					{
						Color a = Rainbow(i / 9f);
						Vector2 dir = Main.rand.NextVector2Unit();
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.Diamond : Shape.ShardA, SpriteMotion.Spin, c, dir * R(2f, 5f),
							new Ramp(Color.White, a, VfxMath.HueShift(a, 0.2f)), R(0.45f, 0.7f), 40)
						{
							Glowing = true,
							Spin = R(-0.2f, 0.2f),
							Front = Main.rand.NextBool()
						});
					}
					break;

				case DashStyle.Sakura:
					for (int i = 0; i < (start ? 10 : 5); i++)
					{
						Vector2 dir = Main.rand.NextVector2Unit();
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Petal, SpriteMotion.Fall, c + dir * 6f, dir * R(1.5f, 4f) - d,
							new Ramp(pal.Hi, pal.A, pal.B), R(0.7f, 1f), 80)
						{
							GlowStrength = 0.4f,
							GlowRamp = pal.B,
							Spin = R(-0.15f, 0.15f),
							Front = Main.rand.NextBool()
						});
					}
					break;

				case DashStyle.Void:
					// разрез: огромный полумесяц поперёк движения и схлопывающееся кольцо
					ParticleSystem.Spawn(new ShapeParticle(Shape.Crescent, SpriteMotion.Spin, c + d * 8f, d * 0.6f, new Ramp(Color.White, pal.A, pal.B), 2.2f * power, 22)
					{
						Glowing = true,
						GlowRamp = pal.B,
						GlowStrength = 0.8f,
						Rotation = rot,
						Spin = 0f,
						Front = true
					});
					ParticleSystem.Spawn(new RingParticle(c, new Ramp(pal.B, pal.A), 110f * power, 10f, 24) { Front = false, Opacity = 0.8f });
					for (int i = 0; i < 5; i++)
						ParticleSystem.Spawn(new SmokeParticle(c + Main.rand.NextVector2Circular(14f, 18f), -d * R(0.3f, 1.2f),
							new Ramp(VfxMath.Shade(pal.A, 0.25f), new Color(6, 2, 12)), R(0.6f, 0.9f), 45) { MaxOpacity = 0.5f, Front = false });
					break;
			}

			if (start)
				Lighting.AddLight(c, pal.A.ToVector3() * 1.2f);
		}

		// ======================= во время рывка =======================

		private static void During(LooksPlayer mp, DashStyle style)
		{
			Player p = mp.Player;
			Palette pal = Pal(mp.Look);
			Vector2 c = p.Center;
			Vector2 d = mp.DashDir;
			Vector2 perp = d.RotatedBy(MathHelper.PiOver2);
			Vector2 v = p.velocity;
			float g = p.gravDir;

			Lighting.AddLight(c, pal.A.ToVector3() * 0.6f);

			// линии скорости вдоль движения
			for (int i = 0; i < 2; i++)
			{
				Vector2 pos = c + perp * R(-20f, 20f) - d * R(0f, 24f);
				ParticleSystem.Spawn(new SparkParticle(pos, v * 0.25f, new Ramp(Color.Lerp(pal.A, Color.White, 0.5f), pal.B), 0.55f, 10)
				{
					Drag = 0.88f,
					Stretch = 3.2f,
					Width = 0.6f,
					Front = Main.rand.NextBool()
				});
			}

			Vector2 behind = c - d * R(4f, 16f) + perp * R(-12f, 12f);
			switch (style)
			{
				case DashStyle.Flash:
					ParticleSystem.Spawn(new GlowParticle(behind, -v * 0.05f, new Ramp(Color.White, pal.A, pal.B), R(0.3f, 0.5f), 24) { Front = Main.rand.NextBool() });
					if (Main.rand.NextBool(3))
						ParticleSystem.Spawn(new FlareParticle(behind, Vector2.Zero, new Ramp(Color.White, pal.A), R(0.4f, 0.6f), 20) { Front = true });
					break;

				case DashStyle.Shadow:
					ParticleSystem.Spawn(new SmokeParticle(behind, -v * 0.05f, new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.6f, 0.9f), 40)
					{
						MaxOpacity = 0.55f,
						Front = Main.rand.NextBool()
					});
					if (Main.rand.NextBool(2))
						ParticleSystem.Spawn(new GlowParticle(behind, -v * 0.08f, new Ramp(pal.A, pal.B), R(0.2f, 0.35f), 26) { Front = true });
					break;

				case DashStyle.Flame:
					ParticleSystem.Spawn(new SmokeParticle(behind, -v * 0.05f + new Vector2(0f, -0.8f * g), pal.Hot, R(0.45f, 0.7f), 26)
					{
						Glowing = true,
						Grow = 0.35f,
						MaxOpacity = 0.8f,
						Front = Main.rand.NextBool()
					});
					ParticleSystem.Spawn(new GlowParticle(behind, -v * 0.1f + Main.rand.NextVector2Circular(1f, 1f), pal.Hot, R(0.2f, 0.35f), 36) { Wander = 0.08f });
					break;

				case DashStyle.Thunder:
					if (mp.DashTimer % 3 == 0)
						ParticleSystem.Spawn(new LightningParticle(c - d * 8f + perp * R(-8f, 8f), c - d * R(40f, 70f) + perp * R(-24f, 24f), new Ramp(pal.Hi, pal.A, pal.B), 0.7f, 10)
						{
							CoreColor = Color.White,
							Front = Main.rand.NextBool()
						});
					if (Main.rand.NextBool(2))
						ParticleSystem.Spawn(new SparkParticle(behind, Main.rand.NextVector2Unit() * R(2f, 5f), pal.Glow, 0.45f, 9) { Drag = 0.85f });
					break;

				case DashStyle.Prism:
				{
					Color a = Rainbow();
					ParticleSystem.Spawn(new ShapeParticle(Shape.ShardA, SpriteMotion.Spin, behind, -v * 0.05f + Main.rand.NextVector2Circular(0.8f, 0.8f),
						new Ramp(Color.White, a, VfxMath.HueShift(a, 0.2f)), R(0.4f, 0.6f), 30)
					{
						Glowing = true,
						Spin = R(-0.2f, 0.2f),
						Front = Main.rand.NextBool()
					});
					break;
				}

				case DashStyle.Sakura:
					ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.PetalRound : Shape.Petal, SpriteMotion.Fall, behind, -v * 0.08f,
						new Ramp(pal.Hi, pal.A, pal.B), R(0.65f, 0.9f), 70)
					{
						GlowStrength = 0.35f,
						GlowRamp = pal.B,
						Spin = R(-0.15f, 0.15f),
						Front = Main.rand.NextBool()
					});
					break;

				case DashStyle.Void:
					ParticleSystem.Spawn(new SmokeParticle(behind, -v * 0.04f, new Ramp(VfxMath.Shade(pal.A, 0.25f), new Color(6, 2, 12)), R(0.55f, 0.8f), 36)
					{
						MaxOpacity = 0.5f,
						Front = false
					});
					ParticleSystem.Spawn(new GlowParticle(behind + perp * R(-10f, 10f), -v * 0.12f, new Ramp(pal.B, pal.A, pal.Hi), R(0.25f, 0.4f), 22) { Front = true });
					break;
			}
		}

		// ======================= послеобразы =======================

		/// <summary>Цветные копии персонажа вдоль пути рывка (видны даже в скрытности разбойника).</summary>
		public static void DrawAfterimages(LooksPlayer mp, Camera camera)
		{
			LookSettings look = mp.Look;
			if (look.Dash == DashStyle.None || mp.DashFade <= 0.01f || mp.HistoryCount < 3)
				return;

			Player player = mp.Player;
			Palette pal = Palette.Of(look.EffectColor, look.EffectColor2);
			var ramp = new Ramp(pal.Hi, pal.A, pal.B);
			bool dark = look.Dash == DashStyle.Shadow || look.Dash == DashStyle.Void;
			const int count = 5;
			const int step = 2;

			try
			{
				TrailEffects.DrawingEcho = true;
				TrailEffects.EchoTint = true;
				for (int i = count; i >= 1; i--)
				{
					int index = i * step;
					if (index >= mp.HistoryCount)
						continue;

					float t = i / (float)count;
					Color color = look.Dash == DashStyle.Prism
						? Main.hslToRgb(VfxMath.Frac(Main.GlobalTimeWrappedHourly * 0.6f + t * 0.6f), 0.8f, 0.72f)
						: ramp.At(t);
					// у тени и пустоты первые копии тёмные, дальние — светящиеся
					TrailEffects.EchoDark = dark && i <= 2;
					if (TrailEffects.EchoDark)
						color = VfxMath.Shade(pal.A, 0.2f, 1.2f);
					TrailEffects.EchoColor = color;
					TrailEffects.EchoOpacity = (TrailEffects.EchoDark ? 0.7f : 0.6f) * (1f - t * 0.65f) * mp.DashFade;

					Vector2 topLeft = mp.History[index] - player.Size / 2f;
					Main.PlayerRenderer.DrawPlayer(camera, player, topLeft, player.fullRotation, player.fullRotationOrigin, 0.2f + 0.1f * i);
				}
			}
			catch (Exception e)
			{
				VfxRenderer.LogOnce("dash afterimages", e);
			}
			finally
			{
				TrailEffects.DrawingEcho = false;
				TrailEffects.EchoDark = false;
			}
		}

		// ======================= росчерк-лента =======================

		private const int RibbonPoints = 16;
		private static readonly Vector2[] RibbonPos = new Vector2[RibbonPoints];
		private static readonly float[] RibbonRot = new float[RibbonPoints];

		/// <summary>Светящийся росчерк вдоль пути рывка (рисуется позади игроков, шейдером).</summary>
		public static void DrawRibbon(LooksPlayer mp)
		{
			LookSettings look = mp.Look;
			if (look.Dash == DashStyle.None || mp.DashFade <= 0.01f)
				return;

			int count = Math.Min(RibbonPoints, mp.HistoryCount);
			if (count < 4)
				return;

			for (int i = 0; i < count; i++)
				RibbonPos[i] = mp.History[i];
			for (int i = 0; i < count; i++)
			{
				Vector2 dir = i == 0 ? RibbonPos[0] - RibbonPos[1] : RibbonPos[i - 1] - RibbonPos[i];
				RibbonRot[i] = dir.LengthSquared() > 0.01f ? dir.ToRotation() : (i > 0 ? RibbonRot[i - 1] : 0f);
			}

			var positions = new Vector2[count];
			var rotations = new float[count];
			Array.Copy(RibbonPos, positions, count);
			Array.Copy(RibbonRot, rotations, count);

			Palette pal = Palette.Of(look.EffectColor, look.EffectColor2);
			var ramp = new Ramp(pal.Hi, pal.A, pal.B);
			float fade = mp.DashFade;
			float width = 11f;

			RibbonStyle shader = look.Dash switch
			{
				DashStyle.Flame => RibbonStyle.Flame,
				DashStyle.Prism => RibbonStyle.Rainbow,
				_ => RibbonStyle.Soft
			};

			Ribbons.Draw(positions, rotations,
				p => look.Dash == DashStyle.Prism
					? VfxMath.Additive(Color.Lerp(Color.White, Main.hslToRgb(VfxMath.Frac(p * 1.4f - Main.GlobalTimeWrappedHourly), 0.85f, 0.65f), VfxMath.Ease(p * 3f)), fade * (1f - p))
					: VfxMath.Additive(ramp.At(p), fade * (1f - p)),
				p => width * fade * VfxMath.Ease(p * 6f) * (1f - p * 0.7f),
				shader);
		}
	}
}
