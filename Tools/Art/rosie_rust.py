"""Rosie Rust, the grumpy farmer, standing arms crossed in front of her farm pickup (front view) with the hay bale
launcher on its bed and her pitchfork leaning against it. Comic style: ink outlines, cel shading (light from the
top left), highlights and textures.

    python3 Tools/Art/rosie_rust.py [scale]

Writes Assets/Art/Rivals/RosieRustPainting.png (the whole scene, 1200 x 1400, transparent around it) and
Assets/Art/Rivals/RosieRust.png (her round portrait, 512 x 512)."""
import math, os, random, sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
sys.path.insert(0, os.path.dirname(__file__))
from paint import Canvas, catmull, ellipse_pts, shift, minus, INK

SCALE = int(sys.argv[1]) if len(sys.argv) > 1 else 2
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Art', 'Rivals')
random.seed(7)

def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# Palette.
SKIN, SKIN_SH, SKIN_HI, BLUSH = hexc('F4C7A1'), hexc('D9946C'), hexc('FFE3C8'), hexc('EE8A78')
HAIR, HAIR_SH, HAIR_HI = hexc('D2561F'), hexc('9C3510'), hexc('F28A45')
STRAW, STRAW_SH, STRAW_HI = hexc('EBC35E'), hexc('C4912F'), hexc('FFE79A')
DENIM, DENIM_SH, DENIM_HI = hexc('3F6FB5'), hexc('2A4C85'), hexc('6E98D6')
PLAID, PLAID_SH, PLAID_LINE = hexc('3E8E4E'), hexc('2B6638'), hexc('C8E3A0')
RED, RED_SH, RED_HI = hexc('C8462F'), hexc('8E2B1C'), hexc('EE7B5A')
CHROME, CHROME_SH, CHROME_HI = hexc('C9CFD6'), hexc('8C949E'), hexc('F4F7FA')
TYRE, TYRE_SH, TYRE_HI = hexc('2E2B2C'), hexc('1C1A1B'), hexc('4A4648')
GLASS, GLASS_HI = hexc('7FB6D8'), hexc('D6EEFA')
BOOT, BOOT_SH = hexc('4E8F3A'), hexc('35662A')
WOOD, WOOD_SH = hexc('A8713F'), hexc('7A4D26')
STEEL, STEEL_SH = hexc('8E9AA6'), hexc('5E6873')
GREEN_METAL, GREEN_SH, GREEN_HI = hexc('4F8B4A'), hexc('35653A'), hexc('7DB571')
YELLOW = hexc('F6C343')

c = Canvas(1200, 1400, SCALE)
S = c.S

def M(pts): return c.mask(pts)
def E(cx, cy, rx, ry, rot=0.0): return c.mask_ellipse(cx, cy, rx, ry, rot)

def hay(m, base=STRAW, dark=STRAW_SH, light=STRAW_HI, seed=1, horizontal=True):
    """Straw: short strands over the bale."""
    rnd = random.Random(seed)
    def p(d, s):
        bb = m.getbbox()
        if not bb: return
        x0, y0, x1, y1 = bb
        for _ in range(int((x1 - x0) * (y1 - y0) / (s * s * 90))):
            x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
            L = rnd.uniform(10, 26) * s
            a = rnd.uniform(-0.35, 0.35) + (0 if horizontal else math.pi / 2)
            col = dark if rnd.random() < 0.5 else light
            d.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], fill=col, width=int(2.2 * s))
    c.texture(m, p, 0.85)

# ---------------------------------------------------------------- the ground shadow
c.blend(E(700, 1338, 520, 46), (60, 40, 30, 255), 0.28)

# ---------------------------------------------------------------- the pickup (behind)
CX = 800
# Hay bales on the bed, behind the cab, and the launcher.
bale1 = M([(640, 600), (790, 592), (796, 470), (650, 476)])
c.part(bale1, STRAW, shade=[(M([(730, 600), (796, 592), (796, 470), (740, 474)]), STRAW_SH, 0.55)])
hay(bale1, seed=3)
c.part(M([(690, 476), (700, 476), (704, 596), (694, 598)]), hexc('8C3F1E'), width=0)
c.part(M([(744, 474), (754, 474), (756, 594), (746, 595)]), hexc('8C3F1E'), width=0)
bale2 = M([(800, 594), (948, 600), (944, 484), (806, 478)])
c.part(bale2, STRAW, shade=[(M([(890, 600), (948, 600), (944, 484), (884, 482)]), STRAW_SH, 0.55)])
hay(bale2, seed=4)
c.part(M([(846, 480), (856, 480), (858, 598), (848, 598)]), hexc('8C3F1E'), width=0)
c.part(M([(900, 482), (910, 482), (910, 599), (900, 599)]), hexc('8C3F1E'), width=0)

