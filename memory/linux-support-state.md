---
name: linux-support-state
description: Where the Linux/Proton work lives and why it's stranded (as of 2026-07-16)
metadata:
  type: project
---

Linux support = run the Windows build under Proton/Wine (WinUI 3 + WebView2 have no native Linux
path). The work lives on `copilot/create-implementation-plan-linux-support` (5 commits, forked from
`master` 4ffb0dc, **not merged** into v0.8.0; local branch is 1 unpushed commit ahead). Contents:
`Program.cs` custom Main + `Bootstrap.Initialize` (WinAppSDK 1.8), `StaticData/PackageHelper.cs`
(`IsPackaged` guards), unpackaged publish profile `UnpackagedWin-x64.pubxml`, and `LINUX_PROTON.md`
(wine prefix + WebView2 install + Steam/Proton launch guide).

**Why:** v0.8.0 diverged (3 commits) after the fork; d6480e5 rewrote the same App.xaml.cs restart
logic the branch modified, so merging conflicts there. The restart path also spawns `powershell.exe`,
which doesn't exist in a Wine prefix.

**How to apply:** to resume Linux work, merge/rebase that branch onto v0.8.0 first, resolve the
restart-logic conflict keeping the infinite-restart guard, and replace the powershell relaunch with a
direct process exec. Commit 540b4f9 on v0.8.0 says "trying to fix for linux" but contains none of the
unpackaged work — ignore its message.
