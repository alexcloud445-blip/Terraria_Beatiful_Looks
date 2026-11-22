using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.Systems;

namespace Terraria_Beatiful_Looks.Common.UI
{
	/// <summary>
	/// Главное окно: заголовок (за него можно тащить), слева превью и пресеты, справа вкладки.
	/// Новая вкладка: добавить в TabNames + метод Build...Tab + case в CreateTabs.
	/// </summary>
	public class LooksPanel : UIState
	{
		private const float WindowWidth = 640f;
		private const float WindowHeight = 450f;
		private const float LeftColumn = 180f;

		private static readonly string[] TabNames = { "TabAura", "TabTrail", "TabEffects", "TabWardrobe" };

		private UIPanel _root;
		private UIPanel _previewBox;
		private UIElement _content;
		private UIElement[] _tabs;
		private int _currentTab;

		private bool _dragging;
		private Vector2 _dragOffset;

		private static LooksPlayer Me => Main.LocalPlayer.GetModPlayer<LooksPlayer>();
		private static LookSettings Look => Me.Look;

		public override void OnInitialize()
		{
			_root = new UIPanel { BackgroundColor = Theme.Window, BorderColor = Theme.Accent * 0.6f };
			_root.Width.Set(WindowWidth, 0f);
			_root.Height.Set(WindowHeight, 0f);
			_root.HAlign = 0.5f;
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
			header.Width.Set(-44f, 1f);
			header.Height.Set(32f, 0f);
			header.OnLeftMouseDown += (_, _) => StartDrag();
			_root.Append(header);

			var title = new DynamicText(() => "~ " + Theme.T("Title") + " ~", 1.1f) { AnchorX = 0.5f, Color = Theme.Accent };
			title.Left.Set(22f, 0f);
			title.Height.Set(32f, 0f);
			header.Append(title);

			var hint = new DynamicText(() => Theme.T("DragHint"), 0.7f) { Color = Theme.TextDim * 0.8f };
			hint.Height.Set(32f, 0f);
			header.Append(hint);

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
			_previewBox = new UIPanel { BackgroundColor = Theme.Inner };
			_previewBox.Top.Set(40f, 0f);
			_previewBox.Width.Set(LeftColumn, 0f);
			_previewBox.Height.Set(190f, 0f);
			_root.Append(_previewBox);

			var presetsTitle = new DynamicText(() => Theme.T("Presets")) { Color = Theme.Accent };
			presetsTitle.Top.Set(236f, 0f);
			presetsTitle.Width.Set(LeftColumn, 0f);
			_root.Append(presetsTitle);

			for (int i = 0; i < LooksPlayer.PresetCount; i++)
			{
				int slot = i;
				float top = 262f + i * 34f;

				var label = new DynamicText(() => (slot + 1) + (Me.Presets[slot] == null ? " -" : " *")) { Color = Theme.TextDim };
				label.Top.Set(top, 0f);
				label.Width.Set(36f, 0f);
				label.Height.Set(30f, 0f);
				_root.Append(label);

				var save = new LooksButton(() => Theme.T("Save"), 68f, 30f, 0.75f);
				save.Left.Set(38f, 0f);
				save.Top.Set(top, 0f);
				save.OnLeftClick += (_, _) =>
				{
					Me.SavePreset(slot);
					Main.NewText(Theme.T("PresetSaved").Replace("{0}", (slot + 1).ToString()), Theme.Accent);
				};
				_root.Append(save);

				var load = new LooksButton(() => Theme.T("Load"), 70f, 30f, 0.75f);
				load.Left.Set(110f, 0f);
				load.Top.Set(top, 0f);
				load.OnLeftClick += (_, _) =>
				{
					if (!Me.LoadPreset(slot))
						Main.NewText(Theme.T("PresetEmpty"), Color.Gray);
				};
				_root.Append(load);
			}

			var random = new LooksButton(() => Theme.T("Random"), 88f, 30f, 0.75f);
			random.Top.Set(368f, 0f);
			random.OnLeftClick += (_, _) => Look.Randomize();
			_root.Append(random);

			var reset = new LooksButton(() => Theme.T("Reset"), 88f, 30f, 0.75f);
			reset.Left.Set(92f, 0f);
			reset.Top.Set(368f, 0f);
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
			float tabWidth = (WindowWidth - 24f - LeftColumn - 12f - 6f * (TabNames.Length - 1)) / TabNames.Length;
			for (int i = 0; i < TabNames.Length; i++)
			{
				int index = i;
				var tab = new LooksButton(() => Theme.T(TabNames[index]), tabWidth, 32f, 0.85f);
				tab.Left.Set(LeftColumn + 12f + i * (tabWidth + 6f), 0f);
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

			_tabs = new[] { BuildAuraTab(), BuildTrailTab(), BuildEffectsTab(), BuildWardrobeTab() };
			SelectTab(0);
		}

		private void SelectTab(int index)
		{
			_currentTab = index;
			_content.RemoveAllChildren();
			_content.Append(_tabs[index]);
			_content.Recalculate();
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

		private UIElement BuildAuraTab()
		{
			UIElement page = NewPage();
			Place(page, new SelectorRow(() => Theme.T("Style"),
				() => Theme.T("Aura." + Look.Aura),
				dir => Look.CycleAura(dir)), 0f);

			Place(page, new LooksSlider(() => Theme.T("Density"),
				() => (Look.AuraDensity - 0.2f) / 1.8f,
				v => Look.AuraDensity = 0.2f + v * 1.8f,
				format: v => (0.2f + v * 1.8f).ToString("0.0") + "x"), 42f);

			Place(page, Header("Color"), 76f);
			Place(page, new ColorPicker(() => Look.AuraColor), 102f);
			return page;
		}

		private UIElement BuildTrailTab()
		{
			UIElement page = NewPage();
			Place(page, new SelectorRow(() => Theme.T("Style"),
				() => Theme.T("Trail." + Look.Trail),
				dir => Look.CycleTrail(dir)), 0f);

			var hint = new DynamicText(() => Theme.T("TrailHint"), 0.75f) { Color = Theme.TextDim };
			Place(page, hint, 38f);

			Place(page, Header("Color"), 76f);
			Place(page, new ColorPicker(() => Look.TrailColor), 102f);
			return page;
		}

		private UIElement BuildEffectsTab()
		{
			UIElement page = NewPage();
			(string key, Func<bool> get, Action toggle)[] toggles =
			{
				("Halo", () => Look.Halo, () => Look.Halo = !Look.Halo),
				("BackGlow", () => Look.BackGlow, () => Look.BackGlow = !Look.BackGlow),
				("GroundRing", () => Look.GroundRing, () => Look.GroundRing = !Look.GroundRing)
			};

			for (int i = 0; i < toggles.Length; i++)
			{
				var t = toggles[i];
				var button = new LooksButton(() => (t.get() ? "> " : "") + Theme.T(t.key), 0f, 32f, 0.8f);
				button.Width.Set(-8f, 1f / 3f);
				button.Left.Set(4f, i / 3f);
				button.IsSelected = t.get;
				button.OnLeftClick += (_, _) => t.toggle();
				Place(page, button, 0f);
			}

			var hint = new DynamicText(() => Theme.T("EffectsHint"), 0.75f) { Color = Theme.TextDim };
			Place(page, hint, 38f);

			Place(page, Header("EffectColor"), 76f);
			Place(page, new ColorPicker(() => Look.EffectColor), 102f);
			return page;
		}

		private UIElement BuildWardrobeTab()
		{
			UIElement page = NewPage();
			var hint = new DynamicText(() => Theme.T("WardrobeHint"), 0.75f) { Color = Theme.TextDim };
			Place(page, hint, 0f);

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
