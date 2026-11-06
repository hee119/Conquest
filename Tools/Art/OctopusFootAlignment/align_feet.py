from PIL import Image, ImageDraw
from pathlib import Path
import json
root=Path(__file__).resolve().parents[3]
move=Image.open(root/'Assets/SpriteSeet/OctopusKingMove.png').convert('RGBA')
source=Image.open(root/'Assets/SpriteSeet/OctopusKingWaterShot.png').convert('RGBA')
out=Image.new('RGBA',source.size)
report=[]
frames=[]
for i in range(25):
 c,r=i%5,i//5
 x0,x1=round(c*source.width/5),round((c+1)*source.width/5)
 y0,y1=round(r*source.height/5),round((r+1)*source.height/5)
 frame=source.crop((x0,y0,x1,y1)); box=frame.getchannel('A').point(lambda a:255 if a>=128 else 0).getbbox()
 ref=move.crop((c*256,r*256,(c+1)*256,(r+1)*256)); rb=ref.getbbox()
 # Empty cells in Move are not animation frames; use its usual standing baseline.
 reference_bottom=rb[3] if rb else 208
 target=round(reference_bottom*(y1-y0)/256)
 dy=target-box[3]
 assert box[1]+dy>=0 and box[3]+dy<=frame.height
 shifted=Image.new('RGBA',frame.size); shifted.paste(frame,(0,dy))
 assert sum(1 for p in frame.getdata() if p[3]>=128)==sum(1 for p in shifted.getdata() if p[3]>=128)
 assert frame.crop(box).tobytes()==shifted.crop((box[0],box[1]+dy,box[2],box[3]+dy)).tobytes()
 out.paste(shifted,(x0,y0))
 report.append(dict(frame=i,offset_y=dy,foot_before=box[3],foot_after=target,move_foot=reference_bottom))
 frames.append(shifted)
folder=Path(__file__).parent
out.save(folder/'OctopusKingWaterShotFeetAligned.png')
(folder/'alignment.json').write_text(json.dumps(report,indent=2))
# Show representative first idle pose, firing pose and recovery pose against Move.
preview=Image.new('RGB',(768,570),(35,38,46)); draw=ImageDraw.Draw(preview)
for j,i in enumerate([0,11,24]):
 ref=move.crop((4*256,0,5*256,256))
 preview.paste(ref,(j*256,25),ref)
 corrected=frames[i].resize((256,256),Image.Resampling.NEAREST)
 preview.paste(corrected,(j*256,300),corrected)
 draw.line((j*256,233,(j+1)*256-1,233),fill=(60,210,140))
 draw.line((j*256,508,(j+1)*256-1,508),fill=(60,210,140))
 draw.text((j*256+10,5),'MOVE REFERENCE',fill='white')
 draw.text((j*256+10,280),f'WATER SHOT {i:02d} - ALIGNED',fill='white')
preview.save(folder/'comparison.png')
print('25 frames: all visible pixels preserved; X unchanged; only vertical translation; no visible clipping. Output:',folder/'OctopusKingWaterShotFeetAligned.png')
print('Offsets:',[r['offset_y'] for r in report])
