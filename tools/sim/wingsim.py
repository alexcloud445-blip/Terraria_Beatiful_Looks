"""Симуляция отрисовки крыльев (повторяет Common/Effects/WingEffects.cs) для просмотра без игры."""
import math, os, sys, colorsys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
import pngio
A = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'VFX') + os.sep
TEX = {}
def tex(n):
    if n not in TEX:
        im = pngio.load(A + n + '.png'); TEX[n] = im
    return TEX[n]

class Canvas:
    def __init__(s, w, h, bg=(14, 12, 26)):
        s.w, s.h = w, h
        s.px = [[list(bg) for _ in range(w)] for _ in range(h)]
    def draw(s, name, pos, origin, rot, scale, color, additive, flipH=False, flipV=False, src=None):
        im = tex(name)
        sx0, sy0, sw, sh = src if src else (0, 0, im.w, im.h)
        sc = scale if isinstance(scale, tuple) else (scale, scale)
        cr, sr = math.cos(rot), math.sin(rot)
        # bounding box
        corners = []
        for cx, cy in ((0, 0), (sw, 0), (0, sh), (sw, sh)):
            lx, ly = (cx - origin[0]) * sc[0], (cy - origin[1]) * sc[1]
            corners.append((pos[0] + lx * cr - ly * sr, pos[1] + lx * sr + ly * cr))
        x0 = max(0, int(min(c[0] for c in corners)) - 1); x1 = min(s.w, int(max(c[0] for c in corners)) + 2)
        y0 = max(0, int(min(c[1] for c in corners)) - 1); y1 = min(s.h, int(max(c[1] for c in corners)) + 2)
        r, g, b, a = color
        for y in range(y0, y1):
            for x in range(x0, x1):
                dx, dy = x + 0.5 - pos[0], y + 0.5 - pos[1]
                lx, ly = dx * cr + dy * sr, -dx * sr + dy * cr
                qx, qy = lx / sc[0] + origin[0], ly / sc[1] + origin[1]
                if not (0 <= qx < sw and 0 <= qy < sh): continue
                tx = sw - 1 - int(qx) if flipH else int(qx)
                ty = sh - 1 - int(qy) if flipV else int(qy)
                tr, tg, tb, ta = im.px[sy0 + ty][sx0 + tx]
                if ta == 0: continue
                al = ta / 255
                # текстура уже premultiplied (как в игре после исправления)
                pr, pg, pb = tr / 255 * r / 255, tg / 255 * g / 255, tb / 255 * b / 255
                pa = al * a / 255
                d = s.px[y][x]
                for k, v in enumerate((pr, pg, pb)):
                    d[k] = min(255, v * 255 + d[k] * (1 - pa))
    def rect(s, x0, y0, x1, y1, c):
        for y in range(y0, y1):
            for x in range(x0, x1):
                s.px[y][x] = list(c)
    def save(s, path, scale=3):
        img = pngio.Img(s.w * scale, s.h * scale)
        for y in range(s.h * scale):
            for x in range(s.w * scale):
                p = s.px[y // scale][x // scale]
                img.px[y][x] = (int(p[0]), int(p[1]), int(p[2]), 255)
        img.save(path)

def hsv(c):
    return colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
def from_hsv(h, s, v):
    r, g, b = colorsys.hsv_to_rgb(h % 1, s, v); return (int(r * 255), int(g * 255), int(b * 255))
def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))
def vivid(a, b, t):
    ha, hb = hsv(a), hsv(b)
    if ha[1] < 0.15 or hb[1] < 0.15: return lerp(a, b, t)
    dh = hb[0] - ha[0]
    if dh > 0.5: dh -= 1
    elif dh < -0.5: dh += 1
    return from_hsv(ha[0] + dh * t, ha[1] + (hb[1] - ha[1]) * t, ha[2] + (hb[2] - ha[2]) * t)
def shade(c, br, sat=1.0):
    h, s, v = hsv(c); return from_hsv(h, min(1, s * sat), min(1, v * br))
def add(c, o): return (c[0], c[1], c[2], 0) if False else (int(c[0] * o), int(c[1] * o), int(c[2] * o), 0)
def solid(c, o=1.0): return (int(c[0] * o), int(c[1] * o), int(c[2] * o), int(255 * o))

class Pal:
    def __init__(s, a, b):
        s.A, s.B = a, b; s.Hi = lerp(a, (255, 255, 255), 0.62); s.Lo = shade(b, 0.42, 1.15)

def rv(a): return (math.cos(a), math.sin(a))
def arm(ph, amp): return -0.4 - math.sin(ph) * amp * 0.55
def fore(ph, amp): return arm(ph, amp) + 0.15 + math.sin(ph - 0.6) * amp * 0.3
def skeleton(ph, amp, size):
    a = rv(arm(ph, amp)); w = (a[0] * 13 * size, a[1] * 13 * size)
    f = rv(fore(ph, amp)); t = (w[0] + f[0] * 16 * size, w[1] + f[1] * 16 * size)
    return w, t
