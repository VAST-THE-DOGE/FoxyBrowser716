---
name: foxybrowser-doc-generation
description: How the Docs/api markdown reference is generated for FoxyBrowser716
metadata:
  type: reference
---

`Scripts/GenerateApiDocs.ps1` + root `docfx.json` generate a markdown API reference into `Docs/api/`
(gitignored). Post-processing strips `<a id>` anchors and the "Inherited Members" lists (the
Inheritance chain stays). The script stamps `Docs/api/_generated.txt` with UTC time + short commit —
check it before trusting a doc for recently-changed code.

Unlike VastChat, this is a **single project**, so none of the `DocfxBuild` linked-document workaround
is needed — docfx points straight at `FoxyBrowser716/FoxyBrowser716.csproj`. `docfx.json` passes
`Platform=x64` (the project has no AnyCPU). docfx loads and exports cleanly against the WinUI/CsWinRT
project with `allowCompilationErrors: true`.

One file per type, named `FoxyBrowser716.{Folder...}.{Class}.md`. Rerun the script after any pass
that changes public type shapes.
