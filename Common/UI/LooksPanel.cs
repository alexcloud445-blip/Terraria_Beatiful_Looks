using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.Systems;

namespace Terraria_Beatiful_Looks.Common.UI
{
	/// <summary>
	/// Главное окно: заголовок (за него можно тащить), слева превью и пресеты, справа вкладки.
	/// Новая вкладка: добавить в TabNames + метод Build...Tab + строку в BuildTabs.
	/// </summary>
	public class LooksPanel : UIState
	{
		private const float WindowWidth = 760f;
		private const float WindowHeight = 500f;
		private const float LeftColumn = 180f;

		private static readonly string[] TabNames = { "TabAura", "TabTrail", "TabEffects", "TabColors", "TabLooks", "TabWardrobe" };
		private const int ColorsTab = 3;

		private UIPanel _root;
		private UIPanel _previewBox;
		private UIElement _content;
		private UIElement[] _tabs;
		private int _currentTab;

		private bool _dragging;
		private Vector2 _dragOffset;

		/// <summary>Какие цвета правит вкладка «Цвета»: 0 аура, 1 след, 2 эффекты; слот 0 — основной, 1 — второй.</summary>
		private int _colorTarget;
		private int _colorSlot;
		private int _hoverLook = -1;
		private int _lastLook = -1;

		private static LooksPlayer Me => Main.LocalPlayer.GetModPlayer<LooksPlayer>();
		private static LookSettings Look => Me.Look;

		public override void OnInitialize()
		{
			_root = new UIPanel { BackgroundColor = Theme.Window, BorderColor = Theme.Accent * 0.6f };
			_root.Width.Set(WindowWidth, 0f);
			_root.Height.Set(WindowHeight, 0f);
			// По умолчанию справа, чтобы персонаж в центре экрана был виден
			_root.HAlign = 1f;
			_root.Left.Set(-30f, 0f);
			_root.VAlign = 0.5f;
			Append(_root);

			BuildHeader();
			BuildLeftColumn();
			BuildTabs();
		}

		// ======================= заголовок =======================
		private void BuildHeader()
		{
			var header = new UIElement();
			header.Width.Set(-170f, 1f);
			header.Height.Set(32f, 0f);
			header.OnLeftMouseDown += (_, _) => StartDrag();
			_root.Append(header);

			var title = new DynamicText(() => "~ " + Theme.T("Title") + " ~", 1.1f) { Color = Theme.Accent };
			title.Left.Set(4f, 0f);
			title.Width.Set(260f, 0f);
			title.Height.Set(32f, 0f);
			header.Append(title);

			var hint = new DynamicText(() => Theme.T("DragHint"), 0.7f) { Color = Theme.TextDim * 0.8f };
			hint.Left.Set(270f, 0f);
			hint.Height.Set(32f, 0f);
			header.Append(hint);

			var master = new LooksButton(() => Theme.T("Effects") + ": " + Theme.T(Look.Enabled ? "On" : "Off"), 126f, 30f, 0.75f);
			master.Left.Set(-166f, 1f);
			master.IsSelected = () => Look.Enabled;
			master.OnLeftClick += (_, _) => Look.Enabled = !Look.Enabled;
			_root.Append(master);

			var close = new LooksButton("X", 36f, 30f);
			close.Left.Set(-36f, 1f);
			close.OnLeftClick += (_, _) => ModContent.GetInstance<LooksUISystem>().Hide();
			_root.Append(close);
		}

		private void StartDrag()
		{
			CalculatedStyle d = _root.GetDimensions();
			_root.HAlign = 0f;
			_root.VAlign = 0f;
			_root.Left.Set(d.X, 0f);
			_root.Top.Set(d.Y, 0f);
			_dragOffset = Main.MouseScreen - new Vector2(d.X, d.Y);
			_dragging = true;
			Recalculate();
		}

