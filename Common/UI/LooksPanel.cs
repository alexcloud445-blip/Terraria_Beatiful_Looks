using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.Systems;

namespace Terraria_Beatiful_Looks.Common.UI
{
	/// <summary>
	/// Меню «Beautiful Looks»: выбор стиля и цвета ауры.
	/// Новую строку настроек добавлять в OnInitialize по тому же образцу (Top += RowHeight).
	/// </summary>
	public class LooksPanel : UIState
	{
		private const string Key = "Mods.Terraria_Beatiful_Looks.UI.";
		private const float RowHeight = 48f;

		private static readonly Color ButtonColor = new Color(63, 82, 151) * 0.85f;
		private static readonly Color ButtonHover = new Color(90, 115, 200);

		private UIPanel _root;
		private UITextPanel<string> _auraName;
		private readonly List<UIPanel> _swatches = new List<UIPanel>();

		private static LooksPlayer Looks => Main.LocalPlayer.GetModPlayer<LooksPlayer>();

		public override void OnInitialize()
		{
			_root = new UIPanel();
			_root.Width.Set(330f, 0f);
			_root.Height.Set(220f, 0f);
			_root.HAlign = 0.5f;
			_root.VAlign = 0.4f;
			_root.BackgroundColor = new Color(45, 30, 90) * 0.92f;
			Append(_root);

			// Заголовок и кнопка закрытия
			var title = new UIText(Language.GetText(Key + "Title"), 0.5f, true);
			title.HAlign = 0.5f;
			_root.Append(title);

			var close = MakeButton("X", 32f);
			close.Left.Set(-32f, 1f);
			close.Top.Set(-4f, 0f);
			close.OnLeftClick += (_, _) => ModContent.GetInstance<LooksUISystem>().Hide();
			_root.Append(close);

			float top = 44f;

			// Строка 1: стиль ауры  [<] Название [>]
			AddLabel("Aura", top);

			var prev = MakeButton("<", 32f);
			prev.Left.Set(110f, 0f);
			prev.Top.Set(top, 0f);
			prev.OnLeftClick += (_, _) => ChangeStyle(-1);
			_root.Append(prev);

			_auraName = new UITextPanel<string>("", 0.9f);
			_auraName.Width.Set(124f, 0f);
			_auraName.Height.Set(32f, 0f);
			_auraName.Left.Set(146f, 0f);
			_auraName.Top.Set(top, 0f);
			_auraName.BackgroundColor = new Color(30, 20, 60) * 0.9f;
			_root.Append(_auraName);

			var next = MakeButton(">", 32f);
			next.Left.Set(274f, 0f);
			next.Top.Set(top, 0f);
			next.OnLeftClick += (_, _) => ChangeStyle(1);
			_root.Append(next);

			top += RowHeight;

			// Строка 2: цвет ауры (палитра)
			AddLabel("Color", top);
			for (int i = 0; i < LooksPlayer.Palette.Length; i++)
			{
				int index = i;
				var swatch = new UIPanel();
				swatch.Width.Set(26f, 0f);
				swatch.Height.Set(26f, 0f);
				swatch.Left.Set(110f + i * 32f, 0f);
				swatch.Top.Set(top + 3f, 0f);
				swatch.BackgroundColor = LooksPlayer.Palette[i];
				swatch.OnLeftClick += (_, _) => SelectColor(index);
				swatch.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
				_swatches.Add(swatch);
				_root.Append(swatch);
			}

			top += RowHeight;

			// Подсказка внизу
			var hint = new UIText(Language.GetText(Key + "Hint"), 0.8f);
			hint.Top.Set(top + 4f, 0f);
			hint.HAlign = 0.5f;
			_root.Append(hint);
		}

		/// <summary>Обновляет подписи и выделение под текущего персонажа.</summary>
		public void Refresh()
		{
			if (_auraName == null)
				return;

			LooksPlayer looks = Looks;
			_auraName.SetText(Language.GetTextValue(Key + "Styles." + looks.Style));

			for (int i = 0; i < _swatches.Count; i++)
				_swatches[i].BorderColor = i == looks.ColorIndex ? Color.White : Color.Black;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// Чтобы клики по меню не использовали предмет в руке
			if (_root.ContainsPoint(Main.MouseScreen))
				Main.LocalPlayer.mouseInterface = true;
		}

		private void ChangeStyle(int dir)
		{
			Looks.CycleStyle(dir);
			SoundEngine.PlaySound(SoundID.MenuTick);
			Refresh();
		}

		private void SelectColor(int index)
		{
			Looks.ColorIndex = index;
			SoundEngine.PlaySound(SoundID.MenuTick);
			Refresh();
		}

		private void AddLabel(string key, float top)
		{
			var label = new UIText(Language.GetText(Key + key));
			label.Top.Set(top + 6f, 0f);
			_root.Append(label);
		}

		private static UITextPanel<string> MakeButton(string text, float width)
		{
			var button = new UITextPanel<string>(text, 0.9f);
			button.Width.Set(width, 0f);
			button.Height.Set(32f, 0f);
			button.BackgroundColor = ButtonColor;
			button.OnMouseOver += (_, el) =>
			{
				((UIPanel)el).BackgroundColor = ButtonHover;
				SoundEngine.PlaySound(SoundID.MenuTick);
			};
			button.OnMouseOut += (_, el) => ((UIPanel)el).BackgroundColor = ButtonColor;
			return button;
		}
	}
}
