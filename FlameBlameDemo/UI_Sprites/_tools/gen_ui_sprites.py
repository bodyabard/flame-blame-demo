from PIL import Image, ImageDraw, ImageFilter
import math, json, os
S=4  # supersample
def canvas(w,h): return Image.new('RGBA',(w*S,h*S),(0,0,0,0))
def save(im,name,w,h): im.resize((w,h),Image.LANCZOS).save(name+'.png')
meta={}
def rrect(name,w,h,r,inset=0,ring=None,border=None):
    im=canvas(w,h); d=ImageDraw.Draw(im)
    box=[inset*S,inset*S,(w-inset)*S-1,(h-inset)*S-1]
    if ring is None: d.rounded_rectangle(box,r*S,fill=(255,255,255,255))
    else: d.rounded_rectangle(box,r*S,outline=(255,255,255,255),width=ring*S)
    save(im,name,w,h); meta[name]={'size':[w,h],'border_LBRT':border}
# panel: fill + outline + inner gloss + shadow
rrect('panel_fill',128,128,30,border=[36,36,36,36])
rrect('panel_outline',128,128,30,ring=6,border=[36,36,36,36])
rrect('pill_fill',128,64,32,border=[34,0,34,0])
rrect('pill_outline',128,64,32,ring=5,border=[34,0,34,0])
# gloss: top highlight band inside panel (white, alpha gradient)
im=canvas(128,128); g=Image.new('L',(128*S,128*S),0); gd=ImageDraw.Draw(g)
gd.rounded_rectangle([10*S,8*S,118*S-1,64*S],22*S,fill=255)
grad=Image.new('L',(1,128*S)); [grad.putpixel((0,y),int(max(0,1-y/(64*S))*70)) for y in range(128*S)]
grad=grad.resize((128*S,128*S)); 
from PIL import ImageChops
a=ImageChops.multiply(g,grad); im.putalpha(a); im=Image.merge('RGBA',(Image.new('L',im.size,255),)*3+(a,))
save(im,'panel_gloss',128,128); meta['panel_gloss']={'size':[128,128],'border_LBRT':[36,36,36,36]}
# soft shadow
w=h=160; im=Image.new('L',(w,h),0); d=ImageDraw.Draw(im); d.rounded_rectangle([16,16,w-17,h-17],30,fill=255)
im=im.filter(ImageFilter.GaussianBlur(8)); out=Image.new('RGBA',(w,h),(0,0,0,0)); out.putalpha(im.point(lambda v:int(v*0.55)))
out.save('panel_shadow.png'); meta['panel_shadow']={'size':[w,h],'border_LBRT':[52,52,52,52]}
# circles (gauge)
def circ(name,sz,ring=None):
    im=canvas(sz,sz); d=ImageDraw.Draw(im); b=[2*S,2*S,(sz-2)*S-1,(sz-2)*S-1]
    if ring: d.ellipse(b,outline=(255,255,255,255),width=ring*S)
    else: d.ellipse(b,fill=(255,255,255,255))
    save(im,name,sz,sz); meta[name]={'size':[sz,sz],'border_LBRT':None}
circ('circle_fill',256); circ('circle_outline',256,ring=12)
# arc ring for radial fill (Image.Type Filled Radial360)
im=canvas(256,256); d=ImageDraw.Draw(im); d.ellipse([20*S,20*S,236*S-1,236*S-1],outline=(255,255,255,255),width=26*S)
save(im,'gauge_ring',256,256); meta['gauge_ring']={'size':[256,256],'border_LBRT':None,'note':'Image Type Filled, Radial 360'}
# needle (pivot bottom-center)
im=canvas(32,128); d=ImageDraw.Draw(im); d.polygon([(16*S,4*S),(26*S,112*S),(16*S,124*S),(6*S,112*S)],fill=(255,255,255,255))
save(im,'gauge_needle',32,128); meta['gauge_needle']={'size':[32,128],'pivot':[0.5,0.06]}
# icons 128
def icon(name,draw):
    im=canvas(128,128); d=ImageDraw.Draw(im); draw(d,lambda v:v*S); save(im,name,128,128); meta[name]={'size':[128,128],'border_LBRT':None}
def drop(d,s):
    d.ellipse([s(30),s(52),s(98),s(120)],fill='white'); d.polygon([(s(64),s(6)),(s(34),s(72)),(s(94),s(72))],fill='white')
icon('icon_water',drop)
def flame(d,s):
    d.ellipse([s(26),s(54),s(102),s(122)],fill='white'); d.polygon([(s(64),s(4)),(s(28),s(84)),(s(100),s(84))],fill='white'); d.polygon([(s(92),s(30)),(s(78),s(80)),(s(104),s(80))],fill='white')
icon('icon_fire',flame)
def jet(d,s):
    d.rounded_rectangle([s(44),s(6),s(84),s(86)],s(20),fill='white')
    d.polygon([(s(44),s(56)),(s(24),s(90)),(s(44),s(84))],fill='white'); d.polygon([(s(84),s(56)),(s(104),s(90)),(s(84),s(84))],fill='white')
    for x in (50,64,78): d.polygon([(s(x-6),s(94)),(s(x+6),s(94)),(s(x),s(124))],fill='white')
icon('icon_jet',jet)
def clock(d,s):
    d.ellipse([s(8),s(8),s(120),s(120)],outline='white',width=s(14)); d.line([s(64),s(64),s(64),s(30)],fill='white',width=s(12)); d.line([s(64),s(64),s(90),s(76)],fill='white',width=s(12)); d.ellipse([s(56),s(56),s(72),s(72)],fill='white')
icon('icon_clock',clock)
def coin(d,s):
    d.ellipse([s(8),s(8),s(120),s(120)],fill='white'); pass
icon('icon_coin',coin)
# coin with $ cutout via font
from PIL import ImageFont
im=Image.open('icon_coin.png'); m=Image.new('L',(512,512),0); d2=ImageDraw.Draw(m)
f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',330)
d2.text((256,262),'$',font=f,fill=255,anchor='mm')
m=m.resize((128,128),Image.LANCZOS); a=ImageChops.subtract(im.getchannel('A'),m); im.putalpha(a); im.save('icon_coin.png')
def bolt(d,s):
    d.polygon([(s(72),s(4)),(s(22),s(72)),(s(58),s(72)),(s(46),s(124)),(s(106),s(50)),(s(68),s(50)),(s(86),s(4))],fill='white')
icon('icon_bolt',bolt)
def ladder(d,s):
    d.rounded_rectangle([s(26),s(6),s(42),s(122)],s(7),fill='white'); d.rounded_rectangle([s(86),s(6),s(102),s(122)],s(7),fill='white')
    for y in (24,52,80,108): d.rounded_rectangle([s(36),s(y-6),s(92),s(y+6)],s(5),fill='white')
icon('icon_ladder',ladder)
# key cap for hints (rounded square)
rrect('keycap_fill',96,96,22,border=[28,28,28,28]); rrect('keycap_outline',96,96,22,ring=6,border=[28,28,28,28])
# burst / star for popups
im=canvas(256,256); d=ImageDraw.Draw(im); pts=[]
for i in range(24):
    ang=i*math.pi/12; r=120 if i%2==0 else 84
    pts.append((S*(128+r*math.cos(ang)),S*(128+r*math.sin(ang))))
d.polygon(pts,fill='white'); save(im,'burst',256,256); meta['burst']={'size':[256,256],'border_LBRT':None}
json.dump(meta,open('sprites_meta.json','w'),indent=1)
