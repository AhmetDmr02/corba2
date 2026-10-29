"""Standalone (no Blender) painter for soup glyphs and soup-icon recolouring. Usage: python soup_glyphs_local.py"""
import os
import numpy as np
from PIL import Image

UI_DIR = r"D:\Corba\Assets\Corba\Resources\Corba\UI"

OLD_LIQUIDS = ["#e8962e", "#c9502b", "#efe9d2", "#b5562f", "#e6dcc4"]
NEW_LIQUIDS = ["#f0a92a", "#d0331c", "#b8edb4", "#8b47c4", "#cde9f2"]
NEW_BANDS = ["#f5b92e", "#e8412c", "#47c46b", "#9a57d6", "#38a3db"]
OLD_BAND = "#5a8fc9"


def col(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])


def noise(h, w, cell, seed):
    rng = np.random.default_rng(seed)
    g = rng.random((h // cell + 2, w // cell + 2))
    img = np.array(Image.fromarray((g * 255).astype(np.uint8)).resize((w + cell, h + cell), Image.BICUBIC)) / 255.0
    return img[:h, :w]


def save(arr, path):
    """arr rows are top-to-bottom."""
    Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), "RGBA").save(path)


def glyphs():
    s = 256
    yy, xx = np.mgrid[0:s, 0:s].astype(float)
    c = (s - 1) / 2
    nx = (xx - c) / c
    ny = (c - yy) / c  # +1 is up

    shield = ((np.abs(nx) < 0.62) & (ny < 0.72) & (ny > -0.9 + np.abs(nx) * 1.15))
    base = (nx ** 2 + (ny + 0.35) ** 2) < 0.47 ** 2
    t = np.clip((ny + 0.35) / 1.3, 0, 1)
    tongue = (ny > -0.35) & (ny < 0.95) & (np.abs(nx - 0.18 * t * t) < 0.47 * (1 - t) ** 0.85)
    flame = base | tongue
    flame &= ~(((nx + 0.02) ** 2 + (ny + 0.4) ** 2) < 0.2 ** 2)
    leaf = (np.abs(nx) < 0.46 * np.sqrt(np.clip(1 - ny * ny, 0, 1)) * (0.5 + 0.5 * (1 + ny) / 2 + 0.25)) & (np.abs(ny) < 0.88)
    leaf &= ~((np.abs(nx) < 0.04) & (ny > -0.75) & (ny < 0.62))

    def circ(x, y, r):
        return (nx - x) ** 2 + (ny - y) ** 2 < r * r

    bubbles = circ(-0.25, -0.3, 0.42) | circ(0.38, 0.12, 0.3) | circ(-0.05, 0.58, 0.22)
    bubbles &= ~(circ(-0.25, -0.3, 0.25) | circ(0.38, 0.12, 0.16) | circ(-0.05, 0.58, 0.1))
    stink = np.zeros_like(nx, dtype=bool)
    for k, off in enumerate((-0.45, 0.0, 0.45)):
        stink |= (np.abs(nx - off - 0.14 * np.sin(ny * 5 + k)) < 0.09) & (np.abs(ny) < 0.8)

    ink = col("#3b2414")
    for name, mask in (("glyph_shield", shield), ("glyph_flame", flame), ("glyph_leaf", leaf), ("glyph_bubbles", bubbles), ("glyph_stink", stink)):
        m = mask.astype(float)
        grow = m.copy()
        r = 9
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    grow = np.maximum(grow, np.roll(np.roll(m, dy, 0), dx, 1))
        shade = 0.9 + noise(s, s, 24, len(name)) * 0.14 + (1 - yy / s - 0.5) * 0.15
        img = np.zeros((s, s, 4))
        img[:, :, :3] = shade[:, :, None] * m[:, :, None] + ink * (1 - m[:, :, None])
        img[:, :, 3] = grow
        save(img, os.path.join(UI_DIR, name + ".png"))


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn + 1e-9
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) / 6
    s = np.where(mx > 0, (mx - mn) / (mx + 1e-9), 0)
    return np.stack([h, s, mx], -1)


def hsv_to_rgb(hsv):
    h, s, v = hsv[..., 0] * 6, hsv[..., 1], hsv[..., 2]
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    out = np.zeros(hsv.shape)
    for k, (a, b, c) in enumerate(((v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q))):
        m = i == k
        out[..., 0][m], out[..., 1][m], out[..., 2][m] = a[m], b[m], c[m]
    return out


def recolour_icons():
    """Hue-shift the old liquid and bowl-band colours to the new readable ones, keeping painted shading."""
    for i in range(5):
        path = os.path.join(UI_DIR, f"icon_soup_{i}.png")
        img = np.array(Image.open(path).convert("RGBA")).astype(float) / 255.0
        hsv = rgb_to_hsv(img[:, :, :3])
        for old, new in ((OLD_LIQUIDS[i], NEW_LIQUIDS[i]), (OLD_BAND, NEW_BANDS[i])):
            o, n = rgb_to_hsv(col(old)[None, None])[0, 0], rgb_to_hsv(col(new)[None, None])[0, 0]
            dh = np.abs(((hsv[..., 0] - o[0]) + 0.5) % 1 - 0.5)
            low_sat_source = o[1] < 0.2
            if low_sat_source:
                mask = (hsv[..., 1] < 0.25) & (hsv[..., 2] > 0.6) & (np.abs(hsv[..., 2] - o[2]) < 0.25)
            else:
                mask = (dh < 0.07) & (hsv[..., 1] > 0.3) & (hsv[..., 2] > 0.25)
            mask &= img[:, :, 3] > 0.5
            hsv[..., 0] = np.where(mask, n[0], hsv[..., 0])
            hsv[..., 1] = np.where(mask, np.clip(n[1] * (hsv[..., 1] / max(o[1], 0.05)) ** 0.3, 0, 1), hsv[..., 1])
            hsv[..., 2] = np.where(mask, np.clip(hsv[..., 2] * (n[2] / max(o[2], 0.05)), 0, 1), hsv[..., 2])
        img[:, :, :3] = hsv_to_rgb(hsv)
        save(img, path)


if __name__ == "__main__":
    glyphs()
    recolour_icons()
    print("ok")