def onarm(u, w, t):
    if u < 0.5: k = u / 0.5; return (w[0] * k, w[1] * k)
    k = (u - 0.5) / 0.5; return (w[0] + (t[0] - w[0]) * k, w[1] + (t[1] - w[1]) * k)
def L(a, b, t): return a + (b - a) * t
def build(ph, amp, size):
    w, t = skeleton(ph, amp, size); fa = fore(ph, amp)
    fold = 1 - 0.25 * max(0, math.cos(ph)) * amp
    els = []
    for i in range(7):
        k = i / 6
        els.append(dict(base=onarm(L(0.52, 0.98, k), w, t), ang=L(fa + 1.15 * fold, fa - 0.12, k), len=L(17, 26, math.sin(k * math.pi / 2)) * size, layer=0, k=k, i=i))
    for i in range(6):
        k = i / 5
        els.append(dict(base=onarm(L(0.06, 0.5, k), w, t), ang=L(1.4, fa + 1.2 * fold, k), len=L(13, 17, k) * size, layer=1, k=k, i=i))
    for i in range(7):
        k = i / 6
        els.append(dict(base=onarm(L(0.04, 0.85, k), w, t), ang=L(1.1, fa + 0.6, k), len=L(7, 10, k) * size, layer=2, k=k, i=i))
    return els

def rot2(v, a):
    c, s = math.cos(a), math.sin(a); return (v[0] * c - v[1] * s, v[0] * s + v[1] * c)

class Frame:
    def __init__(s, shoulder, back, grav=1): s.sh, s.back, s.grav = shoulder, back, grav
    def world(s, l): return (s.sh[0] + l[0] * s.back, s.sh[1] + l[1] * s.grav)

def anchored(cv, f, name, pos, anchor, lrot, scale, color, additive=True, src=None):
    sw = src[2] if src else tex(name).w; shh = src[3] if src else tex(name).h
    ox, oy = anchor; rot = lrot; fh = fv = False
    if f.back < 0: fh = True; ox = sw - ox; rot = -rot
    if f.grav < 0: fv = True; oy = shh - oy; rot = -rot
    cv.draw(name, pos, (ox, oy), rot, scale, color, additive, fh, fv, src)

