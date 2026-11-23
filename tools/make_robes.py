"""Робы v0.6: перекраска шаблона робы из ExampleMod (лицензия MIT) в 5 палитр + узор по подолу
и маска свечения подола.

Запуск: python3 tools/make_robes.py
Нужен клон tModLoader (шаблоны ExampleMod): /home/user/tmodloader/tmodloader (или переменная TML).
Результат: Content/Items/Vanity/<Роба>.png (иконка), <Роба>_Body.png (360x224), <Роба>_Legs.png (40x1120)
и <Роба>_Glow.png (120x1120): три копии листа ног по 40 px, в каждой своя треть светящихся
«бусин» подола — в коде они мерцают со сдвигом по фазе, и по подолу бегут огоньки.

Пиксели шаблона — блоками 2x2, узоры тоже рисуются блоками 2x2.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pngio  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TML = os.environ.get("TML", "/home/user/tmodloader/tmodloader")
SRC = os.path.join(TML, "ExampleMod/Content/Items/Armor/Vanity/")
OUT = os.path.join(ROOT, "Content/Items/Vanity/")

# Роли цветов шаблона
ROLES = {
    (50, 50, 50): "o",      # контур
    (52, 52, 35): "d",      # тёмный шов
    (110, 96, 79): 0, (149, 134, 116): 1, (179, 160, 138): 2, (197, 183, 166): 3,  # ткань (тёмная → светлая)
    (184, 151, 52): "t0", (201, 191, 80): "t1", (215, 208, 125): "t2",          # золотая отделка
}

# name: cloth (4 тона), trim (3 тона), outline, seam, motif
ROBES = {
    "SeraphRobe": dict(cloth=[(150, 140, 165), (200, 196, 215), (232, 229, 242), (250, 249, 255)],
                       trim=[(196, 140, 52), (236, 190, 76), (255, 230, 140)],
                       outline=(72, 62, 86), seam=(120, 96, 64), motif="diamond"),
    "InfernoRobe": dict(cloth=[(42, 14, 20), (72, 20, 26), (108, 28, 30), (142, 42, 36)],
                        trim=[(204, 72, 22), (255, 136, 34), (255, 210, 96)],
                        outline=(20, 8, 10), seam=(64, 22, 12), motif="flames"),
    "FrostRobe": dict(cloth=[(92, 130, 182), (142, 186, 226), (188, 222, 246), (228, 244, 255)],
                      trim=[(112, 172, 222), (172, 226, 255), (238, 251, 255)],
                      outline=(40, 58, 102), seam=(72, 102, 152), motif="snow"),
    "SakuraKimono": dict(cloth=[(182, 82, 124), (226, 128, 168), (246, 172, 202), (255, 212, 230)],
                         trim=[(150, 42, 102), (222, 92, 152), (255, 236, 246)],
                         outline=(92, 30, 62), seam=(142, 62, 102), motif="flower"),
    "VoidRobe": dict(cloth=[(24, 12, 42), (42, 20, 72), (64, 32, 102), (90, 50, 132)],
                     trim=[(122, 42, 202), (192, 82, 255), (255, 152, 242)],
                     outline=(8, 4, 16), seam=(32, 10, 52), motif="stars"),
}

# Узоры в блоках 2x2: список (dx, dy, тон отделки 0..2); dy вверх от верха каймы
MOTIFS = {
    "diamond": [(1, 1, 2), (0, 2, 1), (2, 2, 1), (1, 3, 2)],
    "snow": [(1, 1, 1), (0, 2, 1), (1, 2, 2), (2, 2, 1), (1, 3, 1)],
    "flower": [(1, 1, 1), (0, 2, 1), (1, 2, 2), (2, 2, 1), (1, 3, 1)],
    "stars": [(1, 2, 2), (0, 2, 0), (2, 2, 0), (1, 1, 0), (1, 3, 0)],
}


def recolor(img, robe):
    out = pngio.Img(img.w, img.h)
    for y in range(img.h):
        for x in range(img.w):
            p = img.px[y][x]
            if p[3] == 0:
                continue
            role = ROLES.get(p[:3])
            if role == "o":
                c = robe["outline"]
            elif role == "d":
                c = robe["seam"]
            elif isinstance(role, int):
                c = robe["cloth"][role]
            elif role in ("t0", "t1", "t2"):
                c = robe["trim"][int(role[1])]
            else:
                c = p[:3]
            out.px[y][x] = (*c, p[3])
    return out


def is_cloth(img, x, y):
    if not (0 <= x < img.w and 0 <= y < img.h):
        return False
    p = img.px[y][x]
    return p[3] > 0 and isinstance(ROLES.get(p[:3]), int)


def paintable(img, x, y):
    """Ткань или складка (на них можно рисовать узор), но не контур."""
    if not (0 <= x < img.w and 0 <= y < img.h):
        return False
    p = img.px[y][x]
    role = ROLES.get(p[:3])
    return p[3] > 0 and (isinstance(role, int) or role == "d")


def legs(src, robe):
    """Перекрашенные ноги-юбка + узор + маска свечения (3 группы)."""
    out = recolor(src, robe)
    glow = pngio.Img(120, src.h)
    trim = robe["trim"]

    def put(x, y, color, group, light=255):
        for yy in (y, y + 1):
            for xx in (x, x + 1):
                out.px[yy][xx] = (*color, 255)
                glow.px[yy][xx + 40 * group] = (light, light, light, light)

    for f in range(src.h // 56):
        top = f * 56
        # нижний ряд ткани для каждого блока-столбца
        hem = {}
        for bx in range(20):
            x = bx * 2
            bottom = None
            for y in range(top + 55, top, -1):
                if is_cloth(src, x, y):
                    bottom = y
                    break
            if bottom is not None:
                hem[bx] = bottom - 1  # верхняя строка последнего блока ткани
        if not hem:
            continue
        # кайма: последний блок ткани каждого столбца — светлая отделка (бусины чередуются)
        for bx, y in hem.items():
            tone = 2 if bx % 2 == 0 else 1
            put(bx * 2, y, trim[tone], bx % 3, 255 if tone == 2 else 200)
        # узор над каймой
        motif = robe["motif"]
        cols = sorted(hem)
        left, right = cols[0], cols[-1]
        width = right - left + 1
        if motif == "flames":
            # языки пламени от каймы вверх: высота 1-3 блока, рисунок «зубцами»
            heights = [1, 2, 3, 2, 1, 1, 2, 1]
            for i, bx in enumerate(cols):
                h = heights[(bx + f // 4) % len(heights)] if 0 < i < width - 1 else 0
                for k in range(1, h + 1):
                    y = hem[bx] - 2 * k
                    if is_cloth(src, bx * 2, y) and is_cloth(src, bx * 2, y + 1):
                        tone = 1 if k < h else 0
                        put(bx * 2, y, trim[tone], bx % 3, 230 - 50 * k)
            continue
        cells = MOTIFS[motif]
        # узоры ставим слева направо везде, где помещаются (на ткани и складках), с промежутком в блок
        n = 0
        mx = left
        while mx <= right - 2:
            base = min(hem.get(mx + d, 999) for d in range(3))
            pix = []
            if base != 999:
                for dx, dy, tone in cells:
                    bx = mx + dx
                    y = base - 2 * dy
                    if not (paintable(src, bx * 2, y) and paintable(src, bx * 2, y + 1)):
                        pix = None
                        break
                    pix.append((bx * 2, y, tone))
            if pix:
                for x, y, tone in pix:
                    put(x, y, trim[tone], n % 3, 255 if tone == 2 else 170)
                n += 1
                mx += 4
            else:
                mx += 1
    return out, glow


def main():
    body = pngio.load(SRC + "ExampleRobe_Body.png")
    leg = pngio.load(SRC + "ExampleRobe_Legs.png")
    icon = pngio.load(SRC + "ExampleRobe.png")
    for name, robe in ROBES.items():
        recolor(body, robe).save(OUT + name + "_Body.png")
        recolor(icon, robe).save(OUT + name + ".png")
        lg, glow = legs(leg, robe)
        lg.save(OUT + name + "_Legs.png")
        glow.save(OUT + name + "_Glow.png")
        print("saved", name)


if __name__ == "__main__":
    main()
