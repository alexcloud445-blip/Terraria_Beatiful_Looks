using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Players;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Effects
{
	/// <summary>
	/// Светящийся контур: несколько копий персонажа, сдвинутых на 2 пикселя во все стороны и
	/// закрашенных светом, рисуются позади него — по краям остаётся сияющая обводка.
	/// Цвет плавно переливается между двумя цветами эффектов.
	/// </summary>
	public static class OutlineEffect
	{
		private static readonly Vector2[] Offsets =
		{
			new Vector2(2f, 0f), new Vector2(-2f, 0f), new Vector2(0f, 2f), new Vector2(0f, -2f),
			new Vector2(1.5f, 1.5f), new Vector2(-1.5f, 1.5f), new Vector2(1.5f, -1.5f), new Vector2(-1.5f, -1.5f)
		};

		public static void Draw(LooksPlayer mp, Camera camera)
		{
			LookSettings look = mp.Look;
			if (!look.Outline)
				return;

			Player player = mp.Player;
			float time = Main.GlobalTimeWrappedHourly;
			float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 3f);

			try
			{
				TrailEffects.DrawingEcho = true;
				TrailEffects.EchoTint = true;
				for (int i = 0; i < Offsets.Length; i++)
				{
					float t = 0.5f + 0.5f * (float)Math.Sin(time * 1.6f + i * MathHelper.PiOver4);
					TrailEffects.EchoColor = look.EffectColor.Mix(look.EffectColor2, t);
					TrailEffects.EchoOpacity = (i < 4 ? 0.55f : 0.35f) * pulse;
					Vector2 pos = player.position + Offsets[i] * (0.9f + 0.25f * pulse);
					Main.PlayerRenderer.DrawPlayer(camera, player, pos, player.fullRotation, player.fullRotationOrigin, 0.01f);
				}
			}
			catch (Exception e)
			{
				VfxRenderer.LogOnce("outline", e);
			}
			finally
			{
				TrailEffects.DrawingEcho = false;
			}
		}
	}
}
