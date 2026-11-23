using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria_Beatiful_Looks.Common.Configs;
using Terraria_Beatiful_Looks.Common.Effects;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Systems;
using Terraria_Beatiful_Looks.Common.VFX;

namespace Terraria_Beatiful_Looks.Common.Players
{
	/// <summary>Хранит образ персонажа, пресеты, историю движения; синхронизирует их и запускает эффекты.</summary>
	public class LooksPlayer : ModPlayer
	{
		public const int PresetCount = 5;

		/// <summary>Сколько прошлых позиций помнить (для ленты и призрачных копий).</summary>
		public const int HistoryLength = 64;

		public LookSettings Look = new LookSettings();
		public LookSettings[] Presets = new LookSettings[PresetCount];

		/// <summary>Центр персонажа в прошлые тики: [0] — сейчас, [1] — тик назад...</summary>
		public readonly Vector2[] History = new Vector2[HistoryLength];
		public int HistoryCount;

		public override void Initialize()
		{
			Look = new LookSettings();
			Presets = new LookSettings[PresetCount];
			HistoryCount = 0;
		}

		/// <summary>Показывать ли эффекты этого игрока прямо сейчас (частицы, ленты, копии).</summary>
		public bool EffectsVisible
		{
			get
			{
				if (Main.dedServ || !Player.active || Player.dead || Player.ghost || Player.invis || !Look.Enabled)
					return false;
				if (Player.whoAmI != Main.myPlayer && !(ModContent.GetInstance<LooksClientConfig>()?.ShowOtherPlayers ?? true))
					return false;
				return true;
			}
		}

		/// <summary>То же для слоёв отрисовки (работает и в превью меню, где игрок может быть «неактивен»).</summary>
		public bool EffectsVisibleInDraw
		{
			get
			{
				if (Player.ghost || Player.invis || !Look.Enabled)
					return false;
				if (!Main.gameMenu && Player.whoAmI != Main.myPlayer && !(ModContent.GetInstance<LooksClientConfig>()?.ShowOtherPlayers ?? true))
					return false;
				return true;
			}
		}

		// ---------- сохранение вместе с персонажем ----------
		public override void SaveData(TagCompound tag)
		{
			tag["look"] = Look.Save();
			for (int i = 0; i < PresetCount; i++)
			{
				if (Presets[i] != null)
					tag["preset" + i] = Presets[i].Save();
			}
		}

		public override void LoadData(TagCompound tag)
		{
			if (tag.TryGet("look", out TagCompound look))
			{
				Look = LookSettings.Load(look);
			}
			else if (tag.TryGet("auraStyle", out int oldStyle))
			{
				// Перенос настроек из v0.1
				Look = new LookSettings { Aura = (AuraStyle)Math.Clamp(oldStyle, 0, 3) };
				Color[] oldPalette = { new(120, 200, 255), new(255, 120, 200), new(255, 190, 80), new(130, 255, 140), new(190, 130, 255), Color.White };
				int oldColor = tag.GetInt("auraColor");
				Look.AuraColor.SetFromColor(oldPalette[Math.Clamp(oldColor, 0, oldPalette.Length - 1)]);
			}

			for (int i = 0; i < PresetCount; i++)
			{
				if (tag.TryGet("preset" + i, out TagCompound p))
					Presets[i] = LookSettings.Load(p);
			}
		}

		// ---------- пресеты ----------
		public void SavePreset(int slot) => Presets[slot] = Look.Clone();

		public bool LoadPreset(int slot)
		{
			if (Presets[slot] == null)
				return false;
			Look.CopyFrom(Presets[slot]);
			return true;
		}

		// ---------- мультиплеер ----------
		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)BeautifulLooksMod.PacketType.SyncLook);
			packet.Write((byte)Player.whoAmI);
			Look.Write(packet);
			packet.Send(toWho, fromWho);
		}

		public override void CopyClientState(ModPlayer targetCopy)
		{
			((LooksPlayer)targetCopy).Look.CopyFrom(Look);
		}

		public override void SendClientChanges(ModPlayer clientPlayer)
		{
			if (!((LooksPlayer)clientPlayer).Look.SameAs(Look))
				SyncPlayer(-1, Main.myPlayer, false);
		}

		// ---------- горячая клавиша ----------
		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (KeybindSystem.ToggleLooksUI.JustPressed)
				ModContent.GetInstance<LooksUISystem>().Toggle();
		}

		// ---------- эффекты ----------
		public override void PostUpdate()
		{
			if (Main.dedServ)
				return;

			RecordHistory();

			if (!EffectsVisible)
				return;

			AuraEffects.Update(Player, Look);
			TrailEffects.Update(Player, Look);
		}

		private void RecordHistory()
		{
			// Телепорт или возрождение — начинаем историю заново, чтобы лента не тянулась через весь мир
			if (HistoryCount > 0 && Vector2.DistanceSquared(History[0], Player.Center) > 400f * 400f)
				HistoryCount = 0;

			for (int i = Math.Min(HistoryCount, HistoryLength - 1); i > 0; i--)
				History[i] = History[i - 1];
			History[0] = Player.Center;
			if (HistoryCount < HistoryLength)
				HistoryCount++;
		}

		/// <summary>Призрачные копии рисуются перед самим игроком (позади него).</summary>
		public override void DrawPlayer(Camera camera)
		{
			if (!TrailEffects.DrawingEcho && EffectsVisible)
				TrailEffects.DrawEchoes(this, camera);
		}

		/// <summary>Перекрашивает призрачные копии (вызывается для каждой отрисовки игрока).</summary>
		public override void TransformDrawData(ref PlayerDrawSet drawInfo)
		{
			if (!TrailEffects.DrawingEcho || drawInfo.drawPlayer != Player)
				return;

			var cache = drawInfo.DrawDataCache;
			for (int i = 0; i < cache.Count; i++)
			{
				DrawData data = cache[i];
				if (TrailEffects.EchoTint)
				{
					data.color = VfxMath.Additive(TrailEffects.EchoColor, TrailEffects.EchoOpacity);
					data.shader = 0;
				}
				else
				{
					data.color *= TrailEffects.EchoOpacity;
				}
				cache[i] = data;
			}
		}
	}
}
