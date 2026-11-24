using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using BeautifulLooks.Common.Looks;
using BeautifulLooks.Common.Players;
using BeautifulLooks.Common.VFX;

namespace BeautifulLooks.Common.Effects
{
	/// <summary>
	/// Спутники из света (только для красоты, не предмет и не бафф).
	/// Движение — «пружина» к точке рядом с персонажем (у каждого стиля своя траектория);
	/// путь пишется в LooksPlayer.CompanionTrail (хвост огонька, хвосты лисицы, тело змея).
	/// Рисуются слоем игрока (видны и в превью меню) — относительно центра персонажа.
	/// </summary>
	public static class CompanionEffects
	{
		public const int TrailLength = 32;

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		/// <summary>Куда стремится спутник (мир).</summary>
		private static Vector2 Target(CompanionStyle style, Player p, float s, float t)
		{
			float back = -p.direction;
			Vector2 offset = style switch
			{
				CompanionStyle.Wisp => new Vector2(back * 28f + (float)Math.Sin(t * 1.7f) * 6f, -30f + (float)Math.Sin(t * 2.3f) * 5f),
				CompanionStyle.Fox => new Vector2(back * 36f, -10f + (float)Math.Sin(t * 2.2f) * 4f),
				CompanionStyle.Butterfly => new Vector2((float)Math.Sin(t * 0.8f) * 34f, -34f + (float)Math.Sin(t * 1.6f) * 10f),
				_ => new Vector2((float)Math.Sin(t * 1.05f) * 50f, -14f + (float)Math.Sin(t * 2.1f) * 20f) // змей: «восьмёрка» вокруг
			};
			offset.Y *= p.gravDir;
			return p.Center + offset * s;
		}

		public static void Update(LooksPlayer mp, bool visible)
		{
			Player p = mp.Player;
			LookSettings l = mp.Look;
			if (!visible || l.Companion == CompanionStyle.None)
			{
				mp.CompanionReady = false;
				return;
			}

			float s = l.CompanionSize;
			mp.CompanionClock += 1f / 60f;
			float t = mp.CompanionClock;
			Vector2 target = Target(l.Companion, p, s, t);

			if (!mp.CompanionReady || Vector2.DistanceSquared(mp.CompanionPos, target) > 900f * 900f)
			{
				mp.CompanionPos = target;
				mp.CompanionVel = Vector2.Zero;
				for (int i = 0; i < TrailLength; i++)
					mp.CompanionTrail[i] = target;
				mp.CompanionTrailCount = TrailLength;
				mp.CompanionReady = true;
			}

			float spring, damping, maxSpeed;
			switch (l.Companion)
			{
				case CompanionStyle.Wisp: spring = 0.012f; damping = 0.9f; maxSpeed = 14f; break;
				case CompanionStyle.Fox: spring = 0.01f; damping = 0.88f; maxSpeed = 16f; break;
				case CompanionStyle.Butterfly: spring = 0.02f; damping = 0.9f; maxSpeed = 10f; break;
				default: spring = 0.045f; damping = 0.85f; maxSpeed = 18f; break;
			}

			Vector2 diff = target - mp.CompanionPos;
			mp.CompanionVel += diff * spring;
			if (diff.LengthSquared() > 160f * 160f * s * s)
				mp.CompanionVel += diff * 0.03f; // догоняет, если персонаж умчался
			if (l.Companion == CompanionStyle.Butterfly)
				mp.CompanionVel += Main.rand.NextVector2Circular(0.25f, 0.25f);
			mp.CompanionVel *= damping;
			float limit = maxSpeed + p.velocity.Length();
			if (mp.CompanionVel.LengthSquared() > limit * limit)
				mp.CompanionVel = Vector2.Normalize(mp.CompanionVel) * limit;
			mp.CompanionPos += mp.CompanionVel;

			if (Math.Abs(mp.CompanionVel.X) > 0.5f)
				mp.CompanionFacing = mp.CompanionVel.X > 0f ? 1 : -1;
			else if (l.Companion != CompanionStyle.Butterfly)
				mp.CompanionFacing = p.direction;

			for (int i = TrailLength - 1; i > 0; i--)
				mp.CompanionTrail[i] = mp.CompanionTrail[i - 1];
			mp.CompanionTrail[0] = mp.CompanionPos;
			mp.CompanionAnchor = p.Center;

			if (mp.CompanionBlink > 0)
				mp.CompanionBlink--;
			else if (Main.rand.NextBool(220))
				mp.CompanionBlink = 8;

			Particles(mp, l, s);
		}

