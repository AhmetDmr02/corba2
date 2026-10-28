"""Model recipes for Corba. Every model faces -Y in Blender (+Z in Unity), base on z=0."""
import math
from corba_art import box, cyl, ball, ico, ring, bowl, lathe, leaf, limb, eyes, RNG

SKIN = "#f2c49b"
INK = "#2a1a10"
WOOD = "#9a6234"
WOOD_DARK = "#6b4022"
STONE = "#a39a8c"
IRON = "#4a4a52"
COPPER = "#c8743a"
CREAM = "#f4ead2"
GOLD = "#f2b33d"
LEAF = "#5f9e3c"
LEAF_DARK = "#3f7a34"
MINT = "#7cc98e"
GARLIC = "#efe6f2"
LAVENDER = "#a88fc9"
CHILI = "#d9442b"
AMBER = "#c77a35"
TERRACOTTA = "#c0623a"


# ---------------------------------------------------------------- characters

def chef(apron, scarf, hat_band):
    for s in (-1, 1):
        limb((s * 0.12, 0, 0.08), (s * 0.12, 0, 0.42), 0.09, "#4a3a32")
        ball(0.11, (s * 0.12, -0.05, 0.06), scale=(1, 1.4, 0.6), color="#3a2418")
    ball(0.34, (0, 0, 0.72), scale=(1, 0.85, 1.05), color=CREAM)
    ball(0.33, (0, -0.07, 0.62), scale=(0.95, 0.75, 0.85), color=apron)
    ring(0.3, 0.045, (0, 0, 0.66), color=apron, scale=(1, 0.85, 1))
    ring(0.15, 0.06, (0, 0, 1.0), color=scarf)
    ball(0.07, (0.08, -0.16, 0.95), color=scarf)
    for z in (0.88, 0.76):
        ball(0.03, (0, -0.3, z), color="#d9c08a", segs=8, rings=6)
    ball(0.27, (0, 0, 1.27), color=SKIN)
    ball(0.065, (0, -0.26, 1.25), color="#e9a77f")
    ball(0.075, (-0.075, -0.24, 1.19), scale=(1.4, 0.55, 0.5), rot=(0, -12, 0), color=INK)
    ball(0.075, (0.075, -0.24, 1.19), scale=(1.4, 0.55, 0.5), rot=(0, 12, 0), color=INK)
    eyes(0, -0.235, 1.33, spread=0.09, size=0.035)
    ball(0.05, (-0.16, -0.18, 1.24), scale=(1, 0.5, 0.7), color="#ef8f7a")
    ball(0.05, (0.16, -0.18, 1.24), scale=(1, 0.5, 0.7), color="#ef8f7a")
    cyl(0.21, 0.24, (0, 0.02, 1.55), color="#fbf6ea", verts=14, r2=0.24)
    ring(0.215, 0.03, (0, 0.02, 1.46), color=hat_band, major_seg=16)
    for dx, dy in ((-0.11, 0), (0.11, 0), (0, -0.09), (0, 0.11), (0, 0)):
        ball(0.15, (dx, dy + 0.02, 1.74 + (0.05 if dx == 0 and dy == 0 else 0)), color="#fffaf0")
    for s in (-1, 1):
        limb((s * 0.3, -0.02, 0.92), (s * 0.27, -0.42, 0.9), 0.085, CREAM, r2=0.07)
        ball(0.075, (s * 0.25, -0.48, 0.92), color=SKIN)


def minion(body, legs="#4a3a32", head=SKIN, z=0.0, size=1.0, face=True):
    s = size
    for side in (-1, 1):
        limb((side * 0.1 * s, 0, 0.06 * s), (side * 0.1 * s, 0, 0.26 * s), 0.07 * s, legs)
        ball(0.085 * s, (side * 0.1 * s, -0.04 * s, 0.05 * s), scale=(1, 1.4, 0.6), color="#3a2418")
    ball(0.26 * s, (0, 0, 0.46 * s), scale=(1, 0.9, 1.05), color=body)
    if head:
        ball(0.22 * s, (0, 0, 0.86 * s), color=head)
        if face:
            eyes(0, -0.2 * s, 0.9 * s, spread=0.075 * s, size=0.032 * s)
            ball(0.045 * s, (0, -0.215 * s, 0.84 * s), color="#e9a77f")


def arm(side, start, end, color, hand=SKIN, r=0.065):
    limb(start, end, r, color, r2=r * 0.85)
    ball(r * 1.05, end, color=hand)


def pot():
    bowl(0.28, 0.36, color="#7a7f86", inner="#3a3030", verts=18, thick=0.045)
    cyl(0.06, 0.05, (0.3, 0, 0.26), (0, 90, 0), color="#5b5f66", verts=8)
    cyl(0.06, 0.05, (-0.3, 0, 0.26), (0, 90, 0), color="#5b5f66", verts=8)
    ring(0.29, 0.025, (0, 0, 0.12), color=COPPER)


# ---------------------------------------------------------------- attack units

def u_grunt():
    minion(GOLD)
    ball(0.235, (0, 0.01, 0.95), scale=(1, 1, 0.62), color=IRON)
    ring(0.23, 0.03, (0, 0.01, 0.92), color="#6a6a74")
    cyl(0.03, 0.06, (0, 0.01, 1.11), color=IRON, verts=8)
    ring(0.25, 0.04, (0, 0, 0.42), color=WOOD_DARK)
    arm(1, (0.24, 0, 0.55), (0.32, -0.16, 0.5), GOLD)
    arm(-1, (-0.24, 0, 0.55), (-0.3, -0.18, 0.45), GOLD)
    limb((0.33, -0.18, 0.1), (0.33, -0.18, 1.25), 0.03, WOOD)
    ball(0.1, (0.33, -0.18, 1.33), scale=(1, 0.45, 1.35), color="#c9925a")


