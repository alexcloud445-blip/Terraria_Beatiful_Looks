using System;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace Terraria_Beatiful_Looks.Common.Looks
{
	/// <summary>Стили ауры. Новый стиль: добавить в конец + case в AuraEffects + строку в локализации.</summary>
	public enum AuraStyle
	{
		None,
		Glow,
		Sparkles,
		Orbit,
		Flames,
		Stars,
		Snow,
		Petals,
		Hearts,
		Vortex,
		Ring
	}

	/// <summary>Стили следа при движении.</summary>
	public enum TrailStyle
	{
		None,
		Sparkles,
		Stars,
		Flames,
		Hearts,
		Petals,
		Snow
	}

	/// <summary>Весь «образ» персонажа: то, что сохраняется, синхронизируется и кладётся в пресеты.</summary>
	public class LookSettings
	{
		public static readonly int AuraStyleCount = Enum.GetValues(typeof(AuraStyle)).Length;
		public static readonly int TrailStyleCount = Enum.GetValues(typeof(TrailStyle)).Length;

		public AuraStyle Aura = AuraStyle.None;
		public float AuraDensity = 1f;
		public ColorSetting AuraColor = new ColorSetting(0.55f);

		public TrailStyle Trail = TrailStyle.None;
		public ColorSetting TrailColor = new ColorSetting(0.85f);

		public bool Halo;
		public bool BackGlow;
		public bool GroundRing;
		public ColorSetting EffectColor = new ColorSetting(0.14f, 1f, 0.7f);

		public void CycleAura(int dir) => Aura = (AuraStyle)Wrap((int)Aura + dir, AuraStyleCount);

		public void CycleTrail(int dir) => Trail = (TrailStyle)Wrap((int)Trail + dir, TrailStyleCount);

		public LookSettings Clone()
		{
			var copy = new LookSettings();
			copy.CopyFrom(this);
			return copy;
		}

		public void CopyFrom(LookSettings o)
		{
			Aura = o.Aura;
			AuraDensity = o.AuraDensity;
			AuraColor.CopyFrom(o.AuraColor);
			Trail = o.Trail;
			TrailColor.CopyFrom(o.TrailColor);
			Halo = o.Halo;
			BackGlow = o.BackGlow;
			GroundRing = o.GroundRing;
			EffectColor.CopyFrom(o.EffectColor);
		}

		public bool SameAs(LookSettings o)
		{
			return Aura == o.Aura && AuraDensity == o.AuraDensity && AuraColor.SameAs(o.AuraColor)
				&& Trail == o.Trail && TrailColor.SameAs(o.TrailColor)
				&& Halo == o.Halo && BackGlow == o.BackGlow && GroundRing == o.GroundRing
				&& EffectColor.SameAs(o.EffectColor);
		}

		public void Reset() => CopyFrom(new LookSettings());

		public void Randomize()
		{
			Aura = (AuraStyle)Main.rand.Next(1, AuraStyleCount);
			AuraDensity = Main.rand.NextFloat(0.6f, 1.4f);
			AuraColor.Randomize();
			Trail = (TrailStyle)Main.rand.Next(TrailStyleCount);
			TrailColor.Randomize();
			Halo = Main.rand.NextBool(3);
			BackGlow = Main.rand.NextBool();
			GroundRing = Main.rand.NextBool(3);
			EffectColor.Randomize();
		}

		// ---------- сохранение ----------
		public TagCompound Save()
		{
			return new TagCompound
			{
				["aura"] = (int)Aura,
				["auraDensity"] = AuraDensity,
				["auraColor"] = AuraColor.Save(),
				["trail"] = (int)Trail,
				["trailColor"] = TrailColor.Save(),
				["halo"] = Halo,
				["backGlow"] = BackGlow,
				["groundRing"] = GroundRing,
				["effectColor"] = EffectColor.Save()
			};
		}

		public static LookSettings Load(TagCompound tag)
		{
			var s = new LookSettings();
			if (tag == null)
				return s;

			if (tag.TryGet("aura", out int aura)) s.Aura = (AuraStyle)Wrap(aura, AuraStyleCount);
			if (tag.TryGet("auraDensity", out float density)) s.AuraDensity = Math.Clamp(density, 0.2f, 2f);
			if (tag.TryGet("trail", out int trail)) s.Trail = (TrailStyle)Wrap(trail, TrailStyleCount);
			if (tag.TryGet("halo", out bool halo)) s.Halo = halo;
			if (tag.TryGet("backGlow", out bool glow)) s.BackGlow = glow;
			if (tag.TryGet("groundRing", out bool ring)) s.GroundRing = ring;
			if (tag.TryGet("auraColor", out TagCompound ac)) s.AuraColor.Load(ac);
			if (tag.TryGet("trailColor", out TagCompound tc)) s.TrailColor.Load(tc);
			if (tag.TryGet("effectColor", out TagCompound ec)) s.EffectColor.Load(ec);
			return s;
		}

		// ---------- сеть ----------
		public void Write(BinaryWriter w)
		{
			w.Write((byte)Aura);
			w.Write(AuraDensity);
			AuraColor.Write(w);
			w.Write((byte)Trail);
			TrailColor.Write(w);
			w.Write(Halo);
			w.Write(BackGlow);
			w.Write(GroundRing);
			EffectColor.Write(w);
		}

		public void Read(BinaryReader r)
		{
			Aura = (AuraStyle)Wrap(r.ReadByte(), AuraStyleCount);
			AuraDensity = Math.Clamp(r.ReadSingle(), 0.2f, 2f);
			AuraColor.Read(r);
			Trail = (TrailStyle)Wrap(r.ReadByte(), TrailStyleCount);
			TrailColor.Read(r);
			Halo = r.ReadBoolean();
			BackGlow = r.ReadBoolean();
			GroundRing = r.ReadBoolean();
			EffectColor.Read(r);
		}

		private static int Wrap(int v, int count) => ((v % count) + count) % count;
	}
}
