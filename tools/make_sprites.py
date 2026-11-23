"""Генератор спрайтов-заглушек для мода (без внешних библиотек).

Запуск:  python3 tools/make_sprites.py
Любой PNG можно потом перерисовать вручную (Aseprite / Piskel) — главное
сохранить размер и раскладку кадров (см. комментарии у функций).
"""
import math
import os
import random
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


# ======================= минимальный PNG =======================
class Img:
    def __init__(self, w, h, bg=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.px = [[bg for _ in range(w)] for _ in range(h)]

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return (0, 0, 0, 0)

    def set(self, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c if len(c) == 4 else (*c, 255)

    def save(self, rel):
        path = os.path.join(ROOT, rel)
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        raw = b"".join(b"\x00" + bytes(v for p in row for v in p) for row in self.px)

        def chunk(t, d):
            return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)

        data = (b"\x89PNG\r\n\x1a\n"
                + chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9))
                + chunk(b"IEND", b""))
        with open(path, "wb") as f:
            f.write(data)
        print("saved", rel, f"{self.w}x{self.h}")


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def grad3(pal, t):
    """Градиент по трём цветам палитры."""
    return lerp(pal[0], pal[1], t * 2) if t < 0.5 else lerp(pal[1], pal[2], (t - 0.5) * 2)


