# Default controls and keyboard contract

Updated for 1.0, October 26, 2026. The supported C# control surface is described here;
the [MXML contract](mxml.md) covers markup, generated bindings, events and actions.

## Try the controls

```powershell
dotnet run --no-launch-profile --project MelonUIDemo -- --controls
dotnet run --no-launch-profile --project MelonUIDemo -- --controls --smoke
dotnet run --project MelonUI.Tests
# Optional Windows/Direct3D hardware regression:
dotnet run --project MelonUI.Tests -- --gpu
```

The gallery fits a normal terminal and shows more content at 120 columns by 40 rows.
F2 opens an RGB popup, F3 switches renderers, and Escape closes a modal or exits.
The smoke mode exercises RGB commit/cancel, shrinking/restoring the layout, password
capture, renderer switching and clean shutdown. It does not replace human usability testing.

## Focus and input

- Tab and Shift+Tab cycle in sibling `TabIndex` order, then insertion order. The traversal
  is depth first; `TabIndex` orders siblings rather than providing one global numeric order.
- Alt+arrow keys move spatially using rendered absolute coordinates. Alt+backtick cycles
  focus (Shift reverses). Ordinary arrows belong to the focused control.
- Hidden, disabled, detached, fully clipped and zero-content-size elements are skipped.
  Descendants inherit ancestor visibility/enabled state. Noninteractive defaults opt out.
- `window.Focus(element)` accepts an eligible attached element or a container with eligible
  descendants. Focus never changes Z order. QueueContainer exposes its active card only;
  ButtonPanel is one composite focus stop and owns the selection among its buttons.
- Input runs once on the focused control, then bubbles through ancestors until handled.
  Unhandled input reaches window shortcuts. `EnableGlobalControls` disables those shortcuts;
  `EnableSystemFocusControls` controls built-in navigation. Neither disables ordinary editing.
- `ShowPopup(content)` is modal by default. Tab/spatial navigation and input stay inside
  the top enabled, visible modal; background window shortcuts are suppressed. Unhandled Escape
  closes it. Closing, hiding or disabling a modal restores eligible previous focus. Nested
  modals work the same way. Use `isModal: false` for a nonmodal registration.
- `PopupHandle.IsOpen` reports whether that registration still owns its attachment.
  `Close` detaches for reuse; `Dispose` also permanently destroys the content.

Custom controls override `TryHandleKey(ConsoleKeyInfo)` and return true only when consumed.
Call `base.TryHandleKey` to honor registered control-specific callbacks. `HandleKey` remains
a convenience wrapper for direct calls. Container input is no longer broadcast to all children.
KeyboardControl wildcard predicates receive each key without rewriting their configured `Key`;
the predicate decides which modifiers it accepts. Ordinary registrations match modifiers exactly.

## Control surface

| Control | Supported behavior and keys |
| --- | --- |
| Button | Configured shortcut, Enter and Space activate once; enabled/locked state is respected. `PerformClick()` is the programmatic action. Parameterless construction uses Enter. |
| ButtonPanel / OverflowMenu | Left/Right select a visible action, Enter/Space activate, backtick toggles overflow. Overflow is a modal, fits the viewport, and restores panel focus. Removed, disabled, hidden or disposed source actions cannot be invoked through a stale popup. |
| OptionsMenu / MenuItem | Empty state; dynamic lists; disabled actions; Up/Down wrapping; Home/End; PageUp/PageDown; Enter/Right activate; typing a letter/digit cycles matching prefixes. `SelectedIndex` is -1 for an empty list. |
| TextBox | Single/multiline input, horizontal/vertical scrolling, Home/End, Delete/Backspace, grapheme-aware cursor/deletion, `ReadOnly`, `MaxLength` (UTF-16 units). Enter submits a single line; multiline Enter inserts a newline and Shift+Enter submits. `OnTextChanged` reports user edits and `Clear`, not programmatic `Text` assignment. No selection, clipboard or autocomplete API is supplied. |
| TextBox secrets | `HideCharacters`, Ctrl+R reveal while focused, automatic masking on focus loss. Normal `CaptureText` masks even a revealed field; `includeSensitive: true` explicitly opts in. Applications remain responsible for secret lifetime and logging. |
| ColorPicker / RgbColorPicker | Three RGB channels, original/live swatches, hex input API and explicit commit/cancel. See below. Alpha is preserved rather than edited. |
| FilePicker | File, directory or combined selection; extension filter; no process-CWD mutation. Up/Down/Home/End/Page keys select; typing cycles prefixes; Enter/Right opens a directory; Enter selects a file; Space selects a directory; F2 selects the current directory; Backspace/Left goes up; F5 refreshes. Escape invokes `Cancelled` when subscribed, otherwise modal Escape closes. Errors and disappeared entries are handled locally. |
| TextBlock | Wrapping, long words, blank lines, nine-way text alignment, `SizeBasedOnText`, `[Color(r,g,b)]`/`[ColorReset]`, ANSI SGR colors (16/256/true color). It does not execute terminal escape commands in its text. |
| ProgressBar | All existing styles, determinate/indeterminate display, optional text/percentage and time-based `AnimateProgress`. Display clamps invalid values without writing them back to bound state. |
| WorkLog / WorkItem | Snapshot rendering of jobs, counts/progress/status/elapsed time, `FollowRunning`, explicit status transitions. Waiting jobs do not start their stopwatch; completing/erroring stops it. Rendering does not swallow job errors. |
| Waveform | Display-only supplied peak amplitudes (`float[]`), resampling, scale and progress highlight. No decoding, playback or audio-device ownership. Signed peaks are displayed as magnitudes; nonfinite samples are ignored. |
| Line | Shared layout, bounded endpoints, reverse/horizontal/vertical/point lines, antialiasing, intensity/softness and optional double width. |
| FPSCounter | Automatically subscribes on attach, unsubscribes on detach, measures actual presented frames. Do not manually subscribe for normal C# use. Older duplicate frame subscriptions are deduplicated. |
| GridContainer | Validated per-row column counts, remainder-cell sizing, `SetCell`, child layout constraints, timed open/close animation. Children keep their requested sizes; removal clears placement. |
| QueueContainer | Managed queued children, active-card focus/input, bounded directional previews, detach/reuse through `PopElement`. No mutation of child public size/focus settings. |
| ShaderDisplay | Optional Windows/Direct3D compute display; lazy allocation, working gradient/default dispatch, resize and disposal. `UpdateShader<T>(texture => shader)` supplies a real ComputeSharp shader factory. Unsupported capabilities display `ErrorMessage`. |
| OpenGLShaderElement | Optional OpenGL worker; complete frame publication, startup/resize clipping and joined disposal. Requires a working graphics environment. It is not an input focus target. |

