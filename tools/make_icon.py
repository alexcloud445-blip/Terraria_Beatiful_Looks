"""Иконки мода: icon.png (80x80, обязательная), icon_small.png (30x30) и icon_workshop.png (480x480, превью в Steam).

Запуск: python3 tools/make_icon.py
Нужны numpy и Pillow (только для этого скрипта; сам мод их не использует).

Рисунок: космический фон с туманностью и звёздами, крылья серафима из трёх рядов перьев с градиентом
(сливочный → золото → розовый), золотой нимб, сияющая звезда в центре и наклонное кольцо астролябии,
которое проходит позади крыльев и перед ними. Всё рисуется в 960x960 и уменьшается усреднением,
поэтому края гладкие.
"""
import math
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
N = 960
PX = N / 2.0  # пикселей в единице координат (координаты от -1 до 1)
OY = 0.06  # весь знак (крылья, нимб, звезда, кольцо) чуть ниже центра — так композиция ровнее
WING_SCALE = 0.93

ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
X = (xs + 0.5) / N * 2 - 1
Y = (ys + 0.5) / N * 2 - 1


def col(*c):
    return np.array(c, dtype=np.float32) / 255.0


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def aa(dist_units):
    """Расстояние до края в единицах (отрицательное внутри) → прозрачность со сглаживанием в 1 пиксель."""
    return np.clip(0.5 - dist_units * PX, 0, 1)


def blur(a, radius):
    """Размытие ≈ гаусс: три прохода скользящего среднего (через cumsum)."""
    r = max(1, int(radius))
    out = a.astype(np.float32)
    for _ in range(3):
        for axis in (0, 1):
            pad = [(0, 0)] * out.ndim
            pad[axis] = (r + 1, r)
            p = np.pad(out, pad, mode="constant")
            c = np.cumsum(p, axis=axis)
            if axis == 0:
                out = (c[2 * r + 1:, ...] - c[:-2 * r - 1, ...]) / (2 * r + 1)
            else:
                out = (c[:, 2 * r + 1:, ...] - c[:, :-2 * r - 1, ...]) / (2 * r + 1)
    return out


def noise(seed, cells):
    """Плавный шум 0..1: случайная сетка, увеличенная бикубически."""
    rng = np.random.default_rng(seed)
    g = rng.random((cells, cells)).astype(np.float32)
    img = Image.fromarray(g, mode="F").resize((N, N), Image.BICUBIC)
    return np.clip(np.asarray(img), 0, 1)


def fbm(seed):
    return 0.55 * noise(seed, 5) + 0.3 * noise(seed + 1, 11) + 0.15 * noise(seed + 2, 23)


class Layer:
    """Холст RGB (premultiplied) + альфа. over — обычное наложение, add — сложение света."""

    def __init__(self):
        self.rgb = np.zeros((N, N, 3), np.float32)
        self.a = np.zeros((N, N), np.float32)

    def over(self, color, alpha):
        alpha = np.clip(alpha, 0, 1)[..., None]
        c = color if np.ndim(color) == 3 else np.broadcast_to(color, (N, N, 3))
        self.rgb = c * alpha + self.rgb * (1 - alpha)
        self.a = alpha[..., 0] + self.a * (1 - alpha[..., 0])

    def add(self, color, alpha):
        alpha = np.clip(alpha, 0, None)[..., None]
        c = color if np.ndim(color) == 3 else np.broadcast_to(color, (N, N, 3))
        self.rgb = self.rgb + c * alpha
        self.a = np.clip(self.a + alpha[..., 0] * 0.5, 0, 1)


def lerp3(stops, t):
    """Градиент по нескольким цветам, t — массив 0..1."""
    t = np.clip(t, 0, 1)[..., None] * (len(stops) - 1)
    out = np.zeros(t.shape[:-1] + (3,), np.float32)
    for i in range(len(stops) - 1):
        w = np.clip(t - i, 0, 1)
        if i == 0:
            out = stops[0] + (stops[1] - stops[0]) * w
        else:
            out = out + (stops[i + 1] - stops[i]) * w
    return out


