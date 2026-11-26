from pathlib import Path
import random, math, wave, struct
from PIL import Image, ImageDraw, ImageFilter
random.seed(67)
root=Path('Assets/_Project')
text=Path('prompts/17_diary_loop_ending.md').read_text(encoding='utf-8').split('```')[1].strip()
(root/'Settings/Loop/DiaryContent.txt').write_text(text+'\n',encoding='utf-8')
w,h=512,512
im=Image.new('RGBA',(w,h),(0,0,0,0)); d=ImageDraw.Draw(im)
points=[(x,random.randint(8,16)) for x in range(12,500,5)]+[(random.randint(496,504),y) for y in range(12,500,5)]+[(x,random.randint(496,504)) for x in range(500,12,-5)]+[(random.randint(8,16),y) for y in range(500,12,-5)]
d.polygon(points,fill=(236,220,181,255))
for y in range(45,490,24): d.line((22,y,490,y),fill=(215,205,184,255),width=1)
for i in range(9): d.ellipse((340-i,65-i,432+i,157+i),outline=(218-i,195-i,155-i,255))
d.polygon([(440,12),(500,12),(500,75),(482,56),(468,48),(456,30)],fill=(76,47,26,255))
for i in range(15000):
 x=random.randrange(20,490); y=random.randrange(20,490); d.point((x,y),fill=(random.randrange(226,239),random.randrange(210,223),random.randrange(173,186),255))
im.save(root/'Art/UI/Sprites/diary_paper.png')
# Original procedural recordings, released CC0 with the project.
rate=22050
for name,seconds,gain in [('paper_rustle',.65,.26),('loop_wind',8,.07)]:
 samples=[]; smooth=0
 for i in range(int(rate*seconds)):
  t=i/rate; noise=random.uniform(-1,1); smooth=smooth*.985+noise*.015
  value=noise*(math.sin(math.pi*t/seconds)**2)*gain if name=='paper_rustle' else smooth*(.7+.3*math.sin(2*math.pi*t/seconds))*gain*12
  samples.append(struct.pack('<h',int(max(-1,min(1,value))*32767)))
 with wave.open(str(root/'Audio/Loop'/f'{name}.wav'),'wb') as f: f.setparams((1,2,rate,0,'NONE','not compressed')); f.writeframes(b''.join(samples))
