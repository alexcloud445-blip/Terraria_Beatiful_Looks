using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using BeautifulLooks.Common.UI;

namespace BeautifulLooks.Common.Systems
{
	/// <summary>Показывает/прячет меню и вставляет его в слои интерфейса игры.</summary>
	[Autoload(Side = ModSide.Client)]
	public class LooksUISystem : ModSystem
	{
		private UserInterface _interface;
		private LooksPanel _panel;
		private GameTime _lastGameTime;

		public bool Visible => _interface?.CurrentState != null;

		public override void Load()
		{
			_interface = new UserInterface();
		}

		public override void Unload()
		{
			_interface = null;
			_panel = null;
		}

		public void Toggle()
		{
			if (Visible)
				Hide();
			else
				Show();
		}

		public void Show()
		{
			// Меню создаётся при первом открытии, когда персонаж уже загружен
			if (_panel == null)
			{
				_panel = new LooksPanel();
				_panel.Activate();
			}

			_interface.SetState(_panel);
			_panel.OnShow();
			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public void Hide()
		{
			_interface.SetState(null);
			SoundEngine.PlaySound(SoundID.MenuClose);
		}

		public override void OnWorldUnload()
		{
			_interface?.SetState(null);
		}

		public override void UpdateUI(GameTime gameTime)
		{
			_lastGameTime = gameTime;
			if (Visible)
				_interface.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
			if (index == -1)
				return;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"BeautifulLooks: Looks Panel",
				delegate
				{
					if (Visible && _lastGameTime != null)
						_interface.Draw(Main.spriteBatch, _lastGameTime);
					return true;
				},
				InterfaceScaleType.UI));
		}
	}
}
