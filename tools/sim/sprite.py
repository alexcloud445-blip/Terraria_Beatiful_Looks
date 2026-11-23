"""Мини-«SpriteBatch» на чистом Python для проверки эффектов без игры.

Повторяет смешивание Terraria: текстуры premultiplied, цвет умножается на текстуру,
итог = src + dst * (1 - srcA). Цвет с A = 0 (как VfxMath.Additive) — чистое сложение света.
    t = Target(320, 240, bg=(14, 12, 28))
    t.draw(tex, (x, y), color=(255, 200, 120, 0), rotation=0.3, origin=(16, 16), scale=(1, 0.5))
    t.save("out.png", upscale=2)
"""
import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import pngio  # noqa: E402

_CACHE = {}


def tex(path):
    if path not in _CACHE:
        im = pngio.load(path)
        _CACHE[path] = im
    return _CACHE[path]


class Target:
    def __init__(self, w, h, bg=(12, 10, 24)):
        self.w, self.h = w, h
        self.r = [bg[0] / 255.0] * (w * h)
        self.g = [bg[1] / 255.0] * (w * h)
        self.b = [bg[2] / 255.0] * (w * h)

    def draw(self, im, pos, color=(255, 255, 255, 255), rotation=0.0, origin=None, scale=1.0, src=None, flip_x=False, flip_y=False):
        """im — pngio.Img; pos — экранная точка, куда попадает origin; scale — число или (sx, sy)."""
        sx0, sy0, sw, sh = src if src else (0, 0, im.w, im.h)
        if origin is None:
            origin = (sw / 2, sh / 2)
        if not isinstance(scale, (tuple, list)):
            scale = (scale, scale)
        kx, ky = scale
        if abs(kx) < 1e-6 or abs(ky) < 1e-6:
            return
        cr, cg, cb, ca = (c / 255.0 for c in color)
        cs, sn = math.cos(rotation), math.sin(rotation)
        # габариты на экране
        corners = []
        for lx, ly in ((0, 0), (sw, 0), (0, sh), (sw, sh)):
            x, y = (lx - origin[0]) * kx, (ly - origin[1]) * ky
            corners.append((pos[0] + x * cs - y * sn, pos[1] + x * sn + y * cs))
        x0 = max(0, int(min(c[0] for c in corners)) - 1)
        x1 = min(self.w, int(max(c[0] for c in corners)) + 2)
        y0 = max(0, int(min(c[1] for c in corners)) - 1)
        y1 = min(self.h, int(max(c[1] for c in corners)) + 2)
        px = im.px
        for py in range(y0, y1):
            for qx in range(x0, x1):
                dx, dy = qx + 0.5 - pos[0], py + 0.5 - pos[1]
                lx = (dx * cs + dy * sn) / kx + origin[0]
                ly = (-dx * sn + dy * cs) / ky + origin[1]
                if lx < 0 or ly < 0 or lx >= sw or ly >= sh:
                    continue
                if flip_x:
                    lx = sw - lx
                if flip_y:
                    ly = sh - ly
                # билинейная выборка
                fx, fy = lx - 0.5, ly - 0.5
                ix, iy = int(math.floor(fx)), int(math.floor(fy))
                ax, ay = fx - ix, fy - iy
                acc = [0.0, 0.0, 0.0, 0.0]
                for oy, wy in ((0, 1 - ay), (1, ay)):
                    yy = min(max(iy + oy, 0), sh - 1) + sy0
                    for ox, wx in ((0, 1 - ax), (1, ax)):
                        xx = min(max(ix + ox, 0), sw - 1) + sx0
                        p = px[yy][xx]
                        w = wx * wy
                        acc[0] += p[0] * w
                        acc[1] += p[1] * w
                        acc[2] += p[2] * w
                        acc[3] += p[3] * w
                sr, sg, sb, sa = acc[0] / 255 * cr, acc[1] / 255 * cg, acc[2] / 255 * cb, acc[3] / 255 * ca
                i = py * self.w + qx
                k = 1 - sa
                self.r[i] = sr + self.r[i] * k
                self.g[i] = sg + self.g[i] * k
                self.b[i] = sb + self.b[i] * k

    def save(self, path, upscale=1):
        img = pngio.Img(self.w * upscale, self.h * upscale)
        for y in range(self.h):
            for x in range(self.w):
                i = y * self.w + x
                c = (min(255, int(self.r[i] * 255)), min(255, int(self.g[i] * 255)), min(255, int(self.b[i] * 255)), 255)
                for yy in range(upscale):
                    row = img.px[y * upscale + yy]
                    for xx in range(upscale):
                        row[x * upscale + xx] = c
        img.save(path)


def additive(rgb, opacity=1.0, brightness=0.8):
    k = opacity * brightness
    return (rgb[0] * k, rgb[1] * k, rgb[2] * k, 0)


def mul(rgb, opacity=1.0):
    return (rgb[0] * opacity, rgb[1] * opacity, rgb[2] * opacity, 255 * opacity)