		// ======================= левая колонка =======================
		private void BuildLeftColumn()
		{
			// OverflowHidden: лучи и круги не вылезают за рамку превью
			_previewBox = new UIPanel { BackgroundColor = Theme.Inner, OverflowHidden = true };
			_previewBox.Top.Set(40f, 0f);
			_previewBox.Width.Set(LeftColumn, 0f);
			_previewBox.Height.Set(196f, 0f);
			_root.Append(_previewBox);

			var presetsTitle = new DynamicText(() => Theme.T("Presets")) { Color = Theme.Accent };
			presetsTitle.Top.Set(240f, 0f);
			presetsTitle.Width.Set(LeftColumn, 0f);
			_root.Append(presetsTitle);

			for (int i = 0; i < LooksPlayer.PresetCount; i++)
			{
				int slot = i;
				float top = 266f + i * 31f;

				var label = new DynamicText(() => (slot + 1) + (Me.Presets[slot] == null ? " -" : " *")) { Color = Theme.TextDim };
				label.Top.Set(top, 0f);
				label.Width.Set(36f, 0f);
				label.Height.Set(28f, 0f);
				_root.Append(label);

				var save = new LooksButton(() => Theme.T("Save"), 68f, 28f, 0.75f);
				save.Left.Set(38f, 0f);
				save.Top.Set(top, 0f);
				save.OnLeftClick += (_, _) =>
				{
					Me.SavePreset(slot);
					Main.NewText(Theme.T("PresetSaved").Replace("{0}", (slot + 1).ToString()), Theme.Accent);
				};
				_root.Append(save);

				var load = new LooksButton(() => Theme.T("Load"), 70f, 28f, 0.75f);
				load.Left.Set(110f, 0f);
				load.Top.Set(top, 0f);
				load.OnLeftClick += (_, _) =>
				{
					if (!Me.LoadPreset(slot))
						Main.NewText(Theme.T("PresetEmpty"), Color.Gray);
				};
				_root.Append(load);
			}

			var random = new LooksButton(() => Theme.T("Random"), 88f, 28f, 0.75f);
			random.Top.Set(424f, 0f);
			random.OnLeftClick += (_, _) => Look.Randomize();
			_root.Append(random);

			var reset = new LooksButton(() => Theme.T("Reset"), 88f, 28f, 0.75f);
			reset.Left.Set(92f, 0f);
			reset.Top.Set(424f, 0f);
			reset.OnLeftClick += (_, _) => Look.Reset();
			_root.Append(reset);
		}

		/// <summary>Пересоздаёт превью персонажа (вызывается при открытии меню).</summary>
		private void RefreshPreview()
		{
			_previewBox.RemoveAllChildren();
			var character = new UICharacter(Main.LocalPlayer, false, false, 1.6f);
			character.HAlign = 0.5f;
			character.VAlign = 0.55f;
			_previewBox.Append(character);
			_previewBox.Recalculate();
		}

		// ======================= вкладки =======================
		private void BuildTabs()
		{
			float tabWidth = (WindowWidth - 24f - LeftColumn - 12f - 5f * (TabNames.Length - 1)) / TabNames.Length;
			for (int i = 0; i < TabNames.Length; i++)
			{
				int index = i;
				var tab = new LooksButton(() => Theme.T(TabNames[index]), tabWidth, 32f, 0.8f);
				tab.Left.Set(LeftColumn + 12f + i * (tabWidth + 5f), 0f);
				tab.Top.Set(40f, 0f);
				tab.IsSelected = () => _currentTab == index;
				tab.OnLeftClick += (_, _) => SelectTab(index);
				_root.Append(tab);
			}

			var contentPanel = new UIPanel { BackgroundColor = Theme.Inner };
			contentPanel.Left.Set(LeftColumn + 12f, 0f);
			contentPanel.Top.Set(80f, 0f);
			contentPanel.Width.Set(-(LeftColumn + 12f), 1f);
			contentPanel.Height.Set(-80f, 1f);
			_root.Append(contentPanel);

			_content = new UIElement();
			_content.Width.Set(0f, 1f);
			_content.Height.Set(0f, 1f);
			contentPanel.Append(_content);

			_tabs = new[] { BuildAuraTab(), BuildTrailTab(), BuildEffectsTab(), BuildColorsTab(), BuildLooksTab(), BuildWardrobeTab() };
			SelectTab(0);
		}

		private void SelectTab(int index)
		{
			_currentTab = index;
			_content.RemoveAllChildren();
			_content.Append(_tabs[index]);
			_content.Recalculate();
		}

		/// <summary>Открыть вкладку «Цвета» сразу на нужной паре цветов.</summary>
		private void OpenColors(int target, int slot)
		{
			_colorTarget = target;
			_colorSlot = slot;
			SelectTab(ColorsTab);
		}

		private static UIElement NewPage()
		{
			var page = new UIElement();
			page.Width.Set(0f, 1f);
			page.Height.Set(0f, 1f);
			return page;
		}