# ======================= части рисунка =======================
def rounded_box_sd(hw, radius):
    qx = np.abs(X) - (hw - radius)
    qy = np.abs(Y) - (hw - radius)
    outside = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - radius


def background(L, box_sd):
    inside = aa(box_sd)
    r = np.sqrt(X ** 2 + (Y + 0.08) ** 2)
    base = lerp3([col(86, 38, 140), col(40, 20, 82), col(12, 9, 32)], smooth(0.0, 1.35, r))
    L.over(base, inside)
    # туманность: розовое и бирюзовое облака
    n1, n2 = fbm(11), fbm(23)
    pink = smooth(0.45, 0.95, n1) * smooth(1.4, 0.2, np.sqrt((X + 0.45) ** 2 + (Y + 0.35) ** 2))
    teal = smooth(0.45, 0.95, n2) * smooth(1.4, 0.2, np.sqrt((X - 0.5) ** 2 + (Y - 0.45) ** 2))
    L.add(col(210, 70, 180), 0.32 * pink * inside)
    L.add(col(50, 150, 230), 0.26 * teal * inside)
    # звёзды: немного крупных (видны и в 80x80) и россыпь мелких (видны в превью Steam)
    rng = np.random.default_rng(7)
    stars = np.zeros((N, N), np.float32)
    for _ in range(170):
        sx, sy = rng.uniform(-0.95, 0.95, 2)
        big = rng.random() < 0.12
        rad = (5.5 if big else 2.2) / PX
        bright = rng.uniform(0.5, 1.0) if big else rng.uniform(0.25, 0.8)
        d2 = (X - sx) ** 2 + (Y - sy) ** 2
        stars += bright * np.exp(-d2 / (rad * rad))
    L.add(col(230, 225, 255), np.clip(stars, 0, 1.2) * inside * 0.9)


def god_rays(L, box_inside):
    a = np.arctan2(Y + 0.02 - OY, X)
    r = np.sqrt(X ** 2 + (Y + 0.02 - OY) ** 2)
    rays = (0.5 + 0.5 * np.cos(a * 9)) ** 30 + 0.6 * (0.5 + 0.5 * np.cos(a * 9 + math.pi)) ** 40
    L.add(col(255, 200, 140), 0.16 * rays * np.exp(-r / 0.55) * smooth(0.05, 0.2, r) * box_inside)
    glow = np.exp(-((X ** 2 + (Y + 0.02 - OY) ** 2) / 0.12))
    L.add(col(255, 150, 120), 0.32 * glow * box_inside)
    L.add(col(255, 90, 200), 0.22 * np.exp(-((X ** 2 + (Y + 0.02 - OY) ** 2) / 0.45)) * box_inside)


RING = dict(cx=0.0, cy=0.12 + OY, rx=0.86, ry=0.24, rot=-0.26, thick=0.034)


def ring_fields():
    c, s = math.cos(RING["rot"]), math.sin(RING["rot"])
    dx, dy = X - RING["cx"], Y - RING["cy"]
    lx = dx * c + dy * s
    ly = -dx * s + dy * c
    u, v = lx / RING["rx"], ly / RING["ry"]
    q = np.sqrt(u * u + v * v) + 1e-6
    gx, gy = lx / (RING["rx"] ** 2) / q, ly / (RING["ry"] ** 2) / q
    dist = (q - 1) / (np.sqrt(gx * gx + gy * gy) + 1e-6)
    ang = np.arctan2(v, u)
    return dist, ang, ly


