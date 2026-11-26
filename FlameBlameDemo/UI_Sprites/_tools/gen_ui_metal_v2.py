# Flame&Blame HUD v2 — metal tread plates, roller shutter. Grayscale (tint via Image.color) unless noted.
import numpy as np, json, os, math
from PIL import Image, ImageDraw, ImageFilter
S=4
OUT=os.environ.get('OUT','.')
meta={}
L=np.array([-0.55,-0.83]); L/=np.linalg.norm(L)
def save(arr_rgba,name,w,h,**m):
    im=Image.fromarray(np.clip(arr_rgba,0,255).astype(np.uint8),'RGBA').resize((w,h),Image.LANCZOS)
    im.save(os.path.join(OUT,name+'.png')); meta[name]=dict(size=[w,h],**m)

def rounded_rim(W,H,r,inset_out,rim_w,outline_w):
    """returns alpha, rim-shade(-1..1 or nan), outline mask, inner mask at supersample"""
    ys,xs=np.mgrid[0:H,0:W].astype(np.float32)+0.5
    def sd(ins,rad):  # signed distance to rounded rect inset by ins with radius rad (neg inside)
        x0,y0,x1,y1=ins+rad,ins+rad,W-ins-rad,H-ins-rad
        cx=np.clip(xs,x0,x1); cy=np.clip(ys,y0,y1)
        dx=xs-cx; dy=ys-cy; d=np.sqrt(dx*dx+dy*dy)-rad
        return d,dx,dy
    d_out,_,_=sd(inset_out,r)
    alpha=np.clip(0.5-d_out,0,1)
    d_in,dx,dy=sd(inset_out+outline_w+rim_w, max(r-outline_w-rim_w,1))
    n=np.sqrt(dx*dx+dy*dy)+1e-6; nx,ny=dx/n,dy/n
    shade=nx*L[0]+ny*L[1]
    rim=np.clip(0.5+d_in,0,1)            # 1 outside inner rect
    outline=np.clip(0.5+(d_out+outline_w),0,1)  # 1 in outer outline band
    return alpha,shade,rim,outline

def tread_tile(T):
    """tileable tread pattern T x T (supersampled) -> value array 0..1 delta"""
    img=Image.new('L',(T,T),128); hi=Image.new('L',(T,T),0); lo=Image.new('L',(T,T),0)
    dh,dl,db=ImageDraw.Draw(hi),ImageDraw.Draw(lo),ImageDraw.Draw(img)
    cell=T//2; n=3; Ll=cell*0.62; Wd=cell*0.13; off=cell*0.06
    def lens(d,cx,cy,horiz,shift,fill):
        pts=[]
        for i in range(24):
            t=i/23*math.pi
            a=math.cos(t)*Ll/2; b=math.sin(t)*Wd/2
            pts.append((a,b))
        pts=pts+[(-p[0],-p[1]) for p in pts]
        pp=[((cx+(p[0] if horiz else p[1])+shift[0]),(cy+(p[1] if horiz else p[0])+shift[1])) for p in pts]
        d.polygon(pp,fill=fill)
    for gx in range(2):
        for gy in range(2):
            horiz=(gx+gy)%2==0
            for k in range(n):
                o=(k-(n-1)/2)*cell*0.27
                cx=gx*cell+cell/2+(0 if horiz else o); cy=gy*cell+cell/2+(o if horiz else 0)
                for wx in (-T,0,T):
                    for wy in (-T,0,T):
                        lens(dl,cx+wx,cy+wy,horiz,(off,off),255)
                        lens(dh,cx+wx,cy+wy,horiz,(-off*0.7,-off*0.7),255)
                        lens(db,cx+wx,cy+wy,horiz,(0,0),148)
    v=np.asarray(img,np.float32)/255; h=np.asarray(hi,np.float32)/255; l=np.asarray(lo,np.float32)/255
    body=(v>0.55).astype(np.float32)
    out=np.zeros_like(v)
    out+= l*(1-body)*-0.16   # shadow below lens
    out+= h*(1-body)*0.13    # highlight above
    out+= body*0.06
    return out

