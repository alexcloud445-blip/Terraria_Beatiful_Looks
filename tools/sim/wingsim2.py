"""Симуляция крыльев v2 (новая модель и анимация) — эталон для переноса в WingEffects.cs."""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wingsim import Canvas, Pal, Frame, anchored, vivid, shade, lerp, add, solid, rv, L, tex

TAU = math.tau


def frac(x): return x - math.floor(x)
def ease(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


def wave(ph):
    p = frac(ph / TAU)
    if p < 0.4:
        return -1 + 2 * ease(p / 0.4)
    return 1 - 2 * ease((p - 0.4) / 0.6)


def wave_vel(ph):
    return (wave(ph) - wave(ph - 0.25)) / 0.25


class State:
    def __init__(s, phase, amp, spread, lean=0.0):
        s.phase, s.amp, s.spread, s.lean = phase, amp, spread, lean


def arm_angle(st, lag):
    return -0.35 + wave(st.phase - lag) * st.amp * 0.6 + (1 - st.spread) * 0.55 + st.lean * 0.25


def fore_angle(st, lag):
    return arm_angle(st, lag + 0.35) + L(1.25, 0.12, st.spread)


def skeleton(st, size, lag):
    a = rv(arm_angle(st, lag))
    w = (a[0] * 13 * size, a[1] * 13 * size)
    f = rv(fore_angle(st, lag))
    l2 = 16 * size * L(0.85, 1.0, st.spread)
    t = (w[0] + f[0] * l2, w[1] + f[1] * l2)
    return w, t


def onarm(u, w, t):
    if u < 0.5:
        k = u / 0.5
        return (w[0] * k, w[1] * k)
    k = (u - 0.5) / 0.5
    return (w[0] + (t[0] - w[0]) * k, w[1] + (t[1] - w[1]) * k)


def build(st, size):
    els = []
    sp = st.spread
    rows = [  # (layer, count, u0, u1)
        (0, 7, 0.5, 0.98),
        (1, 6, 0.08, 0.5),
        (2, 6, 0.05, 0.8),
    ]
    for layer, count, u0, u1 in rows:
        for i in range(count):
            k = i / (count - 1)
            u = L(u0, u1, k)
            lag = u * 0.75
            w, t = skeleton(st, size, lag)
            fa = fore_angle(st, lag)
            bend = -wave_vel(st.phase - lag) * st.amp * 0.07 * (0.3 + u)
            if layer == 0:
                ang = L(fa + L(0.3, 1.15, sp), fa - 0.1, k) + bend
                ln = L(18, 28, math.sin(k * math.pi / 2)) * size * L(0.9, 1.0, sp)
            elif layer == 1:
                ang = L(L(1.15, 1.45, sp), fa + L(0.35, 1.2, sp), k) + bend * 0.6
                ln = L(14, 18, k) * size
            else:
                ang = L(1.0, fa + 0.55, k) + bend * 0.3
                ln = L(9, 12, k) * size
            els.append(dict(base=onarm(u, w, t), ang=ang, len=ln, layer=layer, k=k, u=u, i=i))
    return els


def rot2(v, a):
    c, s = math.cos(a), math.sin(a); return (v[0] * c - v[1] * s, v[0] * s + v[1] * c)


def draw_seraph(cv, pal, f, st, size, aoff, far):
    els = build(st, size)
    for e in els:
        if far and e['layer'] == 2:
            continue
        ang = e['ang'] + aoff
        base = rot2(e['base'], aoff)
        bp = f.world(base)
        if e['layer'] == 0:
            col = vivid(pal.A, pal.Hi, 0.3 + 0.4 * e['k'])
        elif e['layer'] == 1:
            col = vivid(pal.A, pal.Hi, 0.2 + 0.25 * e['k'])
        else:
            col = vivid(pal.A, pal.Hi, 0.15 + 0.2 * e['k'])
        if far:
            col = shade(vivid(col, pal.Lo, 0.45), 0.6, 1.1)
        name = 'Covert' if e['layer'] == 2 else 'Feather'
        texlen = 30 if e['layer'] == 2 else 60
        sc = (e['len'] / texlen, (0.62 if e['layer'] == 2 else 0.95) * size)
        anchored(cv, f, name + 'Glow', bp, (2, 8), ang, (sc[0] * 1.06, sc[1] * 1.4), add(pal.B, (0.18 if far else 0.35)))
        anchored(cv, f, name, bp, (2, 8), ang, sc, solid(col))


def scene(st, pal, back=-1):
    cv = Canvas(110, 90)
    cx, cy = 60, 52
    cv.rect(cx - 10, cy - 21, cx + 10, cy + 21, (70, 60, 90))
    cv.rect(cx - 7, cy - 21, cx + 7, cy - 9, (200, 160, 130))
    direction = -back
    sh = (cx - direction * 5, cy - 7)
    f = Frame(sh, back)
    far_f = Frame(f.world((-4, -3)), back)
    far_st = State(st.phase - 0.3, st.amp, st.spread, st.lean)
    draw_seraph(cv, pal, far_f, far_st, 0.88, -0.45, True)
    draw_seraph(cv, pal, f, st, 1.0, 0.0, False)
    return cv


if __name__ == '__main__':
    pal = Pal((255, 214, 110), (255, 150, 90))
    rows = [
        ('fly', [State(ph, 1.0, 1.0) for ph in [i * TAU / 8 for i in range(8)]]),
        ('glide', [State(ph, 0.3, 1.0) for ph in [i * TAU / 8 for i in range(0, 8, 2)]] + [State(0, 0.2, 0.55)] * 0),
        ('idle', [State(ph, 0.12, 0.15) for ph in [i * TAU / 4 for i in range(4)]] + [State(0, 0.2, 0.35, 0.6)]),
    ]
    W, H = 110, 90
    cols = max(len(r[1]) for r in rows)
    big = Canvas(W * cols, H * len(rows))
    for ri, (_, states) in enumerate(rows):
        for ci, st in enumerate(states):
            cv = scene(st, pal)
            for y in range(H):
                big.px[ri * H + y][ci * W:(ci + 1) * W] = cv.px[y]
    big.save(sys.argv[1], 2)


# ---------- остальные стили (повторяют WingEffects.cs) ----------
def depth(c, pal, far):
    return shade(vivid(c, pal.Lo, 0.45), 0.6, 1.1) if far else c


def draw_rig_style(cv, style, pal, f, st, size, aoff, far, time=1.0):
    els = build(st, size)
    gm = 0.5 if far else 1.0
    for e in els:
        if far and e['layer'] == 2 and style != 'Constellation':
            continue
        ang = e['ang'] + aoff
        base = rot2(e['base'], aoff)
        bp = f.world(base)
        k = [0.3 + 0.4 * e['k'], 0.2 + 0.25 * e['k'], 0.15 + 0.2 * e['k']][e['layer']]
        if style == 'Flame':
            fr = (int(time * 10) + e['i'] + e['layer'] * 2) % 4
            src = (fr * 32, 0, 32, 48)
            wdt = (0.45 if e['layer'] == 2 else 0.6) * size
            outer = depth(vivid(pal.A, pal.B, 1 - k), pal, far)
            anchored(cv, f, 'Flame', bp, (16, 46), ang + math.pi / 2, (wdt, e['len'] / 44), add(outer, 0.6 * gm + 0.15), src=src)
            if not far:
                anchored(cv, f, 'Flame', bp, (16, 46), ang + math.pi / 2, (wdt * 0.5, e['len'] / 44 * 0.65), add(pal.Hi, 0.4), src=src)
        elif style == 'Crystal':
            src = (10 * 32, 0, 32, 32)
            sc = (0.8 * size, e['len'] / 24)
            anchored(cv, f, 'ShapesGlow', bp, (16, 28), ang + math.pi / 2, (sc[0] * 1.15, sc[1] * 1.15), add(pal.B, 0.45 * gm), src=src)
            anchored(cv, f, 'Shapes', bp, (16, 28), ang + math.pi / 2, sc, solid(depth(vivid(pal.A, pal.Hi, k), pal, far), 0.92), src=src)
        elif style == 'Spirit':
            sc = (e['len'] * 1.3 / 128, 0.75 * size)
            c = depth(vivid(pal.A, pal.B, k), pal, far)
            anchored(cv, f, 'Blade', bp, (0, 8), ang, (sc[0], sc[1] * 1.6), add(c, 0.35 * gm + 0.05))
            if not far:
                anchored(cv, f, 'Blade', bp, (0, 8), ang, (sc[0] * 0.9, sc[1] * 0.6), add(lerp(c, (255, 255, 255), 0.4), 0.3))
        elif style == 'Constellation':
            if e['layer'] == 2:
                continue
            tip = (base[0] + math.cos(ang) * e['len'], base[1] + math.sin(ang) * e['len'])
            anchored(cv, f, 'Flare', f.world(tip), (64, 64), 0.3, (0.12 * size, 0.12 * size), add(depth(vivid(pal.Hi, pal.A, k), pal, far), gm))


def draw_butterfly(cv, pal, f, st, size, aoff, far):
    w = wave(st.phase)
    flying = 0.3 + 0.7 * (0.5 + 0.5 * w)
    op = L(0.3, L(1.0, flying, st.amp), st.spread)
    r = -0.25 + aoff + w * st.amp * 0.15 + st.lean * 0.2 + (1 - st.spread) * 0.35
    sc = (0.8 * size * op, 0.8 * size)
    gm = 0.5 if far else 1.0
    anchored(cv, f, 'ButterflyGlow', f.sh, (4, 32), r, (sc[0] * 1.05, sc[1] * 1.05), add(pal.B, 0.45 * gm))
    anchored(cv, f, 'ButterflyWing', f.sh, (4, 32), r, sc, solid(depth(pal.A, pal, far), 0.9))
    anchored(cv, f, 'ButterflyPattern', f.sh, (4, 32), r, sc, solid(depth(pal.B, pal, far)))


def draw_bat(cv, pal, f, st, size, aoff, far):
    w = wave(st.phase)
    r = -0.3 + aoff + w * st.amp * 0.55 + (1 - st.spread) * 0.75 + st.lean * 0.25
    fold = L(0.55, 1.0, st.spread)
    sc = (0.62 * size * L(0.75, 1.0, st.spread), 0.62 * size * fold * (0.88 + 0.12 * w * st.amp))
    gm = 0.5 if far else 1.0
    mem = depth(shade(pal.B, 0.32, 1.2), pal, far)
    anchored(cv, f, 'BatWing', f.sh, (4, 22), r, (sc[0] * 1.04, sc[1] * 1.04), add(pal.A, 0.22 * gm))
    anchored(cv, f, 'BatWing', f.sh, (4, 22), r, sc, solid(mem, 0.95))
    anchored(cv, f, 'BatBones', f.sh, (4, 22), r, sc, add(depth(pal.A, pal, far), 0.9 * gm))


def draw_light(cv, pal, f, st, size, aoff, far):
    gm = 0.5 if far else 1.0
    for i in range(9):
        k = i / 8
        d = State(st.phase - (1 - k) * 0.6, st.amp, st.spread, st.lean)
        sweep = wave(d.phase) * d.amp * 0.35
        fan = L(L(0.55, 1.15, k), L(-1.05, 0.75, k), st.spread)
        ang = fan + sweep * (1 - k * 0.5) + aoff + st.lean * 0.25
        ln = (22 + 24 * (1 - k) ** 0.8) * size * L(0.8, 1.0, st.spread)
        c = depth(vivid(pal.A, pal.B, k), pal, far)
        dv = rv(ang)
        base = f.world((dv[0] * 8 * size, dv[1] * 8 * size))
        anchored(cv, f, 'Blade', base, (0, 8), ang, (ln / 128, 0.8 * size * 1.6), add(c, 0.38 * gm + 0.05))
        if not far:
            anchored(cv, f, 'Blade', base, (0, 8), ang, (ln / 128 * 0.92, 0.8 * size * 0.5), add(lerp(c, (255, 255, 255), 0.5), 0.35))


def scene_style(style, pal, st):
    cv = Canvas(110, 90)
    cx, cy = 60, 52
    cv.rect(cx - 10, cy - 21, cx + 10, cy + 21, (70, 60, 90))
    cv.rect(cx - 7, cy - 21, cx + 7, cy - 9, (200, 160, 130))
    f = Frame((cx - 5, cy - 7), -1)
    far_f = Frame(f.world((-4, -3)), -1)
    far_st = State(st.phase - 0.3, st.amp, st.spread, st.lean)
    for fr, s_, size, aoff, far in ((far_f, far_st, 0.88, -0.45, True), (f, st, 1.0, 0.0, False)):
        if style == 'Seraph':
            draw_seraph(cv, pal, fr, s_, size, aoff, far)
        elif style == 'Butterfly':
            draw_butterfly(cv, pal, fr, s_, size, aoff, far)
        elif style == 'Demon':
            draw_bat(cv, pal, fr, s_, size, aoff, far)
        elif style == 'Light':
            draw_light(cv, pal, fr, s_, size, aoff, far)
        else:
            draw_rig_style(cv, style, pal, fr, s_, size, aoff, far)
    return cv


def all_styles(path):
    styles = [('Flame', Pal((255, 160, 40), (235, 40, 40))), ('Crystal', Pal((120, 210, 255), (130, 130, 255))),
              ('Light', Pal((255, 150, 220), (140, 220, 255))), ('Butterfly', Pal((255, 140, 200), (190, 140, 255))),
              ('Demon', Pal((150, 70, 255), (255, 80, 200))), ('Spirit', Pal((80, 230, 255), (160, 110, 255))),
              ('Constellation', Pal((120, 150, 255), (255, 220, 150)))]
    states = [State(0.0, 1.0, 1.0), State(TAU * 0.3, 1.0, 1.0), State(TAU * 0.7, 1.0, 1.0), State(1.0, 0.3, 1.0), State(1.0, 0.12, 0.15)]
    W, H = 110, 90
    big = Canvas(W * len(states), H * len(styles))
    for si, (st_name, pal) in enumerate(styles):
        for ci, st in enumerate(states):
            cv = scene_style(st_name, pal, st)
            for y in range(H):
                big.px[si * H + y][ci * W:(ci + 1) * W] = cv.px[y]
    big.save(path, 2)