# The launcher: a riveted green cannon on a swivel, aimed up and back over the right, a bale in its flared mouth.
base = M([(902, 604), (1012, 612), (1004, 560), (986, 540), (932, 538), (914, 556)])
c.part(base, STEEL, width=7, shade=[(M([(960, 604), (1012, 612), (1004, 560), (962, 540)]), STEEL_SH, 0.7)])
crank = E(904, 560, 30, 30)
c.part(crank, hexc('D8432C'), width=6, shade=[(E(912, 568, 22, 22), RED_SH, 0.6)])
c.part(E(904, 560, 10, 10), STEEL, width=4)
c.line([(904, 560), (872, 532)], 7)
c.line([(904, 560), (872, 532)], 4, STEEL)
c.part(E(868, 528, 9, 9), hexc('3A2A20'), width=3)
barrel = M([(944, 568), (1006, 590), (1104, 400), (1046, 372)])
c.part(barrel, GREEN_METAL, width=8,
       shade=[(M([(984, 584), (1006, 590), (1104, 400), (1084, 392)]), GREEN_SH, 0.85)],
       light=[(M([(954, 566), (968, 572), (1062, 384), (1050, 378)]), GREEN_HI, 0.75)])
for t in (0.18, 0.62):
    x0, y0 = 944 + (1046 - 944) * t, 568 + (372 - 568) * t
    ring = M([(x0 - 8, y0 + 2), (x0 + 66, y0 + 30), (x0 + 74, y0 + 12), (x0 + 0, y0 - 16)])
    c.part(ring, STEEL, width=5, shade=[(shift(ring, 5, 5, S), STEEL_SH, 0.6)], light=[(shift(ring, -4, -6, S), CHROME_HI, 0.5)])
    for k in range(3):
        c.dot(x0 + 8 + k * 22, y0 + 4 + k * 9, 3.6, CHROME_HI)
bell = M([(1018, 384), (1132, 430), (1150, 360), (1100, 318), (1050, 312)])
c.part(bell, GREEN_METAL, width=8, shade=[(M([(1090, 412), (1132, 430), (1150, 360), (1120, 338)]), GREEN_SH, 0.85)],
       light=[(M([(1030, 376), (1060, 322), (1072, 324), (1046, 382)]), GREEN_HI, 0.7)])
mouth = E(1094, 336, 58, 26, 0.42)
c.part(mouth, hexc('24402A'), width=7)
loaded = E(1094, 322, 46, 32, 0.42)
c.part(loaded, STRAW, width=5, shade=[(E(1110, 334, 34, 20, 0.42), STRAW_SH, 0.55)])
hay(loaded, seed=9)
c.line([(1060, 318), (1128, 344)], 4, hexc('8C3F1E'))
# A stencilled HAY on the barrel.
def stencil(d, s):
    pass
# Cab: roof and the windscreen frame.
cab = M([(600, 650), (612, 612), (990, 612), (1002, 650), (1010, 830), (590, 830)])
c.part(cab, RED, width=8, shade=[(M([(900, 612), (990, 612), (1010, 830), (930, 830)]), RED_SH, 0.75)],
       light=[(M([(620, 620), (980, 620), (980, 634), (620, 634)]), RED_HI, 0.8)])
shield = M([(632, 660), (968, 660), (980, 812), (620, 812)])
c.part(shield, GLASS, width=6,
       shade=[(M([(800, 660), (968, 660), (980, 812), (860, 812)]), hexc('5E93B8'), 0.6)],
       light=[(M([(660, 672), (720, 672), (650, 800), (630, 800)]), GLASS_HI, 0.75), (M([(740, 672), (760, 672), (690, 800), (676, 800)]), GLASS_HI, 0.55)])
