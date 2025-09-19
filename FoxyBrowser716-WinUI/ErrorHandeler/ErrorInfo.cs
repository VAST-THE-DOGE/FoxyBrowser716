using System.Diagnostics;
using System.Reflection;
using FoxyBrowser716_WinUI.DataManagement;

namespace FoxyBrowser716_WinUI.ErrorHandeler;

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
}

public class ErrorInfo
{
    public ErrorSeverity Severity { get; set; }
    
    public string? Source { get; set; }
    public MethodBase? Method { get; set; }
    
    public string Message { get; set; } = null!;
    public string? StackTrace { get; set; }
    
    public DateTime TimeStamp { get; set; }
    
    public ErrorInfo? InnerError { get; set; }
    
    private ErrorInfo() {}

    private static List<ErrorInfo> _errors = [];
    public static ReadOnlyCollection<ErrorInfo> Errors => _errors.AsReadOnly();
    
    public static void AddError(Exception ex)
    {
        //TODO: save these, just debug log it for now.
        var ei = CreateEI(ex, ErrorSeverity.Error);
        
        _errors.Add(ei);
        
        SaveLog();
        
        Debug.WriteLine(ei.Message, "\n", ei.StackTrace);
    }

    private static ErrorInfo? CreateEI(Exception ex, ErrorSeverity severity, int recur = 0)
    {
        if (recur > 10) return null; // just in case
        
        return new ErrorInfo()
        {
            Severity = severity,
            Source = ex.Source,
            Method = ex.TargetSite,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            TimeStamp = DateTime.Now,
            InnerError = ex.InnerException != null ? CreateEI(ex.InnerException, severity, ++recur) : null
        };
    }

    private static readonly string _file = FoxyFileManager.BuildFilePath("ErrorLog.json", FoxyFileManager.FolderType.Data);
    public static void LoadLog()
    {
        try
        {
            var result = FoxyFileManager.ReadFromFile<List<ErrorInfo>>(_file);
            if (result is { code: FoxyFileManager.ReturnCode.Success, content: not null })
                _errors = result.content;
        }
        catch (Exception e)
        {
            // ignore
        }
        
    }

    private static void SaveLog()
    {
        //TODO: rate limit this, can be saving 24/7 from a stream of errors.
        try
        {
            _ = FoxyFileManager.SaveToFile(_file, _errors);
        }
        catch (Exception e)
        {
            // ignore
        }
    }
}