def astrolabe(L, front):
    dist, ang, ly = ring_fields()
    half = (ly > 0) if front else (ly <= 0)  # нижняя половина эллипса — ближняя к зрителю
    t = 0.5 + 0.5 * np.sin(ang * 1.0 + 0.6)
    color = lerp3([col(255, 214, 120), col(255, 150, 170), col(170, 140, 255)], t)
    depth = 1.0 if front else 0.55
    core = aa(np.abs(dist) - RING["thick"] / 2) * half
    glow = np.exp(-(dist / 0.05) ** 2) * half
    L.add(color, 0.35 * glow * depth)
    L.over(color * (0.9 if front else 0.7), core * depth)
    L.add(col(255, 250, 235), 0.45 * aa(np.abs(dist) - RING["thick"] / 5) * half * depth)
    # деления шкалы
    k = (ang / (2 * math.pi) * 36) % 1.0
    ticks = (np.abs(k - 0.5) < 0.06) & (dist < 0) & (dist > -0.06) & half
    L.over(col(255, 235, 190) * depth, ticks.astype(np.float32) * 0.85)
    # «планета» на кольце
    for pa in ((0.9,) if front else (-2.3,)):
        px = RING["cx"] + math.cos(RING["rot"]) * RING["rx"] * math.cos(pa) - math.sin(RING["rot"]) * RING["ry"] * math.sin(pa)
        py = RING["cy"] + math.sin(RING["rot"]) * RING["rx"] * math.cos(pa) + math.cos(RING["rot"]) * RING["ry"] * math.sin(pa)
        d2 = (X - px) ** 2 + (Y - py) ** 2
        L.add(col(150, 200, 255), 0.6 * np.exp(-d2 / 0.006) * depth)
        L.over(col(225, 240, 255), aa(np.sqrt(d2) - 0.042) * depth)
        L.add(col(255, 255, 255), 0.6 * np.exp(-d2 / 0.0008))


def feather(cx, cy, ang, length, width, bend, round_tip=False):
    """Поле пера: (альфа с краем, t вдоль 0..1, |s|/w поперёк)."""
    d = (math.cos(ang), math.sin(ang))
    n = (-math.sin(ang), math.cos(ang))
    px, py = X - cx, Y - cy
    t = (px * d[0] + py * d[1]) / length
    s = px * n[0] + py * n[1] - bend * length * np.clip(t, 0, 1) ** 2
    tc = np.clip(t, 0, 1)
    if round_tip:
        w = width * np.maximum(np.sin(math.pi * tc ** 0.6), 0) ** 0.55
    else:
        w = width * np.maximum(np.sin(math.pi * tc ** 0.72), 0) ** 0.85 * (1 - 0.2 * tc)
    inside_t = (t > 0) & (t < 1)
    dist = np.where(inside_t, np.abs(s) - w, 1.0)
    edge = np.clip(np.abs(s) / np.maximum(w, 1e-4), 0, 1)
    return dist, t, edge, s


def wing_rows():
    """Перья правого крыла (левое — зеркально): (ряд, x, y, угол, длина, ширина, изгиб).
    Крыло ангела: «рука» плечо → запястье → кисть задаёт верхнюю дугу; второстепенные перья свисают
    с предплечья вниз, маховые веером расходятся от кисти наружу, сверху — кроющие и мелкие перья."""
    def lerp(a, b, t):
        return a + (b - a) * t

    def on_arm(u):
        sx, sy = 0.08, 0.03 + OY
        wx, wy = 0.33, -0.31 + OY
        tx, ty = 0.54, -0.40 + OY
        if u < 0.6:
            k = u / 0.6
            return lerp(sx, wx, k), lerp(sy, wy, k)
        k = (u - 0.6) / 0.4
        return lerp(wx, tx, k), lerp(wy, ty, k)

    rows = []
    # (ряд, кол-во, участок руки u0..u1, углы град, длины, ширина, изгиб, круглый кончик)
    specs = [
        (0, 8, (0.6, 1.0), (52, -16), [0.44, 0.47, 0.48, 0.47, 0.45, 0.42, 0.39, 0.36], 0.072, -0.07),
        (1, 7, (0.08, 0.62), (84, 58), [0.40, 0.42, 0.43, 0.44, 0.44, 0.44, 0.44], 0.074, -0.05),
        (2, 9, (0.05, 0.95), (80, 4), [0.22, 0.23, 0.24, 0.24, 0.24, 0.23, 0.22, 0.21, 0.2], 0.08, -0.03),
        (3, 10, (0.02, 0.9), (76, 12), [0.13, 0.13, 0.13, 0.13, 0.13, 0.13, 0.12, 0.12, 0.11, 0.1], 0.07, -0.02),
    ]
    for row, count, (u0, u1), (a0, a1), lens, width, bend in specs:
        for i in range(count):
            k = i / (count - 1)
            bx, by = on_arm(lerp(u0, u1, k))
            a = math.radians(lerp(a0, a1, k))
            rows.append((row, bx, by, a, lens[i] * WING_SCALE, width, bend))
    return rows


