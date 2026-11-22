using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria_Beatiful_Looks.Common.Effects;
using Terraria_Beatiful_Looks.Common.Looks;
using Terraria_Beatiful_Looks.Common.Systems;

namespace Terraria_Beatiful_Looks.Common.Players
{
	/// <summary>Хранит образ персонажа, пресеты, синхронизирует их и запускает эффекты.</summary>
	public class LooksPlayer : ModPlayer
	{
		public const int PresetCount = 3;

		public LookSettings Look = new LookSettings();
		public LookSettings[] Presets = new LookSettings[PresetCount];

		public override void Initialize()
		{
			Look = new LookSettings();
			Presets = new LookSettings[PresetCount];
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
			if (Main.dedServ || Player.dead || Player.invis || Player.ghost)
				return;

			AuraEffects.Update(Player, Look);
			TrailEffects.Update(Player, Look);
		}
	}
}
