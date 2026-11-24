using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using BeautifulLooks.Common.Looks;

namespace BeautifulLooks.Common.UI
{
	/// <summary>Цвета и мелкие помощники оформления меню.</summary>
	public static class Theme
	{
		public static readonly Color Window = new Color(33, 24, 66) * 0.95f;
		public static readonly Color Inner = new Color(20, 14, 44) * 0.9f;
		public static readonly Color Button = new Color(63, 72, 151) * 0.9f;
		public static readonly Color ButtonHover = new Color(95, 110, 210);
		public static readonly Color ButtonActive = new Color(150, 90, 220);
		public static readonly Color Accent = new Color(255, 215, 110);
		public static readonly Color TextDim = new Color(190, 185, 220);

		public static string T(string key) => Language.GetTextValue("Mods.BeautifulLooks.UI." + key);

		public static void Rect(SpriteBatch sb, Rectangle r, Color c) => sb.Draw(TextureAssets.MagicPixel.Value, r, c);

		public static void Border(SpriteBatch sb, Rectangle r, Color c, int t = 1)
		{
			Rect(sb, new Rectangle(r.X, r.Y, r.Width, t), c);
			Rect(sb, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
			Rect(sb, new Rectangle(r.X, r.Y, t, r.Height), c);
			Rect(sb, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
		}

		public static void Text(SpriteBatch sb, string text, Vector2 pos, Color color, float scale = 0.9f, float anchorX = 0f, float anchorY = 0f)
		{
			Utils.DrawBorderString(sb, text, pos, color, scale, anchorX, anchorY);
		}
	}

	/// <summary>Текст, который каждый кадр берёт строку из функции (удобно для локализации и значений).</summary>
	public class DynamicText : UIElement
	{
		private readonly Func<string> _text;
		public float Scale = 0.9f;
		public Color Color = Color.White;
		public float AnchorX;

		public DynamicText(Func<string> text, float scale = 0.9f)
		{
			_text = text;
			Scale = scale;
			Height.Set(24f, 0f);
			Width.Set(0f, 1f);
			IgnoresMouseInteraction = true;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle d = GetDimensions();
			var pos = new Vector2(d.X + d.Width * AnchorX, d.Y + d.Height / 2f + 2f);
			Theme.Text(spriteBatch, _text(), pos, Color, Scale, AnchorX, 0.5f);
		}
	}

	/// <summary>Кнопка с подсветкой при наведении и состоянием «выбрано». Длинный текст сам уменьшается.</summary>
	public class LooksButton : UITextPanel<string>
	{
		private readonly Func<string> _textProvider;
		private readonly float _baseScale;
		private string _lastText;
		private float _lastWidth = -1f;
		public Func<bool> IsSelected;

		public LooksButton(Func<string> text, float width, float height = 30f, float textScale = 0.85f)
			: base("", textScale)
		{
			// Текст берём только в Update: при создании меню персонажа может ещё не быть
			_textProvider = text;
			_baseScale = textScale;
			_lastText = null;
			Width.Set(width, 0f);
			Height.Set(height, 0f);
			SetPadding(0f);
			PaddingTop = 4f;
			BackgroundColor = Theme.Button;
		}

		/// <summary>Ставит текст, уменьшая шрифт, если он не помещается в кнопку.</summary>
		private void FitText(string text)
		{
			float available = GetInnerDimensions().Width - 6f;
			float scale = _baseScale;
			if (available > 10f && !string.IsNullOrEmpty(text))
			{
				float textWidth = FontAssets.MouseText.Value.MeasureString(text).X * _baseScale;
				if (textWidth > available)
					scale = Math.Max(0.45f, _baseScale * available / textWidth);
			}

			SetText(text, scale, false);
			// Кнопка не должна раздуваться под текст: размер задаёт раскладка
			MinWidth.Set(0f, 0f);
			MinHeight.Set(0f, 0f);
		}

		public LooksButton(string text, float width, float height = 30f, float textScale = 0.85f)
			: this(() => text, width, height, textScale)
		{
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			string text = _textProvider();
			float width = GetInnerDimensions().Width;
			if (text != _lastText || Math.Abs(width - _lastWidth) > 0.5f)
			{
				_lastText = text;
				_lastWidth = width;
				FitText(text);
			}

			bool selected = IsSelected?.Invoke() ?? false;
			BackgroundColor = IsMouseHovering ? Theme.ButtonHover : selected ? Theme.ButtonActive : Theme.Button;
			BorderColor = selected ? Theme.Accent : Color.Black;
		}
	}

	/// <summary>Строка выбора: «Подпись   [<]  Значение  [>]».</summary>
	public class SelectorRow : UIElement
	{
		public SelectorRow(Func<string> label, Func<string> value, Action<int> change)
		{
			Width.Set(0f, 1f);
			Height.Set(32f, 0f);

			var lbl = new DynamicText(label) { Color = Theme.TextDim };
			lbl.Width.Set(120f, 0f);
			lbl.Height.Set(32f, 0f);
			Append(lbl);

			var prev = new LooksButton("<", 32f);
			prev.Left.Set(124f, 0f);
			prev.OnLeftClick += (_, _) => change(-1);
			Append(prev);

			var valueBox = new UIPanel { BackgroundColor = Theme.Inner };
			valueBox.Left.Set(160f, 0f);
			valueBox.Width.Set(-200f, 1f);
			valueBox.Height.Set(30f, 0f);
			valueBox.SetPadding(0f);
			var valueText = new DynamicText(value) { AnchorX = 0.5f, Color = Theme.Accent };
			valueText.Height.Set(0f, 1f);
			valueBox.Append(valueText);
			Append(valueBox);

			var next = new LooksButton(">", 32f);
			next.Left.Set(-34f, 1f);
			next.OnLeftClick += (_, _) => change(1);
			Append(next);
		}
	}

	/// <summary>Ползунок 0..1 с цветной полосой.</summary>
	public class LooksSlider : UIElement
	{
		private const float LabelWidth = 110f;
		private const float ValueWidth = 48f;

		private readonly Func<string> _label;
		private readonly Func<float> _get;
		private readonly Action<float> _set;
		private readonly Func<float, Color> _barColor;
		private readonly Func<float, string> _format;
		private bool _dragging;

		public LooksSlider(Func<string> label, Func<float> get, Action<float> set,
			Func<float, Color> barColor = null, Func<float, string> format = null)
		{
			_label = label;
			_get = get;
			_set = set;
			_barColor = barColor ?? (t => Color.Lerp(new Color(60, 60, 90), Theme.Accent, t));
			_format = format ?? (v => (int)Math.Round(v * 100) + "%");
			Width.Set(0f, 1f);
			Height.Set(26f, 0f);
		}

		private Rectangle Bar()
		{
			CalculatedStyle d = GetDimensions();
			return new Rectangle((int)(d.X + LabelWidth), (int)(d.Y + d.Height / 2f - 5f), (int)(d.Width - LabelWidth - ValueWidth), 10);
		}

		public override void LeftMouseDown(UIMouseEvent evt)
		{
			base.LeftMouseDown(evt);
			Rectangle bar = Bar();
			bar.Inflate(4, 6);
			if (bar.Contains(Main.MouseScreen.ToPoint()))
				_dragging = true;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (!_dragging)
				return;

			if (!Main.mouseLeft)
			{
				_dragging = false;
				return;
			}

			Rectangle bar = Bar();
			_set(MathHelper.Clamp((Main.MouseScreen.X - bar.X) / bar.Width, 0f, 1f));
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			CalculatedStyle d = GetDimensions();
			Rectangle bar = Bar();
			float value = MathHelper.Clamp(_get(), 0f, 1f);

			Theme.Text(sb, _label(), new Vector2(d.X, d.Y + d.Height / 2f + 2f), Theme.TextDim, 0.85f, 0f, 0.5f);

			// Полоса из цветных отрезков
			Theme.Rect(sb, new Rectangle(bar.X - 2, bar.Y - 2, bar.Width + 4, bar.Height + 4), Color.Black * 0.8f);
			const int step = 3;
			for (int x = 0; x < bar.Width; x += step)
			{
				int w = Math.Min(step, bar.Width - x);
				Theme.Rect(sb, new Rectangle(bar.X + x, bar.Y, w, bar.Height), _barColor(x / (float)bar.Width));
			}

			// Ползунок
			int knobX = bar.X + (int)(value * bar.Width);
			bool hot = _dragging || IsMouseHovering;
			Theme.Rect(sb, new Rectangle(knobX - 3, bar.Y - 5, 6, bar.Height + 10), Color.Black);
			Theme.Rect(sb, new Rectangle(knobX - 2, bar.Y - 4, 4, bar.Height + 8), hot ? Theme.Accent : Color.White);

			Theme.Text(sb, _format(value), new Vector2(d.X + d.Width, d.Y + d.Height / 2f + 2f), Color.White, 0.8f, 1f, 0.5f);
		}
	}

	/// <summary>Цветной квадратик палитры.</summary>
	public class ColorSwatch : UIElement
	{
		private readonly Color _color;
		private readonly Func<bool> _selected;

		public ColorSwatch(Color color, Func<bool> selected, Action onClick, float size = 22f)
		{
			_color = color;
			_selected = selected;
			Width.Set(size, 0f);
			Height.Set(size, 0f);
			OnLeftClick += (_, _) =>
			{
				onClick();
				SoundEngine.PlaySound(SoundID.MenuTick);
			};
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			Rectangle r = GetDimensions().ToRectangle();
			Theme.Rect(sb, r, Color.Black);
			r.Inflate(-2, -2);
			Theme.Rect(sb, r, _color);
			if (_selected() || IsMouseHovering)
			{
				r.Inflate(2, 2);
				Theme.Border(sb, r, _selected() ? Theme.Accent : Color.White, 2);
			}
		}
	}

	/// <summary>Палитра + ползунки оттенка/насыщенности/яркости + радуга.</summary>
	public class ColorPicker : UIElement
	{
		public static readonly Color[] Palette =
		{
			new Color(255, 255, 255), new Color(255, 80, 80), new Color(255, 150, 60), new Color(255, 215, 90),
			new Color(170, 255, 90), new Color(80, 230, 130), new Color(80, 230, 230), new Color(110, 190, 255),
			new Color(90, 110, 255), new Color(170, 100, 255), new Color(255, 110, 220), new Color(255, 170, 200)
		};

		public ColorPicker(Func<ColorSetting> setting)
		{
			Width.Set(0f, 1f);
			Height.Set(150f, 0f);

			for (int i = 0; i < Palette.Length; i++)
			{
				Color c = Palette[i];
				var swatch = new ColorSwatch(c,
					() => !setting().Rainbow && Close(setting().Get(), c),
					() => setting().SetFromColor(c));
				swatch.Left.Set(i * 26f, 0f);
				Append(swatch);
			}

			var hue = new LooksSlider(() => Theme.T("Hue"), () => setting().Hue, v => { setting().Hue = v; setting().Rainbow = false; },
				t => Main.hslToRgb(t, 1f, 0.5f));
			hue.Top.Set(30f, 0f);
			Append(hue);

			var sat = new LooksSlider(() => Theme.T("Saturation"), () => setting().Saturation, v => setting().Saturation = v,
				t => Main.hslToRgb(setting().Hue, t, setting().Lightness));
			sat.Top.Set(58f, 0f);
			Append(sat);

			var light = new LooksSlider(() => Theme.T("Lightness"), () => setting().Lightness, v => setting().Lightness = v,
				t => Main.hslToRgb(setting().Hue, setting().Saturation, t));
			light.Top.Set(86f, 0f);
			Append(light);

			var rainbow = new LooksButton(() => Theme.T("Rainbow") + ": " + Theme.T(setting().Rainbow ? "On" : "Off"), 170f);
			rainbow.Top.Set(118f, 0f);
			rainbow.IsSelected = () => setting().Rainbow;
			rainbow.OnLeftClick += (_, _) => setting().Rainbow = !setting().Rainbow;
			Append(rainbow);

			var preview = new ColorPreview(() => setting().Get());
			preview.Left.Set(180f, 0f);
			preview.Top.Set(118f, 0f);
			Append(preview);
		}

		private static bool Close(Color a, Color b) =>
			Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < 12;
	}

	/// <summary>Полоска с текущим цветом.</summary>
	public class ColorPreview : UIElement
	{
		private readonly Func<Color> _color;

		public ColorPreview(Func<Color> color)
		{
			_color = color;
			Width.Set(-180f, 1f);
			Height.Set(30f, 0f);
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			Rectangle r = GetDimensions().ToRectangle();
			Theme.Rect(sb, r, Color.Black);
			r.Inflate(-2, -2);
			Theme.Rect(sb, r, _color());
		}
	}

	/// <summary>Сетка кнопок-вариантов (стили ауры, следа, нимба...). Выбранная подсвечивается.</summary>
	public class OptionGrid : UIElement
	{
		public readonly List<LooksButton> Buttons = new List<LooksButton>();

		public OptionGrid(int count, int columns, Func<int, string> label, Func<int, bool> selected, Action<int> click,
			float buttonHeight = 26f, float gap = 4f, float textScale = 0.75f)
		{
			int rows = (count + columns - 1) / columns;
			Width.Set(0f, 1f);
			Height.Set(rows * (buttonHeight + gap), 0f);

			for (int i = 0; i < count; i++)
			{
				int index = i;
				int col = i % columns;
				int row = i / columns;
				var button = new LooksButton(() => label(index), 0f, buttonHeight, textScale);
				button.Width.Set(-gap, 1f / columns);
				button.Left.Set(gap / 2f, (float)col / columns);
				button.Top.Set(row * (buttonHeight + gap), 0f);
				button.IsSelected = () => selected(index);
				button.OnLeftClick += (_, _) => click(index);
				Append(button);
				Buttons.Add(button);
			}
		}
	}

	/// <summary>Кнопка с двумя цветными квадратиками слева (цветовые темы, готовые образы).</summary>
	public class SwatchButton : LooksButton
	{
		private readonly Func<Color> _first;
		private readonly Func<Color> _second;

		public SwatchButton(Func<string> text, Func<Color> first, Func<Color> second, float height = 26f, float textScale = 0.75f)
			: base(text, 0f, height, textScale)
		{
			_first = first;
			_second = second;
			PaddingLeft = 34f;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			base.DrawSelf(sb);
			CalculatedStyle d = GetDimensions();
			int size = (int)Math.Min(14f, d.Height - 10f);
			int y = (int)(d.Y + (d.Height - size) / 2f);
			DrawBox(sb, new Rectangle((int)d.X + 5, y, size, size), _first());
			DrawBox(sb, new Rectangle((int)d.X + 7 + size, y, size, size), _second());
		}

		private static void DrawBox(SpriteBatch sb, Rectangle r, Color c)
		{
			Theme.Rect(sb, r, Color.Black);
			r.Inflate(-1, -1);
			Theme.Rect(sb, r, c);
		}
	}

	/// <summary>Квадратик с живым цветом: по клику открывает вкладку «Цвета».</summary>
	public class LiveSwatch : UIElement
	{
		private readonly Func<Color> _color;
		private readonly Func<string> _label;

		public LiveSwatch(Func<Color> color, Func<string> label, Action onClick, float width = 70f, float height = 24f)
		{
			_color = color;
			_label = label;
			Width.Set(width, 0f);
			Height.Set(height, 0f);
			OnLeftClick += (_, _) =>
			{
				onClick();
				SoundEngine.PlaySound(SoundID.MenuTick);
			};
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			Rectangle r = GetDimensions().ToRectangle();
			Theme.Rect(sb, r, IsMouseHovering ? Theme.Accent : Color.Black);
			r.Inflate(-2, -2);
			Theme.Rect(sb, r, _color());
			Theme.Text(sb, _label(), r.Center.ToVector2() + new Vector2(0f, 2f), Color.White, 0.7f, 0.5f, 0.5f);
		}
	}

	/// <summary>Сетка предметов мода с подсказками при наведении.</summary>
	public class ItemGrid : UIElement
	{
		private const int SlotSize = 52;
		private const int Gap = 6;
		private List<Item> _items;

		public ItemGrid()
		{
			Width.Set(0f, 1f);
			Height.Set(0f, 1f);
		}

		private static List<Item> BuildItems()
		{
			return ModContent.GetInstance<BeautifulLooksMod>().GetContent<ModItem>()
				.Select(m =>
				{
					var item = new Item();
					item.SetDefaults(m.Type);
					return item;
				})
				.ToList();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (IsMouseHovering)
				Main.LocalPlayer.mouseInterface = true;
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			_items ??= BuildItems();

			CalculatedStyle d = GetDimensions();
			int cols = Math.Max(1, (int)((d.Width + Gap) / (SlotSize + Gap)));
			Point mouse = Main.MouseScreen.ToPoint();

			for (int i = 0; i < _items.Count; i++)
			{
				var r = new Rectangle((int)d.X + i % cols * (SlotSize + Gap), (int)d.Y + i / cols * (SlotSize + Gap), SlotSize, SlotSize);
				bool hover = r.Contains(mouse);

				Theme.Rect(sb, r, Color.Black * 0.6f);
				var inner = r;
				inner.Inflate(-2, -2);
				Theme.Rect(sb, inner, hover ? Theme.ButtonHover : Theme.Button);

				ItemSlot.DrawItemIcon(_items[i], ItemSlot.Context.InventoryItem, sb, r.Center.ToVector2(), 1f, SlotSize - 14, Color.White);

				if (hover)
				{
					Theme.Border(sb, r, Theme.Accent, 2);
					Main.HoverItem = _items[i].Clone();
					Main.hoverItemName = _items[i].Name;
				}
			}
		}
	}
}
