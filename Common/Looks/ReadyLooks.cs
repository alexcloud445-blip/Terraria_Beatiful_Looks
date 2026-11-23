using System;
using Microsoft.Xna.Framework;

namespace Terraria_Beatiful_Looks.Common.Looks
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

		public static readonly Entry[] All =
		{
			new Entry("Seraph", new Color(255, 214, 110), l =>
			{
				Colors(l, new Color(255, 214, 110), new Color(255, 150, 90));
				l.Aura = AuraStyle.Divine;
				l.Halo = HaloStyle.Radiant;
				l.GodRays = true;
				l.BackGlow = true;
				l.Ribbon = RibbonStyle.Soft;
				l.Trail = TrailStyle.Feathers;
				l.Wings = WingStyle.Seraph;
			}),
			new Entry("Inferno", new Color(255, 110, 30), l =>
			{
				Colors(l, new Color(255, 160, 40), new Color(235, 40, 40));
				l.Aura = AuraStyle.Flames;
				l.AuraDensity = 1.3f;
				l.Halo = HaloStyle.Thorns;
				l.Ribbon = RibbonStyle.Flame;
				l.Trail = TrailStyle.Flames;
				l.PulseRing = true;
				l.EffectDye = 5; // Living Flame
				l.Wings = WingStyle.Flame;
				l.Outline = true;
			}),
			new Entry("Frost", new Color(110, 205, 255), l =>
			{
				Colors(l, new Color(120, 210, 255), new Color(130, 130, 255));
				l.Aura = AuraStyle.Frost;
				l.Halo = HaloStyle.Starry;
				l.Ribbon = RibbonStyle.Soft;
				l.Trail = TrailStyle.Snow;
				l.Echo = EchoStyle.Ghost;
				l.GroundRing = true;
				l.Wings = WingStyle.Crystal;
			}),
			new Entry("Sakura", new Color(255, 140, 200), l =>
			{
				Colors(l, new Color(255, 140, 200), new Color(190, 140, 255));
				l.Aura = AuraStyle.Sakura;
				l.AuraDensity = 1.2f;
				l.Trail = TrailStyle.Petals;
				l.Ribbon = RibbonStyle.Twin;
				l.BackGlow = true;
				l.Wings = WingStyle.Butterfly;
			}),
			new Entry("Void", new Color(150, 70, 255), l =>
			{
				Colors(l, new Color(150, 70, 255), new Color(255, 80, 200));
				l.Aura = AuraStyle.Void;
				l.Sigil = true;
				l.Echo = EchoStyle.Phantom;
				l.Trail = TrailStyle.Smoke;
				l.Halo = HaloStyle.Double;
				l.Wings = WingStyle.Demon;
				l.Outline = true;
			}),
			new Entry("Empress", new Color(255, 120, 220), l =>
			{
				Colors(l, new Color(255, 120, 220), new Color(130, 230, 255), rainbow: true);
				l.Aura = AuraStyle.Prism;
				l.Ribbon = RibbonStyle.Empress;
				l.Trail = TrailStyle.Prism;
				l.Halo = HaloStyle.Radiant;
				l.BackGlow = true;
				l.EffectDye = 3; // Living Rainbow
				l.Wings = WingStyle.Light;
			}),
			new Entry("Storm", new Color(130, 165, 255), l =>
			{
				Colors(l, new Color(130, 170, 255), new Color(190, 120, 255));
				l.Aura = AuraStyle.Storm;
				l.Echo = EchoStyle.Afterimage;
				l.Ribbon = RibbonStyle.Soft;
				l.RibbonWidth = 0.7f;
				l.PulseRing = true;
				l.Wings = WingStyle.Spirit;
				l.Outline = true;
			}),
			new Entry("Celestial", new Color(120, 150, 255), l =>
			{
				Colors(l, new Color(120, 150, 255), new Color(255, 220, 150));
				l.Aura = AuraStyle.CelestialRing;
				l.Sigil = true;
				l.Halo = HaloStyle.Ring;
				l.Trail = TrailStyle.Stars;
				l.Ribbon = RibbonStyle.Rainbow;
				l.Wings = WingStyle.Constellation;
			}),
			new Entry("Fairy", new Color(120, 255, 180), l =>
			{
				Colors(l, new Color(120, 255, 180), new Color(255, 240, 140));
				l.Aura = AuraStyle.Butterflies;
				l.Trail = TrailStyle.Sparkles;
				l.Halo = HaloStyle.Starry;
				l.BackGlow = true;
				l.Wings = WingStyle.Butterfly;
			}),
			new Entry("Comet", new Color(80, 230, 255), l =>
			{
				Colors(l, new Color(80, 230, 255), new Color(160, 110, 255));
				l.Aura = AuraStyle.Comets;
				l.Ribbon = RibbonStyle.Rainbow;
				l.Echo = EchoStyle.Ghost;
				l.GroundRing = true;
				l.Wings = WingStyle.Spirit;
			})
		};
	}
}