ROW_COLORS = [
    [col(255, 250, 244), col(255, 222, 160), col(255, 132, 196)],
    [col(255, 252, 248), col(255, 228, 178), col(255, 156, 200)],
    [col(255, 254, 250), col(255, 240, 206), col(255, 214, 190)],
    [col(255, 255, 252), col(255, 246, 224), col(255, 230, 200)],
]


def wings(L, mask_only=False):
    """Оба крыла. mask_only — вернуть только общую альфу (для свечения позади)."""
    feathers = wing_rows()
    mask = np.zeros((N, N), np.float32)
    for side in (1, -1):
        for row, bx, by, a, ln, w, bend in feathers:
            ang = a if side > 0 else math.pi - a
            fx = bx * side
            dist, t, edge, s = feather(fx, by, ang, ln, w, bend * side, round_tip=(row >= 2))
            alpha = aa(dist)
            if mask_only:
                mask = np.maximum(mask, alpha)
                continue
            # тень-обводка: перья читаются отдельно даже в 80x80
            shadow = aa(dist - w * 0.13)
            L.over(col(110, 56, 140), shadow * 0.5)
            color = lerp3(ROW_COLORS[row], t)
            light = (0.9 + 0.12 * np.clip(t, 0, 1)) * (1 - 0.25 * edge ** 3)
            shaft = (np.abs(s) < 0.006) & (t > 0.05) & (t < 0.85)
            light = np.where(shaft, np.minimum(1.15, light + 0.2), light)
            # одна сторона пера чуть темнее — объём
            light = light * np.where(s * side > 0, 0.88, 1.0)
            L.over(np.clip(color * light[..., None], 0, 1.2), alpha)
    return mask


def halo(L):
    cx, cy, rx, ry, th = 0.0, -0.6 + OY, 0.21, 0.068, 0.046
    u, v = X - cx, Y - cy
    q = np.sqrt((u / rx) ** 2 + (v / ry) ** 2) + 1e-6
    gx, gy = u / (rx * rx) / q, v / (ry * ry) / q
    dist = (q - 1) / (np.sqrt(gx * gx + gy * gy) + 1e-6)
    L.add(col(255, 200, 110), 0.55 * np.exp(-(dist / 0.07) ** 2))
    front = smooth(-0.02, 0.03, v)
    color = lerp3([col(255, 196, 90), col(255, 236, 160)], front)
    L.over(color, aa(np.abs(dist) - th / 2))
    L.add(col(255, 255, 240), 0.55 * aa(np.abs(dist) - th / 6) * (0.4 + 0.6 * front))


def sparkle(L, cx, cy, size, color, strength=1.0, diag=True):
    u, v = X - cx, Y - cy
    r2 = u * u + v * v
    a = np.zeros((N, N), np.float32)
    for p, q in ((u, v), (v, u)):
        ap = np.abs(p) / size
        ap = np.minimum(ap, 1)
        a += np.exp(-(q / (0.012 + 0.05 * size * (1 - ap))) ** 2) * (1 - ap) ** 2.2
    if diag:
        d1, d2 = (u + v) * 0.7071, (u - v) * 0.7071
        for p, q in ((d1, d2), (d2, d1)):
            ap = np.abs(p) / (size * 0.45)
            ap = np.minimum(ap, 1)
            a += 0.5 * np.exp(-(q / (0.008 + 0.03 * size * (1 - ap))) ** 2) * (1 - ap) ** 2.2
    L.add(color, a * strength)
    L.add(col(255, 255, 255), strength * (np.exp(-r2 / (size * size * 0.012)) * 1.2 + 0.35 * np.exp(-r2 / (size * size * 0.08))))


