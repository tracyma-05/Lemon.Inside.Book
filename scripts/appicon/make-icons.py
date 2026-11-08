"""Draws the book's icon, a planetary gear set, and writes every size the apps use.

    python -I scripts/appicon/make-icons.py

Writes (all committed):
    src/Lemon.Inside.Book/Assets/book.png                       512 px; also the source of the macOS .icns
    src/Lemon.Inside.Book/Assets/book.ico                       16-256 px, the Windows exe and window icon
    src/Lemon.Inside.Book.Browser/wwwroot/icons/icon-*.png      the web edition's favicon, PWA and Apple touch icons

Drawn as a sibling of Lemon.Explore.Book's solar system: a rounded square of night blue with a few stars, and in the
middle a glowing gold sun gear. Three planet gears (blue, red and cream, the Explore book's planet colours) mesh with
it and roll inside a ring gear with inner teeth; a thin circle through the planets' axles is the carrier, drawn like
an orbit. It is a real mechanism (bicycle hub gears and car gearboxes use it) and the teeth truly mesh: the sun has
18 teeth, the planets 9, the ring 36, all of one module, and (18 + 36) / 3 is whole, so three evenly spaced planets
fit. Below 48 px the stars and the carrier are left out and the gears get fewer, larger teeth.
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

SKY_TOP = (22, 32, 66)
SKY_BOTTOM = (12, 16, 36)
SUN_CORE = (255, 236, 170)
SUN_EDGE = (246, 176, 66)
RING = (170, 196, 255)
PLANETS = [(102, 178, 255), (232, 112, 82), (232, 200, 140)]

RING_PITCH = 0.33  # the ring gear's pitch radius, in units of the icon's width
TURN_DEGREES = -30  # where the first planet sits, so the set is not square to the page


def sky(size: int, rounded: bool) -> Image.Image:
    """The background: a vertical gradient, in a rounded square (or full bleed for the maskable and Apple icons)."""
    gradient = Image.new("RGB", (1, size))
    for y in range(size):
        t = y / (size - 1)
        gradient.putpixel((0, y), tuple(round(a + (b - a) * t) for a, b in zip(SKY_TOP, SKY_BOTTOM)))
    image = gradient.resize((size, size)).convert("RGBA")
    if rounded:
        mask = Image.new("L", (size, size), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=round(size * 0.22), fill=255)
        image.putalpha(mask)
    return image


def stars(image: Image.Image, count: int, keep_out: float) -> None:
    draw = ImageDraw.Draw(image)
    size = image.width
    # A seeded scatter, so the icon is the same on every run; none behind the gears.
    seed = 20261107
    for i in range(count):
        seed = (seed * 1103515245 + 12345) % 2**31
        x = 0.06 + 0.88 * seed / 2**31
        seed = (seed * 1103515245 + 12345) % 2**31
        y = 0.06 + 0.88 * seed / 2**31
        if math.hypot(x - 0.5, y - 0.5) < keep_out:
            continue
        r = size * (0.004 + 0.003 * (i % 3))
        alpha = 110 + 40 * (i % 4)
        draw.ellipse((x * size - r, y * size - r, x * size + r, y * size + r), fill=(255, 255, 255, alpha))


def outline(cx: float, cy: float, pitch: float, module: float, teeth: int, turn: float, inner: bool) -> list:
    """A gear's toothed edge in pixels; ``turn`` is in teeth, a tooth is centred on it. ``inner`` makes the teeth
    point inwards, for the ring gear."""
    tip = pitch - module if inner else pitch + module
    root = pitch + 1.25 * module if inner else pitch - 1.25 * module
    step = 2 * math.pi / teeth
    # Per tooth, as fractions of one pitch: root, start of the flank, tip land, end of the flank.
    profile = [(-0.50, root), (-0.30, root), (-0.15, tip), (0.15, tip), (0.30, root)]
    points = []
    for i in range(teeth):
        centre = (i + turn) * step
        for fraction, radius in profile:
            a = centre + fraction * step
            points.append((cx + radius * math.cos(a), cy + radius * math.sin(a)))
    return points


def disc(draw: ImageDraw.ImageDraw, cx: float, cy: float, r: float, fill) -> None:
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill)


def gear_set(size: int, simple: bool) -> Image.Image:
    sun_teeth, planet_teeth = (12, 6) if simple else (18, 9)
    ring_teeth = sun_teeth + 2 * planet_teeth
    module = 2 * RING_PITCH / ring_teeth * size
    sun_pitch, planet_pitch = module * sun_teeth / 2, module * planet_teeth / 2
    carrier = sun_pitch + planet_pitch
    c = size / 2
    step_sun, step_planet, step_ring = (2 * math.pi / n for n in (sun_teeth, planet_teeth, ring_teeth))
    first = math.radians(TURN_DEGREES)
    picture = Image.new("RGBA", (size, size))

    # The ring gear: a band with teeth on its inside, pale like the Explore book's orbits. Each planet has a tooth
    # pointing at the sun, so towards the ring it has a tooth (even count) or a gap (odd), and the ring the opposite.
    ring_turn = first / step_ring + (0.5 if planet_teeth % 2 == 0 else 0.0)
    band = Image.new("L", (size, size), 0)
    bd = ImageDraw.Draw(band)
    disc(bd, c, c, carrier + planet_pitch + module * (2.6 if simple else 2.2), 255)
    bd.polygon(outline(c, c, carrier + planet_pitch, module, ring_teeth, ring_turn, inner=True), fill=0)
    ring = Image.new("RGBA", (size, size), RING + (255,))
    ring.putalpha(band.point(lambda v: v * 110 // 255))
    picture.alpha_composite(ring)

    if not simple:
        # The carrier, a thin circle through the planets' axles, drawn like an orbit.
        ImageDraw.Draw(picture).ellipse((c - carrier, c - carrier, c + carrier, c + carrier),
                                        outline=RING + (120,), width=max(1, round(size * 0.006)))

    # The sun gear's glow, then the gear: a radial gradient from a pale core to an orange rim, as Explore's Sun.
    glow = Image.new("RGBA", (size, size))
    disc(ImageDraw.Draw(glow), c, c, sun_pitch * 2.0, SUN_EDGE + (120,))
    picture.alpha_composite(glow.filter(ImageFilter.GaussianBlur(sun_pitch * 0.8)))
    sun_mask = Image.new("L", (size, size), 0)
    # A gap faces the first planet, which then has a tooth pointing straight back into it; the planets are a whole
    # number of sun teeth apart, so the same holds for all three.
    ImageDraw.Draw(sun_mask).polygon(outline(c, c, sun_pitch, module, sun_teeth, first / step_sun + 0.5, False), fill=255)
    sun = Image.new("RGBA", (size, size))
    sd = ImageDraw.Draw(sun)
    reach = sun_pitch + module
    for i in range(48, 0, -1):
        t = i / 48
        disc(sd, c, c, reach * t, tuple(round(a + (b - a) * t) for a, b in zip(SUN_CORE, SUN_EDGE)) + (255,))
    sun.putalpha(sun_mask)
    picture.alpha_composite(sun)

    for k, colour in enumerate(PLANETS):
        angle = first + k * 2 * math.pi / 3
        px, py = c + carrier * math.cos(angle), c + carrier * math.sin(angle)
        mask = Image.new("L", (size, size), 0)
        md = ImageDraw.Draw(mask)
        md.polygon(outline(px, py, planet_pitch, module, planet_teeth, (angle + math.pi) / step_planet, False), fill=255)
        if not simple:
            disc(md, px, py, planet_pitch * 0.28, 0)
        body = Image.new("RGBA", (size, size), colour + (255,))
        body.putalpha(mask)
        picture.alpha_composite(body)
        # The side facing the sun is lit: a soft lighter patch, as on Explore's planets.
        light = Image.new("RGBA", (size, size))
        hx, hy = px - math.cos(angle) * planet_pitch * 0.35, py - math.sin(angle) * planet_pitch * 0.35
        disc(ImageDraw.Draw(light), hx, hy, planet_pitch * 0.7, tuple(min(255, v + 70) for v in colour) + (170,))
        light = light.filter(ImageFilter.GaussianBlur(planet_pitch * 0.3))
        light.putalpha(ImageChops.multiply(light.getchannel("A"), mask))
        picture.alpha_composite(light)
    return picture


def draw_icon(size: int, *, rounded: bool = True, simple: bool = False, safe: float = 1.0) -> Image.Image:
    """The icon at ``size`` px; ``safe`` shrinks the gears to fit a maskable icon's safe zone."""
    image = sky(BIG, rounded)
    if not simple:
        stars(image, 34, keep_out=0.40 * safe)
    picture = gear_set(BIG, simple)
    if safe != 1.0:
        scaled = picture.resize((round(BIG * safe),) * 2, Image.LANCZOS)
        picture = Image.new("RGBA", (BIG, BIG))
        offset = (BIG - scaled.width) // 2
        picture.alpha_composite(scaled, (offset, offset))
    image.alpha_composite(picture)
    return image.resize((size, size), Image.LANCZOS)


def main() -> None:
    full = draw_icon(512)
    full.save(ASSETS / "book.png")
    full.save(WEB / "icon-512.png")
    draw_icon(192).save(WEB / "icon-192.png")
    # Apple touch icons are square: iOS rounds the corners itself.
    draw_icon(180, rounded=False).save(WEB / "icon-180.png")
    # Maskable: full bleed, the gears inside the central 80 % that every mask keeps.
    draw_icon(512, rounded=False, safe=0.8).save(WEB / "icon-512-maskable.png")

    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [draw_icon(s, simple=s < 48) for s in sizes]
    frames[-1].save(ASSETS / "book.ico", sizes=[(s, s) for s in sizes], append_images=frames[:-1])
    for path in (ASSETS / "book.png", ASSETS / "book.ico", *sorted(WEB.glob("icon-*.png"))):
        print(path.relative_to(ROOT))


if __name__ == "__main__":
    main()
