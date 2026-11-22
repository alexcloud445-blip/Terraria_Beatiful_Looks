"""Генератор базовых спрайтов-заглушек для мода (без внешних библиотек).

Запуск:  python3 tools/make_sprites.py
Готовые PNG можно потом перерисовать вручную (Aseprite / Piskel) — размеры
и раскладку кадров при этом нужно сохранить.
"""
import math
import os
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


# ---------- минимальный PNG ----------
class Img:
    def __init__(self, w, h, bg=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.px = [[bg for _ in range(w)] for _ in range(h)]

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return (0, 0, 0, 0)

    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c if len(c) == 4 else (*c, 255)

    def save(self, rel):
        path = os.path.join(ROOT, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
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
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def outline(img, color, x0=0, y0=0, x1=None, y1=None):
    """Обводит непрозрачные пиксели в заданной области."""
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


# ---------- крыло ----------
LIGHT = (170, 235, 255)
MID = (120, 150, 255)
DEEP = (170, 90, 230)


def draw_wing(img, ax, ay, angle, length, maxw, dim=1.0, clip=None):
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
            w = maxw * math.sin(math.pi * min(1.0, t * 1.05)) ** 0.6
            if v > 0:  # задний край — «перья»
                w *= 0.7 + 0.3 * abs(math.sin(u * math.pi / (length / 4.5)))
            if abs(v) > w:
                continue
            c = lerp(LIGHT, MID, t * 1.4) if t < 0.7 else lerp(MID, DEEP, (t - 0.7) / 0.3)
            if v > 0 and int(u) % 6 == 0:
                c = lerp(c, DEEP, 0.35)
            if abs(v) < 1.0 and t < 0.8:
                c = lerp(c, (255, 255, 255), 0.5)
            c = tuple(int(ch * dim) for ch in c)
            img.set(x, y, c)


def sparkle(img, x, y, c=(255, 255, 255)):
    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
        img.set(x + dx, y + dy, c if (dx, dy) == (0, 0) else lerp(c, LIGHT, 0.5))


def wings_sheet():
    """4 кадра 86x56 сверху вниз — стандартная раскладка крыльев Terraria."""
    fw, fh = 86, 56
    img = Img(fw, fh * 4)
    angles = [math.pi - 0.35, math.pi + 0.8, math.pi + 0.3, math.pi - 0.3]
    for i, a in enumerate(angles):
        clip = (0, i * fh, fw, (i + 1) * fh)
        ay = i * fh + 30
        draw_wing(img, 56, ay - 2, a + 0.25, 30, 7, dim=0.65, clip=clip)  # дальнее крыло
        draw_wing(img, 52, ay, a, 36, 9, clip=clip)                        # ближнее
        outline(img, (40, 30, 90, 255), *clip)
    img.save("Content/Items/Wings/StarlightWings_Wings.png")


def wings_item():
    img = Img(26, 28)
    draw_wing(img, 13, 15, math.pi + 0.45, 13, 5)
    draw_wing(img, 13, 15, -0.45, 13, 5)
    outline(img, (40, 30, 90, 255))
    img.save("Content/Items/Wings/StarlightWings.png")


# ---------- корона ----------
GOLD = (255, 205, 70)
GOLD_D = (215, 150, 40)
GEM = (255, 90, 170)


def draw_crown(img, x0, base_y, width, band_h, peaks, oy=0):
    for x in range(x0, x0 + width):
        for y in range(base_y, base_y + band_h):
            img.set(x, y + oy, GOLD if y < base_y + band_h - 1 else GOLD_D)
    for px, h in peaks:
        for k in range(1, h + 1):
            img.set(x0 + px, base_y - k + oy, GOLD)
        img.set(x0 + px, base_y - h - 1 + oy, (255, 245, 180))


def crown_head():
    """20 кадров 40x56 — раскладка шлема (одинаковые кадры, покачивание делает игра)."""
    fw, fh = 40, 56
    img = Img(fw, fh * 20)
    for i in range(20):
        oy = i * fh
        draw_crown(img, 13, 7, 14, 2, [(0, 3), (3, 2), (6, 4), (7, 4), (10, 2), (13, 3)], oy)
        img.set(19, 7 + oy, GEM)
        img.set(20, 7 + oy, GEM)
        outline(img, (110, 60, 20, 255), 0, oy, fw, oy + fh)
    img.save("Content/Items/Vanity/StarlightCrown_Head.png")


def crown_item():
    img = Img(18, 18)
    draw_crown(img, 2, 10, 14, 4, [(0, 5), (1, 4), (4, 3), (6, 6), (7, 6), (9, 3), (12, 4), (13, 5)])
    for x, y in ((8, 11), (9, 11), (8, 12), (9, 12)):
        img.set(x, y, GEM)
    img.set(4, 12, (120, 200, 255))
    img.set(13, 12, (120, 200, 255))
    outline(img, (110, 60, 20, 255))
    img.save("Content/Items/Vanity/StarlightCrown.png")


# ---------- иконка мода ----------
def icon():
    img = Img(80, 80)
    for y in range(80):
        for x in range(80):
            d = math.hypot(x - 40, y - 40) / 57
            img.set(x, y, lerp((70, 50, 140), (15, 10, 35), min(1, d)))
    layer = Img(80, 80)
    draw_wing(layer, 40, 46, math.pi + 0.5, 34, 11)
    draw_wing(layer, 40, 46, -0.5, 34, 11)
    draw_crown(layer, 33, 24, 14, 3, [(0, 3), (3, 2), (6, 5), (7, 5), (10, 2), (13, 3)])
    outline(layer, (30, 20, 70, 255))
    for y in range(80):
        for x in range(80):
            if layer.get(x, y)[3]:
                img.set(x, y, layer.get(x, y))
    for x, y in ((12, 14), (66, 10), (70, 62), (10, 66), (56, 24), (22, 30)):
        sparkle(img, x, y)
    img.save("icon.png")


if __name__ == "__main__":
    wings_sheet()
    wings_item()
    crown_head()
    crown_item()
    icon()
