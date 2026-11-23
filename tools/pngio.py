"""Минимальное чтение/запись PNG без внешних библиотек (RGBA, палитра, RGB; 8 бит и меньше для палитры)."""
import os
import struct
import zlib


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
            self.px[y][x] = tuple(int(max(0, min(255, v))) for v in (c if len(c) == 4 else (*c, 255)))

    def blend_add(self, x, y, c, a):
        """Аддитивно добавляет яркость (для свечения)."""
        x, y = int(x), int(y)
        if not (0 <= x < self.w and 0 <= y < self.h):
            return
        r, g, b, al = self.px[y][x]
        self.px[y][x] = (min(255, int(r + c[0] * a)), min(255, int(g + c[1] * a)),
                         min(255, int(b + c[2] * a)), min(255, int(al + 255 * a)))

    def save(self, path):
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


def _paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    return b if pb <= pc else c


def load(path):
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", path
    i = 8
    idat = b""
    palette, trns = None, None
    while i < len(data):
        length = struct.unpack(">I", data[i:i + 4])[0]
        ctype = data[i + 4:i + 8]
        body = data[i + 8:i + 8 + length]
        if ctype == b"IHDR":
            w, h, depth, color, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert interlace == 0, "interlaced PNG not supported"
        elif ctype == b"PLTE":
            palette = [tuple(body[j:j + 3]) for j in range(0, len(body), 3)]
        elif ctype == b"tRNS":
            trns = body
        elif ctype == b"IDAT":
            idat += body
        i += 12 + length

    raw = zlib.decompress(idat)
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[color]
    bits_pp = channels * depth
    stride = (w * bits_pp + 7) // 8
    bpp = max(1, bits_pp // 8)
    rows, prev, pos = [], bytearray(stride), 0
    for _ in range(h):
        f = raw[pos]
        line = bytearray(raw[pos + 1:pos + 1 + stride])
        pos += 1 + stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + (a + b) // 2) & 255
            elif f == 4:
                line[x] = (line[x] + _paeth(a, b, c)) & 255
        rows.append(line)
        prev = line

    img = Img(w, h)
    for y, line in enumerate(rows):
        for x in range(w):
            if color == 6:
                img.px[y][x] = tuple(line[x * 4:x * 4 + 4])
            elif color == 2:
                img.px[y][x] = (*line[x * 3:x * 3 + 3], 255)
            elif color == 3:
                if depth == 8:
                    idx = line[x]
                else:
                    per = 8 // depth
                    byte = line[x // per]
                    shift = 8 - depth * (x % per + 1)
                    idx = (byte >> shift) & ((1 << depth) - 1)
                r, g, b = palette[idx]
                a = trns[idx] if trns is not None and idx < len(trns) else 255
                img.px[y][x] = (r, g, b, a)
            elif color == 0:
                v = line[x]
                img.px[y][x] = (v, v, v, 255)
            elif color == 4:
                v = line[x * 2]
                img.px[y][x] = (v, v, v, line[x * 2 + 1])
    return img
