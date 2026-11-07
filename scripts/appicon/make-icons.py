"""Draws the book's icon, a lamp shining on two gears, and writes every size the apps use.

    python -I scripts/appicon/make-icons.py

Writes (all committed):
    src/Lemon.Inside.Book/Assets/book.png                       512 px; also the source of the macOS .icns
    src/Lemon.Inside.Book/Assets/book.ico                       16-256 px, the Windows exe and window icon
    src/Lemon.Inside.Book.Browser/wwwroot/icons/icon-*.png      the web edition's favicon, PWA and Apple touch icons

A rounded square of dusk blue with a small lamp in the top-left corner. Its light falls on a gold gear meshing with a
smaller silver one, and both cast a soft shadow away from the lamp: the book's two volumes, light and machines, in one
picture. Below 48 px only the lamp's glow and the gold gear are drawn, so it still reads in a title bar.
Needs Pillow (``pip install pillow``).
"""
from __future__ import annotations

import math
import pathlib

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / "src" / "Lemon.Inside.Book" / "Assets"
WEB = ROOT / "src" / "Lemon.Inside.Book.Browser" / "wwwroot" / "icons"

# Drawn at this size and scaled down, which smooths every edge.
BIG = 2048

BACK_TOP = (34, 46, 78)
BACK_BOTTOM = (14, 18, 36)
LAMP = (255, 240, 196)
GLOW = (255, 206, 120)
GOLD_LIT = (255, 214, 120)
GOLD_DARK = (196, 128, 40)
SILVER_LIT = (236, 240, 246)
SILVER_DARK = (128, 140, 160)

LAMP_AT = (0.20, 0.20)  # in units of the icon's width
MODULE = 0.030          # tooth size: both gears share it, so they mesh
# Gears as (centre, teeth, turn in teeth); the small one sits up and to the right of the big one, at the distance
# where their pitch circles touch, turned half a tooth so its teeth fall in the big one's gaps.
BIG_GEAR = ((0.40, 0.60), 16, 0.0)
SMALL_TEETH = 10
SMALL_ANGLE = math.radians(-45)


def pitch_radius(teeth: int) -> float:
    return MODULE * teeth / 2


def small_gear() -> tuple[tuple[float, float], int, float]:
    (bx, by), big_teeth, _ = BIG_GEAR
    distance = pitch_radius(big_teeth) + pitch_radius(SMALL_TEETH)
    centre = (bx + distance * math.cos(SMALL_ANGLE), by + distance * math.sin(SMALL_ANGLE))
    # The big gear is turned so a gap faces the small one (draw_icon); the small one needs a tooth pointing straight
    # back into that gap.
    turn = ((SMALL_ANGLE + math.pi) / (2 * math.pi / SMALL_TEETH)) % 1.0
    return centre, SMALL_TEETH, turn


def background(size: int, rounded: bool) -> Image.Image:
    """A vertical gradient, in a rounded square (or full bleed for the maskable and Apple icons)."""
    gradient = Image.new("RGB", (1, size))
    for y in range(size):
        t = y / (size - 1)
        gradient.putpixel((0, y), tuple(round(a + (b - a) * t) for a, b in zip(BACK_TOP, BACK_BOTTOM)))
    image = gradient.resize((size, size)).convert("RGBA")
    if rounded:
        mask = Image.new("L", (size, size), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=round(size * 0.22), fill=255)
        image.putalpha(mask)
    return image


def gear_outline(centre: tuple[float, float], teeth: int, turn: float, size: int) -> list[tuple[float, float]]:
    """A gear's outline in pixels: trapezoid teeth between the root and tip circles; ``turn`` is in teeth."""
    pitch = pitch_radius(teeth)
    tip, root = (pitch + MODULE) * size, (pitch - 1.25 * MODULE) * size
    cx, cy = centre[0] * size, centre[1] * size
    step = 2 * math.pi / teeth
    points = []
    # Per tooth, as fractions of one pitch: root, start of the flank, tip land, end of the flank.
    profile = [(-0.50, root), (-0.30, root), (-0.15, tip), (0.15, tip), (0.30, root)]
    for i in range(teeth):
        centre_angle = (i + turn) * step
        for fraction, radius in profile:
            a = centre_angle + fraction * step
            points.append((cx + radius * math.cos(a), cy + radius * math.sin(a)))
    return points


def gear_mask(centre, teeth: int, turn: float, size: int, holes: bool) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.polygon(gear_outline(centre, teeth, turn, size), fill=255)
    cx, cy = centre[0] * size, centre[1] * size
    pitch = pitch_radius(teeth) * size
    # The axle hole, and on the big gear four round windows that lighten it, as real wheels have.
    hub = pitch * 0.22
    draw.ellipse((cx - hub, cy - hub, cx + hub, cy + hub), fill=0)
    if holes:
        for k in range(4):
            a = math.radians(45 + 90 * k)
            hx, hy, hr = cx + pitch * 0.55 * math.cos(a), cy + pitch * 0.55 * math.sin(a), pitch * 0.19
            draw.ellipse((hx - hr, hy - hr, hx + hr, hy + hr), fill=0)
    return mask


