# C# composition and lifecycle

Updated for MelonUI 1.0, October 26, 2026. These are the current APIs in this checkout.

## A window owns one UI loop

Construct a `ConsoleWindowManager`, configure it and build its tree, then await `RunAsync`.
Construction does not access the terminal. `RunAsync` owns input, posted work, recurring updates
and frame composition on one dedicated thread. It completes after stop/cancellation and terminal
cleanup. `Run` is the blocking convenience wrapper; do not start separate render/input/dispatcher threads.

`Stop` is safe from background tasks and UI callbacks. Await the run task before final disposal.
`await using` stops and joins an active run and disposes its attached roots recursively. Calling
`DisposeAsync` inside an active UI callback is rejected: call `Stop` there and dispose after awaiting
the run. A stopped window can run again until disposed. Concurrent runs are rejected.

The default maximum cadence is 60 FPS (`TargetFrameRate`, 1–1000). Cached unchanged pages do not
present duplicate frames; Updating stays recurring and FrameRendered observes actual presentations.
Posted actions execute once;
they do not change the frame-rate limit. `Updating` is a synchronous recurring UI callback with
`Elapsed` since the previous update. `FrameRendered` observes completed frames. Keep callbacks short.

Running requires interactive input/output. Constructing trees and capturing a frame with explicit
dimensions works without a terminal. The default backend restores output encoding, cursor state
and captured Windows title on exit. The Windows native renderer also restores its console mode
and input/output code pages. Unsupported title setting is ignored. Other-platform terminal behavior
still needs hands-on validation.

## Background work

Input, `Updating`, `FrameRendered` and dispatched callbacks execute on the UI owner. Change the tree
directly there. Once running, use `Post` or `InvokeAsync` from server tasks, watchers or plugin callbacks.

```csharp
var run = window.RunAsync(cancellationToken);
try
{
    var result = await ReadLibrarySummaryAsync(cancellationToken); // Application I/O stays outside UI work.
    await window.InvokeAsync(() => status.Text = result);
}
finally
{
    window.Stop();
    await run;
}
```

`Post(Action)` queues one action and returns immediately, even when called on the UI owner.
`InvokeAsync(Action)` completes when the action finishes, propagates its exception, and executes
inline when already on the active UI owner. Queueing is accepted only during a running window
that is not stopping. Pending awaited work is cancelled when the loop exits; unexecuted posted work
is discarded. Application services must stop their producers as part of application shutdown.

Generated/bound property setters and collection mutations check thread access for attached elements.
`CheckAccess()` lets shared code choose between an immediate change and dispatch. Custom controls must
call protected `VerifyAccess()` from their own mutable APIs and `Invalidate()` after visual changes.
Control-specific mutable lists/fields do not all enforce access; use the same UI-owner rule
for those. Unattached trees are reusable, but are not intended for simultaneous mutation by multiple threads.

## Ownership and collections

`window.Elements` is a managed root collection. Every element has a managed `Children` collection,
including `MUIPage`. Collection initializers, `Add`, `Insert`, replacement, `Remove`, `RemoveAt` and
`Clear` use the same ownership path. `Parent` and `ParentWindow` are readable and assigned internally.

Adding a subtree updates all descendant window references; late-added descendants work the same way.
One element may have only one owner. Duplicate attachment, cycles, disposed elements and invalid
replacement are rejected before changing either tree. Detach before moving an element.

```csharp
var page = new MUIPage { Name = "Settings", Width = "100%", Height = "100%" };
page.Children.Add(new TextBlock { Name = "Heading", Text = "Settings", Width = "30", Height = "3" });
window.Elements.Add(page);

page.Remove();                  // Detach; keep the instance in your cache.
window.Elements.Add(page);      // Reattach the same state.
page.Dispose();                 // Final; recursively dispose its children.
```

Removing or clearing detaches without disposing. The application owns detached cached objects and
must eventually dispose them. Hiding (`IsVisible = false`) retains ownership/state. Disposal is final
and idempotent; a disposed instance cannot be attached again. The window disposes only currently attached
roots. Detach clears stale window focus/cache state and suspends event bindings registered through
`SetBinding`; reattachment resumes those bindings once. Direct application event subscriptions need
custom lifetime hooks.

`RequestRemove()` queues the element's removal on its active UI loop. It replaces `RenderThreadDeleteMe`.
Outside a run it removes immediately; an unattached element is a no-op. A request belongs to the current
attachment: it is ignored if that element or an ancestor has since been detached/reused. A stopping
window rejects new requests like other posted work. This operation does not dispose the element.

Grid helpers follow the same rules: `grid.AddElement(child, row, column)` or `grid.Children.Add(child)`
followed by `grid.SetCell(child, row, column)`. A direct add defaults to cell (0,0). Removal also clears
placement. Input routes to the focused child and bubbles to containers once. QueueContainer's queue/pop convenience methods route
through `Children`; `PopElement()` returns the detached element (null for an empty queue) for reuse or
explicit disposal. `ButtonPanel.Children` owns its buttons and
`Buttons` is a read-only snapshot. Its overflow menu is a single temporary registration.

## Lookup, focus and drawing order

`window.Find<TextBlock>("Status")` searches the whole tree; `page.Find<TextBlock>("Status")` searches
that page's descendants. Missing names return null, duplicate names in the chosen scope throw, and
type mismatches throw. `FindById` uses the immutable `UID`. Scope lookup to a page when names repeat.

