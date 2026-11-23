using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>
	/// Светящийся огонёк из трёх слоёв: ореол (более поздний цвет градиента), тело (текущий цвет)
	/// и светлая сердцевина (более ранний цвет). Поэтому огонёк сам по себе выглядит градиентом.
	/// </summary>
	public class GlowParticle : Particle
	{
		public float Drag = 0.96f;
		public Vector2 Gravity;
		/// <summary>Размер в конце жизни (доля от начального).</summary>
		public float EndScale = 0.25f;
		public bool Core = true;
		/// <summary>Скорость мерцания (0 — не мерцает).</summary>
		public float Blink;
		/// <summary>Случайное блуждание (светлячки).</summary>
		public float Wander;
		/// <summary>Сила ореола (0..1).</summary>
		public float Halo = 0.32f;
		private readonly float _phase = Main.rand.NextFloat(MathHelper.TwoPi);

		public GlowParticle(Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 40)
		{
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
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
				alpha *= 0.25f + 0.75f * (0.5f + 0.5f * (float)Math.Sin(Time * Blink + _phase));
			if (alpha <= 0.01f)
				return;

			float s = Scale * MathHelper.Lerp(1f, EndScale, t);
			Vector2 pos = Screen(Position);
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D orb = VfxAssets.Orb;

			sb.Draw(bloom, pos, null, VfxMath.Additive(Ramp.At(Math.Min(1f, t + 0.35f)), Halo * alpha), 0f, bloom.Size() / 2f, s * 0.5f, SpriteEffects.None, 0f);
			sb.Draw(orb, pos, null, VfxMath.Additive(Ramp.At(t), alpha), 0f, orb.Size() / 2f, s * 0.28f, SpriteEffects.None, 0f);
			if (Core)
			{
				Color core = Color.Lerp(Ramp.At(Math.Max(0f, t - 0.3f)), Color.White, 0.5f);
				sb.Draw(orb, pos, null, VfxMath.Additive(core, 0.6f * alpha), 0f, orb.Size() / 2f, s * 0.12f, SpriteEffects.None, 0f);
			}
		}
	}

	/// <summary>Мерцающая звёздочка: четыре луча (основной цвет), острый блик и ореол (второй цвет).</summary>
	public class FlareParticle : Particle
	{
		public float Spin;
		public float Drag = 0.92f;

		public FlareParticle(Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 30)
		{
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
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
			float t = Progress;
			float k = VfxMath.Bump(t) * Opacity;
			if (k <= 0.01f)
				return;

			float s = Scale * (0.35f + 0.65f * k);
			Vector2 pos = Screen(Position);
			Texture2D flare = VfxAssets.Flare;
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D glint = VfxAssets.Glint;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Ramp.At(Math.Min(1f, 0.5f + t)), 0.3f * k), 0f, bloom.Size() / 2f, s * 0.32f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Ramp.At(t), k), Rotation, flare.Size() / 2f, s * 0.36f, SpriteEffects.None, 0f);
			sb.Draw(glint, pos, null, VfxMath.Additive(Color.White, 0.85f * k), Rotation, glint.Size() / 2f, s * 0.55f, SpriteEffects.None, 0f);
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

		public SparkParticle(Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 25)
		{
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
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
			Vector2 pos = Screen(Position);
			Texture2D tex = VfxAssets.Streak;
			Vector2 origin = tex.Size() / 2f;

			sb.Draw(tex, pos, null, VfxMath.Additive(Ramp.At(t), alpha), Rotation, origin,
				new Vector2(length / tex.Width, width / tex.Height), SpriteEffects.None, 0f);
			sb.Draw(tex, pos, null, VfxMath.Additive(Color.Lerp(Ramp.At(0f), Color.White, 0.6f), 0.7f * alpha), Rotation, origin,
				new Vector2(length * 0.7f / tex.Width, width * 0.35f / tex.Height), SpriteEffects.None, 0f);
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

		public SmokeParticle(Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 60)
		{
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
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
			Color c = Ramp.At(t);
			c = Glowing ? VfxMath.Additive(c, alpha) : c * alpha;
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

		public RingParticle(Vector2 position, Ramp ramp, float startSize, float endSize, int lifetime = 30)
		{
			Position = position;
			Ramp = ramp;
			StartSize = startSize;
			EndSize = endSize;
			Lifetime = lifetime;
		}

		public override float CullRadius => Math.Max(StartSize, EndSize);

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float eased = 1f - (1f - t) * (1f - t);
			float size = MathHelper.Lerp(StartSize, EndSize, eased);
			float alpha = (float)Math.Pow(1f - t, 1.5f) * Opacity;
			if (alpha <= 0.01f)
				return;

			Texture2D tex = VfxAssets.RingWave;
			sb.Draw(tex, Screen(Position), null, VfxMath.Additive(Ramp.At(t), alpha), Rotation, tex.Size() / 2f,
				Squash * (size / tex.Width), SpriteEffects.None, 0f);
		}
	}

	/// <summary>Ломаная молния между двумя точками.</summary>
	public class LightningParticle : Particle
	{
		private readonly Vector2[] _points;
		public float Width = 1f;
		public Color CoreColor = Color.White;

		public LightningParticle(Vector2 start, Vector2 end, Ramp ramp, float width = 1f, int lifetime = 14, float jaggedness = 0.3f)
		{
			Position = start;
			Ramp = ramp;
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
				float along = i / (float)(_points.Length - 1);
				Vector2 a = Screen(_points[i]);
				Vector2 diff = _points[i + 1] - _points[i];
				float len = diff.Length() + 1f;
				float rot = diff.ToRotation();
				sb.Draw(line, a, null, VfxMath.Additive(Ramp.At(along), 0.8f * alpha), rot, origin,
					new Vector2(len / line.Width, Width * 1.5f), SpriteEffects.None, 0f);
				sb.Draw(line, a, null, VfxMath.Additive(CoreColor, alpha), rot, origin,
					new Vector2(len / line.Width, Width * 0.45f), SpriteEffects.None, 0f);
			}

			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, Screen(_points[0]), null, VfxMath.Additive(Ramp.At(0f), 0.5f * alpha), 0f, bloom.Size() / 2f, 0.3f * Width, SpriteEffects.None, 0f);
			sb.Draw(bloom, Screen(_points[^1]), null, VfxMath.Additive(Ramp.At(1f), 0.5f * alpha), 0f, bloom.Size() / 2f, 0.26f * Width, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Как движется форма.</summary>
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
		Spin,
		/// <summary>Кружит вокруг владельца по спирали (вихри лепестков, затягивание в пустоту).</summary>
		Orbit
	}

	/// <summary>
	/// Форма из атласа Shapes: заливка (градиент Ramp за жизнь) + мягкое свечение (GlowRamp).
	/// Сердечки, лепестки, листья, снежинки, звёзды, осколки, полумесяцы, перья, бабочки, спирали...
	/// </summary>
	public class ShapeParticle : Particle
	{
		public Shape Shape;
		public SpriteMotion Motion;
		public float Spin;
		/// <summary>Сила свечения вокруг формы (0 — без свечения).</summary>
		public float GlowStrength = 0.55f;
		/// <summary>Цвет свечения за жизнь; по умолчанию — «поздний» цвет основного градиента.</summary>
		public Ramp? GlowRamp;
		/// <summary>true — сама форма рисуется светом (звёзды, осколки, снежинки).</summary>
		public bool Glowing;
		/// <summary>Поворачивать форму по направлению полёта (огоньки-«кометки» Wisp).</summary>
		public bool FaceVelocity;
		/// <summary>Игрок, вокруг которого кружит/порхает форма (-1 — никто).</summary>
		public int Owner = -1;
		public Vector2 HomeOffset;
		// Орбита (Motion.Orbit)
		public float OrbitAngle, OrbitRadius = 40f, OrbitSpeed = 0.05f, RadiusSpeed, OrbitSquash = 0.35f;
		private readonly float _phase = Main.rand.NextFloat(MathHelper.TwoPi);

		public ShapeParticle(Shape shape, SpriteMotion motion, Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 90)
		{
			Shape = shape;
			Motion = motion;
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
			Scale = scale;
			Lifetime = lifetime;
			Rotation = Main.rand.NextFloat(-0.5f, 0.5f);
			Spin = Main.rand.NextFloat(-0.05f, 0.05f);
		}

		private Player OwnerPlayer => Owner >= 0 && Owner < Main.maxPlayers && Main.player[Owner].active ? Main.player[Owner] : null;

		public override float CullRadius => Motion == SpriteMotion.Orbit ? 300f : 60f * Scale;

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
					if (OwnerPlayer is Player home)
						Velocity += (home.Center + HomeOffset - Position) * 0.0025f;
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
				case SpriteMotion.Orbit:
					OrbitAngle += OrbitSpeed;
					OrbitRadius = Math.Max(0f, OrbitRadius + RadiusSpeed);
					Vector2 center = OwnerPlayer?.Center ?? Position;
					Vector2 newPos = center + VfxMath.OnEllipse(OrbitAngle, OrbitRadius, OrbitRadius * OrbitSquash) + HomeOffset;
					Velocity = newPos - Position;
					Position = newPos;
					Front = Math.Sin(OrbitAngle) > 0;
					Rotation += Spin;
					if (FaceVelocity && Velocity.LengthSquared() > 0.01f)
						Rotation = Velocity.ToRotation();
					return;
			}

			Position += Velocity;
			if (FaceVelocity && Velocity.LengthSquared() > 0.01f)
				Rotation = Velocity.ToRotation();
		}

		public override void Draw(SpriteBatch sb)
		{
			float t = Progress;
			float alpha = VfxMath.FadeInOut(t, 0.1f, 0.65f) * Opacity;
			if (alpha <= 0.01f)
				return;

			Shape shape = Shape;
			if (shape >= Shape.Butterfly0 && shape <= Shape.Butterfly3)
				shape = Shapes.ButterflyFrame(Time / 4 + (int)(_phase * 3f));
			Rectangle frame = Shapes.Frame(shape);

			Vector2 pos = Screen(Position);
			float s = Scale * 0.62f * VfxMath.PopIn(Time / 10f);
			SpriteEffects fx = Motion == SpriteMotion.Flutter && Velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			if (GlowStrength > 0f)
			{
				Color glow = GlowRamp.HasValue ? GlowRamp.Value.At(t) : Ramp.At(Math.Min(1f, t + 0.45f));
				sb.Draw(VfxAssets.ShapesGlow, pos, frame, VfxMath.Additive(glow, GlowStrength * alpha), Rotation, Shapes.Origin, s * 1.15f, fx, 0f);
			}

			Color fill = Ramp.At(t);
			sb.Draw(VfxAssets.Shapes, pos, frame, Glowing ? VfxMath.Additive(fill, alpha) : fill * alpha, Rotation, Shapes.Origin, s, fx, 0f);

			if (Glowing)
				sb.Draw(VfxAssets.Shapes, pos, frame, VfxMath.Additive(Color.White, 0.35f * alpha), Rotation, Shapes.Origin, s * 0.6f, fx, 0f);
		}
	}

	/// <summary>Светящаяся руна: всплывает и гаснет.</summary>
	public class RuneParticle : Particle
	{
		public int Frame = Main.rand.Next(8);
		public float Drag = 0.96f;
		public Vector2 Gravity;

		public RuneParticle(Vector2 position, Vector2 velocity, Ramp ramp, float scale = 1f, int lifetime = 60)
		{
			Position = position;
			Velocity = velocity;
			Ramp = ramp;
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
			float t = Progress;
			float alpha = VfxMath.FadeInOut(t, 0.2f, 0.55f) * Opacity;
			if (alpha <= 0.01f)
				return;

			Vector2 pos = Screen(Position);
			float s = Scale * 0.7f * VfxMath.PopIn(Time / 12f);
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Ramp.At(Math.Min(1f, t + 0.4f)), 0.35f * alpha), 0f, bloom.Size() / 2f, s * 0.35f, SpriteEffects.None, 0f);
			sb.Draw(VfxAssets.Runes, pos, VfxAssets.RuneFrame(Frame), VfxMath.Additive(Ramp.At(t), alpha), Rotation, new Vector2(16f, 16f), s, SpriteEffects.None, 0f);
			sb.Draw(VfxAssets.Runes, pos, VfxAssets.RuneFrame(Frame), VfxMath.Additive(Color.White, 0.3f * alpha), Rotation, new Vector2(16f, 16f), s * 0.92f, SpriteEffects.None, 0f);
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
			Ramp = new Ramp(Color.Lerp(color, Color.White, 0.4f), color, color2);
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
			float t = Progress;
			float alpha = TrailFade() * Opacity;
			if (alpha <= 0.01f)
				return;

			Vector2 pos = Screen(Position);
			Texture2D flare = VfxAssets.Flare;
			Texture2D bloom = VfxAssets.Bloom;
			Texture2D glint = VfxAssets.Glint;
			sb.Draw(bloom, pos, null, VfxMath.Additive(TrailColor2, 0.4f * alpha), 0f, bloom.Size() / 2f, Scale * 0.35f, SpriteEffects.None, 0f);
			sb.Draw(flare, pos, null, VfxMath.Additive(Ramp.At(t), alpha), Rotation, flare.Size() / 2f, Scale * 0.28f, SpriteEffects.None, 0f);
			sb.Draw(glint, pos, null, VfxMath.Additive(Color.White, alpha), Rotation * 0.5f, glint.Size() / 2f, Scale * 0.5f, SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// Огонёк, кружащий вокруг игрока по эллипсу, с хвостом-лентой (кометы, призма, руны).
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
		/// <summary>Форма головы (по умолчанию — огонёк).</summary>
		public Shape? HeadShape;
		/// <summary>Голова в виде руны (номер руны 0..7), -1 — не руна.</summary>
		public int RuneHead = -1;
		/// <summary>Второй цвет головы (ореол).</summary>
		public Color Color2 = Color.White;
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

			// дальняя сторона орбиты чуть темнее и меньше — эффект глубины
			float depth = 0.8f + 0.2f * (float)Math.Sin(Angle);
			float s = Scale * depth;
			Vector2 pos = Screen(Position);
			Texture2D bloom = VfxAssets.Bloom;
			sb.Draw(bloom, pos, null, VfxMath.Additive(Color2, 0.4f * alpha), 0f, bloom.Size() / 2f, s * 0.45f, SpriteEffects.None, 0f);

			if (RuneHead >= 0)
			{
				Rectangle frame = VfxAssets.RuneFrame(RuneHead);
				sb.Draw(VfxAssets.Runes, pos, frame, VfxMath.Additive(Color, alpha * depth), 0f, new Vector2(16f, 16f), s * 0.6f, SpriteEffects.None, 0f);
				sb.Draw(VfxAssets.Runes, pos, frame, VfxMath.Additive(Color.White, 0.35f * alpha), 0f, new Vector2(16f, 16f), s * 0.6f, SpriteEffects.None, 0f);
			}
			else if (HeadShape.HasValue)
			{
				Rectangle frame = Shapes.Frame(HeadShape.Value);
				float rot = OldRot[0] + MathHelper.PiOver2;
				sb.Draw(VfxAssets.ShapesGlow, pos, frame, VfxMath.Additive(Color2, 0.6f * alpha), rot, Shapes.Origin, s * 0.85f, SpriteEffects.None, 0f);
				sb.Draw(VfxAssets.Shapes, pos, frame, VfxMath.Additive(Color, alpha), rot, Shapes.Origin, s * 0.7f, SpriteEffects.None, 0f);
				sb.Draw(VfxAssets.Shapes, pos, frame, VfxMath.Additive(Color.White, 0.5f * alpha), rot, Shapes.Origin, s * 0.4f, SpriteEffects.None, 0f);
			}
			else
			{
				Texture2D orb = VfxAssets.Orb;
				Texture2D glint = VfxAssets.Glint;
				sb.Draw(orb, pos, null, VfxMath.Additive(Color, alpha), 0f, orb.Size() / 2f, s * 0.3f, SpriteEffects.None, 0f);
				sb.Draw(orb, pos, null, VfxMath.Additive(Color.Lerp(Color, Color.White, 0.7f), 0.85f * alpha), 0f, orb.Size() / 2f, s * 0.13f, SpriteEffects.None, 0f);
				sb.Draw(glint, pos, null, VfxMath.Additive(Color.White, 0.5f * alpha), Time * 0.03f, glint.Size() / 2f, s * 0.4f, SpriteEffects.None, 0f);
			}
		}
	}
}
