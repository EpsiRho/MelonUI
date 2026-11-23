import {readFileSync, readdirSync, existsSync} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../dist/',import.meta.url));
const pages=[];function walk(dir){for(const entry of readdirSync(dir,{withFileTypes:true})){const file=path.join(dir,entry.name);if(entry.isDirectory())walk(file);else if(file.endsWith('.html'))pages.push(file);}}walk(root);
const failures=[];let links=0;
for(const file of pages){
  const html=readFileSync(file,'utf8');const ids=[...html.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);
  if(new Set(ids).size!==ids.length)failures.push(`Duplicate ids: ${file}`);
  for(const required of ['<title>','<meta name="description"','<h1 id="page-title">','Skip to content','aria-label="Documentation"','<h2>Contents</h2>','<h2>Other Docs</h2>'])if(!html.includes(required))failures.push(`Missing ${required}: ${file}`);
  if(/href="@/.test(html))failures.push(`Unresolved doc link: ${file}`);
  for(const match of html.matchAll(/\b(?:href|src)="([^"]+)"/g)){
    const value=match[1];if(/^(?:https?:|mailto:|data:)/.test(value))continue;
    links++;const [raw,hash]=value.split('#');const target=raw?path.resolve(path.dirname(file),decodeURIComponent(raw)):file;
    if(!target.startsWith(root)){failures.push(`Link escapes static root: ${value}`);continue;}
    if(!existsSync(target)){failures.push(`Missing ${value} from ${path.relative(root,file)}`);continue;}
    if(hash&&target.endsWith('.html')&&!readFileSync(target,'utf8').includes(`id="${hash}"`))failures.push(`Missing anchor ${value}`);
  }
}
const api=JSON.parse(readFileSync(new URL('../content/api.json',import.meta.url)));
const manifest=JSON.parse(readFileSync(new URL('../content/site-manifest.json',import.meta.url)));
for(const type of api)if(!manifest.pages.some(p=>p.title===type.name))failures.push(`Undocumented public type: ${type.name}`);
if(failures.length){console.error(failures.join('\n'));process.exit(1);}
console.log(`PASS: ${pages.length} static pages, ${links} local links/assets/anchors, unique IDs, accessible page landmarks and all ${api.length} public core types documented.`);
