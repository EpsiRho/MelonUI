document.documentElement.classList.add('js');
const $=(selector,root=document)=>root.querySelector(selector);
const $$=(selector,root=document)=>[...root.querySelectorAll(selector)];
function toast(message){const element=$('#toast');element.textContent=message;element.classList.add('visible');clearTimeout(toast.timer);toast.timer=setTimeout(()=>element.classList.remove('visible'),1800);}
$$('.copy').forEach(button=>button.addEventListener('click',async()=>{
  const code=$('code',button.closest('figure'));
  try{if(!navigator.clipboard?.writeText)throw new Error('Clipboard unavailable');await navigator.clipboard.writeText(code.textContent);toast('Code copied.');}
  catch{const selection=getSelection(),range=document.createRange();range.selectNodeContents(code);selection.removeAllRanges();selection.addRange(range);toast('Code selected. Press Ctrl+C or Command+C to copy.');}
}));
const toggle=$('#menu-toggle'),sidebar=$('#sidebar');
function closeMenu(){sidebar.classList.remove('open');document.body.classList.remove('nav-open');toggle.setAttribute('aria-expanded','false');$('main').inert=false;}
toggle.addEventListener('click',()=>{const open=sidebar.classList.toggle('open');document.body.classList.toggle('nav-open',open);toggle.setAttribute('aria-expanded',String(open));$('main').inert=open;});
sidebar.addEventListener('click',event=>{if(event.target.closest('a'))closeMenu();});
document.addEventListener('keydown',event=>{if(event.key==='Escape'&&toggle.getAttribute('aria-expanded')==='true'){closeMenu();toggle.focus();}});
matchMedia('(min-width:761px)').addEventListener('change',event=>{if(event.matches)closeMenu();});
const dialog=$('#search-dialog'),input=$('#search-input'),results=$('#search-results'),button=$('.search-button');
const indexURL=new URL(button.dataset.searchIndex,location.href),siteBase=new URL('../',indexURL);
let indexPromise,indexData,selected=-1;
const loadIndex=()=>indexPromise??=fetch(indexURL).then(response=>{if(!response.ok)throw new Error('Search index unavailable');return response.json();}).then(data=>indexData=data).catch(error=>{indexPromise=null;throw error;});
function selectResult(index){const links=$$('a',results);selected=index;links.forEach((link,i)=>link.toggleAttribute('data-selected',i===index));links[index]?.scrollIntoView({block:'nearest'});}
function renderResults(data){
  const query=input.value.toLowerCase().trim();results.replaceChildren();selected=-1;
  if(!query){const message=document.createElement('p');message.textContent='Enter a control name or topic to search the docs.';results.append(message);return;}
  const words=query.split(/\s+/);
  const ranked=data.map(page=>{const title=page.title.toLowerCase(),body=(page.summary+' '+page.text).toLowerCase();return{page,score:words.every(word=>title.includes(word)||body.includes(word))?words.reduce((sum,word)=>sum+(title===word?100:title.startsWith(word)?40:title.includes(word)?20:1),0):0};}).filter(x=>x.score).sort((a,b)=>b.score-a.score).slice(0,12);
  if(!ranked.length){const message=document.createElement('p');message.textContent=`No results found for “${input.value}”. Try the control name or a topic such as MXML.`;results.append(message);return;}
  for(const {page} of ranked){const link=document.createElement('a');link.href=new URL(page.route,siteBase).href;const title=document.createElement('strong'),description=document.createElement('small');title.textContent=page.title;description.textContent=page.summary;link.append(title,description);results.append(link);}
  selectResult(0);
}
async function openSearch(){closeMenu();if(!dialog.open)dialog.showModal();input.focus();try{renderResults(await loadIndex());}catch{results.replaceChildren();const message=document.createElement('p');message.textContent='The search index could not be loaded. You can still use Contents and Other Docs to find a page.';results.append(message);}}
button.addEventListener('click',openSearch);$('#search-close').addEventListener('click',()=>dialog.close());
input.addEventListener('input',()=>{if(indexData)renderResults(indexData);});
input.addEventListener('keydown',event=>{const links=$$('a',results);if(event.key==='ArrowDown'&&links.length){event.preventDefault();selectResult((selected+1)%links.length);}if(event.key==='ArrowUp'&&links.length){event.preventDefault();selectResult((selected+links.length-1)%links.length);}if(event.key==='Enter'&&links[selected]){event.preventDefault();links[selected].click();}});
document.addEventListener('keydown',event=>{if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='k'){event.preventDefault();openSearch();}});