		private static void Particles(LooksPlayer mp, LookSettings l, float s)
		{
			Palette pal = Palette.Of(l.CompanionColor, l.CompanionColor2, Main.rand.NextFloat(0.3f));
			Vector2 pos = mp.CompanionPos;
			float g = mp.Player.gravDir;
			Lighting.AddLight(pos, pal.A.ToVector3() * 0.35f * s);
			float rate = Fx.GlobalIntensity;
			switch (l.Companion)
			{
				case CompanionStyle.Wisp:
					if (Main.rand.NextFloat() < 0.2f * rate)
						ParticleSystem.Spawn(new GlowParticle(pos + Main.rand.NextVector2Circular(4f, 4f) * s, new Vector2(R(-0.2f, 0.2f), -R(0.4f, 0.9f) * g), pal.Glow, R(0.12f, 0.2f) * s, 35)
						{ Wander = 0.04f, Front = false });
					break;
				case CompanionStyle.Fox:
					if (Main.rand.NextFloat() < 0.25f * rate)
					{
						Vector2 tail = pos + new Vector2(-mp.CompanionFacing * R(10f, 26f), -R(2f, 14f) * g) * s;
						ParticleSystem.Spawn(new GlowParticle(tail, new Vector2(0f, -R(0.2f, 0.5f) * g) - mp.CompanionVel * 0.1f, new Ramp(pal.Hi, pal.A, pal.B), R(0.12f, 0.2f) * s, 40)
						{ Blink = 0.3f, Front = false });
					}
					break;
				case CompanionStyle.Butterfly:
					if (Main.rand.NextFloat() < 0.3f * rate)
						ParticleSystem.Spawn(new GlowParticle(pos + Main.rand.NextVector2Circular(3f, 3f), new Vector2(R(-0.15f, 0.15f), R(0.2f, 0.5f) * g), pal.Soft, R(0.1f, 0.16f) * s, 45)
						{ Blink = 0.35f, Drag = 0.98f, Front = false });
					break;
				case CompanionStyle.Serpent:
					if (Main.rand.NextFloat() < 0.3f * rate)
					{
						Vector2 at = mp.CompanionTrail[Main.rand.Next(TrailLength)];
						if (Main.rand.NextBool(8))
							ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Spin, at, Main.rand.NextVector2Circular(0.3f, 0.3f), new Ramp(Color.White, pal.A, pal.B), R(0.25f, 0.35f) * s, 40)
							{ Glowing = true, Spin = 0.08f, Front = false });
						else
							ParticleSystem.Spawn(new GlowParticle(at, Main.rand.NextVector2Circular(0.3f, 0.3f), new Ramp(pal.Hi, pal.A, pal.B), R(0.1f, 0.18f) * s, 35) { Front = false });
					}
					break;
			}
		}

		// ======================= отрисовка =======================

		private struct View
		{
			public Vector2 Origin;
			public Vector2 Anchor;

			/// <summary>Мир → экран относительно персонажа (так спутник виден и в превью меню).</summary>
			public Vector2 P(Vector2 world) => Origin + (world - Anchor);
		}

		private static void Add(ref PlayerDrawSet info, Texture2D tex, Vector2 pos, Rectangle? frame, Color color, float rotation, Vector2 origin, Vector2 scale,
			SpriteEffects fx = SpriteEffects.None) =>
			info.DrawDataCache.Add(new DrawData(tex, pos, frame, color, rotation, origin, scale, fx, 0));

		public static void Draw(ref PlayerDrawSet info, LooksPlayer mp)
		{
			LookSettings l = mp.Look;
			var v = new View { Origin = LayerUtils.Center(info), Anchor = mp.CompanionAnchor };
			Palette pal = Palette.Of(l.CompanionColor, l.CompanionColor2);
			float s = l.CompanionSize;
			float t = mp.CompanionClock;
			switch (l.Companion)
			{
				case CompanionStyle.Wisp: DrawWisp(ref info, mp, v, pal, s, t); break;
				case CompanionStyle.Fox: DrawFox(ref info, mp, v, pal, s, t); break;
				case CompanionStyle.Butterfly: DrawButterfly(ref info, mp, v, pal, s, t); break;
				case CompanionStyle.Serpent: DrawSerpent(ref info, mp, v, pal, s, t); break;
			}
		}

		/// <summary>Огонёк: светящееся ядро с глазками, хвост-пламя тянется вверх и назад, вокруг две искорки.</summary>
		private static void DrawWisp(ref PlayerDrawSet info, LooksPlayer mp, View v, Palette pal, float s, float t)
		{
			float g = info.drawPlayer.gravDir;
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D orb = VfxAssets.Orb;
			const int tail = 10;
			for (int i = tail; i >= 1; i--)
			{
				float k = i / (float)tail;
				Vector2 tp = v.P(mp.CompanionTrail[Math.Min(i, TrailLength - 1)]) + new Vector2((float)Math.Sin(t * 5f + i * 0.6f) * i * 0.25f, -i * 1.3f * g) * s;
				Color c = VfxMath.LerpVivid(pal.A, pal.B, k);
				Add(ref info, bloom, tp, null, VfxMath.Additive(pal.B, 0.2f * (1f - k)), 0f, bloom.Size() / 2f, new Vector2(0.2f * (1f - k * 0.6f) * s));
				Add(ref info, orb, tp, null, VfxMath.Additive(c, 0.6f * (1f - k)), 0f, orb.Size() / 2f, new Vector2(0.2f * (1f - k * 0.75f) * s));
			}

			Vector2 head = v.P(mp.CompanionPos);
			float flick = 1f + 0.06f * (float)Math.Sin(t * 13f);
			Add(ref info, bloom, head, null, VfxMath.Additive(pal.B, 0.4f), 0f, bloom.Size() / 2f, new Vector2(0.42f * s));
			Add(ref info, orb, head, null, VfxMath.Additive(pal.A, 0.95f), 0f, orb.Size() / 2f, new Vector2(0.32f * s * flick));
			Add(ref info, orb, head, null, VfxMath.Additive(Color.White, 0.6f), 0f, orb.Size() / 2f, new Vector2(0.15f * s));

			// глазки (смотрят туда же, куда летит огонёк) и моргание
			Texture2D disc = VfxAssets.Disc;
			float face = mp.CompanionFacing;
			float blink = mp.CompanionBlink > 0 ? 0.15f : 1f;
			var eyeColor = new Color(30, 14, 40) * 0.9f;
			for (int e = -1; e <= 1; e += 2)
			{
				Vector2 eye = head + new Vector2(face * 1.5f + e * 2.6f, -0.5f * g) * s;
				Add(ref info, disc, eye, null, eyeColor, 0f, disc.Size() / 2f, new Vector2(0.042f, 0.06f * blink) * s);
			}

			for (int k = 0; k < 2; k++)
			{
				float a = t * 3f + k * MathHelper.Pi;
				Vector2 mote = head + VfxMath.OnEllipse(a, 11f, 4f) * s;
				float depth = 0.5f + 0.5f * (float)Math.Sin(a);
				Add(ref info, orb, mote, null, VfxMath.Additive(pal.Hi, 0.4f + 0.5f * depth), 0f, orb.Size() / 2f, new Vector2(0.07f * s * (0.7f + 0.3f * depth)));
			}
		}

		/// <summary>Лисица-дух: светящийся силуэт в прыжке, три пушистых хвоста с огоньками на кончиках.</summary>
		private static void DrawFox(ref PlayerDrawSet info, LooksPlayer mp, View v, Palette pal, float s, float t)
		{
			int face = mp.CompanionFacing;
			Vector2 body = v.P(mp.CompanionPos);
			float tilt = MathHelper.Clamp(mp.CompanionVel.Y * 0.06f, -0.35f, 0.35f) * face;
			float sc = 0.62f * s;
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D orb = VfxAssets.Orb;

			// хвосты (позади тела)
			Vector2 root = body + new Vector2(-18f * face, -1f).RotatedBy(tilt) * sc;
			const int n = 9;
			for (int j = 0; j < 3; j++)
			{
				float ang = (face > 0 ? MathHelper.Pi + 0.15f + j * 0.32f : -0.15f - j * 0.32f) + tilt;
				// при быстром полёте хвосты вытягиваются назад
				ang -= face * MathHelper.Clamp(Math.Abs(mp.CompanionVel.X) * 0.05f, 0f, 0.4f) * (j - 0.5f) * 0.5f;
				Vector2 pos = root;
				Vector2 dir = ang.ToRotationVector2();
				for (int i = 0; i < n; i++)
				{
					float k = (i + 0.5f) / n;
					float a = ang + (float)Math.Sin(t * 3f + j * 1.1f + i * 0.5f) * 0.12f * i / n * face;
					dir = a.ToRotationVector2();
					pos += dir * 3.6f * s;
					float radius = (1.4f + 2.6f * (float)Math.Pow(Math.Sin(MathHelper.Pi * k), 0.7)) * s * (1f - 0.3f * k);
					Color c = VfxMath.LerpVivid(pal.A, pal.Hi, k * 0.9f);
					Add(ref info, bloom, pos, null, VfxMath.Additive(pal.B, 0.07f), 0f, bloom.Size() / 2f, new Vector2(radius * 2.4f / 64f));
					Add(ref info, orb, pos, null, VfxMath.Additive(c, 0.32f), 0f, orb.Size() / 2f, new Vector2(radius / 11f));
				}
				Rectangle frame = VfxAssets.FlameFrame((int)(t * 10f) + j);
				float flicker = 0.8f + 0.2f * (float)Math.Sin(t * 9f + j * 2f);
				Add(ref info, VfxAssets.Flame, pos, frame, VfxMath.Additive(VfxMath.LerpVivid(pal.Hi, pal.B, 0.4f), 0.6f), dir.ToRotation() + MathHelper.PiOver2,
					new Vector2(16f, 46f), new Vector2(0.22f, 0.24f * flicker) * s);
			}

			// тело
			SpriteEffects fx = face < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Texture2D fox = VfxAssets.Fox;
			Texture2D glow = VfxAssets.FoxGlow;
			Add(ref info, glow, body, null, VfxMath.Additive(pal.B, 0.5f), tilt, fox.Size() / 2f, new Vector2(sc * 1.04f), fx);
			Add(ref info, fox, body, null, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.Hi, 0.25f), 0.9f), tilt, fox.Size() / 2f, new Vector2(sc), fx);

			// блик глаза
			Vector2 eye = body + new Vector2(17.3f * face, -11.8f).RotatedBy(tilt) * sc;
			float blink = mp.CompanionBlink > 0 ? 0.3f : 1f;
			Add(ref info, bloom, eye, null, VfxMath.Additive(pal.Hi, 0.3f), 0f, bloom.Size() / 2f, new Vector2(0.06f * s));
			Add(ref info, VfxAssets.Glint, eye, null, VfxMath.Additive(Color.White, 0.75f * blink), 0f, new Vector2(32f, 32f), new Vector2(0.16f * s, 0.16f * s * blink));
		}

		/// <summary>Большая бабочка из света: крылья раскрываются и складываются, за ней сыплется пыльца.</summary>
		private static void DrawButterfly(ref PlayerDrawSet info, LooksPlayer mp, View v, Palette pal, float s, float t)
		{
			Vector2 body = v.P(mp.CompanionPos);
			float flap = (float)Math.Sin(t * 16f);
			float open = 0.2f + 0.8f * Math.Abs(flap);
			float sc = 0.42f * s;
			float tilt = MathHelper.Clamp(mp.CompanionVel.X * 0.08f, -0.4f, 0.4f);
			var scale = new Vector2(sc * open, sc);
			for (int side = -1; side <= 1; side += 2)
			{
				SpriteEffects fx = side < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				var origin = new Vector2(side < 0 ? 60f : 4f, 32f);
				Add(ref info, VfxAssets.ButterflyGlow, body, null, VfxMath.Additive(pal.B, 0.55f), tilt, origin, scale * 1.06f, fx);
				Add(ref info, VfxAssets.ButterflyWing, body, null, pal.A * 0.9f, tilt, origin, scale, fx);
				Add(ref info, VfxAssets.ButterflyPattern, body, null, pal.B, tilt, origin, scale, fx);
				Add(ref info, VfxAssets.ButterflyPattern, body, null, VfxMath.Additive(pal.Hi, 0.35f), tilt, origin, scale, fx);
			}
			Texture2D streak = VfxAssets.Streak;
			Add(ref info, streak, body, null, VfxMath.Additive(pal.Hi, 0.9f), MathHelper.PiOver2 + tilt, streak.Size() / 2f, new Vector2(0.14f, 0.16f) * s);
			Add(ref info, VfxAssets.Glint, body + new Vector2(0f, -6f).RotatedBy(tilt) * s, null, VfxMath.Additive(Color.White, 0.5f), 0f, new Vector2(32f, 32f), new Vector2(0.12f * s));
		}

		/// <summary>Небесный змей: светящееся тело из звеньев, плавники, рожки, усы и огонёк на хвосте.</summary>
		private static void DrawSerpent(ref PlayerDrawSet info, LooksPlayer mp, View v, Palette pal, float s, float t)
		{
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D orb = VfxAssets.Orb;
			const int segments = 14;
			const int step = 2;

			// хребет — тонкая светлая линия через все звенья
			for (int j = segments - 1; j > 0; j--)
			{
				Vector2 a = v.P(mp.CompanionTrail[j * step]);
				Vector2 b = v.P(mp.CompanionTrail[(j - 1) * step]);
				float k = j / (float)(segments - 1);
				LayerUtils.Line(ref info, 0, a, b, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.B, k), 0.35f), (9f - 5f * k) * s);
			}

			for (int j = segments - 1; j >= 0; j--)
			{
				int idx = j * step;
				Vector2 sp = v.P(mp.CompanionTrail[idx]);
				Vector2 dir = (mp.CompanionTrail[Math.Max(0, idx - 1)] - mp.CompanionTrail[Math.Min(TrailLength - 1, idx + 1)]).SafeNormalize(Vector2.UnitX);
				var perp = new Vector2(-dir.Y, dir.X);
				float k = j / (float)(segments - 1);
				float radius = (5.5f - 3.6f * k) * s;
				Color c = VfxMath.LerpVivid(pal.A, pal.B, k);
				Add(ref info, bloom, sp, null, VfxMath.Additive(pal.B, 0.2f), 0f, bloom.Size() / 2f, new Vector2(radius * 2.2f / 64f));
				Add(ref info, orb, sp, null, VfxMath.Additive(c, 0.8f), 0f, orb.Size() / 2f, new Vector2(radius / 11f));

				if (j % 3 == 1 && j < segments - 1)
				{
					// плавники-пёрышки
					float wave = (float)Math.Sin(t * 6f - j * 0.8f) * 0.25f;
					for (int f = -1; f <= 1; f += 2)
					{
						Vector2 tip = sp + (perp * f * 1f - dir * 0.8f).RotatedBy(wave * f) * radius * 1.6f;
						LayerUtils.Line(ref info, 0, sp, tip, VfxMath.Additive(pal.Hi, 0.5f), 3f * s);
					}
				}

				if (j == segments - 1)
				{
					Rectangle frame = VfxAssets.FlameFrame((int)(t * 10f));
					Add(ref info, VfxAssets.Flame, sp, frame, VfxMath.Additive(pal.B, 0.55f), (-dir).ToRotation() + MathHelper.PiOver2, new Vector2(16f, 46f), new Vector2(0.25f, 0.3f) * s);
				}
			}

			// голова
			Vector2 head = v.P(mp.CompanionTrail[0]);
			Vector2 hd = (mp.CompanionTrail[0] - mp.CompanionTrail[2]).SafeNormalize(Vector2.UnitX);
			var hp = new Vector2(-hd.Y, hd.X);
			Rectangle wisp = Shapes.Frame(Shape.Wisp);
			float rot = hd.ToRotation(); // форма Wisp смотрит головой вправо (+X)
			Add(ref info, VfxAssets.ShapesGlow, head, wisp, VfxMath.Additive(pal.B, 0.6f), rot, Shapes.Origin, new Vector2(0.95f * s));
			Add(ref info, VfxAssets.Shapes, head, wisp, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.Hi, 0.4f), 0.95f), rot, Shapes.Origin, new Vector2(0.8f * s));
			for (int side = -1; side <= 1; side += 2)
			{
				// рожки
				Vector2 hornBase = head - hd * 2f * s + hp * side * 2.5f * s;
				LayerUtils.Line(ref info, 0, hornBase, hornBase - hd * 7f * s + hp * side * 4f * s, VfxMath.Additive(pal.Hi, 0.8f), 3f * s);
				// усы колышутся
				Vector2 whiskerBase = head + hd * 3f * s + hp * side * 1.5f * s;
				Vector2 prev = whiskerBase;
				for (int i = 1; i <= 5; i++)
				{
					float wave = (float)Math.Sin(t * 5f - i * 0.7f + side) * 1.5f * i * 0.3f;
					Vector2 next = whiskerBase - hd * i * 3f * s + hp * (side * i * 1.2f + wave) * s;
					LayerUtils.Line(ref info, 0, prev, next, VfxMath.Additive(pal.A, 0.5f * (1f - i / 6f)), 2.2f);
					prev = next;
				}
			}
			Add(ref info, VfxAssets.Glint, head + hd * 1.5f * s, null, VfxMath.Additive(Color.White, 0.7f), 0f, new Vector2(32f, 32f), new Vector2(0.14f * s));
		}
	}

	/// <summary>Слой спутника: позади персонажа, но перед лучами и кругами.</summary>
	public class CompanionLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.MountBack, PlayerDrawLayers.Carpet);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (!LayerUtils.Visible(drawInfo))
				return false;
			LooksPlayer mp = LayerUtils.Mp(drawInfo);
			return mp.Look.Companion != CompanionStyle.None && mp.CompanionReady;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo) =>
			CompanionEffects.Draw(ref drawInfo, LayerUtils.Mp(drawInfo));
	}
}
