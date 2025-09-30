using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using FoxyBrowser716_WinUI.DataManagement;

namespace FoxyBrowser716_WinUI.ErrorHandeler;

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
}

public sealed class ErrorInfo
{
    private const int DefaultMaxEntries = 5000;
    private const int MaxInnerDepth = 10;
    private static readonly string _errorFile = FoxyFileManager.BuildFilePath("errors.jsonl", FoxyFileManager.FolderType.Data);
    private static readonly Lock _sync = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 64,
    };

    // In‑memory circular buffer (most recent first)
    private static readonly LinkedList<ErrorInfo> _errors = [];
    private static int _maxEntries = DefaultMaxEntries;
    private static volatile bool _loaded;

    [JsonPropertyName("S")]
    public ErrorSeverity Severity { get; init; }

    [JsonPropertyName("Src")]
    public string? Source { get; init; }

    // Store lightweight method info instead of MethodBase (safer for serialization)
    [JsonPropertyName("M")]
    public string? MethodDisplay { get; init; }

    [JsonPropertyName("Msg")]
    public string Message { get; init; } = null!;

    [JsonPropertyName("ST")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("TS")]
    public DateTimeOffset TimeStampUtc { get; init; }

    [JsonPropertyName("IE")]
    public ErrorInfo? InnerError { get; init; }

    public ErrorInfo() {}

    public static ReadOnlyCollection<ErrorInfo> Errors
    {
        get
        {
            EnsureLoaded();
            lock (_sync)
            {
                return _errors.ToList().AsReadOnly();
            }
        }
    }

    public static void Configure(int maxEntries)
    {
        if (maxEntries <= 0) return;
        lock (_sync)
        {
            _maxEntries = maxEntries;
            TrimInMemory_NoLock();
        }
    }

    public static void AddError(Exception ex)
    {
        EnsureLoaded();
        var root = Build(ex, ErrorSeverity.Error);
        lock (_sync)
        {
            _errors.AddFirst(root);
            TrimInMemory_NoLock();
            AppendLineSafe(root);
            if (_errors.Count > _maxEntries) // extra guard
                CleanupFileSafe();
        }
    }
    
    public static void AddInfo(string message, string? details = null)
    {
        EnsureLoaded();
        var info = new ErrorInfo
        {
            Severity = ErrorSeverity.Info,
            Message = message,
            StackTrace = details,
            TimeStampUtc = DateTimeOffset.UtcNow
        };
        lock (_sync)
        {
            _errors.AddFirst(info);
            TrimInMemory_NoLock();
            AppendLineSafe(info);
            if (_errors.Count > _maxEntries)
                CleanupFileSafe();
        }
    }
    public static void AddWarning(string message, string? details = null)
    {
        EnsureLoaded();
        var info = new ErrorInfo
        {
            Severity = ErrorSeverity.Warning,
            Message = message,
            StackTrace = details,
            TimeStampUtc = DateTimeOffset.UtcNow
        };
        lock (_sync)
        {
            _errors.AddFirst(info);
            TrimInMemory_NoLock();
            AppendLineSafe(info);
            if (_errors.Count > _maxEntries)
                CleanupFileSafe();
        }
    }

    private static Assembly _currentAssembly = Assembly.GetExecutingAssembly();
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddInfo(string message)
    {
        var callingMethod = new StackTrace()
            .GetFrames()
            .FirstOrDefault(f => f.GetMethod() is { DeclaringType: { } declaringType } method
                                 && !method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                 && !declaringType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                 && declaringType.Assembly == _currentAssembly
                                 && method.Name != nameof(AddInfo))?
            .GetMethod();
        
        AddInfo(message, FormatMethod(callingMethod));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddWarning(string message)
    {
        var callingMethod = new StackTrace()
            .GetFrames()
            .FirstOrDefault(f => f.GetMethod() is { DeclaringType: { } declaringType } method
                                 && !method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                 && !declaringType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                                 && declaringType.Assembly == _currentAssembly
                                 && method.Name != nameof(AddWarning))?
            .GetMethod();
        
        AddWarning(message, FormatMethod(callingMethod));
    }

    public static void LoadLog()
    {
        lock (_sync)
        {
            _errors.Clear();
            if (!File.Exists(_errorFile))
            {
                _loaded = true;
                return;
            }

            try
            {
                var errors = 0;
                var lines = File.ReadLines(_errorFile);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<ErrorInfo>(line, _jsonOptions);
                        if (entry != null)
                        {
                            _errors.AddLast(entry); // oldest at start
                            if (_errors.Count > _maxEntries)
                                _errors.RemoveFirst();
                        }
                    }
                    catch
                    {
                        if (++errors > 100) break; // TODO: stack overflow ono line error
                        
                        continue;
                    }
                }
                // Reorder to most recent first
                var reversed = _errors.Reverse().ToList();
                _errors.Clear();
                foreach (var e in reversed) _errors.AddLast(e);
            }
            catch
            {
                // Ignore load failure
            }
            _loaded = true;
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        LoadLog();
    }

    private static ErrorInfo Build(Exception ex, ErrorSeverity severity)
    {
        ErrorInfo? currentInner = null;
        var depth = 0;
        var cursor = ex;

        while (cursor != null && depth < MaxInnerDepth)
        {
            currentInner = new ErrorInfo
            {
                Severity = severity,
                Source = cursor.Source,
                MethodDisplay = FormatMethod(cursor.TargetSite),
                Message = cursor.Message,
                StackTrace = cursor.StackTrace,
                TimeStampUtc = DateTimeOffset.UtcNow,
                InnerError = currentInner // build chain in reverse
            };
            cursor = cursor.InnerException;
            depth++;
        }

        // currentInner now holds the outermost built from last iteration; rebuild ordering:
        // Because we built from outer->inner incorrectly we need the first created (outermost).
        // Simpler: rebuild in forward order:
        // Re-implement clean:

        // Forward rebuild (clear and redo)
        currentInner = null;
        cursor = ex;
        depth = 0;
        while (cursor != null && depth < MaxInnerDepth)
        {
            var node = new ErrorInfo
            {
                Severity = severity,
                Source = cursor.Source,
                MethodDisplay = FormatMethod(cursor.TargetSite),
                Message = cursor.Message,
                StackTrace = cursor.StackTrace,
                TimeStampUtc = DateTimeOffset.UtcNow,
                InnerError = currentInner
            };
            currentInner = node;
            cursor = cursor.InnerException;
            depth++;
        }
        // currentInner is now OUTERMOST with InnerError chain inward
        return currentInner!;
    }

    private static string? FormatMethod(MethodBase? m)
    {
        if (m == null) return null;
        try
        {
            return $"{m.DeclaringType?.FullName}.{m.Name}";
        }
        catch
        {
            return m.Name;
        }
    }

    private static void TrimInMemory_NoLock()
    {
        while (_errors.Count > _maxEntries)
            _errors.RemoveLast();
    }

    private static void AppendLineSafe(ErrorInfo info)
    {
        string line;
        try
        {
            line = JsonSerializer.Serialize(info, _jsonOptions);
        }
        catch
        {
            return;
        }

        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_errorFile)!);
                using var fs = new FileStream(
                    _errorFile,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    4096,
                    FileOptions.WriteThrough | FileOptions.Asynchronous); // WriteThrough improves crash durability
                using var sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(line);
                sw.Flush(); // ensure persisted
                fs.Flush(true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(30 * (i + 1));
            }
            catch
            {
                return;
            }
        }
    }

    private static void CleanupFileSafe()
    {
        // Rewrite keeping latest _maxEntries lines
        try
        {
            if (!File.Exists(_errorFile)) return;
            var all = File.ReadLines(_errorFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (all.Count <= _maxEntries) return;

            var trimmed = all.Skip(Math.Max(0, all.Count - _maxEntries)).ToList();
            var temp = _errorFile + ".tmp";

            File.WriteAllLines(temp, trimmed);
            File.Move(temp, _errorFile, true);
            
            // Reload in-memory (most recent first)
            _errors.Clear();
            for (var i = trimmed.Count - 1; i >= 0; i--)
            {
                try
                {
                    var e = JsonSerializer.Deserialize<ErrorInfo>(trimmed[i], _jsonOptions);
                    if (e != null) _errors.AddLast(e);
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore cleanup failure
        }
    }
}
