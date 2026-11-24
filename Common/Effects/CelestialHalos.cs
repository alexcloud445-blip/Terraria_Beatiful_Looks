using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using BeautifulLooks.Common.Looks;
using BeautifulLooks.Common.VFX;

namespace BeautifulLooks.Common.Effects
{
	/// <summary>
	/// Нимбы v0.6: венец из рун, звёздная диадема, рога из света, нимб-затмение.
	/// front = true — части перед головой (HaloLayer), false — за головой (HaloBackLayer).
	/// </summary>
	internal static class CelestialHalos
	{
		/// <summary>Есть ли у стиля часть за головой (но перед телом).</summary>
		public static bool HasBackPart(HaloStyle style) =>
			style == HaloStyle.RuneCrown || style == HaloStyle.Horns;

		/// <summary>
		/// Нимб-затмение рисуется в самом заднем слое (BackEffectsLayer): тёмный диск должен быть за телом,
		/// иначе он закрыл бы плечи и грудь.
		/// </summary>
		public static void DrawBehindBody(ref PlayerDrawSet info, LookSettings look)
		{
			if (look.Halo != HaloStyle.Eclipse)
				return;
			Player p = info.drawPlayer;
			Palette pal = Palette.Of(look.EffectColor, look.EffectColor2);
			Vector2 center = LayerUtils.Center(info);
			EclipseHalo(ref info, EffectDyes.ShaderId(look.EffectDye), pal, center + new Vector2(-p.direction * 2f, (-p.height / 2f + 9f) * p.gravDir), Main.GlobalTimeWrappedHourly);
		}

		public static void Draw(ref PlayerDrawSet info, LookSettings look, bool front)
		{
			Player p = info.drawPlayer;
			int shader = EffectDyes.ShaderId(look.EffectDye);
			float time = Main.GlobalTimeWrappedHourly;
			float grav = p.gravDir;
			int dir = p.direction;
			Vector2 center = LayerUtils.Center(info);
			Vector2 headTop = center + new Vector2(0f, -p.height / 2f * grav);
			Palette pal = Palette.Of(look.EffectColor, look.EffectColor2);

			switch (look.Halo)
			{
				case HaloStyle.RuneCrown:
					RuneCrown(ref info, shader, pal, headTop + new Vector2(0f, -4f * grav), time, front);
					break;
				case HaloStyle.Diadem:
					if (front)
						Diadem(ref info, shader, pal, headTop + new Vector2(0f, 1f * grav), time, grav);
					break;
				case HaloStyle.Horns:
					if (front)
						Horn(ref info, shader, pal, headTop + new Vector2(dir * 3f, 3f * grav), dir, grav, time, 1f, false);
					else
						Horn(ref info, shader, pal, headTop + new Vector2(-dir * 5f, 1f * grav), dir, grav, time + 0.4f, 0.9f, true);
					break;
			}
		}

		/// <summary>Восемь рун по эллипсу вокруг макушки: ближние ярче и крупнее, дальние уходят за голову.</summary>
		private static void RuneCrown(ref PlayerDrawSet info, int shader, Palette pal, Vector2 pos, float time, bool front)
		{
			const float rx = 15f, ry = 4.5f;
			if (!front)
			{
				Texture2D bloom = VfxAssets.Bloom;
				LayerUtils.Add(ref info, shader, bloom, pos, null, VfxMath.Additive(pal.B, 0.3f), 0f, bloom.Size() / 2f, new Vector2(0.32f, 0.12f));
				Texture2D ring = VfxAssets.HaloRing;
				LayerUtils.Add(ref info, shader, ring, pos, null, VfxMath.Additive(pal.A, 0.45f), 0f, ring.Size() / 2f, new Vector2(rx * 2f / 27f));
			}

			const int runes = 8;
			for (int k = 0; k < runes; k++)
			{
				float a = time * 1.1f + k * MathHelper.TwoPi / runes;
				float sin = (float)Math.Sin(a);
				if (sin >= 0f != front)
					continue;
				float depth = 0.5f + 0.5f * sin;
				Vector2 rp = pos + VfxMath.OnEllipse(a, rx, ry);
				Color c = VfxMath.LerpVivid(pal.A, pal.Hi, 0.25f + 0.35f * depth);
				LayerUtils.Add(ref info, shader, VfxAssets.Bloom, rp, VfxMath.Additive(pal.B, 0.2f * depth), 0.09f);
				LayerUtils.Add(ref info, shader, VfxAssets.Runes, rp, VfxAssets.RuneFrame(k), VfxMath.Additive(c, 0.45f + 0.55f * depth), 0f,
					new Vector2(16f, 16f), new Vector2(0.3f * (0.8f + 0.2f * depth)));
			}
		}