def u_shield():
    minion(COPPER, size=1.05)
    ball(0.25, (0, 0.01, 1.0), scale=(1, 1, 0.6), color=IRON)
    ring(0.24, 0.035, (0, 0.01, 0.96), color=COPPER)
    cyl(0.45, 0.07, (0, -0.36, 0.55), (90, 0, 0), color="#6d6f78", verts=20, bevel=0.025)
    ring(0.43, 0.035, (0, -0.4, 0.55), (90, 0, 0), color=COPPER, major_seg=24)
    ball(0.08, (0, -0.42, 0.55), color=COPPER)
    for a in range(0, 360, 90):
        r = math.radians(a)
        ball(0.035, (math.cos(r) * 0.3, -0.41, 0.55 + math.sin(r) * 0.3), color="#d9c08a")
    arm(1, (0.25, 0, 0.6), (0.3, -0.25, 0.55), COPPER)


def u_runner():
    lathe([(0.0, 0.05), (0.08, 0.12), (0.2, 0.35), (0.27, 0.6), (0.26, 0.8), (0.18, 0.92), (0.0, 0.96)],
          verts=14, color=CHILI, rot=(-8, 0, 0))
    for side in (-1, 1):
        limb((side * 0.09, 0.02, 0.32), (side * 0.13, -0.08, 0.04), 0.045, LEAF_DARK)
        ball(0.06, (side * 0.13, -0.12, 0.03), scale=(1, 1.5, 0.6), color="#3a2418")
        limb((side * 0.22, -0.02, 0.58), (side * 0.33, 0.08, 0.45), 0.04, LEAF_DARK)
    for i in range(5):
        a = math.radians(i * 72)
        leaf(0.16, 0.1, (0, 0.06, 0.92), (35, 0, i * 72), LEAF)
    limb((0, 0.06, 0.95), (0.05, 0.12, 1.12), 0.035, LEAF_DARK)
    eyes(0, -0.24, 0.66, spread=0.08, size=0.04)
    box((0.1, 0.03, 0.025), (-0.08, -0.25, 0.73), (0, 20, 0), color=INK, bevel=0.01)
    box((0.1, 0.03, 0.025), (0.08, -0.25, 0.73), (0, -20, 0), color=INK, bevel=0.01)
    ball(0.04, (0, -0.255, 0.55), scale=(1.4, 0.5, 0.6), color="#5a1a12")


def u_ember():
    minion("#b8402a")
    ball(0.23, (0, 0.02, 0.96), scale=(1, 1, 0.55), color="#e8d4a8")
    ball(0.07, (0.15, 0.15, 0.95), color="#e8d4a8")
    ring(0.27, 0.04, (0, 0, 0.42), color=INK)
    box((0.5, 0.32, 0.16), (0, -0.38, 0.5), color=IRON, bevel=0.03)
    for x in (-0.16, 0, 0.16):
        for y in (-0.44, -0.32):
            ball(0.07, (x + RNG.uniform(-0.02, 0.02), y, 0.6), color="#ff8a2e", segs=8, rings=6, wob=0.01)
    ball(0.06, (0.05, -0.38, 0.66), color="#ffd86a", segs=8, rings=6)
    for side in (-1, 1):
        arm(side, (side * 0.24, 0, 0.55), (side * 0.27, -0.3, 0.52), "#b8402a")


def u_healer():
    lathe([(0.0, 0.0), (0.34, 0.0), (0.3, 0.2), (0.24, 0.5), (0.2, 0.72), (0.0, 0.76)], verts=16, color=MINT)
    ring(0.25, 0.035, (0, 0, 0.48), color=CREAM)
    ball(0.21, (0, 0, 0.9), color=SKIN)
    eyes(0, -0.19, 0.93, spread=0.07, size=0.03)
    for i in range(6):
        leaf(0.22, 0.14, (0, 0, 1.04), (55, 0, i * 60), MINT if i % 2 else LEAF)
    arm(1, (0.2, 0, 0.62), (0.3, -0.18, 0.6), MINT)
    arm(-1, (-0.2, 0, 0.62), (-0.28, -0.16, 0.56), MINT)
    limb((0.33, -0.2, 0.05), (0.33, -0.2, 1.3), 0.025, WOOD)
    bowl(0.1, 0.08, (0.33, -0.2, 1.3), color="#d9d0bd", inner="#efe9d2", verts=12, thick=0.02, lip=False)
    leaf(0.14, 0.09, (0.33, -0.2, 1.36), (60, 0, 30), LEAF)


def u_golem():
    ico(0.42, (0, 0, 0.42), scale=(1, 0.9, 0.95), color="#f3efe4", sub=2, wob=0.03, smooth=True)
    ico(0.28, (0, -0.02, 0.92), color="#f7f4ec", sub=2, wob=0.02, smooth=True)
    for side in (-1, 1):
        ico(0.15, (side * 0.45, -0.08, 0.45), color="#efeadb", sub=2, wob=0.02, smooth=True)
        ico(0.13, (side * 0.18, -0.02, 0.06), scale=(1, 1.2, 0.6), color="#e7e0cf", sub=2, smooth=True)
    eyes(0, -0.26, 0.97, spread=0.09, size=0.04)
    ball(0.05, (0, -0.27, 0.86), scale=(1.6, 0.5, 0.5), color="#6a5040")
    bowl(0.16, 0.14, (0, -0.02, 1.15), color=COPPER, inner="#f3efe4", verts=14, thick=0.025)
    for i in range(7):
        a = RNG.uniform(0, math.tau)
        ball(0.035, (math.cos(a) * 0.4, math.sin(a) * 0.36 - 0.05, RNG.uniform(0.2, 0.7)), color=LEAF, segs=6, rings=4)


