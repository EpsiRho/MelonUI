# MelonUI Documentation

This folder contains the documentation website for MelonUI. The guides and control pages are built
as static HTML, so the site can be viewed locally or served by a static web host.

## Running the Site

You need Node.js 20 or later. From `docs/site` in the MelonUI repository, run:

```powershell
node scripts/build.mjs
node scripts/check.mjs
node scripts/serve.mjs
```

Open the URL printed by the server, normally http://127.0.0.1:4173. There are no npm dependencies
to install. The built pages are placed in `dist/`.

## Updating the Documentation

- **Guides:** `content/guides.mjs` imports the page definitions from `content/guides/`.
- **Controls:** `content/controls.mjs` contains the descriptions, properties, C# examples and MXML examples.
- **API Descriptions:** `content/reference.mjs` describes the supporting types.
- **Writing and Layout:** `content/STYLE.md` records the conventions used for the site.
- **Page Template:** `scripts/build.mjs` creates the sidebar, page links and reference lists.
- **Styles and Search:** `assets/style.css` and `assets/app.js` contain the presentation and browser features.

Rebuild the site after editing these files. `scripts/check.mjs` checks local links, anchors, page
structure and coverage of the public core types. C# and MXML examples are visible together rather
than being hidden behind tabs. Contents and Other Docs remain available without JavaScript.
JavaScript adds copy buttons, search and the compact mobile menu.

## Updating Code Examples

The example runner requires the .NET 10 SDK and a checkout of MelonUI. From this folder, run:

```powershell
node scripts/prepare-examples.mjs
dotnet run --project examples/DocsExamples.csproj -c Release -- .
node scripts/build.mjs
node scripts/check.mjs
```

The runner compiles the control examples, compiles each MXML page with its required setup, checks
bindings and reloads, and updates `content/api.json` and `content/previews.json`. It also checks the
custom Badge control and its generated properties. FilePicker's directory caption is changed to
`/demo` in its output example so it does not contain a machine-specific path.

If the site source is outside the MelonUI repository, add
`-p:MelonUIRoot=D:/Documents/GitHub/MelonUI/` to `dotnet run`, adjusting the path as needed.
Normal documentation builds can use the committed API and output files without rebuilding .NET.

## Static Output

To host the site yourself, serve the contents of `dist/`. Each page has its own HTML file and uses
relative links, including from a subfolder. No application server or SPA fallback is needed.
The output includes `.nojekyll` for GitHub Pages.

The site is kept local. These scripts do not upload or deploy anything. The unused
`.openai/hosting.json` file only records the previously provisioned, unpublished private project.
The library and these docs use MIT; see LICENSE and the repository dependency notices.
