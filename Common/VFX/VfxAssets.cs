using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>
	/// Текстуры эффектов из Assets/VFX (генерируются скриптом tools/make_vfx.py).
	/// Все белые: цвет задаётся при отрисовке.
	/// </summary>
	[Autoload(Side = ModSide.Client)]
	public class VfxAssets : ModSystem
	{
		private const string Folder = "Terraria_Beatiful_Looks/Assets/VFX/";

		private static Asset<Texture2D> _bloom, _orb, _flare, _streak, _line, _ringWave, _godRay,
			_smoke, _sprites, _runes, _haloRing, _groundRing, _thornHalo, _sigilRings;

		/// <summary>Мягкое гауссово пятно 128x128.</summary>
		public static Texture2D Bloom => _bloom.Value;
		/// <summary>Светящийся шарик с ядром 64x64.</summary>
		public static Texture2D Orb => _orb.Value;
		/// <summary>Четырёхлучевая вспышка 128x128.</summary>
		public static Texture2D Flare => _flare.Value;
		/// <summary>Вытянутая искра 128x32 (вдоль X).</summary>
		public static Texture2D Streak => _streak.Value;
		/// <summary>Мягкая линия 32x16 (растягивается по X).</summary>
		public static Texture2D Line => _line.Value;
		/// <summary>Тонкое кольцо 128x128.</summary>
		public static Texture2D RingWave => _ringWave.Value;
		/// <summary>Луч света 32x128, основание внизу.</summary>
		public static Texture2D GodRay => _godRay.Value;
		/// <summary>4 кадра дыма 64x64 слева направо.</summary>
		public static Texture2D Smoke => _smoke.Value;
		/// <summary>8 спрайтов 32x32: сердце, лепесток, снежинка, звезда, осколок, перо, бабочка x2.</summary>
		public static Texture2D Sprites => _sprites.Value;
		/// <summary>8 рун 32x32.</summary>
		public static Texture2D Runes => _runes.Value;
		/// <summary>Кольцо нимба в перспективе 32x12 (эллипс 27x7.6, рисовать почти 1:1).</summary>
		public static Texture2D HaloRing => _haloRing.Value;
		/// <summary>Кольцо на земле в перспективе 128x40 (эллипс 120x34).</summary>
		public static Texture2D GroundRing => _groundRing.Value;
		/// <summary>Терновый венец в перспективе 40x18, центр эллипса в (20, 11).</summary>
		public static Texture2D ThornHalo => _thornHalo.Value;
		/// <summary>Кольца магического круга в перспективе 128x40 (внешний эллипс 124x37).</summary>
		public static Texture2D SigilRings => _sigilRings.Value;

		public static Rectangle SpriteFrame(int index) => new Rectangle(index * 32, 0, 32, 32);

		public static Rectangle RuneFrame(int index) => new Rectangle((index & 7) * 32, 0, 32, 32);

		public static Rectangle SmokeFrame(int index) => new Rectangle((index & 3) * 64, 0, 64, 64);

		public override void Load()
		{
			_bloom = Request("Bloom");
			_orb = Request("Orb");
			_flare = Request("Flare");
			_streak = Request("Streak");
			_line = Request("Line");
			_ringWave = Request("RingWave");
			_godRay = Request("GodRay");
			_smoke = Request("Smoke");
			_sprites = Request("Sprites");
			_runes = Request("Runes");
			_haloRing = Request("HaloRing");
			_groundRing = Request("GroundRing");
			_thornHalo = Request("ThornHalo");
			_sigilRings = Request("SigilRings");
		}

		public override void Unload()
		{
			_bloom = _orb = _flare = _streak = _line = _ringWave = _godRay = null;
			_smoke = _sprites = _runes = _haloRing = _groundRing = _thornHalo = _sigilRings = null;
		}

		private static Asset<Texture2D> Request(string name) => ModContent.Request<Texture2D>(Folder + name);
	}
}
