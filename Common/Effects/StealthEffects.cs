using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Скрытность разбойника Calamity (работает, только если Calamity установлен; без него ничего не делает).
	/// Скрытность берётся через Mod.Call Calamity: "GetCurrentStealth" / "GetMaxStealth" / "GetStealthProjectile".
	/// - копится скрытность — вокруг сгущается тёмная дымка с огоньками;
	/// - полная скрытность — светится глаз (за ним тянется огненный росчерк) и контур персонажа;
	/// - удар из скрытности (скрытность резко падает) — вспышка, кольцо и разлёт тени.
	/// Цвета — из вкладки «Движение».
	/// </summary>
	public static class StealthEffects
	{
		private static Mod _calamity;
		private static bool _checked;
		private static bool _broken;

		private static Mod Calamity
		{
			get
			{
				if (!_checked)
				{
					_checked = true;
					ModLoader.TryGetMod("CalamityMod", out _calamity);
				}
				return _broken ? null : _calamity;
			}
		}

		/// <summary>Установлен ли Calamity (для подсказки в меню).</summary>
		public static bool CalamityLoaded => Calamity != null;

		public static void Unload()
		{
			_calamity = null;
			_checked = false;
			_broken = false;
		}

		private static void Fail(Exception e)
		{
			_broken = true;
			ModContent.GetInstance<BeautifulLooksMod>().Logger.Warn("Beautiful Looks: Calamity stealth call failed, stealth effects disabled: " + e.Message);
		}

		/// <summary>Текущая и максимальная скрытность игрока. false — нет Calamity или игрок не разбойник.</summary>
		public static bool TryGetStealth(Player p, out float current, out float max)
		{
			current = max = 0f;
			Mod cal = Calamity;
			if (cal == null)
				return false;
			try
			{
				if (cal.Call("GetCurrentStealth", p) is float c && cal.Call("GetMaxStealth", p) is float m)
				{
					current = c;
					max = m;
					return m > 0f;
				}
			}
			catch (Exception e)
			{
				Fail(e);
			}
			return false;
		}

		/// <summary>Снаряд выпущен ударом из скрытности.</summary>
		public static bool IsStealthStrike(Projectile proj)
		{
			Mod cal = Calamity;
			if (cal == null)
				return false;
			try
			{
				return cal.Call("GetStealthProjectile", proj) is bool b && b;
			}
			catch (Exception e)
			{
				Fail(e);
				return false;
			}
		}

		private static float R(float min, float max) => Main.rand.NextFloat(min, max);

		// ======================= обновление =======================

		public static void Update(LooksPlayer mp, bool visible)
		{
			Player p = mp.Player;
			if (!visible || !mp.Look.StealthFx || !TryGetStealth(p, out float cur, out float max))
			{
				mp.StealthRatio = Math.Max(0f, mp.StealthRatio - 0.05f);
				mp.StealthPrev = 0f;
				return;
			}

			float ratio = MathHelper.Clamp(cur / max, 0f, 1f);
			if (mp.StealthPrev >= 0.9f && ratio < mp.StealthPrev - 0.3f)
				Strike(mp);
			mp.StealthPrev = ratio;
			mp.StealthRatio = MathHelper.Lerp(mp.StealthRatio, ratio, 0.15f);

			Palette pal = Palette.Of(mp.Look.MotionColor, mp.Look.MotionColor2, Main.rand.NextFloat(0.3f));
			float k = mp.StealthRatio;
			if (k > 0.2f)
			{
				float density = (k - 0.2f) / 0.8f;
				// тёмная дымка обволакивает тело
				if (Main.rand.NextFloat() < 0.35f * density * Fx.GlobalIntensity)
				{
					Vector2 pos = p.Center + Main.rand.NextVector2Circular(p.width * 0.8f, p.height * 0.55f);
					ParticleSystem.Spawn(new SmokeParticle(pos, new Vector2(R(-0.25f, 0.25f), -R(0.1f, 0.5f) * p.gravDir) - p.velocity * 0.05f,
						new Ramp(VfxMath.Shade(pal.B, 0.2f), new Color(4, 2, 8)), R(0.55f, 0.8f), Main.rand.Next(40, 60))
					{
						MaxOpacity = 0.4f * density,
						Grow = 1.6f,
						Front = Main.rand.NextBool(3)
					});
				}
				// редкие огоньки в дымке
				if (Main.rand.NextFloat() < 0.1f * density * Fx.GlobalIntensity)
					ParticleSystem.Spawn(new GlowParticle(p.Center + Main.rand.NextVector2Circular(p.width, p.height * 0.6f), new Vector2(0f, -R(0.3f, 0.7f) * p.gravDir),
						new Ramp(pal.A, pal.B, pal.Lo), R(0.14f, 0.22f), 40) { Blink = 0.3f, Front = Main.rand.NextBool() });
			}

			if (k > 0.95f && !p.mount.Active)
			{
				// за светящимся глазом тянется росчерк
				Vector2 eye = EyePosition(p);
				ParticleSystem.Spawn(new GlowParticle(eye - p.velocity * 0.3f, new Vector2(-p.direction * R(0.3f, 0.7f), -R(0.05f, 0.25f) * p.gravDir) - p.velocity * 0.1f,
					new Ramp(pal.Hi, pal.A, pal.B), R(0.12f, 0.17f), 18) { Drag = 0.94f, Halo = 0.5f, Front = true });
			}
		}

		/// <summary>Удар из скрытности: вспышка, ударная волна, разлёт тени и искр.</summary>
		private static void Strike(LooksPlayer mp)
		{
			Player p = mp.Player;
			Palette pal = Palette.Of(mp.Look.MotionColor, mp.Look.MotionColor2);
			Vector2 c = p.Center;
			ParticleSystem.Spawn(new FlareParticle(c, Vector2.Zero, new Ramp(Color.White, pal.A, pal.B), 1.6f, 22) { Front = true });
			ParticleSystem.Spawn(new RingParticle(c, new Ramp(pal.Hi, pal.A, pal.B), 10f, 120f, 24) { Front = true });
			ParticleSystem.Spawn(new RingParticle(c, new Ramp(Color.White, pal.A), 6f, 64f, 16) { Front = true, Opacity = 0.8f });
			for (int i = 0; i < 10; i++)
			{
				Vector2 dir = (i * MathHelper.TwoPi / 10f + R(-0.2f, 0.2f)).ToRotationVector2();
				ParticleSystem.Spawn(new SmokeParticle(c + dir * 8f, dir * R(1.2f, 2.6f), new Ramp(VfxMath.Shade(pal.B, 0.2f), new Color(4, 2, 8)), R(0.6f, 0.9f), Main.rand.Next(40, 55))
				{
					MaxOpacity = 0.55f,
					Drag = 0.92f,
					Front = Main.rand.NextBool()
				});
				ParticleSystem.Spawn(new SparkParticle(c, dir * R(4f, 8f), pal.Glow, 0.6f, 22) { Drag = 0.9f, Gravity = new Vector2(0f, 0.12f * p.gravDir), Bounce = true, Front = Main.rand.NextBool() });
			}
			mp.StealthFlash = 1f;
			Lighting.AddLight(c, pal.A.ToVector3() * 1.5f);
		}

		/// <summary>
		/// Где у персонажа глаз (мир). Голова рисуется кадром 40x56 с началом в
		/// (центр X − 20, низ − 56 + 4) + headPosition; глаз в кадре ≈ (24, 21) при взгляде вправо.
		/// Покачивание головы при ходьбе — Main.OffsetsPlayerHeadgear.
		/// </summary>
		public static Vector2 EyePosition(Player p)
		{
			int frameHeight = Math.Max(1, p.bodyFrame.Height);
			int frame = Math.Clamp(p.bodyFrame.Y / frameHeight, 0, Main.OffsetsPlayerHeadgear.Length - 1);
			Vector2 bob = Main.OffsetsPlayerHeadgear[frame] * p.gravDir;
			float fx = p.direction == 1 ? 24f : 40f - 24f;
			var topLeft = new Vector2(p.position.X + p.width / 2f - 20f, p.position.Y + p.gfxOffY + p.height - 56f + 4f);
			Vector2 eye = topLeft + new Vector2(fx, 21f) + p.headPosition + bob;
			if (p.gravDir < 0f)
				eye.Y = 2f * (p.position.Y + p.gfxOffY + p.height / 2f) - eye.Y;
			return eye;
		}

		// ======================= отрисовка =======================

		/// <summary>Светящиеся глаза при полной скрытности (поверх игроков, в слое VfxRenderer).</summary>
		public static void DrawEyes(SpriteBatch sb)
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!p.active || !p.TryGetModPlayer(out LooksPlayer mp) || mp.StealthRatio < 0.6f && mp.StealthFlash <= 0f || !mp.EffectsVisible || p.mount.Active)
					continue;
				float k = MathHelper.Clamp((mp.StealthRatio - 0.6f) / 0.35f, 0f, 1f);
				k = Math.Max(k, mp.StealthFlash);
				if (k <= 0.01f)
					continue;
				Palette pal = Palette.Of(mp.Look.MotionColor, mp.Look.MotionColor2);
				Vector2 pos = EyePosition(p) - Main.screenPosition;
				float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + i);
				Texture2D bloom = VfxAssets.Bloom;
				Texture2D glint = VfxAssets.Glint;
				Texture2D streak = VfxAssets.Streak;
				sb.Draw(bloom, pos, null, VfxMath.Additive(pal.B, 0.35f * k), 0f, bloom.Size() / 2f, 0.12f * pulse, SpriteEffects.None, 0f);
				// вытянутый блик вдоль взгляда
				sb.Draw(streak, pos, null, VfxMath.Additive(pal.A, 0.8f * k), 0f, streak.Size() / 2f, new Vector2(0.16f, 0.16f) * pulse, SpriteEffects.None, 0f);
				sb.Draw(glint, pos, null, VfxMath.Additive(Color.White, 0.85f * k), 0f, glint.Size() / 2f, 0.22f * pulse * (1f + mp.StealthFlash), SpriteEffects.None, 0f);
			}
		}

		/// <summary>Светящийся контур при полной скрытности (копии игрока позади него).</summary>
		public static void DrawOutline(LooksPlayer mp, Camera camera)
		{
			float k = MathHelper.Clamp((mp.StealthRatio - 0.7f) / 0.25f, 0f, 1f);
			if (k <= 0.01f || !mp.Look.StealthFx)
				return;
			OutlineEffect.Draw(mp, camera, mp.Look.MotionColor, mp.Look.MotionColor2, 0.85f * k);
		}
	}

	/// <summary>Сброс статических данных эффектов при выгрузке мода.</summary>
	public class EffectsCleanupSystem : ModSystem
	{
		public override void Unload()
		{
			StealthEffects.Unload();
			WeaponTrails.Unload();
		}
	}
}
