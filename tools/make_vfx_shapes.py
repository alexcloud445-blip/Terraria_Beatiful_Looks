"""Текстуры v0.4: атлас форм (заливка + свечение), детали крыльев, лучи и блики.

Запускается из make_vfx.py (или сам по себе: python3 tools/make_vfx_shapes.py).
У каждой формы две текстуры: заливка с серой светотенью (красится основным цветом)
и размытое свечение (рисуется светом вторым цветом) — так получается градиент
из двух цветов без шейдеров.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_vfx as mv  # noqa: E402
from make_vfx import Canvas, clamp, smooth, seg_dist, in_poly  # noqa: E402


# ======================= размытие (свечение формы) =======================
def blur_alpha(src, radius, passes=3, gain=1.6):
    """Свечение = размытая альфа заливки (3 прохода box blur ≈ гаусс)."""
    w, h = src.w, src.h
    a = list(src.a)
    for _ in range(passes):
        # по X
        out = [0.0] * (w * h)
        for y in range(h):
            row = y * w
            acc = 0.0
            for x in range(-radius, radius + 1):
                if 0 <= x < w:
                    acc += a[row + x]
            for x in range(w):
                out[row + x] = acc / (2 * radius + 1)
                xo, xi = x - radius, x + radius + 1
                if 0 <= xo < w:
                    acc -= a[row + xo]
                if 0 <= xi < w:
                    acc += a[row + xi]
        a = out
        # по Y
        out = [0.0] * (w * h)
        for x in range(w):
            acc = 0.0
            for y in range(-radius, radius + 1):
                if 0 <= y < h:
                    acc += a[y * w + x]
            for y in range(h):
                out[y * w + x] = acc / (2 * radius + 1)
                yo, yi = y - radius, y + radius + 1
                if 0 <= yo < h:
                    acc -= a[yo * w + x]
                if 0 <= yi < h:
                    acc += a[yi * w + x]
        a = out
    glow = Canvas(w, h)
    glow.a = [clamp(v * gain) for v in a]
    return glow


# ======================= формы (u, v в -1..1, вниз = +v) =======================
def facet_light(u, v, n, offset=0.0):
    """Светотень гранями: соседние сектора чередуют яркость (как у кристалла)."""
    ang = math.atan2(v, u) + offset
    k = int(((ang / (2 * math.pi)) % 1.0) * n)
    return 1.0 if k % 2 == 0 else 0.72


def star_poly(n, r_out, r_in, rot=-math.pi / 2):
    pts = []
    for i in range(n * 2):
        a = rot + i * math.pi / n
        r = r_out if i % 2 == 0 else r_in
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts


def s_heart(u, v):
    x, y = u * 1.12, -(v * 1.12) + 0.18
    q = (x * x + y * y - 1) ** 3 - x * x * y ** 3
    if q > 0:
        return 0.0
    light = 0.74 + 0.2 * clamp(0.5 - v)
    if q > -0.02:
        light = 0.55
    if (u + 0.42) ** 2 + (v + 0.35) ** 2 < 0.04:
        light = 1.0
    return (1.0, light)


def s_petal(u, v):
    x, y = u / 0.78, v / 0.95
    width = math.sqrt(max(0.0, 1 - y * y)) * (0.62 + 0.38 * (1 - y) / 2)
    if abs(x) > width or abs(y) > 1:
        return 0.0
    if y < -0.55 and abs(x) < (-0.55 - y) * 0.9:
        return 0.0
    light = 0.72 + 0.28 * (1 - abs(x) / max(width, 0.01)) - 0.15 * clamp(y)
    if abs(x) < 0.04 and y > -0.3:
        light -= 0.12
    return (1.0, clamp(light))


def s_petal_round(u, v):
    x, y = u / 0.7, (v + 0.05) / 0.92
    if x * x + y * y > 1:
        return 0.0
    if y > 0.6 and abs(x) > (1 - y) * 1.4:
        return 0.0
    light = 0.62 + 0.38 * (1 - abs(x + 0.35))
    return (1.0, clamp(light))


def s_leaf(u, v):
    x, y = u, v / 0.95
    w = (1 - y * y) * 0.48
    if abs(y) > 1 or abs(x) > w:
        return 0.0
    light = 0.8 + 0.2 * (1 if x < 0 else 0.6)
    if abs(x) < 0.035:
        return (1.0, 0.55)
    for k in (-0.5, -0.1, 0.3):
        if abs((y - k) - abs(x) * 0.9) < 0.05:
            light = 0.6
    return (1.0, light)


def s_snowflake(u, v):
    best = 9.0
    for i in range(6):
        a = i * math.pi / 3
        ex, ey = math.cos(a) * 0.95, math.sin(a) * 0.95
        best = min(best, seg_dist(u, v, 0, 0, ex, ey))
        for t, bl in ((0.5, 0.32), (0.75, 0.2)):
            bx, by = math.cos(a) * 0.95 * t, math.sin(a) * 0.95 * t
            for side in (-1, 1):
                ba = a + side * 0.8
                best = min(best, seg_dist(u, v, bx, by, bx + math.cos(ba) * bl, by + math.sin(ba) * bl))
    return clamp((0.075 - best) / 0.04 + 0.5)


def s_snow_crystal(u, v):
    pts = star_poly(6, 1.0, 0.5, 0)
    if not in_poly(u, v, pts):
        return 0.0
    r = math.hypot(u, v)
    light = 1.0 if r < 0.35 else facet_light(u, v, 12, math.pi / 12) * 0.95
    return (1.0, light)


def s_star5(u, v):
    if not in_poly(u, v + 0.06, star_poly(5, 1.0, 0.42)):
        return 0.0
    return (1.0, facet_light(u, v + 0.06, 10, math.pi / 2))


def s_star4(u, v):
    if not in_poly(u, v, star_poly(4, 1.0, 0.26)):
        return 0.0
    return (1.0, facet_light(u, v, 8, math.pi / 2))


def s_star8(u, v):
    pts = []
    for i in range(16):
        a = -math.pi / 2 + i * math.pi / 8
        r = (1.0 if (i // 2) % 2 == 0 else 0.6) if i % 2 == 0 else 0.2
        pts.append((math.cos(a) * r, math.sin(a) * r))
    if not in_poly(u, v, pts):
        return 0.0
    return (1.0, 1.0 - 0.3 * math.hypot(u, v))


def s_diamond(u, v):
    pts = [(0, -1), (0.62, -0.25), (0, 1), (-0.62, -0.25)]
    if not in_poly(u, v, pts):
        return 0.0
    if v < -0.25:
        return (1.0, 1.0 if u < 0 else 0.88)
    return (1.0, 0.78 if u < 0 else 0.6)


def s_shard_a(u, v):
    pts = [(0, -1), (0.26, -0.62), (0.26, 0.7), (0, 1), (-0.26, 0.7), (-0.26, -0.62)]
    if not in_poly(u, v, pts):
        return 0.0
    if u < -0.09:
        return (1.0, 1.0)
    if u < 0.09:
        return (1.0, 0.84)
    return (1.0, 0.64)


def s_shard_b(u, v):
    pts = [(-0.15, -0.95), (0.45, -0.35), (0.3, 0.9), (-0.5, 0.55), (-0.45, -0.2)]
    if not in_poly(u, v, pts):
        return 0.0
    return (1.0, 1.0 if u - v * 0.3 < 0 else 0.7)


def s_crescent(u, v):
    if u * u + v * v > 0.82 ** 2:
        return 0.0
    if (u - 0.36) ** 2 + (v + 0.12) ** 2 < 0.66 ** 2:
        return 0.0
    return (1.0, 0.75 + 0.25 * clamp(-u))


def s_feather(u, v):
    # перо по диагонали снизу-слева вверх-вправо
    ax, ay, bx, by = -0.8, 0.85, 0.85, -0.85
    ln = math.hypot(bx - ax, by - ay)
    dx, dy = (bx - ax) / ln, (by - ay) / ln
    px, py = u - ax, v - ay
    t = (px * dx + py * dy) / ln
    side = -px * dy + py * dx
    if t < 0 or t > 1:
        return 0.0
    if abs(side) < 0.045:
        return (1.0, 1.0)
    if t < 0.16:
        return 0.0
    tt = (t - 0.16) / 0.84
    w = 0.34 * math.sin(math.pi * min(1.0, tt * 1.08)) ** 0.65 * (0.75 if side > 0 else 1.0)
    if abs(side) > w:
        return 0.0
    barb = 0.8 + 0.2 * math.sin(t * 46 + abs(side) * 9)
    edge = abs(side) / max(w, 0.01)
    return (1.0, barb * (1.0 - 0.25 * edge))


def s_teardrop(u, v):
    # огонёк-капля остриём вверх
    if (u * u + (v - 0.4) ** 2) < 0.52 ** 2:
        inside = True
    else:
        t = (v + 1) / 1.4
        inside = -1 <= v <= 0.4 and abs(u) < 0.52 * t ** 0.9
    if not inside:
        return 0.0
    light = 1.0 - 0.45 * clamp(math.hypot(u, v - 0.35) / 0.9)
    return (1.0, light)


def s_bubble(u, v):
    r = math.hypot(u, v)
    if r > 0.9:
        return 0.0
    if r > 0.78:
        return (1.0, 0.9)
    if (u + 0.35) ** 2 + (v + 0.35) ** 2 < 0.05:
        return (1.0, 1.0)
    return (0.18, 1.0)


def s_butterfly(openness):
    def ell(u, v, cu, cv, ru, rv, ang):
        ca, sa = math.cos(ang), math.sin(ang)
        du, dv = u - cu, v - cv
        eu = (du * ca + dv * sa) / ru
        ev = (-du * sa + dv * ca) / rv
        return eu * eu + ev * ev

    def fn(u, v):
        if abs(u) < 0.07 and -0.55 < v < 0.6:
            return (1.0, 0.3)
        au = abs(u) / max(openness, 0.06)
        up = ell(au, v, 0.48, -0.3, 0.52, 0.4, -0.5)
        lo = ell(au, v, 0.38, 0.36, 0.36, 0.3, 0.6)
        q = min(up, lo)
        if q > 1:
            return 0.0
        light = 0.95 - 0.3 * q
        if q > 0.8:
            light = 0.55
        if (au - 0.72) ** 2 + (v + 0.42) ** 2 < 0.018 or (au - 0.42) ** 2 + (v - 0.45) ** 2 < 0.012:
            light = 1.0
        return (1.0, light)

    return fn


def s_swirl(u, v):
    best = 9.0
    turns = 1.6
    prev = None
    for i in range(80):
        t = i / 79
        a = t * turns * 2 * math.pi
        r = 0.1 + 0.85 * t
        p = (math.cos(a) * r, math.sin(a) * r)
        if prev:
            d = seg_dist(u, v, prev[0], prev[1], p[0], p[1])
            thick = 0.03 + 0.07 * t
            best = min(best, d - thick)
        prev = p
    return clamp(-best / 0.04 + 0.5)


def s_planet(u, v):
    ca, sa = math.cos(-0.35), math.sin(-0.35)
    ru, rv = u * ca + v * sa, -u * sa + v * ca
    ring_q = (ru / 0.95) ** 2 + (rv / 0.28) ** 2
    on_ring = 0.62 < ring_q < 1.0
    disc = u * u + v * v < 0.5 ** 2
    if disc and not (on_ring and rv > 0):
        return (1.0, 0.65 + 0.35 * clamp(-(u + v) * 0.9 + 0.3))
    if on_ring:
        return (1.0, 0.9)
    return 0.0


def s_lotus(u, v):
    def petal(px, py, ang, rx, ry):
        ca, sa = math.cos(ang), math.sin(ang)
        x, y = px * ca + py * sa, -px * sa + py * ca
        return (x / rx) ** 2 + ((y + ry * 0.2) / ry) ** 2 < 1

    base = (u, v - 0.55)
    if petal(base[0], base[1] + 0.55, 0, 0.3, 0.7):
        return (1.0, 1.0)
    for ang in (-0.75, 0.75):
        bx, by = base
        ca, sa = math.cos(ang), math.sin(ang)
        x, y = bx * ca - by * sa, bx * sa + by * ca
        if (x / 0.26) ** 2 + ((y + 0.55) / 0.6) ** 2 < 1:
            return (1.0, 0.78)
    return 0.0


def s_wisp(u, v):
    if (u - 0.45) ** 2 + v * v < 0.33 ** 2:
        return (1.0, 1.0)
    if -1 <= u <= 0.45:
        t = (u + 1) / 1.45
        if abs(v) < 0.33 * t ** 1.3:
            return (t ** 1.5, 0.7 + 0.3 * t)
    return 0.0


SHAPES = [
    s_heart, s_petal, s_petal_round, s_leaf, s_snowflake, s_snow_crystal, s_star5, s_star4,
    s_star8, s_diamond, s_shard_a, s_shard_b, s_crescent, s_feather, s_teardrop, s_bubble,
    s_butterfly(1.0), s_butterfly(0.72), s_butterfly(0.42), s_butterfly(0.14), s_swirl, s_planet, s_lotus, s_wisp,
]


def shapes():
    """Shapes 384x64: 24 формы в ячейках 32x32 (12 x 2), ShapesGlow — их свечение.
    Порядок совпадает с enum Shape в коде (Common/VFX/Shapes.cs)."""
    cell, cols = 32, 12
    fill = Canvas(cell * cols, cell * 2)
    radius = 12.0
    for i, fn in enumerate(SHAPES):
        ox, oy = (i % cols) * cell, (i // cols) * cell
        cx, cy = ox + cell / 2, oy + cell / 2

        def px(x, y, fn=fn, cx=cx, cy=cy):
            return fn((x - cx) / radius, (y - cy) / radius)

        fill.paint(px, ox, oy, ox + cell, oy + cell, ss=3)
    fill.save("Shapes")
    blur_alpha(fill, 2, gain=1.8).save("ShapesGlow")


# ======================= детали крыльев =======================
def feather_tex():
    """Feather 64x16: маховое перо — длинное, узкое, с острым кончиком; основание слева (x=2), кончик справа.
    Светлеет к кончику, по краю тёмная кайма (чтобы соседние перья читались отдельно), стержень светлый.
    FeatherGlow — свечение. Covert 32x16 — короткое округлое кроющее перо, CovertGlow — его свечение."""
    c = Canvas(64, 16)

    def fn(x, y):
        t = (x - 2) / 60
        if t < 0 or t > 1:
            return 0.0
        side = y - 8
        # профиль ширины: быстро расширяется, максимум на 35%, к кончику сходится в острие
        rise = min(1.0, t / 0.35) ** 0.6
        fall = 1.0 if t < 0.35 else ((1 - t) / 0.65) ** 0.85
        w = 6.4 * rise * fall * (0.62 if side < 0 else 1.0)
        if t < 0.05 or abs(side) > w + 0.01:
            if abs(side) < 0.6 and t < 0.12:
                return (1.0, 0.75)  # очин у основания
            return 0.0
        edge = abs(side) / max(w, 0.01)
        light = 0.62 + 0.38 * t ** 0.7          # светлее к кончику
        light *= 1.0 - 0.18 * edge ** 3          # мягкая светотень к краю
        if edge > 0.8:
            light *= 0.68                        # тёмная кайма
        if abs(side) < 0.55 and t < 0.9:
            light = min(1.0, light + 0.25)       # стержень
        return (1.0, light)

    c.paint(fn, ss=3)
    c.save("Feather")
    blur_alpha(c, 2, gain=1.5).save("FeatherGlow")

    cv = Canvas(32, 16)

    def fc(x, y):
        t = (x - 1) / 30
        if t < 0 or t > 1:
            return 0.0
        side = y - 8
        w = 6.0 * math.sin(math.pi * min(1.0, 0.08 + t * 0.92)) ** 0.5
        if abs(side) > w:
            return 0.0
        edge = abs(side) / max(w, 0.01)
        light = (0.7 + 0.3 * t) * (1.0 - 0.2 * edge ** 2)
        if edge > 0.82:
            light *= 0.72
        return (1.0, light)

    cv.paint(fc, ss=3)
    cv.save("Covert")
    blur_alpha(cv, 2, gain=1.5).save("CovertGlow")


def butterfly_wing():
    """ButterflyWing 64x64: пара крыльев одной стороны, крепление в (4, 32).
    ButterflyPattern — узор (кайма и «глазки») для второго цвета, ButterflyGlow — свечение."""
    fill = Canvas(64, 64)
    pattern = Canvas(64, 64)

    def ell(x, y, cx, cy, rx, ry, ang):
        ca, sa = math.cos(ang), math.sin(ang)
        dx, dy = x - cx, y - cy
        eu = (dx * ca + dy * sa) / rx
        ev = (-dx * sa + dy * ca) / ry
        return eu * eu + ev * ev

    def q_of(x, y):
        up = ell(x, y, 30, 18, 28, 15, -0.55)
        lo = ell(x, y, 24, 44, 19, 13, 0.6)
        return min(up, lo), up < lo

    def f_fill(x, y):
        q, upper = q_of(x, y)
        if q > 1:
            return 0.0
        dist = math.hypot(x - 4, y - 32) / 60
        light = 0.62 + 0.38 * clamp(dist * 1.4)
        # прожилки от крепления
        ang = math.atan2(y - 32, x - 4)
        if abs(math.sin(ang * 7.0)) < 0.08 and dist > 0.12:
            light *= 0.75
        return (1.0, light)

    def f_pattern(x, y):
        q, upper = q_of(x, y)
        if q > 1:
            return 0.0
        a = 0.0
        if q > 0.72:
            a = 1.0  # кайма
        # «глазки»
        for cx, cy, r in ((44, 12, 4.5), (30, 50, 3.5)):
            d = math.hypot(x - cx, y - cy)
            if d < r:
                a = 1.0 if d > r * 0.45 else 0.35
        return a

    fill.paint(f_fill, ss=3)
    pattern.paint(f_pattern, ss=3)
    fill.save("ButterflyWing")
    pattern.save("ButterflyPattern")
    blur_alpha(fill, 3, gain=1.4).save("ButterflyGlow")


def bat_wing():
    """BatWing 96x64: перепонка, крепление в (4, 22). BatBones — кости (для светящихся прожилок)."""
    shoulder = (4, 22)
    wrist = (38, 8)
    tips = [(93, 6), (88, 30), (70, 52), (42, 61)]
    membrane = Canvas(96, 64)
    bones = Canvas(96, 64)
    tris = []
    # треугольники перепонки: запястье + соседние кончики пальцев, плюс от плеча к последнему пальцу
    chain = tips + [(shoulder[0] + 6, shoulder[1] + 22)]
    for i in range(len(chain) - 1):
        tris.append([wrist, chain[i], chain[i + 1]])
    tris.append([shoulder, wrist, chain[-1]])

    def scallop_cut(x, y):
        # вырезы между пальцами: круги у середин кромки
        for i in range(len(chain) - 1):
            ax, ay = chain[i]
            bx, by = chain[i + 1]
            mx, my = (ax + bx) / 2, (ay + by) / 2
            # смещаем центр выреза наружу от запястья
            dx, dy = mx - wrist[0], my - wrist[1]
            dl = math.hypot(dx, dy) + 1e-9
            seg = math.hypot(bx - ax, by - ay)
            r = seg * 0.42
            cx, cy = mx + dx / dl * r * 0.45, my + dy / dl * r * 0.45
            if math.hypot(x - cx, y - cy) < r:
                return True
        return False

    def f_membrane(x, y):
        inside = any(in_poly(x, y, t) for t in tris)
        if not inside or scallop_cut(x, y):
            return 0.0
        d = math.hypot(x - wrist[0], y - wrist[1]) / 80
        return (1.0, 0.6 + 0.4 * clamp(1 - d))

    segs = [(shoulder[0], shoulder[1], wrist[0], wrist[1])] + [(wrist[0], wrist[1], tx, ty) for tx, ty in tips]

    def f_bones(x, y):
        d = min(seg_dist(x, y, *s) for s in segs)
        return clamp(1.6 - d)

    membrane.paint(f_membrane, ss=2)
    bones.paint(f_bones, ss=3)
    membrane.save("BatWing")
    bones.save("BatBones")


def flame_wisp():
    """Flame 128x48: 4 кадра язычка пламени 32x48 (основание внизу по центру)."""
    c = Canvas(128, 48)
    for frame in range(4):
        n = mv.value_noise(700 + frame, 8)
        ox = frame * 32

        def fn(x, y, ox=ox, n=n, frame=frame):
            u = (x - ox - 16) / 13
            t = 1 - y / 48  # 0 внизу, 1 вверху
            if t < 0 or t > 1:
                return 0.0
            wob = (n(t * 3 + frame, 0.5) - 0.5) * 0.6 * t
            u2 = u - wob
            width = (math.sin(math.pi * min(1.0, t * 0.9 + 0.1)) ** 0.8) * (1 - t) ** 0.35
            if abs(u2) > width:
                return 0.0
            core = 1 - abs(u2) / max(width, 0.01)
            return (clamp(0.4 + core * 1.2), 0.55 + 0.45 * core * (1 - t * 0.5))

        c.paint(fn, ox, 0, ox + 32, 48, ss=2)
    c.save("Flame")


def blade():
    """Blade 128x16: клинок света, основание слева, острый кончик справа."""
    c = Canvas(128, 16)

    def fn(x, y):
        t = x / 128
        v = abs(y - 8) / 8
        rise = min(t / 0.25, 1.0) ** 0.5
        fall = 1.0 if t < 0.25 else max(0.0, (1 - t) / 0.75) ** 1.2
        w = 0.06 + 0.94 * rise * fall
        if t > 0.99:
            return 0.0
        core = math.exp(-(v / (w * 0.45 + 0.02)) ** 2)
        soft = 0.35 * math.exp(-(v / (w + 0.05)) ** 2)
        return clamp((core + soft) * smooth(0.0, 0.05, t))

    c.paint(fn, ss=2)
    c.save("Blade")


def glint():
    """Glint 64x64: острый блик — два тонких длинных луча и маленькое ядро."""
    c = Canvas(64, 64)

    def fn(x, y):
        u, v = (x - 32) / 32, (y - 32) / 32
        r2 = u * u + v * v
        a = 0.0
        for p, q in ((u, v), (v, u)):
            ap = abs(p)
            if ap < 1:
                a += math.exp(-(q / (0.018 + 0.03 * (1 - ap))) ** 2) * (1 - ap) ** 2.2
        a += math.exp(-r2 * 120) + 0.3 * math.exp(-r2 * 18)
        return clamp(a)

    c.paint(fn, ss=2)
    c.save("Glint")


def light_beam():
    """GodRay 32x128 (замена): тонкий луч, основание внизу, к концу сужается и гаснет."""
    c = Canvas(32, 128)

    def fn(x, y):
        u = (x - 16) / 16
        t = 1 - y / 128
        w = 0.08 + 0.5 * math.sin(math.pi * min(1.0, t * 1.4)) ** 0.8 * (1 - t) ** 0.5
        a = math.exp(-(u / (w + 0.02)) ** 2)
        return clamp(a * (1 - t) ** 1.2 * smooth(0.0, 0.12, t))

    c.paint(fn, ss=2)
    c.save("GodRay")


def main():
    random.seed(4)
    shapes()
    feather_tex()
    butterfly_wing()
    bat_wing()
    flame_wisp()
    blade()
    glint()
    light_beam()


if __name__ == "__main__":
    main()