def plate(name,size,border,r,base,pattern=True,rivets=True,outline_v=0.16,rim_w=10):
    W=H=size*S
    alpha,shade,rim,outline=rounded_rim(W,H,r*S,0,rim_w*S,4*S)
    val=np.full((H,W),base,np.float32)
    if pattern:
        T=(size-2*border)*S  # center exactly one tile, edges tile period too
        t=tread_tile(T)
        tt=np.tile(t,(math.ceil(H/T)+1,math.ceil(W/T)+1))
        oy=ox=(border*S)%T
        val+=tt[T-oy:T-oy+H, T-ox:T-ox+W]*0.55
    # rim bevel
    val=val*(1-rim)+(base+0.16*shade+0.02)*rim
    # outline
    val=val*(1-outline)+outline_v*outline
    rgba=np.zeros((H,W,4),np.float32)
    rgba[...,0]=rgba[...,1]=rgba[...,2]=val*255
    rgba[...,3]=alpha*255
    if rivets:
        im=Image.fromarray(np.clip(rgba,0,255).astype(np.uint8),'RGBA'); d=ImageDraw.Draw(im)
        rr=4.2*S; c=(rim_w+4+9)*S
        for (x,y) in [(c,c),(W-c,c),(c,H-c),(W-c,H-c)]:
            d.ellipse([x-rr+1.2*S,y-rr+1.6*S,x+rr+1.2*S,y+rr+1.6*S],fill=(40,40,40,200))
            d.ellipse([x-rr,y-rr,x+rr,y+rr],fill=(int(base*255*0.9),)*3+(255,))
            d.ellipse([x-rr*0.55-0.8*S,y-rr*0.55-0.8*S,x+rr*0.15,y+rr*0.15],fill=(235,235,235,255))
        rgba=np.asarray(im,np.float32)
    save(rgba,name,size,size,border_LBRT=[border]*4,image_type='Tiled' if pattern else 'Sliced')

plate('plate_tread',192,48,24,0.58)
plate('plate_tread_small',128,32,18,0.58)   # for keycaps/pills small plates? center 64
plate('keycap_metal',96,30,18,0.80,pattern=False,rivets=False,rim_w=7)
plate('plate_flat',128,36,22,0.62,pattern=False,rivets=True)

# sheen overlay (sliced): soft top-left highlight
def sheen():
    W=H=128*S; ys,xs=np.mgrid[0:H,0:W]/W
    a=np.clip(0.55-(ys*0.9+xs*0.25),0,1)**1.6*110
    alpha,_,_,_=rounded_rim(W,H,22*S,4*S,1,1)
    rgba=np.zeros((H,W,4)); rgba[...,:3]=255; rgba[...,3]=a*alpha
    save(rgba,'plate_sheen',128,128,border_LBRT=[36]*4,image_type='Sliced')
sheen()

# roller shutter slat tile 64x64: uniform along X, profile along Y (one slat = 32px, two per tile)
def slat_tile():
    W=H=64*S; y=(np.arange(H)%(32*S))/(32*S)
    prof=np.interp(y,[0,0.06,0.12,0.5,0.86,0.92,1.0],[0.30,0.80,0.70,0.60,0.48,0.24,0.30])
    rib=np.where((y>0.40)&(y<0.46),0.08,0)+np.where((y>0.46)&(y<0.52),-0.06,0)
    v=np.tile((prof+rib)[:,None],(1,W))
    rgba=np.zeros((H,W,4)); rgba[...,:3]=v[...,None]*255; rgba[...,3]=255
    save(rgba,'shutter_slat_tile',64,64,border_LBRT=None,image_type='Tiled (Wrap Repeat)',note='tile vertically & horizontally; one slat = 32 px')
slat_tile()

