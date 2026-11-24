using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader.IO;

namespace BeautifulLooks.Common.Looks
{
	/// <summary>Цвет в формате HSL + режим «радуга». Используется аурой, следами и эффектами.</summary>
	public class ColorSetting
	{
		public float Hue = 0.55f;
		public float Saturation = 0.9f;
		public float Lightness = 0.65f;
		public bool Rainbow;

		public ColorSetting() { }

		public ColorSetting(float hue, float saturation = 0.9f, float lightness = 0.65f)
		{
			Hue = hue;
			Saturation = saturation;
			Lightness = lightness;
		}

		/// <summary>Текущий цвет. offset сдвигает оттенок в режиме радуги (для переливов).</summary>
		public Color Get(float offset = 0f)
		{
			float hue = Rainbow ? Wrap(Main.GlobalTimeWrappedHourly * 0.25f + offset) : Hue;
			return Main.hslToRgb(hue, Saturation, Lightness);
		}

		/// <summary>Смесь двух цветов палитры: t = 0 — этот, t = 1 — второй.</summary>
		public Color Mix(ColorSetting second, float t, float offset = 0f) =>
			BeautifulLooks.Common.VFX.VfxMath.LerpVivid(Get(offset), second.Get(offset + 0.15f), t);

		public void Set(Color color, bool rainbow = false)
		{
			SetFromColor(color);
			Rainbow = rainbow;
		}

		public void SetFromColor(Color color)
		{
			Vector3 hsl = Main.rgbToHsl(color);
			Hue = hsl.X;
			Saturation = hsl.Y;
			Lightness = hsl.Z;
			Rainbow = false;
		}

		public void CopyFrom(ColorSetting other)
		{
			Hue = other.Hue;
			Saturation = other.Saturation;
			Lightness = other.Lightness;
			Rainbow = other.Rainbow;
		}

		public bool SameAs(ColorSetting other)
		{
			return Hue == other.Hue && Saturation == other.Saturation && Lightness == other.Lightness && Rainbow == other.Rainbow;
		}

		public void Randomize()
		{
			Hue = Main.rand.NextFloat();
			Saturation = Main.rand.NextFloat(0.6f, 1f);
			Lightness = Main.rand.NextFloat(0.5f, 0.75f);
			Rainbow = Main.rand.NextBool(6);
		}

		public TagCompound Save()
		{
			return new TagCompound
			{
				["h"] = Hue,
				["s"] = Saturation,
				["l"] = Lightness,
				["rainbow"] = Rainbow
			};
		}

		public void Load(TagCompound tag)
		{
			if (tag == null)
				return;
			if (tag.TryGet("h", out float h)) Hue = Clamp01(h);
			if (tag.TryGet("s", out float s)) Saturation = Clamp01(s);
			if (tag.TryGet("l", out float l)) Lightness = Clamp01(l);
			if (tag.TryGet("rainbow", out bool r)) Rainbow = r;
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(Hue);
			writer.Write(Saturation);
			writer.Write(Lightness);
			writer.Write(Rainbow);
		}

		public void Read(BinaryReader reader)
		{
			Hue = Clamp01(reader.ReadSingle());
			Saturation = Clamp01(reader.ReadSingle());
			Lightness = Clamp01(reader.ReadSingle());
			Rainbow = reader.ReadBoolean();
		}

		private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);

		private static float Wrap(float v) => v - (float)Math.Floor(v);
	}
}
