using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>Светящийся огонёк: ореол + цветное ядро + белая сердцевина. Угольки, искорки, светлячки.</summary>
	public class GlowParticle : Particle
	{
		public float Drag = 0.96f;
		public Vector2 Gravity;
		/// <summary>Размер в конце жизни (доля от начального).</summary>
		public float EndScale = 0.25f;
		/// <summary>Цвет к концу жизни (например, жёлтый уголёк краснеет).</summary>
		public Color? EndColor;
		public bool Core = true;
		/// <summary>Скорость мерцания (0 — не мерцает).</summary>
		public float Blink;
		/// <summary>Случайное блуждание (светлячки).</summary>
		public float Wander;
		private readonly float _phase = Main.rand.NextFloat(MathHelper.TwoPi);

		public GlowParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 40)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
		}

		public override void Update()
		{
			if (Wander > 0f)
				Velocity += Main.rand.NextVector2Circular(Wander, Wander);
			Velocity = Velocity * Drag + Gravity;
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float alpha = VfxMath.FadeInOut(t, 0.12f, 0.45f) * Opacity;
			if (Blink > 0f)
				alpha *= 0.3f + 0.7f * (0.5f + 0.5f * (float)Math.Sin(Time * Blink + _phase));
			if (alpha <= 0.01f)
				return;

			float s = Scale * MathHelper.Lerp(1f, EndScale, t);
			Color c = EndColor.HasValue ? Color.Lerp(Color, EndColor.Value, t) : Color;
			Vector2 pos = Screen(Position);
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D orb = VfxAssets.Orb;

			sb.Draw(bloom, pos, null, VfxMath.Additive(c, 0.5f * alpha), 0f, bloom.Size() / 2f, s * 0.45f, SpriteEffects.None, 0f);
			sb.Draw(orb, pos, null, VfxMath.Additive(c, alpha), 0f, orb.Size() / 2f, s * 0.3f, SpriteEffects.None, 0f);
			if (Core)
				sb.Draw(orb, pos, null, VfxMath.Additive(Color.White, 0.75f * alpha), 0f, orb.Size() / 2f, s * 0.13f, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Четырёхлучевая вспышка-мерцание.</summary>
	public class FlareParticle : Particle
	{
		public float Spin;
		public float Drag = 0.92f;

		public FlareParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 30)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
			Rotation = Main.rand.NextBool() ? 0f : MathHelper.PiOver4 * 0.3f;
		}

		public override void Update()
		{
			Velocity *= Drag;
			Position += Velocity;
			Rotation += Spin;
		}

		public override void Draw(SpriteBatch sb)
		{
			float k = VfxMath.Bump(Progress) * Opacity;
			if (k <= 0.01f)
				return;

			float s = Scale * (0.4f + 0.6f * k);
			Vector2 pos = Screen(Position);
			Texture2D flare = VfxAssets.Flare;
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Color, 0.4f * k), 0f, bloom.Size() / 2f, s * 0.35f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Color, k), Rotation, flare.Size() / 2f, s * 0.42f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Color.White, 0.8f * k), Rotation, flare.Size() / 2f, s * 0.22f, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Растянутая искра, повёрнутая по направлению полёта.</summary>
	public class SparkParticle : Particle
	{
		public float Drag = 0.93f;
		public Vector2 Gravity;
		/// <summary>Насколько искра вытягивается от скорости.</summary>
		public float Stretch = 3f;
		public float Width = 1f;
		public Color? EndColor;

		public SparkParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 25)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
			Rotation = velocity.ToRotation();
		}

		public override void Update()
		{
			Velocity = Velocity * Drag + Gravity;
			Position += Velocity;
			if (Velocity.LengthSquared() > 0.0001f)
				Rotation = Velocity.ToRotation();
		}

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float alpha = VfxMath.FadeInOut(t, 0.05f, 0.4f) * Opacity;
			if (alpha <= 0.01f)
				return;

			float length = (10f + Velocity.Length() * Stretch) * Scale;
			float width = 7f * Scale * Width * (1f - 0.5f * t);
			Color c = EndColor.HasValue ? Color.Lerp(Color, EndColor.Value, t) : Color;
			Vector2 pos = Screen(Position);
			Texture2D tex = VfxAssets.Streak;
			Vector2 origin = tex.Size() / 2f;

			sb.Draw(tex, pos, null, VfxMath.Additive(c, alpha), Rotation, origin,
				new Vector2(length / tex.Width, width / tex.Height), SpriteEffects.None, 0f);
			sb.Draw(tex, pos, null, VfxMath.Additive(Color.White, 0.7f * alpha), Rotation, origin,
				new Vector2(length * 0.7f / tex.Width, width * 0.4f / tex.Height), SpriteEffects.None, 0f);
		}
	}

	/// <summary>Клуб дыма. Обычный — полупрозрачный, Glowing — светящийся (пламя, туман).</summary>
	public class SmokeParticle : Particle
	{
		public float Spin;
		public float Grow = 1.8f;
		public float Drag = 0.95f;
		public Vector2 Gravity;
		public bool Glowing;
		public float MaxOpacity = 0.55f;
		private readonly int _frame = Main.rand.Next(4);

		public SmokeParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 60)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
			Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			Spin = Main.rand.NextFloat(-0.02f, 0.02f);
		}

		public override void Update()
		{
			Velocity = Velocity * Drag + Gravity;
			Position += Velocity;
			Rotation += Spin;
		}

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float alpha = VfxMath.FadeInOut(t, 0.2f, 0.35f) * Opacity * MaxOpacity;
			if (alpha <= 0.01f)
				return;

			float s = Scale * MathHelper.Lerp(1f, Grow, t) * 0.6f;
			Color c = Glowing ? VfxMath.Additive(Color, alpha) : Color * alpha;
			sb.Draw(VfxAssets.Smoke, Screen(Position), VfxAssets.SmokeFrame(_frame), c, Rotation, new Vector2(32f, 32f), s, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Расходящееся кольцо-волна (можно сплющить в эллипс — «на земле»).</summary>
	public class RingParticle : Particle
	{
		public Vector2 Squash = Vector2.One;
		/// <summary>Диаметр в пикселях в начале и в конце.</summary>
		public float StartSize;
		public float EndSize;

		public RingParticle(Vector2 position, Color color, float startSize, float endSize, int lifetime = 30)
		{
			Position = position;
			Color = color;
			StartSize = startSize;
			EndSize = endSize;
			Lifetime = lifetime;
		}

		public override float CullRadius => EndSize;

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float eased = 1f - (1f - t) * (1f - t);
			float size = MathHelper.Lerp(StartSize, EndSize, eased);
			float alpha = (float)Math.Pow(1f - t, 1.5f) * Opacity;
			if (alpha <= 0.01f)
				return;

			Texture2D tex = VfxAssets.RingWave;
			sb.Draw(tex, Screen(Position), null, VfxMath.Additive(Color, alpha), Rotation, tex.Size() / 2f,
				Squash * (size / tex.Width), SpriteEffects.None, 0f);
		}
	}

	/// <summary>Ломаная молния между двумя точками.</summary>
	public class LightningParticle : Particle
	{
		private readonly Vector2[] _points;
		public float Width = 1f;
		public Color CoreColor = Color.White;

		public LightningParticle(Vector2 start, Vector2 end, Color color, float width = 1f, int lifetime = 14, float jaggedness = 0.3f)
		{
			Position = start;
			Color = color;
			Width = width;
			Lifetime = lifetime;

			float length = Vector2.Distance(start, end);
			int count = Math.Clamp((int)(length / 9f), 3, 28);
			_points = new Vector2[count + 1];
			Vector2 normal = (end - start).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
			float offset = 0f;
			for (int i = 0; i <= count; i++)
			{
				float t = i / (float)count;
				offset = offset * 0.55f + Main.rand.NextFloat(-1f, 1f) * length * jaggedness * 0.4f;
				float envelope = (float)Math.Sin(t * MathHelper.Pi);
				_points[i] = Vector2.Lerp(start, end, t) + normal * offset * envelope;
			}
		}

		public override float CullRadius => 4000f;

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float alpha = (1f - t) * (1f - t) * Opacity * (Time % 4 < 2 ? 1f : 0.65f);
			if (alpha <= 0.01f)
				return;

			Texture2D line = VfxAssets.Line;
			var origin = new Vector2(0f, line.Height / 2f);
			for (int i = 0; i < _points.Length - 1; i++)
			{
				Vector2 a = Screen(_points[i]);
				Vector2 diff = _points[i + 1] - _points[i];
				float len = diff.Length() + 1f;
				float rot = diff.ToRotation();
				sb.Draw(line, a, null, VfxMath.Additive(Color, 0.8f * alpha), rot, origin,
					new Vector2(len / line.Width, Width * 1.5f), SpriteEffects.None, 0f);
				sb.Draw(line, a, null, VfxMath.Additive(CoreColor, alpha), rot, origin,
					new Vector2(len / line.Width, Width * 0.45f), SpriteEffects.None, 0f);
			}

			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, Screen(_points[0]), null, VfxMath.Additive(Color, 0.6f * alpha), 0f, bloom.Size() / 2f, 0.35f * Width, SpriteEffects.None, 0f);
			sb.Draw(bloom, Screen(_points[^1]), null, VfxMath.Additive(Color, 0.6f * alpha), 0f, bloom.Size() / 2f, 0.3f * Width, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Кадры листа Sprites.png.</summary>
	public enum SpriteKind
	{
		Heart = 0,
		Petal = 1,
		Snowflake = 2,
		Star = 3,
		Shard = 4,
		Feather = 5,
		Butterfly = 6
	}

	/// <summary>Как движется спрайт.</summary>
	public enum SpriteMotion
	{
		/// <summary>Всплывает, покачиваясь (сердечки).</summary>
		Float,
		/// <summary>Падает, кружась (лепестки).</summary>
		Fall,
		/// <summary>Медленно опускается (снежинки).</summary>
		Drift,
		/// <summary>Порхает около владельца (бабочки).</summary>
		Flutter,
		/// <summary>Планирует, раскачиваясь (перья).</summary>
		Sway,
		/// <summary>Летит по инерции и вращается (звёзды, осколки).</summary>
		Spin
	}

	/// <summary>Спрайт с листа Sprites.png: сердечко, лепесток, снежинка, звезда, осколок, перо, бабочка.</summary>
	public class SpriteParticle : Particle
	{
		public SpriteKind Kind;
		public SpriteMotion Motion;
		public float Spin;
		/// <summary>Сила ореола позади спрайта (0 — без ореола).</summary>
		public float GlowStrength = 0.35f;
		/// <summary>true — сам спрайт рисуется светом (звёзды, осколки, снежинки).</summary>
		public bool Glowing;
		/// <summary>Игрок, вокруг которого порхают бабочки (-1 — никто).</summary>
		public int Owner = -1;
		public Vector2 HomeOffset;
		private readonly float _phase = Main.rand.NextFloat(MathHelper.TwoPi);

		public SpriteParticle(SpriteKind kind, SpriteMotion motion, Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 90)
		{
			Kind = kind;
			Motion = motion;
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
			Rotation = Main.rand.NextFloat(-0.5f, 0.5f);
			Spin = Main.rand.NextFloat(-0.05f, 0.05f);
		}

		public override void Update()
		{
			float wave = (float)Math.Sin(Time * 0.06f + _phase);
			switch (Motion)
			{
				case SpriteMotion.Float:
					Velocity.X = wave * 0.45f;
					Velocity.Y *= 0.985f;
					Rotation = Velocity.X * 0.35f;
					break;
				case SpriteMotion.Fall:
					Velocity.Y = Math.Min(Velocity.Y + 0.025f, 1.1f);
					Velocity.X = MathHelper.Lerp(Velocity.X, wave * 1.1f, 0.05f);
					Rotation += Spin;
					break;
				case SpriteMotion.Drift:
					Velocity.Y = Math.Min(Velocity.Y + 0.015f, 0.7f);
					Velocity.X = MathHelper.Lerp(Velocity.X, wave * 0.5f, 0.05f);
					Rotation += Spin * 0.5f;
					break;
				case SpriteMotion.Flutter:
					if (Owner >= 0 && Owner < Main.maxPlayers && Main.player[Owner].active)
						Velocity += (Main.player[Owner].Center + HomeOffset - Position) * 0.0025f;
					Velocity += Main.rand.NextVector2Circular(0.2f, 0.2f);
					if (Velocity.LengthSquared() > 2.2f * 2.2f)
						Velocity = Vector2.Normalize(Velocity) * 2.2f;
					Velocity *= 0.97f;
					Rotation = Velocity.X * 0.12f;
					break;
				case SpriteMotion.Sway:
					Velocity.Y = Math.Min(Velocity.Y + 0.012f, 0.6f);
					Velocity.X = wave * 1.0f;
					Rotation = -0.6f + wave * 0.5f;
					break;
				case SpriteMotion.Spin:
					Velocity *= 0.95f;
					Rotation += Spin;
					break;
			}

			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float alpha = VfxMath.FadeInOut(Progress, 0.1f, 0.65f) * Opacity;
			if (alpha <= 0.01f)
				return;

			int frame = (int)Kind;
			if (Kind == SpriteKind.Butterfly)
				frame += (Time / 5 + (int)(_phase * 3f)) % 2;

			Vector2 pos = Screen(Position);
			float s = Scale * 0.6f;

			if (GlowStrength > 0f)
			{
				Texture2D bloom = VfxAssets.Bloom;
				sb.Draw(bloom, pos, null, VfxMath.Additive(Color, GlowStrength * alpha), 0f, bloom.Size() / 2f, s * 0.32f, SpriteEffects.None, 0f);
			}

			SpriteEffects fx = Kind == SpriteKind.Butterfly && Velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Color c = Glowing ? VfxMath.Additive(Color, alpha) : Color * alpha;
			sb.Draw(VfxAssets.Sprites, pos, VfxAssets.SpriteFrame(frame), c, Rotation, new Vector2(16f, 16f), s, fx, 0f);

			if (Glowing)
				sb.Draw(VfxAssets.Sprites, pos, VfxAssets.SpriteFrame(frame), VfxMath.Additive(Color.White, 0.5f * alpha), Rotation, new Vector2(16f, 16f), s * 0.6f, fx, 0f);
		}
	}

	/// <summary>Светящаяся руна: всплывает и гаснет.</summary>
	public class RuneParticle : Particle
	{
		public int Frame = Main.rand.Next(8);
		public float Drag = 0.96f;
		public Vector2 Gravity;

		public RuneParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, int lifetime = 60)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			Scale = scale;
			Lifetime = lifetime;
		}

		public override void Update()
		{
			Velocity = Velocity * Drag + Gravity;
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float alpha = VfxMath.FadeInOut(Progress, 0.2f, 0.55f) * Opacity;
			if (alpha <= 0.01f)
				return;

			Vector2 pos = Screen(Position);
			float s = Scale * 0.7f * (0.92f + 0.08f * (float)Math.Sin(Time * 0.2f));
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Color, 0.45f * alpha), 0f, bloom.Size() / 2f, s * 0.35f, SpriteEffects.None, 0f);
			sb.Draw(VfxAssets.Runes, pos, VfxAssets.RuneFrame(Frame), VfxMath.Additive(Color, alpha), Rotation, new Vector2(16f, 16f), s, SpriteEffects.None, 0f);
			sb.Draw(VfxAssets.Runes, pos, VfxAssets.RuneFrame(Frame), VfxMath.Additive(Color.White, 0.35f * alpha), Rotation, new Vector2(16f, 16f), s, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Падающая звезда с хвостом-лентой.</summary>
	public class ShootingStarParticle : TrailParticle
	{
		public ShootingStarParticle(Vector2 position, Vector2 velocity, Color color, Color color2, float scale = 1f, int lifetime = 45)
			: base(14)
		{
			Position = position;
			Velocity = velocity;
			Color = color;
			TrailColor = color;
			TrailColor2 = color2;
			Scale = scale;
			Lifetime = lifetime;
			TrailWidth = 7f;
		}

		public override void Update()
		{
			Position += Velocity;
			Rotation += 0.08f;
			RecordTrail();
		}

		protected override float TrailFade() => VfxMath.FadeInOut(Progress, 0.1f, 0.6f);

		public override void Draw(SpriteBatch sb)
		{
			float alpha = TrailFade() * Opacity;
			if (alpha <= 0.01f)
				return;

			Vector2 pos = Screen(Position);
			Texture2D flare = VfxAssets.Flare;
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Color, 0.5f * alpha), 0f, bloom.Size() / 2f, Scale * 0.35f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Color, alpha), Rotation, flare.Size() / 2f, Scale * 0.3f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Color.White, alpha), Rotation, flare.Size() / 2f, Scale * 0.16f, SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// Огонёк, кружащий вокруг игрока по эллипсу, с хвостом-лентой (кометы, призма).
	/// Живёт, пока Alive возвращает true; Refresh каждый тик обновляет цвета и параметры из настроек владельца.
	/// Нижняя половина орбиты рисуется перед игроком, верхняя — позади (эффект объёма).
	/// </summary>
	public class OrbitParticle : TrailParticle
	{
		public int Owner;
		public float Angle;
		public float AngularSpeed = 0.06f;
		public float RadiusX = 40f;
		public float RadiusY = 14f;
		public float Tilt;
		public float Bob = 4f;
		/// <summary>Множители от настроек (размер орбиты и скорость).</summary>
		public float SizeMul = 1f;
		public float SpeedMul = 1f;
		/// <summary>Голова в виде осколка (призма), иначе огонёк.</summary>
		public bool ShardHead;
		/// <summary>Голова в виде руны (номер руны 0..7), -1 — не руна.</summary>
		public int RuneHead = -1;
		public Func<Player, bool> Alive;
		public Action<OrbitParticle, Player> Refresh;
		private float _fade;
		private bool _dying;

		public OrbitParticle(int owner, float angle, int trailLength = 18)
			: base(trailLength)
		{
			Owner = owner;
			Angle = angle;
			Lifetime = int.MaxValue;
			TrailWidth = 9f;
		}

		public bool Dying => _dying;

		public override float CullRadius => 300f;

		public override void Update()
		{
			Player p = Owner >= 0 && Owner < Main.maxPlayers ? Main.player[Owner] : null;
			bool ok = p != null && p.active && !p.dead && Alive != null && Alive(p);
			if (!ok)
				_dying = true;

			_fade = _dying ? _fade - 0.06f : Math.Min(1f, _fade + 0.05f);
			if (_dying && _fade <= 0f)
			{
				Dead = true;
				return;
			}

			if (!_dying)
			{
				ParticleSystem.OrbitCounts[Owner]++;
				Refresh?.Invoke(this, p);
			}

			if (p != null && p.active)
			{
				Angle += AngularSpeed * SpeedMul;
				Vector2 offset = VfxMath.OnEllipse(Angle, RadiusX * SizeMul, RadiusY * SizeMul).RotatedBy(Tilt);
				offset.Y += (float)Math.Sin(Time * 0.05f + Angle) * Bob;
				Vector2 newPos = p.Center + offset;
				Velocity = newPos - Position;
				Position = newPos;
				Front = Math.Sin(Angle) > 0;
			}

			Opacity = _fade;
			RecordTrail();
		}

		public override void Draw(SpriteBatch sb)
		{
			float alpha = Opacity;
			if (alpha <= 0.01f)
				return;

			Vector2 pos = Screen(Position);
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Color, 0.55f * alpha), 0f, bloom.Size() / 2f, Scale * 0.45f, SpriteEffects.None, 0f);

			if (RuneHead >= 0)
			{
				Rectangle frame = VfxAssets.RuneFrame(RuneHead);
				sb.Draw(VfxAssets.Runes, pos, frame, VfxMath.Additive(Color, alpha), 0f, new Vector2(16f, 16f), Scale * 0.6f, SpriteEffects.None, 0f);
				sb.Draw(VfxAssets.Runes, pos, frame, VfxMath.Additive(Color.White, 0.4f * alpha), 0f, new Vector2(16f, 16f), Scale * 0.6f, SpriteEffects.None, 0f);
			}
			else if (ShardHead)
			{
				float rot = OldRot[0] + MathHelper.PiOver2;
				sb.Draw(VfxAssets.Sprites, pos, VfxAssets.SpriteFrame((int)SpriteKind.Shard), VfxMath.Additive(Color, alpha), rot,
					new Vector2(16f, 16f), Scale * 0.75f, SpriteEffects.None, 0f);
				sb.Draw(VfxAssets.Sprites, pos, VfxAssets.SpriteFrame((int)SpriteKind.Shard), VfxMath.Additive(Color.White, 0.6f * alpha), rot,
					new Vector2(16f, 16f), Scale * 0.45f, SpriteEffects.None, 0f);
			}
			else
			{
				Texture2D orb = VfxAssets.Orb;
				sb.Draw(orb, pos, null, VfxMath.Additive(Color, alpha), 0f, orb.Size() / 2f, Scale * 0.32f, SpriteEffects.None, 0f);
				sb.Draw(orb, pos, null, VfxMath.Additive(Color.White, 0.85f * alpha), 0f, orb.Size() / 2f, Scale * 0.15f, SpriteEffects.None, 0f);
			}
		}
	}
}