def bar(name,w,h,border_lr,prof_x,prof_v,ends=True):
    W,H=w*S,h*S; y=np.arange(H)/H
    v=np.interp(y,prof_x,prof_v); v=np.tile(v[:,None],(1,W))
    if ends:
        x=np.arange(W)/S
        e=np.minimum(x,w-x); cap=(e<border_lr*0.5)
        v=np.where(cap,v*0.8,v)
    rgba=np.zeros((H,W,4)); rgba[...,:3]=v[...,None]*255; rgba[...,3]=255
    im=Image.fromarray(rgba.astype(np.uint8),'RGBA'); d=ImageDraw.Draw(im)
    d.rectangle([0,0,W-1,H-1],outline=(40,40,40,255),width=3*S)
    save(np.asarray(im,np.float32),name,w,h,border_LBRT=[border_lr,0,border_lr,0],image_type='Sliced')
bar('shutter_bottom_bar',256,64,40,[0,0.08,0.2,0.55,0.8,0.92,1],[0.25,0.75,0.62,0.50,0.38,0.22,0.18])
bar('shutter_housing',256,128,48,[0,0.06,0.25,0.45,0.75,0.94,1],[0.22,0.62,0.74,0.66,0.44,0.26,0.18])

def handle():
    W,H=128*S,48*S; im=Image.new('RGBA',(W,H),(0,0,0,0)); d=ImageDraw.Draw(im)
    d.rounded_rectangle([6*S,14*S,122*S,40*S],12*S,fill=(30,30,30,180))
    d.rounded_rectangle([4*S,8*S,120*S,34*S],12*S,fill=(150,150,150,255),outline=(40,40,40,255),width=3*S)
    d.rounded_rectangle([14*S,12*S,110*S,18*S],3*S,fill=(225,225,225,255))
    save(np.asarray(im,np.float32),'shutter_handle',128,48,border_LBRT=None)
handle()

def hazard():
    W=H=64*S; ys,xs=np.mgrid[0:H,0:W]
    band=((xs+ys)//(16*S))%2==0
    rgba=np.zeros((H,W,4)); rgba[...,0]=np.where(band,255,34); rgba[...,1]=np.where(band,196,26); rgba[...,2]=np.where(band,48,22); rgba[...,3]=255
    save(rgba,'hazard_tile',64,64,border_LBRT=None,image_type='Tiled (Wrap Repeat)',note='COLORED, do not tint')
hazard()

def bezel():
    W=H=256*S; ys,xs=np.mgrid[0:H,0:W]+0.5; cx=cy=W/2
    dx,dy=xs-cx,ys-cy; rr=np.sqrt(dx*dx+dy*dy)
    R1,R0=124*S,98*S
    a=np.clip(R1-rr+0.5,0,1)*np.clip(rr-R0+0.5,0,1)
    n=np.stack([dx,dy])/ (rr+1e-6)
    shade=n[0]*L[0]+n[1]*L[1]
    mid=(R1+R0)/2; t=(rr-mid)/((R1-R0)/2)   # -1 inner .. 1 outer
    v=0.62+0.22*shade*np.sign(t+1e-6)*np.abs(t)**0.6
    edge=(np.abs(rr-R1)<3*S)|(np.abs(rr-R0)<3*S)
    v=np.where(edge,0.16,v)
    rgba=np.zeros((H,W,4)); rgba[...,:3]=v[...,None]*255; rgba[...,3]=a*255
    im=Image.fromarray(np.clip(rgba,0,255).astype(np.uint8),'RGBA'); d=ImageDraw.Draw(im)
    for k in range(6):
        ang=k*math.pi/3+math.pi/6; x=cx+math.cos(ang)*mid; y=cy+math.sin(ang)*mid; r=5*S
        d.ellipse([x-r+S,y-r+1.5*S,x+r+S,y+r+1.5*S],fill=(40,40,40,200)); d.ellipse([x-r,y-r,x+r,y+r],fill=(150,150,150,255)); d.ellipse([x-r*0.6,y-r*0.6,x+r*0.1,y+r*0.1],fill=(235,235,235,255))
    save(np.asarray(im,np.float32),'gauge_bezel',256,256,border_LBRT=None)
bezel()
json.dump(meta,open(os.path.join(OUT,'sprites_meta_v2.json'),'w'),indent=1)
print('ok',list(meta))
