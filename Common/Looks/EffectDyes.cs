using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace Terraria_Beatiful_Looks.Common.Looks
{
	/// <summary>
	/// Краски, которыми можно перекрасить эффекты на персонаже (нимб, сияние, круги).
	/// Используются шейдеры ванильных красок. Номер 0 — без краски.
	/// Новую краску добавлять только В КОНЕЦ: номер сохраняется в файл персонажа.
	/// </summary>
	public static class EffectDyes
	{
		public static readonly int[] Items =
		{
			0,
			ItemID.RainbowDye,
			ItemID.IntenseRainbowDye,
			ItemID.LivingRainbowDye,
			ItemID.MidnightRainbowDye,
			ItemID.LivingFlameDye,
			ItemID.LivingOceanDye,
			ItemID.HadesDye,
			ItemID.BurningHadesDye,
			ItemID.ShadowflameHadesDye,
			ItemID.NebulaDye,
			ItemID.SolarDye,
			ItemID.VortexDye,
			ItemID.StardustDye,
			ItemID.PhaseDye,
			ItemID.ReflectiveGoldDye,
			ItemID.TwilightDye,
			ItemID.VoidDye,
			ItemID.PrismaticDye
		};

		public static int Count => Items.Length;

		/// <summary>Номер шейдера краски для DrawData.shader (0 — без шейдера).</summary>
		public static int ShaderId(int index)
		{
			if (index <= 0 || index >= Items.Length)
				return 0;
			return GameShaders.Armor.GetShaderIdFromItemId(Items[index]);
		}

		/// <summary>Название краски на языке игры.</summary>
		public static string Name(int index)
		{
			if (index <= 0 || index >= Items.Length)
				return null;
			return Lang.GetItemNameValue(Items[index]);
		}
	}
}
