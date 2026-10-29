'use strict';
const test=require('node:test'),a=require('node:assert/strict'),{verify}=require('./staff-control-bounds.cjs');
const good=()=>({viewport:390,boxes:Array.from({length:25},()=>({left:22,right:368,width:346,containerLeft:22,containerRight:368}))});
test('Staff nested geometry checks catch clipped controls hidden by outer overflow rules',()=>{a.equal(verify(good()),true);for(const mutate of[v=>v.boxes[2].right=470,v=>v.boxes[3].left=-24,v=>v.boxes[4].right=375,v=>v.boxes[5].left=12,v=>v.boxes[6].width=0,v=>v.boxes[7].right=NaN,v=>v.boxes.pop()&&v.boxes.splice(0,20),v=>v.viewport=0]){const value=good();mutate(value);a.throws(()=>verify(value));}});
