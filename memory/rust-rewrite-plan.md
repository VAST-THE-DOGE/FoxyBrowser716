---
name: rust-rewrite-plan
description: FoxyBrowser Rust rewrite direction (2026-07): cef-rs + Slint, spike-first, new branch; OSR chosen (airspace is a hard constraint); clean-slate data
metadata:
  type: project
---

Decided direction (planning stage as of 2026-07-16): skip the Avalonia port, rewrite FoxyBrowser in
Rust, spike-first, on a **new branch** off v0.8.0. Stack: **cef-rs** (Rust binding to CEF's C API,
tracks CEF ~150) + **Slint**. This supersedes the Proton path in [[linux-support-state]] as the
long-term answer.

**Slint re-confirmed 2026-07-26** after reviewing alternatives, on two grounds: its DSL is
structurally XAML-shaped (declarative element tree, property animations, states/transitions), which
makes porting the existing UI a mechanical translation rather than a rewrite; and it imports wgpu
textures for the CEF-OSR pipeline. Runners-up considered and rejected:
- **Iced** (Rust-native, `shader` widget for the OSR texture, proven by COSMIC): no declarative
  animation story, which is exactly what this UI leans on.
- **Xilem** (Linebender, Vello/Masonry/parley/AccessKit): best a11y and text of the field, Apache/MIT
  licensed, but pre-1.0 with churning API, no animation system yet, and **the blocker is external
  GPU textures**: Vello's image API is CPU-pixel-oriented, so feeding it a CEF shared texture may
  mean a readback per frame, defeating accelerated OSR. Verify before ever reconsidering.
- User is fine with experimental deps as long as they're actively maintained; Xilem qualifies on
  maintenance, it's the API churn plus the texture question that rule it out for *this* project.
  **Re-evaluate Xilem in ~2027-07** for the user's other project (Rust migration ~1yr out, needs
  wasm + mobile), where Slint currently wins on both but Xilem is closing.

Open item: **check Slint's license terms** (GPLv3 vs royalty-free conditions vs paid) before
distributing binaries. Xilem's Apache/MIT is friction-free by comparison.

**Hard constraints (user, 2026-07-26):**
- **Zero airspace.** Overlays must draw over web content. Non-negotiable, this drove the rewrite.
- **Input latency budget: >40ms is noticeable, below that is fine** (5 vs 12ms is indistinguishable
  to them, they don't game in the browser). One extra frame of OSR latency is acceptable.
- **Keep the existing UI.** Port it, don't redesign. A lot of hand-tuned custom styling (color
  animations, corner radii, etc.) that must carry over by translating the XAML to the Rust UI
  library's equivalent. Pixel-perfect is not required; "looks like FoxyBrowser" is.
- **Extensions: manual/native implementations are acceptable** where the extension API can't reach,
  explicitly including tab enumeration. This softens Band 3 below: the *capability* can be built
  natively even though the chrome.tabs API is out.
- **Data: clean slate.** No reading of C# on-disk formats (settings/profiles/backups/instance cache),
  no converter planned.
- **Audience: one user (the author) plus one friend who moved to Linux.** So native Linux is a real
  target, not hypothetical, and the Chromium-update tax is acknowledged but deferred: auto-update is
  a someday, not a blocker.
- **No web-UI-in-a-native-wrapper, ever.** See [[no-webview-wrapped-apps]] in the personal memory
  dir. cef-rs is only a Rust binding to CEF and pulls in none of the Tauri framework.

**adblock-rust: dropped from scope.** Adblocking is tested as an *extension* (uBO Lite), not
reimplemented natively. Brave's adblock-rust stays a maybe-later idea, not a workstream.

**Extension scope bands:** Band 1 (page-world: blockers, Dark Reader) = full support target. Band 2
(popup/shortcut extensions, e.g. Global Speed) = verified-by-spike. Band 3 (tab managers/session/
tab-group extensions) = OUT OF SCOPE, because CEF maps chrome.tabs onto browser *windows* (each
embedded browser = 1-tab window); fixing that means forking Chromium (~200GB disk, multi-hour builds,
continuous upstream merges), rejected as impractical on the user's 32GB/7900X as a workflow.

**OSR-vs-windowed: settled, OSR wins.** Windowed Chrome-style CEF gets officially-supported
extensions but reintroduces native-child airspace, which the zero-airspace constraint forbids
outright. So: Chrome-style OSR (CEF issue #3293, Ozone-based), where extension support is officially
gray/case-by-case. cef-rs ships an accelerated OSR example (shared textures: D3D11/IOSurface/DMA-BUF
into wgpu) and Slint >=1.12 imports wgpu textures, so CEF-OSR -> wgpu -> Slint scene with overlays is
coherent. Fallback for extensions that don't survive OSR: reimplement natively via user-script
injection, which CEF does regardless of the extension system (Dark Reader is injected CSS/JS, Global
Speed is media-element JS plus a native shortcut layer, uBO is a filter engine).

**How to apply, spike checklist** (live app the user will test):
- **Go/no-go number: input latency and frame pacing under accelerated OSR.** Budget is generous
  (<40ms added, roughly one extra frame), so this is a sanity check rather than the crux it looked
  like before the budget was known. Still measure before building on top.
- Run the extension suite under OSR and enumerate exactly what survives: uBO Lite, Dark Reader,
  Global Speed, plus a homemade chrome.tabs-probe extension. Windowed only as a reference run.
- OSR taxes to verify, all self-implemented there: IME, accessibility, drag-out from web content,
  PDF viewer, print, spellcheck, context menus, DevTools.
- Slint: build the *hardest* UI first (widget home page with edit overlays, tab tear-out across
  windows), not a toolbar. Separately confirm look parity for composition acrylic/blur and Win2D
  effects, which Slint does not provide: either a DWM backdrop attribute or rendered in wgpu.
- Pin a known-good CEF version (shared-texture regressions exist, e.g. null handle on CEF 143
  Windows). Note we now own Chromium updates: ~4-week cadence, ~200MB of binaries per ship, and an
  ignored update becomes unpatched CVEs rather than feature lag (version-check plumbing already
  exists in `AppServer` / `VersionInfo`).
- Global Speed manifest, for reference: MV3; storage, tabCapture, webNavigation, scripting,
  offscreen, userScripts, contextMenus; popup; 19 commands; content scripts all_frames in
  isolated+MAIN worlds.