# Mirrors.
for mx, flip in ((560, -1), (1040, 1)):
    arm = M([(mx - 4 * flip, 760), (mx + 40 * flip, 748), (mx + 40 * flip, 760), (mx - 4 * flip, 772)])
    c.part(arm, STEEL_SH, width=4)
    mirror = M([(mx - 30, 700), (mx + 30, 700), (mx + 30, 780), (mx - 30, 780)])
    c.part(mirror, RED, width=6, shade=[(M([(mx + 6, 700), (mx + 30, 700), (mx + 30, 780), (mx + 6, 780)]), RED_SH, 0.7)])
    c.part(M([(mx - 20, 712), (mx + 20, 712), (mx + 20, 768), (mx - 20, 768)]), GLASS, width=0, light=[(M([(mx - 18, 714), (mx - 6, 714), (mx - 14, 766), (mx - 18, 766)]), GLASS_HI, 0.7)])

# Tyres (front view: tall, chunky, tread blocks).
for tx in (470, 1000):
    tyre = M([(tx, 1040), (tx + 130, 1040), (tx + 136, 1330), (tx - 6, 1330)])
    c.part(tyre, TYRE, width=8, shade=[(M([(tx + 70, 1040), (tx + 130, 1040), (tx + 136, 1330), (tx + 76, 1330)]), TYRE_SH, 0.8)])
    def treads(d, s, tx=tx):
        for y in range(1050, 1330, 26):
            d.rectangle(((tx + 8) * s, y * s, (tx + 122) * s, (y + 10) * s), fill=TYRE_HI)
    c.texture(tyre, treads, 0.55)

# Body: the bonnet, fenders, the face of the truck.
body = M([(520, 860), (560, 822), (1040, 822), (1080, 860), (1090, 1120), (510, 1120)])
c.part(body, RED, width=8,
       shade=[(M([(930, 822), (1040, 822), (1090, 1120), (960, 1120)]), RED_SH, 0.75), (M([(510, 1080), (1090, 1080), (1090, 1120), (510, 1120)]), RED_SH, 0.5)],
       light=[(M([(560, 832), (1030, 832), (1036, 850), (556, 850)]), RED_HI, 0.85), (M([(530, 870), (548, 870), (540, 1060), (524, 1060)]), RED_HI, 0.5)])
# Rust spots (she is Rosie RUST).
for (rx, ry, rr) in ((560, 1080, 14), (1050, 900, 10), (1060, 1060, 16), (980, 850, 8)):
    c.blend(E(rx, ry, rr * 1.3, rr), hexc('8A4A22'), 0.55)
    c.blend(E(rx + 3, ry + 2, rr * 0.7, rr * 0.5), hexc('6A3416'), 0.6)
# Grille.
grille = M([(690, 920), (910, 920), (916, 1080), (684, 1080)])
c.part(grille, hexc('2B2A2E'), width=7)
def bars(d, s):
    for x in range(700, 905, 24):
        d.rectangle((x * s, 924 * s, (x + 10) * s, 1076 * s), fill=CHROME_SH)
        d.rectangle((x * s, 924 * s, (x + 4) * s, 1076 * s), fill=CHROME_HI)
c.texture(grille, bars)
# Headlights.
for hx in (610, 990):
    rim = E(hx, 960, 56, 56)
    c.part(rim, CHROME, width=7, shade=[(E(hx + 16, 974, 50, 50), CHROME_SH, 0.6)])
    lamp = E(hx, 960, 40, 40)
    c.part(lamp, hexc('FFF2B0'), width=4, shade=[(E(hx + 12, 972, 34, 34), hexc('F5D36A'), 0.6)])
    c.blend(E(hx - 12, 946, 12, 9), (255, 255, 255, 255), 0.95)
# Bumper and plate.
bumper = M([(496, 1112), (1104, 1112), (1110, 1170), (490, 1170)])
c.part(bumper, CHROME, width=8, shade=[(M([(490, 1146), (1110, 1146), (1110, 1170), (490, 1170)]), CHROME_SH, 0.7)],
       light=[(M([(510, 1120), (1090, 1120), (1090, 1130), (510, 1130)]), CHROME_HI, 0.9)])
