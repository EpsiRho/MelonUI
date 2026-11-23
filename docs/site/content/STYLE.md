# Documentation Writing and Layout

The reference is the author's Melon documentation at https://melon.docs.epsirho.com/.
This is a style reference; MelonUI API details still come from the current MelonUI source.
Do not copy old framework versions, server endpoints or unfinished features into these docs.

## Writing

- Start with what the class or feature does, then explain how it is used.
- Use direct explanations such as "You can", "For example", and "This method".
- Tutorials use numbered steps, followed by an Understanding the Code section where useful.
- Describe properties and methods as lists with bold names and an explanation after the colon.
- Use Notes for limitations and lifetime details that affect the example.
- Keep API names accurate, even when their current spelling is unusual, such as ScaleAmmount.
- Link to related guides in normal sentences or lists. Avoid promotional headings, taglines,
  decorative cards, version badges and repeated validation claims inside articles.
- Keep C# and MXML examples visible. Follow examples with text output where it is useful.
- Explain constructor/factory or backend setup before relying on it in MXML.
- Keep source snippets and output examples connected to the executable example runner.

## Layout

The layout follows the same structure as the Melon site:

- A fixed left sidebar with Contents for the current page, then Other Docs.
- Collapsible groups under Other Docs and a plain Home link.
- A single article column, without a top navigation bar or a right-hand contents column.
- Dark backgrounds (#121212 for the page, #1e1e1e for the sidebar), white text, and green
  links (#1ae113). Text uses Segoe UI at 16px with a 1.6 line height.
- Plain headings with divider lines, lists, bordered code/output blocks, and full-width tables.
- Tables use the same green header treatment as the Melon API reference.
- On small screens the sidebar becomes a compact menu. Long code and text output scroll inside
  their blocks rather than making the whole page wider.

The original layout was checked on the Melon home page, plugin introduction and tutorials,
Authorization API reference, Shuffling guide, and Melon Scanner architecture page.
