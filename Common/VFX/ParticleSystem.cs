using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using BeautifulLooks.Common.Configs;

namespace BeautifulLooks.Common.VFX
{
	/// <summary>
	/// Своя система частиц: список частиц обновляется раз в тик (после всего остального),
	/// рисуется из VfxRenderer в два слоя — позади игроков и перед ними.
	/// </summary>
	[Autoload(Side = ModSide.Client)]
	public class ParticleSystem : ModSystem
	{
		private static readonly List<Particle> Particles = new List<Particle>(1024);

		/// <summary>Сколько живых орбитальных огоньков у каждого игрока (считается при обновлении).</summary>
		public static readonly int[] OrbitCounts = new int[Main.maxPlayers + 1];

		public static int Count => Particles.Count;

		public static int Limit
		{
			get
			{
				LooksClientConfig config = ModContent.GetInstance<LooksClientConfig>();
				return config?.MaxParticles ?? 2500;
			}
		}

		/// <summary>Добавляет частицу (если не превышен лимит). Возвращает её же для настройки.</summary>
		public static T Spawn<T>(T particle) where T : Particle
		{
			if (Main.dedServ || particle == null || Particles.Count >= Limit)
				return particle;
			Particles.Add(particle);
			return particle;
		}

		public override void PostUpdateEverything()
		{
			Array.Clear(OrbitCounts);
			for (int i = 0; i < Particles.Count; i++)
			{
				Particle p = Particles[i];
				p.Time++;
				try
				{
					p.Update();
				}
				catch (Exception e)
				{
					p.Dead = true;
					VfxRenderer.LogOnce("particle update", e);
				}

				if (p.Dead || p.Time >= p.Lifetime)
				{
					// быстрое удаление: на место мёртвой ставим последнюю
					int last = Particles.Count - 1;
					Particles[i] = Particles[last];
					Particles.RemoveAt(last);
					i--;
				}
			}
		}

		public override void OnWorldUnload() => Particles.Clear();

		public override void Unload() => Particles.Clear();

		private static bool OnScreen(Particle p)
		{
			float r = p.CullRadius;
			Vector2 s = p.Position - Main.screenPosition;
			return s.X > -r && s.Y > -r && s.X < Main.screenWidth + r && s.Y < Main.screenHeight + r;
		}

		/// <summary>Хвосты-ленты частиц нужного слоя (вызывается вне SpriteBatch).</summary>
		public static void DrawTrails(bool front)
		{
			for (int i = 0; i < Particles.Count; i++)
			{
				if (Particles[i] is TrailParticle t && t.Front == front && OnScreen(t))
					t.DrawTrail();
			}
		}

		/// <summary>Сами частицы нужного слоя (в активный SpriteBatch).</summary>
		public static void DrawParticles(SpriteBatch sb, bool front)
		{
			for (int i = 0; i < Particles.Count; i++)
			{
				Particle p = Particles[i];
				if (p.Front == front && OnScreen(p))
					p.Draw(sb);
			}
		}
	}
}