plate = M([(730, 1120), (870, 1120), (870, 1162), (730, 1162)])
c.part(plate, hexc('FFF4D6'), width=5)
from PIL import ImageFont
FONT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Art', 'Fonts', 'Barlow', 'BarlowCondensed-BlackItalic.ttf')
def plate_text(d, s):
    f = ImageFont.truetype(FONT, int(34 * s))
    w = d.textlength('ROSIE', font=f)
    d.text((800 * s - w / 2, 1121 * s), 'ROSIE', font=f, fill=hexc('2B4C85'))
c.texture(plate, plate_text)

# ---------------------------------------------------------------- Rosie
# Boots.
for bx, flip in ((300, -1), (420, 1)):
    boot = M([(bx - 44, 1250), (bx + 44, 1250), (bx + 48, 1300), (bx + 52 + 18 * flip, 1340), (bx - 52 + 18 * flip, 1342), (bx - 50, 1300)])
    c.part(boot, BOOT, width=8, shade=[(M([(bx + 6, 1250), (bx + 44, 1250), (bx + 70, 1342), (bx + 10, 1342)]), BOOT_SH, 0.7)],
           light=[(M([(bx - 36, 1258), (bx - 24, 1258), (bx - 28, 1320), (bx - 40, 1320)]), hexc('7FBF63'), 0.7)])
    c.part(M([(bx - 56 + 18 * flip, 1318), (bx + 58 + 18 * flip, 1318), (bx + 56 + 18 * flip, 1342), (bx - 54 + 18 * flip, 1344)]), hexc('2F3A2C'), width=5)

# Legs: the dungarees, rolled-up cuffs over the boots.
legs = M([(256, 950), (490, 950), (488, 1262), (382, 1264), (370, 1120), (356, 1264), (244, 1262)])
c.part(legs, DENIM, width=8,
       shade=[(M([(410, 960), (480, 960), (476, 1262), (420, 1262)]), DENIM_SH, 0.75), (M([(352, 1100), (372, 1100), (380, 1264), (344, 1264)]), DENIM_SH, 0.7)],
       light=[(M([(270, 980), (292, 980), (276, 1240), (258, 1240)]), DENIM_HI, 0.6)])
def denim(d, s):
    for y in range(960, 1270, 7):
        d.line([(240 * s, y * s), (500 * s, (y + 3) * s)], fill=(255, 255, 255, 18), width=int(1.2 * s))
c.texture(legs, denim)
for (cx0, cx1) in ((244, 356), (382, 488)):
    cuff = M([(cx0 - 6, 1226), (cx1 + 6, 1226), (cx1 + 6, 1262), (cx0 - 6, 1264)])
    c.part(cuff, DENIM_HI, width=6, shade=[(M([(cx0 - 6, 1248), (cx1 + 6, 1248), (cx1 + 6, 1264), (cx0 - 6, 1264)]), DENIM, 0.8)])

# Shirt: the body (green plaid).
def plaid(d, s):
    for x in range(160, 580, 34):
        d.rectangle((x * s, 620 * s, (x + 8) * s, 1000 * s), fill=(20, 60, 30, 110))
        d.rectangle(((x + 16) * s, 620 * s, (x + 18) * s, 1000 * s), fill=PLAID_LINE[:3] + (130,))
    for y in range(640, 1000, 34):
        d.rectangle((160 * s, y * s, 580 * s, (y + 8) * s), fill=(20, 60, 30, 110))
        d.rectangle((160 * s, (y + 16) * s, 580 * s, (y + 18) * s), fill=hexc('E85A3A')[:3] + (120,))
shirt = M([(244, 700), (300, 650), (444, 650), (500, 700), (500, 960), (246, 960)])
c.part(shirt, PLAID, width=8, shade=[(M([(420, 650), (500, 700), (500, 960), (430, 960)]), PLAID_SH, 0.75)])
c.texture(shirt, plaid)

# The bib of the dungarees with its straps and brass buttons.
bib = M([(290, 752), (452, 752), (466, 970), (276, 970)])
c.part(bib, DENIM, width=7, shade=[(M([(404, 752), (452, 752), (466, 970), (414, 970)]), DENIM_SH, 0.75)])
c.texture(bib, denim)
for sx in (296, 446):
    strap = M([(sx - 18, 652), (sx + 16, 652), (sx + 14, 768), (sx - 14, 768)])
    c.part(strap, DENIM, width=6, shade=[(shift(strap, 8, 0, S), DENIM_SH, 0.5)])
