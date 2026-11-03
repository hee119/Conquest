import {fit,TAU} from '../../core/index.js';
import {WATER,phase,drop,sphere} from './_water.js';
export const meta={name:'Octopus heavy water bomb',desc:'A large gathered sphere of water, twice the cannonball diameter, with three white wrapping currents on differently tilted paths and soft weighty wobble. No fuse or casing.',tags:['water','projectile','bomb','sphere','octopus','loop'],size:[144,144],origin:[72,72],duration:4,loop:true,fps:24,bg:'dark',options:{palette:["#123e79","#246fa8","#299fbe","#58c8e8","#93e3f7","#c5f4ff","#e6fbff","#ffffff"],radius:30,seed:7}};
export function draw(p,t,o){
 const C=fit(o.palette,8);
 for(let k=0;k<4;k++){
  const q=phase(t+k/4),a=k*TAU/4+.3;
  const d=o.radius+5+q*9;
  drop(p,Math.cos(a)*d,Math.sin(a)*d,1.6+(k%2)*.7,C,.65*(1-q));
 }
 sphere(p,t,o,o.radius,true);
}