def u_saboteur():
    minion("#7a4a2a", head=None)
    lathe([(0.0, 0.62), (0.25, 0.62), (0.26, 0.85), (0.2, 1.0), (0.08, 1.14), (0.0, 1.18)], verts=14, color=AMBER, rot=(-10, 0, 0))
    ball(0.17, (0, -0.06, 0.83), scale=(1, 0.8, 1), color="#2e1e14")
    ball(0.035, (-0.06, -0.19, 0.86), color="#ffd86a", segs=8, rings=6)
    ball(0.035, (0.06, -0.19, 0.86), color="#ffd86a", segs=8, rings=6)
    ball(0.22, (0, 0.24, 0.55), scale=(1, 0.8, 1.1), color="#b89060", wob=0.02)
    ring(0.07, 0.025, (0, 0.24, 0.78), color=WOOD_DARK)
    arm(1, (0.22, 0, 0.55), (0.3, -0.2, 0.5), "#7a4a2a")
    limb((0.3, -0.22, 0.5), (0.32, -0.42, 0.62), 0.02, "#c9c9c9")


def u_cloud():
    for (x, y, z, r) in ((0, 0, 0.55, 0.4), (-0.32, 0.02, 0.5, 0.28), (0.33, 0.0, 0.52, 0.3),
                         (0.12, 0.05, 0.82, 0.3), (-0.15, 0.08, 0.78, 0.26), (0, 0.18, 0.55, 0.32)):
        ico(r, (x, y, z), color="#dba46a", sub=2, wob=0.02, smooth=True)
    eyes(0, -0.37, 0.6, spread=0.1, size=0.045)
    box((0.12, 0.03, 0.03), (-0.1, -0.38, 0.69), (0, -15, 0), color=INK, bevel=0.01)
    box((0.12, 0.03, 0.03), (0.1, -0.38, 0.69), (0, 15, 0), color=INK, bevel=0.01)
    ball(0.05, (0, -0.39, 0.45), scale=(1.5, 0.4, 0.8), color="#5a2a12")
    for x in (-0.25, 0.05, 0.3):
        ball(0.05, (x, -0.1, 0.18), scale=(1, 1, 1.6), color="#b5562f", segs=8, rings=6)


def u_brute():
    minion("#8c7aa8", legs="#4a3a52", head=None, size=1.25)
    lathe([(0.0, 0.92), (0.2, 0.95), (0.3, 1.08), (0.31, 1.2), (0.24, 1.34), (0.1, 1.44), (0.04, 1.6), (0.0, 1.62)],
          verts=12, color=GARLIC, wob=0.012)
    for i in range(6):
        a = math.radians(i * 60)
        limb((math.cos(a) * 0.12, math.sin(a) * 0.12, 0.98), (math.cos(a) * 0.2, math.sin(a) * 0.2, 1.42), 0.012, "#c9b3e6")
    eyes(0, -0.29, 1.18, spread=0.1, size=0.045)
    box((0.14, 0.03, 0.035), (-0.1, -0.3, 1.27), (0, 25, 0), color=INK, bevel=0.01)
    box((0.14, 0.03, 0.035), (0.1, -0.3, 1.27), (0, -25, 0), color=INK, bevel=0.01)
    ball(0.06, (0, -0.3, 1.06), scale=(1.8, 0.4, 0.5), color="#3a1a20")
    for side in (-1, 1):
        arm(side, (side * 0.32, 0, 0.7), (side * 0.42, -0.2, 0.5), "#8c7aa8", hand="#d8c7e8", r=0.1)
    limb((0.42, -0.25, 0.45), (0.48, -0.45, 1.25), 0.07, WOOD, r2=0.13)
    for i in range(3):
        ball(0.05, (0.47 + RNG.uniform(-0.05, 0.05), -0.45, 1.0 + i * 0.1), color=WOOD_DARK, segs=6, rings=4)


def u_bomber():
    minion("#7d5aa0")
    ball(0.23, (0, 0.02, 0.97), scale=(1, 1, 0.5), color="#5a3a7a")
    ring(0.27, 0.04, (0, 0, 0.42), color=WOOD_DARK)
    cyl(0.17, 0.5, (0, 0.28, 0.6), color="#5aa05a", verts=14, bevel=0.04)
    cyl(0.07, 0.18, (0, 0.28, 0.93), color="#5aa05a", verts=10)
    cyl(0.08, 0.08, (0, 0.28, 1.05), color="#c9925a", verts=10)
    limb((0, 0.28, 1.09), (0.08, 0.32, 1.25), 0.015, INK)
    ball(0.05, (0.08, 0.32, 1.27), color="#ffd86a", segs=8, rings=6)
    ball(0.08, (0, 0.28, 0.75), scale=(1.8, 0.5, 1), color=CREAM)
    for side in (-1, 1):
        arm(side, (side * 0.24, 0, 0.55), (side * 0.25, -0.2, 0.62), "#7d5aa0")


# ---------------------------------------------------------------- defences

def stone_base(r=0.95, h=0.45, color=STONE):
    cyl(r, h, (0, 0, h / 2), color=color, verts=10, bevel=0.06, wob=0.03)
    ring(r * 0.98, 0.06, (0, 0, h), color=WOOD_DARK, major_seg=16)
    for i in range(7):
        a = i / 7 * math.tau + 0.3
        ico(0.16, (math.cos(a) * r * 1.02, math.sin(a) * r * 1.02, 0.1), color="#8f877b", wob=0.03)


