// Original Conquest recipes built with pixelfx. Water rim/caustic and droplet techniques informed by
// Sprite Fusion's open-source energy and blood VFX, MIT License, Copyright (c) 2026 Hugo Duprez.
import {fit, TAU, clamp, smooth, Rng, WHITE} from '../../core/index.js';
export const WATER = ['#062947','#0a4771','#0b70ac','#1599d5','#35bde9','#75def4','#c6f8ff','#f1feff'];
export const phase = (x) => ((x % 1) + 1) % 1;
export function drop(p,x,y,r,C,alpha=1) {
  p.disc(x,y,r,C[3],alpha*.85);
  p.disc(x-.35*r,y-.4*r,r*.45,C[6],alpha);
  if(r>1.6) p.blend(x-r*.35,y-r*.4,WHITE,alpha);
}
export function sphere(p,t,o,R,big=false) {
  const C=fit(o.palette,8), T=t*TAU, rx=R*(1+.012*Math.sin(T*2)), ry=R*(1-.01*Math.sin(T*2));
  const spin=t*TAU/4,cs=Math.cos(spin),sn=Math.sin(spin),wave=spin;
  // Clear water has a strong lower blue rim, transparent middle and broken white reflections.
  p.shade(-R-4,-R-4,R+4,R+7,(x,y,out)=>{
    const dx=x/rx,dy=y/ry, a=Math.atan2(dy,dx);
    const boundary=1+.005*Math.sin(3*a+T*2)+.004*Math.sin(5*a-T*3);
    const d=Math.hypot(dx,dy)/boundary;
    if(d>1)return;
    const nz=Math.sqrt(Math.max(0,1-d*d));
    const light=-dx*.4-dy*.55+nz*.72;
    if(d>.94){out.c=dy>.15?C[0]:(dx+dy<-.45?C[6]:C[2]);return;}
    let body=light>.95?C[5]:light>.76?C[4]:light>.48?C[3]:light>.22?C[2]:C[1];
    if(d>.82&&dy>.2)body=C[1];
    p.blend(x,y,body,.98);

  });
  // Connected, branching white caustics: elongated blue openings between broad foam seams.
  // Independent cell drift and traveling wave fields deform the foam separately from Y-axis rotation.
  const inner=new Rng(o.seed+193),sites=[],heading=inner.range(-.7,.7);
  const columns=10;
  for(let gy=-5;gy<=5;gy++)for(let gx=0;gx<columns;gx++){
    const phase=inner.range(0,TAU);
    sites.push([gx+.5+inner.range(-.4,.4)+.14*Math.sin(wave*2+phase),
      gy+.5+inner.range(-.4,.4)+.11*Math.cos(wave+phase)]);
  }
  p.shade(-R,-R,R,R,(px,py,out)=>{
    const nx=px/rx,ny=py/ry,d=Math.hypot(nx,ny);if(d>.86)return;
    // Inverse yaw on the front hemisphere: markings move left and wrap around the sides.
    const nz=Math.sqrt(Math.max(0,1-nx*nx-ny*ny));
    const qx=nx*cs+nz*sn,qz=-nx*sn+nz*cs;
    const x=Math.atan2(qx,qz),y=Math.asin(ny);
    const u=(x+heading)*columns/TAU+.32*Math.sin(y*4+.65*Math.sin(wave))+.085*Math.sin(y*5-x*2-wave*2);
    const v=y*2.7+.26*Math.sin(x*3+.75*Math.cos(wave))+.08*Math.sin(y*3+wave*2);
    const ix=Math.floor(u),iy=Math.floor(v);
    let first=99,second=99,third=99;
    for(let j=iy-1;j<=iy+1;j++)for(let i=ix-1;i<=ix+1;i++){
      const col=((i%columns)+columns)%columns,q=sites[(j+5)*columns+col];if(!q)continue;
      const dist=Math.hypot(u-(q[0]+i-col),v-q[1]);
      if(dist<first){third=second;second=first;first=dist;}else if(dist<second){third=second;second=dist;}else if(dist<third)third=dist;
    }
    const gap=second-first,width=.24+.045*Math.sin(x*4+y*3-wave*2)+.025*Math.sin(wave*2+x*2)+.13*(1-smooth(.15,.5,third-first));
    const seam=1-smooth(width*.45,width,gap);
    if(seam<=0)return;
    const edge=1-smooth(.69,.86,d);
    const light=-nx*.4-ny*.55+nz*.72;
    p.blend(px,py,light>.7?WHITE:light>.4?C[6]:C[5],seam*edge*(.65+.27*nz));
  });
  // Fixed top-left reflections make the rotating water read as a lit volume.
  p.arc(-R*.015,-R*.015,R*.84,big?1.5:.8,Math.PI*1.07,Math.PI*1.40,C[6]);
  p.ellipse(-R*.34,-R*.53,R*.14,R*.05,WHITE);
  if(o.outerRibbons!==false){
  // Three detached white water ribbons flow gently around the sphere.
  // Subtle traveling swells soften the curves without noisy foam or abrupt thickness changes.
  const rng=new Rng(o.seed+41), ribbons=[];
  for(let k=0;k<3;k++){
    const angle=k*TAU/3+rng.range(-.3,.3)+.08*Math.sin(T+k*2.1),tilt=rng.range(.55,1.05);
    const speed=k===1?-1:1,offset=rng.range(0,TAU);
    const ca=Math.cos(angle),sa=Math.sin(angle),ct=Math.cos(tilt),st=Math.sin(tilt);
    for(let j=0;j<=100;j++){
      const u=j/100,a=offset+T*speed+u*TAU*.30;
      const swell=Math.sin(a*2-T+k*2.2),fold=Math.sin(a*3+T+k);
      const x=Math.cos(a),y=Math.sin(a)*ct+.018*swell,z=Math.sin(a)*st;
      const orbit=1.35+.018*swell+.005*fold;
      const ax=(x*ca-y*sa)*rx*orbit,ay=(x*sa+y*ca)*ry*orbit;
      const az=z*R*orbit;
      ribbons.push({x:ax*cs-az*sn,y:ay,z:ax*sn+az*cs,u,k,swell,fold});
    }
  }
  ribbons.sort((a,b)=>a.z-b.z);
  for(const v of ribbons){
    const taper=Math.sin(Math.PI*v.u)**.65;
    if(taper<.12)continue;
    const r=(big?1.8:.9)*taper*(.92+.08*v.swell);
    // The opaque sphere hides the far ribbon, revealing an actual air gap at its silhouette.
    if(v.z<0&&Math.hypot(v.x/rx,v.y/ry)<1.05+r/R)continue;
    const back=v.z<0,dx=v.x/Math.hypot(v.x,v.y),dy=v.y/Math.hypot(v.x,v.y);
    p.disc(v.x,v.y,r+.2,C[6],.95);
    p.disc(v.x+dx*r*.12,v.y+dy*r*.12,r*.85,WHITE,.98);

  }
  }
  // Irregular breathing along the underside reads as water held together, never a bomb casing.
  if(big){
    const sag=.5+.5*Math.sin(T*2);
    p.disc(R*.15,ry-1,2+sag,C[3],.8);

  }
  p.glow(-R*.25,-R*.3,R*.82,C[5],.1);
}
export function splash(p,t,o,D,big=false){
  if(t>=D||t<0)return;
  const C=fit(o.palette,8), R=o.radius, q=t/D, fade=1-smooth(.62,1,q), rise=smooth(0,.1,q);
  const radius=R*(.15+.85*Math.pow(q,.52));
  const rng=new Rng(o.seed);
  // A low, foamy elliptical crown; opening center keeps the hit readable.
  if(q<.57){
    const a=(1-smooth(.28,.57,q))*rise;
    for(let k=0;k<2;k++){
      const rr=radius*(1-k*.11),pts=[];
      for(let j=0;j<=60;j++){
        const g=j/60*TAU;
        pts.push([Math.cos(g)*rr,Math.sin(g)*rr*.38-Math.max(0,-Math.sin(g))*R*.14*Math.sin(Math.min(1,q*4)*Math.PI)]);
      }
      p.polyline(pts,k?C[5]:C[6],a);
    }
  }
  // Broad liquid sheets rise first, then tear apart into rounded foam fingers.
  if(q<.53){
    const a=rise*(1-smooth(.3,.53,q)), spread=R*(.12+.95*Math.pow(q,.6));
    const pts=[[-spread,0]];
    for(let i=0;i<=16;i++){
      const u=i/16,x=-spread+u*spread*2;
      const finger=(.35+.65*Math.abs(Math.sin(i*2.3)))*Math.sin(Math.PI*u);
      const h=R*(.07+.52*finger)*Math.sin(Math.min(1,q*4)*Math.PI*.87);
      pts.push([x,-h]);
    }
    pts.push([spread,1]);
    p.poly(pts,C[4],a);
    p.polyline(pts.slice(1,-1),C[6],a);
    for(let k=0;k<7;k++){
      const u=(k+.5)/7,x=(u*2-1)*spread;
      const h=R*(.1+.26*Math.sin(k*2.1)**2)*Math.sin(Math.min(1,q*4)*Math.PI*.87);
      p.ellipse(x,-h,Math.max(1,R*.035),R*.027,C[6],a);
    }
  }
  // Lobes peel off the splash crown and arc back to the floor.
  const n=big?30:17;
  for(let i=0;i<n;i++){
    const a=rng.range(0,TAU),speed=rng.range(.62,1.02),delay=rng.range(0,.06);
    const age=Math.max(0,q-delay),x=Math.cos(a)*R*speed*age*1.25;
    const y=Math.sin(a)*R*.3*speed*age-R*rng.range(.5,.98)*Math.sin(Math.min(1,age/.82)*Math.PI);
    const r=rng.range(big?1.6:1.1,big?3.6:2.5)*(1-smooth(.45,.95,age));
    if(r>.2&&q>delay)drop(p,x,y,r,C,fade);
  }
  if(q<.2){
    const a=(1-q/.2)*rise;
    p.ellipse(0,-3,R*.2*(1+q*2),R*.09,C[6],a);
    p.ellipse(0,-4,R*.1,R*.04,WHITE,a);
  }
  // Restrained mist only near the landing surface.
  for(let i=0;i<5;i++){
    const x=Math.sin(i*2.3)*R*.5*q,y=-2-Math.cos(i*1.6)*R*.08-q*R*.1;
    p.disc(x,y,R*(.08+.09*q),C[5],d=>.14*fade*Math.sin(Math.PI*q)*(1-d*d));
  }
  if(q<.4)p.glow(0,-4,R*.65,C[5],.2*(1-q/.4));
}
