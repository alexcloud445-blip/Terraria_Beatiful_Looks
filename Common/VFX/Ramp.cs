using System;
using Microsoft.Xna.Framework;
using BeautifulLooks.Common.Looks;

namespace BeautifulLooks.Common.VFX
{
	/// <summary>
	/// Градиент из 2–4 цветов. Цвета смешиваются по цветовому кругу (HSV), а не по RGB,
	/// поэтому переход, например, от розового к голубому идёт через сиреневый, а не через серый.
	/// </summary>
	public struct Ramp
	{
		private Color _a, _b, _c, _d;
		private int _count;

		public Ramp(Color a, Color b)
		{
			_a = a;
			_b = b;
			_c = _d = b;
			_count = 2;
		}

		public Ramp(Color a, Color b, Color c)
		{
			_a = a;
			_b = b;
			_c = c;
			_d = c;
			_count = 3;
		}

		public Ramp(Color a, Color b, Color c, Color d)
		{
			_a = a;
			_b = b;
			_c = c;
			_d = d;
			_count = 4;
		}

		/// <summary>Один цвет без перехода (так любой Color превращается в Ramp).</summary>
		public static implicit operator Ramp(Color c) => new Ramp(c, c);

		private Color Stop(int i) => i switch { 0 => _a, 1 => _b, 2 => _c, _ => _d };

		/// <summary>Цвет в точке t (0 — начало, 1 — конец).</summary>
		public Color At(float t)
		{
			if (_count < 2)
				return _a;
			t = MathHelper.Clamp(t, 0f, 1f);
			float x = t * (_count - 1);
			int i = Math.Min((int)x, _count - 2);
			return VfxMath.LerpVivid(Stop(i), Stop(i + 1), x - i);
		}

		public Color Start => _a;

		public Color End => Stop(Math.Max(1, _count) - 1);
	}

	/// <summary>
	/// Палитра из двух цветов образа: светлый блик, основной, второй и тёмный.
	/// Из неё собираются градиенты для частиц.
	/// </summary>
	public struct Palette
	{
		/// <summary>Почти белый оттенок основного цвета (сердцевина свечения).</summary>
		public Color Hi;
		/// <summary>Основной цвет.</summary>
		public Color A;
		/// <summary>Второй цвет.</summary>
		public Color B;
		/// <summary>Тёмный насыщенный оттенок второго цвета (угасание).</summary>
		public Color Lo;

		public Palette(Color a, Color b)
		{
			A = a;
			B = b;
			Hi = Color.Lerp(a, Color.White, 0.62f);
			Lo = VfxMath.Shade(b, 0.42f, 1.15f);
		}

		/// <summary>Палитра из двух цветовых настроек; offset сдвигает радугу, если она включена.</summary>
		public static Palette Of(ColorSetting first, ColorSetting second, float offset = 0f) =>
			new Palette(first.Get(offset), second.Get(offset + 0.12f));

		/// <summary>Белое ядро → основной → второй → тёмный: огонь, вспышки, искры.</summary>
		public Ramp Hot => new Ramp(Hi, A, B, Lo);

		/// <summary>Основной → второй: мягкие огоньки.</summary>
		public Ramp Soft => new Ramp(A, B);

		/// <summary>Блик → основной → второй: всё, что светится и гаснет.</summary>
		public Ramp Glow => new Ramp(Hi, A, B);

		/// <summary>Второй → основной: обратный переход.</summary>
		public Ramp Reverse => new Ramp(B, A);

		/// <summary>Случайный цвет между основным и вторым.</summary>
		public Color Mix(float t) => VfxMath.LerpVivid(A, B, t);
	}
}