def d_catapult():
    box((1.8, 1.6, 0.35), (0, 0, 0.18), color=STONE, bevel=0.06, wob=0.02)
    for x in (-0.55, 0.55):
        limb((x, 0.45, 0.35), (x, 0.0, 1.5), 0.08, WOOD)
        limb((x, -0.45, 0.35), (x, 0.0, 1.5), 0.08, WOOD)
    limb((-0.65, 0, 1.45), (0.65, 0, 1.45), 0.09, WOOD_DARK)
    limb((0, 0.55, 1.1), (0, -1.0, 2.35), 0.07, WOOD)
    bowl(0.42, 0.3, (0, -1.05, 2.25), color=GOLD, inner="#e8962e", verts=16, thick=0.05)
    ball(0.3, (0, -1.05, 2.5), scale=(1, 1, 0.45), color="#e8962e")
    for (x, y) in ((0.75, 0.55), (-0.75, 0.6), (0.7, -0.6)):
        ball(0.25, (x, y, 0.55), scale=(1, 0.9, 1.15), color="#c9a15a", wob=0.02)
        ring(0.08, 0.03, (x, y, 0.82), color=WOOD_DARK)
    ring(0.2, 0.05, (0, 0, 1.45), (0, 90, 0), color=IRON)


def d_barricade():
    for x in (-2.0, 2.0):
        limb((x, 0, 0.0), (x, 0, 1.45), 0.11, WOOD_DARK)
        ball(0.13, (x, 0, 1.5), color=WOOD)
    for row, z in enumerate((0.25, 0.65, 1.0)):
        n = 7 - row * 2
        for i in range(n):
            x = (i - (n - 1) / 2) * 0.58
            ball(0.32, (x + RNG.uniform(-0.04, 0.04), RNG.uniform(-0.05, 0.05), z), scale=(1, 0.75, 0.65), color="#c9a15a" if (i + row) % 2 else "#b88c48", wob=0.025)
            ring(0.07, 0.025, (x - 0.22, 0, z + 0.05), (0, 90, 0), color=WOOD_DARK)
    box((4.2, 0.12, 0.18), (0, 0.32, 0.55), (0, 0, 0), color=WOOD, bevel=0.03, wob=0.02)
    for i in range(10):
        ball(0.05, (RNG.uniform(-1.8, 1.8), RNG.uniform(-0.45, 0.45), 0.03), scale=(1, 1, 0.5), color="#e8962e", segs=6, rings=4)


def d_chili():
    stone_base(0.9, 0.5, "#b0604a")
    lathe([(0.0, 0.5), (0.55, 0.55), (0.62, 1.0), (0.58, 1.6), (0.48, 2.2), (0.32, 2.75), (0.15, 3.05), (0.0, 3.1)],
          verts=16, color=CHILI, rot=(0, 0, 0), wob=0.015)
    for i in range(6):
        leaf(0.4, 0.22, (0, 0, 3.0), (40, 0, i * 60), LEAF)
    limb((0, 0, 3.05), (0.12, -0.08, 3.45), 0.07, LEAF_DARK)
    box((0.3, 0.1, 0.4), (0, -0.6, 1.35), color=INK, bevel=0.06)
    box((0.42, 0.12, 0.1), (0, -0.61, 1.12), color=WOOD, bevel=0.03)
    for i in range(14):
        a = RNG.uniform(0, math.tau)
        z = RNG.uniform(0.7, 2.6)
        r = 0.6 - (z - 0.7) * 0.12
        ball(0.04, (math.cos(a) * r, math.sin(a) * r, z), color="#8a1a12", segs=6, rings=4)


def d_griddle():
    for i in range(5):
        a = i / 5 * math.tau
        ico(0.22, (math.cos(a) * 1.15, math.sin(a) * 1.15, 0.12), color="#8f877b", wob=0.03)
    for i in range(16):
        a = i / 16 * math.tau + RNG.uniform(-0.1, 0.1)
        rr = RNG.uniform(1.45, 1.75)
        ball(0.13, (math.cos(a) * rr, math.sin(a) * rr, 0.07), scale=(1, 1, 0.6), color="#ff8a2e" if i % 3 else "#ffd86a", segs=8, rings=5)
    lathe([(0.0, 0.42), (0.6, 0.38), (1.2, 0.27), (1.55, 0.18), (1.6, 0.14), (1.5, 0.13), (0.0, 0.33)], verts=24, color="#4a4048")
    ring(1.57, 0.035, (0, 0, 0.16), color="#5a5a64", major_seg=28)
    for i in range(8):
        a = RNG.uniform(0, math.tau)
        rr = RNG.uniform(0.3, 1.2)
        ball(0.07, (math.cos(a) * rr, math.sin(a) * rr, 0.42 - rr * 0.18), scale=(1.4, 1, 0.3), color="#c9502b", segs=8, rings=4)


def d_mint():
    stone_base(0.85, 0.4)
    lathe([(0.0, 0.4), (0.5, 0.42), (0.68, 0.8), (0.72, 1.4), (0.62, 2.0), (0.45, 2.25), (0.48, 2.4), (0.0, 2.42)],
          verts=18, color="#f4f0e6")
    ring(0.7, 0.05, (0, 0, 1.15), color="#5a8fc9", major_seg=20)
    ring(0.66, 0.04, (0, 0, 1.75), color="#5a8fc9", major_seg=20)
    for i in range(8):
        tilt = 25 + (i % 2) * 25
        leaf(0.75, 0.42, (0, 0, 2.38), (tilt, 0, i * 45), MINT if i % 2 else LEAF)
    limb((0, 0, 2.38), (0, 0, 3.2), 0.05, LEAF_DARK)
    for i in range(4):
        leaf(0.4, 0.25, (0, 0, 2.7 + i * 0.12), (30, 0, i * 90 + 45), MINT)


def d_yogurt():
    lathe([(0.0, 0.08), (0.8, 0.07), (1.35, 0.05), (1.6, 0.02), (1.62, 0.0), (0.0, 0.0)], verts=24, color="#f6f2e8", wob=0.02)
    for i in range(6):
        a = RNG.uniform(0, math.tau)
        rr = RNG.uniform(0.2, 1.2)
        leaf(0.22, 0.13, (math.cos(a) * rr, math.sin(a) * rr, 0.09), (5, 0, RNG.uniform(0, 360)), MINT)
    bowl(0.42, 0.32, (1.35, 0.9, 0.0), color=WOOD, inner="#f6f2e8", verts=14, thick=0.05)
    for i in range(3):
        ball(0.12, (RNG.uniform(-0.8, 0.8), RNG.uniform(-0.8, 0.8), 0.09), scale=(1.4, 1, 0.3), color="#ffffff", segs=8, rings=4)