		private static UIElement Place(UIElement parent, UIElement child, float top)
		{
			child.Top.Set(top, 0f);
			parent.Append(child);
			return child;
		}

		private static DynamicText Header(string key) => new DynamicText(() => Theme.T(key)) { Color = Theme.Accent };

		private static DynamicText Hint(string key) => new DynamicText(() => Theme.T(key), 0.72f) { Color = Theme.TextDim };

		/// <summary>Слайдер множителя 0.2x..2x.</summary>
		private static LooksSlider MulSlider(string key, Func<float> get, Action<float> set)
		{
			const float range = LookSettings.MaxMul - LookSettings.MinMul;
			return new LooksSlider(() => Theme.T(key),
				() => (get() - LookSettings.MinMul) / range,
				v => set(LookSettings.MinMul + v * range),
				format: v => (LookSettings.MinMul + v * range).ToString("0.0") + "x");
		}

		/// <summary>Строка «Цвета: [1] [2]» — по клику открывает вкладку «Цвета».</summary>
		private UIElement ColorChips(int target, Func<ColorSetting> first, Func<ColorSetting> second)
		{
			var row = new UIElement();
			row.Width.Set(0f, 1f);
			row.Height.Set(26f, 0f);

			var label = new DynamicText(() => Theme.T("Colors")) { Color = Theme.TextDim };
			label.Width.Set(110f, 0f);
			label.Height.Set(26f, 0f);
			row.Append(label);

			var a = new LiveSwatch(() => first().Get(), () => "1", () => OpenColors(target, 0));
			a.Left.Set(112f, 0f);
			row.Append(a);

			var b = new LiveSwatch(() => second().Get(), () => "2", () => OpenColors(target, 1));
			b.Left.Set(188f, 0f);
			row.Append(b);

			var hint = new DynamicText(() => Theme.T("ColorsHint"), 0.7f) { Color = Theme.TextDim * 0.8f };
			hint.Left.Set(268f, 0f);
			hint.Width.Set(-268f, 1f);
			hint.Height.Set(26f, 0f);
			row.Append(hint);
			return row;
		}

		// ---------- Аура ----------
		private UIElement BuildAuraTab()
		{
			UIElement page = NewPage();
			Place(page, Header("AuraStyle"), 0f);
			Place(page, new OptionGrid(LookSettings.AuraStyleCount, 4,
				i => Theme.T("Aura." + (AuraStyle)i),
				i => (int)Look.Aura == i,
				i => Look.Aura = (AuraStyle)i), 24f);

			Place(page, MulSlider("Density", () => Look.AuraDensity, v => Look.AuraDensity = v), 182f);
			Place(page, MulSlider("Size", () => Look.AuraSize, v => Look.AuraSize = v), 210f);
			Place(page, MulSlider("Speed", () => Look.AuraSpeed, v => Look.AuraSpeed = v), 238f);
			Place(page, ColorChips(0, () => Look.AuraColor, () => Look.AuraColor2), 274f);
			Place(page, Hint("AuraHint"), 306f);
			return page;
		}

		// ---------- След ----------
		private UIElement BuildTrailTab()
		{
			UIElement page = NewPage();
			Place(page, Header("Ribbon"), 0f);
			Place(page, new OptionGrid(LookSettings.RibbonStyleCount, 6,
				i => Theme.T("RibbonStyle." + (RibbonStyle)i),
				i => (int)Look.Ribbon == i,
				i => Look.Ribbon = (RibbonStyle)i), 24f);

			Place(page, MulSlider("Width", () => Look.RibbonWidth, v => Look.RibbonWidth = v), 56f);
			Place(page, MulSlider("Length", () => Look.RibbonLength, v => Look.RibbonLength = v), 82f);

			Place(page, Header("TrailParticles"), 112f);
			Place(page, new OptionGrid(LookSettings.TrailStyleCount, 6,
				i => Theme.T("Trail." + (TrailStyle)i),
				i => (int)Look.Trail == i,
				i => Look.Trail = (TrailStyle)i), 136f);

			Place(page, Header("Echo"), 198f);
			Place(page, new OptionGrid(LookSettings.EchoStyleCount, 4,
				i => Theme.T("EchoStyle." + (EchoStyle)i),
				i => (int)Look.Echo == i,
				i => Look.Echo = (EchoStyle)i), 222f);

			Place(page, ColorChips(1, () => Look.TrailColor, () => Look.TrailColor2), 258f);
			Place(page, Hint("TrailHint"), 290f);
			return page;
		}

