import {readFileSync, writeFileSync, mkdirSync, cpSync, readdirSync, rmSync, lstatSync} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {controls} from '../content/controls.mjs';
import {guides} from '../content/guides.mjs';
import {descriptions} from '../content/reference.mjs';

const root=fileURLToPath(new URL('../',import.meta.url));
const out=path.join(root,'dist');
const previews=JSON.parse(readFileSync(path.join(root,'content/previews.json'),'utf8'));
const api=JSON.parse(readFileSync(path.join(root,'content/api.json'),'utf8'));
const escape=value=>String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;').replaceAll("'",'&#39;');
const p=text=>`<p>${text}</p>`;
const list=(items,ordered=false)=>`<${ordered?'ol':'ul'}>${items.map(x=>`<li>${x}</li>`).join('')}</${ordered?'ol':'ul'}>`;
const subheading=text=>`<h3>${escape(text)}</h3>`;
const links=items=>list(items.map(([slug,title,description])=>`<a href="@${slug}">${escape(title)}</a>${description?list([escape(description)]):''}`));
function highlight(language,text){
  if(language!=='csharp')return escape(text);
  const tokens=/\/\/[^\n]*|\/\*[\s\S]*?\*\/|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|\b(?:using|namespace|public|private|protected|internal|partial|class|static|sealed|abstract|override|virtual|void|var|new|return|if|else|while|foreach|in|true|false|null|async|await|this|base|typeof|string|int|bool|float|double|object|event|try|catch|finally|throw|ref|out)\b|\b\d+(?:\.\d+)?[fFdD]?\b/g;
  let result='',at=0;
  for(const match of text.matchAll(tokens)){
    result+=escape(text.slice(at,match.index));
    const token=match[0];const kind=token.startsWith('//')||token.startsWith('/*')?'comment':token.startsWith('"')||token.startsWith("'")?'string':/^\d/.test(token)?'number':'keyword';
    result+=`<span class="token-${kind}">${escape(token)}</span>`;at=match.index+token.length;
  }
  return result+escape(text.slice(at));
}
const code=(language,text,label='')=>`<figure class="code-block"><figcaption>${escape(label||({csharp:'C#',xml:'MXML',shell:'Command Line'}[language]||language))}<button class="copy" type="button" aria-label="Copy ${escape(label||language)} code">Copy</button></figcaption><pre tabindex="0"><code class="language-${language}">${highlight(language,text)}</code></pre></figure>`;
const pair=(a,b)=>subheading('C# Example')+a+subheading('MXML Example')+b;
const terminal=slug=>`<figure class="output"><figcaption>Example Output:</figcaption><pre tabindex="0" aria-label="${escape(slug)} text render">${escape(previews[slug]??'')}</pre></figure>`;
const table=(heads,rows)=>`<div class="table-wrap" tabindex="0"><table><thead><tr>${heads.map(x=>`<th scope="col">${escape(x)}</th>`).join('')}</tr></thead><tbody>${rows.map(row=>`<tr>${row.map((x,i)=>`<${i?'td':'th scope="row"'}>${escape(x)}</${i?'td':'th'}>`).join('')}</tr>`).join('')}</tbody></table></div>`;
const file=name=>readFileSync(path.join(root,'examples',name),'utf8').trim();
function recipe(name){const source=file('Recipes.cs').replaceAll('\r','');const text=source.split(`// sample:${name}\n`)[1]?.split(`// end:${name}`)[0];if(!text)throw new Error(`Missing recipe ${name}`);return text.split('\n').map(line=>line.startsWith('        ')?line.slice(8):line).join('\n').trim();}
const pages=guides({p,code,table,pair,terminal,file,recipe,list,links,subheading});
const knownRoutes=new Map(pages.filter(x=>x.type).map(x=>[x.type,x.slug]));
for(const control of controls)knownRoutes.set(control.title,control.slug);
for(const c of controls){
  pages.push({...c,type:c.title,group:'Controls',route:`controls/${c.slug}`,sections:[
    {id:'members',title:'Properties and Methods',html:list(c.members.map(([name,description])=>`<strong><code>${escape(name)}</code>:</strong> ${escape(description)}`))},
    {id:'csharp',title:'C# Example',html:p('Create the control and add it to the page\'s Children or the window\'s Elements collection. The <a href="@controls">Controls Overview</a> lists the namespaces used by these examples.')+code('csharp',c.code)},
    {id:'mxml',title:'MXML Example',html:(c.setup?p('This example needs a factory to configure the control. Register it before compiling the page:')+code('csharp',c.setup):'')+(c.mxml.includes('{App.')?p('App refers to the SettingsModel backend from the <a href="@data-binding">Data Binding Guide</a>. Register it with page.BackendObjects.Add("App", model) before compiling.'):'')+code('xml',c.mxml)},
    {id:'output',title:'Output Example',html:terminal(c.slug)},
    ...(c.keys?[{id:'keyboard',title:'Keyboard Controls',html:list(c.keys.map(([key,description])=>`<strong>${escape(key)}:</strong> ${escape(description)}`))}]:[]),
    {id:'notes',title:'Notes',html:c.notes.map(p).join('')+p('The common position, color, visibility and lifetime settings are inherited from <a href="@uielement">UIElement</a>. While attached to a running window, update the control on the <a href="@window">UI thread</a>.')},
    {id:'related',title:'Related Docs',html:links(c.related.map(slug=>[slug,pages.find(x=>x.slug===slug)?.title||controls.find(x=>x.slug===slug)?.title||slug]))}
  ]});
}
pages.splice(2,0,{slug:'controls',title:'Controls Overview',group:'Getting Started',summary:'MelonUI includes controls for displaying information, handling input and arranging pages. The following pages describe how to use each control in C# and MXML, with examples of the output.',sections:[
  {id:'imports',title:'Using Controls in C#',html:p('The C# examples use these namespaces:')+code('csharp',`using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Enums;
using System.Drawing;`)+p('Each example creates a control variable. Add it to Children on a page, or Elements on the window. MXML automatically resolves core types. Examples using App bindings refer to the SettingsModel class in the Data Binding guide.')},
  ...[...new Set(controls.map(x=>x.group))].map(group=>({id:group.toLowerCase().replaceAll(/[^a-z]+/g,'-'),title:group.replaceAll(' & ',' and '),html:links(controls.filter(c=>c.group===group).map(c=>[c.slug,c.title,c.summary]))})),
  {id:'other-controls',title:'Other Controls',html:p('You can also build a <a href="@custom-controls">custom UIElement</a>, or use the <a href="@shaders">optional shader controls</a>. The output examples are plain text, so they do not show the color of a rendered frame.')}
]});
function inventory(type){
  const groups={Constructors:[],Properties:[],Events:[],Fields:[],Methods:[]};
  for(const signature of type.signatures){const category=signature.startsWith(type.name+'(')?'Constructors':signature.includes(' { ')?'Properties':signature.startsWith('event ')?'Events':signature.includes('(')?'Methods':'Fields';groups[category].push(signature);}
  return p(`The following members are declared by <code>${escape(type.fullName??type.name)}</code>.${type.baseType?` The base type is <code>${escape(type.baseType)}</code>.`:''}`)+
    (type.isElement?p('Common inherited element members are described in <a href="@uielement">UIElement</a>.'):'')+
    (type.values.length?subheading('Values')+list(type.values.map(value=>`<code>${escape(value)}</code>`)):'')+
    Object.entries(groups).filter(([,items])=>items.length).map(([title,items])=>subheading(title)+list(items.map(item=>`<code>${escape(item)}</code>`))).join('')+
    (!type.values.length&&!type.signatures.length?p('This type does not declare additional public members. See its base type for inherited behavior.'):'')+
    p('These signatures list public members from the current assembly. Nullable reference annotations and generic constraints are not reproduced here.');
}
for(const type of api){
  const section={id:'api',title:'API Reference',html:inventory(type)};
  const slug=knownRoutes.get(type.name);
  if(slug)pages.find(x=>x.slug===slug).sections.push(section);
  else{
    const name='api-'+type.fullName.replaceAll(/[^a-zA-Z0-9]/g,'-').toLowerCase();knownRoutes.set(type.name,name);
    pages.push({slug:name,route:`api/${type.name.toLowerCase()}`,title:type.name,group:'API',type:type.name,summary:descriptions[type.name]||`The ${type.name} structure is used by the platform renderer for native interop. Normal application UI should be managed through ConsoleWindowManager.`,sections:[section]});
  }
}
pages.push({slug:'api',title:'API Reference',group:'Reference',summary:'The following types are available in the core MelonUI library. Each page lists the public members declared by that type, including properties generated by the binding analyzer.',sections:[
  ...[...new Set(api.map(x=>x.ns||'(global namespace)'))].map(ns=>({id:ns.replaceAll(/[^a-zA-Z0-9]/g,'-').toLowerCase(),title:ns,html:list(api.filter(x=>(x.ns||'(global namespace)')===ns).map(type=>`<a href="@${knownRoutes.get(type.name)}"><code>${escape(type.name)}</code></a> (${escape(type.kind)})`))})),
  {id:'optional',title:'Optional Library',html:p('OpenGLShaderElement and ShaderDisplay are in MelonUI.Shaders. See <a href="@shaders">Optional Shader Controls</a> for usage and platform requirements. Platform rendering helpers are also exposed in the current core API, but normal applications do not need to use them directly.')}
]});

