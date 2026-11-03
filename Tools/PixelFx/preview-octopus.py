"""Build a same-scale animated overview of the exported octopus VFX. Requires Pillow."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools/PixelFx/records/octopus"
OUT.mkdir(parents=True, exist_ok=True)
IDS = [
    ("hydro-pump", "01  하이드로 펌프"),
    ("water-cannon", "02  물대포"),
    ("radial-stone-eruption", "03  원형 지면 파쇄"),
    ("water-bomb", "04  대형 물폭탄"),
    ("water-cannon-impact", "05  물대포 착탄"),
    ("water-bomb-impact", "06  물폭탄 착탄"),
]
FONT = ImageFont.truetype("/System/Library/Fonts/AppleSDGothicNeo.ttc", 14)
effects = []
for name, label in IDS:
    folder = ROOT / "Assets/PixelFx/Sheets/conquest" / name
    data = json.loads((folder / f"{name}.json").read_text())
    sheet = Image.open(folder / f"{name}.png").convert("RGBA")
    frames = []
    for entry in data["frames"]:
        f = entry["frame"]
        frames.append(sheet.crop((f["x"], f["y"], f["x"] + f["w"], f["y"] + f["h"])))
    effects.append((label, data["meta"]["pixelfx"], frames))

def board(t, still=False):
    canvas = Image.new("RGB", (736, 710), "#11141e")
    d = ImageDraw.Draw(canvas)
    for index, (label, meta, frames) in enumerate(effects):
        x, y = 8 + index % 2 * 368, 8 + index // 2 * 234
        d.rounded_rectangle((x, y, x + 360, y + 226), radius=7, fill="#1c2230")
        d.text((x + 12, y + 8), label, fill="#d8edf6", font=FONT)
        # Same native pixel scale for all six, so the bomb/cannon size comparison is honest.
        at = (.65 if index == 2 else .21) if still else t
        f = int(at * meta["fps"])
        f = f % len(frames) if meta["loop"] else min(f, len(frames) - 1)
        frame = frames[f]
        # Only excess transparent margins are cropped; pixels retain a common scale.
        alpha = Image.new("L", frame.size)
        for candidate in frames:
            from PIL import ImageChops
            alpha = ImageChops.lighter(alpha, candidate.getchannel("A"))
        bounds = alpha.getbbox()
        if bounds:
            frame = frame.crop(bounds)
            canvas.paste(frame, (x + (360 - frame.width) // 2, y + 31 + (190 - frame.height) // 2), frame)
    return canvas.resize((1472, 1420), Image.Resampling.NEAREST)

board(0, True).save(OUT / "octopus-effects.png")
images = [board(i / 24) for i in range(66)]
images[0].save(OUT / "octopus-effects.gif", save_all=True, append_images=images[1:],
               duration=[40, 40, 50] * 22, loop=0, optimize=True)
print(OUT / "octopus-effects.gif")