		// ---------- Эффекты ----------
		private UIElement BuildEffectsTab()
		{
			UIElement page = NewPage();
			Place(page, Header("Halo"), 0f);
			Place(page, new OptionGrid(LookSettings.HaloStyleCount, 6,
				i => Theme.T("HaloStyle." + (HaloStyle)i),
				i => (int)Look.Halo == i,
				i => Look.Halo = (HaloStyle)i), 24f);

			Place(page, Header("OnCharacter"), 60f);
			(string key, Func<bool> get, Action toggle)[] toggles =
			{
				("BackGlow", () => Look.BackGlow, () => Look.BackGlow = !Look.BackGlow),
				("GodRays", () => Look.GodRays, () => Look.GodRays = !Look.GodRays),
				("GroundRing", () => Look.GroundRing, () => Look.GroundRing = !Look.GroundRing),
				("Sigil", () => Look.Sigil, () => Look.Sigil = !Look.Sigil),
				("PulseRing", () => Look.PulseRing, () => Look.PulseRing = !Look.PulseRing)
			};
			Place(page, new OptionGrid(toggles.Length, 5,
				i => (toggles[i].get() ? "> " : "") + Theme.T(toggles[i].key),
				i => toggles[i].get(),
				i => toggles[i].toggle()), 84f);

			Place(page, new SelectorRow(() => Theme.T("Dye"),
				() => EffectDyes.Name(Look.EffectDye) ?? Theme.T("NoDye"),
				dir => Look.EffectDye = LookSettings.Wrap(Look.EffectDye + dir, EffectDyes.Count)), 124f);

			Place(page, ColorChips(2, () => Look.EffectColor, () => Look.EffectColor2), 166f);
			Place(page, Hint("EffectsHint"), 198f);
			Place(page, Hint("DyeHint"), 222f);
			return page;
		}

		// ---------- Цвета ----------
		private ColorSetting CurrentColor()
		{
			LookSettings l = Look;
			return _colorTarget switch
			{
				1 => _colorSlot == 0 ? l.TrailColor : l.TrailColor2,
				2 => _colorSlot == 0 ? l.EffectColor : l.EffectColor2,
				_ => _colorSlot == 0 ? l.AuraColor : l.AuraColor2
			};
		}

		private (ColorSetting, ColorSetting) CurrentPair()
		{
			LookSettings l = Look;
			return _colorTarget switch
			{
				1 => (l.TrailColor, l.TrailColor2),
				2 => (l.EffectColor, l.EffectColor2),
				_ => (l.AuraColor, l.AuraColor2)
			};
		}

		private UIElement BuildColorsTab()
		{
			UIElement page = NewPage();

			string[] targets = { "TabAura", "TabTrail", "TabEffects" };
			var targetGrid = new OptionGrid(3, 3, i => Theme.T(targets[i]), i => _colorTarget == i, i => _colorTarget = i);
			targetGrid.Width.Set(-4f, 0.6f);
			Place(page, targetGrid, 0f);

			var slotGrid = new OptionGrid(2, 2, i => Theme.T(i == 0 ? "Color1" : "Color2"), i => _colorSlot == i, i => _colorSlot = i);
			slotGrid.Width.Set(-4f, 0.4f);
			slotGrid.Left.Set(4f, 0.6f);
			Place(page, slotGrid, 0f);

			Place(page, new ColorPicker(CurrentColor), 34f);

			Place(page, Header("Themes"), 188f);
			ColorTheme[] themes = ColorThemes.All;
			var themeGrid = new UIElement();
			themeGrid.Width.Set(0f, 1f);
			themeGrid.Height.Set(3 * 30f, 0f);
			for (int i = 0; i < themes.Length; i++)
			{
				ColorTheme theme = themes[i];
				var button = new SwatchButton(() => Theme.T("Theme." + theme.Key), () => theme.Primary, () => theme.Secondary);
				button.Width.Set(-4f, 0.25f);
				button.Left.Set(2f, (i % 4) * 0.25f);
				button.Top.Set(i / 4 * 30f, 0f);
				button.OnLeftClick += (_, _) =>
				{
					(ColorSetting a, ColorSetting b) = CurrentPair();
					theme.ApplyTo(a, b);
				};
				themeGrid.Append(button);
			}
			Place(page, themeGrid, 212f);

			var copyAll = new LooksButton(() => Theme.T("CopyToAll"), 0f, 28f, 0.75f);
			copyAll.Width.Set(-4f, 0.5f);
			copyAll.OnLeftClick += (_, _) =>
			{
				(ColorSetting a, ColorSetting b) = CurrentPair();
				LookSettings l = Look;
				foreach ((ColorSetting x, ColorSetting y) in new[] { (l.AuraColor, l.AuraColor2), (l.TrailColor, l.TrailColor2), (l.EffectColor, l.EffectColor2) })
				{
					if (x != a)
					{
						x.CopyFrom(a);
						y.CopyFrom(b);
					}
				}
				Main.NewText(Theme.T("CopiedToAll"), Theme.Accent);
			};
			Place(page, copyAll, 306f);

			var swap = new LooksButton(() => Theme.T("SwapColors"), 0f, 28f, 0.75f);
			swap.Width.Set(-4f, 0.5f);
			swap.Left.Set(4f, 0.5f);
			swap.OnLeftClick += (_, _) =>
			{
				(ColorSetting a, ColorSetting b) = CurrentPair();
				var tmp = new ColorSetting();
				tmp.CopyFrom(a);
				a.CopyFrom(b);
				b.CopyFrom(tmp);
			};
			Place(page, swap, 306f);
			return page;
		}

