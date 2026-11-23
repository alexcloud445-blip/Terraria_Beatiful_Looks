using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>Запоминает путь снарядов игрока (вызывает WeaponTrails после ИИ каждого снаряда).</summary>
	public class WeaponTrailGlobal : GlobalProjectile
	{
		public override void PostAI(Projectile projectile)
		{
			if (!Main.dedServ)
				WeaponTrails.Track(projectile);
		}
	}

	/// <summary>
	/// Светящийся след за брошенным оружием (и, по желанию, за всеми снарядами игрока).
	/// «Метательное» определяется по классу урона: всё, что считается Throwing, — сюда входит и разбойник Calamity.
	/// Удары из скрытности Calamity получают след шире и ярче.
	/// Путь хранится в своём массиве по номеру снаряда (projectile.identity защищает от перепутывания).
	/// </summary>
	public static class WeaponTrails
	{
		private const int Length = 14;
		/// <summary>ProjAIStyleID.ThrownProjectile — ИИ брошенных предметов (сюрикен, метательный нож).</summary>
		private const int ThrownAiStyle = 2;
		private const int MaxTrails = 80;

		private class Trail
		{
			public int Identity = -1;
			public int Type;
			public int Owner;
			public uint LastTick;
			public bool Stealth;
			public int Count;
			public readonly Vector2[] Pos = new Vector2[Length];
			public readonly float[] Rot = new float[Length];
		}

		private static readonly Trail[] Trails = new Trail[1001];
		private static int _activeThisTick;
		private static uint _countTick;

		// массивы точной длины для VertexStrip (он рисует массив целиком)
		private static readonly Vector2[][] PosBuf = new Vector2[Length + 1][];
		private static readonly float[][] RotBuf = new float[Length + 1][];

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		public static void Unload()
		{
			Array.Clear(Trails);
		}

		/// <summary>Подходит ли снаряд и чей у него образ.</summary>
		private static bool Qualifies(Projectile proj, out LooksPlayer mp)
		{
			mp = null;
			if (!proj.active || !proj.friendly || proj.hostile || proj.npcProj || proj.owner < 0 || proj.owner >= Main.maxPlayers)
				return false;
			if (proj.minion || proj.sentry || Main.projPet[proj.type] || proj.damage <= 0 || proj.width > 160 || ProjectileID.Sets.IsAWhip[proj.type])
				return false;
			Player owner = Main.player[proj.owner];
			if (owner == null || !owner.active || owner.heldProj == proj.whoAmI)
				return false;
			if (!owner.TryGetModPlayer(out mp) || !mp.EffectsVisible || mp.Look.WeaponTrail == WeaponTrailStyle.None)
				return false;
			// метательное: класс Throwing (разбойник Calamity и др.) или ванильный «брошенный» ИИ (сюрикены, ножи, кинжалы):
			// в Terraria 1.4 ванильное метательное оружие числится дальним боем
			return mp.Look.WeaponTrailAll || proj.CountsAsClass(DamageClass.Throwing) || proj.aiStyle == ThrownAiStyle;
		}

		public static void Track(Projectile proj)
		{
			int i = proj.whoAmI;
			if (i < 0 || i >= Trails.Length)
				return;
			if (!Qualifies(proj, out LooksPlayer mp))
				return;

			if (_countTick != Main.GameUpdateCount)
			{
				_countTick = Main.GameUpdateCount;
				_activeThisTick = 0;
			}
			Trail t = Trails[i] ??= new Trail();
			bool fresh = t.Identity != proj.identity || t.Type != proj.type || Main.GameUpdateCount - t.LastTick > 2;
			if (fresh)
			{
				if (_activeThisTick >= MaxTrails)
					return;
				t.Identity = proj.identity;
				t.Type = proj.type;
				t.Owner = proj.owner;
				t.Count = 0;
				t.Stealth = StealthEffects.IsStealthStrike(proj);
				if (t.Stealth)
					StealthBurst(proj.Center, mp.Look);
			}
			_activeThisTick++;
			t.LastTick = Main.GameUpdateCount;

			for (int k = Math.Min(t.Count, Length - 1); k > 0; k--)
			{
				t.Pos[k] = t.Pos[k - 1];
				t.Rot[k] = t.Rot[k - 1];
			}
			t.Pos[0] = proj.Center;
			Vector2 dir = t.Count > 0 ? t.Pos[0] - t.Pos[1] : proj.velocity;
			t.Rot[0] = dir.LengthSquared() > 0.01f ? dir.ToRotation() : (t.Count > 0 ? t.Rot[1] : 0f);
			if (t.Count < Length)
				t.Count++;

			if (t.Count > 1 && Vector2.DistanceSquared(t.Pos[0], t.Pos[1]) > 4f)
				Particles(proj, t, mp.Look);
		}

		/// <summary>Удар из скрытности: вспышка и кольцо там, где появился снаряд.</summary>
		private static void StealthBurst(Vector2 pos, LookSettings look)
		{
			Palette pal = Palette.Of(look.MotionColor, look.MotionColor2);
			ParticleSystem.Spawn(new FlareParticle(pos, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), 1.1f, 20) { Front = true });
			ParticleSystem.Spawn(new RingParticle(pos, new Ramp(pal.Hi, pal.A, pal.B), 6f, 60f, 18) { Front = true });
		}

		private static void Particles(Projectile proj, Trail t, LookSettings look)
		{
			Palette pal = Palette.Of(look.MotionColor, look.MotionColor2, Main.rand.NextFloat(0.3f));
			Vector2 pos = proj.Center - proj.velocity * R(0f, 0.6f);
			Vector2 vel = -proj.velocity * 0.05f;
			float rate = t.Stealth ? 2f : 1f;
			bool front = Main.rand.NextBool();
			switch (look.WeaponTrail)
			{
				case WeaponTrailStyle.Soft:
					if (Main.rand.NextFloat() < 0.15f * rate)
						ParticleSystem.Spawn(new GlowParticle(pos, vel, pal.Glow, R(0.15f, 0.25f), 25) { Front = front });
					break;
				case WeaponTrailStyle.Sparkle:
					if (Main.rand.NextFloat() < 0.35f * rate)
						ParticleSystem.Spawn(new FlareParticle(pos, vel + Main.rand.NextVector2Circular(0.4f, 0.4f), new Ramp(Color.White, pal.A, pal.B), R(0.25f, 0.4f), 24) { Front = front });
					break;
				case WeaponTrailStyle.Flame:
					if (Main.rand.NextFloat() < 0.5f * rate)
						ParticleSystem.Spawn(new GlowParticle(pos, vel + new Vector2(R(-0.4f, 0.4f), -R(0.3f, 1f)), pal.Hot, R(0.18f, 0.3f), 30) { Wander = 0.05f, Front = front });
					if (Main.rand.NextFloat() < 0.12f * rate)
						ParticleSystem.Spawn(new SparkParticle(pos, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1f), pal.Hot, 0.35f, 40)
						{ Gravity = new Vector2(0f, 0.14f), Drag = 0.98f, Bounce = true, Front = front });
					break;
				case WeaponTrailStyle.Prism:
					if (Main.rand.NextFloat() < 0.2f * rate)
					{
						Color a = Main.hslToRgb(Main.rand.NextFloat(), 0.8f, 0.75f);
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool() ? Shape.ShardA : Shape.Diamond, SpriteMotion.Spin, pos, vel + Main.rand.NextVector2Circular(0.8f, 0.8f),
							new Ramp(Color.White, a, VfxMath.HueShift(a, 0.2f)), R(0.3f, 0.45f), 30) { Glowing = true, Spin = R(-0.2f, 0.2f), Front = front });
					}
					break;
				case WeaponTrailStyle.Shadow:
					if (Main.rand.NextFloat() < 0.6f * rate)
						ParticleSystem.Spawn(new SmokeParticle(pos, vel, new Ramp(VfxMath.Shade(pal.A, 0.22f), new Color(5, 3, 10)), R(0.35f, 0.55f), Main.rand.Next(30, 45))
						{ MaxOpacity = 0.5f, Grow = 1.4f, Front = front });
					if (Main.rand.NextFloat() < 0.15f * rate)
						ParticleSystem.Spawn(new GlowParticle(pos, vel, new Ramp(pal.A, pal.B), 0.2f, 24) { Front = front });
					break;
				case WeaponTrailStyle.Stardust:
					if (Main.rand.NextFloat() < 0.25f * rate)
						ParticleSystem.Spawn(new ShapeParticle(Main.rand.NextBool(3) ? Shape.Star5 : Shape.Star4, SpriteMotion.Spin, pos, vel + Main.rand.NextVector2Circular(0.5f, 0.5f),
							new Ramp(Color.White, pal.A, pal.B), R(0.3f, 0.45f), 34) { Glowing = true, GlowRamp = pal.B, Spin = R(-0.12f, 0.12f), Front = front });
					break;
			}
		}

		private static bool Alive(int i, out Trail t, out Projectile proj)
		{
			t = Trails[i];
			proj = null;
			if (t == null || t.Count < 3 || Main.GameUpdateCount - t.LastTick > 1)
				return false;
			proj = Main.projectile[i];
			return proj != null && proj.active && proj.identity == t.Identity && proj.type == t.Type;
		}

		private static float PathLength(Trail t)
		{
			float len = 0f;
			for (int k = 1; k < t.Count; k++)
				len += Vector2.Distance(t.Pos[k], t.Pos[k - 1]);
			return len;
		}

		/// <summary>Ленты-следы на шейдерах (вызывается из VfxRenderer вне SpriteBatch, позади игроков).</summary>
		public static void DrawRibbons()
		{
			for (int i = 0; i < Trails.Length; i++)
			{
				if (!Alive(i, out Trail t, out Projectile proj))
					continue;
				Player owner = Main.player[t.Owner];
				if (!owner.TryGetModPlayer(out LooksPlayer mp))
					continue;
				LookSettings look = mp.Look;
				float opacity = MathHelper.Clamp((PathLength(t) - 8f) / 60f, 0f, 1f);
				if (opacity <= 0.01f)
					continue;

				int count = t.Count;
				Vector2[] pos = PosBuf[count] ??= new Vector2[count];
				float[] rot = RotBuf[count] ??= new float[count];
				Array.Copy(t.Pos, pos, count);
				Array.Copy(t.Rot, rot, count);

				Palette pal = Palette.Of(look.MotionColor, look.MotionColor2);
				var ramp = new Ramp(pal.Hi, pal.A, pal.B);
				float width = (look.WeaponTrail == WeaponTrailStyle.Sparkle || look.WeaponTrail == WeaponTrailStyle.Shadow ? 5f : 8f)
					* (t.Stealth ? 1.6f : 1f) * MathHelper.Clamp(Math.Max(proj.width, proj.height) / 20f, 0.7f, 1.5f);
				float bright = t.Stealth ? 1.25f : 1f;
				VertexStrip.StripHalfWidthFunction widthFn = p => width * VfxMath.Ease(p * 4f) * (1f - p * 0.8f);

				switch (look.WeaponTrail)
				{
					case WeaponTrailStyle.Flame:
						Ribbons.Draw(pos, rot, p => VfxMath.Additive(pal.Hot.At(p), opacity * bright * (1f - p)), widthFn, RibbonStyle.Flame);
						break;
					case WeaponTrailStyle.Prism:
						Ribbons.Draw(pos, rot,
							p => VfxMath.Additive(Color.Lerp(Color.White, Main.hslToRgb(VfxMath.Frac(p * 1.5f - Main.GlobalTimeWrappedHourly), 0.85f, 0.65f), VfxMath.Ease(p * 3f)), opacity * bright * (1f - p)),
							widthFn, RibbonStyle.Rainbow);
						break;
					case WeaponTrailStyle.Stardust:
						Ribbons.Draw(pos, rot, p => VfxMath.Additive(new Ramp(pal.Hi, pal.B, pal.A).At(p), opacity * bright * (1f - p)), widthFn, RibbonStyle.Soft);
						break;
					default:
						Ribbons.Draw(pos, rot, p => VfxMath.Additive(ramp.At(p), opacity * bright * (1f - p)), widthFn, RibbonStyle.Soft);
						break;
				}
			}
		}

		/// <summary>Запасной вариант без шейдеров: цепочка мягких искр (в активный SpriteBatch).</summary>
		public static void DrawSprites(SpriteBatch sb)
		{
			Texture2D streak = VfxAssets.Streak;
			for (int i = 0; i < Trails.Length; i++)
			{
				if (!Alive(i, out Trail t, out Projectile _))
					continue;
				if (!Main.player[t.Owner].TryGetModPlayer(out LooksPlayer mp))
					continue;
				Palette pal = Palette.Of(mp.Look.MotionColor, mp.Look.MotionColor2);
				var ramp = new Ramp(pal.Hi, pal.A, pal.B);
				for (int k = 1; k < t.Count; k++)
				{
					float p = k / (float)(t.Count - 1);
					Vector2 a = t.Pos[k - 1], b = t.Pos[k];
					Vector2 d = a - b;
					float len = d.Length();
					if (len < 0.5f)
						continue;
					sb.Draw(streak, (a + b) / 2f - Main.screenPosition, null, VfxMath.Additive(ramp.At(p), 0.7f * (1f - p)), d.ToRotation(), streak.Size() / 2f,
						new Vector2(len * 2f / streak.Width, (t.Stealth ? 0.5f : 0.35f) * (1f - p * 0.6f)), SpriteEffects.None, 0f);
				}
			}
		}
	}
}
