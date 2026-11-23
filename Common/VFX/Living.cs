using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Configs;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>
	/// «Живые» частицы: лепестки и снежинки ложатся на землю, угольки отскакивают от блоков.
	/// Здесь — проверка, есть ли в точке мира твёрдый блок (платформа считается только сверху).
	/// </summary>
	public static class Living
	{
		public static bool Enabled => ModContent.GetInstance<LooksClientConfig>()?.LivingParticles ?? true;

		/// <summary>Есть ли твёрдый блок в точке мира.</summary>
		public static bool Solid(Vector2 world)
		{
			int x = (int)(world.X / 16f);
			int y = (int)(world.Y / 16f);
			if (world.X < 0f || world.Y < 0f || !WorldGen.InWorld(x, y, 2))
				return false;
			Tile tile = Main.tile[x, y];
			if (!tile.HasUnactuatedTile)
				return false;
			ushort type = tile.TileType;
			if (Main.tileSolidTop[type])
				return world.Y - y * 16f < 5f; // платформа: только верхние пиксели
			return Main.tileSolid[type];
		}
	}
}
