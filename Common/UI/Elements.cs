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
using Terraria_Beatiful_Looks.Common.Looks;

namespace Terraria_Beatiful_Looks.Common.UI
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

		public static string T(string key) => Language.GetTextValue("Mods.Terraria_Beatiful_Looks.UI." + key);

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

	/// <summary>Кнопка с подсветкой при наведении и состоянием «выбрано».</summary>
	public class LooksButton : UITextPanel<string>
	{
		private readonly Func<string> _textProvider;
		private string _lastText;
		public Func<bool> IsSelected;

		public LooksButton(Func<string> text, float width, float height = 30f, float textScale = 0.85f)
			: base(text(), textScale)
		{
			_textProvider = text;
			_lastText = text();
			Width.Set(width, 0f);
			Height.Set(height, 0f);
			SetPadding(0f);
			PaddingTop = 4f;
			BackgroundColor = Theme.Button;
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
			if (text != _lastText)
			{
				_lastText = text;
				SetText(text);
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
