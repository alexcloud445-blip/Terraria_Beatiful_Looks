using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
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
			_smoke, _runes, _haloRing, _groundRing, _thornHalo, _sigilRings,
			_shapes, _shapesGlow, _feather, _featherGlow, _covert, _covertGlow, _butterflyWing, _butterflyPattern, _butterflyGlow,
			_batWing, _batBones, _flame, _blade, _glint;

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
		/// <summary>Атлас форм 384x64 (enum Shape), заливка с серой светотенью.</summary>
		public static Texture2D Shapes => _shapes.Value;
		/// <summary>Свечение форм (тот же атлас, размытый).</summary>
		public static Texture2D ShapesGlow => _shapesGlow.Value;
		/// <summary>Маховое перо 64x16, основание слева.</summary>
		public static Texture2D Feather => _feather.Value;
		public static Texture2D FeatherGlow => _featherGlow.Value;
		/// <summary>Кроющее перо 32x16 (короткое, округлое), основание слева.</summary>
		public static Texture2D Covert => _covert.Value;
		public static Texture2D CovertGlow => _covertGlow.Value;
		/// <summary>Крыло бабочки 64x64 (верхнее + нижнее), крепление в (4, 32).</summary>
		public static Texture2D ButterflyWing => _butterflyWing.Value;
		public static Texture2D ButterflyPattern => _butterflyPattern.Value;
		public static Texture2D ButterflyGlow => _butterflyGlow.Value;
		/// <summary>Крыло летучей мыши 96x64, крепление в (4, 22); кости отдельно.</summary>
		public static Texture2D BatWing => _batWing.Value;
		public static Texture2D BatBones => _batBones.Value;
		/// <summary>4 кадра язычка пламени 32x48, основание внизу по центру.</summary>
		public static Texture2D Flame => _flame.Value;
		/// <summary>Клинок света 128x16, основание слева.</summary>
		public static Texture2D Blade => _blade.Value;
		/// <summary>Острый блик 64x64.</summary>
		public static Texture2D Glint => _glint.Value;
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

		private static readonly List<Asset<Texture2D>> All = new List<Asset<Texture2D>>();
		private static bool _prepared;

		/// <summary>
		/// Один раз (в главном потоке, перед первой отрисовкой) проверяет текстуры эффектов:
		/// цвет должен быть умножен на прозрачность (premultiplied). Если это не так
		/// (например, в папке мода остались старые PNG), исправляет прямо в памяти и пишет об этом в лог.
		/// Без этого мягкое свечение рисуется ровными кругами с резким краем.
		/// </summary>
		public static void Prepare()
		{
			if (_prepared || Main.dedServ)
				return;
			_prepared = true;

			int fixedCount = 0;
			var names = new List<string>();
			try
			{
				foreach (Asset<Texture2D> asset in All)
				{
					if (Premultiply(asset.Value))
					{
						fixedCount++;
						names.Add(asset.Name);
					}
				}
				ModContent.GetInstance<BeautifulLooksMod>().Logger.Info(fixedCount == 0
					? "Beautiful Looks: VFX textures are premultiplied, OK"
					: $"Beautiful Looks: fixed {fixedCount} non-premultiplied VFX textures at runtime: {string.Join(", ", names)}");
			}
			catch (Exception e)
			{
				VfxRenderer.LogOnce("prepare textures", e);
			}
		}

		/// <summary>Умножает цвет на прозрачность, если текстура ещё не в таком виде. true — была исправлена.</summary>
		private static bool Premultiply(Texture2D tex)
		{
			if (tex == null || tex.Width <= 1)
				return false;
			var data = new Color[tex.Width * tex.Height];
			tex.GetData(data);

			bool needed = false;
			for (int i = 0; i < data.Length; i++)
			{
				Color c = data[i];
				if (c.R > c.A + 2 || c.G > c.A + 2 || c.B > c.A + 2)
				{
					needed = true;
					break;
				}
			}
			if (!needed)
				return false;

			for (int i = 0; i < data.Length; i++)
			{
				Color c = data[i];
				int a = c.A;
				data[i] = new Color(c.R * a / 255, c.G * a / 255, c.B * a / 255, a);
			}
			tex.SetData(data);
			return true;
		}

		public static Rectangle FlameFrame(int index) => new Rectangle((index & 3) * 32, 0, 32, 48);

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
			_shapes = Request("Shapes");
			_shapesGlow = Request("ShapesGlow");
			_feather = Request("Feather");
			_featherGlow = Request("FeatherGlow");
			_covert = Request("Covert");
			_covertGlow = Request("CovertGlow");
			_butterflyWing = Request("ButterflyWing");
			_butterflyPattern = Request("ButterflyPattern");
			_butterflyGlow = Request("ButterflyGlow");
			_batWing = Request("BatWing");
			_batBones = Request("BatBones");
			_flame = Request("Flame");
			_blade = Request("Blade");
			_glint = Request("Glint");
			_runes = Request("Runes");
			_haloRing = Request("HaloRing");
			_groundRing = Request("GroundRing");
			_thornHalo = Request("ThornHalo");
			_sigilRings = Request("SigilRings");
		}

		public override void Unload()
		{
			All.Clear();
			_prepared = false;
			_bloom = _orb = _flare = _streak = _line = _ringWave = _godRay = null;
			_smoke = _runes = _haloRing = _groundRing = _thornHalo = _sigilRings = null;
			_shapes = _shapesGlow = _feather = _featherGlow = _covert = _covertGlow = _butterflyWing = _butterflyPattern = _butterflyGlow = null;
			_batWing = _batBones = _flame = _blade = _glint = null;
		}

		private static Asset<Texture2D> Request(string name)
		{
			Asset<Texture2D> asset = ModContent.Request<Texture2D>(Folder + name);
			All.Add(asset);
			return asset;
		}
	}
}