const routes=new Map(pages.map(page=>[page.slug,page.route??(page.slug==='introduction'?'':page.slug)]));
function href(from,target){if(!routes.has(target))throw new Error(`Unknown link ${target}`);return (path.posix.relative(routes.get(from.slug),routes.get(target))||'.')+'/index.html';}
const asset=(page,name)=>'../'.repeat((routes.get(page.slug)||'').split('/').filter(Boolean).length)+'assets/'+name;
const link=(page,target,title,attributes='')=>`<a href="${href(page,target)}" ${attributes}>${escape(title)}</a>`;
const navGroups=[
  ['Guides',pages.filter(x=>x.group==='Getting Started'&&x.slug!=='introduction').concat(pages.filter(x=>x.group==='Guides'))],
  ['MXML',pages.filter(x=>x.group==='MXML')],
  ['Controls',pages.filter(x=>x.group==='Controls')],
  ['Custom Controls',pages.filter(x=>x.group==='Custom Controls')],
  ['API Reference',pages.filter(x=>x.group==='Reference').concat(pages.filter(x=>x.group==='API'))]
];
function nav(page){return `<ul><li>${link(page,'introduction','Home',page.slug==='introduction'?'aria-current="page"':'')}</li></ul>`+navGroups.map(([title,items])=>`<details${items.includes(page)?' open':''}><summary>${escape(title)}</summary><ul>${items.map(item=>`<li>${link(page,item.slug,item.nav??item.title,item===page?'aria-current="page"':'')}</li>`).join('')}</ul></details>`).join('');}

