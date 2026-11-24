using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using BeautifulLooks.Common.Looks;

namespace BeautifulLooks.Common.VFX
{
	/// <summary>
	/// Светящиеся ленты на ванильных шейдерах (те же, что у Радужного жезла, Огненного хлыста,
	/// Терраризмы и Императрицы Света). Рисуются вне SpriteBatch через VertexStrip.
	/// </summary>
	public static class Ribbons
	{
		private static VertexStrip _strip;

		/// <summary>Имя ванильного шейдера для стиля ленты.</summary>
		private static string ShaderName(RibbonStyle style) => style switch
		{
			RibbonStyle.Rainbow => "RainbowRod",
			RibbonStyle.Flame => "FlameLash",
			RibbonStyle.Empress => "EmpressBlade",
			_ => "MagicMissile"
		};

		/// <summary>Состояние видеокарты для лент: обычное смешивание (свет — через A = 0), без отсечения граней.</summary>
		public static void PrepareDevice()
		{
			GraphicsDevice gd = Main.instance.GraphicsDevice;
			gd.BlendState = BlendState.AlphaBlend;
			gd.DepthStencilState = DepthStencilState.None;
			gd.RasterizerState = RasterizerState.CullNone;
		}

		/// <summary>
		/// Рисует ленту по точкам в МИРОВЫХ координатах. positions[0] — голова ленты.
		/// colors/width получают progress 0..1 от головы к хвосту; width — половина ширины в пикселях.
		/// </summary>
		public static void Draw(Vector2[] positions, float[] rotations, VertexStrip.StripColorFunction colors,
			VertexStrip.StripHalfWidthFunction width, RibbonStyle style)
		{
			if (positions == null || positions.Length < 2 || style == RibbonStyle.None)
				return;
			if (!GameShaders.Misc.TryGetValue(ShaderName(style), out MiscShaderData shader))
				return;

			switch (style)
			{
				case RibbonStyle.Rainbow:
					shader.UseSaturation(-2.8f);
					shader.UseOpacity(4f);
					break;
				case RibbonStyle.Flame:
					shader.UseSaturation(-2f);
					shader.UseOpacity(6f);
					break;
				case RibbonStyle.Empress:
					shader.UseShaderSpecificData(new Vector4(1f, 0f, 0f, 0.6f));
					break;
				default:
					shader.UseSaturation(-2.8f);
					shader.UseOpacity(2f);
					break;
			}

			// Матрица проекции: экранные пиксели (с учётом зума) → экран. Та же, что у SpriteBatch мира.
			Viewport viewport = Main.instance.GraphicsDevice.Viewport;
			Matrix projection = Main.GameViewMatrix.TransformationMatrix
				* Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, -1f, 1f);
			shader.Shader.Parameters["uWorldViewProjection"]?.SetValue(projection);

			shader.Apply();
			_strip ??= new VertexStrip();
			_strip.PrepareStripWithProceduralPadding(positions, rotations, colors, width, -Main.screenPosition, includeBacksides: true);
			_strip.DrawTrail();
			Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		}
	}
}
