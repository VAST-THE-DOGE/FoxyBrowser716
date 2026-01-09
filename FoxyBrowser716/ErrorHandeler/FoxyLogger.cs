using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using FoxyBrowser716.DataManagement;

namespace FoxyBrowser716.ErrorHandeler;

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical,
}

public sealed class FoxyLogger
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

    private static readonly LinkedList<FoxyLogger> _errors = [];
    private static int _maxEntries = DefaultMaxEntries;
    private static volatile bool _loaded;

    [JsonPropertyName("S")]
    public ErrorSeverity Severity { get; init; }

    [JsonPropertyName("Src")]
    public string? Source { get; init; }

    [JsonPropertyName("M")]
    public string? MethodDisplay { get; init; }

    [JsonPropertyName("Msg")]
    public string Message { get; init; } = null!;

    [JsonPropertyName("ST")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("TS")]
    public DateTimeOffset TimeStampUtc { get; init; }

    [JsonPropertyName("IE")]
    public FoxyLogger? InnerError { get; init; }

    public FoxyLogger() {}

    public static ReadOnlyCollection<FoxyLogger> Errors
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
        var root = Build(ex, ErrorSeverity.Error);
        AddItem(root);
    }
    
    public static void AddInfo(string message, string details)
    {
        var info = new FoxyLogger
        {
            Severity = ErrorSeverity.Info,
            Message = message,
            StackTrace = details,
            TimeStampUtc = DateTimeOffset.UtcNow
        };
        AddItem(info);
    }
    public static void AddWarning(string message, string? details = null)
    {
        var info = new FoxyLogger
        {
            Severity = ErrorSeverity.Warning,
            Message = message,
            StackTrace = details,
            TimeStampUtc = DateTimeOffset.UtcNow
        };
        AddItem(info);
    }
    
    public static void AddCritical(string message, string? details = null)
    {
        var info = new FoxyLogger
        {
            Severity = ErrorSeverity.Critical,
            Message = message,
            StackTrace = details,
            TimeStampUtc = DateTimeOffset.UtcNow
        };
        AddItem(info);
    }

    private static void AddItem(FoxyLogger item)
    {
        EnsureLoaded();
        
        lock (_sync)
        {
#if DEBUG
            Console.ResetColor();

            var writeColor = item.Severity switch
                {
                    ErrorSeverity.Info => ConsoleColor.Cyan,
                    ErrorSeverity.Warning => ConsoleColor.Yellow,
                    ErrorSeverity.Error => ConsoleColor.Red,
                    ErrorSeverity.Critical => ConsoleColor.DarkRed,
                };
        
            Console.ForegroundColor = writeColor;
            
            Console.WriteLine($"[{item.Severity.ToString()}] {item.Message}");
            Console.ResetColor();
            Console.WriteLine(item.StackTrace);
#endif
            
            _errors.AddFirst(item);
            TrimInMemory_NoLock();
            AppendLineSafe(item);
            if (_errors.Count > _maxEntries)
                CleanupFileSafe();
        }
    }

    private static Assembly _currentAssembly = Assembly.GetExecutingAssembly();
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddInfo(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
        => AddInfo(message, $"CallerInfo:" +
                            $"\n    Line: {lineNumber}" +
                            $"\n    File: {filePath}");
    

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddWarning(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null) 
        => AddWarning(message, $"CallerInfo:" +
                               $"\n    Line {lineNumber}" +
                               $"\n    File: {filePath}");

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
                        var entry = JsonSerializer.Deserialize<FoxyLogger>(line, _jsonOptions);
                        if (entry != null)
                        {
                            _errors.AddLast(entry);
                            if (_errors.Count > _maxEntries)
                                _errors.RemoveFirst();
                        }
                    }
                    catch
                    {
                        if (++errors > 100) break;
                    }
                }

                var reversed = _errors.Reverse().ToList();
                _errors.Clear();
                foreach (var e in reversed) _errors.AddLast(e);
            }
            catch
            {
                // Ignore 
            }
            _loaded = true;
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        LoadLog();
    }

    private static FoxyLogger Build(Exception ex, ErrorSeverity severity)
    {
        FoxyLogger? currentInner = null;
        var depth = 0;
        var cursor = ex;

        while (cursor != null && depth < MaxInnerDepth)
        {
            currentInner = new FoxyLogger
            {
                Severity = severity,
                Source = cursor.Source,
                MethodDisplay = FormatMethod(cursor.TargetSite),
                Message = cursor.Message,
                StackTrace = cursor.StackTrace,
                TimeStampUtc = DateTimeOffset.UtcNow,
                InnerError = currentInner
            };
            cursor = cursor.InnerException;
            depth++;
        }

        currentInner = null;
        cursor = ex;
        depth = 0;
        while (cursor != null && depth < MaxInnerDepth)
        {
            var node = new FoxyLogger
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

    private static void AppendLineSafe(FoxyLogger info)
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
                    FileOptions.WriteThrough);
                using var sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(line);
                sw.Flush();
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
        try
        {
            if (!File.Exists(_errorFile)) return;
            var all = File.ReadLines(_errorFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (all.Count <= _maxEntries) return;

            var trimmed = all.Skip(Math.Max(0, all.Count - _maxEntries)).ToList();
            var temp = _errorFile + ".tmp";

            File.WriteAllLines(temp, trimmed);
            File.Move(temp, _errorFile, true);
            
            _errors.Clear();
            for (var i = trimmed.Count - 1; i >= 0; i--)
            {
                try
                {
                    var e = JsonSerializer.Deserialize<FoxyLogger>(trimmed[i], _jsonOptions);
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
            // ignore
        }
    }
}
