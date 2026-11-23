"""Генератор текстур для визуальных эффектов (v0.3), чистый Python без библиотек.

Запуск:  python3 tools/make_vfx.py
Все текстуры белые: цвет задаётся при отрисовке. Свечение рисуется аддитивно
(в коде у цвета ставится A = 0), поэтому у «светящихся» текстур важна только
прозрачность. Результат кладётся в Assets/VFX/.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pngio  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = "Assets/VFX/"


# ======================= холст =======================
class Canvas:
    """Альфа-канал + яркость (оттенок серого), сохраняется как белая RGBA-картинка."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.a = [0.0] * (w * h)
        self.l = [1.0] * (w * h)

    def paint(self, fn, x0=0, y0=0, x1=None, y1=None, ss=2, mode="max"):
        """fn(x, y) -> альфа или (альфа, яркость); x, y — координаты в пикселях (центр пикселя = +0.5)."""
        x1 = self.w if x1 is None else x1
        y1 = self.h if y1 is None else y1
        inv = 1.0 / (ss * ss)
        for py in range(y0, y1):
            for px in range(x0, x1):
                acc_a, acc_l = 0.0, 0.0
                for sy in range(ss):
                    for sx in range(ss):
                        r = fn(px + (sx + 0.5) / ss, py + (sy + 0.5) / ss)
                        if isinstance(r, tuple):
                            acc_a += r[0]
                            acc_l += r[0] * r[1]
                        else:
                            acc_a += r
                            acc_l += r
                a = acc_a * inv
                if a <= 0.0:
                    continue
                light = acc_l / acc_a if acc_a > 0 else 1.0
                i = py * self.w + px
                if mode == "max":
                    if a > self.a[i]:
                        self.l[i] = light
                        self.a[i] = a
                elif mode == "add":
                    self.a[i] = min(1.0, self.a[i] + a)
                elif mode == "over":  # поверх: смешиваем яркость
                    old = self.a[i]
                    na = a + old * (1 - a)
                    if na > 0:
                        self.l[i] = (light * a + self.l[i] * old * (1 - a)) / na
                    self.a[i] = na

    def save(self, name):
        img = pngio.Img(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                i = y * self.w + x
                a = max(0.0, min(1.0, self.a[i]))
                v = int(255 * max(0.0, min(1.0, self.l[i])))
                img.px[y][x] = (v, v, v, int(round(255 * a)))
        path = os.path.join(ROOT, OUT + name + ".png")
        img.save(path)
        print("saved", OUT + name + ".png", f"{self.w}x{self.h}")


# ======================= математика =======================
def clamp(v, a=0.0, b=1.0):
    return a if v < a else b if v > b else v


def smooth(e0, e1, x):
    t = clamp((x - e0) / (e1 - e0))
    return t * t * (3 - 2 * t)


def seg_dist(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    ll = dx * dx + dy * dy
    t = 0.0 if ll == 0 else clamp(((px - ax) * dx + (py - ay) * dy) / ll)
    qx, qy = ax + dx * t - px, ay + dy * t - py
    return math.sqrt(qx * qx + qy * qy)


def in_poly(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi:
            inside = not inside
        j = i
    return inside


def stroke(segs, thick, glow=0.0, glow_r=3.0):
    """Сглаженные линии (список отрезков) + мягкий ореол вокруг."""

    def fn(x, y):
        d = min(seg_dist(x, y, *s) for s in segs)
        core = clamp(thick / 2 + 0.5 - d)
        if glow > 0:
            core = max(core, glow * math.exp(-(d / glow_r) ** 2))
        return core

    return fn


def norm(x, y, cx, cy, r):
    return (x - cx) / r, (y - cy) / r


# ======================= свечение =======================
def bloom():
    """Bloom 128: мягкое гауссово пятно, ровно гаснет к краю."""
    c = Canvas(128, 128)
    k = 4.5
    tail = math.exp(-k)

    def fn(x, y):
        u, v = norm(x, y, 64, 64, 64)
        r2 = u * u + v * v
        if r2 >= 1:
            return 0.0
        return (math.exp(-r2 * k) - tail) / (1 - tail)

    c.paint(fn, ss=1)
    c.save("Bloom")


def orb():
    """Orb 64: яркое ядро + ореол (огонёк, уголёк, светлячок)."""
    c = Canvas(64, 64)

    def fn(x, y):
        u, v = norm(x, y, 32, 32, 32)
        r = math.sqrt(u * u + v * v)
        if r >= 1:
            return 0.0
        core = smooth(0.34, 0.12, r)
        halo = 0.55 * (math.exp(-r * r * 7) - math.exp(-7))
        return clamp(core + halo)

    c.paint(fn)
    c.save("Orb")


def flare():
    """Flare 128: четырёхлучевая вспышка с короткими диагональными лучами."""
    c = Canvas(128, 128)

    def ray(u, v, length, width):
        au = abs(u) / length
        if au >= 1:
            return 0.0
        w = width * (1 - au) + 0.004
        return math.exp(-(v / w) ** 2) * (1 - au) ** 1.6

    def fn(x, y):
        u, v = norm(x, y, 64, 64, 64)
        r2 = u * u + v * v
        a = ray(u, v, 1.0, 0.05) + ray(v, u, 1.0, 0.05)
        d1, d2 = (u + v) * 0.7071, (u - v) * 0.7071
        a += 0.45 * (ray(d1, d2, 0.55, 0.04) + ray(d2, d1, 0.55, 0.04))
        a += 0.9 * math.exp(-r2 * 60) + 0.35 * math.exp(-r2 * 10)
        if r2 >= 1:
            a *= 0
        return clamp(a)

    c.paint(fn)
    c.save("Flare")


def streak():
    """Streak 128x32: вытянутая искра (направление = ось X)."""
    c = Canvas(128, 32)

    def fn(x, y):
        u = (x - 64) / 64
        v = (y - 16) / 16
        taper = (1 - u * u) ** 1.5 if abs(u) < 1 else 0.0
        w = 0.18 + 0.32 * taper
        return clamp(math.exp(-(v / w) ** 2) * taper * 1.15)

    c.paint(fn)
    c.save("Streak")


def line_tex():
    """Line 32x16: мягкая линия для молний и лучей (растягивается по X)."""
    c = Canvas(32, 16)

    def fn(x, y):
        v = (y - 8) / 8
        return clamp(math.exp(-(v / 0.32) ** 2) * 1.1 + 0.25 * math.exp(-(v / 0.8) ** 2))

    c.paint(fn, ss=1)
    c.save("Line")


def ring_wave():
    """RingWave 128: тонкое кольцо с ореолом (волны, нимбы, круги под ногами)."""
    c = Canvas(128, 128)

    def fn(x, y):
        u, v = norm(x, y, 64, 64, 64)
        r = math.sqrt(u * u + v * v)
        if r >= 1:
            return 0.0
        a = math.exp(-((r - 0.84) / 0.045) ** 2) + 0.3 * math.exp(-((r - 0.82) / 0.13) ** 2)
        return clamp(a * smooth(1.0, 0.95, r))

    c.paint(fn)
    c.save("RingWave")


def god_ray():
    """GodRay 32x128: луч света, основание внизу по центру."""
    c = Canvas(32, 128)

    def fn(x, y):
        u = (x - 16) / 16
        t = 1 - y / 128  # 0 внизу, 1 вверху
        width = 0.25 + 0.55 * t
        return clamp(math.exp(-(u / width) ** 2) * (1 - t) ** 1.3 * smooth(0.0, 0.08, t))

    c.paint(fn, ss=1)
    c.save("GodRay")


# ======================= дым =======================
def value_noise(seed, size):
    rnd = random.Random(seed)
    grid = [[rnd.random() for _ in range(size + 1)] for _ in range(size + 1)]

    def f(x, y):
        x0, y0 = int(x), int(y)
        fx, fy = x - x0, y - y0
        fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        x0 %= size
        y0 %= size
        x1, y1 = (x0 + 1) % size, (y0 + 1) % size
        a = grid[y0][x0] + (grid[y0][x1] - grid[y0][x0]) * fx
        b = grid[y1][x0] + (grid[y1][x1] - grid[y1][x0]) * fx
        return a + (b - a) * fy

    return f


def smoke():
    """Smoke 256x64: 4 кадра клубов дыма 64x64 (слева направо)."""
    c = Canvas(256, 64)
    for frame in range(4):
        n1 = value_noise(100 + frame, 16)
        n2 = value_noise(200 + frame, 16)
        n3 = value_noise(300 + frame, 16)
        ox = frame * 64

        def fn(x, y, ox=ox, n1=n1, n2=n2, n3=n3):
            u, v = norm(x - ox, y, 32, 32, 30)
            r = math.sqrt(u * u + v * v)
            if r >= 1:
                return 0.0
            lx, ly = (x - ox) / 64 * 2.5, y / 64 * 2.5
            n = 0.6 * n1(lx, ly) + 0.28 * n2(lx * 2, ly * 2) + 0.12 * n3(lx * 4, ly * 4)
            fall = smooth(1.0, 0.25, r)
            a = clamp((n * 1.25 - 0.25) * fall * 1.6)
            return (a, 0.75 + 0.25 * n)

        c.paint(fn, ox, 0, ox + 64, 64, ss=1)
    c.save("Smoke")


# ======================= спрайты =======================
def heart(c, ox, oy, s):
    def fn(x, y):
        u = (x - ox - s / 2) / (s * 0.42)
        v = -(y - oy - s * 0.52) / (s * 0.42)
        # классическое уравнение сердца
        q = (u * u + v * v - 1) ** 3 - u * u * v ** 3
        if q > 0:
            return 0.0
        light = 0.8 + 0.2 * clamp(v) + 0.25 * math.exp(-((u + 0.45) ** 2 + (v - 0.45) ** 2) * 12)
        return (1.0, clamp(light))

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def petal(c, ox, oy, s):
    def fn(x, y):
        u = (x - ox - s / 2) / (s * 0.38)
        v = (y - oy - s / 2) / (s * 0.46)
        # капля, сужающаяся книзу, с вырезом сверху
        width = math.sqrt(max(0.0, 1 - v * v)) * (0.65 + 0.35 * (1 - v) / 2)
        if abs(u) > width or abs(v) > 1:
            return 0.0
        notch = v < -0.55 and abs(u) < (-0.55 - v) * 0.9
        if notch:
            return 0.0
        light = 0.78 + 0.22 * (1 - abs(u) / max(width, 0.01)) + 0.15 * (v > 0.3) * (v - 0.3)
        return (1.0, clamp(light))

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def snowflake(c, ox, oy, s):
    cx, cy = ox + s / 2, oy + s / 2
    r = s * 0.46
    segs = []
    for i in range(6):
        a = i * math.pi / 3
        ex, ey = cx + math.cos(a) * r, cy + math.sin(a) * r
        segs.append((cx, cy, ex, ey))
        for t, blen in ((0.55, 0.28), (0.8, 0.18)):
            bx, by = cx + math.cos(a) * r * t, cy + math.sin(a) * r * t
            for side in (-1, 1):
                ba = a + side * 0.75
                segs.append((bx, by, bx + math.cos(ba) * r * blen, by + math.sin(ba) * r * blen))
    c.paint(stroke(segs, s / 16, glow=0.35, glow_r=s / 14), ox, oy, ox + s, oy + s, ss=3)


def star5(c, ox, oy, s):
    cx, cy = ox + s / 2, oy + s / 2 + s * 0.03
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = s * (0.47 if i % 2 == 0 else 0.2)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))

    def fn(x, y):
        if in_poly(x, y, pts):
            d = math.hypot(x - cx, y - cy) / (s * 0.47)
            return (1.0, 1.0 - 0.25 * d)
        return 0.0

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def shard(c, ox, oy, s):
    cx, cy = ox + s / 2, oy + s / 2
    pts = [(cx, oy + 1), (cx + s * 0.16, cy - s * 0.05), (cx, oy + s - 1), (cx - s * 0.16, cy + s * 0.05)]

    def fn(x, y):
        if in_poly(x, y, pts):
            return (1.0, 1.0 if x < cx else 0.72)
        return 0.0

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def feather(c, ox, oy, s):
    # перо по диагонали из левого нижнего угла в правый верхний
    ax, ay = ox + s * 0.15, oy + s * 0.88
    bx, by = ox + s * 0.86, oy + s * 0.12
    length = math.hypot(bx - ax, by - ay)
    dx, dy = (bx - ax) / length, (by - ay) / length

    def fn(x, y):
        px, py = x - ax, y - ay
        t = (px * dx + py * dy) / length
        side = -px * dy + py * dx
        if t < 0 or t > 1:
            return 0.0
        if abs(side) < s * 0.025:
            return (1.0, 0.65)
        if t < 0.18:
            return 0.0
        tt = (t - 0.18) / 0.82
        w = s * 0.2 * math.sin(math.pi * min(1.0, tt * 1.1)) ** 0.7
        if abs(side) > w:
            return 0.0
        barb = 0.85 + 0.15 * math.sin((t * 40 + abs(side) * 0.6))
        return (1.0, barb)

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def butterfly(c, ox, oy, s, openness):
    cx, cy = ox + s / 2, oy + s / 2

    def ellipse(u, v, cu, cv, ru, rv, ang):
        ca, sa = math.cos(ang), math.sin(ang)
        du, dv = u - cu, v - cv
        eu = (du * ca + dv * sa) / ru
        ev = (-du * sa + dv * ca) / rv
        return eu * eu + ev * ev

    def fn(x, y):
        u = (x - cx) / (s / 2)
        v = (y - cy) / (s / 2)
        if abs(u) < 0.07 and -0.55 < v < 0.6:
            return (1.0, 0.35)
        au = abs(u) / max(openness, 0.05)
        up = ellipse(au, v, 0.48, -0.3, 0.52, 0.38, -0.5)
        lo = ellipse(au, v, 0.38, 0.35, 0.36, 0.3, 0.6)
        q = min(up, lo)
        if q > 1:
            return 0.0
        light = 0.95 - 0.35 * q
        if (au - 0.75) ** 2 + (v + 0.45) ** 2 < 0.02 or (au - 0.45) ** 2 + (v - 0.45) ** 2 < 0.012:
            light = 1.0
        return (1.0, light)

    c.paint(fn, ox, oy, ox + s, oy + s, ss=3)


def sprites():
    """Sprites 256x32: 8 спрайтов 32x32 слева направо:
    0 сердце, 1 лепесток, 2 снежинка, 3 звезда, 4 осколок, 5 перо, 6 бабочка (открыта), 7 бабочка (сложена)."""
    s = 32
    c = Canvas(8 * s, s)
    heart(c, 0, 0, s)
    petal(c, s, 0, s)
    snowflake(c, 2 * s, 0, s)
    star5(c, 3 * s, 0, s)
    shard(c, 4 * s, 0, s)
    feather(c, 5 * s, 0, s)
    butterfly(c, 6 * s, 0, s, 1.0)
    butterfly(c, 7 * s, 0, s, 0.35)
    c.save("Sprites")


# ======================= руны =======================
# Руны в квадрате 0..1, списки отрезков (стилизованный футарк)
RUNES = [
    [(0.35, 0.05, 0.35, 0.95), (0.35, 0.35, 0.75, 0.1), (0.35, 0.6, 0.75, 0.35)],          # Fehu
    [(0.3, 0.95, 0.3, 0.05), (0.3, 0.05, 0.7, 0.3), (0.7, 0.3, 0.7, 0.95)],                # Uruz
    [(0.5, 0.05, 0.5, 0.95), (0.5, 0.3, 0.8, 0.5), (0.8, 0.5, 0.5, 0.7)],                  # Thurisaz
    [(0.5, 0.05, 0.5, 0.95), (0.5, 0.45, 0.2, 0.15), (0.5, 0.45, 0.8, 0.15)],              # Algiz
    [(0.2, 0.05, 0.8, 0.95), (0.8, 0.05, 0.2, 0.95)],                                      # Gebo
    [(0.5, 0.05, 0.2, 0.4), (0.2, 0.4, 0.5, 0.75), (0.5, 0.75, 0.8, 0.4), (0.8, 0.4, 0.5, 0.05),
     (0.2, 0.95, 0.5, 0.75), (0.8, 0.95, 0.5, 0.75)],                                      # Othala
    [(0.3, 0.05, 0.3, 0.95), (0.7, 0.05, 0.7, 0.95), (0.3, 0.05, 0.7, 0.95), (0.7, 0.05, 0.3, 0.95)],  # Dagaz
    [(0.5, 0.05, 0.5, 0.95), (0.25, 0.25, 0.75, 0.55), (0.75, 0.25, 0.25, 0.55)],          # звезда-руна
]


def runes():
    """Runes 256x32: 8 светящихся рун 32x32 слева направо."""
    s = 32
    c = Canvas(8 * s, s)
    pad = 5
    for i, rune in enumerate(RUNES):
        ox = i * s
        segs = [(ox + pad + a * (s - 2 * pad), pad + b * (s - 2 * pad), ox + pad + cc * (s - 2 * pad), pad + d * (s - 2 * pad))
                for a, b, cc, d in rune]
        c.paint(stroke(segs, 2.2, glow=0.45, glow_r=3.2), ox, 0, ox + s, s, ss=3)
    c.save("Runes")


# ======================= нимбы и круги =======================
def ellipse_dist(x, y, cx, cy, rx, ry):
    """Приближённое расстояние (в пикселях) от точки до эллипса."""
    u, v = (x - cx) / rx, (y - cy) / ry
    q = math.sqrt(u * u + v * v) + 1e-9
    gx, gy = (x - cx) / (rx * rx) / q, (y - cy) / (ry * ry) / q
    g = math.sqrt(gx * gx + gy * gy) + 1e-9
    return (q - 1) / g


def ring_fn(cx, cy, rx, ry, thick, glow=0.35, glow_r=2.5):
    def fn(x, y):
        d = abs(ellipse_dist(x, y, cx, cy, rx, ry))
        return clamp(max(clamp(thick / 2 + 0.5 - d), glow * math.exp(-(d / glow_r) ** 2)))

    return fn


def halo_ring():
    """HaloRing 32x12: эллипс нимба уже в перспективе (рисуется почти 1:1)."""
    c = Canvas(32, 12)
    c.paint(ring_fn(16, 6, 13.5, 3.8, 1.4, glow=0.4, glow_r=1.6), ss=4)
    c.save("HaloRing")


def ground_ring():
    """GroundRing 128x40: кольцо на земле в перспективе."""
    c = Canvas(128, 40)
    c.paint(ring_fn(64, 20, 60, 17, 2.2, glow=0.4, glow_r=3.0), ss=3)
    c.save("GroundRing")


def thorn_halo():
    """ThornHalo 40x18: терновый венец в перспективе — шипы смотрят вверх и наружу."""
    c = Canvas(40, 18)
    cx, cy, rx, ry = 20, 11, 15, 4.2
    spikes = []
    n = 12
    for k in range(n):
        a = k * 2 * math.pi / n + 0.13
        bx, by = cx + math.cos(a) * rx, cy + math.sin(a) * ry
        dx, dy = math.cos(a) * 0.55, -0.85
        ln = 4.5 + 1.5 * (k % 2)
        tx, ty = bx + dx * ln, by + dy * ln
        px, py = -dy, dx
        w = 1.4
        spikes.append([(bx + px * w, by + py * w), (tx, ty), (bx - px * w, by - py * w)])

    ring = ring_fn(cx, cy, rx, ry, 1.6, glow=0.0)

    def fn(x, y):
        a = ring(x, y)
        for tri in spikes:
            if in_poly(x, y, tri):
                return 1.0
        return a

    c.paint(fn, ss=4)
    c.save("ThornHalo")


def sigil_rings():
    """SigilRings 128x40: кольца магического круга сразу в перспективе (без вращающихся деталей)."""
    c = Canvas(128, 40)
    cx, cy, rx = 64, 20, 62.0
    ry = rx * 0.3
    rings = [(0.97, 1.6, 1.0), (0.89, 1.0, 0.8), (0.62, 1.3, 0.9), (0.55, 0.9, 0.6), (0.22, 1.0, 0.7)]
    ticks = 40

    def fn(x, y):
        a = 0.0
        for k, thick, power in rings:
            d = abs(ellipse_dist(x, y, cx, cy, rx * k, ry * k))
            a += power * max(clamp(thick / 2 + 0.5 - d), 0.3 * math.exp(-(d / 1.8) ** 2))
        u, v = (x - cx) / rx, (y - cy) / ry
        r = math.sqrt(u * u + v * v)
        ang = math.atan2(v, u)
        ph = (ang / (2 * math.pi) * ticks) % 1.0
        if 0.9 < r < 0.96 and abs(ph - 0.5) < 0.12:
            a += 0.7
        a += 0.1 * smooth(0.97, 0.5, r)
        return clamp(a)

    c.paint(fn, ss=3)
    c.save("SigilRings")


def main():
    random.seed(1)
    bloom()
    orb()
    flare()
    streak()
    line_tex()
    ring_wave()
    god_ray()
    smoke()
    sprites()
    runes()
    halo_ring()
    ground_ring()
    thorn_halo()
    sigil_rings()


if __name__ == "__main__":
    main()
