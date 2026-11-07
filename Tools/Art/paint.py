"""A tiny comic-painting kit (PIL): smooth shapes from control points, each painted as a layer with an ink outline,
cel shading clipped to the shape, highlights and textures. Everything is drawn at 2x and scaled down at the end."""
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

INK = (43, 27, 20, 255)

def catmull(points, closed=True, n=14):
    pts = list(points)
    out = []
    count = len(pts) if closed else len(pts) - 1
    for i in range(count):
        p0 = pts[(i - 1) % len(pts)] if closed or i > 0 else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % len(pts)]
        p3 = pts[(i + 2) % len(pts)] if closed or i + 2 < len(pts) else pts[-1]
        for k in range(n):
            t = k / n; t2 = t * t; t3 = t2 * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            out.append((x, y))
    if not closed: out.append(pts[-1])
    return out

def ellipse_pts(cx, cy, rx, ry, rot=0.0, n=64):
    c, s = math.cos(rot), math.sin(rot)
    return [(cx + rx * math.cos(a) * c - ry * math.sin(a) * s, cy + rx * math.cos(a) * s + ry * math.sin(a) * c)
            for a in [i * 2 * math.pi / n for i in range(n)]]

class Canvas:
    def __init__(self, w, h, scale=2):
        self.S = scale
        self.W, self.H = w * scale, h * scale
        self.img = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))

    def _p(self, pts):
        return [(x * self.S, y * self.S) for x, y in pts]

    def mask(self, pts, smooth=True):
        m = Image.new('L', (self.W, self.H), 0)
        poly = catmull(pts) if smooth else pts
        ImageDraw.Draw(m).polygon(self._p(poly), fill=255)
        return m

    def mask_ellipse(self, cx, cy, rx, ry, rot=0.0):
        return self.mask(ellipse_pts(cx, cy, rx, ry, rot), smooth=False)

    def fill(self, mask, color):
        layer = Image.new('RGBA', (self.W, self.H), color)
        self.img = Image.composite(layer, self.img, mask) if color[3] == 255 else Image.alpha_composite(self.img, Image.composite(layer, Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0)), mask))

    def blend(self, mask, color, alpha=1.0):
        layer = Image.new('RGBA', (self.W, self.H), color[:3] + (int(255 * alpha),))
        clear = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))
        self.img = Image.alpha_composite(self.img, Image.composite(layer, clear, mask))

    def outline(self, mask, width=7, color=INK):
        w = int(width * self.S) | 1
        grown = mask.filter(ImageFilter.MaxFilter(w if w <= 31 else 31))
        if w > 31:
            for _ in range((w - 31) // 30 + 1): grown = grown.filter(ImageFilter.MaxFilter(31))
        self.blend(grown, color)

    def part(self, mask, color, width=7, shade=None, light=None):
        """Ink outline, the fill, then shading (list of (mask, colour, alpha)) clipped to the part."""
        if width: self.outline(mask, width)
        self.fill(mask, color)
        for extra in (shade or []) + (light or []):
            m, c, a = extra
            self.blend(ImageChops.multiply(m, mask), c, a)
        return mask

    def line(self, pts, width, color=INK, smooth=True):
        poly = catmull(pts, closed=False) if smooth else pts
        d = ImageDraw.Draw(self.img)
        pp = self._p(poly)
        d.line(pp, fill=color, width=int(width * self.S), joint='curve')
        r = width * self.S / 2
        for x, y in (pp[0], pp[-1]): d.ellipse((x - r, y - r, x + r, y + r), fill=color)

    def dot(self, x, y, r, color):
        d = ImageDraw.Draw(self.img)
        d.ellipse(((x - r) * self.S, (y - r) * self.S, (x + r) * self.S, (y + r) * self.S), fill=color)

    def texture(self, mask, painter, alpha=1.0):
        """painter(draw, scale) draws onto a clear layer that is then clipped to the mask."""
        layer = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))
        painter(ImageDraw.Draw(layer), self.S)
        if alpha < 1.0:
            a = layer.split()[3].point(lambda v: int(v * alpha))
            layer.putalpha(a)
        clear = Image.new('RGBA', (self.W, self.H), (0, 0, 0, 0))
        self.img = Image.alpha_composite(self.img, Image.composite(layer, clear, mask))

    def result(self, size=None):
        out = self.img.resize((self.W // self.S, self.H // self.S), Image.LANCZOS)
        return out if size is None else out.resize(size, Image.LANCZOS)

def shift(mask, dx, dy, S=2):
    return ImageChops.offset(mask, int(dx * S), int(dy * S))

def minus(a, b):
    return ImageChops.subtract(a, b)
