using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using BeautifulLooks.Common.Configs;
using BeautifulLooks.Common.VFX;

namespace BeautifulLooks.Common.Effects
{
	/// <summary>Короткие помощники для частиц (используются крыльями и костюмами).</summary>
	public static class Fx
	{
		/// <summary>Общий множитель из настроек мода.</summary>
		public static float GlobalIntensity => ModContent.GetInstance<LooksClientConfig>()?.AuraIntensity ?? 1f;

		/// <summary>Светящаяся искорка (градиент от светлого к цвету).</summary>
		public static void Spark(Vector2 pos, Vector2 vel, Color color, float scale = 0.9f) =>
			ParticleSystem.Spawn(new GlowParticle(pos, vel, new Ramp(Color.Lerp(color, Color.White, 0.5f), color), scale * 0.45f, Main.rand.Next(30, 45)) { Front = Main.rand.NextBool() });

		public static void Star(Vector2 pos, Vector2 vel, Color color, float scale = 1f) =>
			ParticleSystem.Spawn(new ShapeParticle(Shape.Star4, SpriteMotion.Spin, pos, vel, new Ramp(Color.White, color), scale * 0.7f, 40)
			{
				Glowing = true,
				Spin = Main.rand.NextFloat(-0.08f, 0.08f),
				Front = Main.rand.NextBool()
			});

		public static void Heart(Vector2 pos, Vector2 vel, Color color, float scale = 1f) =>
			ParticleSystem.Spawn(new ShapeParticle(Shape.Heart, SpriteMotion.Float, pos, vel, color, scale * 0.8f, 70) { GlowStrength = 0.5f });

		public static void Petal(Vector2 pos, Vector2 vel, Color color, float scale = 1f) =>
			ParticleSystem.Spawn(new ShapeParticle(Shape.Petal, SpriteMotion.Fall, pos, vel, color, scale * 0.85f, 100) { GlowStrength = 0.3f, Settle = true });

		public static void Snow(Vector2 pos, Vector2 vel, Color color, float scale = 1f) =>
			ParticleSystem.Spawn(new ShapeParticle(Shape.Snowflake, SpriteMotion.Drift, pos, vel, new Ramp(Color.White, color), scale * 0.75f, 90) { Glowing = true, Settle = true });

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