def d_ferment():
    stone_base(0.95, 0.35)
    lathe([(0.0, 0.35), (0.45, 0.36), (0.8, 0.8), (0.92, 1.4), (0.82, 2.0), (0.5, 2.4), (0.35, 2.5), (0.42, 2.62), (0.0, 2.64)],
          verts=18, color=TERRACOTTA, wob=0.01)
    ball(0.45, (0, 0, 2.62), scale=(1, 1, 0.4), color="#e8d4a8")
    ring(0.4, 0.04, (0, 0, 2.55), color=WOOD_DARK, major_seg=18)
    ring(0.9, 0.04, (0, 0, 1.45), color="#e8d4a8", major_seg=20)
    for i in range(10):
        a = RNG.uniform(0, math.tau)
        ball(RNG.uniform(0.06, 0.13), (math.cos(a) * 0.25, math.sin(a) * 0.25, 2.78 + RNG.uniform(0, 0.3)), color="#e0a060", segs=8, rings=5)
    for i in range(5):
        a = RNG.uniform(0, math.tau)
        ball(0.08, (math.cos(a) * 0.86, math.sin(a) * 0.86, RNG.uniform(1.0, 1.9)), scale=(0.6, 0.6, 1.6), color=AMBER, segs=8, rings=5)


def d_barrel():
    lathe([(0.0, 0.0), (0.42, 0.0), (0.5, 0.3), (0.53, 0.6), (0.5, 0.9), (0.42, 1.2), (0.0, 1.2)], verts=18, color=WOOD)
    for z, r in ((0.15, 0.47), (0.6, 0.545), (1.05, 0.47)):
        ring(r, 0.035, (0, 0, z), color=IRON, major_seg=20)
    ball(0.36, (0, 0, 1.2), scale=(1, 1, 0.15), color="#b5562f")
    for i in range(4):
        a = i * 1.6
        ball(0.06, (math.cos(a) * 0.48, math.sin(a) * 0.48, 1.0), scale=(0.6, 0.6, 2.0), color="#b5562f", segs=8, rings=5)
    box((0.3, 0.04, 0.22), (0, -0.52, 0.65), (8, 0, 0), color=CREAM, bevel=0.01)
    ico(0.08, (0, -0.55, 0.66), scale=(1, 0.4, 1), color="#5a8a3a")


def d_garlic():
    stone_base(0.9, 0.45)
    lathe([(0.0, 0.45), (0.5, 0.48), (0.9, 0.85), (1.0, 1.35), (0.85, 1.85), (0.45, 2.25), (0.15, 2.55), (0.1, 2.9), (0.0, 2.95)],
          verts=12, color=GARLIC, wob=0.02)
    for i in range(6):
        a = math.radians(i * 60 + 15)
        limb((math.cos(a) * 0.55, math.sin(a) * 0.55, 0.55), (math.cos(a) * 0.95, math.sin(a) * 0.95, 1.35), 0.03, LAVENDER)
        limb((math.cos(a) * 0.95, math.sin(a) * 0.95, 1.35), (math.cos(a) * 0.3, math.sin(a) * 0.3, 2.4), 0.025, LAVENDER)
    for i in range(3):
        leaf(0.6, 0.12, (0, 0, 2.9), (20, 0, i * 120), "#9ab86a")
    for i in range(5):
        a = RNG.uniform(0, math.tau)
        limb((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.46), (math.cos(a) * 0.55, math.sin(a) * 0.55, 0.4), 0.02, CREAM)


def d_snare():
    for x, y in ((-0.9, -0.5), (0.6, 0.6), (0.1, -0.9)):
        ring(0.38, 0.045, (x, y, 0.05), color="#c9a46a", major_seg=16, minor_seg=6)
        limb((x + 0.38, y, 0.0), (x + 0.42, y, 0.4), 0.04, WOOD_DARK)
    limb((-0.52, -0.5, 0.05), (0.98, 0.6, 0.05), 0.03, "#c9a46a")
    cyl(0.18, 0.5, (1.25, -0.6, 0.25), color="#6fae6a", verts=14, bevel=0.04)
    cyl(0.07, 0.2, (1.25, -0.6, 0.6), color="#6fae6a", verts=10)
    cyl(0.08, 0.08, (1.25, -0.6, 0.73), color="#c9925a", verts=10)
    box((0.2, 0.04, 0.18), (1.25, -0.79, 0.3), color=CREAM, bevel=0.01)
    for i in range(5):
        ball(0.1, (RNG.uniform(-1.2, 1.2), RNG.uniform(-1.1, 1.1), 0.02), scale=(1.5, 1, 0.2), color="#d7e37a", segs=8, rings=4)


# ---------------------------------------------------------------- map

