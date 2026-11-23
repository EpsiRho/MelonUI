# Changelog

## 1.0.0

The first real release. Most of MelonUI got rewritten for this one.

- MXML: pages can be written in markup now, with bindings, events, includes, and errors that tell
  you the line and column. A failed reload keeps the last page that worked.
- Bindings: the `[Binding]` generator ships in the package, so custom controls just work.
- Windows: one owner per window, `Post`/`InvokeAsync` for touching the UI from other threads, and
  proper cleanup when things close.
- Focus: Tab, arrow key, and popup focus all work the way you'd expect.
- Controls: fixed up the default controls, added RGB editing, and passwords are hidden in captures.
- Rendering: only redraws when something changed, and Unicode, layout, and transparency are fixed.
- Shaders: moved into their own optional package, MelonUI.Shaders.
- Removed the old image and audio experiments.