for bx in (298, 444):
    c.part(E(bx, 768, 14, 14), hexc('E0B04A'), width=4, light=[(E(bx - 4, 764, 5, 4), hexc('FFF0B0'), 0.9)])

# Arms crossed. The upper arms hang from the shoulders to the elbows; her left forearm (on the right of the
# picture) crosses behind, its hand tucked under the other arm; her right forearm crosses in front, its fist
# resting on the far upper arm.
def sleeve(pts, shaded):
    m = M(pts)
    c.part(m, PLAID, width=8, shade=[(shaded, PLAID_SH, 0.75)])
    c.texture(m, plaid)
    return m
sleeve([(450, 668), (506, 690), (536, 760), (540, 880), (504, 912), (466, 880), (452, 780)], M([(500, 690), (536, 760), (540, 880), (504, 912), (500, 800)]))
back = M([(512, 860), (528, 902), (500, 924), (330, 902), (300, 868), (330, 836), (470, 846)])
c.part(back, PLAID, width=8, shade=[(M([(300, 890), (512, 900), (500, 924), (330, 902)]), PLAID_SH, 0.75)])
c.texture(back, plaid)
sleeve([(292, 668), (236, 690), (206, 760), (202, 880), (238, 912), (276, 880), (290, 780)], M([(206, 840), (276, 860), (238, 912), (202, 880)]))
front = M([(214, 880), (240, 830), (420, 812), (470, 826), (476, 862), (440, 880), (250, 916)])
c.part(front, PLAID, width=8, shade=[(M([(214, 900), (476, 856), (476, 862), (440, 880), (250, 916)]), PLAID_SH, 0.8)],
       light=[(M([(250, 836), (420, 816), (420, 826), (256, 846)]), PLAID_LINE, 0.25)])
c.texture(front, plaid)
# The rolled-up cuff and the fist on the far arm.
cuff = M([(396, 810), (430, 806), (440, 870), (404, 878)])
c.part(cuff, PLAID_SH, width=6, light=[(M([(398, 812), (428, 808), (430, 822), (400, 826)]), PLAID_LINE, 0.5)])
fist = M([(430, 800), (480, 796), (506, 820), (502, 862), (470, 878), (436, 870)])
c.part(fist, SKIN, width=7, shade=[(M([(470, 800), (506, 820), (502, 862), (470, 878)]), SKIN_SH, 0.6)], light=[(E(450, 812, 12, 7), SKIN_HI, 0.8)])
for k in range(3): c.line([(470, 806 + k * 18), (500, 812 + k * 18)], 3.5)

# Neck and the neckerchief.
neck = M([(338, 600), (402, 600), (406, 666), (334, 666)])
c.part(neck, SKIN, width=6, shade=[(M([(338, 600), (406, 600), (406, 630), (338, 630)]), SKIN_SH, 0.6)])
scarf = M([(306, 648), (434, 648), (404, 700), (370, 748), (336, 700)])
c.part(scarf, RED, width=6, shade=[(M([(380, 648), (434, 648), (404, 700), (372, 746)]), RED_SH, 0.7)])
for (dx, dy) in ((338, 666), (372, 690), (406, 666), (366, 660), (372, 722)):
    c.dot(dx, dy, 4.5, (255, 240, 225, 255))

# Braids (behind the head, falling forward over the shoulders).
for bx, flip in ((250, -1), (490, 1)):
    for k in range(5):
        y = 520 + k * 46
        lobe = E(bx + flip * (k * 3), y, 34 - k * 2, 30)
        c.part(lobe, HAIR, width=6, shade=[(E(bx + flip * (k * 3) + 10, y + 10, 24, 20), HAIR_SH, 0.7)],
               light=[(E(bx + flip * (k * 3) - 10, y - 10, 10, 8), HAIR_HI, 0.8)])
    tie = E(bx + flip * 16, 752, 22, 12)
    c.part(tie, YELLOW, width=5)
    tuft = M([(bx + flip * 16 - 22, 760), (bx + flip * 16 + 22, 760), (bx + flip * 16 + 30, 806), (bx + flip * 16, 790), (bx + flip * 16 - 30, 806)])
    c.part(tuft, HAIR, width=5, shade=[(shift(tuft, 8, 6, S), HAIR_SH, 0.6)])

