"""Симуляция спутников v0.6 (повторяет CompanionEffects.cs): огонёк, лисица, бабочка, змей."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wingsim import Canvas, Pal, vivid, solid  # noqa: E402
from v6sim import add, line, shape_src, person  # noqa: E402

TAU = math.tau


def trail_of(path_fn, t, n=32, dt=1 / 60):
    return [path_fn(t - i * dt) for i in range(n)]


def wisp(cv, pal, pos_fn, t, s=1.0, facing=1):
    tr = trail_of(pos_fn, t)
    for i in range(10, 0, -1):
        k = i / 10
        p = tr[i]
        tp = (p[0] + math.sin(t * 5 + i * 0.6) * i * 0.25 * s, p[1] - i * 1.3 * s)
        c = vivid(pal.A, pal.B, k)
        cv.draw('Bloom', tp, (64, 64), 0, 0.2 * (1 - k * 0.6) * s, add(pal.B, 0.2 * (1 - k)), True)
        cv.draw('Orb', tp, (32, 32), 0, 0.2 * (1 - k * 0.75) * s, add(c, 0.6 * (1 - k)), True)
    h = tr[0]
    cv.draw('Bloom', h, (64, 64), 0, 0.42 * s, add(pal.B, 0.4), True)
    cv.draw('Orb', h, (32, 32), 0, 0.32 * s, add(pal.A, 0.95), True)
    cv.draw('Orb', h, (32, 32), 0, 0.15 * s, add((255, 255, 255), 0.6), True)
    for e in (-1, 1):
        cv.draw('Disc', (h[0] + (facing * 1.5 + e * 2.6) * s, h[1] - 0.5 * s), (32, 32), 0, (0.042 * s, 0.06 * s), solid((30, 14, 40), 0.9), False)


def fox(cv, pal, body, t, s=1.0, face=1, tilt=0.0):
    sc = 0.62 * s
    root = (body[0] - 18 * face * sc, body[1] - 1 * sc)
    for j in range(3):
        ang = (math.pi + 0.15 + j * 0.32 if face > 0 else -0.15 - j * 0.32) + tilt
        pos = root
        d = (math.cos(ang), math.sin(ang))
        for i in range(9):
            k = (i + 0.5) / 9
            a = ang + math.sin(t * 3 + j * 1.1 + i * 0.5) * 0.12 * i / 9 * face
            d = (math.cos(a), math.sin(a))
            pos = (pos[0] + d[0] * 3.6 * s, pos[1] + d[1] * 3.6 * s)
            r = (1.4 + 2.6 * math.sin(math.pi * k) ** 0.7) * s * (1 - 0.3 * k)
            c = vivid(pal.A, pal.Hi, k * 0.9)
            cv.draw('Bloom', pos, (64, 64), 0, r * 2.4 / 64, add(pal.B, 0.07), True)
            cv.draw('Orb', pos, (32, 32), 0, r / 11, add(c, 0.32), True)
        fr = (int(t * 10) + j) % 4
        cv.draw('Flame', pos, (16, 46), math.atan2(d[1], d[0]) + math.pi / 2, (0.22 * s, 0.24 * s), add(vivid(pal.Hi, pal.B, 0.4), 0.6), True, src=(fr * 32, 0, 32, 48))
    cv.draw('FoxGlow', body, (32, 24), tilt, sc * 1.04, add(pal.B, 0.5), True, flipH=face < 0)
    cv.draw('Fox', body, (32, 24), tilt, sc, add(vivid(pal.A, pal.Hi, 0.25), 0.9), True, flipH=face < 0)
    eye = (body[0] + 17.3 * face * sc, body[1] - 11.8 * sc)
    cv.draw('Glint', eye, (32, 32), 0, 0.16 * s, add((255, 255, 255), 0.75), True)


def butterfly(cv, pal, body, t, s=1.0):
    flap = math.sin(t * 16)
    op = 0.2 + 0.8 * abs(flap)
    sc = 0.42 * s
    for side in (-1, 1):
        org = (60 if side < 0 else 4, 32)
        fh = side < 0
        cv.draw('ButterflyGlow', body, org, 0, (sc * op * 1.06, sc * 1.06), add(pal.B, 0.55), True, flipH=fh)
        cv.draw('ButterflyWing', body, org, 0, (sc * op, sc), solid(pal.A, 0.9), False, flipH=fh)
        cv.draw('ButterflyPattern', body, org, 0, (sc * op, sc), solid(pal.B), False, flipH=fh)
    cv.draw('Streak', body, (64, 16), math.pi / 2, (0.14 * s, 0.16 * s), add(pal.Hi, 0.9), True)


def serpent(cv, pal, path, t, s=1.0):
    tr = trail_of(path, t)
    for j in range(13, 0, -1):
        a, b = tr[j * 2], tr[(j - 1) * 2]
        k = j / 13
        line(cv, a, b, add(vivid(pal.A, pal.B, k), 0.35), (9 - 5 * k) * s)
    for j in range(13, -1, -1):
        idx = j * 2
        sp = tr[idx]
        k = j / 13
        r = (5.5 - 3.6 * k) * s
        c = vivid(pal.A, pal.B, k)
        cv.draw('Bloom', sp, (64, 64), 0, r * 2.2 / 64, add(pal.B, 0.2), True)
        cv.draw('Orb', sp, (32, 32), 0, r / 11, add(c, 0.8), True)
        if j % 3 == 1 and j < 13:
            p0, p1 = tr[max(0, idx - 1)], tr[min(31, idx + 1)]
            d = (p0[0] - p1[0], p0[1] - p1[1])
            n = math.hypot(*d) or 1
            d = (d[0] / n, d[1] / n)
            perp = (-d[1], d[0])
            for f in (-1, 1):
                tip = (sp[0] + (perp[0] * f - d[0] * 0.8) * r * 1.6, sp[1] + (perp[1] * f - d[1] * 0.8) * r * 1.6)
                line(cv, sp, tip, add(pal.Hi, 0.5), 3 * s)
    h = tr[0]
    d = (tr[0][0] - tr[2][0], tr[0][1] - tr[2][1])
    n = math.hypot(*d) or 1
    d = (d[0] / n, d[1] / n)
    rot = math.atan2(d[1], d[0])
    cv.draw('ShapesGlow', h, (16, 16), rot, 0.95 * s, add(pal.B, 0.6), True, src=(23 % 12 * 32, 23 // 12 * 32, 32, 32))
    cv.draw('Shapes', h, (16, 16), rot, 0.8 * s, add(vivid(pal.A, pal.Hi, 0.4), 0.95), True, src=(23 % 12 * 32, 23 // 12 * 32, 32, 32))


if __name__ == '__main__':
    W, H = 150, 120
    big = Canvas(W * 4, H)
    pal = Pal((140, 220, 255), (190, 120, 255))
    cx, cy = 75, 70
    t = 2.0
    cvs = []
    cv = Canvas(W, H); person(cv, cx, cy)
    wisp(cv, pal, lambda tt: (cx + 28 + math.sin(tt * 1.7) * 6, cy - 30 + math.sin(tt * 2.3) * 5 - 0 * tt), t, 1.0, 1)
    cvs.append(cv)
    cv = Canvas(W, H); fox(cv, Pal((255, 190, 120), (255, 110, 80)), (cx - 36, cy - 10), t, 1.0, 1); person(cv, cx, cy); cvs.append(cv)
    cv = Canvas(W, H); person(cv, cx, cy); butterfly(cv, Pal((255, 150, 210), (150, 120, 255)), (cx + 20, cy - 34), t); cvs.append(cv)
    cv = Canvas(W, H)
    serpent(cv, Pal((120, 230, 255), (255, 200, 120)), lambda tt: (cx + math.sin(tt * 1.05 * 3) * 50, cy - 14 + math.sin(tt * 2.1 * 3) * 20), t)
    person(cv, cx, cy); cvs.append(cv)
    for i, c in enumerate(cvs):
        for y in range(H):
            big.px[y][i * W:(i + 1) * W] = c.px[y]
    big.save(sys.argv[1] if len(sys.argv) > 1 else 'companions.png', 2)
    print('ok')
