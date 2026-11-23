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

		/// <summary>Случайная точка на эллипсе вокруг центра.</summary>
		public static Vector2 OnEllipse(float angle, float rx, float ry) => new Vector2((float)Math.Cos(angle) * rx, (float)Math.Sin(angle) * ry);
	}
}
