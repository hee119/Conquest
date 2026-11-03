// Water-jet banding techniques informed by Sprite Fusion's energy beam recipe,
// MIT License, Copyright (c) 2026 Hugo Duprez. Original Conquest pressure-stream design.
import {fit, TAU, WHITE} from '../../core/index.js';
import {WATER,phase,drop} from './_water.js';
export const meta={name:'Octopus hydro pump',desc:'A thick pressurized water stream with braided currents, foam crests and forward spray, anchored at the mouth and facing right.',tags:['water','beam','hydro','octopus','loop'],size:[320,144],origin:[28,70],duration:1,loop:true,fps:24,bg:'dark',options:{palette:["#123e79","#246fa8","#299fbe","#58c8e8","#93e3f7","#c5f4ff","#e6fbff","#ffffff"],length:232,width:13,seed:4}};
export function draw(p,t,o){
 const C=fit(o.palette,8),T=t*TAU,L=Math.min(240,o.length);
 for(let x=0;x<L;x++){
  const u=x/L,yc=Math.sin(T*3-x*.065)*(.3+u*.8);
  const half=o.width*(.57+.43*u)+Math.sin(x*.16-T*5)*(.7+u*1.2);
  for(let y=Math.floor(yc-half-1);y<=yc+half+1;y++){
   const v=(y-yc)/half,a=Math.abs(v);if(a>1)continue;
   const wave=Math.sin(x*.12-T*6+v*4);
   let c=a>.88?C[2]:a>.66?C[3]:a>.37?C[4]:C[5];
   if(wave>.72&&a<.83)c=C[6];
   if(a<.16&&Math.sin(x*.1-T*8)>.25)c=WHITE;
   if(v<-.72&&wave>-.2)c=C[6];

   p.px(x,y,c);
  }
 }
 // Sweeping pale currents down the beam, rather than a stationary laser core.
 for(let k=0;k<5;k++){
  const start=phase(t*4+k*.2)*L,pts=[];
  for(let j=0;j<20;j++){
   const x=start+j-20;if(x<0||x>L-2)continue;
   pts.push([x,Math.sin(x*.06-T*3+k*1.7)*(3+4*x/L)]);
  }
  if(pts.length>1)p.polyline(pts,k%2?C[6]:WHITE,.9);
 }
 p.ellipse(1,0,5,6,C[6]);p.ellipse(0,-1,3,3,WHITE);
 p.glow(4,0,15,C[4],.22);
}