def frame(L, box_sd):
    inside = aa(box_sd)
    # тёмная кромка снаружи и тонкая светлая линия внутри
    L.over(col(14, 8, 28), aa(np.abs(box_sd + 0.012) - 0.012) * inside)
    t = smooth(-1.2, 1.2, X + Y)
    line_col = lerp3([col(255, 224, 150), col(255, 150, 200), col(160, 140, 255)], t)
    line = aa(np.abs(box_sd + 0.042) - 0.007) * inside
    L.add(line_col, 0.25 * np.exp(-((box_sd + 0.042) / 0.03) ** 2) * inside)
    L.over(line_col, line * 0.95)


# ======================= сборка =======================
def render(full=True):
    L = Layer()
    box = rounded_box_sd(0.985, 0.2)
    inside = aa(box)
    if full:
        background(L, box)
        god_rays(L, inside)
        astrolabe(L, front=False)
    else:
        r = np.sqrt(X ** 2 + (Y - OY) ** 2)
        L.over(lerp3([col(80, 36, 130), col(26, 14, 56)], smooth(0.0, 1.2, r)), inside)
    # свечение позади крыльев
    mask = wings(None, mask_only=True)
    g = blur(mask, 22)
    L.add(col(255, 120, 200), 0.75 * g)
    L.add(col(255, 200, 130), 0.45 * blur(mask, 8))
    wings(L)
    # лёгкое сияние поверх крыльев — они светятся, а не просто «нарисованы»
    L.add(col(255, 236, 200), 0.16 * blur(mask, 3))
    sparkle(L, 0.0, 0.06 + OY, 0.42, col(255, 205, 150), 0.8)
    halo(L)
    if full:
        astrolabe(L, front=True)
        for (sx, sy, sz, c) in ((-0.72, -0.66, 0.13, col(150, 220, 255)), (0.74, -0.74, 0.1, col(255, 170, 220)),
                                (-0.8, 0.42, 0.09, col(255, 220, 160)), (0.78, 0.5, 0.12, col(170, 200, 255)),
                                (-0.36, 0.76, 0.08, col(255, 170, 220)), (0.42, 0.8, 0.07, col(255, 235, 200))):
            sparkle(L, sx, sy, sz, c, 0.9, diag=False)
        frame(L, box)
        L.a = np.maximum(L.a, inside)
        L.rgb *= inside[..., None]
        L.a *= inside
    return L


def to_image(L, size):
    rgb = np.clip(L.rgb, 0, 1)
    a = np.clip(L.a, 0, 1)
    # rgb уже premultiplied: усредняем premultiplied, затем делим на альфу
    k = N // size
    pr = rgb.reshape(size, k, size, k, 3).mean(axis=(1, 3))
    pa = a.reshape(size, k, size, k).mean(axis=(1, 3))
    straight = np.where(pa[..., None] > 1e-4, pr / np.maximum(pa[..., None], 1e-4), 0)
    out = np.nan_to_num(np.concatenate([np.clip(straight, 0, 1), pa[..., None]], axis=2))
    return Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")


def sharpen(img, amount=0.35):
    """Лёгкая резкость для маленькой иконки (после усреднения края мягкие)."""
    from PIL import ImageFilter
    return img.filter(ImageFilter.UnsharpMask(radius=1.0, percent=int(amount * 100), threshold=1))


def main():
    full = render(full=True)
    to_image(full, 480).save(os.path.join(ROOT, "icon_workshop.png"))
    sharpen(to_image(full, 80)).save(os.path.join(ROOT, "icon.png"))
    small = render(full=False)
    small.rgb *= aa(rounded_box_sd(0.985, 0.2))[..., None]
    small.a = aa(rounded_box_sd(0.985, 0.2))
    sharpen(to_image(small, 30), 0.25).save(os.path.join(ROOT, "icon_small.png"))
    print("saved icon.png (80), icon_small.png (30), icon_workshop.png (480)")


if __name__ == "__main__":
    main()
