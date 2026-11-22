using Microsoft.Xna.Framework;
using Terraria;
using Terraria_Beatiful_Looks.Common.Looks;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>След, который остаётся за персонажем при движении.</summary>
	public static class TrailEffects
	{
		public static void Update(Player player, LookSettings look)
		{
			if (look.Trail == TrailStyle.None)
				return;

			float speed = player.velocity.Length();
			if (speed < 1.5f)
				return;

			// Чем быстрее движемся — тем гуще след
			float chance = MathHelper.Clamp(speed / 6f, 0.25f, 1.5f) * Fx.GlobalIntensity;
			ColorSetting col = look.TrailColor;

			for (int i = Fx.Rolls(chance); i > 0; i--)
			{
				Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-4f, 18f))
					- player.velocity * Main.rand.NextFloat(0.5f, 1.5f);
				Vector2 vel = -player.velocity * 0.08f;
				Color c = col.Get(Main.rand.NextFloat());

				switch (look.Trail)
				{
					case TrailStyle.Sparkles: Fx.Spark(pos, vel, c, 0.9f); break;
					case TrailStyle.Stars: Fx.Star(pos, vel, c, 1f); break;
					case TrailStyle.Flames: Fx.Spark(pos, vel + new Vector2(0f, -1.2f), Color.Lerp(Color.White, c, 0.8f), 1.2f); break;
					case TrailStyle.Hearts: Fx.Heart(pos, vel + new Vector2(0f, -0.5f), c, 0.9f); break;
					case TrailStyle.Petals: Fx.Petal(pos, vel, c, 1f); break;
					case TrailStyle.Snow: Fx.Snow(pos, vel, c, 0.9f); break;
				}
			}
		}
	}
}
