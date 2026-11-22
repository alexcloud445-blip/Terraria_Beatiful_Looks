using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Terraria_Beatiful_Looks.Common.Configs
{
	/// <summary>Настройки на стороне игрока (Настройки → Конфигурация модов).</summary>
	public class LooksClientConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(1f)]
		[Range(0.2f, 2f)]
		[Increment(0.1f)]
		public float AuraIntensity;
	}
}