Use `Focus(element)` instead of assigning `FocusedElement`. The target must be attached, visible, enabled and have visible content area;
a nonfocusable container selects an eligible descendant when it has one. Empty pages are valid.
Detach repairs stale focus. `IsFocused` and `FocusedElement` are read-only to applications. Focus no
longer changes Z order; use `BringToFront` / `SendToBack` explicitly. Those operations reorder siblings.
Tab/Shift+Tab traverses siblings by TabIndex then insertion order; Alt+arrows moves spatially.
The top modal contains focus and suppresses background shortcuts. See [the control guide](default-controls.md)
for input routing, composite controls and per-control keys.

Nested property/collection changes invalidate ancestors. Custom controls can explicitly call
`Invalidate()`. Plain pull bindings/animations may need caching disabled or explicit invalidation.
Notification-capable bindings dispatch invalidation and suspend subscriptions on detach.

`CaptureText(includeColor: false, width: 80, height: 24)` uses the normal frame composition, including
the final row. Explicit dimensions allow capture before running. While running, call it on the UI owner.
There is no built-in heading/status area or reserved two-row offset. Rendered headings and status are
ordinary controls; `Title` only changes the native console/terminal title. Secret TextBox values stay
masked in ordinary captures, including when revealed interactively; `includeSensitive: true` explicitly opts in.

## Popups and custom controls

`var popup = window.ShowPopup(content)` registers an ordinary root, brings it forward and focuses its
eligible content. `popup.Close()` detaches it for reuse and restores still-valid previous focus.
`popup.Dispose()` also permanently disposes its content. Close is idempotent and an old handle does
not remove a later attachment of that content. Explicit disposal remains final even after content reuse.
The window owns content that remains attached when the window is disposed. Registrations are modal
by default: focus/input stay inside the top visible, enabled modal, and unhandled Escape closes it.
Use `ShowPopup(content, isModal: false)` for a nonmodal registration. `IsOpen` reports registration state.

Custom controls can override `OnAttachedToWindow`, `OnDetachedFromWindow`, `OnChildAdded`,
`OnChildRemoved` and `DisposeCore`. Stop window-specific subscriptions/work on detach, resume on
attach, and permanently release resources in `DisposeCore`. Call base hooks when extending a container.
Keep hooks synchronous and avoid throwing from cleanup. Removal does not call `DisposeCore`.

Experimental shader controls now release their worker/native resources on final disposal. The GPU
renderer publishes completed pixel frames together with their dimensions. During startup the UI can
remain blank until a frame is ready; during resizing it clips the previous completed frame using that
frame's own stride. Resize requests are applied by the GPU thread before rendering the next frame.
`dotnet run --no-launch-profile --project MelonUIDemo -- --shader --smoke` checks real shader startup,
repeated resizing and shutdown in an interactive graphics-capable terminal. The GPU
controls are optional: compute display uses Windows/Direct3D; OpenGL needs a working graphics context.
Windows hardware smoke checks cover startup/resize/disposal. The audio-player prototype and decoder
dependencies are removed; Waveform displays host-provided peaks. Further platform coverage remains release work.

## Errors and migration

An `InvokeAsync` action's exception faults its task without stopping the window. Unexpected posted,
keyboard and frame callback errors reach `UnhandledException`. Set `Handled = true` only if the
application can safely continue; otherwise the run task faults after terminal cleanup. Disposal still
performs cleanup and propagates an earlier run failure when applicable. Do not use `async void` actions
to represent awaited background work.

| Old API | Replacement |
| --- | --- |
| `ManageConsole`, `StartDispatcherQueue` | One awaited `RunAsync(token)` / blocking `Run(token)` |
| Dispatcher dictionary/recurring queued actions | `Post` / `InvokeAsync` once, `Updating` for recurring work |
| `AddElement`, `RemoveElement` on CWM/page | `Elements` / `Children`; element `Remove()` |
| `RenderThreadDeleteMe = true` | `RequestRemove()` |
| `SetTitle`, rendered title/header flags | `Title` property, ordinary heading controls |
| `SetStatus`, status styling fields | A normal `TextBlock` named `Status` |
| `OptionsMenu.UseStatusBar` | Menu name draws locally; the application updates its status control |
| `IsManaged` and mutable worker state | Read-only `IsRunning`; lifecycle methods |
| Focus assignment / `MoveFocus(element)` | `Focus(element)` |
| `GetSubChildByName` and recursive parent utilities | Scoped `Find<T>`, `FindById`; managed ownership |
| `Screenshot` on CWM | `CaptureText` |
| `ConsoleImage` / ImageSharp references | Removed from the base library |

No compatibility layer is provided for the evolving old API. Melon's old in-process console integration
is a later step and has not been switched to this library in sprint one.

## Validation and remaining sprints

`MelonUI.Tests` is a dependency-free executable regression runner (run with `dotnet run --project MelonUI.Tests`;
it is not a `dotnet test` adapter). It exercises lifecycle/cancellation/errors, dispatch, ownership, late
attachment, detach/reuse, stale removal requests, cache invalidation, lookup, grid removal, popup/overflow
lifetime, bottom-row capture and a flat MXML compatibility smoke check. `MelonUIDemo --smoke` exercises the
real interactive terminal path. Debug/Release builds and Windows terminal checks are recorded with the
Melon server sprint plan.

The control/focus/RGB contract is in [default-controls.md](default-controls.md). The transactional
compiler, bindings, includes, actions and generator are documented in [mxml.md](mxml.md).
See [release-1.0.md](release-1.0.md) for packaging, migration and remaining platform limits.
