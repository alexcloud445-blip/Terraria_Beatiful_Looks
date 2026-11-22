using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria_Beatiful_Looks.Common.Configs;
using Terraria_Beatiful_Looks.Common.Systems;

namespace Terraria_Beatiful_Looks.Common.Players
{
	/// <summary>Стили ауры. Новый стиль: добавить сюда + case в PostUpdate + строку в локализации.</summary>
	public enum AuraStyle
	{
		None,
		Glow,
		Sparkles,
		Orbit
	}

	/// <summary>Хранит косметические настройки персонажа и рисует ауру.</summary>
	public class LooksPlayer : ModPlayer
	{
		public static readonly Color[] Palette =
		{
			new Color(120, 200, 255), // голубой
			new Color(255, 120, 200), // розовый
			new Color(255, 190, 80),  // золотой
			new Color(130, 255, 140), // зелёный
			new Color(190, 130, 255), // фиолетовый
			new Color(255, 255, 255)  // белый
		};

		public static readonly int StyleCount = Enum.GetValues(typeof(AuraStyle)).Length;

		public AuraStyle Style = AuraStyle.None;
		public int ColorIndex;

		public Color AuraColor => Palette[Math.Clamp(ColorIndex, 0, Palette.Length - 1)];

		public void CycleStyle(int dir)
		{
			Style = (AuraStyle)(((int)Style + dir + StyleCount) % StyleCount);
		}

		// ---------- сохранение вместе с персонажем ----------
		public override void SaveData(TagCompound tag)
		{
			tag["auraStyle"] = (int)Style;
			tag["auraColor"] = ColorIndex;
		}

		public override void LoadData(TagCompound tag)
		{
			int style = tag.GetInt("auraStyle");
			Style = style >= 0 && style < StyleCount ? (AuraStyle)style : AuraStyle.None;
			ColorIndex = Math.Clamp(tag.GetInt("auraColor"), 0, Palette.Length - 1);
		}

		// ---------- горячая клавиша ----------
		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (KeybindSystem.ToggleLooksUI.JustPressed)
				ModContent.GetInstance<LooksUISystem>().Toggle();
		}

		// ---------- аура ----------
		public override void PostUpdate()
		{
			if (Main.dedServ || Style == AuraStyle.None || Player.dead || Player.invis)
				return;

			float intensity = ModContent.GetInstance<LooksClientConfig>().AuraIntensity;
			Color color = AuraColor;

			Lighting.AddLight(Player.Center, color.ToVector3() * 0.5f * intensity);

			switch (Style)
			{
				case AuraStyle.Glow:
					// Мягкое свечение + редкие поднимающиеся искры
					if (Main.rand.NextFloat() < 0.12f * intensity)
						SpawnDust(Player.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -4f),
							new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.4f)), color, 0.9f);
					break;

				case AuraStyle.Sparkles:
					// Искры по всему телу
					if (Main.rand.NextFloat() < 0.5f * intensity)
						SpawnDust(Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height * 0.6f),
							Main.rand.NextVector2Circular(0.5f, 0.5f) + new Vector2(0f, -0.4f), color, 0.8f);
					break;

				case AuraStyle.Orbit:
					// Три огонька, кружащие вокруг персонажа
					for (int i = 0; i < 3; i++)
					{
						if (Main.rand.NextFloat() > intensity)
							continue;
						float angle = Main.GameUpdateCount * 0.07f + i * MathHelper.TwoPi / 3f;
						Vector2 offset = angle.ToRotationVector2() * new Vector2(30f, 20f);
						SpawnDust(Player.Center + offset, Vector2.Zero, color, 1.0f);
					}
					break;
			}
		}

		private static void SpawnDust(Vector2 position, Vector2 velocity, Color color, float scale)
		{
			Dust d = Dust.NewDustPerfect(position, DustID.RainbowMk2, velocity, 0, color, scale);
			d.noGravity = true;
		}
	}
}
