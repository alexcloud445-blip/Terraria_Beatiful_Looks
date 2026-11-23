using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Configs;
using Terraria_Beatiful_Looks.Common.Effects;

namespace Terraria_Beatiful_Looks.Common.VFX
{
	/// <summary>
	/// Встраивает отрисовку эффектов в кадр игры: перехватывает Main.DrawPlayers_AfterProjectiles.
	/// До отрисовки игроков рисуется слой «позади», после — слой «спереди».
	/// Любая ошибка ловится, пишется в лог один раз, а после многих ошибок эффекты отключаются,
	/// чтобы не уронить игру.
	/// </summary>
	[Autoload(Side = ModSide.Client)]
	public class VfxRenderer : ModSystem
	{
		private static readonly HashSet<string> Logged = new HashSet<string>();
		private static int _errors;
		private static FieldInfo _beginCalled;

		public override void Load()
		{
			_beginCalled = typeof(SpriteBatch).GetField("beginCalled", BindingFlags.NonPublic | BindingFlags.Instance);
			On_Main.DrawPlayers_AfterProjectiles += DrawPlayersHook;
		}

		public override void Unload()
		{
			Logged.Clear();
			_errors = 0;
			_beginCalled = null;
		}

		private static void DrawPlayersHook(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
		{
			DrawLayer(front: false);
			orig(self);
			DrawLayer(front: true);
		}

		/// <summary>Пишет ошибку в лог мода один раз для каждого места.</summary>
		public static void LogOnce(string where, Exception e)
		{
			_errors++;
			if (Logged.Add(where))
				ModContent.GetInstance<BeautifulLooksMod>().Logger.Warn($"Beautiful Looks VFX error ({where}): {e}");
		}

		private static bool SpriteBatchActive(SpriteBatch sb) =>
			_beginCalled != null && _beginCalled.GetValue(sb) is bool b && b;

		private static void DrawLayer(bool front)
		{
			if (Main.gameMenu || Main.dedServ || _errors > 50)
				return;

			SpriteBatch sb = Main.spriteBatch;
			if (SpriteBatchActive(sb))
				return; // кто-то оставил SpriteBatch открытым — не вмешиваемся

			VfxAssets.Prepare();

			LooksClientConfig config = ModContent.GetInstance<LooksClientConfig>();
			bool shaderRibbons = config == null || config.ShaderRibbons;
			if (shaderRibbons)
			{
				try
				{
					Ribbons.PrepareDevice();
					if (!front)
					{
						TrailEffects.DrawRibbons();
						WeaponTrails.DrawRibbons();
					}
					ParticleSystem.DrawTrails(front);
				}
				catch (Exception e)
				{
					LogOnce("ribbons", e);
				}
			}

			bool begun = false;
			try
			{
				sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
					RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
				begun = true;
				if (!front && !shaderRibbons)
					WeaponTrails.DrawSprites(sb);
				ParticleSystem.DrawParticles(sb, front);
				if (front)
					StealthEffects.DrawEyes(sb);
			}
			catch (Exception e)
			{
				LogOnce("particles", e);
			}
			finally
			{
				if (begun)
				{
					try
					{
						sb.End();
					}
					catch (Exception e)
					{
						LogOnce("particles end", e);
					}
				}
			}
		}
	}
}
