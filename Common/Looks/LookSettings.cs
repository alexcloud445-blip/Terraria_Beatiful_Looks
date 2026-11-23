using System;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace Terraria_Beatiful_Looks.Common.Looks
{
	/// <summary>
	/// Стили ауры. Новый стиль: добавить В КОНЕЦ (номера сохраняются в файл персонажа)
	/// + case в AuraEffects + строку в локализации (UI.Aura.Имя).
	/// </summary>
	public enum AuraStyle
	{
		None,
		Glow,
		Sparkles,
		Comets,
		Flames,
		Starfall,
		Frost,
		Sakura,
		Hearts,
		Vortex,
		CelestialRing,
		Storm,
		Void,
		Divine,
		Prism,
		Butterflies,
		Runes,
		Fireflies,
		// v0.6
		Astrolabe,
		Aurora,
		Galaxy,
		Eclipse,
		Mandala
	}

	/// <summary>Частицы, которые сыплются за персонажем при движении.</summary>
	public enum TrailStyle
	{
		None,
		Sparkles,
		Stars,
		Flames,
		Hearts,
		Petals,
		Snow,
		Feathers,
		Smoke,
		Runes,
		Prism,
		Butterflies
	}

	/// <summary>Светящаяся лента за персонажем (шейдеры Terraria).</summary>
	public enum RibbonStyle
	{
		None,
		Soft,
		Rainbow,
		Flame,
		Empress,
		Twin
	}

	/// <summary>Призрачные копии персонажа при движении.</summary>
	public enum EchoStyle
	{
		None,
		Ghost,
		Afterimage,
		Phantom
	}

	/// <summary>Стили нимба.</summary>
	public enum HaloStyle
	{
		None,
		Ring,
		Double,
		Radiant,
		Thorns,
		Starry,
		// v0.6
		RuneCrown,
		Diadem,
		Horns,
		Eclipse
	}

	/// <summary>Крылья из эффектов (рисуются поверх/вместо надетых крыльев).</summary>
	public enum WingStyle
	{
		None,
		Seraph,
		Light,
		Butterfly,
		Demon,
		Flame,
		Crystal,
		Constellation,
		Spirit,
		// v0.6
		Phoenix,
		Aurora,
		Nebula,
		Floral
	}

	/// <summary>Эффект рывка (щит Ктулху, таби, рывки Calamity и т. п.).</summary>
	public enum DashStyle
	{
		None,
		Flash,
		Shadow,
		Flame,
		Thunder,
		Prism,
		Sakura,
		Void
	}

	/// <summary>Следы шагов, прыжок и приземление.</summary>
	public enum StepStyle
	{
		None,
		Flowers,
		Frost,
		Embers,
		Stardust,
		Shadow,
		Ripple
	}

	/// <summary>Светящийся след за брошенным оружием и снарядами игрока.</summary>
	public enum WeaponTrailStyle
	{
		None,
		Soft,
		Sparkle,
		Flame,
		Prism,
		Shadow,
		Stardust
	}

	/// <summary>Спутник из света, который летает рядом с персонажем.</summary>
	public enum CompanionStyle
	{
		None,
		Wisp,
		Fox,
		Butterfly,
		Serpent
	}

	/// <summary>Весь «образ» персонажа: то, что сохраняется, синхронизируется и кладётся в пресеты.</summary>
	public class LookSettings
	{
		public static readonly int AuraStyleCount = Enum.GetValues(typeof(AuraStyle)).Length;
		public static readonly int TrailStyleCount = Enum.GetValues(typeof(TrailStyle)).Length;
		public static readonly int RibbonStyleCount = Enum.GetValues(typeof(RibbonStyle)).Length;
		public static readonly int EchoStyleCount = Enum.GetValues(typeof(EchoStyle)).Length;
		public static readonly int HaloStyleCount = Enum.GetValues(typeof(HaloStyle)).Length;
		public static readonly int WingStyleCount = Enum.GetValues(typeof(WingStyle)).Length;
		public static readonly int DashStyleCount = Enum.GetValues(typeof(DashStyle)).Length;
		public static readonly int StepStyleCount = Enum.GetValues(typeof(StepStyle)).Length;
		public static readonly int WeaponTrailStyleCount = Enum.GetValues(typeof(WeaponTrailStyle)).Length;
		public static readonly int CompanionStyleCount = Enum.GetValues(typeof(CompanionStyle)).Length;

		/// <summary>Общий выключатель всех эффектов образа.</summary>
		public bool Enabled = true;

		// ---------- аура ----------
		public AuraStyle Aura = AuraStyle.None;
		public float AuraDensity = 1f;
		public float AuraSize = 1f;
		public float AuraSpeed = 1f;
		public ColorSetting AuraColor = new ColorSetting(0.55f);
		public ColorSetting AuraColor2 = new ColorSetting(0.75f, 0.9f, 0.8f);

		// ---------- след ----------
		public TrailStyle Trail = TrailStyle.None;
		public RibbonStyle Ribbon = RibbonStyle.None;
		public float RibbonWidth = 1f;
		public float RibbonLength = 1f;
		public EchoStyle Echo = EchoStyle.None;
		public ColorSetting TrailColor = new ColorSetting(0.85f);
		public ColorSetting TrailColor2 = new ColorSetting(0.6f, 0.9f, 0.8f);

		// ---------- эффекты на персонаже ----------
		public HaloStyle Halo = HaloStyle.None;
		public bool BackGlow;
		public bool GodRays;
		public bool GroundRing;
		public bool Sigil;
		public bool PulseRing;
		/// <summary>Светящийся контур вокруг персонажа.</summary>
		public bool Outline;
		/// <summary>Номер краски из EffectDyes.Items (0 — без краски).</summary>
		public int EffectDye;
		public ColorSetting EffectColor = new ColorSetting(0.14f, 1f, 0.7f);
		public ColorSetting EffectColor2 = new ColorSetting(0.05f, 0.9f, 0.68f);

		// ---------- крылья ----------
		public WingStyle Wings = WingStyle.None;
		public float WingSize = 1f;
		public ColorSetting WingColor = new ColorSetting(0.13f, 0.9f, 0.75f);
		public ColorSetting WingColor2 = new ColorSetting(0.55f, 0.8f, 0.8f);

		// ---------- движение (вкладка «Движение») ----------
		/// <summary>Эффект рывка.</summary>
		public DashStyle Dash = DashStyle.None;
		/// <summary>Следы шагов, прыжок и приземление.</summary>
		public StepStyle Steps = StepStyle.None;
		/// <summary>След за брошенным оружием и снарядами.</summary>
		public WeaponTrailStyle WeaponTrail = WeaponTrailStyle.None;
		/// <summary>false — только метательное оружие (и разбойник Calamity), true — все снаряды игрока.</summary>
		public bool WeaponTrailAll;
		/// <summary>Эффекты скрытности разбойника Calamity (дымка, глаза, контур, вспышка удара).</summary>
		public bool StealthFx = true;
		public ColorSetting MotionColor = new ColorSetting(0.14f, 1f, 0.7f);
		public ColorSetting MotionColor2 = new ColorSetting(0.05f, 0.9f, 0.68f);

		// ---------- спутник ----------
		public CompanionStyle Companion = CompanionStyle.None;
		public float CompanionSize = 1f;
		public ColorSetting CompanionColor = new ColorSetting(0.52f, 0.85f, 0.72f);
		public ColorSetting CompanionColor2 = new ColorSetting(0.78f, 0.85f, 0.75f);

		/// <summary>Все пары цветов образа (для тем, «скопировать на всё» и т. п.).</summary>
		public (ColorSetting, ColorSetting)[] ColorPairs => new[]
		{
			(AuraColor, AuraColor2), (TrailColor, TrailColor2), (MotionColor, MotionColor2),
			(EffectColor, EffectColor2), (WingColor, WingColor2), (CompanionColor, CompanionColor2)
		};

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
			Enabled = o.Enabled;
			Aura = o.Aura;
			AuraDensity = o.AuraDensity;
			AuraSize = o.AuraSize;
			AuraSpeed = o.AuraSpeed;
			AuraColor.CopyFrom(o.AuraColor);
			AuraColor2.CopyFrom(o.AuraColor2);
			Trail = o.Trail;
			Ribbon = o.Ribbon;
			RibbonWidth = o.RibbonWidth;
			RibbonLength = o.RibbonLength;
			Echo = o.Echo;
			TrailColor.CopyFrom(o.TrailColor);
			TrailColor2.CopyFrom(o.TrailColor2);
			Halo = o.Halo;
			BackGlow = o.BackGlow;
			GodRays = o.GodRays;
			GroundRing = o.GroundRing;
			Sigil = o.Sigil;
			PulseRing = o.PulseRing;
			Outline = o.Outline;
			Dash = o.Dash;
			EffectDye = o.EffectDye;
			EffectColor.CopyFrom(o.EffectColor);
			EffectColor2.CopyFrom(o.EffectColor2);
			Wings = o.Wings;
			WingSize = o.WingSize;
			WingColor.CopyFrom(o.WingColor);
			WingColor2.CopyFrom(o.WingColor2);
			Steps = o.Steps;
			WeaponTrail = o.WeaponTrail;
			WeaponTrailAll = o.WeaponTrailAll;
			StealthFx = o.StealthFx;
			MotionColor.CopyFrom(o.MotionColor);
			MotionColor2.CopyFrom(o.MotionColor2);
			Companion = o.Companion;
			CompanionSize = o.CompanionSize;
			CompanionColor.CopyFrom(o.CompanionColor);
			CompanionColor2.CopyFrom(o.CompanionColor2);
		}

		public bool SameAs(LookSettings o)
		{
			return Enabled == o.Enabled
				&& Aura == o.Aura && AuraDensity == o.AuraDensity && AuraSize == o.AuraSize && AuraSpeed == o.AuraSpeed
				&& AuraColor.SameAs(o.AuraColor) && AuraColor2.SameAs(o.AuraColor2)
				&& Trail == o.Trail && Ribbon == o.Ribbon && RibbonWidth == o.RibbonWidth && RibbonLength == o.RibbonLength
				&& Echo == o.Echo && TrailColor.SameAs(o.TrailColor) && TrailColor2.SameAs(o.TrailColor2)
				&& Halo == o.Halo && BackGlow == o.BackGlow && GodRays == o.GodRays && GroundRing == o.GroundRing
				&& Sigil == o.Sigil && PulseRing == o.PulseRing && Outline == o.Outline && Dash == o.Dash && EffectDye == o.EffectDye
				&& EffectColor.SameAs(o.EffectColor) && EffectColor2.SameAs(o.EffectColor2)
				&& Wings == o.Wings && WingSize == o.WingSize && WingColor.SameAs(o.WingColor) && WingColor2.SameAs(o.WingColor2)
				&& Steps == o.Steps && WeaponTrail == o.WeaponTrail && WeaponTrailAll == o.WeaponTrailAll && StealthFx == o.StealthFx
				&& MotionColor.SameAs(o.MotionColor) && MotionColor2.SameAs(o.MotionColor2)
				&& Companion == o.Companion && CompanionSize == o.CompanionSize
				&& CompanionColor.SameAs(o.CompanionColor) && CompanionColor2.SameAs(o.CompanionColor2);
		}

		public void Reset() => CopyFrom(new LookSettings());

		public void Randomize()
		{
			Enabled = true;
			Aura = (AuraStyle)Main.rand.Next(1, AuraStyleCount);
			AuraDensity = Main.rand.NextFloat(0.7f, 1.4f);
			AuraSize = Main.rand.NextFloat(0.8f, 1.3f);
			AuraSpeed = Main.rand.NextFloat(0.8f, 1.3f);
			Trail = Main.rand.NextBool(3) ? TrailStyle.None : (TrailStyle)Main.rand.Next(1, TrailStyleCount);
			Ribbon = Main.rand.NextBool(3) ? RibbonStyle.None : (RibbonStyle)Main.rand.Next(1, RibbonStyleCount);
			RibbonWidth = Main.rand.NextFloat(0.7f, 1.4f);
			RibbonLength = Main.rand.NextFloat(0.7f, 1.4f);
			Echo = Main.rand.NextBool(3) ? (EchoStyle)Main.rand.Next(1, EchoStyleCount) : EchoStyle.None;
			Halo = Main.rand.NextBool(3) ? (HaloStyle)Main.rand.Next(1, HaloStyleCount) : HaloStyle.None;
			BackGlow = Main.rand.NextBool();
			GodRays = Main.rand.NextBool(4);
			GroundRing = Main.rand.NextBool(5);
			Sigil = Main.rand.NextBool(4);
			PulseRing = Main.rand.NextBool(5);
			Outline = Main.rand.NextBool(4);
			Dash = Main.rand.NextBool(3) ? DashStyle.None : (DashStyle)Main.rand.Next(1, DashStyleCount);
			EffectDye = 0;
			Wings = Main.rand.NextBool(3) ? WingStyle.None : (WingStyle)Main.rand.Next(1, WingStyleCount);
			WingSize = Main.rand.NextFloat(0.85f, 1.2f);
			Steps = Main.rand.NextBool(3) ? StepStyle.None : (StepStyle)Main.rand.Next(1, StepStyleCount);
			WeaponTrail = (WeaponTrailStyle)Main.rand.Next(1, WeaponTrailStyleCount);
			Companion = Main.rand.NextBool(3) ? CompanionStyle.None : (CompanionStyle)Main.rand.Next(1, CompanionStyleCount);
			CompanionSize = 1f;

			// Одна случайная тема на весь образ, чтобы цвета сочетались
			ColorTheme theme = ColorThemes.All[Main.rand.Next(ColorThemes.All.Length)];
			theme.ApplyToAll(this);
		}

		// ---------- сохранение ----------
		public TagCompound Save()
		{
			return new TagCompound
			{
				["v"] = 4,
				["enabled"] = Enabled,
				["aura"] = (int)Aura,
				["auraDensity"] = AuraDensity,
				["auraSize"] = AuraSize,
				["auraSpeed"] = AuraSpeed,
				["auraColor"] = AuraColor.Save(),
				["auraColor2"] = AuraColor2.Save(),
				["trail"] = (int)Trail,
				["ribbon"] = (int)Ribbon,
				["ribbonWidth"] = RibbonWidth,
				["ribbonLength"] = RibbonLength,
				["echo"] = (int)Echo,
				["trailColor"] = TrailColor.Save(),
				["trailColor2"] = TrailColor2.Save(),
				["haloStyle"] = (int)Halo,
				["backGlow"] = BackGlow,
				["godRays"] = GodRays,
				["groundRing"] = GroundRing,
				["sigil"] = Sigil,
				["pulseRing"] = PulseRing,
				["dye"] = EffectDye,
				["effectColor"] = EffectColor.Save(),
				["effectColor2"] = EffectColor2.Save(),
				["outline"] = Outline,
				["dash"] = (int)Dash,
				["wings"] = (int)Wings,
				["wingSize"] = WingSize,
				["wingColor"] = WingColor.Save(),
				["wingColor2"] = WingColor2.Save(),
				["steps"] = (int)Steps,
				["weaponTrail"] = (int)WeaponTrail,
				["weaponTrailAll"] = WeaponTrailAll,
				["stealthFx"] = StealthFx,
				["motionColor"] = MotionColor.Save(),
				["motionColor2"] = MotionColor2.Save(),
				["companion"] = (int)Companion,
				["companionSize"] = CompanionSize,
				["companionColor"] = CompanionColor.Save(),
				["companionColor2"] = CompanionColor2.Save()
			};
		}

		public static LookSettings Load(TagCompound tag)
		{
			var s = new LookSettings();
			if (tag == null)
				return s;

			if (tag.TryGet("enabled", out bool enabled)) s.Enabled = enabled;
			if (tag.TryGet("aura", out int aura)) s.Aura = (AuraStyle)Wrap(aura, AuraStyleCount);
			if (tag.TryGet("auraDensity", out float density)) s.AuraDensity = ClampMul(density);
			if (tag.TryGet("auraSize", out float size)) s.AuraSize = ClampMul(size);
			if (tag.TryGet("auraSpeed", out float speed)) s.AuraSpeed = ClampMul(speed);
			if (tag.TryGet("auraColor", out TagCompound ac)) s.AuraColor.Load(ac);
			if (tag.TryGet("auraColor2", out TagCompound ac2)) s.AuraColor2.Load(ac2);

			if (tag.TryGet("trail", out int trail)) s.Trail = (TrailStyle)Wrap(trail, TrailStyleCount);
			if (tag.TryGet("ribbon", out int ribbon)) s.Ribbon = (RibbonStyle)Wrap(ribbon, RibbonStyleCount);
			if (tag.TryGet("ribbonWidth", out float rw)) s.RibbonWidth = ClampMul(rw);
			if (tag.TryGet("ribbonLength", out float rl)) s.RibbonLength = ClampMul(rl);
			if (tag.TryGet("echo", out int echo)) s.Echo = (EchoStyle)Wrap(echo, EchoStyleCount);
			if (tag.TryGet("trailColor", out TagCompound tc)) s.TrailColor.Load(tc);
			if (tag.TryGet("trailColor2", out TagCompound tc2)) s.TrailColor2.Load(tc2);

			if (tag.TryGet("haloStyle", out int halo)) s.Halo = (HaloStyle)Wrap(halo, HaloStyleCount);
			else if (tag.TryGet("halo", out bool oldHalo) && oldHalo) s.Halo = HaloStyle.Ring; // v0.2: нимб был вкл/выкл
			if (tag.TryGet("backGlow", out bool glow)) s.BackGlow = glow;
			if (tag.TryGet("godRays", out bool rays)) s.GodRays = rays;
			if (tag.TryGet("groundRing", out bool ring)) s.GroundRing = ring;
			if (tag.TryGet("sigil", out bool sigil)) s.Sigil = sigil;
			if (tag.TryGet("pulseRing", out bool pulse)) s.PulseRing = pulse;
			if (tag.TryGet("dye", out int dye)) s.EffectDye = Wrap(dye, EffectDyes.Count);
			if (tag.TryGet("effectColor", out TagCompound ec)) s.EffectColor.Load(ec);
			if (tag.TryGet("effectColor2", out TagCompound ec2)) s.EffectColor2.Load(ec2);
			if (tag.TryGet("outline", out bool outline)) s.Outline = outline;
			if (tag.TryGet("dash", out int dash)) s.Dash = (DashStyle)Wrap(dash, DashStyleCount);
			if (tag.TryGet("wings", out int wings)) s.Wings = (WingStyle)Wrap(wings, WingStyleCount);
			if (tag.TryGet("wingSize", out float wingSize)) s.WingSize = ClampMul(wingSize);
			if (tag.TryGet("wingColor", out TagCompound wc)) s.WingColor.Load(wc);
			if (tag.TryGet("wingColor2", out TagCompound wc2)) s.WingColor2.Load(wc2);

			// v0.6: вкладка «Движение» со своими цветами. В старых образах рывок брал цвета эффектов.
			if (tag.TryGet("motionColor", out TagCompound mc)) s.MotionColor.Load(mc);
			else s.MotionColor.CopyFrom(s.EffectColor);
			if (tag.TryGet("motionColor2", out TagCompound mc2)) s.MotionColor2.Load(mc2);
			else s.MotionColor2.CopyFrom(s.EffectColor2);
			if (tag.TryGet("steps", out int steps)) s.Steps = (StepStyle)Wrap(steps, StepStyleCount);
			if (tag.TryGet("weaponTrail", out int wt)) s.WeaponTrail = (WeaponTrailStyle)Wrap(wt, WeaponTrailStyleCount);
			if (tag.TryGet("weaponTrailAll", out bool wtAll)) s.WeaponTrailAll = wtAll;
			if (tag.TryGet("stealthFx", out bool stealth)) s.StealthFx = stealth;
			if (tag.TryGet("companion", out int comp)) s.Companion = (CompanionStyle)Wrap(comp, CompanionStyleCount);
			if (tag.TryGet("companionSize", out float compSize)) s.CompanionSize = ClampMul(compSize);
			if (tag.TryGet("companionColor", out TagCompound cc)) s.CompanionColor.Load(cc);
			if (tag.TryGet("companionColor2", out TagCompound cc2)) s.CompanionColor2.Load(cc2);
			return s;
		}

		// ---------- сеть (порядок записи и чтения должен совпадать) ----------
		public void Write(BinaryWriter w)
		{
			w.Write(Enabled);
			w.Write((byte)Aura);
			w.Write(AuraDensity);
			w.Write(AuraSize);
			w.Write(AuraSpeed);
			AuraColor.Write(w);
			AuraColor2.Write(w);
			w.Write((byte)Trail);
			w.Write((byte)Ribbon);
			w.Write(RibbonWidth);
			w.Write(RibbonLength);
			w.Write((byte)Echo);
			TrailColor.Write(w);
			TrailColor2.Write(w);
			w.Write((byte)Halo);
			w.Write(BackGlow);
			w.Write(GodRays);
			w.Write(GroundRing);
			w.Write(Sigil);
			w.Write(PulseRing);
			w.Write((byte)EffectDye);
			EffectColor.Write(w);
			EffectColor2.Write(w);
			w.Write(Outline);
			w.Write((byte)Dash);
			w.Write((byte)Wings);
			w.Write(WingSize);
			WingColor.Write(w);
			WingColor2.Write(w);
			w.Write((byte)Steps);
			w.Write((byte)WeaponTrail);
			w.Write(WeaponTrailAll);
			w.Write(StealthFx);
			MotionColor.Write(w);
			MotionColor2.Write(w);
			w.Write((byte)Companion);
			w.Write(CompanionSize);
			CompanionColor.Write(w);
			CompanionColor2.Write(w);
		}

		public void Read(BinaryReader r)
		{
			Enabled = r.ReadBoolean();
			Aura = (AuraStyle)Wrap(r.ReadByte(), AuraStyleCount);
			AuraDensity = ClampMul(r.ReadSingle());
			AuraSize = ClampMul(r.ReadSingle());
			AuraSpeed = ClampMul(r.ReadSingle());
			AuraColor.Read(r);
			AuraColor2.Read(r);
			Trail = (TrailStyle)Wrap(r.ReadByte(), TrailStyleCount);
			Ribbon = (RibbonStyle)Wrap(r.ReadByte(), RibbonStyleCount);
			RibbonWidth = ClampMul(r.ReadSingle());
			RibbonLength = ClampMul(r.ReadSingle());
			Echo = (EchoStyle)Wrap(r.ReadByte(), EchoStyleCount);
			TrailColor.Read(r);
			TrailColor2.Read(r);
			Halo = (HaloStyle)Wrap(r.ReadByte(), HaloStyleCount);
			BackGlow = r.ReadBoolean();
			GodRays = r.ReadBoolean();
			GroundRing = r.ReadBoolean();
			Sigil = r.ReadBoolean();
			PulseRing = r.ReadBoolean();
			EffectDye = Wrap(r.ReadByte(), EffectDyes.Count);
			EffectColor.Read(r);
			EffectColor2.Read(r);
			Outline = r.ReadBoolean();
			Dash = (DashStyle)Wrap(r.ReadByte(), DashStyleCount);
			Wings = (WingStyle)Wrap(r.ReadByte(), WingStyleCount);
			WingSize = ClampMul(r.ReadSingle());
			WingColor.Read(r);
			WingColor2.Read(r);
			Steps = (StepStyle)Wrap(r.ReadByte(), StepStyleCount);
			WeaponTrail = (WeaponTrailStyle)Wrap(r.ReadByte(), WeaponTrailStyleCount);
			WeaponTrailAll = r.ReadBoolean();
			StealthFx = r.ReadBoolean();
			MotionColor.Read(r);
			MotionColor2.Read(r);
			Companion = (CompanionStyle)Wrap(r.ReadByte(), CompanionStyleCount);
			CompanionSize = ClampMul(r.ReadSingle());
			CompanionColor.Read(r);
			CompanionColor2.Read(r);
		}

		/// <summary>Множители (плотность, размер, скорость, ширина) живут в пределах 0.2..2.</summary>
		public const float MinMul = 0.2f;
		public const float MaxMul = 2f;

		public static float ClampMul(float v) => float.IsFinite(v) ? Math.Clamp(v, MinMul, MaxMul) : 1f;

		public static int Wrap(int v, int count) => ((v % count) + count) % count;
	}
}
