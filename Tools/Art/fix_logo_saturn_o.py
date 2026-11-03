from pathlib import Path
import math
from PIL import Image, ImageFilter, ImageDraw

root = Path(__file__).resolve().parents[2]
source = root / 'output/imagegen/planetary-pioneer-wordmark-symmetric-o.png'
out = root / 'output/imagegen'
# Each logical cell is exactly 10 by 10 output pixels.
scale = 10
white = Image.new('1', (18, 19))
for y, inset in enumerate([4,3,2,1]+[0]*9+[1,2,3,4]):
    for x in range(inset, 16-inset):
        white.putpixel((x+1,y+1),1)
# Symmetric counter, independent of the asymmetric illustration inside it.
for y, inset in enumerate([2,1]+[0]*7+[1,2]):
    for x in range(inset, 10-inset):
        white.putpixel((x+4,y+4),0)
assert white.tobytes() == white.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes()
assert white.tobytes() == white.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()
outer = Image.new('L',(18,19))
for y in range(19):
    for x in range(18):
        if white.getpixel((x,y)): outer.putpixel((x,y),255)
outer=outer.filter(ImageFilter.MaxFilter(3))
glyph=Image.new('RGBA',(18,19))
glyph.paste((0,0,0,255),(0,0,18,19),outer)
glyph.paste((255,255,255,255),(0,0,18,19),white.convert('L').point(lambda p:255 if p else 0))
# Use a 5px grid for the small planet; O retains the original 10px grid.
glyph=glyph.resize((36,38),Image.Resampling.NEAREST)
angle=math.radians(-22)
for y in range(38):
    for x in range(36):
        if white.getpixel((x//2,y//2)) or not (8 <= x < 28 and 8 <= y < 30):
            continue
        dx,dy=x-17.5,y-18.5
        u=dx*math.cos(angle)+dy*math.sin(angle)
        v=-dx*math.sin(angle)+dy*math.cos(angle)
        ring=math.sqrt((u/7.5)**2+(v/2.2)**2)
        disk=dx*dx+dy*dy <= 5.0**2
        lit=disk or ring <= 1.15
        if lit:
            assert not white.getpixel((x//2,y//2))
            glyph.putpixel((x,y),(255,255,255,255))
# One continuous front-ring separation, rather than scattered cutout pixels.
# The rear ring and circular body remain filled; the visible foreground edge
# is a single staircase line on the same 5px grid.
front = Image.new('1', glyph.size)
ImageDraw.Draw(front).line([(11,22),(24,17)], fill=1, width=1)
for y in range(38):
    for x in range(36):
        if front.getpixel((x,y)) and glyph.getpixel((x,y)) == (255,255,255,255):
            assert not white.getpixel((x//2,y//2))
            glyph.putpixel((x,y),(0,0,0,255))
large=glyph.resize((180,190),Image.Resampling.NEAREST)
im=Image.open(source).convert('RGBA')
im.paste((0,0,0,0),(488,507,668,707))
im.paste(large,(488,512))
im.save(out/'planetary-pioneer-wordmark-saturn.png')
glyph.save(out/'planetary-pioneer-saturn-grid.png')
large.resize((720,760),Image.Resampling.NEAREST).save(out/'planetary-pioneer-saturn-closeup.png')
print('PASS: outer letter and counter mirror exactly on both axes; logical grid 18 x 19; nearest-neighbor 10x; binary RGBA glyph.')
