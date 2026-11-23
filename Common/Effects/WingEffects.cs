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
	/// <summary>Состояние крыльев в данный момент (плавно меняется в LooksPlayer).</summary>
	public struct WingState
	{
		/// <summary>Фаза цикла взмаха (радианы).</summary>
		public float Phase;
		/// <summary>Сила взмаха 0..1.</summary>
		public float Amp;
		/// <summary>0 — крылья сложены за спиной, 1 — раскрыты.</summary>
		public float Spread;
		/// <summary>Отклонение назад от скорости бега 0..1.</summary>
		public float Lean;

		public WingState Delayed(float lag)
		{
			WingState s = this;
			s.Phase -= lag;
			return s;
		}
	}

	/// <summary>Одно «перо» крыла в локальных координатах: X — назад от лица, Y — вниз.</summary>
	public struct WingElement
	{
		public Vector2 Base;
		public float Angle;
		public float Length;
		/// <summary>0 — маховые (сзади), 1 — второстепенные, 2 — кроющие (спереди).</summary>
		public int Layer;
		/// <summary>Положение в ряду 0..1.</summary>
		public float K;
		/// <summary>Положение вдоль крыла 0..1 (от плеча к кончику).</summary>
		public float U;
		public int Index;

		public Vector2 Tip => Base + Angle.ToRotationVector2() * Length;
	}

	/// <summary>
	/// «Скелет» крыла: плечо → запястье → кончик и три ряда перьев.
	/// Анимация:
	/// - взмах несимметричный: вниз быстро (40% цикла), вверх медленно;
	/// - волна идёт от плеча к кончику: дальние перья запаздывают и изгибаются от «сопротивления воздуха»;
	/// - Spread складывает крылья за спиной (на земле) и раскрывает (в полёте, при планировании);
	/// - Lean отводит крылья назад при беге.
	/// </summary>
	public static class WingRig
	{
		public const int MaxElements = 24;

		/// <summary>Волна взмаха: -1 — крылья вверху, +1 — внизу.</summary>
		public static float Wave(float phase)
		{
			float p = VfxMath.Frac(phase / MathHelper.TwoPi);
			if (p < 0.4f)
				return -1f + 2f * VfxMath.Ease(p / 0.4f);
			return 1f - 2f * VfxMath.Ease((p - 0.4f) / 0.6f);
		}

		/// <summary>Скорость волны (для изгиба перьев).</summary>
		public static float WaveVelocity(float phase) => (Wave(phase) - Wave(phase - 0.25f)) / 0.25f;

		public static float ArmAngle(WingState s) =>
			-0.35f + Wave(s.Phase) * s.Amp * 0.6f + (1f - s.Spread) * 0.55f + s.Lean * 0.25f;

		public static float ForearmAngle(WingState s) =>
			ArmAngle(s.Delayed(0.35f)) + MathHelper.Lerp(1.25f, 0.12f, s.Spread);

		/// <summary>Точки скелета (плечо в нуле): запястье и кончик.</summary>
		public static void Skeleton(WingState s, float size, out Vector2 wrist, out Vector2 tip)
		{
			wrist = ArmAngle(s).ToRotationVector2() * 13f * size;
			tip = wrist + ForearmAngle(s).ToRotationVector2() * 16f * size * MathHelper.Lerp(0.85f, 1f, s.Spread);
		}

		private static Vector2 OnArm(float u, Vector2 wrist, Vector2 tip) =>
			u < 0.5f ? Vector2.Lerp(Vector2.Zero, wrist, u / 0.5f) : Vector2.Lerp(wrist, tip, (u - 0.5f) / 0.5f);

		/// <summary>Заполняет buffer перьями (сначала задний ряд). Возвращает количество.</summary>
		public static int Build(WingElement[] buffer, WingState s, float size)
		{
			int n = 0;
			float sp = s.Spread;
			n = Row(buffer, n, s, size, 0, 7, 0.5f, 0.98f);
			n = Row(buffer, n, s, size, 1, 6, 0.08f, 0.5f);
			n = Row(buffer, n, s, size, 2, 6, 0.05f, 0.8f);
			return n;
		}

		private static int Row(WingElement[] buffer, int n, WingState s, float size, int layer, int count, float u0, float u1)
		{
			float sp = s.Spread;
			for (int i = 0; i < count; i++)
			{
				float k = i / (float)(count - 1);
				float u = MathHelper.Lerp(u0, u1, k);
				// дальше от плеча — позже по фазе: по крылу бежит волна
				WingState d = s.Delayed(u * 0.75f);
				Skeleton(d, size, out Vector2 wrist, out Vector2 tip);
				float fore = ForearmAngle(d);
				float bend = -WaveVelocity(d.Phase) * s.Amp * 0.07f * (0.3f + u);

				float angle, length;
				switch (layer)
				{
					case 0:
						angle = MathHelper.Lerp(fore + MathHelper.Lerp(0.3f, 1.15f, sp), fore - 0.1f, k) + bend;
						length = MathHelper.Lerp(18f, 28f, (float)Math.Sin(k * MathHelper.PiOver2)) * size * MathHelper.Lerp(0.9f, 1f, sp);
						break;
					case 1:
						angle = MathHelper.Lerp(MathHelper.Lerp(1.15f, 1.45f, sp), fore + MathHelper.Lerp(0.35f, 1.2f, sp), k) + bend * 0.6f;
						length = MathHelper.Lerp(14f, 18f, k) * size;
						break;
					default:
						angle = MathHelper.Lerp(1.0f, fore + 0.55f, k) + bend * 0.3f;
						length = MathHelper.Lerp(9f, 12f, k) * size;
						break;
				}

				buffer[n++] = new WingElement
				{
					Base = OnArm(u, wrist, tip),
					Angle = angle,
					Length = length,
					Layer = layer,
					K = k,
					U = u,
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

		/// <summary>Плечо на экране/в мире, сторона «назад» (1 или -1) и гравитация (1 или -1).</summary>
		private struct Frame
		{
			public Vector2 Shoulder;
			public int Back;
			public int Grav;

			public Vector2 ToWorld(Vector2 local) => Shoulder + new Vector2(local.X * Back, local.Y * Grav);
		}

		/// <summary>Как рисуется одно крыло: ближнее — ярко, дальнее — темнее, холоднее, меньше и выше.</summary>
		private struct WingPass
		{
			public Frame F;
			public WingState S;
			public float Size;
			public float AngleOffset;
			public bool Far;
		}

		private static Frame MakeFrame(Player p, Vector2 center) => new Frame
		{
			Shoulder = center + new Vector2(-p.direction * 5f, -7f * p.gravDir),
			Back = -p.direction,
			Grav = p.gravDir < 0 ? -1 : 1
		};

		/// <summary>Цвет дальнего крыла: темнее и ближе к «тени» палитры — так оно уходит на задний план.</summary>
		private static Color Depth(Color c, Palette pal, bool far) =>
			far ? VfxMath.Shade(VfxMath.LerpVivid(c, pal.Lo, 0.45f), 0.6f, 1.1f) : c;

		// ======================= частицы =======================

		public static void Update(LooksPlayer mp)
		{
			if (!mp.TryGetWings(out WingLook wl))
				return;

			Player p = mp.Player;
			Frame f = MakeFrame(p, p.Center);
			WingState st = mp.WingState;
			WingRig.Skeleton(st, wl.Size, out Vector2 wrist, out Vector2 tipLocal);
			Vector2 tip = f.ToWorld(tipLocal);
			Vector2 mid = f.ToWorld(Vector2.Lerp(wrist, tipLocal, 0.5f) + new Vector2(0f, 10f * wl.Size));
			Palette pal = wl.Pal;

			// Порыв в начале каждого удара вниз: перья/искры срываются с кончиков
			int cycle = (int)(st.Phase / MathHelper.TwoPi);
			bool gust = cycle != mp.WingLastCycle && st.Amp > 0.5f;
			mp.WingLastCycle = cycle;
			if (gust)
			{
				for (int i = 0; i < 4; i++)
				{
					Vector2 pos = Vector2.Lerp(tip, mid, i / 3f);
					Vector2 vel = new Vector2(f.Back * 0.6f, 1.4f * f.Grav) + Main.rand.NextVector2Circular(0.4f, 0.4f);
					SpawnStyleParticle(wl, pal, p, pos, vel, 1.3f);
				}
			}

			float intensity = Fx.GlobalIntensity * MathHelper.Lerp(0.35f, 1f, st.Amp);
			if (Main.rand.NextFloat() < 0.25f * intensity)
				SpawnStyleParticle(wl, pal, p, Vector2.Lerp(tip, mid, Main.rand.NextFloat()), Vector2.Zero, 1f);
		}

		private static void SpawnStyleParticle(WingLook wl, Palette pal, Player p, Vector2 spot, Vector2 push, float power)
		{
			const bool front = false;
			switch (wl.Style)
			{
				case WingStyle.Seraph:
					if (Main.rand.NextBool(6))
						ParticleSystem.Spawn(new ShapeParticle(Shape.Feather, SpriteMotion.Sway, spot, push, new Ramp(pal.Hi, pal.A), 0.8f, 120) { GlowRamp = pal.B, Front = front });
					else
						ParticleSystem.Spawn(new GlowParticle(spot, push + new Vector2(0f, 0.3f), pal.Glow, Main.rand.NextFloat(0.25f, 0.4f) * power, 40) { Front = front });
					break;
				case WingStyle.Light:
				{
					Color c = wl.Rainbow ? Main.hslToRgb(Main.rand.NextFloat(), 0.8f, 0.75f) : pal.Mix(Main.rand.NextFloat());
					ParticleSystem.Spawn(new FlareParticle(spot, push + Main.rand.NextVector2Circular(0.4f, 0.4f), new Ramp(Color.White, c), Main.rand.NextFloat(0.3f, 0.5f) * power, 28) { Front = front });
					break;
				}
				case WingStyle.Butterfly:
					ParticleSystem.Spawn(new GlowParticle(spot, push + new Vector2(0f, 0.4f * p.gravDir), pal.Soft, Main.rand.NextFloat(0.15f, 0.28f) * power, 45) { Blink = 0.3f, Front = front });
					break;
				case WingStyle.Demon:
					if (Main.rand.NextBool())
						ParticleSystem.Spawn(new SmokeParticle(spot, push + new Vector2(0f, -0.4f * p.gravDir), new Ramp(VfxMath.Shade(pal.B, 0.3f), Color.Black), 0.5f, 50) { MaxOpacity = 0.35f, Front = front });
					else
						ParticleSystem.Spawn(new GlowParticle(spot, push + new Vector2(0f, -0.5f * p.gravDir), pal.Hot, 0.28f * power, 40) { Wander = 0.05f, Front = front });
					break;
				case WingStyle.Flame:
					ParticleSystem.Spawn(new GlowParticle(spot, push + new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.8f, 1.8f) * p.gravDir), pal.Hot, Main.rand.NextFloat(0.22f, 0.4f) * power, 45) { Wander = 0.06f, Front = front });
					break;
				case WingStyle.Crystal:
					if (Main.rand.NextBool())
						ParticleSystem.Spawn(new ShapeParticle(Shape.ShardB, SpriteMotion.Spin, spot, push + new Vector2(0f, 0.8f * p.gravDir), new Ramp(pal.Hi, pal.A, pal.B), 0.42f * power, 50) { Glowing = true, Spin = 0.1f, Front = front });
					else
						ParticleSystem.Spawn(new FlareParticle(spot, push, new Ramp(Color.White, pal.A), 0.38f * power, 26) { Front = front });
					break;
				case WingStyle.Constellation:
					ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Spin, spot, push + Main.rand.NextVector2Circular(0.3f, 0.3f), new Ramp(Color.White, pal.A, pal.B), 0.38f * power, 50) { Glowing = true, Spin = 0.05f, Front = front });
					break;
				case WingStyle.Spirit:
					ParticleSystem.Spawn(new GlowParticle(spot, push - p.velocity * 0.1f + new Vector2(0f, -0.3f * p.gravDir), pal.Glow, Main.rand.NextFloat(0.3f, 0.45f) * power, 50) { Drag = 0.97f, Core = false, Halo = 0.4f, Front = front });
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
			WingState st = mp.WingState;

			// Дальнее крыло: плечо чуть вперёд и выше, запаздывает по фазе, поднято, меньше и темнее
			var far = new WingPass
			{
				F = new Frame { Shoulder = f.ToWorld(new Vector2(-4f, -3f)), Back = f.Back, Grav = f.Grav },
				S = st.Delayed(0.3f),
				Size = wl.Size * 0.88f,
				AngleOffset = -0.45f,
				Far = true
			};
			var near = new WingPass { F = f, S = st, Size = wl.Size, AngleOffset = 0f, Far = false };

			DrawWing(ref info, wl, far, shader);
			DrawWing(ref info, wl, near, shader);
		}

		private static void DrawWing(ref PlayerDrawSet info, WingLook wl, WingPass w, int shader)
		{
			switch (wl.Style)
			{
				case WingStyle.Butterfly:
					DrawButterfly(ref info, wl, w, shader);
					return;
				case WingStyle.Demon:
					DrawBat(ref info, wl, w, shader);
					return;
				case WingStyle.Light:
					DrawLightBlades(ref info, wl, w, shader);
					return;
			}

			float time = Main.GlobalTimeWrappedHourly;
			Palette pal = wl.Pal;
			Frame wf = w.F;
			float size = w.Size;
			float glowMul = w.Far ? 0.5f : 1f;
			int count = WingRig.Build(Buffer, w.S, size);

			for (int i = 0; i < count; i++)
			{
				WingElement e = Buffer[i];
				// у дальнего крыла кроющие перья не видны — силуэт чище
				if (w.Far && e.Layer == 2 && wl.Style != WingStyle.Constellation)
					continue;
				e.Angle += w.AngleOffset;
				e.Base = e.Base.RotatedBy(w.AngleOffset);
				Vector2 basePos = wf.ToWorld(e.Base);
				// градиент вдоль крыла: у тела — основной цвет, к кончикам маховых — светлее
				float k = e.Layer == 0 ? 0.3f + 0.4f * e.K : e.Layer == 1 ? 0.2f + 0.25f * e.K : 0.15f + 0.2f * e.K;

				switch (wl.Style)
				{
					case WingStyle.Seraph:
					{
						bool covert = e.Layer == 2;
						Texture2D tex = covert ? VfxAssets.Covert : VfxAssets.Feather;
						Texture2D glow = covert ? VfxAssets.CovertGlow : VfxAssets.FeatherGlow;
						float texLen = covert ? 30f : 60f;
						var scale = new Vector2(e.Length / texLen, (covert ? 0.62f : 0.95f) * size);
						Color fill = Depth(VfxMath.LerpVivid(pal.A, pal.Hi, k), pal, w.Far);
						Anchored(ref info, shader, glow, null, basePos, new Vector2(2f, 8f), e.Angle, scale * new Vector2(1.06f, 1.4f),
							VfxMath.Additive(pal.B, 0.4f * glowMul), wf);
						Anchored(ref info, shader, tex, null, basePos, new Vector2(2f, 8f), e.Angle, scale, fill, wf);
						if (!w.Far && e.Layer == 0 && (float)Math.Sin(time * 3f + e.Index * 1.9f) > 0.8f)
							Anchored(ref info, shader, VfxAssets.Glint, null, wf.ToWorld(e.Tip), new Vector2(32f, 32f), time, new Vector2(0.22f * size),
								VfxMath.Additive(Color.White, 0.6f), wf);
						break;
					}
					case WingStyle.Flame:
					{
						Texture2D flame = VfxAssets.Flame;
						Rectangle frame = VfxAssets.FlameFrame((int)(time * 10f) + e.Index + e.Layer * 2);
						float rot = e.Angle + MathHelper.PiOver2;
						float width = (e.Layer == 2 ? 0.45f : 0.6f) * size;
						Color outer = Depth(VfxMath.LerpVivid(pal.A, pal.B, 1f - k), pal, w.Far);
						Anchored(ref info, shader, flame, frame, basePos, new Vector2(16f, 46f), rot, new Vector2(width, e.Length / 44f),
							VfxMath.Additive(outer, 0.6f * glowMul + 0.15f), wf);
						if (!w.Far)
							Anchored(ref info, shader, flame, frame, basePos, new Vector2(16f, 46f), rot, new Vector2(width * 0.5f, e.Length / 44f * 0.65f),
								VfxMath.Additive(pal.Hi, 0.4f), wf);
						break;
					}
					case WingStyle.Crystal:
					{
						Rectangle frame = Shapes.Frame(Shape.ShardA);
						float rot = e.Angle + MathHelper.PiOver2;
						var scale = new Vector2(0.8f * size, e.Length / 24f);
						Anchored(ref info, shader, VfxAssets.ShapesGlow, frame, basePos, new Vector2(16f, 28f), rot, scale * 1.15f,
							VfxMath.Additive(pal.B, 0.45f * glowMul), wf);
						Color fill = Depth(VfxMath.LerpVivid(pal.A, pal.Hi, k), pal, w.Far);
						Anchored(ref info, shader, VfxAssets.Shapes, frame, basePos, new Vector2(16f, 28f), rot, scale, fill * 0.92f, wf);
						if (!w.Far && e.Layer < 2 && (float)Math.Sin(time * 2.6f + e.Index * 2.3f + e.Layer) > 0.85f)
							Anchored(ref info, shader, VfxAssets.Glint, null, wf.ToWorld(e.Tip), new Vector2(32f, 32f), 0f, new Vector2(0.28f * size),
								VfxMath.Additive(Color.White, 0.7f), wf);
						break;
					}
					case WingStyle.Spirit:
					{
						Texture2D blade = VfxAssets.Blade;
						float wave = (float)Math.Sin(time * 2.4f + e.Index * 0.7f + e.Layer) * 0.06f;
						var scale = new Vector2(e.Length * 1.3f / 128f, 0.75f * size);
						Color c = Depth(VfxMath.LerpVivid(pal.A, pal.B, k), pal, w.Far);
						Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), e.Angle + wave, scale * new Vector2(1f, 1.6f),
							VfxMath.Additive(c, 0.35f * glowMul + 0.05f), wf);
						if (!w.Far)
							Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), e.Angle + wave, scale * new Vector2(0.9f, 0.6f),
								VfxMath.Additive(Color.Lerp(c, Color.White, 0.4f), 0.3f), wf);
						break;
					}
					case WingStyle.Constellation:
					{
						if (e.Layer == 2)
							break;
						Texture2D bloom = VfxAssets.Bloom;
						Vector2 tipPos = wf.ToWorld(e.Tip);
						Anchored(ref info, shader, bloom, null, Vector2.Lerp(basePos, tipPos, 0.5f), new Vector2(64f, 64f), e.Angle,
							new Vector2(e.Length / 110f, 0.12f * size), VfxMath.Additive(pal.B, 0.18f * glowMul), wf);
						if (i + 1 < count && Buffer[i + 1].Layer == e.Layer)
						{
							WingElement next = Buffer[i + 1];
							next.Angle += w.AngleOffset;
							next.Base = next.Base.RotatedBy(w.AngleOffset);
							DrawLine(ref info, shader, tipPos, wf.ToWorld(next.Tip), VfxMath.Additive(Depth(pal.A, pal, w.Far), 0.5f * glowMul + 0.1f), 1.3f);
						}
						float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 4f + e.Index * 2.1f + e.Layer * 3f);
						Color star = Depth(VfxMath.LerpVivid(pal.Hi, pal.A, k), pal, w.Far);
						Anchored(ref info, shader, VfxAssets.Flare, null, tipPos, new Vector2(64f, 64f), time * 0.3f, new Vector2(0.12f * size * twinkle),
							VfxMath.Additive(star, glowMul), wf);
						if (!w.Far)
							Anchored(ref info, shader, VfxAssets.Glint, null, tipPos, new Vector2(32f, 32f), 0f, new Vector2(0.22f * size * twinkle),
								VfxMath.Additive(Color.White, 0.55f), wf);
						break;
					}
				}
			}

			if (wl.Style == WingStyle.Constellation)
			{
				// «кость» созвездия: плечо → запястье → кончик
				WingRig.Skeleton(w.S, size, out Vector2 wrist, out Vector2 tip);
				Color bone = VfxMath.Additive(Depth(pal.Hi, pal, w.Far), 0.55f * glowMul + 0.1f);
				Vector2 s1 = wf.ToWorld(wrist.RotatedBy(w.AngleOffset));
				Vector2 s2 = wf.ToWorld(tip.RotatedBy(w.AngleOffset));
				DrawLine(ref info, shader, wf.Shoulder, s1, bone, 1.5f);
				DrawLine(ref info, shader, s1, s2, bone, 1.5f);
			}
		}

		private static void DrawLine(ref PlayerDrawSet info, int shader, Vector2 a, Vector2 b, Color color, float thickness)
		{
			Texture2D line = VfxAssets.Line;
			Vector2 diff = b - a;
			info.DrawDataCache.Add(new DrawData(line, a, null, color, diff.ToRotation(), new Vector2(0f, line.Height / 2f),
				new Vector2((diff.Length() + 1f) / line.Width, thickness / line.Height), SpriteEffects.None, 0) { shader = shader });
		}

		/// <summary>
		/// Крылья бабочки. Взмах — крыло поворачивается к зрителю ребром (сжатие по горизонтали):
		/// внизу раскрыто, вверху почти сложено. На земле сложены «домиком».
		/// </summary>
		private static void DrawButterfly(ref PlayerDrawSet info, WingLook wl, WingPass w, int shader)
		{
			Palette pal = wl.Pal;
			WingState s = w.S;
			float wave = WingRig.Wave(s.Phase);
			float flying = 0.3f + 0.7f * (0.5f + 0.5f * wave);
			float open = MathHelper.Lerp(0.3f, MathHelper.Lerp(1f, flying, s.Amp), s.Spread);
			float rot = -0.25f + w.AngleOffset + wave * s.Amp * 0.15f + s.Lean * 0.2f + (1f - s.Spread) * 0.35f;
			var scale = new Vector2(0.8f * w.Size * open, 0.8f * w.Size);
			var anchor = new Vector2(4f, 32f);
			float glowMul = w.Far ? 0.5f : 1f;
			Anchored(ref info, shader, VfxAssets.ButterflyGlow, null, w.F.Shoulder, anchor, rot, scale * 1.05f, VfxMath.Additive(pal.B, 0.45f * glowMul), w.F);
			Anchored(ref info, shader, VfxAssets.ButterflyWing, null, w.F.Shoulder, anchor, rot, scale, Depth(pal.A, pal, w.Far) * 0.9f, w.F);
			Anchored(ref info, shader, VfxAssets.ButterflyPattern, null, w.F.Shoulder, anchor, rot, scale, Depth(pal.B, pal, w.Far), w.F);
			if (!w.Far)
				Anchored(ref info, shader, VfxAssets.ButterflyPattern, null, w.F.Shoulder, anchor, rot, scale, VfxMath.Additive(pal.Hi, 0.3f), w.F);
		}

		/// <summary>Крылья демона: тёмная перепонка и светящиеся кости. Сложены — перепонка поджата.</summary>
		private static void DrawBat(ref PlayerDrawSet info, WingLook wl, WingPass w, int shader)
		{
			Palette pal = wl.Pal;
			WingState s = w.S;
			float wave = WingRig.Wave(s.Phase);
			float rot = -0.3f + w.AngleOffset + wave * s.Amp * 0.55f + (1f - s.Spread) * 0.75f + s.Lean * 0.25f;
			float fold = MathHelper.Lerp(0.55f, 1f, s.Spread);
			var scale = new Vector2(0.62f * w.Size * MathHelper.Lerp(0.75f, 1f, s.Spread), 0.62f * w.Size * fold * (0.88f + 0.12f * wave * s.Amp));
			var anchor = new Vector2(4f, 22f);
			float glowMul = w.Far ? 0.5f : 1f;
			Color membrane = Depth(VfxMath.Shade(pal.B, 0.32f, 1.2f), pal, w.Far);
			Anchored(ref info, shader, VfxAssets.BatWing, null, w.F.Shoulder, anchor, rot, scale * 1.04f, VfxMath.Additive(pal.A, 0.22f * glowMul), w.F);
			Anchored(ref info, shader, VfxAssets.BatWing, null, w.F.Shoulder, anchor, rot, scale, membrane * 0.95f, w.F);
			Anchored(ref info, shader, VfxAssets.BatBones, null, w.F.Shoulder, anchor, rot, scale, VfxMath.Additive(Depth(pal.A, pal, w.Far), 0.9f * glowMul), w.F);
			if (!w.Far)
				Anchored(ref info, shader, VfxAssets.BatBones, null, w.F.Shoulder, anchor, rot, scale, VfxMath.Additive(pal.Hi, 0.3f), w.F);
		}

		/// <summary>Крылья света (как у Императрицы): веер клинков; по вееру бежит волна, сложены — веер прижат.</summary>
		private static void DrawLightBlades(ref PlayerDrawSet info, WingLook wl, WingPass w, int shader)
		{
			Palette pal = wl.Pal;
			Texture2D blade = VfxAssets.Blade;
			float time = Main.GlobalTimeWrappedHourly;
			float size = w.Size;
			float glowMul = w.Far ? 0.5f : 1f;
			const int blades = 9;
			for (int i = 0; i < blades; i++)
			{
				float k = i / (float)(blades - 1);
				WingState d = w.S.Delayed((1f - k) * 0.6f);
				float sweep = WingRig.Wave(d.Phase) * d.Amp * 0.35f;
				float fan = MathHelper.Lerp(MathHelper.Lerp(0.55f, 1.15f, k), MathHelper.Lerp(-1.05f, 0.75f, k), w.S.Spread);
				float angle = fan + sweep * (1f - k * 0.5f) + w.AngleOffset + w.S.Lean * 0.25f;
				float len = (22f + 24f * (float)Math.Pow(1f - k, 0.8f) + 3f * (float)Math.Sin(time * 3f + i)) * size * MathHelper.Lerp(0.8f, 1f, w.S.Spread);
				Color c = wl.Rainbow
					? Main.hslToRgb(VfxMath.Frac(time * 0.25f + k * 0.8f), 0.8f, 0.72f)
					: VfxMath.LerpVivid(pal.A, pal.B, k);
				c = Depth(c, pal, w.Far);
				// клинки начинаются чуть в стороне от плеча, чтобы у основания не было белого пятна
				Vector2 dir = angle.ToRotationVector2();
				Vector2 basePos = w.F.ToWorld(dir * 5f * size);
				var scale = new Vector2(len / 128f, 0.8f * size);
				Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), angle, scale * new Vector2(1f, 1.6f), VfxMath.Additive(c, 0.38f * glowMul + 0.05f), w.F);
				if (w.Far)
					continue;
				Anchored(ref info, shader, blade, null, basePos, new Vector2(0f, 8f), angle, scale * new Vector2(0.92f, 0.5f), VfxMath.Additive(Color.Lerp(c, Color.White, 0.5f), 0.35f), w.F);

				Vector2 tip = w.F.ToWorld(dir * (5f * size + len * 0.95f));
				float twinkle = 0.5f + 0.5f * (float)Math.Sin(time * 4f + i * 1.7f);
				Anchored(ref info, shader, VfxAssets.Glint, null, tip, new Vector2(32f, 32f), 0f, new Vector2(0.18f * size * (0.6f + twinkle)),
					VfxMath.Additive(Color.White, 0.6f * twinkle), w.F);
			}

			if (!w.Far)
			{
				Texture2D bloom = VfxAssets.Bloom;
				info.DrawDataCache.Add(new DrawData(bloom, w.F.Shoulder, null, VfxMath.Additive(pal.A, 0.28f), 0f, bloom.Size() / 2f, 0.26f * size, SpriteEffects.None, 0) { shader = shader });
			}
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
