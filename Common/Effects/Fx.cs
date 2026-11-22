using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Configs;
using Terraria_Beatiful_Looks.Content.Dusts;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>Вспомогательные функции для частиц.</summary>
	public static class Fx
	{
		/// <summary>Общий множитель из настроек мода.</summary>
		public static float GlobalIntensity => ModContent.GetInstance<LooksClientConfig>().AuraIntensity;

		/// <summary>Цветная светящаяся искра (ванильная пыль RainbowMk2).</summary>
		public static Dust Spark(Vector2 pos, Vector2 vel, Color color, float scale = 0.9f)
		{
			Dust d = Dust.NewDustPerfect(pos, DustID.RainbowMk2, vel, 0, color, scale);
			d.noGravity = true;
			return d;
		}

		public static Dust Custom<T>(Vector2 pos, Vector2 vel, Color color, float scale = 1f) where T : ModDust
		{
			return Dust.NewDustPerfect(pos, ModContent.DustType<T>(), vel, 0, color, scale);
		}

		public static Dust Star(Vector2 pos, Vector2 vel, Color color, float scale = 1f) => Custom<StarDust>(pos, vel, color, scale);
		public static Dust Heart(Vector2 pos, Vector2 vel, Color color, float scale = 1f) => Custom<HeartDust>(pos, vel, color, scale);
		public static Dust Petal(Vector2 pos, Vector2 vel, Color color, float scale = 1f) => Custom<PetalDust>(pos, vel, color, scale);
		public static Dust Snow(Vector2 pos, Vector2 vel, Color color, float scale = 1f) => Custom<SnowflakeDust>(pos, vel, color, scale);

		/// <summary>Срабатывает с вероятностью chance (может быть больше 1 — тогда иногда несколько раз).</summary>
		public static int Rolls(float chance)
		{
			int n = (int)chance;
			if (Main.rand.NextFloat() < chance - n)
				n++;
			return n;
		}
	}
}
