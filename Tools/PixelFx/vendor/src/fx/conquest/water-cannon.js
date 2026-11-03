import {TAU,fit} from '../../core/index.js';
import {WATER,phase,drop,sphere} from './_water.js';
export const meta={name:'Octopus water cannon',desc:'A compact round water cannonball with a glassy blue body, white inner wave patterns and no outer orbiting ribbons and short trailing droplets.',tags:['water','projectile','cannonball','octopus','loop'],size:[100,88],origin:[56,44],duration:4,loop:true,fps:24,bg:'dark',options:{palette:["#123e79","#246fa8","#299fbe","#58c8e8","#93e3f7","#c5f4ff","#e6fbff","#ffffff"],radius:15,seed:3,outerRibbons:false}};
export function draw(p,t,o){
 const C=fit(o.palette,8);
 for(let k=0;k<5;k++){
  const q=phase(t*2+k/5),x=-o.radius-5-q*22,y=Math.sin(k*2.5)*7*(.4+q);
  drop(p,x,y,1.1+(k%2)*.7,C,(1-q)*.7);
 }
 sphere(p,t,o,o.radius,false);
}