def lit_fill(size: int, lit, dark) -> Image.Image:
    """Metal lit from the lamp: brightest near the lamp, darker away from it."""
    small = 64
    fill = Image.new("RGBA", (small, small))
    lx, ly = LAMP_AT
    for y in range(small):
        for x in range(small):
            d = math.hypot(x / small - lx, y / small - ly)
            t = min(1.0, max(0.0, (d - 0.2) / 0.75))
            fill.putpixel((x, y), tuple(round(a + (b - a) * t) for a, b in zip(lit, dark)) + (255,))
    return fill.resize((size, size), Image.BICUBIC)


def glow(image: Image.Image, simple: bool, safe: float) -> None:
    """A warm glow round the lamp, wide enough to wash over the gears' near side. It is drawn on the background, not
    shrunk with the picture for the maskable icon (which would leave its edge as a visible box)."""
    size = image.width
    lx, ly = (0.5 + (c - 0.5) * safe for c in LAMP_AT)
    # A radial falloff worked out on a small grid and scaled up, so it fades smoothly with no edge.
    small = 128
    reach = 0.62 if simple else 0.55
    layer = Image.new("RGBA", (small, small))
    for y in range(small):
        for x in range(small):
            d = math.hypot(x / small - lx, y / small - ly) / reach
            layer.putpixel((x, y), GLOW + (round(120 * max(0.0, 1 - d) ** 2),))
    image.alpha_composite(layer.resize((size, size), Image.BICUBIC))


def lamp(image: Image.Image) -> None:
    size = image.width
    cx, cy, r = LAMP_AT[0] * size, LAMP_AT[1] * size, size * 0.045
    ImageDraw.Draw(image).ellipse((cx - r, cy - r, cx + r, cy + r), fill=LAMP + (255,))


def shadow(image: Image.Image, mask: Image.Image) -> None:
    """The gears' shadow, cast away from the lamp and softened, as the lamp has a size."""
    size = image.width
    dx = dy = round(size * 0.028)
    moved = ImageChops.offset(mask, dx, dy).filter(ImageFilter.GaussianBlur(size * 0.014))
    dark = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    dark.putalpha(moved.point(lambda v: v * 150 // 255))
    image.alpha_composite(dark)


def draw_icon(size: int, *, rounded: bool = True, simple: bool = False, safe: float = 1.0) -> Image.Image:
    """The icon at ``size`` px; ``safe`` shrinks the picture to fit a maskable icon's safe zone."""
    image = background(BIG, rounded)
    corners = image.getchannel("A")
    glow(image, simple, safe)
    picture = Image.new("RGBA", (BIG, BIG))
    if not simple:
        lamp(picture)

    big_centre, big_teeth, _ = BIG_GEAR
    # Turn the big gear so a gap, not a tooth, faces the small gear.
    big_turn = (SMALL_ANGLE / (2 * math.pi / big_teeth)) % 1.0 + 0.5
    big = gear_mask(big_centre, big_teeth, big_turn, BIG, holes=not simple)
    if simple:
        # Larger teeth, so the gear reads at 16-32 px.
        big = gear_mask((0.5, 0.56), big_teeth, big_turn, BIG, holes=False)
        big = big.resize((round(BIG * 1.25),) * 2, Image.LANCZOS).crop(
            (round(BIG * 0.125), round(BIG * 0.125), round(BIG * 1.125), round(BIG * 1.125)))
    masks = [(big, GOLD_LIT, GOLD_DARK)]
    if not simple:
        centre, teeth, turn = small_gear()
        masks.append((gear_mask(centre, teeth, turn, BIG, holes=False), SILVER_LIT, SILVER_DARK))

    together = Image.new("L", (BIG, BIG), 0)
    for mask, _, _ in masks:
        together = ImageChops.lighter(together, mask)
    shadow(picture, together)
    for mask, lit, dark in masks:
        metal = lit_fill(BIG, lit, dark)
        metal.putalpha(mask)
        picture.alpha_composite(metal)

    if safe != 1.0:
        scaled = picture.resize((round(BIG * safe),) * 2, Image.LANCZOS)
        picture = Image.new("RGBA", (BIG, BIG))
        offset = (BIG - scaled.width) // 2
        picture.alpha_composite(scaled, (offset, offset))
    image.alpha_composite(picture)
    # Keep the glow and the shadow inside the rounded corners.
    image.putalpha(ImageChops.multiply(image.getchannel("A"), corners))
    return image.resize((size, size), Image.LANCZOS)


def main() -> None:
    full = draw_icon(512)
    full.save(ASSETS / "book.png")
    full.save(WEB / "icon-512.png")
    draw_icon(192).save(WEB / "icon-192.png")
    # Apple touch icons are square: iOS rounds the corners itself.
    draw_icon(180, rounded=False).save(WEB / "icon-180.png")
    # Maskable: full bleed, the picture inside the central 80 % that every mask keeps.
    draw_icon(512, rounded=False, safe=0.8).save(WEB / "icon-512-maskable.png")

    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [draw_icon(s, simple=s < 48) for s in sizes]
    frames[-1].save(ASSETS / "book.ico", sizes=[(s, s) for s in sizes], append_images=frames[:-1])
    for path in (ASSETS / "book.png", ASSETS / "book.ico", *sorted(WEB.glob("icon-*.png"))):
        print(path.relative_to(ROOT))


if __name__ == "__main__":
    main()
