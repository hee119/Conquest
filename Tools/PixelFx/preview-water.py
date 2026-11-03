"""Preview the current exported cannon and bomb at the same pixel scale (Pillow)."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
effects = []
for name in ("water-cannon", "water-bomb"):
    folder = ROOT / "Assets/PixelFx/Sheets/conquest" / name
    data = json.loads((folder / f"{name}.json").read_text())
    sheet = Image.open(folder / f"{name}.png").convert("RGBA")
    frames = []
    for entry in data["frames"]:
        f = entry["frame"]
        frames.append(sheet.crop((f["x"], f["y"], f["x"] + f["w"], f["y"] + f["h"])))
    effects.append(frames)

font = ImageFont.truetype("/System/Library/Fonts/AppleSDGothicNeo.ttc", 13)
images = []
for i in range(len(effects[0])):
    im = Image.new("RGB", (304, 180), "#1c2230")
    draw = ImageDraw.Draw(im)
    draw.text((40, 12), "물대포", font=font, fill="#d8edf6")
    draw.text((200, 12), "물폭탄", font=font, fill="#d8edf6")
    im.paste(effects[0][i], (20, 54), effects[0][i])
    im.paste(effects[1][i], (152, 26), effects[1][i])
    images.append(im.resize((912, 540), Image.Resampling.NEAREST))
out = ROOT / "Tools/PixelFx/records/octopus"
out.mkdir(parents=True, exist_ok=True)
images[4].save(out / "water-inner-currents.png")
images[0].save(out / "water-inner-currents.gif", save_all=True,
               append_images=images[1:], duration=42, loop=0)
print(out / "water-inner-currents.gif")
