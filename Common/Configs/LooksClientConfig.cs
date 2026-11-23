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

		/// <summary>Сколько частиц мода может быть на экране одновременно.</summary>
		[DefaultValue(2500)]
		[Range(300, 8000)]
		[Increment(100)]
		public int MaxParticles;

		/// <summary>Светящиеся ленты на шейдерах Terraria. Выключить, если с ними что-то не так.</summary>
		[DefaultValue(true)]
		public bool ShaderRibbons;

		/// <summary>Показывать эффекты других игроков (в мультиплеере).</summary>
		[DefaultValue(true)]
		public bool ShowOtherPlayers;
	}
}