		/// <summary>Диадема: тонкий обруч и пять звёзд-зубцов, по которым пробегает мерцание.</summary>
		private static void Diadem(ref PlayerDrawSet info, int shader, Palette pal, Vector2 basePos, float time, float grav)
		{
			Texture2D ring = VfxAssets.HaloRing;
			LayerUtils.Add(ref info, shader, ring, basePos, null, VfxMath.Additive(pal.A, 0.75f), 0f, ring.Size() / 2f, new Vector2(24f / 27f, 0.8f));

			float[] xs = { -10f, -5f, 0f, 5f, 10f };
			float[] hs = { 4f, 7f, 10f, 7f, 4f };
			float[] sizes = { 0.32f, 0.4f, 0.55f, 0.4f, 0.32f };
			for (int i = 0; i < xs.Length; i++)
			{
				Vector2 foot = basePos + new Vector2(xs[i], 1.5f * grav);
				Vector2 star = basePos + new Vector2(xs[i], -hs[i] * grav);
				LayerUtils.Line(ref info, shader, foot, star, VfxMath.Additive(pal.A, 0.55f), 3f);

				float twinkle = (float)Math.Pow(Math.Max(0.0, Math.Sin(time * 2.6f - i * 0.9f)), 8.0);
				Shape shape = i == 2 ? Shape.Star8 : Shape.Star4;
				Rectangle frame = Shapes.Frame(shape);
				float s = sizes[i] * (1f + 0.15f * twinkle);
				LayerUtils.Add(ref info, shader, VfxAssets.ShapesGlow, star, frame, VfxMath.Additive(pal.B, 0.55f), 0f, Shapes.Origin, new Vector2(s * 1.2f));
				LayerUtils.Add(ref info, shader, VfxAssets.Shapes, star, frame, VfxMath.Additive(VfxMath.LerpVivid(pal.A, pal.Hi, 0.4f + 0.6f * twinkle), 0.95f), 0f, Shapes.Origin, new Vector2(s));
				if (twinkle > 0.05f)
					LayerUtils.Add(ref info, shader, VfxAssets.Glint, star, null, VfxMath.Additive(Color.White, 0.8f * twinkle), 0f, new Vector2(32f, 32f), new Vector2(0.35f * twinkle + 0.1f));
			}
		}

