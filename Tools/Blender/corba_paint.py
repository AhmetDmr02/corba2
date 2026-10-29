"""Procedural hand-painted 2D textures (UI sprites, ground, FX) for Corba."""
import os
import numpy as np
from corba_art import save_rgba, UI_DIR, TEX_DIR, hexc


def noise(h, w, cell_y, cell_x=None, seed=0, tile=False):
    cell_x = cell_x or cell_y
    rng = np.random.default_rng(seed)
    gh, gw = max(2, int(round(h / cell_y))), max(2, int(round(w / cell_x)))
    if tile:
        g = rng.random((gh, gw))
        g = np.concatenate([g, g[:1]], 0)
        g = np.concatenate([g, g[:, :1]], 1)
    else:
        g = rng.random((gh + 1, gw + 1))
    ys = np.linspace(0, gh, h, endpoint=not tile)
    xs = np.linspace(0, gw, w, endpoint=not tile)
    y0 = np.clip(np.floor(ys).astype(int), 0, g.shape[0] - 2)
    x0 = np.clip(np.floor(xs).astype(int), 0, g.shape[1] - 2)
    fy = (ys - y0)[:, None]
    fx = (xs - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]
    b = g[y0][:, x0 + 1]
    c = g[y0 + 1][:, x0]
    d = g[y0 + 1][:, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(h, w, cell, seed=0, octaves=4, tile=False, stretch=1.0):
    total = np.zeros((h, w))
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        c = max(2, cell / (2 ** o))
        total += noise(h, w, c, c * stretch, seed + o * 17, tile) * amp
        norm += amp
        amp *= 0.5
    return total / norm


def col(h):
    return np.array(hexc(h))


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def rrect_sdf(h, w, radius, inset=0.0):
    y, x = np.mgrid[0:h, 0:w].astype(np.float64)
    cx, cy = (w - 1) / 2, (h - 1) / 2
    hx, hy = w / 2 - inset - radius, h / 2 - inset - radius
    qx = np.abs(x - cx) - hx
    qy = np.abs(y - cy) - hy
    out = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
    inside = np.minimum(np.maximum(qx, qy), 0)
    return out + inside - radius, y / (h - 1)


def painted_rect(w, h, fill, border, border_w, radius, seed, ink="#3b2414", wobble=3.0, streak=1.0, inset=6, highlight=0.18):
    d, yy = rrect_sdf(h, w, radius, inset)
    d = d + (fbm(h, w, 40, seed, 3) - 0.5) * 2 * wobble
    img = np.zeros((h, w, 4))
    base = np.ones((h, w, 3)) * col(fill)
    var = fbm(h, w, 60, seed + 1, 4)
    strokes = fbm(h, w, 14, seed + 2, 3, stretch=6.0 * streak)
    base *= (0.9 + var[:, :, None] * 0.2)
    base *= (0.94 + strokes[:, :, None] * 0.12)
    base *= (1.0 + (yy[:, :, None] - 0.5) * highlight)
    inner_shade = smooth(-border_w - 26, -border_w, d)
    base *= (1 - inner_shade[:, :, None] * 0.18)
    bcol = np.ones((h, w, 3)) * col(border)
    bstroke = fbm(h, w, 10, seed + 3, 3, stretch=3.0)
    bcol *= (0.8 + bstroke[:, :, None] * 0.35)
    bcol *= (1.0 + smooth(-border_w, -border_w * 0.4, d)[:, :, None] * 0.25 * (yy[:, :, None] > 0.5))
    is_border = smooth(-border_w - 1.2, -border_w + 1.2, d)
    rgb = base * (1 - is_border[:, :, None]) + bcol * is_border[:, :, None]
    edge = smooth(-4.5, -2.0, d)
    rgb = rgb * (1 - edge[:, :, None]) + col(ink) * edge[:, :, None]
    gloss = smooth(-border_w * 0.75, -border_w * 0.55, d) * (1 - smooth(-border_w * 0.45, -border_w * 0.25, d))
    rgb += gloss[:, :, None] * 0.12 * (yy[:, :, None] > 0.55)
    img[:, :, :3] = np.clip(rgb, 0, 1)
    img[:, :, 3] = smooth(0.8, -0.8, d)
    return img


def circle(size, fill, border, border_w, seed, ink="#3b2414", wobble=2.0, highlight=0.3):
    y, x = np.mgrid[0:size, 0:size].astype(np.float64)
    c = (size - 1) / 2
    r = np.sqrt((x - c) ** 2 + (y - c) ** 2)
    d = r - (size / 2 - 5) + (fbm(size, size, 30, seed, 3) - 0.5) * 2 * wobble
    yy = y / (size - 1)
    img = np.zeros((size, size, 4))
    base = np.ones((size, size, 3)) * col(fill)
    base *= (0.9 + fbm(size, size, 30, seed + 1, 4)[:, :, None] * 0.2)
    base *= (1 + (yy[:, :, None] - 0.5) * highlight)
    shine = np.exp(-(((x - c * 0.7) / (size * 0.18)) ** 2 + ((y - c * 1.35) / (size * 0.1)) ** 2))
    base += shine[:, :, None] * 0.25
    bw = smooth(-border_w - 1, -border_w + 1, d)
    rgb = base * (1 - bw[:, :, None]) + col(border) * (0.85 + fbm(size, size, 8, seed + 2, 2)[:, :, None] * 0.3) * bw[:, :, None]
    e = smooth(-4, -1.5, d)
    rgb = rgb * (1 - e[:, :, None]) + col(ink) * e[:, :, None]
    img[:, :, :3] = np.clip(rgb, 0, 1)
    img[:, :, 3] = smooth(0.8, -0.8, d)
    return img


def soft_blob(size, seed, edge=0.35, irregular=0.25, hard=False):
    y, x = np.mgrid[0:size, 0:size].astype(np.float64)
    c = (size - 1) / 2
    ang = np.arctan2(y - c, x - c)
    r = np.sqrt((x - c) ** 2 + (y - c) ** 2) / (size / 2)
    rng = np.random.default_rng(seed)
    wav = sum(rng.uniform(0.3, 1) * np.sin(ang * k + rng.uniform(0, 6.28)) for k in (3, 5, 7)) / 3 * irregular
    d = r - (0.82 + wav)
    a = smooth(0.0, -edge, d) if not hard else smooth(0.03, -0.03, d)
    tex = 0.85 + fbm(size, size, size / 6, seed, 3) * 0.3
    hi = np.exp(-(((x - c * 0.75) / (size * 0.16)) ** 2 + ((y - c * 1.3) / (size * 0.1)) ** 2)) * 0.6
    img = np.zeros((size, size, 4))
    img[:, :, :3] = np.clip(tex[:, :, None] + hi[:, :, None], 0, 1)
    img[:, :, 3] = a
    return img


def build_ui():
    os.makedirs(UI_DIR, exist_ok=True)
    out = lambda n, a: save_rgba(a, os.path.join(UI_DIR, n + ".png"))
    out("ui_panel", painted_rect(256, 256, "#f1dfb6", "#7a4a26", 26, 30, 11, highlight=0.12))
    out("ui_panel_dark", painted_rect(256, 256, "#5a3a24", "#2e1c10", 20, 26, 12, ink="#1c0f06", streak=1.6, highlight=0.25))
    out("ui_banner", painted_rect(512, 200, "#9c3524", "#5a2a14", 22, 40, 13, highlight=0.3))
    out("ui_ribbon", painted_rect(512, 128, "#e6dccb", "#6b4022", 14, 50, 14, highlight=0.3))
    out("ui_button", painted_rect(256, 96, "#a8673a", "#5a3218", 12, 24, 15, streak=2.5, highlight=0.45))
    out("ui_card", painted_rect(128, 176, "#efe6d2", "#4a2c16", 10, 18, 16, highlight=0.25))
    out("ui_chip", painted_rect(128, 48, "#efe6d2", "#4a2c16", 6, 20, 17, wobble=1.5, highlight=0.3))
    out("ui_field", painted_rect(128, 64, "#fbf3e0", "#7a4a26", 8, 16, 18, wobble=1.5, highlight=-0.1))
    out("ui_bar_frame", painted_rect(256, 40, "#3a2414", "#7a4a26", 6, 18, 19, wobble=1.2, highlight=0.0))
    out("ui_bar_fill", painted_rect(256, 28, "#f4ead8", "#d8ccb4", 4, 12, 20, ink="#c8b898", wobble=1.0, highlight=0.6))
    out("ui_badge", circle(128, "#f4ead8", "#c9b48e", 8, 21))
    out("ui_bowl_back", circle(256, "#efe6d2", "#5a8fc9", 18, 22, highlight=0.2))
    rim = circle(256, "#000000", "#7a4a26", 12, 23)
    y, x = np.mgrid[0:256, 0:256]
    rr = np.sqrt((x - 127.5) ** 2 + (y - 127.5) ** 2)
    rim[:, :, 3] *= smooth(108, 113, rr)
    out("ui_bowl_rim", rim)
    sc = soft_blob(256, 24, edge=0.04, irregular=0.06, hard=True)
    sc[:, :, :3] = np.clip(sc[:, :, :3] * 0.9 + 0.1, 0, 1)
    out("ui_soup_circle", sc)
    vig = np.zeros((512, 512, 4))
    yy, xx = np.mgrid[0:512, 0:512] / 511.0
    v = np.sqrt((xx - 0.5) ** 2 * 1.2 + (yy - 0.5) ** 2) * 1.6
    vig[:, :, :3] = col("#2a1608") * (0.8 + fbm(512, 512, 60, 25, 3)[:, :, None] * 0.4)
    vig[:, :, 3] = np.clip(smooth(0.25, 1.1, v) * 0.85 + 0.25, 0, 1)
    out("ui_vignette", vig)


def build_textures():
    os.makedirs(TEX_DIR, exist_ok=True)
    out = lambda n, a: save_rgba(a, os.path.join(TEX_DIR, n + ".png"))
    s = 512
    g = np.zeros((s, s, 4))
    big = fbm(s, s, 128, 31, 4, tile=True)
    strokes = fbm(s, s, 16, 32, 3, tile=True, stretch=0.25)
    dabs = noise(s, s, 12, 12, 33, tile=True)
    v = 0.56 + big * 0.14 + strokes * 0.1 + smooth(0.75, 0.9, dabs) * 0.06 - smooth(0.25, 0.1, dabs) * 0.05
    g[:, :, 0] = v * 0.98
    g[:, :, 1] = v * 1.02
    g[:, :, 2] = v * 0.86
    g[:, :, 3] = 1
    out("ground_grass", g)

    h, w = 512, 256
    r = np.zeros((h, w, 4))
    xx = np.mgrid[0:h, 0:w][1] / (w - 1)
    wob = (noise(h, w, 40, w, 41, tile=True) - 0.5) * 0.12
    edge = np.abs(xx - 0.5) * 2 + wob
    base = col("#c9a46e") * (0.88 + fbm(h, w, 64, 42, 4, tile=True)[:, :, None] * 0.24)
    ruts = np.exp(-((np.abs(xx - 0.5) - 0.24) / 0.07) ** 2) * 0.12
    base *= (1 - ruts[:, :, None])
    base *= (0.95 + fbm(h, w, 12, 43, 2, tile=True, stretch=0.2)[:, :, None] * 0.1)
    peb = noise(h, w, 9, 9, 44, tile=True)
    pl = smooth(0.84, 0.9, peb)[:, :, None]
    base = base * (1 - pl) + col("#e3d2ad") * pl
    pd = (smooth(0.14, 0.08, peb) * 0.6)[:, :, None]
    base = base * (1 - pd) + col("#8a6a44") * pd
    grass = col("#7ea64e") * (0.85 + fbm(h, w, 20, 45, 3, tile=True)[:, :, None] * 0.3)
    gm = smooth(0.72, 0.95, edge)
    rgb = base * (1 - gm[:, :, None]) + grass * gm[:, :, None]
    dark = smooth(0.62, 0.78, edge) * (1 - smooth(0.82, 0.95, edge))
    rgb *= (1 - dark[:, :, None] * 0.2)
    r[:, :, :3] = np.clip(rgb, 0, 1)
    r[:, :, 3] = 1
    out("ground_road", r)

    ss = np.zeros((256, 256, 4))
    yy, xx = np.mgrid[0:256, 0:256]
    rr = np.sqrt((xx - 127.5) ** 2 + (yy - 127.5) ** 2) / 128
    swirl = fbm(256, 256, 40, 51, 3)
    ss[:, :, :3] = (0.85 + swirl[:, :, None] * 0.2 + np.exp(-((rr - 0.55) / 0.08) ** 2)[:, :, None] * 0.12
                    + np.exp(-(((xx - 90) / 22.0) ** 2 + ((yy - 165) / 12.0) ** 2))[:, :, None] * 0.4)
    ss[:, :, :3] *= (1 - smooth(0.75, 1.0, rr)[:, :, None] * 0.35)
    ss[:, :, 3] = 1
    out("soup_surface", np.clip(ss, 0, 1))

    out("fx_blob", soft_blob(64, 61, edge=0.2, irregular=0.15))
    out("fx_splat", soft_blob(128, 62, edge=0.06, irregular=0.5, hard=True))
    puff = soft_blob(128, 63, edge=0.6, irregular=0.3)
    out("fx_puff", puff)
    ring = np.zeros((256, 256, 4))
    rr = np.sqrt((xx - 127.5) ** 2 + (yy - 127.5) ** 2) / 128
    ring[:, :, :3] = 1
    ring[:, :, 3] = np.exp(-((rr - 0.85) / 0.06) ** 2) * (0.75 + fbm(256, 256, 20, 64, 2) * 0.5) + np.exp(-((rr - 0.6) / 0.03) ** 2) * 0.3
    out("fx_ring", np.clip(ring, 0, 1))
    st = np.zeros((64, 64, 4))
    y, x = np.mgrid[0:64, 0:64] - 31.5
    star = np.exp(-np.abs(x) / 2.5) * np.exp(-np.abs(y) / 12) + np.exp(-np.abs(y) / 2.5) * np.exp(-np.abs(x) / 12)
    st[:, :, :3] = 1
    st[:, :, 3] = np.clip(star * 1.4, 0, 1)
    out("fx_star", st)
    bar = painted_rect(64, 16, "#ffffff", "#ffffff", 1, 6, 65, ink="#ffffff", wobble=0.5, highlight=0.3, inset=1)
    out("ui_bar_world", bar)


def tile_tex(size, seed, base, var=0.22, streak=0.1, cell=64):
    t = np.ones((size, size, 4))
    v = 0.88 + fbm(size, size, cell, seed, 4, tile=True) * var
    v *= (1 - streak / 2) + fbm(size, size, 14, seed + 1, 3, tile=True, stretch=0.25) * streak
    t[:, :, :3] = col(base) * v[:, :, None]
    return t


def build_terrain_textures():
    out = lambda n, a: save_rgba(np.clip(a, 0, 1), os.path.join(TEX_DIR, n + ".png"))
    s = 512
    dirt = tile_tex(s, 71, "#c9a46e")
    peb = noise(s, s, 9, 9, 72, tile=True)
    pl = smooth(0.84, 0.9, peb)[:, :, None]
    dirt[:, :, :3] = dirt[:, :, :3] * (1 - pl) + col("#e8d8b4") * pl
    pd = (smooth(0.14, 0.08, peb) * 0.5)[:, :, None]
    dirt[:, :, :3] = dirt[:, :, :3] * (1 - pd) + col("#8a6a44") * pd
    out("ground_dirt", dirt)

    plaza = tile_tex(s, 73, "#d8c8a8", var=0.15)
    y, x = np.mgrid[0:s, 0:s]
    cells = 4
    cs = s // cells
    gx = (x % cs) / cs
    gy = (((y + (x // cs % 2) * cs // 2)) % cs) / cs
    jitter = fbm(s, s, 24, 74, 2, tile=True) * 0.08
    edge = np.minimum(np.minimum(gx, 1 - gx), np.minimum(gy, 1 - gy)) + jitter - 0.04
    mortar = smooth(0.05, 0.0, edge)
    stone_var = noise(s, s, cs, cs, 75, tile=True)
    plaza[:, :, :3] *= (0.86 + stone_var[:, :, None] * 0.22)
    plaza[:, :, :3] *= (1 + smooth(0.05, 0.2, edge)[:, :, None] * 0.08)
    plaza[:, :, :3] = plaza[:, :, :3] * (1 - mortar[:, :, None]) + col("#8a7a62") * mortar[:, :, None]
    out("ground_plaza", plaza)

    cliff = tile_tex(s, 76, "#a8988a", var=0.2, streak=0.0)
    strata = np.sin((y / s) * np.pi * 2 * 6 + fbm(s, s, 64, 77, 3, tile=True) * 6) * 0.5 + 0.5
    cliff[:, :, :3] *= (0.85 + strata[:, :, None] * 0.18)
    cracks = smooth(0.08, 0.0, np.abs(fbm(s, s, 40, 78, 3, tile=True) - 0.5))
    cliff[:, :, :3] *= (1 - cracks[:, :, None] * 0.35)
    cliff[:, :, :3] = cliff[:, :, :3] * 0.85 + col("#9a8fb0") * 0.15
    out("ground_cliff", cliff)

    water = tile_tex(s, 79, "#4f9cc0", var=0.18)
    ripple = smooth(0.06, 0.0, np.abs(fbm(s, s, 48, 80, 3, tile=True) - 0.5))
    water[:, :, :3] = water[:, :, :3] * (1 - ripple[:, :, None] * 0.6) + col("#d8f0f4") * ripple[:, :, None] * 0.6
    out("ground_water", water)

    wood = np.ones((s, s, 4))
    plank = (x // (s // 4))
    pv = noise(s, s, s, s // 4, 81, tile=True)
    grain = fbm(s, s, 6, 82, 3, tile=True, stretch=12) * 0.25
    wood[:, :, :3] = col("#a8703e") * (0.78 + pv[:, :, None] * 0.25 + grain[:, :, None])
    gap = smooth(0.03, 0.0, np.minimum((x % (s // 4)) / (s / 4), 1 - (x % (s // 4)) / (s / 4)))
    wood[:, :, :3] = wood[:, :, :3] * (1 - gap[:, :, None]) + col("#4a2c16") * gap[:, :, None]
    out("ground_wood", wood)

    a = np.zeros((256, 256, 4))
    yy, xx = np.mgrid[0:256, 0:256].astype(float)
    cx = 127.5
    head = (yy > 120) & (np.abs(xx - cx) < (230 - yy) * 1.0)
    shaft = (yy <= 125) & (yy > 30) & (np.abs(xx - cx) < 34)
    shape = (head | shaft).astype(float)
    wob = fbm(256, 256, 20, 83, 2) * 0.3
    a[:, :, :3] = col("#fff2d0") * (0.9 + wob[:, :, None])
    a[:, :, 3] = shape * 0.55
    out("road_arrow", a)

    for name, glyph_col, high in (("icon_pad", "#fff4dc", False), ("icon_pad_high", "#ffd86a", True)):
        im = circle(256, "#5a3a24", glyph_col, 14, 84)
        plus_v = (np.abs(xx - cx) < 18) & (np.abs(yy - cx) < 70)
        plus_h = (np.abs(yy - cx) < 18) & (np.abs(xx - cx) < 70)
        if high:
            tri = (yy > 60) & (yy < 190) & (np.abs(xx - cx) < (190 - yy) * 0.62)
            plus_v = tri
            plus_h = np.zeros_like(tri)
        m = (plus_v | plus_h).astype(float)
        im[:, :, :3] = im[:, :, :3] * (1 - m[:, :, None]) + col(glyph_col) * m[:, :, None]
        out(name, im)

    b = np.zeros((64, 64, 4))
    yy, xx = np.mgrid[0:64, 0:64].astype(float)
    r = np.sqrt((xx - 31.5) ** 2 + (yy - 31.5) ** 2) / 32
    b[:, :, :3] = 1
    hi = np.exp(-(((xx - 22) / 6) ** 2 + ((yy - 42) / 4) ** 2))
    b[:, :, 3] = np.clip(smooth(0.95, 0.8, r) * (0.35 + smooth(0.6, 0.9, r) * 0.6) + hi, 0, 1)
    out("fx_bubble", b)


def build_glyphs():
    s = 256
    yy, xx = np.mgrid[0:s, 0:s].astype(float)
    cx = (s - 1) / 2

    def stroke_fill(mask, fill, ink="#3b2414", width=10, seed=90):
        m = mask.astype(float)
        grow = m.copy()
        for dy in range(-width, width + 1, 2):
            for dx in range(-width, width + 1, 2):
                if dx * dx + dy * dy <= width * width:
                    grow = np.maximum(grow, np.roll(np.roll(m, dy, 0), dx, 1))
        img = np.zeros((s, s, 4))
        shade = 0.85 + fbm(s, s, 40, seed, 3) * 0.25 + (yy / s - 0.5)[:, :] * 0.25
        img[:, :, :3] = col(fill) * shade[:, :, None] * m[:, :, None] + col(ink) * (1 - m[:, :, None])
        img[:, :, 3] = grow
        return np.clip(img, 0, 1)

    head = (yy > 110) & (yy < 236) & (np.abs(xx - cx) < (236 - yy) * 0.85)
    shaft = (yy >= 30) & (yy <= 115) & (np.abs(xx - cx) < 34)
    save_rgba(stroke_fill(head | shaft, "#ffd86a"), os.path.join(UI_DIR, "icon_arrow.png"))

    ang = np.arctan2(yy - cx, xx - cx) - np.pi / 2
    r = np.sqrt((xx - cx) ** 2 + (yy - cx) ** 2)
    star_r = 60 + 50 * (np.cos(ang * 5) * 0.5 + 0.5) ** 1.8
    save_rgba(stroke_fill(r < star_r, "#ffcf3a", width=9, seed=91), os.path.join(UI_DIR, "icon_star.png"))

    down_head = (yy < 140) & (yy > 20) & (np.abs(xx - cx) < (yy - 20) * 0.85)
    down_shaft = (yy >= 135) & (yy <= 226) & (np.abs(xx - cx) < 34)
    save_rgba(stroke_fill(down_head | down_shaft, "#ffe48a", seed=92), os.path.join(TEX_DIR, "icon_marker.png"))


def build_soup_glyphs():
    """White shape glyphs (tinted in UI): shield, flame, leaf, bubbles, stink lines."""
    s = 256
    yy, xx = np.mgrid[0:s, 0:s].astype(float)
    cx = (s - 1) / 2
    nx, ny = (xx - cx) / cx, (yy - cx) / cx

    shield = (np.abs(nx) < 0.62) & (ny < 0.7) & (ny > -0.85 + np.abs(nx) * 1.1) | ((np.abs(nx) < 0.62) & (ny >= 0.55) & (ny < 0.78))
    flame_r = 0.55 * (1 - (ny + 0.7) / 1.7) ** 0.7
    flame = (ny > -0.75) & (ny < 0.95) & (np.abs(nx + 0.12 * np.sin(ny * 4)) < np.clip(flame_r * (1.6 - np.abs(ny - 0.0)), 0, 0.6))
    flame &= ~((ny < -0.1) & (ny > -0.55) & (np.abs(nx) < 0.16))
    leaf = (np.abs(nx) < 0.62 * np.sqrt(np.clip(1 - ny * ny, 0, 1)) * (0.55 + 0.45 * (1 - ny))) & (np.abs(ny) < 0.85)
    leaf &= ~((np.abs(nx) < 0.04) & (ny > -0.8) & (ny < 0.6))
    def circ(x, y, r):
        return (nx - x) ** 2 + (ny - y) ** 2 < r * r
    bubbles = circ(-0.25, -0.3, 0.42) | circ(0.38, 0.1, 0.3) | circ(-0.05, 0.55, 0.22)
    bubbles &= ~(circ(-0.25, -0.3, 0.26) | circ(0.38, 0.1, 0.17) | circ(-0.05, 0.55, 0.11))
    stink = np.zeros_like(nx, dtype=bool)
    for k, off in enumerate((-0.45, 0.0, 0.45)):
        stink |= (np.abs(nx - off - 0.14 * np.sin(ny * 5 + k)) < 0.09) & (np.abs(ny) < 0.8)

    for name, mask in (("glyph_shield", shield), ("glyph_flame", flame), ("glyph_leaf", leaf), ("glyph_bubbles", bubbles), ("glyph_stink", stink)):
        m = mask.astype(float)
        grow = m.copy()
        w = 8
        for dy in range(-w, w + 1, 2):
            for dx in range(-w, w + 1, 2):
                if dx * dx + dy * dy <= w * w:
                    grow = np.maximum(grow, np.roll(np.roll(m, dy, 0), dx, 1))
        img = np.zeros((s, s, 4))
        shade = 0.92 + fbm(s, s, 40, 77, 3) * 0.12 + (yy / s - 0.5) * 0.15
        ink = col("#3b2414")
        img[:, :, :3] = shade[:, :, None] * m[:, :, None] + ink * (1 - m[:, :, None])
        img[:, :, 3] = grow
        save_rgba(np.clip(img, 0, 1), os.path.join(UI_DIR, name + ".png"))
