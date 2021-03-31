# WPF + Console App Dependency Publishing Bug

## Versions

**.NET SDK:** 5.0.201

**Windows:** 10 (20H2 / build 19042)

## Problem Description

I have a need to create an application that exposes itself as a console
application and also a WPF "Windows" application. In some circumstances the
console application may show UI from the WPF application. There are also
scenarios where the WPF application can be invoked directly (not via the console
app).

The problem occurs when trying to run the WPF application directly after
publishing the console application (of which the WPF application is a
dependency) as a self-contained deployment.

```text
 ---------------------                   -----------------
| Console Application |---depends-on--->| WPF Application |
 ---------------------                   -----------------
```

Both the console application and WPF application are targeting the Windows
specific TFM for .NET 5 (`net5.0-windows`).

## Reproduction Steps

1. Clone this repository
2. Publish the console application for x86 on Windows

```powershell
PS> dotnet publish .\cli\cli.csproj -r win-x86 -f net5.0-windows
```

3. Run the console application (works OK)

```powershell
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\cli.exe
Hello World!
# A window appears (close the window)
Goodbye World!
```

4. Run the WPF application (does not run)

```powershell
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\wpfapp.exe
# No window appears
```

One clue to the problem appears if you try to instead publish targeting .NET
Framework (for example `net472`).

```powershell
PS> dotnet publish .\cli\cli.csproj -r win-x86 -f net472
Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Program Files\dotnet\sdk\5.0.201\Sdks\Microsoft.NET.Sdk\targets\Microsoft.PackageDependencyResolution.targets(241,5): error NETSDK1047: Assets file 'C:\Users\<USER>\scratch\wpfconsole\wpfapp\obj\project.assets.json' doesn't have a target for 'net472/win7-x86'. Ensure that restore has run and that you have included 'net472' in the TargetFrameworks for your project. You may also need to include 'win7-x86' in your project's RuntimeIdentifiers. [C:\Users\<USER>\scratch\wpfconsole\wpfapp\wpfapp.csproj]
```

---

**BUG 1**

The runtime identifier is not being passed through to the child projects on
publish. You must update the child apps' project files to specify the RID, or at
least include the RID you want to use in a list of RIDs.

**Questions:**

- Should the chosen publish RID be passed through to child projects on publish?
- Should there be a build warning or error when publishing `net5.0*`, similar to
  how `net4*` behaves?

---

5. Open `wpfapp\wpfapp.csproj` and add the following `<RuntimeIdentifiers/>`
   line:

```diff
   <PropertyGroup>
     <OutputType>WinExe</OutputType>
     <TargetFrameworks>net472;net5.0-windows</TargetFrameworks>
+     <RuntimeIdentifiers>win-x86;win-x64</RuntimeIdentifiers>
    <UseWPF>true</UseWPF>
   </PropertyGroup>
```

6. Retry publishing the console application for `net472` (now works OK)

```powershell
PS> dotnet publish .\cli\cli.csproj -r win-x86 -f net472
PS> .\cli\bin\Debug\net472\win-x86\publish\cli.exe
Hello World!
# A window appears (close the window)
Goodbye World!
PS> .\cli\bin\Debug\net472\win-x86\publish\wpfapp.exe
# A window appears (close the window)
```

7. Try publishing the console application for `net5.0-windows` again (works ok!)

```powershell
PS> dotnet publish .\cli\cli.csproj -r win-x86 -f net5.0-windows
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\cli.exe
Hello World!
# A window appears (close the window)
Goodbye World!
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\wpfapp.exe
# A window appears (close the window)
```

There is also an interesting issue with publishing as a single file (with the
`-p:PublishSingleFile=true` argument).

8. Publish the console application for `net5.0-windows` as a single file:

```powershell
PS> dotnet publish .\cli\cli.csproj -r win-x86 -f net5.0-windows -p:PublishSingleFile=true
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\cli.exe
Hello World!
# A window appears (close the window)
Goodbye World!
PS> .\cli\bin\Debug\net5.0-windows\win-x86\publish\wpfapp.exe
# No window appears
```

---

**BUG 2 (?)**

It appears the CLR has been bundled in to the console application AppHost
(`cli.exe`, made in to a "SuperHost"), but the WPF application's AppHost
(`wpfapp.exe`) has not (it's not a "SuperHost").

**Questions:**

- Should each "entry" application in the dependency graph be published as a
  single file too (multiple copies of the CLR)?
- Include all entry executables in the SuperHost? (... given the executable
  doesn't actually work, and only serves as a library assembly.)
- Something else (dynamically linking to the other SuperHost)?
- Should a warning be emitted at the very least if this is not supported?

---
