using Terraria.ModLoader;

namespace BeautifulLooks.Common.Systems
{
	public class KeybindSystem : ModSystem
	{
		public static ModKeybind ToggleLooksUI { get; private set; }

		public override void Load()
		{
			ToggleLooksUI = KeybindLoader.RegisterKeybind(Mod, "ToggleLooksUI", "K");
		}

		public override void Unload()
		{
			ToggleLooksUI = null;
		}
	}
}