		// ---------- Образы ----------
		private UIElement BuildLooksTab()
		{
			UIElement page = NewPage();
			Place(page, Header("ReadyLooks"), 0f);

			ReadyLooks.Entry[] looks = ReadyLooks.All;
			var grid = new UIElement();
			grid.Width.Set(0f, 1f);
			int rows = (looks.Length + 1) / 2;
			grid.Height.Set(rows * 34f, 0f);
			for (int i = 0; i < looks.Length; i++)
			{
				int index = i;
				ReadyLooks.Entry entry = looks[i];
				var button = new SwatchButton(() => Theme.T("Look." + entry.Key), () => entry.Icon, () => Color.White, 30f, 0.85f);
				button.Width.Set(-6f, 0.5f);
				button.Left.Set(3f, (i % 2) * 0.5f);
				button.Top.Set(i / 2 * 34f, 0f);
				button.IsSelected = () => _lastLook == index;
				button.OnMouseOver += (_, _) => _hoverLook = index;
				button.OnMouseOut += (_, _) =>
				{
					if (_hoverLook == index)
						_hoverLook = -1;
				};
				button.OnLeftClick += (_, _) =>
				{
					entry.Apply(Look);
					_lastLook = index;
				};
				grid.Append(button);
			}
			Place(page, grid, 26f);

			float descTop = 26f + rows * 34f + 6f;
			var desc = new DynamicText(() =>
			{
				int shown = _hoverLook >= 0 ? _hoverLook : _lastLook;
				return shown >= 0 ? Theme.T("Look." + looks[shown].Key + "Desc") : Theme.T("LooksHint");
			}, 0.8f) { Color = Color.White };
			Place(page, desc, descTop);

			Place(page, Hint("LooksTip"), descTop + 28f);
			return page;
		}

		// ---------- Гардероб ----------
		private UIElement BuildWardrobeTab()
		{
			UIElement page = NewPage();
			Place(page, Hint("WardrobeHint"), 0f);

			var grid = new ItemGrid();
			grid.Height.Set(-30f, 1f);
			Place(page, grid, 30f);
			return page;
		}

		// ======================= жизненный цикл =======================
		public void OnShow()
		{
			RefreshPreview();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (_dragging)
			{
				if (!Main.mouseLeft)
				{
					_dragging = false;
				}
				else
				{
					CalculatedStyle screen = GetDimensions();
					Vector2 pos = Main.MouseScreen - _dragOffset;
					pos.X = MathHelper.Clamp(pos.X, 0f, Math.Max(0f, screen.Width - WindowWidth));
					pos.Y = MathHelper.Clamp(pos.Y, 0f, Math.Max(0f, screen.Height - WindowHeight));
					_root.Left.Set(pos.X, 0f);
					_root.Top.Set(pos.Y, 0f);
					Recalculate();
				}
			}

			// Клики по окну не должны использовать предмет в руке
			if (_dragging || _root.ContainsPoint(Main.MouseScreen))
				Main.LocalPlayer.mouseInterface = true;
		}
	}
}
