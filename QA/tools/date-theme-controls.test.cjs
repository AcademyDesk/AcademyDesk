// Actual shared components, controlled React hooks. Not browser/device proof.
const fs=require('node:fs'),assert=require('node:assert/strict'),{test}=require('node:test');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
function compile(file){return ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.ReactJSX}}).outputText;}
const nodes=node=>node==null?[]:Array.isArray(node)?node.flatMap(nodes):typeof node==='object'?[node,...nodes(node.props?.children)]:[node];
function date(value=''){
 const states=[],effects=[],changes=[];let index=0,mounted=false,props={name:'synthetic-date',value,onChange:value=>changes.push(value),label:'Synthetic date'};
 const react={useState:initial=>{const current=index++;if(!(current in states))states[current]=typeof initial==='function'?initial():initial;return[states[current],next=>states[current]=typeof next==='function'?next(states[current]):next];},useEffect:fn=>{if(!mounted)effects.push(fn);},useRef:value=>({current:value}),useCallback:fn=>fn,useMemo:fn=>fn()};
 const mod={exports:{}};new Function('require','module','exports',compile('apps/web/src/components/standard-date-field.tsx'))(name=>name==='react/jsx-runtime'?jsx:name==='react'?react:{AnchoredPopup:'dialog'},mod,mod.exports);
 const render=()=>{index=0;const result=mod.exports.StandardDateField(props);mounted=true;return nodes(result);};
 const trigger=()=>render().find(node=>node.props?.className==='standard-date-trigger').props.onClick();
 return{render,trigger,changes,props:value=>{props={...props,...value};},effects,states};
}
test('date selected value and hidden form value are stable before mount',()=>{const f=date('2001-02-03'),tree=f.render();assert.equal(tree.find(node=>node.type==='input').props.value,'2001-02-03');assert.equal(tree.find(node=>node.props?.className==='standard-date-trigger').props['aria-expanded'],false);});
test('opening an empty date uses current local year/month, with direct year selection',()=>{const f=date();f.render();f.trigger();const tree=f.render(),now=new Date();assert.equal(tree.find(node=>node.props?.['aria-label']==='Select year').props.value,now.getFullYear());assert.equal(tree.find(node=>node.props?.['aria-label']==='Select month').props.value,now.getMonth());assert.ok(tree.filter(node=>node.type==='option').some(node=>node.props.value===1900));});
test('open calendar follows externally replaced value without a syncing effect',()=>{const f=date('2001-02-03');f.render();f.trigger();f.props({value:'2032-11-04'});const tree=f.render();assert.equal(tree.find(node=>node.props?.['aria-label']==='Select year').props.value,2032);assert.equal(tree.find(node=>node.props?.['aria-label']==='Select month').props.value,10);});
test('year and month navigation retain the current controlled source value',()=>{const f=date('2001-02-03');f.render();f.trigger();f.render().find(node=>node.props?.['aria-label']==='Select year').props.onChange({target:{value:'1990'}});f.render().find(node=>node.props?.['aria-label']==='Select month').props.onChange({target:{value:'6'}});const tree=f.render();assert.equal(tree.find(node=>node.props?.['aria-label']==='Select year').props.value,1990);assert.equal(tree.find(node=>node.props?.['aria-label']==='Select month').props.value,6);assert.equal(tree.find(node=>node.type==='input').props.value,'2001-02-03');assert.deepEqual(f.changes,[]);});
test('choosing a date emits once and closes calendar',()=>{const f=date('2001-02-03');f.render();f.trigger();f.render().find(node=>node.props?.['data-selected']).props.onClick();assert.deepEqual(f.changes,['2001-02-03']);assert.equal(f.render().some(node=>node.type==='dialog'),false);});
test('required dates cannot be cleared by shared calendar action',()=>{const f=date('2001-02-03');f.props({required:true});f.render();f.trigger();assert.equal(f.render().find(node=>node.type==='button'&&node.props.children==='Clear').props.disabled,true);});
test('disabled date cannot display its popup',()=>{const f=date('2001-02-03');f.render();f.trigger();f.props({disabled:true});const tree=f.render();assert.equal(tree.some(node=>node.type==='dialog'),false);assert.equal(tree.find(node=>node.props?.className==='standard-date-trigger').props.disabled,true);});
for(const theme of ['light','dark'])for(const hydrated of [false,true])test(`theme toggle ${theme}, hydrated ${hydrated}`,()=>{
 const saved=[],mod={exports:{}};const react={useState:initial=>[initial,()=>{}],useEffect:()=>{},useSyncExternalStore:(_subscribe,client,server)=>(hydrated?client:server)()};
 new Function('require','module','exports',compile('apps/web/src/components/theme-toggle.tsx'))(name=>name==='react/jsx-runtime'?jsx:name==='react'?react:{useTheme:()=>({theme,setTheme:value=>saved.push(value)})},mod,mod.exports);
 const buttons=nodes(mod.exports.ThemeToggle()).filter(node=>node.type==='button');assert.equal(buttons.length,2);
 assert.equal(buttons[0].props['aria-pressed'],hydrated?theme==='light':undefined);assert.equal(buttons[1].props['aria-pressed'],hydrated?theme==='dark':undefined);
 buttons[0].props.onClick();buttons[1].props.onClick();assert.deepEqual(saved,['light','dark']);
});
