using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria_Beatiful_Looks.Common.Players;

// Пространство имён должно совпадать с именем папки мода (Terraria_Beatiful_Looks),
// иначе tModLoader не найдёт текстуры.
namespace Terraria_Beatiful_Looks
{
	public class BeautifulLooksMod : Mod
	{
		public enum PacketType : byte
		{
			SyncLook
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			var type = (PacketType)reader.ReadByte();
			switch (type)
			{
				case PacketType.SyncLook:
					byte playerIndex = reader.ReadByte();
					// На сервере доверяем только отправителю: клиент меняет лишь свой образ
					if (Main.netMode == NetmodeID.Server)
						playerIndex = (byte)whoAmI;

					LooksPlayer looks = Main.player[playerIndex].GetModPlayer<LooksPlayer>();
					looks.Look.Read(reader);

					if (Main.netMode == NetmodeID.Server)
						looks.SyncPlayer(-1, whoAmI, false);
					break;

				default:
					Logger.Warn($"Unknown packet type: {type}");
					break;
			}
		}
	}
}