def draw_wing(cv, style, pal, f, ph, amp, size, aoff, bright, time=1.0):
    if style == 'Butterfly':
        op = 0.3 + 0.7 * (0.5 + 0.5 * math.cos(ph * 1.5)) * L(1, 0.85, amp)
        r = -0.25 + aoff + math.sin(ph * 1.5) * 0.08
        sc = (0.72 * size * op, 0.72 * size)
        anchored(cv, f, 'ButterflyGlow', f.sh, (4, 32), r, (sc[0] * 1.05, sc[1] * 1.05), add(pal.B, 0.45 * bright))
        anchored(cv, f, 'ButterflyWing', f.sh, (4, 32), r, sc, solid(shade(pal.A, bright), 0.9))
        anchored(cv, f, 'ButterflyPattern', f.sh, (4, 32), r, sc, solid(shade(pal.B, bright)))
        anchored(cv, f, 'ButterflyPattern', f.sh, (4, 32), r, sc, add(pal.Hi, 0.3 * bright))
        return
    if style == 'Demon':
        s_ = math.sin(ph); r = -0.3 + aoff - s_ * amp * 0.5
        sc = (0.62 * size, 0.62 * size * (0.8 + 0.2 * math.cos(ph)))
        mem = shade(pal.B, 0.32 * bright, 1.2)
        anchored(cv, f, 'BatWing', f.sh, (4, 22), r, (sc[0] * 1.04, sc[1] * 1.04), add(pal.A, 0.22 * bright))
        anchored(cv, f, 'BatWing', f.sh, (4, 22), r, sc, solid(mem, 0.95))
        anchored(cv, f, 'BatBones', f.sh, (4, 22), r, sc, add(pal.A, 0.9 * bright))
        anchored(cv, f, 'BatBones', f.sh, (4, 22), r, sc, add(pal.Hi, 0.35 * bright))
        return
    if style == 'Light':
        sweep = math.sin(ph) * amp * 0.35
        for i in range(9):
            k = i / 8
            ang = L(-1.05, 0.75, k) - sweep * (1 - k * 0.5) + aoff
            ln = (22 + 24 * (1 - k) ** 0.8) * size
            c = vivid(pal.A, pal.B, k)
            d = rv(ang); base = f.world((d[0] * 7 * size, d[1] * 7 * size))
            anchored(cv, f, 'Blade', base, (0, 8), ang, (ln / 128, 0.8 * size * 1.6), add(c, 0.38 * bright))
            anchored(cv, f, 'Blade', base, (0, 8), ang, (ln / 128 * 0.92, 0.8 * size * 0.5), add(lerp(c, (255, 255, 255), 0.5), 0.4 * bright))
        return
    els = build(ph, amp, size)
    for e in els:
        e['ang'] += aoff; e['base'] = rot2(e['base'], aoff)
        bp = f.world(e['base'])
        lay = [1, 0.92, 0.85][e['layer']]
        k = [0.55 + 0.45 * e['k'], 0.25 + 0.3 * e['k'], 0.1 + 0.2 * e['k']][e['layer']]
        if style == 'Seraph':
            fill = shade(vivid(pal.Hi, pal.A, k), bright * lay)
            sc = (e['len'] / 60, 0.85 * size)
            anchored(cv, f, 'FeatherGlow', bp, (2, 8), e['ang'], (sc[0] * 1.06, sc[1] * 1.4), add(pal.B, 0.4 * bright))
            anchored(cv, f, 'Feather', bp, (2, 8), e['ang'], sc, solid(fill))
        elif style == 'Flame':
            fr = (int(time * 10) + e['i'] + e['layer'] * 2) % 4
            src = (fr * 32, 0, 32, 48)
            wdt = (0.45 if e['layer'] == 2 else 0.6) * size
            anchored(cv, f, 'Flame', bp, (16, 46), e['ang'] + math.pi / 2, (wdt, e['len'] / 44), add(vivid(pal.A, pal.B, k), 0.6 * bright), src=src)
            anchored(cv, f, 'Flame', bp, (16, 46), e['ang'] + math.pi / 2, (wdt * 0.5, e['len'] / 44 * 0.65), add(pal.Hi, 0.4 * bright), src=src)
        elif style == 'Crystal':
            src = (10 * 32, 0, 32, 32)
            sc = (0.85 * size, e['len'] / 24)
            anchored(cv, f, 'ShapesGlow', bp, (16, 28), e['ang'] + math.pi / 2, (sc[0] * 1.15, sc[1] * 1.15), add(pal.B, 0.5 * bright), src=src)
            fill = shade(vivid(pal.Hi, pal.A, k), bright * lay)
            anchored(cv, f, 'Shapes', bp, (16, 28), e['ang'] + math.pi / 2, sc, solid(fill, 0.92), src=src)
        elif style == 'Spirit':
            wave = math.sin(time * 2.4 + e['i'] * 0.7 + e['layer']) * 0.09
            sc = (e['len'] * 1.3 / 128, 0.75 * size)
            c = vivid(pal.A, pal.B, k)
            anchored(cv, f, 'Blade', bp, (0, 8), e['ang'] + wave, (sc[0], sc[1] * 1.6), add(c, 0.35 * bright))
            anchored(cv, f, 'Blade', bp, (0, 8), e['ang'] + wave, (sc[0] * 0.9, sc[1] * 0.6), add(lerp(c, (255, 255, 255), 0.4), 0.3 * bright))
        elif style == 'Constellation':
            if e['layer'] == 2: continue
            tip = (e['base'][0] + math.cos(e['ang']) * e['len'], e['base'][1] + math.sin(e['ang']) * e['len'])
            tp = f.world(tip)
            anchored(cv, f, 'Flare', tp, (64, 64), 0.3, (0.12 * size, 0.12 * size), add(vivid(pal.Hi, pal.A, k), bright))

def scene(style, pal, ph, amp, path, back=-1):
    cv = Canvas(110, 90)
    cx, cy = 60, 50
    # силуэт персонажа 20x42
    cv.rect(cx - 10, cy - 21, cx + 10, cy + 21, (70, 60, 90))
    cv.rect(cx - 7, cy - 21, cx + 7, cy - 9, (200, 160, 130))
    direction = -back
    sh = (cx - direction * 5, cy - 7)
    f = Frame(sh, back)
    far = Frame(f.world((-3, -2)), back)
    draw_wing(cv, style, pal, far, ph - 0.35, amp, 0.86, -0.28, 0.55)
    draw_wing(cv, style, pal, f, ph, amp, 1.0, 0, 1.0)
    return cv

if __name__ == '__main__':
    out = sys.argv[1]
    styles = [('Seraph', Pal((255, 214, 110), (255, 150, 90))), ('Flame', Pal((255, 160, 40), (235, 40, 40))),
              ('Crystal', Pal((120, 210, 255), (130, 130, 255))), ('Light', Pal((255, 120, 220), (130, 230, 255))),
              ('Butterfly', Pal((255, 140, 200), (190, 140, 255))), ('Demon', Pal((150, 70, 255), (255, 80, 200))),
              ('Spirit', Pal((80, 230, 255), (160, 110, 255))), ('Constellation', Pal((120, 150, 255), (255, 220, 150)))]
    phases = [(0.0, 0.25), (1.57, 1.0), (4.7, 1.0)]
    W, H = 110, 90
    big = Canvas(W * len(phases), H * len(styles))
    for si, (st, pal) in enumerate(styles):
        for pi, (ph, amp) in enumerate(phases):
            cv = scene(st, pal, ph, amp, None)
            for y in range(H):
                big.px[si * H + y][pi * W:(pi + 1) * W] = cv.px[y]
    big.save(out, 2)
