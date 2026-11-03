// Original Conquest radial stone-wave choreography. Faceted stone and layered eruption principles
// informed by Sprite Fusion's ice/explosion recipes, MIT License, Copyright (c) 2026 Hugo Duprez.
import {fit,Rng,TAU,clamp,smooth,ease} from '../../core/index.js';
const STONE=['#1c1923','#302b32','#4a4144','#665951','#897763','#b39c7c','#dbc4a0','#f2dfbc'];
export const meta={name:'Octopus radial stone eruption',desc:'Cracks run outward from the center as successive circular rows of grounded stone slabs punch up, shed grit and settle into the floor.',tags:['earth','stone','radial','crack','octopus'],size:[352,256],origin:[176,151],duration:2.2,loop:false,fps:24,bg:'ground',options:{palette:STONE,radius:130,rings:3,seed:9}};
function stone(p,s,t,C){
 const age=t-s.at;if(age<0||age>1.13)return;
 const up=ease.outExpo(clamp(age/.12))*(1-smooth(.63,1.13,age));
 const h=s.h*up,w=s.w,base=s.y;
 if(h<.5)return;
 p.ellipse(s.x,base+1,w*1.15,2.2,C[0]);
 // A broad buried foot and one off-center apex give each slab a heavy, sharp profile.
 const tip=s.x+w*s.lean, ridge=s.x-w*.12;
 const pts=[[s.x-w,base],[s.x-w*.84,base-h*s.shoulder],[tip,base-h],
  [s.x+w*.76,base-h*(s.shoulder*.85)],[s.x+w,base]];
 p.poly(pts,C[2]);
 p.poly([[s.x-w,base],[s.x-w*.84,base-h*s.shoulder],[tip,base-h],
  [ridge,base-h*.38],[s.x-w*.1,base]],C[4]);
 p.poly([[tip,base-h],[s.x+w*.76,base-h*(s.shoulder*.85)],[s.x+w,base],
  [s.x+w*.18,base],[ridge,base-h*.38]],C[2]);
 p.poly([[tip,base-h],[tip+w*.21,base-h*.69],[ridge,base-h*.38]],C[6]);
 p.line(tip,base-h,ridge,base-h*.38,C[5]);
 p.line(ridge,base-h*.38,s.x-w*.1,base-1,C[3]);
 p.line(s.x-w*.68,base-h*.25,s.x-w*.36,base-h*.17,C[5]);
 if(h>16)p.polyline([[s.x+w*.23,base-h*(s.shoulder*.85)],[s.x+w*.05,base-h*.24],
  [s.x+w*.36,base-h*.15]],C[1]);
 // A dark broken foot locks the broad stone into the floor as it rises.
 p.poly([[s.x-w,base],[s.x-w*.52,base-2*up],[s.x-w*.12,base-1],
  [s.x+w*.33,base-3*up],[s.x+w,base],[s.x+w*.8,base+1]],C[1]);
 const puff=Math.sin(Math.min(1,age/.55)*Math.PI);
 if(puff>0){p.disc(s.x-w,base-1-age*3,3+age*7,C[5],puff*.22);p.disc(s.x+w,base,3+age*6,C[4],puff*.18);}
 for(let k=0;k<3;k++){
  const q=age-k*.018;if(q<0||q>.5)continue;
  const x=s.x+(k-1)*q*18,y=base-q*(22+k*9)+q*q*65;
  p.rect(x,y,2,2,C[4+k]);
 }
}
export function draw(p,t,o){
 if(t>=meta.duration||t<0)return;
 const C=fit(o.palette,8),rng=new Rng(o.seed),R=Math.min(130,Math.max(24,o.radius));
 const rings=Math.min(4,Math.max(1,Math.round(o.rings))),fade=1-smooth(1.4,2.2,t),stones=[];
 // Dark fissures advance before the moving stone front, with short branch cracks.
 for(let k=0;k<13;k++){
  const a=k/13*TAU+rng.range(-.09,.09),extent=R*smooth(.04,.56,t),pts=[];
  for(let j=0;j<=5;j++){
   const r=extent*j/5,aa=a+(j%2?.045:-.035);
   pts.push([Math.cos(aa)*r,Math.sin(aa)*r*.7]);
  }
  p.polyline(pts,C[0],fade);
  if(extent>30){const tip=pts[3];p.line(tip[0],tip[1],tip[0]+Math.cos(a+.7)*12,tip[1]+Math.sin(a+.7)*6,C[1],fade);}
 }
 for(let j=0;j<rings;j++){
  const r=R*(j+1)/rings,n=8+j*4;
  for(let k=0;k<n;k++){
   const a=k/n*TAU+(j%2?.18:0)+rng.range(-.035,.035);
   const scale=rng.range(.5,1.6);
   stones.push({x:Math.cos(a)*r,y:Math.sin(a)*r*.7,
    w:rng.range(10,14)*scale,h:rng.range(23,36)*scale,lean:rng.range(-.4,.4),shoulder:rng.range(.3,.55),
    at:.1+j*.17+rng.range(0,.045)});
  }
 }
 stones.sort((a,b)=>a.y-b.y);
 for(const s of stones)stone(p,s,t,C);
}
