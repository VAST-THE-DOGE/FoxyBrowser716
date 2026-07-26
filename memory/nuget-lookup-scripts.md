---
name: nuget-lookup-scripts
description: GetNugetDoc/GetNugetApi scripts + the WinUI projection-assembly caveat
metadata:
  type: reference
---

`Scripts/GetNugetDoc.ps1` (xml docs) and `Scripts/GetNugetApi.ps1` (reflected public surface) look up
NuGet package APIs without opening package sources. Both default `-Project FoxyBrowser716` and read
that project's `obj/project.assets.json`, so the project must be restored first. Ported from VastChat;
only the default project name changed.

**WinUI caveat (GetNugetApi `-Package` mode):** reflecting a WinUI package pulls in WinRT projection
assemblies like `Microsoft.Windows.SDK.NET` that are generated into the build output, not present in
the package cache's compile graph — so `AssemblyResolve` can't find them and the dump throws
"Could not load file or assembly 'Microsoft.Windows.SDK.NET…'". Fix: use `-Dll` against the built
copy under `FoxyBrowser716\bin\...\win-x64\`, where those projections sit as siblings and resolve.
`GetNugetDoc.ps1` is unaffected (it only reads xml, never loads assemblies). See [[foxybrowser-doc-generation]].
