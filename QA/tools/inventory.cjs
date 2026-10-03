/* Phase 1 source inventory only: no network, database access, or application writes.
 * Run from repository root: node QA/tools/inventory.cjs
 * Source anchors are navigation aids, not executable test results.
 */
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const cp = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const ts = require(path.join(root, 'apps/web/node_modules/typescript'));
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const walk = d => fs.readdirSync(path.join(root, d), {withFileTypes:true}).flatMap(e => ['node_modules','bin','obj','.next','out','wwwroot'].includes(e.name) ? [] : e.isDirectory() ? walk(`${d}/${e.name}`) : [`${d}/${e.name}`]);
const line = (s, p) => s.slice(0, p).split('\n').length;
const clean = v => String(v ?? '').replace(/\s+/g,' ').trim();
const cell = v => clean(Array.isArray(v) ? v.join('; ') : v).replace(/\|/g,'\\|').replace(/`/g,"'");
const table = (heads, rows) => `| ${heads.join(' | ')} |\n| ${heads.map(()=> '---').join(' | ')} |\n${rows.map(r=>`| ${r.map(cell).join(' | ')} |`).join('\n')}\n`;
const out = (p, s) => {fs.mkdirSync(path.dirname(path.join(root,p)),{recursive:true});fs.writeFileSync(path.join(root,p),s);};
const anchor = (f,l) => `${f}:${l}`;
const inventory = {pages:[], forms:[], fields:[], ui:[], calls:[], functions:[], endpoints:[], contracts:[], classContracts:[], models:[], tests:[], resetRisks:[], files:[]};
const sourceFiles = [...walk('apps/web/src'), ...walk('apps/api'), ...walk('tests')].filter(f => /\.(tsx?|cs|csproj|css)$/.test(f));
for (const file of sourceFiles) {
  const source = read(file);
  inventory.files.push({file,sha256:crypto.createHash('sha256').update(source).digest('hex'),lines:source.split('\n').length});
  if (!/\.tsx?$/.test(file)) continue;
  const sf=ts.createSourceFile(file,source,ts.ScriptTarget.Latest,true,file.endsWith('.tsx')?ts.ScriptKind.TSX:ts.ScriptKind.TS);
  const route = file.endsWith('/page.tsx') ? '/'+file.replace('apps/web/src/app/','').replace(/\/?page.tsx$/,'') : '';
  if(route) inventory.pages.push({route,file,imports:[],headings:[]});
  function attr(n,k) { const a=n.attributes.properties.find(p=>ts.isJsxAttribute(p)&&p.name.getText(sf)===k); return a ? a.initializer ? clean(a.initializer.getText(sf)) : 'true' : ''; }
  function visit(n,form='',fn='') {
    if(ts.isFunctionDeclaration(n)||ts.isArrowFunction(n)||ts.isFunctionExpression(n)||ts.isMethodDeclaration(n)) {
      fn=n.name?.getText(sf)|| (ts.isVariableDeclaration(n.parent)?n.parent.name.getText(sf):fn)||'inline';
      const body=n.body?.getText(sf)||'';
      inventory.functions.push({file,line:line(source,n.pos),name:fn,async:!!n.modifiers?.some(m=>m.kind===ts.SyntaxKind.AsyncKeyword),hasAwait:/\bawait\b/.test(body)});
      const resets=[...body.matchAll(/\b(event|e)\.currentTarget\.reset\(\)/g)].filter(m=>/\bawait\b/.test(body.slice(0,m.index)));
      for(const m of resets) inventory.resetRisks.push({file,function:fn,line:line(source,(n.body?.getStart(sf)||0)+m.index),code:m[0]});
    }
    if(ts.isImportDeclaration(n)&&route) inventory.pages.at(-1).imports.push(n.moduleSpecifier.text);
    if(ts.isJsxElement(n)||ts.isJsxSelfClosingElement(n)) {
      const opening=ts.isJsxElement(n)?n.openingElement:n;
      const tag=opening.tagName.getText(sf), loc=line(source,n.getStart(sf));
      const text=ts.isJsxElement(n)?clean(n.children.filter(ts.isJsxText).map(x=>x.text).join(' ')):'';
      if(tag==='form') {
        // Line numbers are not unique: minified JSX can put several forms on one line.
        // This is an inventory occurrence key, not a permanent test-registry ID.
        form=`${file}#form-${inventory.forms.filter(f=>f.file===file).length+1}`;
        inventory.forms.push({id:form,file,line:loc,route,handler:attr(opening,'onSubmit'),function:fn});
      }
      const props=Object.fromEntries(opening.attributes.properties.filter(ts.isJsxAttribute).map(a=>[a.name.getText(sf),a.initializer?clean(a.initializer.getText(sf)):'true']));
      if(['input','select','textarea','StandardDateField','StandardSelectField','StandardTimeField'].includes(tag)) inventory.fields.push({file,line:loc,route,form,tag,name:props.name||props['aria-label']||props.placeholder||props.label||props.value||'unnamed',type:props.type||tag,identity:`${tag}:${props.type||tag}:${props.accept||''}`,props});
      if(['button','a','Link','dialog','table','ul','ol','details','summary','nav','article'].includes(tag)||props.onClick||props.onKeyDown||/Modal|Dialog|Popover|Tile|Select|DateField|TimeField|Menu|Tooltip|Sheet/.test(tag)) inventory.ui.push({file,line:loc,route,tag,text,props});
      if(/^h[1-6]$/.test(tag)&&route) inventory.pages.at(-1).headings.push(text||clean(n.getText(sf)).slice(0,160));
    }
    if(ts.isCallExpression(n)&&/(academyApi|fetch)$/.test(n.expression.getText(sf))) {
      const init=n.arguments[1], props=init&&ts.isObjectLiteralExpression(init)?init.properties:[];
      const get=k=>props.find(p=>p.name?.getText(sf)===k)?.initializer?.getText(sf)||'';
      inventory.calls.push({file,line:line(source,n.getStart(sf)),function:fn,path:clean(n.arguments[0]?.getText(sf)),method:get('method').replace(/["']/g,'')||'GET or dynamic init',body:clean(get('body'))});
    }
    ts.forEachChild(n,c=>visit(c,form,fn));
  }
  visit(sf);
}
const perm=read('apps/api/Security/PermissionCatalog.cs');
const permissions=Object.fromEntries([...perm.matchAll(/\["(\w+Controller)"\]\s*=\s*\[([^\]]+)\]/g)].map(m=>[m[1],m[2].replace(/"/g,'')]));
for(const file of walk('apps/api/Controllers').filter(f=>f.endsWith('.cs'))) {
 const s=read(file);
 const controllers=[...s.matchAll(/public\s+(?:sealed\s+)?class\s+(\w+Controller)\b/g)];
 const methods=[...s.matchAll(/\[Http(Get|Post|Put|Patch|Delete)(?:\(([^\]]*)\))?\]/g)];
 for(let i=0;i<methods.length;i++) {
  const m=methods[i], chunk=s.slice(m.index,methods[i+1]?.index??s.length);
  const classMatch=controllers.filter(c=>c.index<m.index).at(-1);
  const controller=classMatch?.[1]||path.basename(file,'.cs');
  const classPrefix=s.slice(0,classMatch?.index??m.index);
  const base=[...classPrefix.matchAll(/\[Route\("([^"]*)"\)\]/g)].at(-1)?.[1]?.replace('[controller]',controller.replace('Controller',''))||'';
  const sig=chunk.match(/public\s+(?:async\s+)?([^\n{]+?\([^]*?\))\s*(?:\{|=>)/);
  const signature=clean(sig?.[1]);
  const name=signature.match(/(\w+)\s*\(/)?.[1]||'UNRESOLVED';
  const template=m[2]?.match(/^"([^"]*)"/)?.[1];
  const route=template?.startsWith('/')?template:'/'+[base,template].filter(Boolean).join('/');
  const refs=[...new Set([...chunk.matchAll(/\b(?:db|dbContext|academyDb|identityDb)\.(\w+)/g)].map(x=>x[1]).filter(x=>!['SaveChangesAsync','Database','Add','AddRange','Entry'].includes(x)))];
  const validations=chunk.split(/\r?\n/).map(clean).filter(x=>/\bif\s*\(|BadRequest|Forbid\(|NotFound\(/.test(x));
  const returns=chunk.split(/\r?\n/).map(clean).filter(x=>/return (?:Ok|Created|File|BadRequest|NotFound|Forbid|Unauthorized|NoContent)|^Ok\(/.test(x));
  inventory.endpoints.push({file,line:line(s,m.index),controller,name,method:m[1].toUpperCase(),route,signature,permission:permissions[controller]||'no catalog mapping',academyFilter:/\bGuid academyId\b/.test(signature),explicitAuthorize:/(?:\[|,)\s*Authorize/.test(s),dbSets:refs,saveCalls:[...chunk.matchAll(/SaveChangesAsync\(/g)].length,validations,returns});
 }
 for(const m of s.matchAll(/(?:public|private)\s+(?:sealed\s+)?record\s+(\w+)\s*\(([^]*?)\);/g)) inventory.contracts.push({file,line:line(s,m.index),name:m[1],fields:clean(m[2])});
 for(const m of s.matchAll(/public\s+(?:sealed\s+)?class\s+(\w+(?:Request|Dto))\s*\{/g)) {let depth=1,end=m.index+m[0].length;for(;end<s.length&&depth;end++){if(s[end]==='{')depth++;if(s[end]==='}')depth--;}inventory.classContracts.push({file,line:line(s,m.index),name:m[1],definition:clean(s.slice(m.index,end))});}
}
const model=read('apps/api/Data/AcademyDeskDbContext.cs');
const snap=read('apps/api/Migrations/AcademyDeskDbContextModelSnapshot.cs');
for(const m of model.matchAll(/DbSet<(\w+)>\s+(\w+)/g)) {
 const file=`apps/api/Domain/Entities/${m[1]}.cs`, s=fs.existsSync(path.join(root,file))?read(file):'';
 const config=model.match(new RegExp(`modelBuilder.Entity<${m[1]}>\\(entity =>\\s*\\{([^]*?)\\n        \\}\\);`))?.[1]||'';
 const snapBlock=snap.split(`modelBuilder.Entity("AcademyDesk.Api.Domain.Entities.${m[1]}", b =>`)[1]?.split('modelBuilder.Entity(')[0]||'';
 inventory.models.push({name:m[1],dbSet:m[2],table:snapBlock.match(/ToTable\("([^"]+)"/)?.[1]||m[2],file,properties:[...s.matchAll(/public\s+(?:required\s+)?([\w<>?\[\]]+)\s+(\w+)\s*\{\s*get;/g)].map(p=>({type:p[1],name:p[2],nullable:p[1].includes('?')})),constraints:config.split(/\r?\n/).map(clean).filter(Boolean)});
}
for(const file of walk('tests').filter(f=>f.endsWith('.cs'))) {
 const s=read(file);
 for(const m of s.matchAll(/\[Fact\]\s*public async Task (\w+)\(/g)) inventory.tests.push({file,line:line(s,m.index),name:m[1],provider:'EF InMemory; controller direct call; no HTTP pipeline'});
}
inventory.resetRisks=[...new Map(inventory.resetRisks.map(r=>[`${r.file}:${r.line}:${r.code}`,r])).values()];
const git=a=>cp.execFileSync('git',a,{cwd:root,encoding:'utf8'}).trim();
inventory.baseline={date:new Date().toISOString(),branch:git(['branch','--show-current']),commit:git(['rev-parse','HEAD']),dirtyApplicationFiles:git(['diff','--name-only','--','apps','tests']).split('\n').filter(Boolean),scope:'Working tree; AST/lexical source inventory, not runtime coverage'};
out('QA/INVENTORY/source-inventory.json',JSON.stringify(inventory,null,2)+'\n');
out('QA/INVENTORY/PAGES.md','# Page and import inventory\n\nCount is route files, not dynamically rendered tabs. API consumers are file-local; use imports to trace composed components.\n\n'+table(['Route','Source','Headings','Imports','API calls'],inventory.pages.map(p=>[p.route,p.file,p.headings,p.imports,inventory.calls.filter(c=>c.file===p.file).map(c=>`${c.method} ${c.path}`)])));
out('QA/INVENTORY/FORMS_AND_FIELDS.md','# Form and field source occurrences\n\nNative form counts and JSX control occurrences are exact for this scan. Repeated/map-generated controls count once in source; hidden controls and shared control implementations are explicitly included. Empty form association means controlled inputs or shared components outside a native form. Not a claim about rendered field counts.\n\n'+table(['Form key','Route','Submit handler'],inventory.forms.map(f=>[f.id,f.route,f.handler]))+'\n## Fields\n\n'+table(['Source','Form','Control','Name/identity','Declared constraints and bindings','Boundary families'],inventory.fields.map(f=>{const p=f.props;const test=/number/.test(f.type)?'blank/null; zero/negative/decimal; finite/overflow; min/max ± step':/Date|date/.test(f.type)?'blank/null; valid ISO; invalid date/leap day; future/past per server; timezone':/Time|time/.test(f.type)?'blank/null; 00:00/23:59; AM/PM; paired range; zone conversion':/checkbox/.test(f.type)?'true/false; omitted; invalid API bool':/file/.test(f.type)?'empty; allowed/disallowed MIME/ext; size boundary; interrupted upload':/Select|select/.test(f.tag)?'empty; allowed; unknown/deleted/cross-tenant ID; long labels':'blank/null/space; trim; Unicode; declared length ±1; malformed email/URL where typed';return [anchor(f.file,f.line),f.form,f.tag,f.name,JSON.stringify(p),test];})));
out('QA/INVENTORY/UI_COMPONENTS.md','# UI element source occurrences\n\nIncludes buttons/actions/links, menus, cards, tables, lists and shared overlays. Props retain handlers and conditions for tracing, not evaluated DOM. CSS effects and dynamic states need browser testing.\n\n'+table(['Source','Route','Element','Text','Props / handler'],inventory.ui.map(u=>[anchor(u.file,u.line),u.route,u.tag,u.text,JSON.stringify(u.props)])));
out('QA/INVENTORY/API_ENDPOINTS.md','# Controller action inventory\n\nAttribute routes and action signatures extracted from source; Identity mapped endpoints, health and development OpenAPI are recorded separately in the main inventory. Validations below are candidate branches, not test results. Last-action chunks can include private helpers/records; consult source anchors.\n\n'+table(['Source','Method','Route','Action / request signature','Global academy gate','Permission catalog','Data sets','Save calls'],inventory.endpoints.map(e=>[anchor(e.file,e.line),e.method,e.route,e.signature,e.academyFilter?'AcademyAccessFilter + action checks':'Action/middleware checks',e.permission,e.dbSets,e.saveCalls]))+'\n## Action branch and response traces\n\n'+inventory.endpoints.map(e=>`### ${e.controller}.${e.name} (${e.method} ${e.route})\n\nSource: ${anchor(e.file,e.line)}\n\n${table(['Validation/guard expressions'],e.validations.map(x=>[x]))}\n${table(['Return / serialization expressions'],e.returns.map(x=>[x]))}`).join('\n'));
out('QA/INVENTORY/CONTRACTS.md','# Request / response record contracts\n\nNullable annotations, defaults and DTO types from source. Default ASP.NET JSON uses camelCase; verify real serialized responses separately.\n\n'+table(['Source','Contract','Fields'],inventory.contracts.map(c=>[anchor(c.file,c.line),c.name,c.fields]))+'\n## Class-based multipart contracts\n\n'+table(['Source','Contract','Definition'],inventory.classContracts.map(c=>[anchor(c.file,c.line),c.name,c.definition]))+'\n## Client calls and serialized bodies\n\n'+table(['Source','Function','Method','API path','Request expression'],inventory.calls.map(c=>[anchor(c.file,c.line),c.function,c.method,c.path,c.body])));
out('QA/INVENTORY/DATABASE.md','# Application database sets and fields\n\nSQL Server mappings from DbContext and EF model snapshot. Actual deployed schema is not introspected. EntityBase/AcademyEntity inherited fields and Identity tables are documented in the main inventory.\n\n'+inventory.models.map(m=>`## ${m.name} → ${m.table}\n\nSource: ${m.file}; DbSet: ${m.dbSet}\n\n${table(['Property','Type','Nullable annotation'],m.properties.map(p=>[p.name,p.type,p.nullable?'yes':'no']))}\n${table(['Mapping / constraint'],m.constraints.map(c=>[c]))}`).join('\n'));
out('QA/INVENTORY/ASYNC_FORM_RISKS.md','# Async form event reset candidates\n\nSource finding: these handlers use event.currentTarget.reset() after await. Browser reproduction is required before closure; no production write was performed. React currentTarget is only set while dispatching the handler.\n\n'+table(['Source','Function','Expression'],inventory.resetRisks.map(r=>[anchor(r.file,r.line),r.function,r.code])));
out('QA/INVENTORY/SOURCE_MANIFEST.md','# Audited source manifest\n\nSHA-256 fingerprints preserve the precise working-tree baseline including pre-existing modifications. Generated EF migrations included for schema traceability.\n\n'+table(['File','Lines','SHA-256'],inventory.files.map(f=>[f.file,f.lines,f.sha256])));
const tracked=git(['ls-files']).split('\n').filter(f=>!f.startsWith('QA/')&&fs.existsSync(path.join(root,f))).map(file=>({file,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')}));
out('QA/INVENTORY/REPOSITORY_MANIFEST.md','# Tracked repository fingerprint\n\nIncludes documentation, deployment definitions, package lock and configuration file hashes. Values/secrets are not exported. Not every file has been semantically verified.\n\n'+table(['File','SHA-256'],tracked.map(f=>[f.file,f.sha256])));
console.log(JSON.stringify({pages:inventory.pages.length,forms:inventory.forms.length,fieldOccurrences:inventory.fields.length,uiOccurrences:inventory.ui.length,apiActions:inventory.endpoints.length,controllers:new Set(inventory.endpoints.map(e=>e.controller)).size,models:inventory.models.length,contracts:inventory.contracts.length,tests:inventory.tests.length,resetRisks:inventory.resetRisks.length,sourceFiles:inventory.files.length},null,2));