All defaults handle empty lists/data and tiny/clipped layouts. Min/max height uses the vertical
extent; explicit maxima, alignment and decimal percentages are honored. Cached descendants reflow
on viewport changes. Transparent controls preserve their underlying background; alpha is composited.
UTF-16 pairs are emitted together in screenshots and platform renderers; Linux wide cells preserve
row indexing. Exact combining/ZWJ emoji shaping and display width still depend on the terminal/font
and the existing cell buffer; editing does not split those text elements.

## RGB editing and dialogs

`ColorPicker` retains the short name; `RgbColorPicker` is an explicit alias for the same control.
Up/Down selects a channel. Left/Right adjusts it with bounded key-repeat acceleration; Shift uses
steps of ten. Home/End sets 0/255. Digits enter a channel value. `TrySetHex("#12ABEF")` accepts six
hex digits. All edits clamp to byte range and preserve alpha.

```csharp
var picker = new RgbColorPicker
{
    Width = "60%", Height = "8", XYAlignment = Alignment.Centered
};
picker.BeginEdit(theme.Accent);
var dialog = window.ShowPopup(picker);
picker.ColorChanged += color => preview.Foreground = color;
picker.Confirmed += color => { theme.Accent = color; dialog.Dispose(); };
picker.Cancelled += color => { preview.Foreground = color; dialog.Dispose(); };
```

Enter calls `Commit`; Escape calls `Cancel`, which restores the baseline established by `BeginEdit`
or attachment. A successful commit becomes the new baseline. These methods emit events; the control
does not decide whether to remove itself. For asynchronous dialogs, retain the handle until the dialog
ends (do not let a method-local `using` dispose it immediately on return). The gallery shows explicit
handle ownership. Use ordinary UI fields/buttons around it for validation or application-specific flows.

## Threading, performance and removals

Mutate attached controls on the UI owner using `Post`/`InvokeAsync` from background code.
Plain lists, WorkItem state and control-specific configuration properties follow the same rule even
where setters do not enforce it. Do filesystem/network/scanner work outside UI callbacks. FilePicker's
local directory enumeration is synchronous: hosts browsing slow/network/very large folders should
provide their own asynchronous loading UX. It is a picker, not a sandbox or permission boundary.

Animations/live-list controls request continuous rendering; ordinary cached descendants are invalidated
when their layout changes. Notification-capable bindings dispatch invalidation and release subscriptions
with their UI lifetime. MXML and analyzer behavior are covered by compiler/package-consumer tests.

The audio-player prototype, playback managers, WaveFormer decoder, NAudio and ATL dependencies are
removed. Supply waveform samples from the host's audio service. The old no-op ShaderBase wrapper is
removed; use the typed compute factory. Shaders remain optional and are unnecessary for Melon's console.
Shaders now live in the optional MelonUI.Shaders package; OpenGL needs Init on the application main
thread before RunAsync. Local 1.0 packages and fresh-consumer checks are provided. Native macOS,
other terminal families/SSH and long-session profiling remain checks beyond the bounded reload soak.
See [release-1.0.md](release-1.0.md) for the exact platform/build boundaries.
