// Run before paint; storage may be disabled in private/restricted browser contexts.
try {
  const choice = localStorage.getItem('melonui-theme');
  if (choice === 'light' || choice === 'dark') document.documentElement.dataset.theme = choice;
} catch {}
