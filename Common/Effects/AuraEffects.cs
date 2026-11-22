using Microsoft.Xna.Framework;
using Terraria;
using Terraria_Beatiful_Looks.Common.Looks;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>Частицы ауры. Каждый стиль — отдельный метод.</summary>
	public static class AuraEffects
	{
		public static void Update(Player player, LookSettings look)
		{
			if (look.Aura == AuraStyle.None)
				return;

			float d = look.AuraDensity * Fx.GlobalIntensity;
			ColorSetting col = look.AuraColor;

			Lighting.AddLight(player.Center, col.Get().ToVector3() * 0.45f * Fx.GlobalIntensity);

			switch (look.Aura)
			{
				case AuraStyle.Glow: Glow(player, col, d); break;
				case AuraStyle.Sparkles: Sparkles(player, col, d); break;
				case AuraStyle.Orbit: Orbit(player, col, d); break;
				case AuraStyle.Flames: Flames(player, col, d); break;
				case AuraStyle.Stars: Stars(player, col, d); break;
				case AuraStyle.Snow: Snow(player, col, d); break;
				case AuraStyle.Petals: Petals(player, col, d); break;
				case AuraStyle.Hearts: Hearts(player, col, d); break;
				case AuraStyle.Vortex: Vortex(player, col, d); break;
				case AuraStyle.Ring: Ring(player, col, d); break;
			}
		}

		private static float Rand01 => Main.rand.NextFloat();

		private static void Glow(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.15f * d); i > 0; i--)
				Fx.Spark(p.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -4f),
					new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.4f)), c.Get(Rand01), 0.9f);
		}

		private static void Sparkles(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.6f * d); i > 0; i--)
				Fx.Spark(p.Center + Main.rand.NextVector2Circular(p.width, p.height * 0.6f),
					Main.rand.NextVector2Circular(0.5f, 0.5f) + new Vector2(0f, -0.4f), c.Get(Rand01), 0.8f);
		}

		private static void Orbit(Player p, ColorSetting c, float d)
		{
			for (int i = 0; i < 3; i++)
			{
				if (Rand01 > d)
					continue;
				float angle = Main.GameUpdateCount * 0.07f + i * MathHelper.TwoPi / 3f;
				Vector2 offset = angle.ToRotationVector2() * new Vector2(30f, 20f);
				Fx.Spark(p.Center + offset, Vector2.Zero, c.Get(i / 3f), 1.0f);
			}
		}

		private static void Flames(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(1.2f * d); i > 0; i--)
			{
				Vector2 pos = p.Bottom + new Vector2(Main.rand.NextFloat(-11f, 11f), -2f);
				Vector2 vel = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(1.5f, 3.2f));
				Color color = Color.Lerp(Color.White, c.Get(Rand01), Main.rand.NextFloat(0.5f, 1f));
				Fx.Spark(pos, vel, color, Main.rand.NextFloat(0.9f, 1.3f));
			}
		}

		private static void Stars(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.25f * d); i > 0; i--)
				Fx.Star(p.Center + Main.rand.NextVector2Circular(34f, 34f), Vector2.Zero, c.Get(Rand01), Main.rand.NextFloat(0.8f, 1.2f));
		}

		private static void Snow(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.3f * d); i > 0; i--)
				Fx.Snow(p.Top + new Vector2(Main.rand.NextFloat(-36f, 36f), -Main.rand.NextFloat(8f, 24f)),
					new Vector2(0f, 0.4f), c.Get(Rand01), Main.rand.NextFloat(0.8f, 1.1f));
		}

		private static void Petals(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.25f * d); i > 0; i--)
				Fx.Petal(p.Top + new Vector2(Main.rand.NextFloat(-40f, 40f), -Main.rand.NextFloat(8f, 24f)),
					new Vector2(0f, 0.3f), c.Get(Rand01), Main.rand.NextFloat(0.9f, 1.2f));
		}

		private static void Hearts(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(0.12f * d); i > 0; i--)
				Fx.Heart(p.Center + new Vector2(Main.rand.NextFloat(-20f, 20f), Main.rand.NextFloat(-10f, 16f)),
					new Vector2(0f, -Main.rand.NextFloat(0.5f, 1f)), c.Get(Rand01), Main.rand.NextFloat(0.9f, 1.2f));
		}

		private static void Vortex(Player p, ColorSetting c, float d)
		{
			for (int i = Fx.Rolls(1f * d); i > 0; i--)
			{
				float angle = Main.rand.NextFloat(MathHelper.TwoPi);
				Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(34f, 46f);
				// скорость — к центру с закруткой
				Vector2 vel = (-offset).SafeNormalize(Vector2.Zero).RotatedBy(1.1f) * 2.2f;
				Fx.Spark(p.Center + offset, vel, c.Get(angle / MathHelper.TwoPi), 0.85f);
			}
		}

		private static void Ring(Player p, ColorSetting c, float d)
		{
			const int points = 10;
			for (int i = 0; i < points; i++)
			{
				if (Rand01 > 0.5f * d)
					continue;
				float angle = Main.GameUpdateCount * 0.04f + i * MathHelper.TwoPi / points;
				Vector2 offset = angle.ToRotationVector2() * new Vector2(28f, 7f);
				Fx.Spark(p.Bottom + new Vector2(0f, -3f) + offset, new Vector2(0f, -0.4f), c.Get(i / (float)points), 0.75f);
			}
		}
	}
}
