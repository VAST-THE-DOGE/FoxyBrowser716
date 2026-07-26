param(
    [string]$Package,
    [string]$Type,
    [string]$Member,
    [string]$Project = 'FoxyBrowser716',
    [string]$Dll
)
$ErrorActionPreference = 'Stop'
if (-not $Package -and -not $Dll) { throw 'Pass -Package (nuget lookup) or -Dll (any built assembly).' }

$repoRoot = Split-Path $PSScriptRoot -Parent

if ($Dll) {
    $mainDll = (Resolve-Path $Dll).Path
    $mainRel = Split-Path $mainDll -Leaf
    $libKey = $mainRel
    #C siblings in the output folder cover dependency loads for locally built assemblies
    $resolveMap = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($sibling in Get-ChildItem (Split-Path $mainDll) -Filter '*.dll') { $resolveMap[$sibling.BaseName] = $sibling.FullName }
}
else {
    $assetsPath = if (Test-Path $Project) { Join-Path $Project 'obj\project.assets.json' }
                  else { Join-Path $repoRoot "$Project\obj\project.assets.json" }
    if (-not (Test-Path $assetsPath)) { throw "No assets file at $assetsPath - restore the project first" }

    $assets = Get-Content $assetsPath -Raw | ConvertFrom-Json
    $libKey = $assets.libraries.PSObject.Properties.Name | Where-Object { $_ -like "$Package/*" } | Select-Object -First 1
    if (-not $libKey) {
        $near = $assets.libraries.PSObject.Properties.Name | Where-Object { $_ -match [regex]::Escape($Package) }
        if ($near) { throw "Package '$Package' not found. Similar: $($near -join ', ')" }
        throw "Package '$Package' not found in $assetsPath"
    }

    function Resolve-PkgDir([string]$key) {
        foreach ($root in $assets.packageFolders.PSObject.Properties.Name) {
            $candidate = Join-Path $root $key.ToLowerInvariant()
            if (Test-Path $candidate) { return $candidate }
        }
        return $null
    }

    #C map every compile-time dll in the dependency graph so AssemblyResolve can serve them
    $tfmTarget = ($assets.targets.PSObject.Properties | Select-Object -First 1).Value
    $resolveMap = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $mainDll = $null
    $mainRel = $null
    foreach ($entry in $tfmTarget.PSObject.Properties) {
        $compile = $entry.Value.compile
        if (-not $compile) { continue }
        $dir = Resolve-PkgDir $entry.Name
        if (-not $dir) { continue }
        foreach ($rel in $compile.PSObject.Properties.Name) {
            if ($rel -notlike '*.dll') { continue }
            $full = Join-Path $dir ($rel.Replace('/', '\'))
            if (-not (Test-Path $full)) { continue }
            $resolveMap[[System.IO.Path]::GetFileNameWithoutExtension($full)] = $full
            if ($entry.Name -eq $libKey -and -not $mainDll) { $mainDll = $full; $mainRel = $rel }
        }
    }
    if (-not $mainDll) { throw "$libKey has no compile-time assembly (meta/analyzer package?)" }
}

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class NugetApiDumper
{
    static Dictionary<string, string> _map;

    public static void Init(Dictionary<string, string> map)
    {
        _map = map;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            string path;
            return name != null && _map.TryGetValue(name, out path) ? Assembly.LoadFrom(path) : null;
        };
    }

    static Type[] SafeTypes(Assembly asm)
    {
        try { return asm.GetExportedTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null && t.IsPublic).ToArray(); }
    }

    public static string[] ListTypes(string dll)
    {
        return SafeTypes(Assembly.LoadFrom(dll)).Select(t => t.FullName).OrderBy(n => n).ToArray();
    }

    static string Pretty(Type t)
    {
        if (t == null) return "?";
        if (t.IsByRef) return "ref " + Pretty(t.GetElementType());
        if (t.IsArray) return Pretty(t.GetElementType()) + "[]";
        var nullable = Nullable.GetUnderlyingType(t);
        if (nullable != null) return Pretty(nullable) + "?";
        if (t.IsGenericType)
            return t.Name.Split('`')[0] + "<" + string.Join(", ", t.GetGenericArguments().Select(Pretty)) + ">";
        switch (t.FullName)
        {
            case "System.Void": return "void";
            case "System.String": return "string";
            case "System.Object": return "object";
            case "System.Boolean": return "bool";
            case "System.Int32": return "int";
            case "System.Int64": return "long";
            case "System.UInt32": return "uint";
            case "System.UInt64": return "ulong";
            case "System.Int16": return "short";
            case "System.UInt16": return "ushort";
            case "System.Byte": return "byte";
            case "System.SByte": return "sbyte";
            case "System.Char": return "char";
            case "System.Double": return "double";
            case "System.Single": return "float";
            case "System.Decimal": return "decimal";
        }
        return t.Name;
    }

    static string Params(MethodBase m)
    {
        return "(" + string.Join(", ", m.GetParameters().Select(p => Pretty(p.ParameterType) + " " + p.Name)) + ")";
    }

    static bool IsStatic(PropertyInfo p)
    {
        var accessor = p.GetMethod ?? p.SetMethod;
        return accessor != null && accessor.IsStatic;
    }

    static void Emit(StringBuilder sb, string filter, Func<string> line)
    {
        string s;
        try { s = line(); }
        catch (Exception e) { s = "  !! unresolvable member (" + e.GetType().Name + ": " + e.Message.Split('\n')[0].Trim() + ")"; }
        if (filter == null || s.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) sb.AppendLine(s);
    }

    public static string Dump(string dll, string typeName, string memberFilter)
    {
        if (string.IsNullOrEmpty(memberFilter)) memberFilter = null;
        var types = SafeTypes(Assembly.LoadFrom(dll));
        var matches = types.Where(t => t.FullName == typeName).ToArray();
        if (matches.Length == 0)
            matches = types.Where(t => t.Name == typeName || t.Name.Split('`')[0] == typeName).ToArray();
        if (matches.Length == 0) return "Type '" + typeName + "' not found; run without -Type to list all types.";
        if (matches.Length > 1) return "Multiple matches - rerun with the full name:\n" + string.Join("\n", matches.Select(t => "  " + t.FullName));

        var type = matches[0];
        var sb = new StringBuilder();
        var kind = type.IsEnum ? "enum" : type.IsValueType ? "struct" : type.IsInterface ? "interface"
                 : typeof(MulticastDelegate).IsAssignableFrom(type) ? "delegate"
                 : (type.IsAbstract && type.IsSealed) ? "static class" : type.IsAbstract ? "abstract class"
                 : type.IsSealed ? "sealed class" : "class";
        var bases = new List<string>();
        if (!type.IsValueType && !type.IsEnum && type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(MulticastDelegate))
            bases.Add(Pretty(type.BaseType));
        bases.AddRange(type.GetInterfaces().Where(i => i.IsPublic).Select(Pretty));
        sb.AppendLine("public " + kind + " " + type.Namespace + "." + Pretty(type) + (bases.Count > 0 ? " : " + string.Join(", ", bases) : ""));

        if (type.IsEnum)
        {
            foreach (var n in Enum.GetNames(type))
                sb.AppendLine("  " + n + " = " + Convert.ToInt64(Enum.Parse(type, n)));
            return sb.ToString();
        }

        const BindingFlags F = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var c in type.GetConstructors())
            Emit(sb, memberFilter, () => "  " + type.Name.Split('`')[0] + Params(c));
        foreach (var p in type.GetProperties(F).OrderBy(x => x.Name))
        {
            var prop = p;
            Emit(sb, memberFilter, () => "  " + (IsStatic(prop) ? "static " : "") + Pretty(prop.PropertyType) + " " + prop.Name + " { "
                + (prop.GetMethod != null && prop.GetMethod.IsPublic ? "get; " : "")
                + (prop.SetMethod != null && prop.SetMethod.IsPublic ? "set; " : "") + "}");
        }
        foreach (var m in type.GetMethods(F).Where(x => !x.IsSpecialName).OrderBy(x => x.Name))
        {
            var method = m;
            Emit(sb, memberFilter, () => "  " + (method.IsStatic ? "static " : "") + Pretty(method.ReturnType) + " " + method.Name
                + (method.IsGenericMethod ? "<" + string.Join(", ", method.GetGenericArguments().Select(Pretty)) + ">" : "")
                + Params(method));
        }
        foreach (var f in type.GetFields(F).Where(x => !x.IsSpecialName).OrderBy(x => x.Name))
        {
            var field = f;
            Emit(sb, memberFilter, () => "  " + (field.IsLiteral ? "const " : field.IsStatic ? "static " : "") + Pretty(field.FieldType) + " " + field.Name);
        }
        foreach (var e in type.GetEvents(F).OrderBy(x => x.Name))
        {
            var evt = e;
            Emit(sb, memberFilter, () => "  event " + Pretty(evt.EventHandlerType) + " " + evt.Name);
        }
        foreach (var n in type.GetNestedTypes())
        {
            var nested = n;
            Emit(sb, memberFilter, () => "  nested " + Pretty(nested));
        }
        return sb.ToString();
    }
}
'@

[NugetApiDumper]::Init($resolveMap)
Write-Host "# $libKey -> $mainRel (reflected surface; declared members only - base type named in header)`n"

if (-not $Type) {
    [NugetApiDumper]::ListTypes($mainDll) |
        Group-Object { if ($_ -match '^(.*)\.[^.]+$') { $Matches[1] } else { '(global)' } } |
        Sort-Object Name | ForEach-Object {
            "## $($_.Name)"
            (($_.Group | ForEach-Object { ($_ -split '\.')[-1] } | Sort-Object) -join ', ')
            ''
        }
    exit 0
}

[NugetApiDumper]::Dump($mainDll, $Type, $Member)
