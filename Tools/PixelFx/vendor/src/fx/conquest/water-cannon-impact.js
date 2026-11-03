import {WATER,splash} from './_water.js';
export const meta={name:'Water cannon splash',desc:'A compact round water shot breaks into a low splash crown and short arcing droplets, fading to empty.',tags:['water','impact','splash','octopus'],size:[128,112],origin:[64,82],duration:.9,loop:false,fps:24,bg:'dark',options:{palette:WATER,radius:35,seed:4}};
export function draw(p,t,o){splash(p,t,o,meta.duration,false);}
