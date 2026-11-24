using Microsoft.Xna.Framework;

namespace BeautifulLooks.Common.Looks
{
	/// <summary>Готовая пара цветов (основной + второй). Название берётся из локализации UI.Theme.Key.</summary>
	public class ColorTheme
	{
		public readonly string Key;
		public readonly Color Primary;
		public readonly Color Secondary;
		public readonly bool Rainbow;

		public ColorTheme(string key, Color primary, Color secondary, bool rainbow = false)
		{
			Key = key;
			Primary = primary;
			Secondary = secondary;
			Rainbow = rainbow;
		}

		public void ApplyTo(ColorSetting first, ColorSetting second)
		{
			first.Set(Primary, Rainbow);
			second.Set(Secondary);
		}

		/// <summary>Красит весь образ: ауру, след, движение, эффекты, крылья и спутника.</summary>
		public void ApplyToAll(LookSettings look)
		{
			foreach ((ColorSetting a, ColorSetting b) in look.ColorPairs)
				ApplyTo(a, b);
		}
	}

	public static class ColorThemes
	{
		public static readonly ColorTheme[] All =
		{
			new ColorTheme("Seraph", new Color(255, 214, 110), new Color(255, 150, 90)),
			new ColorTheme("Inferno", new Color(255, 160, 40), new Color(235, 40, 40)),
			new ColorTheme("Frost", new Color(120, 210, 255), new Color(130, 130, 255)),
			new ColorTheme("Sakura", new Color(255, 140, 200), new Color(190, 140, 255)),
			new ColorTheme("Void", new Color(150, 70, 255), new Color(255, 80, 200)),
			new ColorTheme("Empress", new Color(255, 120, 220), new Color(130, 230, 255), rainbow: true),
			new ColorTheme("Emerald", new Color(70, 255, 150), new Color(40, 200, 255)),
			new ColorTheme("Ocean", new Color(60, 140, 255), new Color(80, 255, 225)),
			new ColorTheme("BloodMoon", new Color(230, 30, 60), new Color(255, 140, 60)),
			new ColorTheme("Dusk", new Color(255, 140, 90), new Color(150, 100, 255)),
			new ColorTheme("Storm", new Color(130, 170, 255), new Color(190, 120, 255)),
			new ColorTheme("Sun", new Color(255, 195, 50), new Color(255, 90, 30))
		};
	}
}