# Ears.
for ex in (232, 508):
    c.part(E(ex, 480, 24, 34), SKIN, width=6, shade=[(E(ex + 4, 486, 12, 20), SKIN_SH, 0.7)])

# Head.
head = M([(370, 330), (470, 360), (504, 450), (490, 560), (430, 618), (370, 630), (310, 618), (250, 560), (236, 450), (270, 360)])
c.part(head, SKIN, width=8,
       shade=[(M([(430, 340), (504, 450), (490, 560), (430, 618), (400, 626), (450, 520), (452, 400)]), SKIN_SH, 0.55)],
       light=[(E(300, 430, 40, 50), SKIN_HI, 0.45)])
# The brim's shadow over her forehead.
c.blend(ImageChops.multiply(c.mask(ellipse_pts(380, 392, 190, 46, -0.06), smooth=False), head), SKIN_SH, 0.55)
# Hair at the front, under the hat (fringe), and the sides.
fringe = M([(244, 400), (300, 352), (370, 344), (440, 352), (498, 400), (470, 420), (430, 398), (400, 420), (370, 400), (336, 424), (300, 400), (268, 428)])
c.part(fringe, HAIR, width=6, shade=[(M([(400, 360), (498, 400), (470, 420), (430, 398), (420, 380)]), HAIR_SH, 0.6)],
       light=[(M([(290, 372), (340, 360), (330, 376), (296, 386)]), HAIR_HI, 0.8)])

# Cheeks, freckles.
for bx in (300, 440):
    c.blend(E(bx, 540, 34, 22), BLUSH, 0.55)
    rnd = random.Random(bx)
    for _ in range(7):
        c.dot(bx + rnd.uniform(-26, 26), 522 + rnd.uniform(-12, 14), 2.6, hexc('B8664A'))

# Eyes: the left (her right) a little squinted, a raised brow on the other side: a grumpy, knowing look.
for ex, ey, squint in ((318, 478, 0.75), (424, 474, 1.0)):
    white = E(ex, ey, 30, 34 * squint)
    c.part(white, (255, 255, 255, 255), width=5)
    iris = E(ex + 4, ey + 3, 18, 20 * squint)
    c.part(iris, hexc('4E8D3E'), width=0, shade=[(E(ex + 8, ey + 9, 14, 12 * squint), hexc('2F5F27'), 0.7)])
    c.part(E(ex + 5, ey + 4, 9, 10 * squint), hexc('1E1A18'), width=0)
    c.dot(ex - 2, ey - 6 * squint, 5, (255, 255, 255, 255))
    lid = M([(ex - 34, ey - 6 * squint), (ex, ey - 30 * squint), (ex + 34, ey - 10 * squint), (ex + 34, ey - 44), (ex - 34, ey - 44)])
    c.part(lid, SKIN_SH, width=0)
    c.line([(ex - 32, ey - 6 * squint), (ex, ey - 30 * squint), (ex + 32, ey - 12 * squint)], 5)
c.line([(282, 432), (316, 426), (350, 440)], 13, HAIR_SH)       # low brow (frowning)
c.line([(392, 418), (424, 400), (458, 412)], 13, HAIR_SH)       # raised brow
# Nose: round and rosy.
nose = E(372, 520, 28, 24)
c.part(nose, SKIN, width=6, shade=[(E(382, 530, 20, 14), SKIN_SH, 0.7)], light=[(E(362, 512, 8, 6), SKIN_HI, 0.9)])
c.blend(E(372, 522, 22, 16), BLUSH, 0.35)
# Mouth: a lopsided smirk, a dimple.
mouth = M([(320, 572), (360, 578), (408, 566), (430, 548), (426, 562), (404, 584), (360, 592), (324, 582)])
c.part(mouth, hexc('7A2A22'), width=4)
c.part(M([(352, 580), (392, 576), (396, 584), (360, 588)]), (255, 250, 240, 255), width=0)
c.line([(432, 544), (440, 556)], 4)
# Smile lines.
c.line([(300, 560), (306, 584)], 3, SKIN_SH)

# The straw hat: the brim (tilted a little), the crown, a red band.
brim = c.mask(ellipse_pts(372, 352, 236, 58, -0.06), smooth=False)
c.part(brim, STRAW, width=8, shade=[(c.mask(ellipse_pts(390, 368, 220, 40, -0.06), smooth=False), STRAW_SH, 0.6)],
       light=[(c.mask(ellipse_pts(330, 336, 150, 20, -0.06), smooth=False), STRAW_HI, 0.7)])
