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
	/// <summary>Одно «перо» крыла в локальных координатах: X — назад от лица, Y — вниз.</summary>
	public struct WingElement
	{
		public Vector2 Base;
		public float Angle;
		public float Length;
		/// <summary>0 — маховые (сзади), 1 — второстепенные, 2 — кроющие (спереди).</summary>
		public int Layer;
		/// <summary>Положение в ряду 0..1 (от плеча к кончику).</summary>
		public float K;
		public int Index;

		public Vector2 Tip => Base + Angle.ToRotationVector2() * Length;
	}

	/// <summary>
	/// «Скелет» крыла: плечо → запястье → кончик и три ряда перьев. Взмах задаётся фазой и силой.
	/// Используется и для отрисовки, и для частиц с кончиков крыльев.
	/// </summary>
	public static class WingRig
	{
		public const int MaxElements = 24;

		public static float ArmAngle(float phase, float amp) => -0.4f - (float)Math.Sin(phase) * amp * 0.55f;

		public static float ForearmAngle(float phase, float amp) => ArmAngle(phase, amp) + 0.15f + (float)Math.Sin(phase - 0.6f) * amp * 0.3f;

		/// <summary>Точки скелета: плечо, запястье, кончик.</summary>
		public static void Skeleton(float phase, float amp, float size, out Vector2 wrist, out Vector2 tip)
		{
			wrist = ArmAngle(phase, amp).ToRotationVector2() * 13f * size;
			tip = wrist + ForearmAngle(phase, amp).ToRotationVector2() * 16f * size;
		}

		private static Vector2 OnArm(float u, Vector2 wrist, Vector2 tip) =>
			u < 0.5f ? Vector2.Lerp(Vector2.Zero, wrist, u / 0.5f) : Vector2.Lerp(wrist, tip, (u - 0.5f) / 0.5f);

		/// <summary>Заполняет buffer перьями (сначала задний ряд). Возвращает количество.</summary>
		public static int Build(WingElement[] buffer, float phase, float amp, float size)
		{
			Skeleton(phase, amp, size, out Vector2 wrist, out Vector2 tip);
			float fore = ForearmAngle(phase, amp);
			// на взмахе вверх перья немного складываются
			float fold = 1f - 0.25f * Math.Max(0f, (float)Math.Cos(phase)) * amp;
			int n = 0;

			const int primaries = 7;
			for (int i = 0; i < primaries; i++)
			{
				float k = i / (float)(primaries - 1);
				buffer[n++] = new WingElement
				{
					Base = OnArm(MathHelper.Lerp(0.52f, 0.98f, k), wrist, tip),
					Angle = MathHelper.Lerp(fore + 1.15f * fold, fore - 0.12f, k),
					Length = MathHelper.Lerp(17f, 26f, (float)Math.Sin(k * MathHelper.PiOver2)) * size,
					Layer = 0,
					K = k,
					Index = i
				};
			}

			const int secondaries = 6;
			for (int i = 0; i < secondaries; i++)
			{
				float k = i / (float)(secondaries - 1);
				buffer[n++] = new WingElement
				{
					Base = OnArm(MathHelper.Lerp(0.06f, 0.5f, k), wrist, tip),
					Angle = MathHelper.Lerp(1.4f, fore + 1.2f * fold, k),
					Length = MathHelper.Lerp(13f, 17f, k) * size,
					Layer = 1,
					K = k,
					Index = i
				};
			}

			const int coverts = 7;
			for (int i = 0; i < coverts; i++)
			{
				float k = i / (float)(coverts - 1);
				buffer[n++] = new WingElement
				{
					Base = OnArm(MathHelper.Lerp(0.04f, 0.85f, k), wrist, tip),
					Angle = MathHelper.Lerp(1.1f, fore + 0.6f, k),
					Length = MathHelper.Lerp(7f, 10f, k) * size,
					Layer = 2,
					K = k,
					Index = i
				};
			}

			return n;
		}
	}

	/// <summary>Параметры крыльев, которые сейчас показывает игрок.</summary>
	public struct WingLook
	{
		public WingStyle Style;
		public Palette Pal;
		public float Size;
		public bool Rainbow;
	}

	/// <summary>Крылья из эффектов: отрисовка (слой игрока) и частицы.</summary>
	public static class WingEffects
	{
		private static readonly WingElement[] Buffer = new WingElement[WingRig.MaxElements];

		/// <summary>Сторона «назад» (1 или -1) и гравитация (1 или -1).</summary>
		private struct Frame
		{
			public Vector2 Shoulder;
			public int Back;
			public int Grav;

			public Vector2 ToWorld(Vector2 local) => Shoulder + new Vector2(local.X * Back, local.Y * Grav);
		}

		private static Frame MakeFrame(Player p, Vector2 center) => new Frame
		{
			Shoulder = center + new Vector2(-p.direction * 5f, -7f * p.gravDir),
			Back = -p.direction,
			Grav = p.gravDir < 0 ? -1 : 1
		};

		// ======================= частицы =======================

		public static void Update(LooksPlayer mp)
		{
			if (!mp.TryGetWings(out WingLook wl))
				return;

			Player p = mp.Player;
			Frame f = MakeFrame(p, p.Center);
			float intensity = Fx.GlobalIntensity * (p.velocity.Y != 0f ? 1f : 0.45f);
			WingRig.Skeleton(mp.WingPhase, mp.WingAmp, wl.Size, out Vector2 wrist, out Vector2 tipLocal);
			Vector2 tip = f.ToWorld(tipLocal);
			Vector2 mid = f.ToWorld(Vector2.Lerp(wrist, tipLocal, 0.5f) + new Vector2(0f, 10f * wl.Size));
			Vector2 spot = Vector2.Lerp(tip, mid, Main.rand.NextFloat());
			Palette pal = wl.Pal;
			bool front = false;

			switch (wl.Style)
			{
				case WingStyle.Seraph:
					if (Main.rand.NextFloat() < 0.25f * intensity)
						ParticleSystem.Spawn(new GlowParticle(spot, new Vector2(0f, 0.3f), pal.Glow, Main.rand.NextFloat(0.25f, 0.45f), 40) { Front = front });
					if (Main.rand.NextFloat() < 0.025f * intensity)
						ParticleSystem.Spawn(new ShapeParticle(Shape.Feather, SpriteMotion.Sway, spot, Vector2.Zero, new Ramp(pal.Hi, pal.A), 0.8f, 120) { GlowRamp = pal.B, Front = front });
					break;
				case WingStyle.Light:
					if (Main.rand.NextFloat() < 0.2f * intensity)
					{
						Color c = wl.Rainbow ? Main.hslToRgb(Main.rand.NextFloat(), 0.8f, 0.75f) : pal.Mix(Main.rand.NextFloat());
						ParticleSystem.Spawn(new FlareParticle(spot, Main.rand.NextVector2Circular(0.4f, 0.4f), new Ramp(Color.White, c), Main.rand.NextFloat(0.35f, 0.6f), 28) { Front = front });
					}
					break;
				case WingStyle.Butterfly:
					if (Main.rand.NextFloat() < 0.3f * intensity)
						ParticleSystem.Spawn(new GlowParticle(spot, new Vector2(0f, 0.4f * p.gravDir), pal.Soft, Main.rand.NextFloat(0.15f, 0.3f), 45) { Blink = 0.3f, Front = front });
					break;
				case WingStyle.Demon:
					if (Main.rand.NextFloat() < 0.12f * intensity)
						ParticleSystem.Spawn(new SmokeParticle(spot, new Vector2(0f, -0.4f * p.gravDir), new Ramp(VfxMath.Shade(pal.B, 0.3f), Color.Black), 0.5f, 50) { MaxOpacity = 0.4f, Front = front });
					if (Main.rand.NextFloat() < 0.12f * intensity)
						ParticleSystem.Spawn(new GlowParticle(spot, new Vector2(0f, -0.5f * p.gravDir), pal.Hot, 0.3f, 40) { Wander = 0.05f, Front = front });
					break;
				case WingStyle.Flame:
					if (Main.rand.NextFloat() < 0.6f * intensity)
						ParticleSystem.Spawn(new GlowParticle(spot, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.8f, 1.8f) * p.gravDir), pal.Hot, Main.rand.NextFloat(0.25f, 0.45f), 45) { Wander = 0.06f, Front = front });
					break;
				case WingStyle.Crystal:
					if (Main.rand.NextFloat() < 0.06f * intensity)
						ParticleSystem.Spawn(new ShapeParticle(Shape.ShardB, SpriteMotion.Spin, spot, new Vector2(0f, 0.8f * p.gravDir), new Ramp(pal.Hi, pal.A, pal.B), 0.45f, 50) { Glowing = true, Spin = 0.1f, Front = front });
					if (Main.rand.NextFloat() < 0.08f * intensity)
						ParticleSystem.Spawn(new FlareParticle(spot, Vector2.Zero, new Ramp(Color.White, pal.A), 0.4f, 26) { Front = front });
					break;
				case WingStyle.Constellation:
					if (Main.rand.NextFloat() < 0.12f * intensity)
						ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Spin, spot, Main.rand.NextVector2Circular(0.3f, 0.3f), new Ramp(Color.White, pal.A, pal.B), 0.4f, 50) { Glowing = true, Spin = 0.05f, Front = front });
					break;
				case WingStyle.Spirit:
					if (Main.rand.NextFloat() < 0.18f * intensity)
						ParticleSystem.Spawn(new GlowParticle(spot, -p.velocity * 0.1f + new Vector2(0f, -0.3f * p.gravDir), pal.Glow, Main.rand.NextFloat(0.3f, 0.5f), 50) { Drag = 0.97f, Core = false, Halo = 0.45f, Front = front });
					break;
			}
		}

		// ======================= отрисовка =======================

		/// <summary>Спрайт с точкой крепления: правильно зеркалится, когда персонаж смотрит в другую сторону.</summary>
		private static void Anchored(ref PlayerDrawSet info, int shader, Texture2D tex, Rectangle? frame, Vector2 pos, Vector2 anchor, float localRot,
			Vector2 scale, Color color, Frame f)
		{
			Rectangle src = frame ?? new Rectangle(0, 0, tex.Width, tex.Height);
			SpriteEffects fx = SpriteEffects.None;
			Vector2 origin = anchor;
			float rot = localRot;
			if (f.Back < 0)
			{
				fx |= SpriteEffects.FlipHorizontally;
				origin.X = src.Width - origin.X;
				rot = -rot;
			}
			if (f.Grav < 0)
			{
				fx |= SpriteEffects.FlipVertically;
				origin.Y = src.Height - origin.Y;
				rot = -rot;
			}
			info.DrawDataCache.Add(new DrawData(tex, pos, frame, color, rot, origin, scale, fx, 0) { shader = shader });
		}

		public static void Draw(ref PlayerDrawSet info, LooksPlayer mp, WingLook wl)
		{
			Player p = info.drawPlayer;
			Vector2 center = (info.Position - Main.screenPosition + p.Size / 2f).Floor();
			Frame f = MakeFrame(p, center);
			int shader = info.cWings;

			// дальнее крыло: с запаздыванием, приподнято, темнее и меньше
			DrawWing(ref info, wl, f, mp.WingPhase - 0.35f, mp.WingAmp, wl.Size * 0.86f, -0.42f, 0.6f, shader, new Vector2(-3f, -2f));
			DrawWing(ref info, wl, f, mp.WingPhase, mp.WingAmp, wl.Size, 0f, 1f, shader, Vector2.Zero);
		}

		private static void DrawWing(ref PlayerDrawSet info, WingLook wl, Frame f, float phase, float amp, float size, float angleOffset,
			float bright, int shader, Vector2 offset)
		{
			Frame wf = f;
			wf.Shoulder = f.ToWorld(offset);
			float time = Main.GlobalTimeWrappedHourly;
			Palette pal = wl.Pal;

			switch (wl.Style)
			{
				case WingStyle.Butterfly:
					DrawButterfly(ref info, wl, wf, phase, amp, size, angleOffset, bright, shader);
					return;
				case WingStyle.Demon:
					DrawBat(ref info, wl, wf, phase, amp, size, angleOffset, bright, shader);
					return;
				case WingStyle.Light:
					DrawLightBlades(ref info, wl, wf, phase, amp, size, angleOffset, bright, shader, time);
					return;
			}

			int count = WingRig.Build(Buffer, phase, amp, size);
			for (int i = 0; i < count; i++)
			{
				WingElement e = Buffer[i];
				// у дальнего крыла кроющие перья не видны — силуэт чище
				if (bright < 0.99f && e.Layer == 2 && wl.Style != WingStyle.Constellation)
					continue;
				e.Angle += angleOffset;
				e.Base = e.Base.RotatedBy(angleOffset);
				Vector2 basePos = wf.ToWorld(e.Base);
				float layerShade = e.Layer == 0 ? 1f : e.Layer == 1 ? 0.92f : 0.85f;
				float k = e.Layer == 0 ? 0.55f + 0.45f * e.K : e.Layer == 1 ? 0.25f + 0.3f * e.K : 0.1f + 0.2f * e.K;

				switch (wl.Style)
				{
					case WingStyle.Seraph:
					{
						Color fill = VfxMath.Shade(VfxMath.LerpVivid(pal.Hi, pal.A, k), bright * layerShade, bright < 0.99f ? 1.25f : 1f);
						Texture2D glow = VfxAssets.FeatherGlow;
						Texture2D feather = VfxAssets.Feather;
						var scale = new Vector2(e.Length / 60f, 1f * size);
						Anchored(ref info, shader, glow, null, basePos, new Vector2(2f, 8f), e.Angle, scale * new Vector2(1.06f, 1.4f),
							VfxMath.Additive(pal.B, 0.4f * bright), wf);
						Anchored(ref info, shader, feather, null, basePos, new Vector2(2f, 8f), e.Angle, scale, fill, wf);
						if (e.Layer == 0 && (float)Math.Sin(time * 3f + e.Index * 1.9f) > 0.75f)
							Anchored(ref info, shader, VfxAssets.Glint, null, wf.ToWorld(e.Tip), new Vector2(32f, 32f), time, new Vector2(0.25f * size),
								VfxMath.Additive(Color.White, 0.7f * bright), wf);
						break;
					}
					case WingStyle.Flame:
					{
						Texture2D flame = VfxAssets.Flame;
						Rectangle frame = VfxAssets.FlameFrame((int)(time * 10f) + e.Index + e.Layer * 2);
						float rot = e.Angle + MathHelper.PiOver2;
						float width = (e.Layer == 2 ? 0.45f : 0.6f) * size;
						Anchored(ref info, shader, flame, frame, basePos, new Vector2(16f, 46f), rot, new Vector2(width, e.Length / 44f),
							VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.B, k), 0.6f * bright), wf);
						Anchored(ref info, shader, flame, frame, basePos, new Vector2(16f, 46f), rot, new Vector2(width * 0.5f, e.Length / 44f * 0.65f),
							VfxMath.Additive(pal.Hi, 0.4f * bright), wf);
						break;
					}
					case WingStyle.Crystal:
					{
						Rectangle frame = Shapes.Frame(Shape.ShardA);
						float rot = e.Angle + MathHelper.PiOver2;
						var scale = new Vector2(0.85f * size, e.Length / 24f);
						Anchored(ref info, shader, VfxAssets.ShapesGlow, frame, basePos, new Vector2(16f, 28f), rot, scale * 1.15f,
							VfxMath.Additive(pal.B, 0.5f * bright), wf);
						Color fill = VfxMath.Shade(VfxMath.LerpVivid(pal.Hi, pal.A, k), bright * layerShade);
						Anchored(ref info, shader, VfxAssets.Shapes, frame, basePos, new Vector2(16f, 28f), rot, scale, fill * 0.92f, wf);
						if (e.Layer < 2 && (float)Math.Sin(time * 2.6f + e.Index * 2.3f + e.Layer) > 0.82f)
							Anchored(ref info, shader, VfxAssets.Glint, null, wf.ToWorld(e.Tip), new Vector2(32f, 32f), 0f, new Vector2(0.3f * size),
								VfxMath.Additive(Color.White, 0.8f * bright), wf);
						break;
					}
					case WingStyle.Spirit:
					{
						Texture2D blade = VfxAssets.Blade;
						float wave = (float)Math.Sin(time * 2.4f + e.Index * 0.7f + e.Layer) * 0.09f;
						var scale = new Vector2(e.Length * 1.3f / 128f, 0.75f * size);
						Color c = VfxMath.LerpVivid(pal.A, pal.B, k);
						Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), e.Angle + wave, scale * new Vector2(1f, 1.6f),
							VfxMath.Additive(c, 0.35f * bright), wf);
						Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), e.Angle + wave, scale * new Vector2(0.9f, 0.6f),
							VfxMath.Additive(Color.Lerp(c, Color.White, 0.4f), 0.3f * bright), wf);
						break;
					}
					case WingStyle.Constellation:
					{
						if (e.Layer == 2)
							break;
						Texture2D bloom = VfxAssets.Bloom;
						Vector2 tipPos = wf.ToWorld(e.Tip);
						// туманность вдоль пера
						Anchored(ref info, shader, bloom, null, Vector2.Lerp(basePos, tipPos, 0.5f), new Vector2(64f, 64f), e.Angle,
							new Vector2(e.Length / 110f, 0.12f * size), VfxMath.Additive(pal.B, 0.18f * bright), wf);
						// линия к следующему кончику в ряду
						if (i + 1 < count && Buffer[i + 1].Layer == e.Layer)
						{
							WingElement next = Buffer[i + 1];
							next.Angle += angleOffset;
							next.Base = next.Base.RotatedBy(angleOffset);
							DrawLine(ref info, shader, tipPos, wf.ToWorld(next.Tip), VfxMath.Additive(pal.A, 0.55f * bright), 1.3f);
						}
						float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 4f + e.Index * 2.1f + e.Layer * 3f);
						Anchored(ref info, shader, VfxAssets.Flare, null, tipPos, new Vector2(64f, 64f), time * 0.3f, new Vector2(0.12f * size * twinkle),
							VfxMath.Additive(VfxMath.LerpVivid(pal.Hi, pal.A, k), bright), wf);
						Anchored(ref info, shader, VfxAssets.Glint, null, tipPos, new Vector2(32f, 32f), 0f, new Vector2(0.22f * size * twinkle),
							VfxMath.Additive(Color.White, 0.6f * bright), wf);
						break;
					}
				}
			}

			if (wl.Style == WingStyle.Constellation)
			{
				// «кость» созвездия: плечо → запястье → кончик
				WingRig.Skeleton(phase, amp, size, out Vector2 wrist, out Vector2 tip);
				Vector2 s0 = wf.Shoulder;
				Vector2 s1 = wf.ToWorld(wrist.RotatedBy(angleOffset));
				Vector2 s2 = wf.ToWorld(tip.RotatedBy(angleOffset));
				DrawLine(ref info, shader, s0, s1, VfxMath.Additive(pal.Hi, 0.6f * bright), 1.5f);
				DrawLine(ref info, shader, s1, s2, VfxMath.Additive(pal.Hi, 0.6f * bright), 1.5f);
			}
		}

		private static void DrawLine(ref PlayerDrawSet info, int shader, Vector2 a, Vector2 b, Color color, float thickness)
		{
			Texture2D line = VfxAssets.Line;
			Vector2 diff = b - a;
			info.DrawDataCache.Add(new DrawData(line, a, null, color, diff.ToRotation(), new Vector2(0f, line.Height / 2f),
				new Vector2((diff.Length() + 1f) / line.Width, thickness / line.Height), SpriteEffects.None, 0) { shader = shader });
		}

		/// <summary>Крылья бабочки: взмах — сжатие по горизонтали (крыло поворачивается к зрителю ребром).</summary>
		private static void DrawButterfly(ref PlayerDrawSet info, WingLook wl, Frame f, float phase, float amp, float size, float angleOffset,
			float bright, int shader)
		{
			Palette pal = wl.Pal;
			float open = 0.3f + 0.7f * (0.5f + 0.5f * (float)Math.Cos(phase * 1.5f)) * MathHelper.Lerp(1f, 0.85f, amp);
			float rot = -0.25f + angleOffset + (float)Math.Sin(phase * 1.5f) * 0.08f;
			var scale = new Vector2(0.8f * size * open, 0.8f * size);
			var anchor = new Vector2(4f, 32f);
			Anchored(ref info, shader, VfxAssets.ButterflyGlow, null, f.Shoulder, anchor, rot, scale * 1.05f, VfxMath.Additive(pal.B, 0.45f * bright), f);
			Anchored(ref info, shader, VfxAssets.ButterflyWing, null, f.Shoulder, anchor, rot, scale, VfxMath.Shade(pal.A, bright) * 0.9f, f);
			Anchored(ref info, shader, VfxAssets.ButterflyPattern, null, f.Shoulder, anchor, rot, scale, VfxMath.Shade(pal.B, bright), f);
			Anchored(ref info, shader, VfxAssets.ButterflyPattern, null, f.Shoulder, anchor, rot, scale, VfxMath.Additive(pal.Hi, 0.3f * bright), f);
		}

		/// <summary>Крылья демона: тёмная перепонка и светящиеся кости.</summary>
		private static void DrawBat(ref PlayerDrawSet info, WingLook wl, Frame f, float phase, float amp, float size, float angleOffset,
			float bright, int shader)
		{
			Palette pal = wl.Pal;
			float s = (float)Math.Sin(phase);
			float rot = -0.3f + angleOffset - s * amp * 0.5f;
			var scale = new Vector2(0.62f * size, 0.62f * size * (0.8f + 0.2f * (float)Math.Cos(phase)));
			var anchor = new Vector2(4f, 22f);
			Color membrane = VfxMath.Shade(pal.B, 0.32f * bright, 1.2f);
			Anchored(ref info, shader, VfxAssets.BatWing, null, f.Shoulder, anchor, rot, scale * 1.04f, VfxMath.Additive(pal.A, 0.22f * bright), f);
			Anchored(ref info, shader, VfxAssets.BatWing, null, f.Shoulder, anchor, rot, scale, membrane * 0.95f, f);
			Anchored(ref info, shader, VfxAssets.BatBones, null, f.Shoulder, anchor, rot, scale, VfxMath.Additive(pal.A, 0.9f * bright), f);
			Anchored(ref info, shader, VfxAssets.BatBones, null, f.Shoulder, anchor, rot, scale, VfxMath.Additive(pal.Hi, 0.35f * bright), f);
		}

		/// <summary>Крылья света (как у Императрицы): веер клинков из плеча.</summary>
		private static void DrawLightBlades(ref PlayerDrawSet info, WingLook wl, Frame f, float phase, float amp, float size, float angleOffset,
			float bright, int shader, float time)
		{
			Palette pal = wl.Pal;
			Texture2D blade = VfxAssets.Blade;
			const int blades = 9;
			float sweep = (float)Math.Sin(phase) * amp * 0.35f;
			for (int i = 0; i < blades; i++)
			{
				float k = i / (float)(blades - 1);
				float angle = MathHelper.Lerp(-1.05f, 0.75f, k) - sweep * (1f - k * 0.5f) + angleOffset;
				float len = (22f + 24f * (float)Math.Pow(1f - k, 0.8f) + 3f * (float)Math.Sin(time * 3f + i)) * size;
				Color c = wl.Rainbow
					? Main.hslToRgb(VfxMath.Frac(time * 0.25f + k * 0.8f), 0.8f, 0.72f)
					: VfxMath.LerpVivid(pal.A, pal.B, k);
				// клинки начинаются чуть в стороне от плеча, чтобы у основания не было белого пятна
				Vector2 dir = angle.ToRotationVector2();
				Vector2 basePos = f.ToWorld(dir * 8f * size);
				var scale = new Vector2(len / 128f, 0.8f * size);
				Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), angle, scale * new Vector2(1f, 1.6f), VfxMath.Additive(c, 0.38f * bright), f);
				Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), angle, scale * new Vector2(0.92f, 0.5f), VfxMath.Additive(Color.Lerp(c, Color.White, 0.5f), 0.35f * bright), f);

				Vector2 tip = f.ToWorld(dir * (8f * size + len * 0.95f));
				float twinkle = 0.5f + 0.5f * (float)Math.Sin(time * 4f + i * 1.7f);
				Anchored(ref info, shader, VfxAssets.Glint, null, tip, new Vector2(32f, 32f), 0f, new Vector2(0.18f * size * (0.6f + twinkle)),
					VfxMath.Additive(Color.White, 0.6f * bright * twinkle), f);
			}

			Texture2D bloom = VfxAssets.Bloom;
			info.DrawDataCache.Add(new DrawData(bloom, f.Shoulder, null, VfxMath.Additive(pal.A, 0.2f * bright), 0f, bloom.Size() / 2f, 0.3f * size, SpriteEffects.None, 0) { shader = shader });
		}
	}

	/// <summary>Слой отрисовки крыльев-эффектов (позади тела, на месте обычных крыльев).</summary>
	public class VfxWingsLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.HairBack);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (!LayerUtils.Visible(drawInfo))
				return false;
			return LayerUtils.Mp(drawInfo).TryGetWings(out _);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			LooksPlayer mp = LayerUtils.Mp(drawInfo);
			if (mp.TryGetWings(out WingLook wl))
				WingEffects.Draw(ref drawInfo, mp, wl);
		}
	}
}
