using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>Мелкие помощники для эффектов.</summary>
	public static class VfxMath
	{
		/// <summary>Цвет «как свет»: при обычном смешивании складывается с фоном (A = 0).</summary>
		public static Color Additive(Color c, float opacity = 1f)
		{
			Color r = c * opacity;
			r.A = 0;
			return r;
		}

		/// <summary>Плавное появление в начале и угасание в конце (t от 0 до 1).</summary>
		public static float FadeInOut(float t, float fadeIn = 0.15f, float fadeOutStart = 0.6f)
		{
			if (t < fadeIn)
				return Ease(t / fadeIn);
			if (t > fadeOutStart)
				return 1f - Ease((t - fadeOutStart) / (1f - fadeOutStart));
			return 1f;
		}

		/// <summary>Сглаженная ступенька 0..1.</summary>
		public static float Ease(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		/// <summary>Синусоидальная «вспышка»: 0 → 1 → 0.</summary>
		public static float Bump(float t) => (float)Math.Sin(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi);

		public static float Time => Main.GlobalTimeWrappedHourly;

		/// <summary>Дробная часть (всегда 0..1, в том числе для отрицательных чисел).</summary>
		public static float Frac(float v) => v - (float)Math.Floor(v);

		/// <summary>Появление с лёгким «перескоком» (0 → 1.12 → 1), для частиц, которые «выпрыгивают».</summary>
		public static float PopIn(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			float s = 1.7f;
			t -= 1f;
			return 1f + t * t * ((s + 1f) * t + s);
		}

		// ---------- цвет ----------

		/// <summary>RGB → HSV (h 0..1, s 0..1, v 0..1).</summary>
		public static Vector3 ToHsv(Color c)
		{
			float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
			float max = Math.Max(r, Math.Max(g, b));
			float min = Math.Min(r, Math.Min(g, b));
			float d = max - min;
			float h = 0f;
			if (d > 0.0001f)
			{
				if (max == r)
					h = (g - b) / d / 6f;
				else if (max == g)
					h = ((b - r) / d + 2f) / 6f;
				else
					h = ((r - g) / d + 4f) / 6f;
				h = Frac(h);
			}
			float s = max <= 0f ? 0f : d / max;
			return new Vector3(h, s, max);
		}

		/// <summary>HSV → RGB.</summary>
		public static Color FromHsv(float h, float s, float v, byte a = 255)
		{
			h = Frac(h) * 6f;
			int i = (int)h;
			float f = h - i;
			float p = v * (1f - s), q = v * (1f - s * f), t = v * (1f - s * (1f - f));
			float r, g, b;
			switch (i % 6)
			{
				case 0: r = v; g = t; b = p; break;
				case 1: r = q; g = v; b = p; break;
				case 2: r = p; g = v; b = t; break;
				case 3: r = p; g = q; b = v; break;
				case 4: r = t; g = p; b = v; break;
				default: r = v; g = p; b = q; break;
			}
			return new Color(r, g, b) { A = a };
		}

		/// <summary>
		/// «Живое» смешивание цветов: оттенок идёт по короткой дуге цветового круга, поэтому
		/// не получается грязно-серой середины. С белым/серым смешивается обычным способом.
		/// </summary>
		public static Color LerpVivid(Color a, Color b, float t)
		{
			if (t <= 0f)
				return a;
			if (t >= 1f)
				return b;
			Vector3 ha = ToHsv(a), hb = ToHsv(b);
			byte alpha = (byte)(a.A + (b.A - a.A) * t);
			if (ha.Y < 0.15f || hb.Y < 0.15f)
				return Color.Lerp(a, b, t);
			float dh = hb.X - ha.X;
			if (dh > 0.5f)
				dh -= 1f;
			else if (dh < -0.5f)
				dh += 1f;
			return FromHsv(ha.X + dh * t, MathHelper.Lerp(ha.Y, hb.Y, t), MathHelper.Lerp(ha.Z, hb.Z, t), alpha);
		}

		/// <summary>Темнее (brightness &lt; 1) и насыщеннее (saturation &gt; 1).</summary>
		public static Color Shade(Color c, float brightness, float saturation = 1f)
		{
			Vector3 hsv = ToHsv(c);
			return FromHsv(hsv.X, MathHelper.Clamp(hsv.Y * saturation, 0f, 1f), MathHelper.Clamp(hsv.Z * brightness, 0f, 1f), c.A);
		}

		/// <summary>Сдвиг оттенка (переливы).</summary>
		public static Color HueShift(Color c, float amount)
		{
			Vector3 hsv = ToHsv(c);
			return FromHsv(hsv.X + amount, hsv.Y, hsv.Z, c.A);
		}

		/// <summary>Случайная точка на эллипсе вокруг центра.</summary>
		public static Vector2 OnEllipse(float angle, float rx, float ry) => new Vector2((float)Math.Cos(angle) * rx, (float)Math.Sin(angle) * ry);
	}
}