def weave(d, s):
    for r in range(70, 240, 18):
        d.ellipse(((372 - r) * s, (352 - r * 0.25) * s, (372 + r) * s, (352 + r * 0.25) * s), outline=STRAW_SH[:3] + (140,), width=int(2 * s))
c.texture(brim, weave)
crown = M([(282, 344), (288, 262), (330, 226), (414, 222), (456, 258), (462, 342), (372, 356)])
c.part(crown, STRAW, width=8, shade=[(M([(400, 224), (456, 258), (462, 342), (410, 352)]), STRAW_SH, 0.65)],
       light=[(M([(300, 270), (330, 240), (340, 250), (312, 290)]), STRAW_HI, 0.8)])
def weave2(d, s):
    for y in range(226, 350, 14):
        d.line([(270 * s, y * s), (470 * s, (y - 4) * s)], fill=STRAW_SH[:3] + (120,), width=int(2 * s))
c.texture(crown, weave2)
band = M([(284, 310), (460, 304), (462, 338), (282, 344)])
c.part(band, RED, width=6, shade=[(M([(400, 306), (460, 304), (462, 338), (404, 340)]), RED_SH, 0.7)])
# A sunflower tucked in the band.
for k in range(10):
    a = k * math.pi / 5
    petal = E(300 + math.cos(a) * 18, 318 + math.sin(a) * 18, 14, 7, a)
    c.part(petal, YELLOW, width=3)
c.part(E(300, 318, 12, 12), hexc('6B3F1E'), width=3)
# A strand of straw in the corner of her mouth.
c.line([(424, 566), (470, 552), (500, 540)], 5, STRAW_SH)
c.line([(424, 566), (470, 552), (500, 540)], 3, STRAW)

portrait_source = c.result()  # before the pitchfork (it would poke into the portrait)
# ---------------------------------------------------------------- the pitchfork, planted beside her
handle = M([(120, 1338), (136, 1340), (176, 600), (160, 598)])
c.part(handle, WOOD, width=6, shade=[(M([(129, 1340), (136, 1340), (176, 600), (170, 598)]), WOOD_SH, 0.8)])
head = M([(120, 604), (210, 598), (212, 616), (122, 622)])
c.part(head, STEEL, width=5, shade=[(shift(head, 0, 6, S), STEEL_SH, 0.5)])
for i, tx in enumerate((124, 152, 180, 206)):
    tine = M([(tx - 5, 608), (tx + 5, 608), (tx + 4 + (i - 1.5) * 4, 462), (tx - 4 + (i - 1.5) * 4, 462)])
    c.part(tine, STEEL, width=4, light=[(M([(tx - 4, 606), (tx - 1, 606), (tx - 2 + (i - 1.5) * 4, 466), (tx - 4 + (i - 1.5) * 4, 466)]), CHROME_HI, 0.8)])

out = c.result()
os.makedirs(OUT, exist_ok=True)
out.save(os.path.join(OUT, 'RosieRustPainting.png'))

# The portrait: her head and shoulders in a circle, on a warm field-yellow disc.
size = 512
portrait = Image.new('RGBA', (size, size), (0, 0, 0, 0))
disc = Image.new('RGBA', (size, size), (0, 0, 0, 0))
dd = ImageDraw.Draw(disc)
dd.ellipse((0, 0, size - 1, size - 1), fill=hexc('173D33'))
dd.ellipse((14, 14, size - 15, size - 15), fill=hexc('F6D68A'))
crop = portrait_source.crop((128, 196, 616, 684)).resize((size - 28, size - 28), Image.LANCZOS)
inner = Image.new('L', (size, size), 0)
ImageDraw.Draw(inner).ellipse((14, 14, size - 15, size - 15), fill=255)
layer = Image.new('RGBA', (size, size), (0, 0, 0, 0))
layer.paste(crop, (14, 14))
disc = Image.alpha_composite(disc, Image.composite(layer, Image.new('RGBA', (size, size), (0, 0, 0, 0)), inner))
disc.save(os.path.join(OUT, 'RosieRust.png'))
print('painted', out.size)
