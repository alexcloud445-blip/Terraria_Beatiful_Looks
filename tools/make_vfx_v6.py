"""Текстуры v0.6: северное сияние, галактика, корона затмения, диск, мандала, туманность, лисица-дух.

Запуск: python3 tools/make_vfx_v6.py  (или вместе со всеми: python3 tools/make_vfx.py)
Как и остальные текстуры эффектов — белые, premultiplied (см. Canvas.save в make_vfx.py).
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_vfx as mv  # noqa: E402
from make_vfx import Canvas, clamp, smooth, seg_dist, in_poly  # noqa: E402
from make_vfx_shapes import blur_alpha  # noqa: E402


# ======================= северное сияние =======================
def aurora_strip():
    """AuroraStrip 32x128: одна «складка» занавеса сияния. Основание (яркая кромка) внизу по центру,
    вверх свет плавно гаснет. Рисуется рядом много раз с перекрытием — получается сплошной занавес."""
    c = Canvas(32, 128)

    def fn(x, y):
        u = (x - 16) / 16
        t = 1 - y / 128  # 0 внизу, 1 вверху
        across = math.exp(-(u / 0.62) ** 2) * smooth(1.0, 0.85, abs(u))
        edge = math.exp(-((t - 0.08) / 0.05) ** 2)            # светлая нижняя кромка
        body = (1 - t) ** 1.6 * smooth(0.03, 0.16, t) * 0.6   # мягкий свет вверх
        a = across * (edge * 0.85 + body) * smooth(0.0, 0.04, t)
        light = 0.7 + 0.3 * clamp(edge * 1.4)
        return (clamp(a), light)

    c.paint(fn, ss=2)
    c.save("AuroraStrip")


# ======================= галактика =======================
GAL_FRAMES = 32
GAL_W, GAL_H = 160, 64
GAL_RX, GAL_RY = 76.0, 76.0 * 0.4


def galaxy():
    """Galaxy 1280x256: 32 кадра 160x64 (сетка 8x4) спирального диска в перспективе, каждый следующий
    повёрнут на 1/32 оборота. Верхняя половина кадра — дальняя сторона диска (рисуется позади
    персонажа), нижняя — ближняя (перед ним). Кадры плавно смешиваются в коде."""
    rnd = random.Random(61)
    noise = mv.value_noise(611, 32)
    noise2 = mv.value_noise(612, 32)
    stars = []
    for _ in range(170):
        r = rnd.random() ** 0.7 * 0.97
        phi = rnd.random() * 2 * math.pi
        b = 0.35 + 0.65 * rnd.random() ** 2
        stars.append((r, phi, b))

    c = Canvas(GAL_W * 8, GAL_H * 4)
    for f in range(GAL_FRAMES):
        ox, oy = (f % 8) * GAL_W, (f // 8) * GAL_H
        rot = f * 2 * math.pi / GAL_FRAMES
        cx, cy = ox + GAL_W / 2, oy + GAL_H / 2
        # звёзды этого кадра (в перспективе)
        fs = []
        for r, phi, b in stars:
            a = phi + rot
            fs.append((cx + math.cos(a) * r * GAL_RX, cy + math.sin(a) * r * GAL_RY, b))

        def fn(x, y, cx=cx, cy=cy, rot=rot, fs=fs):
            u = (x - cx) / GAL_RX
            v = (y - cy) / GAL_RY
            r = math.sqrt(u * u + v * v)
            if r > 1.0:
                return 0.0
            th = math.atan2(v, u) - rot
            # две спиральные ветви (логарифмическая спираль)
            arm = 0.5 + 0.5 * math.cos(2 * (th - 2.4 * math.log(r + 0.06)))
            arm = arm ** 3
            # клочья пыли: шум в координатах, которые вращаются вместе с диском
            nx, ny = r * math.cos(th) * 5 + 16, r * math.sin(th) * 5 + 16
            clump = 0.55 * noise(nx, ny) + 0.45 * noise2(nx * 2.1, ny * 2.1)
            dust = arm * (0.35 + 0.9 * clump) * smooth(1.0, 0.25, r) * smooth(0.04, 0.2, r)
            disc = 0.16 * math.exp(-r * r * 3.0)
            core = math.exp(-r * r * 60) * 1.1 + 0.35 * math.exp(-r * r * 12)
            a = dust * 0.75 + disc + core
            light = 0.62 + 0.38 * clamp(core * 2)
            # звёзды
            for sx, sy, b in fs:
                dx, dy = x - sx, y - sy
                d2 = dx * dx + dy * dy
                if d2 < 9:
                    s = b * math.exp(-d2 / 0.9)
                    if s > 0.02:
                        a = max(a, s)
                        light = max(light, 0.95)
            return (clamp(a), light)

        c.paint(fn, ox, oy, ox + GAL_W, oy + GAL_H, ss=1)
    c.save("Galaxy")


# ======================= затмение =======================
def corona():
    """Corona 128x128: корона затмения — яркий ободок (r = 0.36) и неровные лучи наружу.
    Внутри пусто: середину закрывает тёмный диск (Disc)."""
    c = Canvas(128, 128)
    rays = mv.value_noise(901, 64)
    rays2 = mv.value_noise(902, 64)
    r0 = 0.36

    def fn(x, y):
        u, v = (x - 64) / 64, (y - 64) / 64
        r = math.sqrt(u * u + v * v)
        if r >= 1 or r < r0 - 0.06:
            return 0.0
        ang = (math.atan2(v, u) / (2 * math.pi)) % 1.0
        ray = rays(ang * 48, 0.5) ** 2.2 * 1.3 + 0.5 * rays2(ang * 96, 3.5) ** 3
        out = max(0.0, r - r0)
        a = math.exp(-((r - r0) / 0.025) ** 2) * 1.0          # ободок
        a += 0.55 * math.exp(-out / 0.1) * smooth(r0 - 0.04, r0, r)  # мягкое свечение
        a += ray * math.exp(-out / (0.16 + 0.2 * ray)) * smooth(r0 - 0.02, r0 + 0.04, r)
        a *= smooth(1.0, 0.85, r)
        light = 0.7 + 0.3 * math.exp(-((r - r0) / 0.05) ** 2)
        return (clamp(a), light)

    c.paint(fn, ss=2)
    c.save("Corona")


def disc():
    """Disc 64x64: залитый круг радиусом 30 с гладким краем (тёмный диск затмения)."""
    c = Canvas(64, 64)

    def fn(x, y):
        d = math.hypot(x - 32, y - 32)
        return clamp(30.5 - d)

    c.paint(fn, ss=3)
    c.save("Disc")


# ======================= мандала =======================
def _ring(r, rr, thick):
    return clamp(thick / 2 + 0.5 - abs(r - rr))


def mandala_outer():
    """MandalaOuter 256x256: двойное кольцо, 16 заострённых лепестков, точки и бусины."""
    c = Canvas(256, 256)
    s = 128.0
    petals = 16

    def fn(x, y):
        dx, dy = x - 128, y - 128
        rp = math.hypot(dx, dy)
        r = rp / s
        if r > 1:
            return 0.0
        ang = math.atan2(dy, dx)
        a = _ring(rp, 0.97 * s, 2.6) + _ring(rp, 0.92 * s, 1.6) + 0.8 * _ring(rp, 0.6 * s, 1.8)
        # лепестки: контур дуги между r 0.6 и 0.9
        k = (ang / (2 * math.pi) * petals) % 1.0
        w = abs(k - 0.5) * 2  # 0 в середине лепестка, 1 на стыке
        tip = 0.6 + 0.3 * (1 - w ** 1.6)
        a += _ring(rp, tip * s, 2.0) * smooth(0.0, 0.08, 1 - w)
        # внутренняя жилка лепестка
        if abs(k - 0.5) * 2 * math.pi * rp / petals < 0.9 and 0.64 * s < rp < (tip - 0.04) * s:
            a += 0.55
        # бусины между кольцами
        kb = (ang / (2 * math.pi) * petals * 2 + 0.5) % 1.0
        bead = math.hypot((kb - 0.5) * 2 * math.pi * 0.945 * s / (petals * 2), rp - 0.945 * s)
        a += clamp(2.2 - bead)
        # мелкие насечки по внешнему кольцу
        kt = (ang / (2 * math.pi) * 96) % 1.0
        if abs(kt - 0.5) < 0.12 and 0.985 * s < rp < 1.0 * s:
            a += 0.8
        return clamp(a)

    c.paint(fn, ss=2)
    c.save("MandalaOuter")


def mandala_inner():
    """MandalaInner 256x256: восьмиконечная звезда из двух квадратов, кольца, 12 малых лепестков и цветок в центре."""
    c = Canvas(256, 256)
    s = 128.0
    sq = []
    for k in range(2):
        pts = []
        for i in range(4):
            a = k * math.pi / 4 + i * math.pi / 2 + math.pi / 4
            pts.append((128 + math.cos(a) * 0.78 * s, 128 + math.sin(a) * 0.78 * s))
        sq.append(pts)
    segs = []
    for pts in sq:
        for i in range(4):
            segs.append((*pts[i], *pts[(i + 1) % 4]))
    # лучи от центра к вершинам звезды
    for i in range(8):
        a = i * math.pi / 4
        segs.append((128 + math.cos(a) * 0.36 * s, 128 + math.sin(a) * 0.36 * s,
                     128 + math.cos(a) * 0.56 * s, 128 + math.sin(a) * 0.56 * s))

    def fn(x, y):
        dx, dy = x - 128, y - 128
        rp = math.hypot(dx, dy)
        if rp > s:
            return 0.0
        ang = math.atan2(dy, dx)
        a = 0.0
        d = min(seg_dist(x, y, *sg) for sg in segs)
        a += clamp(1.6 - d)
        a += _ring(rp, 0.58 * s, 2.0) + 0.9 * _ring(rp, 0.34 * s, 1.8) + 0.7 * _ring(rp, 0.82 * s, 1.4)
        # 12 малых лепестков между r 0.12 и 0.32
        k = (ang / (2 * math.pi) * 12) % 1.0
        w = abs(k - 0.5) * 2
        tip = 0.14 + 0.18 * (1 - w ** 1.4)
        a += _ring(rp, tip * s, 1.8) * smooth(0.0, 0.1, 1 - w)
        a += clamp(4.5 - rp) * 0.9  # точка в центре
        return clamp(a)

    c.paint(fn, ss=2)
    c.save("MandalaInner")


# ======================= туманность =======================
NEB_ANCHOR = (6, 40)


def nebula_shape(x, y):
    """Силуэт крыла-перепонки: веер от точки крепления с тремя округлыми лопастями по краю.
    Возвращает (внутри ли, насколько близко к краю 0..1)."""
    ax, ay = NEB_ANCHOR
    dx, dy = x - ax, y - ay
    r = math.hypot(dx, dy)
    phi = math.atan2(dy, dx)  # 0 — вправо, + — вниз
    lo, hi = -0.5, 1.35
    if phi < lo or phi > hi or r < 1:
        return False, 0.0
    t = (phi - lo) / (hi - lo)
    # длинная верхняя лопасть, к низу короче; по краю три округлых выреза-«фестона»
    base = 80 - 38 * t ** 1.25
    scallop = 7 * (1 - abs(math.sin(t * math.pi * 3)) ** 0.7)
    edge_r = (base - scallop) * smooth(0.0, 0.07, t) ** 0.35 * smooth(1.0, 0.9, t) ** 0.4
    if r > edge_r:
        return False, 0.0
    return True, r / max(edge_r, 1)


def nebula_wing():
    """NebulaWing 96x88 (облачная полупрозрачная перепонка), NebulaRim (светлая кромка и жилки),
    NebulaStarsA/B (два набора звёзд для мерцания). Крепление в (6, 40)."""
    n1 = mv.value_noise(1201, 24)
    n2 = mv.value_noise(1202, 24)
    mem = Canvas(96, 88)
    rim = Canvas(96, 88)

    def f_mem(x, y):
        inside, k = nebula_shape(x, y)
        if not inside:
            return 0.0
        cloud = 0.55 * n1(x / 9, y / 9) + 0.45 * n2(x / 4.5, y / 4.5)
        a = (0.3 + 0.7 * cloud ** 1.5) * (0.55 + 0.45 * k) * smooth(1.0, 0.92, k)
        return (clamp(a), 0.65 + 0.35 * cloud)

    def f_rim(x, y):
        inside, k = nebula_shape(x, y)
        if not inside:
            return 0.0
        a = smooth(0.86, 0.99, k) * smooth(1.0, 0.985, k) * 1.2
        # жилки от крепления
        ax, ay = NEB_ANCHOR
        phi = math.atan2(y - ay, x - ax)
        for vein in (-0.2, 0.25, 0.7, 1.1):
            d = abs(phi - vein) * math.hypot(x - ax, y - ay)
            a = max(a, clamp(0.9 - d * 0.9) * 0.55 * smooth(0.95, 0.4, k))
        return clamp(a)

    mem.paint(f_mem, ss=2)
    rim.paint(f_rim, ss=3)
    mem.save("NebulaWing")
    rim.save("NebulaRim")

    rnd = random.Random(77)
    for name in ("NebulaStarsA", "NebulaStarsB"):
        pts = []
        while len(pts) < 16:
            x, y = rnd.uniform(8, 92), rnd.uniform(2, 86)
            inside, k = nebula_shape(x, y)
            if inside and k < 0.9:
                pts.append((x, y, rnd.uniform(0.6, 1.0), rnd.random() < 0.3))
        st = Canvas(96, 88)

        def f_st(x, y, pts=pts):
            a = 0.0
            for sx, sy, b, cross in pts:
                dx, dy = x - sx, y - sy
                d2 = dx * dx + dy * dy
                a = max(a, b * math.exp(-d2 / 0.8))
                if cross and d2 < 16:
                    a = max(a, 0.55 * b * (math.exp(-(dy / 0.45) ** 2) * clamp(1 - abs(dx) / 4) +
                                         math.exp(-(dx / 0.45) ** 2) * clamp(1 - abs(dy) / 4)))
            return clamp(a)

        st.paint(f_st, ss=2)
        st.save(name)


# ======================= лисица-дух =======================
def fox():
    """Fox 64x48: лисица-дух в прыжке-полёте (передние лапы вытянуты вперёд, задние назад), смотрит вправо.
    Хвосты рисуются отдельно (кодом) от точки (14, 23). FoxGlow — свечение силуэта."""
    c = Canvas(64, 48)
    snout = [(50.5, 11.0), (61.5, 16.0), (50.5, 19.5)]
    ear1 = [(42.0, 10.0), (43.8, 0.8), (47.8, 8.5)]
    ear2 = [(46.5, 8.5), (51.2, 1.2), (52.2, 10.0)]
    ruff = [(42.5, 15.0), (49.5, 17.5), (46.0, 23.5), (41.0, 22.0)]
    # (откуда, куда, толщина у основания)
    legs = [(39.5, 26.5, 51.0, 32.5, 2.6), (36.5, 27.5, 47.0, 35.0, 2.4),
            (21.0, 27.5, 8.5, 32.0, 2.8), (24.5, 28.5, 12.0, 36.0, 2.5)]

    def ell(x, y, cx, cy, rx, ry, ang=0.0):
        ca, sa = math.cos(ang), math.sin(ang)
        dx, dy = x - cx, y - cy
        u = (dx * ca + dy * sa) / rx
        v = (-dx * sa + dy * ca) / ry
        return u * u + v * v

    def fn(x, y):
        body = ell(x, y, 28, 24.5, 15.5, 7.2, -0.1) <= 1
        chest = ell(x, y, 40, 20.5, 7.2, 7.8) <= 1
        head = ell(x, y, 47, 13.5, 6.6, 6.0) <= 1
        hip = ell(x, y, 17.5, 23.5, 5.2, 5.0) <= 1
        inside = body or chest or head or hip or in_poly(x, y, snout) or in_poly(x, y, ear1) or in_poly(x, y, ear2) or in_poly(x, y, ruff)
        leg = False
        for ax, ay, bx, by, w in legs:
            ll = (bx - ax) ** 2 + (by - ay) ** 2
            t = clamp(((x - ax) * (bx - ax) + (y - ay) * (by - ay)) / ll)
            if seg_dist(x, y, ax, ay, bx, by) < w * (1 - 0.55 * t) + 0.3:
                leg = True
        if not inside and not leg:
            return 0.0
        if math.hypot(x - 49.3, y - 12.2) < 1.25:
            return 0.0  # глаз — дырочка (в коде там блик)
        # светотень: спина темнее, грудь, щёки, морда и брюшко светлые
        light = 0.68 + 0.2 * clamp((y - 16) / 14)
        q = ell(x, y, 41, 22, 6.5, 7.5)
        if q < 1:
            light = max(light, 0.8 + 0.12 * (1 - q))  # светлая грудка, мягко к краю
        if in_poly(x, y, ruff) or ell(x, y, 52, 16, 5, 2.6) <= 1:
            light = 0.9
        if in_poly(x, y, [(43.6, 8.5), (44.3, 3.5), (46.4, 8.2)]) or in_poly(x, y, [(48.4, 8.4), (50.7, 4.0), (51.0, 9.4)]):
            light = 0.5  # внутри ушей
        if leg and not inside:
            light = 0.62
        if math.hypot(x - 61, y - 16) < 1.6:
            light = 0.45  # нос
        return (1.0, light)

    c.paint(fn, ss=3)
    c.save("Fox")
    blur_alpha(c, 2, gain=1.6).save("FoxGlow")


def main():
    random.seed(6)
    aurora_strip()
    galaxy()
    corona()
    disc()
    mandala_outer()
    mandala_inner()
    nebula_wing()
    fox()


if __name__ == "__main__":
    main()