def outline(img, color, x0=0, y0=0, x1=None, y1=None):
    x1 = img.w if x1 is None else x1
    y1 = img.h if y1 is None else y1
    add = []
    for y in range(y0, y1):
        for x in range(x0, x1):
            if img.get(x, y)[3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if x0 <= nx < x1 and y0 <= ny < y1 and img.get(nx, ny)[3] == 255:
                        add.append((x, y))
                        break
    for x, y in add:
        img.set(x, y, color)


def rect(img, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            img.set(x, y, c)


def line(img, x0, y0, x1, y1, c, thick=1):
    n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(n + 1):
        t = i / n
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        for dx in range(thick):
            for dy in range(thick):
                img.set(round(x) + dx, round(y) + dy, c)


# ======================= крылья =======================
PALETTES = {
    "Starlight": ((170, 235, 255), (120, 150, 255), (170, 90, 230), (40, 30, 90)),
    "Ember":     ((255, 240, 150), (255, 140, 40), (200, 40, 30), (90, 20, 10)),
    "Frost":     ((240, 255, 255), (140, 220, 255), (60, 120, 220), (20, 50, 110)),
    "Sakura":    ((255, 235, 245), (255, 160, 200), (220, 90, 160), (110, 30, 80)),
    "Void":      ((210, 150, 255), (100, 45, 160), (30, 10, 50), (10, 0, 20)),
    "Fairy":     ((235, 255, 225), (140, 240, 170), (60, 180, 160), (20, 80, 60)),
}
SHAPES = {"Starlight": "feather", "Ember": "ember", "Frost": "frost",
          "Sakura": "butterfly", "Void": "bat", "Fairy": "butterfly"}


def draw_wing(img, ax, ay, angle, length, maxw, pal, shape, dim=1.0, clip=None):
    ca, sa = math.cos(angle), math.sin(angle)
    x0, y0, x1, y1 = clip or (0, 0, img.w, img.h)
    for y in range(y0, y1):
        for x in range(x0, x1):
            dx, dy = x + 0.5 - ax, y + 0.5 - ay
            u = dx * ca + dy * sa
            v = -dx * sa + dy * ca
            if not (0 <= u <= length):
                continue
            t = u / length
            if shape == "frost":
                w = maxw * (1 - abs(2 * t - 1)) ** 0.7
                if v > 0:
                    w *= 0.6 + 0.4 * ((u % 5) / 5)
            else:
                w = maxw * math.sin(math.pi * min(1.0, t * 1.05)) ** 0.6
                if v > 0:
                    if shape == "bat":
                        w *= 0.4 + 0.6 * abs(math.sin(u * math.pi / (length / 3)))
                    elif shape == "ember":
                        w *= 0.55 + 0.45 * abs(math.sin(u * math.pi / (length / 6))) ** 2
                    else:
                        w *= 0.7 + 0.3 * abs(math.sin(u * math.pi / (length / 4.5)))
            if abs(v) > w:
                continue
            c = grad3(pal, t)
            if shape == "ember":  # пламя: светлее к центру
                c = lerp(c, pal[0], 1 - abs(v) / max(w, 0.1) - 0.3)
            if shape == "bat" and abs((u % (length / 3)) - 1) < 0.8:
                c = lerp(c, pal[2], 0.6)  # «пальцы» перепонки
            elif v > 0 and int(u) % 6 == 0 and shape in ("feather", "ember"):
                c = lerp(c, pal[2], 0.35)
            if abs(v) < 1.0 and t < 0.8 and shape != "bat":
                c = lerp(c, (255, 255, 255), 0.45)
            img.set(x, y, tuple(int(ch * dim) for ch in c))


def draw_butterfly(img, ax, ay, angle, length, pal, dim=1.0, clip=None):
    x0, y0, x1, y1 = clip or (0, 0, img.w, img.h)
    lobes = [(angle, length * 0.55, length * 0.42, length * 0.3),          # верхнее
             (angle - 0.95, length * 0.35, length * 0.3, length * 0.22)]   # нижнее
    for la, dist, rx, ry in lobes:
        cx, cy = ax + math.cos(la) * dist, ay + math.sin(la) * dist
        ca, sa = math.cos(la), math.sin(la)
        for y in range(y0, y1):
            for x in range(x0, x1):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                u = dx * ca + dy * sa
                v = -dx * sa + dy * ca
                r = (u / rx) ** 2 + (v / ry) ** 2
                if r > 1:
                    continue
                c = grad3(pal, r ** 0.7)
                if 0.25 < r < 0.4:
                    c = lerp(c, (255, 255, 255), 0.5)  # узор-колечко
                img.set(x, y, tuple(int(ch * dim) for ch in c))


def wings_sheet(name):
    """4 кадра 86x56, сложенные сверху вниз (стандарт Terraria)."""
    pal = PALETTES[name]
    shape = SHAPES[name]
    fw, fh = 86, 56
    img = Img(fw, fh * 4)
    angles = [math.pi - 0.35, math.pi + 0.8, math.pi + 0.3, math.pi - 0.3]
    for i, a in enumerate(angles):
        clip = (0, i * fh, fw, (i + 1) * fh)
        ay = i * fh + 30
        if shape == "butterfly":
            draw_butterfly(img, 56, ay - 2, a + 0.25, 30, pal, 0.65, clip)
            draw_butterfly(img, 52, ay, a, 36, pal, 1.0, clip)
        else:
            draw_wing(img, 56, ay - 2, a + 0.25, 30, 7, pal, shape, 0.65, clip)
            draw_wing(img, 52, ay, a, 36, 9, pal, shape, 1.0, clip)
        outline(img, (*pal[3], 255), *clip)
    img.save(f"Content/Items/Wings/{name}Wings_Wings.png")


def wings_item(name):
    pal = PALETTES[name]
    shape = SHAPES[name]
    img = Img(26, 28)
    for a in (math.pi + 0.45, -0.45):
        if shape == "butterfly":
            draw_butterfly(img, 13, 15, a, 14, pal)
        else:
            draw_wing(img, 13, 15, a, 13, 5, pal, shape)
    outline(img, (*pal[3], 255))
    img.save(f"Content/Items/Wings/{name}Wings.png")


# ======================= головные уборы =======================
# Кадр головы 40x56, персонаж смотрит вправо. Макушка примерно y=6..8, x=13..26.
GOLD = (255, 205, 70)
GOLD_D = (215, 150, 40)
GEM = (255, 90, 170)


def hat_crown(img, oy):
    rect(img, 13, 7 + oy, 26, 7 + oy, GOLD)
    rect(img, 13, 8 + oy, 26, 8 + oy, GOLD_D)
    for px, h in [(0, 3), (3, 2), (6, 4), (7, 4), (10, 2), (13, 3)]:
        for k in range(1, h + 1):
            img.set(13 + px, 7 - k + oy, GOLD)
        img.set(13 + px, 7 - h - 1 + oy, (255, 245, 180))
    img.set(19, 7 + oy, GEM)
    img.set(20, 7 + oy, GEM)
    return (110, 60, 20)


def hat_cat_ears(img, oy):
    fur, inner = (70, 65, 80), (255, 150, 190)
    rect(img, 13, 7 + oy, 26, 7 + oy, fur)
    for bx, apex in ((13, 14), (22, 25)):
        for row in range(5):
            y = 6 - row + oy
            half = (4 - row) * 0.6
            for x in range(bx, bx + 5):
                if abs(x - apex) <= half + 0.5:
                    img.set(x, y, fur)
        img.set(apex, 5 + oy, inner)
        img.set(apex, 4 + oy, inner)
    return (25, 20, 30)


def hat_horns(img, oy):
    for base, sign in ((14, -1), (25, 1)):
        pts = [(base, 8), (base + sign * 0.5, 6), (base + sign * 1.5, 4), (base + sign * 3, 2.5), (base + sign * 4, 2)]
        for i in range(len(pts) - 1):
            t = i / (len(pts) - 1)
            c = lerp((150, 30, 40), (40, 10, 15), t)
            line(img, pts[i][0], pts[i][1] + oy, pts[i + 1][0], pts[i + 1][1] + oy, c, 2 if i < 2 else 1)
    return (20, 5, 10)


def hat_wreath(img, oy):
    rect(img, 13, 7 + oy, 26, 8 + oy, (70, 160, 70))
    flowers = [(14, (255, 170, 210)), (18, (255, 240, 120)), (22, (255, 255, 255)), (26, (255, 170, 210))]
    for fx, c in flowers:
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            img.set(fx + dx, 6 + dy + oy, c)
        img.set(fx, 6 + oy, (255, 200, 60))
    return (30, 70, 30)


def hat_tiara(img, oy):
    ice, ice_d, white = (160, 225, 255), (90, 160, 230), (240, 255, 255)
    rect(img, 13, 8 + oy, 26, 8 + oy, ice_d)
    for sx, h in ((16, 2), (20, 5), (23, 2)):
        for k in range(h):
            img.set(sx, 7 - k + oy, ice if k < h - 1 else white)
        img.set(sx - 1, 7 + oy, ice)
        img.set(sx + 1, 7 + oy, ice)
    img.set(20, 6 + oy, (120, 200, 255))
    return (30, 60, 120)


HATS = {"StarlightCrown": hat_crown, "CatEars": hat_cat_ears, "DemonHorns": hat_horns,
        "FlowerWreath": hat_wreath, "FrostTiara": hat_tiara}


def hat_sheets(name):
    """_Head.png: 20 кадров 40x56 (кадры одинаковые, покачивание делает игра)."""
    fn = HATS[name]
    fw, fh = 40, 56
    img = Img(fw, fh * 20)
    for i in range(20):
        oy = i * fh
        oc = fn(img, oy)
        outline(img, (*oc, 255), 0, oy, fw, oy + fh)
    img.save(f"Content/Items/Vanity/{name}_Head.png")

    # Иконка предмета: тот же рисунок, увеличенный в 2 раза
    tmp = Img(fw, fh)
    oc = fn(tmp, 0)
    icon = Img(32, 24)
    for y in range(12):
        for x in range(16):
            c = tmp.get(x + 12, y)
            if c[3]:
                for a in range(2):
                    for b in range(2):
                        icon.set(x * 2 + a, y * 2 + b, c)
    outline(icon, (*oc, 255))
    icon.save(f"Content/Items/Vanity/{name}.png")


# ======================= плащи =======================
CAPES = {
    "StarlightCape": ((90, 110, 230), (40, 30, 110), (255, 230, 120), True),
    "RoyalCape":     ((220, 40, 60), (120, 15, 30), (255, 205, 70), False),
    "ShadowCape":    ((70, 50, 100), (15, 10, 25), (180, 110, 255), False),
}


def draw_cape(img, ox, oy, top, bottom, xr_top, xr_bot, xl_top, xl_bot, c1, c2, trim, stars):
    for y in range(top, bottom + 1):
        t = (y - top) / (bottom - top)
        xl = xl_top + (xl_bot - xl_top) * t ** 0.8 + math.sin(y * 0.7) * 0.6 * t
        xr = xr_top + (xr_bot - xr_top) * t
        for x in range(int(round(xl)), int(round(xr)) + 1):
            c = lerp(c1, c2, t * 0.8 + (xr - x) / max(1, xr - xl) * 0.2)
            if y >= bottom - 1 or x <= round(xl):
                c = trim
            img.set(ox + x, oy + y, c)
    if stars:
        rnd = random.Random(7)
        for _ in range(5):
            y = rnd.randint(top + 3, bottom - 4)
            t = (y - top) / (bottom - top)
            x = rnd.randint(int(xl_top + (xl_bot - xl_top) * t) + 2, int(xr_top + (xr_bot - xr_top) * t) - 1)
            img.set(ox + x, oy + y, (255, 255, 220))


def cape_sheets(name):
    """_Back.png: 20 кадров 40x56. Плащ висит за спиной (персонаж смотрит вправо)."""
    c1, c2, trim, stars = CAPES[name]
    fw, fh = 40, 56
    img = Img(fw, fh * 20)
    for i in range(20):
        oy = i * fh
        draw_cape(img, 0, oy, 19, 46, 21, 21, 13, 6, c1, c2, trim, stars)
        outline(img, (20, 10, 30, 255), 0, oy, fw, oy + fh)
    img.save(f"Content/Items/Vanity/{name}_Back.png")

    icon = Img(24, 28)
    draw_cape(icon, 0, 0, 2, 26, 18, 22, 6, 1, c1, c2, trim, stars)
    rect(icon, 5, 1, 18, 2, trim)
    outline(icon, (20, 10, 30, 255))
    icon.save(f"Content/Items/Vanity/{name}.png")


# ======================= частицы (белые — красятся в коде) =======================
def dust_sheet(name, draw):
    """3 варианта 10x10 сверху вниз."""
    img = Img(10, 30)
    for i in range(3):
        draw(img, i * 10, i)
    img.save(f"Content/Dusts/{name}.png")


def d_heart(img, oy, v):
    rows = ["0110110", "1111111", "1111111", "0111110", "0011100", "0001000"]
    s = 1 if v < 2 else 0
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == "1":
                img.set(1 + x + s, 2 + y + oy, (255, 255, 255) if (x, y) != (1, 1) else (255, 255, 255))
    img.set(2 + s, 3 + oy, (200, 200, 200))


def d_petal(img, oy, v):
    shapes = [["0011", "0111", "1110", "1100"], ["00110", "01111", "11110", "01100"], ["010", "111", "111", "010"]]
    for y, r in enumerate(shapes[v]):
        for x, ch in enumerate(r):
            if ch == "1":
                img.set(3 + x, 3 + y + oy, (255, 255, 255) if y < 2 else (215, 215, 215))


def d_star(img, oy, v):
    size = [4, 3, 2][v]
    for k in range(-size, size + 1):
        g = 255 - abs(k) * 25
        img.set(5 + k, 5 + oy, (g, g, g))
        img.set(5, 5 + k + oy, (g, g, g))
    if v == 0:
        for d in (-1, 1):
            img.set(5 + d, 5 + d + oy, (220, 220, 220))
            img.set(5 + d, 5 - d + oy, (220, 220, 220))


def d_snow(img, oy, v):
    c = (255, 255, 255)
    r = [4, 3, 2][v]
    for k in range(-r, r + 1):
        img.set(5 + k, 5 + oy, c)
        img.set(5, 5 + k + oy, c)
        if abs(k) < r:
            img.set(5 + k, 5 + k + oy, (210, 210, 210))
            img.set(5 + k, 5 - k + oy, (210, 210, 210))


# ======================= эффекты (мягкие белые текстуры) =======================
def soft_glow():
    s = 64
    img = Img(s, s)
    for y in range(s):
        for x in range(s):
            d = math.hypot(x + 0.5 - s / 2, y + 0.5 - s / 2) / (s / 2)
            a = max(0.0, 1 - d) ** 2
            img.set(x, y, (255, 255, 255, int(a * 255)))
    img.save("Assets/Effects/Glow.png")


def ellipse_ring(path, w, h, thick):
    img = Img(w, h)
    rx, ry = w / 2 - 1, h / 2 - 1
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5 - w / 2) / rx, (y + 0.5 - h / 2) / ry
            d = math.hypot(dx, dy)
            edge = abs(d - (1 - thick / 2))
            a = max(0.0, 1 - edge / (thick / 2))
            if a > 0:
                img.set(x, y, (255, 255, 255, int(min(1, a * 1.4) * 255)))
    img.save(path)


# ======================= иконка мода =======================
def icon():
    img = Img(80, 80)
    for y in range(80):
        for x in range(80):
            d = math.hypot(x - 40, y - 40) / 57
            img.set(x, y, lerp((70, 50, 140), (15, 10, 35), d))
    layer = Img(80, 80)
    pal = PALETTES["Starlight"]
    draw_wing(layer, 40, 46, math.pi + 0.5, 34, 11, pal, "feather")
    draw_wing(layer, 40, 46, -0.5, 34, 11, pal, "feather")
    rect(layer, 33, 24, 46, 25, GOLD)
    rect(layer, 33, 26, 46, 26, GOLD_D)
    for px, h in [(0, 3), (3, 2), (6, 5), (7, 5), (10, 2), (13, 3)]:
        for k in range(1, h + 1):
            layer.set(33 + px, 24 - k, GOLD)
    outline(layer, (30, 20, 70, 255))
    for y in range(80):
        for x in range(80):
            if layer.get(x, y)[3]:
                img.set(x, y, layer.get(x, y))
    for x, y in ((12, 14), (66, 10), (70, 62), (10, 66), (56, 24), (22, 30)):
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            img.set(x + dx, y + dy, (255, 255, 255) if (dx, dy) == (0, 0) else (170, 235, 255))
    img.save("icon.png")


if __name__ == "__main__":
    for n in PALETTES:
        wings_sheet(n)
        wings_item(n)
    for n in HATS:
        hat_sheets(n)
    for n in CAPES:
        cape_sheets(n)
    dust_sheet("HeartDust", d_heart)
    dust_sheet("PetalDust", d_petal)
    dust_sheet("StarDust", d_star)
    dust_sheet("SnowflakeDust", d_snow)
    # Текстуры эффектов (свечение, кольца, частицы) теперь делает tools/make_vfx.py
    icon()
