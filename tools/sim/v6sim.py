"""Симуляция эффектов v0.6 (повторяет CelestialAuras.cs, CelestialHalos.cs и новые крылья WingEffects.cs).

Запуск: python3 tools/sim/v6sim.py out_dir
Рисует wings_v6.png, auras_v6.png, halos_v6.png. Яркость 0.8 — как EffectBrightness по умолчанию.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wingsim import Canvas, Pal, Frame, anchored, vivid, shade, lerp, solid, rv, L  # noqa: E402
from wingsim2 import State, build, wave, rot2, TAU  # noqa: E402

BR = 0.8


def add(c, o):
    return (int(c[0] * o * BR), int(c[1] * o * BR), int(c[2] * o * BR), 0)


def frac(x):
    return x - math.floor(x)


def hot(pal, t):
    stops = [pal.Hi, pal.A, pal.B, pal.Lo]
    t = max(0.0, min(1.0, t)) * 3
    i = min(int(t), 2)
    return vivid(stops[i], stops[i + 1], t - i)


def depth(c, pal, far):
    return shade(vivid(c, pal.Lo, 0.45), 0.6, 1.1) if far else c


def line(cv, a, b, color, thick):
    dx, dy = b[0] - a[0], b[1] - a[1]
    ln = math.hypot(dx, dy)
    cv.draw('Line', a, (0, 8), math.atan2(dy, dx), (ln / 32, thick / 16), color, True)


def person(cv, cx, cy):
    cv.rect(cx - 10, cy - 21, cx + 10, cy + 21, (70, 60, 90))
    cv.rect(cx - 7, cy - 21, cx + 7, cy - 9, (200, 160, 130))


# ======================= крылья =======================
def wing_phoenix(cv, pal, f, st, size, aoff, far, time):
    gm = 0.5 if far else 1.0
    for e in build(st, size):
        if far and e['layer'] == 2:
            continue
        ang = e['ang'] + aoff
        base = rot2(e['base'], aoff)
        bp = f.world(base)
        cov = e['layer'] == 2
        name = 'Covert' if cov else 'Feather'
        tl = 30 if cov else 60
        sc = (e['len'] / tl, (0.62 if cov else 0.9) * size)
        heat = [0.3 + 0.55 * e['k'], 0.18 + 0.35 * e['k'], 0.08 + 0.2 * e['k']][e['layer']]
        fill = depth(hot(pal, heat), pal, far)
        anchored(cv, f, name + 'Glow', bp, (2, 8), ang, (sc[0] * 1.08, sc[1] * 1.5), add(pal.B, 0.45 * gm))
        anchored(cv, f, name, bp, (2, 8), ang, sc, solid(fill, 0.5))
        anchored(cv, f, name, bp, (2, 8), ang, sc, add(fill, 0.55 * gm + 0.2))
        if not far and e['layer'] == 0:
            fr = (int(time * 12) + e['i']) % 4
            tip = (base[0] + math.cos(ang) * e['len'] * 0.8, base[1] + math.sin(ang) * e['len'] * 0.8)
            anchored(cv, f, 'Flame', f.world(tip), (16, 46), ang + math.pi / 2, (0.32 * size, 0.38 * size),
                     add(hot(pal, 0.45 + 0.3 * e['k']), 0.55), src=(fr * 32, 0, 32, 48))


def phoenix_tail(cv, pal, f, st, size, time):
    root = (3 * size, 15 * size)
    for s in range(3):
        pos = root
        ang = 1.15 + (s - 1) * 0.3 - st.lean * 0.6 + (1 - st.spread) * 0.15
        seg = (4.6 + (0.5 if s == 1 else 0)) * size
        last = (0, 1)
        for i in range(10):
            t = i / 9
            ang += math.sin(time * 2.6 - i * 0.55 + s * 1.3) * 0.09
            nxt = (pos[0] + math.cos(ang) * seg, pos[1] + math.sin(ang) * seg)
            a, b = f.world(pos), f.world(nxt)
            d = (b[0] - a[0], b[1] - a[1])
            last = d
            width = (1 - t * 0.7) * (1.0 if s == 1 else 0.8)
            cv.draw('Streak', ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2), (64, 16), math.atan2(d[1], d[0]),
                    (math.hypot(*d) * 2 / 128, 0.5 * width * size), add(hot(pal, 0.12 + 0.7 * t), 0.6 * (1 - t * 0.45)), True)
            pos = nxt
        fr = (int(time * 11) + s * 2) % 4
        cv.draw('Flame', f.world(pos), (16, 46), math.atan2(last[1], last[0]) + math.pi / 2, (0.4 * size, 0.4 * size),
                add(hot(pal, 0.6), 0.6), True, src=(fr * 32, 0, 32, 48))


def wing_aurora(cv, pal, f, st, size, aoff, far, time):
    gm = 0.5 if far else 1.0
    for b in range(7):
        k = b / 6
        d = State(st.phase - (1 - k) * 0.6, st.amp, st.spread, st.lean)
        sweep = wave(d.phase) * d.amp * 0.35
        fan = L(L(0.55, 1.15, k), L(-1.0, 0.8, k), st.spread)
        ang = fan + sweep * (1 - k * 0.5) + aoff + st.lean * 0.25
        ln = (24 + 22 * (1 - k) ** 0.8) * size * L(0.8, 1.0, st.spread)
        dv = rv(ang)
        nv = (-dv[1], dv[0])
        prev = f.world((dv[0] * 4 * size, dv[1] * 4 * size))
        for i in range(1, 10):
            t = i / 9
            wv = math.sin(time * 2.4 - t * 4 + b * 0.9) * 4 * size * t
            loc = (dv[0] * (4 * size + ln * t) + nv[0] * wv, dv[1] * (4 * size + ln * t) + nv[1] * wv)
            cur = f.world(loc)
            hue = 0.5 + 0.5 * math.sin(time * 0.9 + t * 2.2 + k * 2.5)
            c = depth(vivid(pal.A, pal.B, hue), pal, far)
            fade = math.sin(t * math.pi * 0.9 + 0.1)
            line(cv, prev, cur, add(c, 0.16 * gm * fade), 18 * size)
            line(cv, prev, cur, add(c, 0.3 * gm * fade), 7 * size)
            line(cv, prev, cur, add(lerp(c, (255, 255, 255), 0.35), 0.45 * gm * fade), 2.8)
            if not far and i % 2 == 0:
                up = (-(cur[1] - prev[1]), cur[0] - prev[0])
                n = math.hypot(*up) or 1
                up = (up[0] / n, up[1] / n)
                if up[1] > 0:
                    up = (-up[0], -up[1])
                cv.draw('AuroraStrip', cur, (16, 118), math.atan2(up[1], up[0]) + math.pi / 2, (0.35 * size, 0.16 * size), add(c, 0.12 * fade), True)
            prev = cur


def wing_nebula(cv, pal, f, st, size, aoff, far, time):
    w = wave(st.phase)
    r = -0.2 + aoff + w * st.amp * 0.5 + (1 - st.spread) * 0.8 + st.lean * 0.25
    fold = L(0.5, 1.0, st.spread)
    sc = (0.58 * size * L(0.7, 1.0, st.spread), 0.58 * size * fold * (0.9 + 0.1 * w * st.amp))
    gm = 0.5 if far else 1.0
    tw = 0.5 + 0.5 * math.sin(time * 2.2)
    anchored(cv, f, 'NebulaWing', f.sh, (6, 40), r, sc, solid((0, 0, 0), 0.25 if far else 0.35))
    anchored(cv, f, 'NebulaWing', f.sh, (6, 40), r, sc, add(depth(pal.B, pal, far), 0.5 * gm + 0.1))
    anchored(cv, f, 'NebulaWing', f.sh, (6, 40), r + 0.05, (sc[0] * 0.95, sc[1] * 0.95), add(depth(pal.A, pal, far), 0.3 * gm))
    anchored(cv, f, 'NebulaRim', f.sh, (6, 40), r, sc, add(depth(pal.Hi, pal, far), 0.65 * gm + 0.1))
    anchored(cv, f, 'NebulaStarsA', f.sh, (6, 40), r, sc, add((255, 255, 255), 0.95 * gm * tw))
    anchored(cv, f, 'NebulaStarsB', f.sh, (6, 40), r, sc, add((255, 255, 255), 0.95 * gm * (1 - tw)))


SHAPE_INDEX = {'Petal': 1, 'PetalRound': 2, 'Leaf': 3, 'Star4': 7, 'Star8': 8, 'Lotus': 22}


def shape_src(name):
    i = SHAPE_INDEX[name]
    return (i % 12 * 32, i // 12 * 32, 32, 32)


def wing_floral(cv, pal, f, st, size, aoff, far, time):
    gm = 0.5 if far else 1.0
    for e in build(st, size):
        ang = e['ang'] + aoff
        base = rot2(e['base'], aoff)
        bp = f.world(base)
        k = [0.3 + 0.4 * e['k'], 0.2 + 0.25 * e['k'], 0.15 + 0.2 * e['k']][e['layer']]
        shape = ['Petal', 'Leaf', 'PetalRound'][e['layer']]
        flutter = math.sin(time * 3 + e['i'] * 1.3 + e['layer']) * 0.05
        rot = ang + math.pi / 2 + flutter
        sc = ([0.95, 0.7, 0.75][e['layer']] * size, e['len'] / 24)
        fill = vivid(pal.B, pal.Hi, 0.08 + 0.15 * e['k']) if e['layer'] == 1 else vivid(pal.A, pal.Hi, k)
        if far:
            fill = shade(fill, 0.68)
        anchored(cv, f, 'ShapesGlow', bp, (16, 28), rot, (sc[0] * 1.12, sc[1] * 1.12), add(pal.B if e['layer'] == 1 else pal.A, 0.3 * gm), src=shape_src(shape))
        anchored(cv, f, 'Shapes', bp, (16, 28), rot, sc, solid(fill, 0.95), src=shape_src(shape))
    if not far:
        spin = math.sin(time * 1.2) * 0.15
        anchored(cv, f, 'ShapesGlow', f.sh, (16, 16), spin, (0.85 * size, 0.85 * size), add(pal.B, 0.5), src=shape_src('Lotus'))
        anchored(cv, f, 'Shapes', f.sh, (16, 16), spin, (0.7 * size, 0.7 * size), solid(pal.Hi), src=shape_src('Lotus'))


def wing_scene(style, pal, st, time=1.3):
    cv = Canvas(110, 90)
    cx, cy = 60, 52
    f = Frame((cx - 5, cy - 7), -1)
    far_f = Frame(f.world((-4, -3)), -1)
    far_st = State(st.phase - 0.3, st.amp, st.spread, st.lean)
    if style == 'Phoenix':
        phoenix_tail(cv, pal, f, st, 1.0, time)
    fn = {'Phoenix': wing_phoenix, 'Aurora': wing_aurora, 'Nebula': wing_nebula, 'Floral': wing_floral}[style]
    fn(cv, pal, far_f, far_st, 0.88, -0.45, True, time)
    person(cv, cx, cy)
    fn(cv, pal, f, st, 1.0, 0.0, False, time)
    return cv


def wings_sheet(path):
    styles = [('Phoenix', Pal((255, 170, 60), (235, 50, 40))), ('Aurora', Pal((90, 255, 180), (150, 110, 255))),
              ('Nebula', Pal((150, 90, 255), (255, 90, 200))), ('Floral', Pal((255, 160, 210), (110, 220, 140)))]
    states = [State(0.0, 1.0, 1.0), State(TAU * 0.6, 1.0, 1.0), State(1.0, 0.3, 1.0), State(1.0, 0.12, 0.15)]
    W, H = 110, 90
    big = Canvas(W * len(states), H * len(styles))
    for si, (name, pal) in enumerate(styles):
        for ci, st in enumerate(states):
            cv = wing_scene(name, pal, st)
            for y in range(H):
                big.px[si * H + y][ci * W:(ci + 1) * W] = cv.px[y]
    big.save(path, 2)


# ======================= ауры =======================
RAD = [46, 38, 30]
TILT = [0.45, 1.3, 0.9]
PREC = [0.32, -0.26, 0.45]
PHASE = [0, 1.4, 2.7]


def basis(r, t):
    phi = t * PREC[r] + PHASE[r]
    st = math.sin(TILT[r])
    n = (st * math.cos(phi), math.cos(TILT[r]), st * math.sin(phi))
    e1 = (n[2], 0.0, -n[0])  # cross(UnitY, n) = (n.z, 0, -n.x)
    l1 = math.sqrt(e1[0] ** 2 + e1[2] ** 2)
    e1 = (e1[0] / l1, 0.0, e1[2] / l1)
    e2 = (n[1] * e1[2] - n[2] * e1[1], n[2] * e1[0] - n[0] * e1[2], n[0] * e1[1] - n[1] * e1[0])
    return e1, e2


def pt(e1, e2, th, rad):
    c, s = math.cos(th), math.sin(th)
    return tuple((e1[i] * c + e2[i] * s) * rad for i in range(3))


def astrolabe(cv, pal, center, t, front):
    s = 1.0
    if not front:
        top, bot = (center[0], center[1] - 58), (center[0], center[1] + 58)
        line(cv, top, bot, add(pal.A, 0.1), 7)
        cv.draw('Glint', top, (32, 32), 0, 0.22, add(pal.Hi, 0.55), True)
    for r in range(3):
        e1, e2 = basis(r, t)
        rad = RAD[r] * s
        rc = pal.A if r == 0 else pal.Hi if r == 1 else pal.B
        for k in range(40):
            a0, a1 = k * TAU / 40, (k + 1) * TAU / 40
            p0, p1 = pt(e1, e2, a0, rad), pt(e1, e2, a1, rad)
            z = (p0[2] + p1[2]) * 0.5 / rad
            if (z >= 0) != front:
                continue
            dp = 0.5 + 0.5 * z
            shine = max(0.0, math.sin(a0 * 0.5 - t * 0.8 - r * 1.3)) ** 12
            c = vivid(rc, pal.Hi, 0.15 + 0.6 * shine)
            sa = (center[0] + p0[0], center[1] - p0[1])
            sb = (center[0] + p1[0], center[1] - p1[1])
            line(cv, sa, sb, add(rc, 0.1 + 0.14 * dp), 10)
            line(cv, sa, sb, add(c, 0.35 + 0.5 * dp + 0.4 * shine), 3.2)
            if r == 0:
                ia = (center[0] + p0[0] * 0.9, center[1] - p0[1] * 0.9)
                ib = (center[0] + p1[0] * 0.9, center[1] - p1[1] * 0.9)
                line(cv, ia, ib, add(rc, 0.18 + 0.3 * dp), 2.4)
                if k % 2 == 0:
                    o = (p0[0], -p0[1])
                    n = math.hypot(*o) or 1
                    ln = 5 if k % 10 == 0 else 2.5
                    line(cv, ia, (ia[0] + o[0] / n * ln, ia[1] + o[1] / n * ln), add(c, 0.3 + 0.4 * dp), 2.6)
        if r == 1:
            for k in range(6):
                p = pt(e1, e2, t * 0.9 + k * TAU / 6, rad)
                if (p[2] >= 0) != front:
                    continue
                dp = 0.5 + 0.5 * p[2] / rad
                pos = (center[0] + p[0], center[1] - p[1])
                cv.draw('Runes', pos, (16, 16), 0, 0.38 * (0.85 + 0.15 * dp), add(vivid(rc, pal.Hi, 0.35), 0.45 + 0.5 * dp), True, src=(k * 32, 0, 32, 32))
        if r == 2:
            for k in range(2):
                p = pt(e1, e2, -t * 1.2 + k * math.pi, rad)
                if (p[2] >= 0) != front:
                    continue
                dp = 0.5 + 0.5 * p[2] / rad
                pos = (center[0] + p[0], center[1] - p[1])
                cv.draw('Bloom', pos, (64, 64), 0, 0.16, add(pal.B, 0.3 * dp), True)
                cv.draw('Orb', pos, (32, 32), 0, 0.15 * (0.8 + 0.2 * dp), add(pal.Hi if k == 0 else pal.A, 0.6 + 0.4 * dp), True)


def aurora(cv, pal, center, t):
    for layer in range(2):
        far = layer == 0
        width = 150 * (0.85 if far else 1)
        base_y = -(21 + (28 if far else 16))
        for i in range(30):
            u = i / 29
            x = (u - 0.5) * width
            env = math.sin(u * math.pi) ** 0.7
            fold = math.sin(x * 0.045 + t * 1.1 + layer * 1.7) * 7 + math.sin(x * 0.11 - t * 1.6 + layer) * 3.5
            sway = math.sin(t * 0.7 + x * 0.03 + layer) * 5
            h = (54 + 22 * math.sin(x * 0.06 + t * 0.5 + layer * 2)) * (0.8 if far else 1)
            pos = (center[0] + x + sway, center[1] + base_y + fold)
            hue = 0.5 + 0.5 * math.sin(x * 0.025 + t * 0.6 + layer * 2.1)
            lower = vivid(pal.A, pal.B, hue * 0.35)
            upper = vivid(pal.B, pal.A, hue * 0.3)
            ray = 0.5 + 0.5 * math.sin(i * 1.93 + t * 0.9 + layer) * math.sin(i * 0.71 - t * 0.6)
            alpha = env * (0.22 if far else 0.34) * (0.45 + 0.55 * ray)
            sc = (width / 30 * 2.6 / 32, h * (0.8 + 0.35 * ray) / 128)
            cv.draw('AuroraStrip', pos, (16, 118), 0, sc, add(lower, alpha), True)
            cv.draw('AuroraStrip', pos, (16, 118), 0, (sc[0] * 1.15, sc[1] * 1.5), add(upper, alpha * 0.55), True)


def galaxy(cv, pal, center, t, front):
    f = t * 2
    i0 = int(math.floor(f)) % 32
    bl = f - math.floor(f)
    pos = (center[0], center[1] + 6)
    alpha = 0.55 if front else 1.0
    for k in range(2):
        idx = (i0 + k) % 32
        fx, fy = (idx & 7) * 160, ((idx >> 3) & 3) * 64
        w = 1 - bl if k == 0 else bl
        half = (fx, fy + 32, 160, 32) if front else (fx, fy, 160, 32)
        org = (80, 0 if front else 32)
        cv.draw('Galaxy', pos, org, 0, 0.9 * 1.08, add(pal.B, alpha * w * 0.45), True, src=half)
        cv.draw('Galaxy', pos, org, 0, 0.9, add(pal.A, alpha * w), True, src=half)
    if not front:
        cv.draw('Bloom', pos, (64, 64), 0, (1.1, 0.42), add(pal.B, 0.25), True)
        cv.draw('Bloom', pos, (64, 64), 0, 0.22, add(pal.Hi, 0.4), True)


def eclipse(cv, pal, center, t):
    pos = (center[0], center[1] - 8)
    rad = 26
    cv.draw('Bloom', pos, (64, 64), 0, 1.15, add(pal.B, 0.3), True)
    cv.draw('Corona', pos, (64, 64), -t * 0.07, rad / 23 * 1.25, add(pal.B, 0.55), True)
    cv.draw('Corona', pos, (64, 64), t * 0.05, rad / 23, add(pal.A, 0.95), True)
    for k in range(5):
        a = t * 0.12 + k * TAU / 5 + math.sin(t * 0.4 + k) * 0.3
        d = rv(a)
        fr = (int(t * 9) + k) % 4
        cv.draw('Flame', (pos[0] + d[0] * (rad - 2), pos[1] + d[1] * (rad - 2)), (16, 46), a + math.pi / 2, (0.38, 0.32),
                add(vivid(pal.A, pal.B, 0.4), 0.55), True, src=(fr * 32, 0, 32, 48))
    cv.draw('Disc', pos, (32, 32), 0, rad / 30, solid((4, 2, 10), 0.96), False)
    cv.draw('RingWave', pos, (64, 64), 0, rad / 53.8, add(pal.Hi, 0.55), True)
    da = t * 0.3
    dm = (pos[0] + math.cos(da) * rad, pos[1] + math.sin(da) * rad)
    cv.draw('Bloom', dm, (64, 64), 0, 0.2, add(pal.Hi, 0.4), True)
    cv.draw('Glint', dm, (32, 32), 0, 0.55, add((255, 255, 255), 0.9), True)


def mandala(cv, pal, center, t):
    cv.draw('Bloom', center, (64, 64), 0, 1.25, add(pal.B, 0.18), True)
    cv.draw('MandalaOuter', center, (128, 128), t * 0.08, 0.58 * 1.015, add(pal.B, 0.22), True)
    cv.draw('MandalaOuter', center, (128, 128), t * 0.08, 0.58, add(pal.A, 0.5), True)
    cv.draw('MandalaInner', center, (128, 128), -t * 0.13, 0.55, add(vivid(pal.A, pal.B, 0.6), 0.5), True)
    cv.draw('MandalaInner', center, (128, 128), t * 0.25, 0.22, add(pal.Hi, 0.35), True)


def auras_sheet(path):
    W, H = 200, 190
    items = [('Astrolabe', Pal((255, 205, 110), (120, 170, 255))), ('Aurora', Pal((90, 255, 170), (170, 100, 255))),
             ('Galaxy', Pal((150, 170, 255), (255, 120, 200))), ('Eclipse', Pal((255, 190, 90), (255, 80, 60))),
             ('Mandala', Pal((255, 200, 110), (255, 110, 190)))]
    big = Canvas(W * len(items), H)
    for i, (name, pal) in enumerate(items):
        cv = Canvas(W, H)
        c = (W / 2, H / 2 + 20)
        t = 3.7
        if name == 'Astrolabe':
            astrolabe(cv, pal, c, t, False)
        elif name == 'Aurora':
            aurora(cv, pal, c, t)
        elif name == 'Galaxy':
            galaxy(cv, pal, c, t, False)
        elif name == 'Eclipse':
            eclipse(cv, pal, c, t)
        else:
            mandala(cv, pal, c, t)
        person(cv, int(c[0]), int(c[1]))
        if name == 'Astrolabe':
            astrolabe(cv, pal, c, t, True)
        elif name == 'Galaxy':
            galaxy(cv, pal, c, t, True)
        for y in range(H):
            big.px[y][i * W:(i + 1) * W] = cv.px[y]
    big.save(path, 2)


# ======================= нимбы =======================
def halos_sheet(path):
    W, H = 70, 70
    pal = Pal((255, 210, 120), (255, 120, 170))
    big = Canvas(W * 4, H)
    t = 2.1
    for i, name in enumerate(['RuneCrown', 'Diadem', 'Horns', 'Eclipse']):
        cv = Canvas(W, H)
        cx, cy = 35, 45
        head_top = (cx, cy - 21)
        direction = 1
        # задняя часть
        if name == 'RuneCrown':
            pos = (head_top[0], head_top[1] - 4)
            cv.draw('HaloRing', pos, (16, 6), 0, 30 / 27, add(pal.A, 0.45), True)
            for k in range(8):
                a = t * 1.1 + k * TAU / 8
                if math.sin(a) < 0:
                    dp = 0.5 + 0.5 * math.sin(a)
                    cv.draw('Runes', (pos[0] + math.cos(a) * 15, pos[1] + math.sin(a) * 4.5), (16, 16), 0, 0.3 * (0.8 + 0.2 * dp),
                            add(vivid(pal.A, pal.Hi, 0.25 + 0.35 * dp), 0.45 + 0.55 * dp), True, src=(k * 32, 0, 32, 32))
        if name == 'Horns':
            horn(cv, pal, (head_top[0] - direction * 5, head_top[1] + 1), direction, t + 0.4, 0.9, True)
        if name == 'Eclipse':
            pos = (cx - direction * 2, cy - 21 + 9)
            r = 12
            cv.draw('Bloom', pos, (64, 64), 0, 0.55, add(pal.B, 0.3), True)
            cv.draw('Corona', pos, (64, 64), 0, r / 23 * 1.3, add(pal.B, 0.55), True)
            cv.draw('Corona', pos, (64, 64), 0.5, r / 23, add(pal.A, 0.95), True)
            cv.draw('Disc', pos, (32, 32), 0, r / 30, solid((4, 2, 10), 0.96), False)
            cv.draw('RingWave', pos, (64, 64), 0, r / 53.8, add(pal.Hi, 0.6), True)
        person(cv, cx, cy)
        if name == 'RuneCrown':
            pos = (head_top[0], head_top[1] - 4)
            for k in range(8):
                a = t * 1.1 + k * TAU / 8
                if math.sin(a) >= 0:
                    dp = 0.5 + 0.5 * math.sin(a)
                    cv.draw('Runes', (pos[0] + math.cos(a) * 15, pos[1] + math.sin(a) * 4.5), (16, 16), 0, 0.3 * (0.8 + 0.2 * dp),
                            add(vivid(pal.A, pal.Hi, 0.25 + 0.35 * dp), 0.45 + 0.55 * dp), True, src=(k * 32, 0, 32, 32))
        if name == 'Diadem':
            base = (head_top[0], head_top[1] + 1)
            cv.draw('HaloRing', base, (16, 6), 0, (24 / 27, 0.8), add(pal.A, 0.75), True)
            for j, (x, h, sz) in enumerate(zip((-10, -5, 0, 5, 10), (4, 7, 10, 7, 4), (0.32, 0.4, 0.55, 0.4, 0.32))):
                foot = (base[0] + x, base[1] + 1.5)
                star = (base[0] + x, base[1] - h)
                line(cv, foot, star, add(pal.A, 0.55), 3)
                tw = max(0.0, math.sin(t * 2.6 - j * 0.9)) ** 8
                src = shape_src('Star8' if j == 2 else 'Star4')
                cv.draw('ShapesGlow', star, (16, 16), 0, sz * 1.2, add(pal.B, 0.55), True, src=src)
                cv.draw('Shapes', star, (16, 16), 0, sz * (1 + 0.15 * tw), add(vivid(pal.A, pal.Hi, 0.4 + 0.6 * tw), 0.95), True, src=src)
        if name == 'Horns':
            horn(cv, pal, (head_top[0] + direction * 3, head_top[1] + 3), direction, t, 1.0, False)
        for y in range(H):
            big.px[y][i * W:(i + 1) * W] = cv.px[y]
    big.save(path, 4)


def horn(cv, pal, base, direction, t, size, far):
    p1 = (base[0] + direction * 2 * size, base[1] - 12 * size)
    p2 = (base[0] - direction * (9 if far else 12) * size, base[1] - (21 if far else 19) * size)
    pulse_at = frac(t * 0.6)
    prev = base
    for i in range(1, 13):
        tt = i / 12
        u = 1 - tt
        p = (u * u * base[0] + 2 * u * tt * p1[0] + tt * tt * p2[0], u * u * base[1] + 2 * u * tt * p1[1] + tt * tt * p2[1])
        mid = tt - 0.5 / 12
        width = (1 - mid) ** 0.8 * 9 + 1.5
        pulse = math.exp(-((mid - pulse_at) * 5) ** 2)
        core = vivid(pal.A, pal.Hi, min(1, 0.2 + 0.6 * mid + 0.3 * pulse))
        if far:
            core = shade(vivid(core, pal.Lo, 0.4), 0.7)
        alpha = 0.5 if far else 0.9
        line(cv, prev, p, add(pal.Lo if far else pal.B, 0.25 * alpha), width * 2.4 * size)
        line(cv, prev, p, add(core, alpha * (0.7 + 0.5 * pulse)), width * size)
        prev = p


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    wings_sheet(os.path.join(out, 'wings_v6.png'))
    auras_sheet(os.path.join(out, 'auras_v6.png'))
    halos_sheet(os.path.join(out, 'halos_v6.png'))
    print('ok')
