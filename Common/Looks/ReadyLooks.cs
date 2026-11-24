using System;
using Microsoft.Xna.Framework;

namespace BeautifulLooks.Common.Looks
{
	/// <summary>
	/// Готовые образы для вкладки «Образы». Название и описание: UI.Look.Key и UI.Look.KeyDesc.
	/// Каждый образ начинает с чистого листа (Reset), поэтому старые настройки не смешиваются.
	/// </summary>
	public static class ReadyLooks
	{
		public class Entry
		{
			public readonly string Key;
			public readonly Color Icon;
			private readonly Action<LookSettings> _apply;

			public Entry(string key, Color icon, Action<LookSettings> apply)
			{
				Key = key;
				Icon = icon;
				_apply = apply;
			}

			public void Apply(LookSettings look)
			{
				look.Reset();
				_apply(look);
			}
		}

		private static void Colors(LookSettings l, Color a, Color b, bool rainbow = false)
		{
			var theme = new ColorTheme("", a, b, rainbow);
			theme.ApplyToAll(l);
		}

		// В каждом образе 4–6 эффектов, которые дополняют друг друга: больше — и всё сливается в пятно.
		// След оружия, шаги и спутник почти не перекрывают персонажа, поэтому их можно добавлять смелее.
		public static readonly Entry[] All =
		{
			new Entry("Seraph", new Color(255, 214, 110), l =>
			{
				Colors(l, new Color(255, 214, 110), new Color(255, 150, 90));
				l.Aura = AuraStyle.Divine;
				l.AuraDensity = 0.8f;
				l.Halo = HaloStyle.Radiant;
				l.GodRays = true;
				l.Wings = WingStyle.Seraph;
				l.Ribbon = RibbonStyle.Soft;
				l.RibbonWidth = 0.8f;
				l.Dash = DashStyle.Flash;
				l.WeaponTrail = WeaponTrailStyle.Soft;
			}),
			new Entry("Inferno", new Color(255, 160, 40), l =>
			{
				Colors(l, new Color(255, 160, 40), new Color(235, 40, 40));
				l.Aura = AuraStyle.Flames;
				l.Halo = HaloStyle.Thorns;
				l.Wings = WingStyle.Flame;
				l.Ribbon = RibbonStyle.Flame;
				l.Dash = DashStyle.Flame;
				l.WeaponTrail = WeaponTrailStyle.Flame;
				l.Steps = StepStyle.Embers;
			}),
			new Entry("Frost", new Color(120, 210, 255), l =>
			{
				Colors(l, new Color(120, 210, 255), new Color(130, 130, 255));
				l.Aura = AuraStyle.Frost;
				l.AuraDensity = 0.8f;
				l.Halo = HaloStyle.Starry;
				l.Wings = WingStyle.Crystal;
				l.Echo = EchoStyle.Ghost;
				l.Dash = DashStyle.Flash;
				l.WeaponTrail = WeaponTrailStyle.Sparkle;
				l.Steps = StepStyle.Frost;
			}),
			new Entry("Sakura", new Color(255, 140, 200), l =>
			{
				Colors(l, new Color(255, 140, 200), new Color(190, 140, 255));
				l.Aura = AuraStyle.Sakura;
				l.Wings = WingStyle.Butterfly;
				l.Ribbon = RibbonStyle.Twin;
				l.RibbonWidth = 0.8f;
				l.Dash = DashStyle.Sakura;
				l.WeaponTrail = WeaponTrailStyle.Sparkle;
				l.Steps = StepStyle.Flowers;
			}),
			new Entry("Void", new Color(150, 70, 255), l =>
			{
				Colors(l, new Color(150, 70, 255), new Color(255, 80, 200));
				l.Aura = AuraStyle.Void;
				l.AuraDensity = 0.8f;
				l.Wings = WingStyle.Demon;
				l.Sigil = true;
				l.Echo = EchoStyle.Phantom;
				l.Outline = true;
				l.Dash = DashStyle.Void;
				l.WeaponTrail = WeaponTrailStyle.Shadow;
			}),
			new Entry("Empress", new Color(255, 120, 220), l =>
			{
				Colors(l, new Color(255, 150, 220), new Color(140, 220, 255), rainbow: true);
				l.Aura = AuraStyle.Prism;
				l.AuraDensity = 0.8f;
				l.Wings = WingStyle.Light;
				l.Halo = HaloStyle.Radiant;
				l.Ribbon = RibbonStyle.Empress;
				l.RibbonWidth = 0.8f;
				l.Dash = DashStyle.Prism;
				l.WeaponTrail = WeaponTrailStyle.Prism;
			}),
			new Entry("Storm", new Color(130, 170, 255), l =>
			{
				Colors(l, new Color(130, 170, 255), new Color(190, 120, 255));
				l.Aura = AuraStyle.Storm;
				l.Wings = WingStyle.Spirit;
				l.Echo = EchoStyle.Afterimage;
				l.Outline = true;
				l.Dash = DashStyle.Thunder;
				l.WeaponTrail = WeaponTrailStyle.Sparkle;
			}),
			new Entry("Celestial", new Color(120, 150, 255), l =>
			{
				Colors(l, new Color(120, 150, 255), new Color(255, 220, 150));
				l.Aura = AuraStyle.CelestialRing;
				l.Wings = WingStyle.Constellation;
				l.Halo = HaloStyle.Ring;
				l.Sigil = true;
				l.Dash = DashStyle.Prism;
				l.WeaponTrail = WeaponTrailStyle.Stardust;
			}),
			new Entry("Fairy", new Color(120, 255, 180), l =>
			{
				Colors(l, new Color(120, 255, 180), new Color(255, 240, 140));
				l.Aura = AuraStyle.Butterflies;
				l.Wings = WingStyle.Spirit;
				l.Halo = HaloStyle.Starry;
				l.Trail = TrailStyle.Sparkles;
				l.Dash = DashStyle.Sakura;
				l.WeaponTrail = WeaponTrailStyle.Sparkle;
				l.Companion = CompanionStyle.Wisp;
			}),
			new Entry("Comet", new Color(80, 230, 255), l =>
			{
				Colors(l, new Color(80, 230, 255), new Color(160, 110, 255));
				l.Aura = AuraStyle.Comets;
				l.Ribbon = RibbonStyle.Rainbow;
				l.RibbonWidth = 0.8f;
				l.Echo = EchoStyle.Ghost;
				l.Dash = DashStyle.Flash;
				l.WeaponTrail = WeaponTrailStyle.Stardust;
			}),

			// ---------- v0.6 ----------
			new Entry("Astral", new Color(255, 205, 110), l =>
			{
				Colors(l, new Color(255, 205, 110), new Color(120, 170, 255));
				l.Aura = AuraStyle.Astrolabe;
				l.Halo = HaloStyle.Diadem;
				l.Wings = WingStyle.Constellation;
				l.Companion = CompanionStyle.Wisp;
				l.Steps = StepStyle.Stardust;
				l.WeaponTrail = WeaponTrailStyle.Stardust;
				l.Dash = DashStyle.Prism;
			}),
			new Entry("Phoenix", new Color(255, 170, 60), l =>
			{
				Colors(l, new Color(255, 170, 60), new Color(235, 50, 40));
				l.Aura = AuraStyle.Glow;
				l.AuraDensity = 0.7f;
				l.Halo = HaloStyle.Radiant;
				l.Wings = WingStyle.Phoenix;
				l.Steps = StepStyle.Embers;
				l.WeaponTrail = WeaponTrailStyle.Flame;
				l.Dash = DashStyle.Flame;
			}),
			new Entry("Aurora", new Color(90, 255, 170), l =>
			{
				Colors(l, new Color(90, 255, 170), new Color(170, 100, 255));
				l.Aura = AuraStyle.Aurora;
				l.Wings = WingStyle.Aurora;
				l.Companion = CompanionStyle.Fox;
				l.Steps = StepStyle.Frost;
				l.WeaponTrail = WeaponTrailStyle.Soft;
				l.Dash = DashStyle.Flash;
			}),
			new Entry("Galaxy", new Color(150, 170, 255), l =>
			{
				Colors(l, new Color(150, 170, 255), new Color(255, 120, 200));
				l.Aura = AuraStyle.Galaxy;
				l.Wings = WingStyle.Nebula;
				l.Halo = HaloStyle.Starry;
				l.Companion = CompanionStyle.Serpent;
				l.Steps = StepStyle.Stardust;
				l.WeaponTrail = WeaponTrailStyle.Stardust;
				l.Dash = DashStyle.Void;
			}),
			new Entry("Eclipse", new Color(255, 190, 90), l =>
			{
				Colors(l, new Color(255, 190, 90), new Color(255, 80, 60));
				l.Aura = AuraStyle.Eclipse;
				l.Halo = HaloStyle.Horns;
				l.Wings = WingStyle.Demon;
				l.Steps = StepStyle.Shadow;
				l.WeaponTrail = WeaponTrailStyle.Shadow;
				l.Dash = DashStyle.Shadow;
			}),
			new Entry("Lotus", new Color(255, 170, 215), l =>
			{
				Colors(l, new Color(255, 170, 215), new Color(120, 230, 150));
				l.Aura = AuraStyle.Mandala;
				l.Wings = WingStyle.Floral;
				l.Halo = HaloStyle.RuneCrown;
				l.Companion = CompanionStyle.Butterfly;
				l.Steps = StepStyle.Flowers;
				l.WeaponTrail = WeaponTrailStyle.Sparkle;
				l.Dash = DashStyle.Sakura;
			})
		};
	}
}