def cauldron():
    for i in range(9):
        a = i / 9 * math.tau
        ico(0.32, (math.cos(a) * 1.15, math.sin(a) * 1.15, 0.16), scale=(1.2, 1, 0.75), color="#8f877b" if i % 2 else STONE, wob=0.04)
    for i in range(4):
        a = i / 4 * math.tau + 0.4
        limb((math.cos(a) * 0.85, math.sin(a) * 0.85, 0.06), (math.cos(a) * 0.1, math.sin(a) * 0.1, 0.18), 0.1, WOOD_DARK if i % 2 else WOOD)
    for i in range(7):
        ball(RNG.uniform(0.13, 0.22), (RNG.uniform(-0.45, 0.45), RNG.uniform(-0.45, 0.45), 0.24), scale=(1, 1, 1.5), color="#ff8a2e" if i % 2 else "#ffd86a", segs=8, rings=6)
    bowl(1.02, 1.08, (0, 0, 0.24), color=IRON, inner=None, verts=24, thick=0.07)
    ring(1.04, 0.06, (0, 0, 0.62), color=COPPER, major_seg=28)
    ring(0.99, 0.05, (0, 0, 1.0), color="#5a5a64", major_seg=28)
    for s in (-1, 1):
        ring(0.16, 0.045, (s * 1.08, 0, 1.12), (90, 0, 0), color="#6a6a74")
    limb((0.55, -0.35, 1.0), (1.05, -0.8, 2.1), 0.05, WOOD)
    bowl(0.2, 0.12, (1.08, -0.84, 2.12), color=WOOD, inner=None, verts=10, thick=0.03, lip=False)


def flag_high():
    limb((0, 0, 0), (0, 0, 1.9), 0.04, WOOD_DARK)
    box((0.03, 0.62, 0.42), (0, 0.31, 1.62), color=GOLD, bevel=0.015, wob=0.01)
    cyl(0.12, 0.16, (0, 0.3, 1.62), (0, 90, 0), color="#f4ead2", verts=3, bevel=0)
    ball(0.07, (0, 0, 1.95), color=GOLD)


def ocak():
    for i in range(12):
        a = i / 12 * math.tau
        ico(0.3, (math.cos(a) * 1.35, math.sin(a) * 1.35, 0.2), scale=(1, 1, 0.8), color="#8f877b" if i % 2 else STONE, wob=0.04)
    for i in range(5):
        a = i / 5 * math.tau
        limb((math.cos(a) * 0.9, math.sin(a) * 0.9, 0.08), (math.cos(a) * 0.15, math.sin(a) * 0.15, 0.25), 0.09, WOOD_DARK)
    for i in range(7):
        ball(RNG.uniform(0.12, 0.22), (RNG.uniform(-0.35, 0.35), RNG.uniform(-0.35, 0.35), 0.3), scale=(1, 1, 1.4), color="#ff8a2e" if i % 2 else "#ffd86a", segs=8, rings=6)
    for i in range(3):
        a = i / 3 * math.tau + 0.5
        limb((math.cos(a) * 1.1, math.sin(a) * 1.1, 0.0), (0, 0, 2.4), 0.06, WOOD)
    limb((0, 0, 2.4), (0, 0, 1.7), 0.02, IRON)
    bowl(0.7, 0.75, (0, 0, 0.95), color=COPPER, inner="#3a3030", verts=20, thick=0.06)
    ring(0.71, 0.05, (0, 0, 1.3), color="#a85a2a", major_seg=22)


def sofra():
    cyl(1.9, 0.04, (0, 0, 0.02), color="#a8352a", verts=28, bevel=0.01)
    ring(1.8, 0.04, (0, 0, 0.04), color=GOLD, major_seg=32)
    for i in range(6):
        a = i / 6 * math.tau
        ball(0.36, (math.cos(a) * 1.55, math.sin(a) * 1.55, 0.14), scale=(1, 1, 0.35), color=("#d9442b", "#f2b33d", "#3f7fbf")[i % 3])
    cyl(1.15, 0.1, (0, 0, 0.4), color=WOOD, verts=24, bevel=0.03)
    cyl(0.5, 0.35, (0, 0, 0.18), color=WOOD_DARK, verts=12)
    soups = ("#e8962e", "#c9502b", "#efe9d2", "#b5562f", "#e6dcc4")
    for i in range(5):
        a = i / 5 * math.tau
        bowl(0.17, 0.12, (math.cos(a) * 0.72, math.sin(a) * 0.72, 0.45), color="#f4f0e6", inner=soups[i], verts=14, thick=0.025)
    ball(0.28, (0, 0, 0.55), scale=(1, 0.7, 0.45), color="#d9a05a")
    for i in range(4):
        limb((-0.18 + i * 0.12, -0.05, 0.66), (-0.14 + i * 0.12, 0.05, 0.66), 0.012, "#a86a2a")
    cyl(0.3, 0.08, (0.35, 0.3, 0.49), color="#c9925a", verts=14)


def pad():
    cyl(1.05, 0.06, (0, 0, 0.03), color="#b5a58a", verts=20, bevel=0.02)
    for i in range(10):
        a = i / 10 * math.tau
        ico(0.2, (math.cos(a) * 1.1, math.sin(a) * 1.1, 0.06), scale=(1.3, 1, 0.5), color=STONE if i % 2 else "#8f877b", wob=0.02)


def soup_sign():
    limb((0, 0, 0), (0, 0, 1.5), 0.05, WOOD_DARK)
    box((0.7, 0.08, 0.42), (0, -0.07, 1.3), color="#f6efe0", bevel=0.04, wob=0.01)
    bowl(0.16, 0.12, (0, 0, 1.52), color="#f6efe0", inner="#e8962e", verts=12, thick=0.02)


def banner():
    limb((0, 0, 0), (0, 0, 3.2), 0.05, WOOD_DARK)
    ball(0.1, (0, 0, 3.25), color=GOLD)
    box((0.04, 1.0, 0.7), (0, 0.5, 2.75), color=CHILI, bevel=0.02, wob=0.02)
    ball(0.15, (0, 0.6, 2.75), scale=(0.3, 1, 1), color=GOLD)


def pip():
    cyl(0.5, 0.18, (0, 0, 0.09), color=GOLD, verts=16, bevel=0.04)
    ball(0.2, (0, 0, 0.2), scale=(1, 1, 0.4), color="#ffd86a")


