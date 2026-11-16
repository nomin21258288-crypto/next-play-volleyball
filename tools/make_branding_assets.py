#!/usr/bin/env python3
"""Generates game-ready textures from the ORIGINAL reference photos (no AI, no face editing).

For each of the six characters this only:
  1. crops a head-and-hair rectangle (3:4) from the matching original photo,
  2. resizes it (LANCZOS) to 384x512,
  3. applies a soft elliptical alpha mask so the photo background does not show on the 3D head,
  4. samples average hair / skin colours for the parts of the model that have no photo.
Nothing is repainted, beautified, swapped or mixed. Crop boxes are in config/characters.json so a human can re-check them.

Also draws the two app-icon OPTIONS (generated volleyball mark, and three-face icon) - neither is a final logo.

Run:  python3 tools/make_branding_assets.py        (needs Pillow + numpy)
"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REF = os.path.join(ROOT, "design-references", "Handoff_latest_starter", "Character_References")
OUT_FACE = os.path.join(ROOT, "client-unity", "Assets", "Resources", "Characters")
OUT_ICON = os.path.join(ROOT, "client-unity", "Assets", "Branding", "Icons")
CFG = os.path.join(ROOT, "config", "characters.json")
TEX_W, TEX_H = 384, 512


def face_texture(src, box):
    im = Image.open(os.path.join(REF, src)).convert("RGB")
    crop = im.crop(tuple(box)).resize((TEX_W, TEX_H), Image.LANCZOS)
    yy, xx = np.mgrid[0:TEX_H, 0:TEX_W].astype(np.float32)
    cx, cy, rx, ry = TEX_W * 0.5, TEX_H * 0.52, TEX_W * 0.49, TEX_H * 0.49
    d = np.sqrt(((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2)
    alpha = np.clip((1.0 - d) / 0.18, 0.0, 1.0)  # feather the outer 18%
    rgba = np.dstack([np.asarray(crop, dtype=np.uint8), (alpha * 255).astype(np.uint8)])
    return Image.fromarray(rgba, "RGBA"), np.asarray(crop, dtype=np.float32), alpha


def avg(arr, alpha, y0, y1, x0, x1):
    sub = arr[int(y0 * TEX_H):int(y1 * TEX_H), int(x0 * TEX_W):int(x1 * TEX_W)]
    w = alpha[int(y0 * TEX_H):int(y1 * TEX_H), int(x0 * TEX_W):int(x1 * TEX_W)] > 0.9
    px = sub[w] if w.any() else sub.reshape(-1, 3)
    c = px.mean(axis=0)
    return "#%02X%02X%02X" % tuple(int(round(v)) for v in c)


def draw_ball(size, bg=None):
    """Simple generated volleyball mark (original artwork, not a copy of any brand)."""
    S = size * 4
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if bg:
        d.rounded_rectangle([0, 0, S, S], radius=int(S * 0.22), fill=bg)
    r = int(S * 0.36); cx = cy = S // 2
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(250, 250, 250, 255), outline=(20, 40, 110, 255), width=int(S * 0.012))
    cols = [(24, 80, 200, 255), (255, 200, 30, 255), (24, 80, 200, 255)]
    import math
    for i in range(3):
        a0 = math.radians(i * 120 + 20)
        pts = []
        for t in range(-40, 41):
            tt = t / 40.0
            ang = a0 + tt * 0.9
            rad = r * (0.98 - 0.55 * (1 - tt * tt))
            pts.append((cx + rad * math.cos(ang), cy + rad * math.sin(ang)))
        d.line(pts, fill=cols[i], width=int(S * 0.045), joint="curve")
    # court / net motif under the ball
    d.rectangle([int(S * 0.12), int(S * 0.80), int(S * 0.88), int(S * 0.815)], fill=(255, 255, 255, 230))
    for k in range(9):
        x = int(S * (0.12 + k * 0.095))
        d.line([x, int(S * 0.72), x, int(S * 0.80)], fill=(255, 255, 255, 200), width=int(S * 0.006))
    d.line([int(S * 0.12), int(S * 0.76), int(S * 0.88), int(S * 0.76)], fill=(255, 255, 255, 200), width=int(S * 0.006))
    return im.resize((size, size), Image.LANCZOS)


def icon_mark(size=1024):
    bg = Image.new("RGBA", (size, size), (9, 24, 80, 255))
    # simple radial-ish vignette (two blends, no art copied from anywhere)
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([size * 0.1, size * 0.05, size * 0.9, size * 0.85], fill=(30, 90, 220, 160))
    glow = glow.filter(ImageFilter.GaussianBlur(size * 0.1))
    bg = Image.alpha_composite(bg, glow)
    return Image.alpha_composite(bg, draw_ball(size))


def icon_three_faces(textures, size=1024):
    """Three DIFFERENT people (01 centre, 04A left, 04B right), each only from its own photo (whole head, unedited)."""
    bg = Image.new("RGBA", (size, size), (9, 24, 80, 255))
    order = [("04A", 0.27, 0.62, 0.50), ("04B", 0.73, 0.62, 0.50), ("01", 0.5, 0.45, 0.60)]
    for cid, fx, fy, fs in order:
        th = int(size * fs); tw = int(th * 0.75)
        t = textures[cid].resize((tw, th), Image.LANCZOS)
        disc = Image.new("RGBA", (tw + 20, th + 20), (0, 0, 0, 0))
        ImageDraw.Draw(disc).ellipse([0, 0, disc.size[0] - 1, disc.size[1] - 1], fill=(255, 200, 30, 255))
        ImageDraw.Draw(disc).ellipse([10, 10, disc.size[0] - 11, disc.size[1] - 11], fill=(20, 36, 96, 255))
        x, y = int(size * fx - disc.size[0] / 2), int(size * fy - disc.size[1] / 2)
        bg.alpha_composite(disc, (x, y)); bg.alpha_composite(t, (x + 10, y + 10))
    return bg


def main():
    cfg = json.load(open(CFG))
    os.makedirs(OUT_FACE, exist_ok=True); os.makedirs(OUT_ICON, exist_ok=True)
    textures = {}
    for c in cfg["characters"]:
        img, arr, alpha = face_texture(c["source"], c["cropBox"])
        img.save(os.path.join(OUT_FACE, c["faceTexture"] + ".png"), optimize=True)
        textures[c["sourceKey"]] = img
        c["hairColor"] = avg(arr, alpha, 0.04, 0.20, 0.25, 0.75)
        c["skinColor"] = avg(arr, alpha, 0.50, 0.62, 0.30, 0.70)
        print(c["id"], c["source"], "hair", c["hairColor"], "skin", c["skinColor"])
    json.dump(cfg, open(CFG, "w"), indent=2, ensure_ascii=False); open(CFG, "a").write("\n")
    icon_mark().convert("RGB").save(os.path.join(OUT_ICON, "icon_option_mark.png"), optimize=True)
    icon_three_faces(textures).convert("RGB").save(os.path.join(OUT_ICON, "icon_option_three_faces.png"), optimize=True)
    # contact sheet for human review (not shipped in the game)
    sheet = Image.new("RGB", (TEX_W * 3, TEX_H * 2), (60, 60, 60))
    for i, c in enumerate(cfg["characters"]):
        t = Image.open(os.path.join(OUT_FACE, c["faceTexture"] + ".png"))
        sheet.paste(t, ((i % 3) * TEX_W, (i // 3) * TEX_H), t)
    sheet.save(os.path.join(ROOT, "docs", "face_textures_review_sheet.png"))
    print("ok")


if __name__ == "__main__":
    main()
