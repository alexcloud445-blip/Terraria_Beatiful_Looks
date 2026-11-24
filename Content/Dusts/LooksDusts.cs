using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BeautifulLooks.Content.Dusts
{
	/// <summary>
	/// Базовая частица мода: белая текстура из 3 вариантов 10x10 (сверху вниз),
	/// красится цветом из Dust.NewDustPerfect(..., newColor), светится сама.
	/// </summary>
	public abstract class LooksDust : ModDust
	{
		/// <summary>Аддитивное «светящееся» смешивание (для звёзд и снежинок).</summary>
		protected virtual bool Additive => false;

		protected virtual float Shrink => 0.012f;

		public override void OnSpawn(Dust dust)
		{
			dust.frame = new Rectangle(0, Main.rand.Next(3) * 10, 10, 10);
			dust.noGravity = true;
			dust.noLight = true;
			dust.rotation = Main.rand.NextFloat(-0.4f, 0.4f);
			dust.fadeIn = Main.rand.NextFloat(100f); // используем как таймер/фазу
		}

		public override bool Update(Dust dust)
		{
			dust.fadeIn += 1f;
			Move(dust);
			dust.position += dust.velocity;
			dust.scale -= Shrink;
			if (dust.scale < 0.2f)
				dust.active = false;
			return false;
		}

		protected virtual void Move(Dust dust)
		{
			dust.velocity *= 0.97f;
		}

		public override Color? GetAlpha(Dust dust, Color lightColor)
		{
			Color c = dust.color * ((255 - dust.alpha) / 255f);
			if (Additive)
				c.A = 0;
			return c;
		}
	}

	/// <summary>Сердечко: медленно всплывает и покачивается.</summary>
	public class HeartDust : LooksDust
	{
		protected override float Shrink => 0.01f;

		protected override void Move(Dust dust)
		{
			dust.velocity.X = (float)System.Math.Sin(dust.fadeIn * 0.1f) * 0.4f;
			dust.velocity.Y *= 0.98f;
		}
	}

	/// <summary>Лепесток сакуры: падает, кружась.</summary>
	public class PetalDust : LooksDust
	{
		protected override float Shrink => 0.006f;

		protected override void Move(Dust dust)
		{
			dust.velocity.Y = System.Math.Min(dust.velocity.Y + 0.03f, 1.2f);
			dust.velocity.X = (float)System.Math.Sin(dust.fadeIn * 0.07f) * 0.8f;
			dust.rotation += 0.05f;
		}
	}

	/// <summary>Звёздочка: мерцает на месте.</summary>
	public class StarDust : LooksDust
	{
		protected override bool Additive => true;
		protected override float Shrink => 0.02f;

		protected override void Move(Dust dust)
		{
			dust.velocity *= 0.92f;
			dust.rotation += 0.03f;
		}
	}

	/// <summary>Снежинка: плавно падает.</summary>
	public class SnowflakeDust : LooksDust
	{
		protected override bool Additive => true;
		protected override float Shrink => 0.007f;

		protected override void Move(Dust dust)
		{
			dust.velocity.Y = System.Math.Min(dust.velocity.Y + 0.02f, 0.9f);
			dust.velocity.X = (float)System.Math.Sin(dust.fadeIn * 0.05f) * 0.4f;
			dust.rotation += 0.02f;
		}
	}
}
