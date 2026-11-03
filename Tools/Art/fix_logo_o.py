from pathlib import Path
from PIL import Image, ImageFilter

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
# Planet and orbital pixels remain wholly inside the counter.
for y, xs in {7:[8,9],8:[7,8,9,10],9:[7,8,9,10],10:[8,9],6:[12],7:[11],10:[6],11:[5]}.items():
    for x in xs:
        assert not white.getpixel((x,y))
        glyph.putpixel((x,y),(255,255,255,255))
large=glyph.resize((180,190),Image.Resampling.NEAREST)
im=Image.open(source).convert('RGBA')
im.paste((0,0,0,0),(488,507,668,707))
im.paste(large,(488,512))
im.save(out/'planetary-pioneer-wordmark-pixel-fixed.png')
glyph.save(out/'planetary-pioneer-o-grid.png')
large.resize((720,760),Image.Resampling.NEAREST).save(out/'planetary-pioneer-o-closeup.png')
print('PASS: outer letter and counter mirror exactly on both axes; logical grid 18 x 19; nearest-neighbor 10x; binary RGBA glyph.')