// dist is generated output. Refuse to remove any other path or a linked directory.
if(path.resolve(out)!==path.resolve(root,'dist'))throw new Error('Unexpected output path');
const previous=lstatSync(out,{throwIfNoEntry:false});if(previous?.isSymbolicLink())throw new Error('Output directory must not be a symlink');
if(previous)rmSync(out,{recursive:true,force:true});
mkdirSync(path.join(out,'assets'),{recursive:true});
for(const name of ['style.css','app.js','favicon.svg'])cpSync(path.join(root,'assets',name),path.join(out,'assets',name));
mkdirSync(path.join(out,'examples'),{recursive:true});
for(const name of readdirSync(path.join(root,'examples')).filter(x=>/\.(cs|mxml)$/.test(x)&&!x.startsWith('Generated')&&x!=='Program.cs'))cpSync(path.join(root,'examples',name),path.join(out,'examples',name));
for(const page of pages){
  const toc=[];const ids=new Set(['content','page-title']);
  let body=page.sections.map(section=>{
    ids.add(section.id);toc.push({level:2,id:section.id,title:section.title});
    const html=section.html.replace(/<h3>(.*?)<\/h3>/g,(_,title)=>{let id=section.id+'-'+title.toLowerCase().replaceAll(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');while(ids.has(id))id+='-2';ids.add(id);toc.push({level:3,id,title});return `<h3 id="${id}">${title}</h3>`;});
    return `<section id="${section.id}"><h2>${escape(section.title)}</h2>${html}</section>`;
  }).join('');
  body=body.replace(/href="@([^"#]+)(#[^"]*)?"/g,(_,target,hash)=>`href="${href(page,target)}${hash??''}"`);
  const visible=pages.filter(x=>x.group!=='API');const index=visible.indexOf(page);
  const previousPage=index>0?visible[index-1]:null,nextPage=index>=0&&index<visible.length-1?visible[index+1]:null;
  const pagination=previousPage||nextPage?`<nav class="pagination" aria-label="Previous and next pages">${previousPage?link(page,previousPage.slug,'Previous: '+(previousPage.nav??previousPage.title)):''}${previousPage&&nextPage?' &lt;--- ---&gt; ':''}${nextPage?link(page,nextPage.slug,'Next: '+(nextPage.nav??nextPage.title)):''}</nav>`:'';
  const contents=`<ul><li><a href="#page-title">${escape(page.title)}</a><ul>${toc.map(item=>`<li class="toc-level-${item.level}"><a href="#${item.id}">${item.title}</a></li>`).join('')}</ul></li></ul>`;
  const html=`<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="description" content="${escape(page.summary)}"><title>${escape(page.title)} - MelonUI Docs</title><link rel="icon" href="${asset(page,'favicon.svg')}" type="image/svg+xml"><link rel="stylesheet" href="${asset(page,'style.css')}"><script src="${asset(page,'app.js')}" defer></script></head><body>
<a class="skip" href="#content">Skip to content</a><div class="mobile-bar"><button id="menu-toggle" type="button" aria-expanded="false" aria-controls="sidebar">Contents and Other Docs</button></div>
<aside class="sidebar" id="sidebar" aria-label="Documentation navigation"><h2>Contents</h2><nav aria-label="Contents">${contents}</nav><h2>Other Docs</h2><nav aria-label="Documentation">${nav(page)}</nav><button type="button" class="search-button" aria-label="Search documentation" data-search-index="${asset(page,'search-index.json')}">Search Docs <kbd>Ctrl K</kbd></button></aside>
<main class="content" id="content" tabindex="-1"><h1 id="page-title">${escape(page.title)}</h1><p>${escape(page.summary)}</p>${body}${pagination}</main>
<dialog id="search-dialog" aria-labelledby="search-title"><div class="search-head"><label id="search-title" for="search-input">Search Docs</label><button type="button" id="search-close" aria-label="Close search">Close</button></div><input id="search-input" type="search" placeholder="Enter a control name or topic" autocomplete="off"><div id="search-results" role="status" aria-live="polite"><p>Enter a control name or topic to search the docs.</p></div><p class="search-help">Use Up/Down to select a result, Enter to open it, or Escape to close.</p></dialog><div id="toast" role="status" aria-live="polite"></div></body></html>`;
  const folder=path.join(out,routes.get(page.slug));mkdirSync(folder,{recursive:true});writeFileSync(path.join(folder,'index.html'),html);
}
const plain=html=>html.replace(/<[^>]+>/g,' ').replace(/&(?:lt|gt|amp|quot|#39);/g,' ').replace(/\s+/g,' ').trim();
writeFileSync(path.join(out,'assets/search-index.json'),JSON.stringify(pages.map(page=>({title:page.nav??page.title,group:page.group,summary:page.summary,route:(routes.get(page.slug)?routes.get(page.slug)+'/':'')+'index.html',text:plain(page.sections.map(s=>s.title+' '+s.html).join(' '))}))));
writeFileSync(path.join(out,'.nojekyll'),'');
writeFileSync(path.join(root,'content/site-manifest.json'),JSON.stringify({pages:pages.map(page=>({slug:page.slug,route:routes.get(page.slug),title:page.title})),controls:controls.length,apiTypes:api.length},null,2));
console.log(`Built ${pages.length} static pages, ${controls.length} control examples and ${api.length} public core types.`);
