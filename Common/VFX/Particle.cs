using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using BeautifulLooks.Common.Looks;

namespace BeautifulLooks.Common.VFX
{
	/// <summary>
	/// Частица собственной системы (не Dust). Живёт Lifetime тиков, рисуется в мировых координатах.
	/// Светящиеся частицы рисуются аддитивно: у цвета A = 0 (см. VfxMath.Additive).
	/// </summary>
	public abstract class Particle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		/// <summary>Цвет за жизнь частицы: от начала (Progress = 0) к концу (1).</summary>
		public Ramp Ramp = Color.White;
		public float Scale = 1f;
		public float Rotation;
		public float Opacity = 1f;
		public int Time;
		public int Lifetime = 60;
		/// <summary>true — рисуется перед игроками, false — позади.</summary>
		public bool Front = true;
		public bool Dead;

		/// <summary>Начальный цвет; присваивание делает градиент из одного цвета.</summary>
		public Color Color
		{
			get => Ramp.Start;
			set => Ramp = value;
		}

		/// <summary>0 в начале жизни, 1 в конце.</summary>
		public float Progress => Lifetime <= 0 ? 1f : MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f);

		/// <summary>Радиус для отсечения частиц за экраном.</summary>
		public virtual float CullRadius => 80f * Scale;

		/// <summary>Вызывается раз в тик, Time уже увеличен.</summary>
		public virtual void Update()
		{
			Position += Velocity;
		}

		/// <summary>Отрисовка в активный SpriteBatch (экранные координаты = мир - Main.screenPosition).</summary>
		public abstract void Draw(SpriteBatch sb);

		/// <summary>
		/// «Живые» частицы: отскок от блоков (меняет Velocity до перемещения). true — был удар.
		/// Частица, появившаяся внутри блока, не сталкивается, пока не вылетит из него.
		/// </summary>
		protected bool BounceOffTiles(ref int bounces, float restitution = 0.45f)
		{
			if (!Living.Enabled || Living.Solid(Position))
				return false;
			Vector2 next = Position + Velocity;
			bool hit = false;
			if (Velocity.Y > 0f && Living.Solid(new Vector2(Position.X, next.Y + 1f)))
			{
				Velocity.Y *= -restitution;
				Velocity.X *= 0.7f;
				hit = true;
			}
			else if (Velocity.Y < 0f && Living.Solid(new Vector2(Position.X, next.Y - 1f)))
			{
				Velocity.Y *= -0.3f;
				hit = true;
			}
			if (Math.Abs(Velocity.X) > 0.01f && Living.Solid(new Vector2(next.X, Position.Y)))
			{
				Velocity.X *= -restitution;
				hit = true;
			}
			if (hit)
				bounces++;
			return hit;
		}

		protected static Vector2 Screen(Vector2 world) => world - Main.screenPosition;
	}

	/// <summary>Частица с хвостом-лентой: хранит прошлые позиции, хвост рисуется шейдером.</summary>
	public abstract class TrailParticle : Particle
	{
		public readonly Vector2[] OldPos;
		public readonly float[] OldRot;
		public RibbonStyle Ribbon = RibbonStyle.Soft;
		public float TrailWidth = 8f;
		public Color TrailColor = Color.White;
		public Color TrailColor2 = Color.White;
		private bool _trailStarted;

		protected TrailParticle(int length)
		{
			OldPos = new Vector2[length];
			OldRot = new float[length];
		}

		/// <summary>Сдвигает историю позиций и записывает текущую.</summary>
		protected void RecordTrail()
		{
			if (!_trailStarted)
			{
				for (int i = 0; i < OldPos.Length; i++)
					OldPos[i] = Position;
				_trailStarted = true;
			}

			for (int i = OldPos.Length - 1; i > 0; i--)
			{
				OldPos[i] = OldPos[i - 1];
				OldRot[i] = OldRot[i - 1];
			}

			OldPos[0] = Position;
			Vector2 dir = OldPos[0] - OldPos[1];
			OldRot[0] = dir.LengthSquared() > 0.0001f ? dir.ToRotation() : OldRot[1];
		}

		/// <summary>Рисует хвост (вызывается вне SpriteBatch).</summary>
		public virtual void DrawTrail()
		{
			if (!_trailStarted || Ribbon == RibbonStyle.None)
				return;

			float opacity = Opacity * TrailFade();
			if (opacity <= 0.01f)
				return;

			Color c1 = TrailColor, c2 = TrailColor2;
			float width = TrailWidth * Scale;
			Ribbons.Draw(OldPos, OldRot,
				p => VfxMath.Additive(Color.Lerp(c1, c2, p), opacity * (1f - p)),
				p => width * (1f - p) * (0.6f + 0.4f * VfxMath.Ease(Math.Min(1f, p * 6f))),
				Ribbon);
		}

		/// <summary>Прозрачность хвоста (например, угасание в конце жизни).</summary>
		protected virtual float TrailFade() => 1f;
	}
}
