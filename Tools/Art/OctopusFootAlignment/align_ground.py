from PIL import Image
from pathlib import Path
import json
p=Path(__file__).parent
src=Image.open(p.parents[2]/'Assets/SpriteSeet/OctopusKingGroundAttack.png').convert('RGBA')
out=Image.new('RGBA',(src.width,1600)); report=[]
for i in range(25):
 c,r=i%5,i//5;x0,x1=round(c*src.width/5),round((c+1)*src.width/5);y0,y1=round(r*src.height/5),round((r+1)*src.height/5)
 frame=src.crop((x0,y0,x1,y1));b=frame.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
 dy=260-b[3] if b else 0
 canvas=Image.new('RGBA',(frame.width,320));canvas.paste(frame,(0,dy))
 assert sum(v[3]>=128 for v in frame.getdata())==sum(v[3]>=128 for v in canvas.getdata())
 out.paste(canvas,(x0,r*320));report.append(dict(frame=i,offset_y=dy,foot_after=260))
out.save(p/'OctopusKingGroundAttackFeetAligned.png');(p/'ground_alignment.json').write_text(json.dumps(report,indent=2))
print('23 attack frames aligned, expanded vertical padding to preserve crown and tentacles. No scaling.')
