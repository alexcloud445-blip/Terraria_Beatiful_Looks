using Microsoft.Xna.Framework;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>
	/// Формы из атласа Shapes.png / ShapesGlow.png (ячейки 32x32, 12 в ряд).
	/// Порядок совпадает со списком SHAPES в tools/make_vfx_shapes.py.
	/// </summary>
	public enum Shape
	{
		Heart,
		Petal,
		PetalRound,
		Leaf,
		Snowflake,
		SnowCrystal,
		Star5,
		Star4,
		Star8,
		Diamond,
		ShardA,
		ShardB,
		Crescent,
		Feather,
		Teardrop,
		Bubble,
		Butterfly0,
		Butterfly1,
		Butterfly2,
		Butterfly3,
		Swirl,
		Planet,
		Lotus,
		Wisp
	}

	public static class Shapes
	{
		public const int Cell = 32;

		public static Rectangle Frame(Shape shape) => Frame((int)shape);

		public static Rectangle Frame(int index) => new Rectangle(index % 12 * Cell, index / 12 * Cell, Cell, Cell);

		public static readonly Vector2 Origin = new Vector2(Cell / 2f, Cell / 2f);

		/// <summary>Кадр бабочки, которая машет крыльями (туда-обратно по 4 кадрам).</summary>
		public static Shape ButterflyFrame(int time) => Shape.Butterfly0 + FlapCycle[time % FlapCycle.Length];

		private static readonly int[] FlapCycle = { 0, 1, 2, 3, 2, 1 };
	}
}
