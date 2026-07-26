# Rust rewrite: proof of concept

**Goal:** prove the three things that can kill the [Rust rewrite](../../memory/rust-rewrite-plan.md),
and nothing else. Those are:

1. CEF renders offscreen into a GPU texture that Slint can composite, at acceptable latency.
2. Slint UI draws *over* live web content (the zero-airspace requirement that drove the rewrite).
3. Extensions load and function under Chrome-style OSR.

Everything else is deliberately out. This is a spike, not a foundation. Expect to throw most of it
away.

**Shape:** one window, one CEF browser, hardcoded to `music.youtube.com`, no tab strip, no settings,
no chrome beyond what a milestone needs. YouTube Music is the target because it exercises both test
extensions at once: uBlock has ads to block, Global Speed has a media element to retime.

**Where:** new branch off `v0.8.0`, cargo workspace in **`rust-temp/`**, so the C# solution is
untouched and keeps building. The directory name is deliberate: this code is scaffolding for
answering questions, not the start of the real app.

**After the PoC, this does not get copy-pasted.** Once the questions above are answered, the code
moves to its real home as a *review* pass, not a move: every piece gets the "can we do this better"
question before it survives. Expect the module layout, the CEF lifetime handling, and the input
plumbing to all be redesigned once the constraints are actually known rather than guessed. The
findings are the asset here; the code is a means to them.

---

## M0. Environment and API audit

Cheapest milestone, and it can veto the plan before any code is written.

- [ ] Rust toolchain + cef-rs, with the CEF binary distribution download working.
- [ ] Run cef-rs's own examples unmodified, **especially the accelerated OSR example**. If that
      doesn't run on this machine, stop and solve that before touching FoxyBrowser code.
- [ ] Record the exact CEF version that worked and pin it. Shared-texture regressions are real
      (null handle on CEF 143 Windows).
- [ ] **Audit cef-rs API coverage** for the three things this PoC depends on. Anything unwrapped
      means hand-written unsafe FFI, and it is much better to know that now:
  - [ ] accelerated OSR paint callback (`OnAcceleratedPaint` and the shared-texture handle)
  - [ ] extension loading (`CefRequestContext::LoadExtension` or the Chrome-runtime equivalent)
  - [ ] input event senders (`SendMouseClickEvent`, `SendMouseMoveEvent`, `SendMouseWheelEvent`,
        `SendKeyEvent`)

**Gate:** any of the three missing from cef-rs → decide whether to write the FFI or reconsider the
binding, before proceeding.

## M1. Slint window displays the CEF texture

The highest-risk milestone. If the texture handoff doesn't work, the architecture is dead, and this
is where that gets discovered.

- [ ] Slint window, single full-bleed surface, no chrome.
- [ ] CEF OSR shared texture (D3D11 on Windows) imported into Slint via its wgpu texture API.
- [ ] Resize handling: window resize propagates to CEF's view rect and the texture is re-imported.
- [ ] Input forwarding from Slint into CEF: mouse move, click, wheel, and keyboard. This is
      deceptively large and is where OSR's tax first shows up. Modifier keys and focus included.
- [ ] Load `music.youtube.com` on startup.

**Deliverable:** YouTube Music loads, plays, and is clickable, scrollable, and typeable (the search
box is the test).

**Gate:** no zero-copy texture path → the cef-rs + Slint architecture is wrong. Fall back to
reconsidering windowed CEF, accepting the airspace consequences, or a different embedding entirely.

## M2. Overlay proof

Should be nearly free once M1 works. That it is nearly free *is* the proof.

- [ ] Draw a Slint overlay above the web content: a translucent rounded panel with an animated
      hover colour, plus something following the cursor.
- [ ] Confirm no z-order fighting, no flicker, no clipping at the web-content boundary.
- [ ] Confirm it holds up over *video playback*, not just a static page.

**Deliverable:** screenshot of UI compositing cleanly over a playing video.

## M3. Latency check

- [ ] Side-by-side against real Chrome/Edge on the same page and machine.
- [ ] Phone slow-mo (240fps) of click-to-visual-change in both, or a test page logging
      `performance.now()` on pointerdown. For a 40ms budget this is precise enough; no special
      hardware needed.
- [ ] Subjective pass: scroll feel, typing echo, video smoothness.

**Deliverable:** a number, or a clear "indistinguishable from Chrome" verdict, written down.

**Gate:** budget is under 40ms added, roughly one extra frame. Over that, investigate frame pacing
and vsync alignment before concluding anything.

## M4. Extensions (the actual point)

- [ ] Load unpacked uBlock Origin Lite (MV3) and Global Speed (MV3) under Chrome-style OSR.
- [ ] uBO Lite on YouTube Music:
  - [ ] pre-roll and mid-roll ads blocked
  - [ ] cosmetic filtering applied (banners gone, not just requests failing)
  - [ ] its action popup opens and renders
- [ ] Global Speed on YouTube Music:
  - [ ] playback rate changes from its popup
  - [ ] its keyboard commands fire (19 `chrome.commands`)
  - [ ] it survives navigation between tracks (`webNavigation` + MAIN-world content script)
  - [ ] note which of `tabCapture` / `offscreen` / `userScripts` work or fail

**Deliverable:** the capability table below, filled in. That table is the real output of this PoC.

**Gate:** extensions entirely non-functional under Chrome-style OSR is **not** a project failure. The
pre-agreed fallback is native re-implementation via user-script injection, which CEF supports
regardless of the extension system. What matters is knowing exactly which capabilities survive.

## M5. chrome.tabs probe

- [ ] Homemade extension that calls `chrome.tabs.query` and logs the result.
- [ ] Confirm and document precisely what CEF reports (expected: one window per browser, so it sees
      a single tab).

This exists to pin down the known limitation so the native tab-enumeration fallback can be designed
against real behaviour instead of an assumption.

---

## Explicitly out of scope

Multiple tabs, tab groups, profiles and instances, settings, the widget home page, downloads,
bookmarks, theming, the AI panel, packaging, auto-update, and Linux. Windows only for now, but avoid
gratuitously Windows-only assumptions in the texture path where portability is free.

## Results table

Fill this in as M4 and M5 land. This is what gets carried back into the rewrite plan.

| Capability | Extension | Works under OSR | Notes |
|---|---|---|---|
| network blocking | uBO Lite | | |
| cosmetic filtering | uBO Lite | | |
| action popup UI | uBO Lite | | |
| content script (isolated) | Global Speed | | |
| content script (MAIN) | Global Speed | | |
| `chrome.commands` | Global Speed | | |
| `chrome.storage` | Global Speed | | |
| `webNavigation` | Global Speed | | |
| `tabCapture` | Global Speed | | |
| `offscreen` | Global Speed | | |
| `contextMenus` | Global Speed | | |
| `chrome.tabs.query` | probe | | expected: single tab |