def mud():
    lathe([(0.0, 0.03), (0.6, 0.03), (0.95, 0.015), (1.0, 0.0), (0.0, 0.0)], verts=20, color="#6b4a2e", wob=0.04)
    for i in range(4):
        ball(0.15, (RNG.uniform(-0.5, 0.5), RNG.uniform(-0.5, 0.5), 0.03), scale=(1.5, 1, 0.15), color="#4e3420", segs=8, rings=4)


def rock_small():
    ico(0.35, (0, 0, 0.12), scale=(1.2, 1, 0.65), color=STONE, sub=1, wob=0.05)
    ico(0.18, (0.3, 0.15, 0.06), scale=(1, 1, 0.6), color="#8f877b", wob=0.03)


def rock_big():
    ico(0.8, (0, 0, 0.35), scale=(1.2, 1, 0.8), color=STONE, sub=1, wob=0.1)
    ico(0.45, (0.7, 0.3, 0.2), color="#8f877b", wob=0.06)
    ball(0.35, (-0.3, -0.2, 0.85), scale=(1, 1, 0.3), color="#6f9e4a")


def root():
    for i in range(3):
        a = i * 2.1
        limb((0, 0, 0.02), (math.cos(a) * 0.6, math.sin(a) * 0.6, 0.12), 0.08, WOOD_DARK, r2=0.04)


def stump():
    cyl(0.32, 0.4, (0, 0, 0.2), color=WOOD, verts=12, bevel=0.03, wob=0.02)
    cyl(0.28, 0.02, (0, 0, 0.41), color="#d9b07a", verts=12, bevel=0)
    for i in range(3):
        a = i * 2.1
        limb((math.cos(a) * 0.25, math.sin(a) * 0.25, 0.1), (math.cos(a) * 0.5, math.sin(a) * 0.5, 0.0), 0.08, WOOD_DARK, r2=0.04)


def tree_a():
    limb((0, 0, 0), (0.05, 0, 1.6), 0.18, WOOD, r2=0.12)
    limb((0.05, 0, 1.3), (0.5, 0.1, 2.0), 0.08, WOOD)
    for (x, y, z, r) in ((0, 0, 2.4, 1.0), (0.6, 0.2, 2.1, 0.7), (-0.55, -0.1, 2.15, 0.7), (0.1, 0.1, 3.05, 0.65)):
        ico(r, (x, y, z), color=LEAF if z > 2.3 else LEAF_DARK, sub=2, wob=0.06, smooth=True)


def tree_b():
    limb((0, 0, 0), (0, 0, 0.8), 0.13, WOOD_DARK)
    lathe([(0.0, 0.5), (0.55, 0.7), (0.62, 1.5), (0.5, 2.6), (0.3, 3.5), (0.0, 4.1)], verts=10, color="#3f6e3a", wob=0.06)


def tree_c():
    limb((0, 0, 0), (0, 0.05, 1.2), 0.15, WOOD, r2=0.11)
    ico(0.95, (0, 0.05, 1.9), scale=(1.1, 1.1, 0.85), color="#6aa547", sub=2, wob=0.07, smooth=True)
    for i in range(8):
        a = RNG.uniform(0, math.tau)
        z = RNG.uniform(1.5, 2.3)
        ball(0.11, (math.cos(a) * 0.95, math.sin(a) * 0.95, z), color="#c9302b", segs=8, rings=6)


def bush():
    for (x, y, r) in ((0, 0, 0.5), (0.45, 0.1, 0.38), (-0.4, 0.05, 0.4), (0.05, 0.35, 0.35)):
        ico(r, (x, y, r * 0.75), color=LEAF if r > 0.39 else LEAF_DARK, sub=2, wob=0.04, smooth=True)


def flowers():
    for i in range(7):
        x, y = RNG.uniform(-0.4, 0.4), RNG.uniform(-0.4, 0.4)
        h = RNG.uniform(0.25, 0.45)
        limb((x, y, 0), (x, y, h), 0.015, LEAF_DARK)
        ball(0.07, (x, y, h), scale=(1, 1, 0.6), color=("#f2d03d", "#e86a8a", "#f4f0e6", "#a88fc9")[i % 4], segs=8, rings=5)
    ball(0.3, (0, 0, 0.06), scale=(1.4, 1.4, 0.3), color=LEAF)


def hay():
    lathe([(0.0, 0.0), (0.7, 0.0), (0.75, 0.4), (0.6, 0.85), (0.3, 1.1), (0.0, 1.15)], verts=14, color="#e0be5a", wob=0.04)
    ring(0.7, 0.04, (0, 0, 0.4), color=WOOD_DARK, major_seg=16)


def mushroom():
    for (x, y, s) in ((0, 0, 1.0), (0.22, 0.1, 0.6), (-0.18, 0.12, 0.5)):
        limb((x, y, 0), (x, y, 0.25 * s), 0.05 * s, CREAM)
        ball(0.16 * s, (x, y, 0.27 * s), scale=(1, 1, 0.6), color="#d9442b")
        ball(0.03 * s, (x + 0.06 * s, y - 0.06 * s, 0.34 * s), color="#ffffff", segs=6, rings=4)


def fence():
    for y in (-1.2, 0, 1.2):
        limb((0, y, 0), (0, y, 0.9), 0.06, WOOD_DARK)
    box((0.08, 2.6, 0.12), (0, 0, 0.7), (4, 0, 0), color=WOOD, bevel=0.02, wob=0.02)
    box((0.08, 2.6, 0.12), (0, 0, 0.38), (-3, 0, 0), color=WOOD, bevel=0.02, wob=0.02)


SOUP_GARNISH = (("#ffe14a", "#c9302b"), ("#8a1a12", "#5f9e3c"), ("#5f9e3c", "#7cc98e"), ("#8a2a12", "#f4ead2"), ("#f4ead2", "#c9302b"))
SOUP_BAND = ("#f2b33d", "#d9442b", "#7cc98e", "#c77a35", "#a88fc9")


