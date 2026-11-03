import {WATER,splash} from './_water.js';
export const meta={name:'Heavy water bomb splash',desc:'A heavy water sphere lands in a broad foamy crown, throwing large water lobes and falling droplets around the impact. No warning marker.',tags:['water','bomb','impact','splash','octopus'],size:[240,192],origin:[120,144],duration:1.3,loop:false,fps:24,bg:'dark',options:{palette:WATER,radius:78,seed:8}};
export function draw(p,t,o){splash(p,t,o,meta.duration,true);}