		/// <summary>
		/// Рог из света: изогнутая кривая от макушки вверх и назад, сужается к кончику.
		/// От основания к кончику бежит светлый импульс. Дальний рог темнее и меньше.
		/// </summary>
		private static void Horn(ref PlayerDrawSet info, int shader, Palette pal, Vector2 basePos, int dir, float grav, float time, float size, bool far)
		{
			// дальний рог смотрит чуть выше — так оба рога читаются раздельно
			Vector2 p1 = basePos + new Vector2(dir * 2f, -12f * grav) * size;
			Vector2 p2 = basePos + new Vector2(-dir * (far ? 9f : 12f), (far ? -21f : -19f) * grav) * size;
			const int segs = 12;
			float pulseAt = VfxMath.Frac(time * 0.6f);
			Vector2 prev = basePos;
			for (int i = 1; i <= segs; i++)
			{
				float t = i / (float)segs;
				float u = 1f - t;
				Vector2 point = u * u * basePos + 2f * u * t * p1 + t * t * p2;
				float mid = t - 0.5f / segs;
				float width = (float)Math.Pow(1f - mid, 0.8f) * 9f + 1.5f;
				float pulse = (float)Math.Exp(-Math.Pow((mid - pulseAt) * 5f, 2));
				Color core = VfxMath.LerpVivid(pal.A, pal.Hi, 0.2f + 0.6f * mid + 0.3f * pulse);
				if (far)
					core = VfxMath.Shade(VfxMath.LerpVivid(core, pal.Lo, 0.4f), 0.7f);
				float alpha = far ? 0.5f : 0.9f;
				LayerUtils.Line(ref info, shader, prev, point, VfxMath.Additive(far ? pal.Lo : pal.B, 0.25f * alpha), width * 2.4f * size);
				LayerUtils.Line(ref info, shader, prev, point, VfxMath.Additive(core, alpha * (0.7f + 0.5f * pulse)), width * size);
				prev = point;
			}

			float twinkle = 0.6f + 0.4f * (float)Math.Sin(time * 4f);
			if (!far)
				LayerUtils.Add(ref info, shader, VfxAssets.Glint, p2, null, VfxMath.Additive(Color.White, 0.6f * twinkle), 0f, new Vector2(32f, 32f), new Vector2(0.25f * twinkle));
		}

		/// <summary>Маленькое затмение за головой: тёмный диск, корона и блик на краю.</summary>
		private static void EclipseHalo(ref PlayerDrawSet info, int shader, Palette pal, Vector2 pos, float time)
		{
			const float radius = 12f;
			Texture2D bloom = VfxAssets.Bloom;
			LayerUtils.Add(ref info, shader, bloom, pos, VfxMath.Additive(pal.B, 0.3f), 0.55f);
			Texture2D corona = VfxAssets.Corona;
			LayerUtils.Add(ref info, shader, corona, pos, null, VfxMath.Additive(pal.B, 0.4f), -time * 0.1f, corona.Size() / 2f, new Vector2(radius / 23f * 1.25f));
			LayerUtils.Add(ref info, shader, corona, pos, null, VfxMath.Additive(pal.A, 0.95f), time * 0.08f, corona.Size() / 2f, new Vector2(radius / 23f));
			Texture2D disc = VfxAssets.Disc;
			LayerUtils.Add(ref info, shader, disc, pos, null, new Color(4, 2, 10) * 0.96f, 0f, disc.Size() / 2f, new Vector2(radius / 30f));
			Texture2D ring = VfxAssets.RingWave;
			LayerUtils.Add(ref info, shader, ring, pos, null, VfxMath.Additive(pal.Hi, 0.6f), 0f, ring.Size() / 2f, new Vector2(radius / 53.8f));
			Vector2 diamond = pos + (time * 0.5f).ToRotationVector2() * radius;
			float sparkle = 0.6f + 0.4f * (float)Math.Sin(time * 3.3f);
			LayerUtils.Add(ref info, shader, VfxAssets.Glint, diamond, null, VfxMath.Additive(Color.White, 0.85f * sparkle), 0f, new Vector2(32f, 32f), new Vector2(0.35f * sparkle));
		}
	}

	/// <summary>Части нимбов, которые находятся за головой (дальние руны, дальний рог, нимб-затмение).</summary>
	public class HaloBackLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Head);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
			LayerUtils.Visible(drawInfo) && CelestialHalos.HasBackPart(LayerUtils.Mp(drawInfo).Look.Halo);

		protected override void Draw(ref PlayerDrawSet drawInfo) =>
			CelestialHalos.Draw(ref drawInfo, LayerUtils.Mp(drawInfo).Look, false);
	}
}