def soup_bowl(index):
    liquid = SOUP_LIQUIDS[index]
    bowl(0.5, 0.36, (0, 0, 0), color="#f4f0e6", inner=None, verts=20, thick=0.05)
    ring(0.47, 0.04, (0, 0, 0.2), color=SOUP_BAND[index], major_seg=22)
    cyl(0.45, 0.04, (0, 0, 0.31), color=liquid, verts=20, bevel=0)
    g1, g2 = SOUP_GARNISH[index]
    for i in range(6):
        ball(0.055, (RNG.uniform(-0.28, 0.28), RNG.uniform(-0.28, 0.28), 0.34), scale=(1, 1, 0.4), color=g1 if i % 2 else g2, segs=6, rings=4)
    if index == 0:
        cyl(0.13, 0.03, (-0.12, -0.1, 0.35), (12, 0, 0), color="#ffe14a", verts=12)
    if index == 2:
        for i in range(3):
            leaf(0.14, 0.09, (0.05 * i - 0.1, -0.05, 0.34), (5, 0, i * 110), "#5f9e3c")
    limb((0.3, 0.1, 0.32), (0.75, 0.35, 0.6), 0.03, "#c9925a")


RECIPES = {
    "chef_a": (lambda: chef("#d9442b", "#f2b33d", "#d9442b"), 512, True),
    "chef_b": (lambda: chef("#3f7fbf", "#7cc98e", "#3f7fbf"), 512, True),
    "pot": (pot, 256, False),
    "u_grunt": (u_grunt, 512, True),
    "u_shield": (u_shield, 512, True),
    "u_runner": (u_runner, 512, True),
    "u_ember": (u_ember, 512, True),
    "u_healer": (u_healer, 512, True),
    "u_golem": (u_golem, 512, True),
    "u_saboteur": (u_saboteur, 512, True),
    "u_cloud": (u_cloud, 512, True),
    "u_brute": (u_brute, 512, True),
    "u_bomber": (u_bomber, 512, True),
    "d_catapult": (d_catapult, 512, True),
    "d_barricade": (d_barricade, 512, True),
    "d_chili": (d_chili, 512, True),
    "d_griddle": (d_griddle, 512, True),
    "d_mint": (d_mint, 512, True),
    "d_yogurt": (d_yogurt, 512, True),
    "d_ferment": (d_ferment, 512, True),
    "d_barrel": (d_barrel, 512, True),
    "d_garlic": (d_garlic, 512, True),
    "d_snare": (d_snare, 512, True),
    "cauldron": (cauldron, 1024, False),
    "flag_high": (flag_high, 256, False),
    "ocak": (ocak, 512, False),
    "sofra": (sofra, 512, False),
    "pad": (pad, 256, False),
    "soup_sign": (soup_sign, 256, False),
    "banner": (banner, 256, False),
    "pip": (pip, 128, False),
    "mud": (mud, 256, False),
    "rock_small": (rock_small, 256, False),
    "rock_big": (rock_big, 256, False),
    "root": (root, 256, False),
    "stump": (stump, 256, False),
    "tree_a": (tree_a, 512, False),
    "tree_b": (tree_b, 256, False),
    "tree_c": (tree_c, 512, False),
    "bush": (bush, 256, False),
    "flowers": (flowers, 256, False),
    "hay": (hay, 256, False),
    "mushroom": (mushroom, 256, False),
    "fence": (fence, 256, False),
}

SOUP_LIQUIDS = ("#e8962e", "#c9502b", "#efe9d2", "#b5562f", "#e6dcc4")


MINION = dict(leg_top=0.3, neck=0.66, body_r=0.2, shoulder=(0.24, 0.55), hand=(0.3, 0.18, 0.5), hip_x=0.1, head_len=0.45)

RIGGED = {
    "chef_a": (lambda: chef("#d9442b", "#f2b33d", "#d9442b"), dict(leg_top=0.45, neck=1.02, body_r=0.26, arm_x=0.17, shoulder=(0.3, 0.92), hand=(0.25, 0.48, 0.92), hip_x=0.12, head_len=0.75), "chef"),
    "chef_b": (lambda: chef("#3f7fbf", "#7cc98e", "#3f7fbf"), dict(leg_top=0.45, neck=1.02, body_r=0.26, arm_x=0.17, shoulder=(0.3, 0.92), hand=(0.25, 0.48, 0.92), hip_x=0.12, head_len=0.75), "chef"),
    "u_grunt": (u_grunt, MINION, "unit"),
    "u_shield": (u_shield, dict(MINION, leg_top=0.32, neck=0.7, body_r=0.22), "unit"),
    "u_runner": (u_runner, dict(leg_top=0.33, neck=0.88, body_r=0.2, shoulder=(0.22, 0.58), hand=(0.33, -0.08, 0.45), hip_x=0.1, head_len=0.3), "unit"),
    "u_ember": (u_ember, MINION, "unit"),
    "u_healer": (u_healer, dict(MINION, legs=False, leg_top=0.1, neck=0.72, body_r=0.19, shoulder=(0.2, 0.62), hand=(0.3, 0.18, 0.6)), "unit"),
    "u_golem": (u_golem, dict(leg_top=0.2, neck=0.7, body_r=0.33, shoulder=(0.35, 0.5), hand=(0.5, 0.08, 0.45), hip_x=0.18, head_len=0.5), "unit"),
    "u_saboteur": (u_saboteur, MINION, "unit"),
    "u_cloud": (u_cloud, dict(MINION, all_body=True), "float"),
    "u_brute": (u_brute, dict(leg_top=0.36, neck=0.9, body_r=0.28, shoulder=(0.32, 0.7), hand=(0.42, 0.2, 0.5), hip_x=0.125, head_len=0.7), "unit"),
    "u_bomber": (u_bomber, MINION, "unit"),
}
