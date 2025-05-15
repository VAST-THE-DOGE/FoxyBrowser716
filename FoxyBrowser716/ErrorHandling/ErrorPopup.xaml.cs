using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media; // Required for MouseButtonEventArgs
using System.Windows.Threading;

namespace FoxyBrowser716.ErrorHandling;

public partial class ErrorPopup : Window
{
	private Exception? normEx;
	private DispatcherUnhandledExceptionEventArgs? dispatcherEx;
	private UnobservedTaskExceptionEventArgs? unobservedEx;
	private string details;
	
	public ErrorPopup(Exception ex)
	{
		normEx = ex;
		InitializeComponent();
		InitializeErrorDetails();
		// Add this line to enable dragging
		this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
	}
	
	public ErrorPopup(DispatcherUnhandledExceptionEventArgs ex)
	{
		dispatcherEx = ex;
		InitializeComponent();
		InitializeErrorDetails();
		// Add this line to enable dragging
		this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
	}
	
	public ErrorPopup(UnobservedTaskExceptionEventArgs ex)
	{
		unobservedEx = ex;
		InitializeComponent();
		InitializeErrorDetails();
		// Add this line to enable dragging
		this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
	}

    // Event handler for dragging the window
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
	
	private void InitializeErrorDetails()
	{
		details = GenerateErrorDetails();
	}
	
	private string GenerateErrorDetails()
	{
		var sb = new StringBuilder();
		
		// Get application version
		var assembly = System.Reflection.Assembly.GetExecutingAssembly();
		var version = assembly.GetName().Version;
		var appVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
		
		// Get current UI culture
		var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
		var currentUICulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
		
		sb.AppendLine("=== ERROR DETAILS ===");
		sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
		sb.AppendLine($"Application: FoxyBrowser716 v{appVersion}");
		
		// Add current window information if available
		try
		{
			if (Application.Current.MainWindow != null)
			{
				sb.AppendLine($"Active Window: {Application.Current.MainWindow.GetType().Name}");
			}
			
			// Count open windows
			var windowCount = Application.Current.Windows.Count;
			sb.AppendLine($"All Open Windows (includes error dialog): {windowCount}");
			sb.AppendLine($"ServerManager.Context.BrowserWindows Count: {ServerManager.Context.BrowserWindows.Count}");
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Unable to retrieve window information: {ex.Message}");
		}
		
		sb.AppendLine();
		
		Exception? exception = null;
		
		if (normEx != null)
		{
			exception = normEx;
			sb.AppendLine("Error Type: Unhandled Exception");
		}
		else if (dispatcherEx != null)
		{
			exception = dispatcherEx.Exception;
			sb.AppendLine("Error Type: Dispatcher Unhandled Exception");
			//sb.AppendLine($"Handled: {dispatcherEx.Handled}");
		}
		else if (unobservedEx != null)
		{
			exception = unobservedEx.Exception;
			sb.AppendLine("Error Type: Unobserved Task Exception");
			//sb.AppendLine($"Observed: {unobservedEx.Observed}");
		}
		
		if (exception != null)
		{
			sb.AppendLine();
			sb.AppendLine($"Exception: {exception.GetType().FullName}");
			sb.AppendLine($"Message: {exception.Message}");
			sb.AppendLine($"HRESULT: 0x{exception.HResult:X8}");
			
			// Add more specific info based on exception type
			if (exception is System.IO.IOException)
			{
				sb.AppendLine("Type: I/O Error");
			}
			else if (exception is System.Net.WebException webEx)
			{
				sb.AppendLine($"Web Error Status: {webEx.Status}");
				if (webEx.Response != null && webEx.Response is System.Net.HttpWebResponse response)
				{
					sb.AppendLine($"HTTP Status Code: {(int)response.StatusCode} ({response.StatusCode})");
				}
			}
			else if (exception is System.ComponentModel.Win32Exception win32Ex)
			{
				sb.AppendLine($"Win32 Error Code: {win32Ex.NativeErrorCode}");
			}
			
			sb.AppendLine();
			sb.AppendLine("Stack Trace:");
			sb.AppendLine(exception.StackTrace);
			
			// Add data from exception.Data collection if any
			if (exception.Data.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("Exception Data:");
				foreach (System.Collections.DictionaryEntry entry in exception.Data)
				{
					sb.AppendLine($"  {entry.Key}: {entry.Value}");
				}
			}
			
			// Include inner exception details if available
			var innerException = exception.InnerException;
			while (innerException != null)
			{
				sb.AppendLine();
				sb.AppendLine($"Inner Exception: {innerException.GetType().FullName}");
				sb.AppendLine($"Message: {innerException.Message}");
				sb.AppendLine($"HRESULT: 0x{innerException.HResult:X8}");
				sb.AppendLine();
				sb.AppendLine("Stack Trace:");
				sb.AppendLine(innerException.StackTrace);
				
				innerException = innerException.InnerException;
			}
		}
		
		sb.AppendLine();
		sb.AppendLine("=== SYSTEM INFORMATION ===");
		sb.AppendLine($"OS: {Environment.OSVersion}");
		sb.AppendLine($"OS Version: {GetWindowsVersion()}");
		sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
		sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
		sb.AppendLine($".NET Version: {Environment.Version}");
		sb.AppendLine($"Machine Name: {Environment.MachineName}");
		sb.AppendLine($"Current Culture: {currentCulture}");
		sb.AppendLine($"Current UI Culture: {currentUICulture}");
		sb.AppendLine($"System Boot Time: {GetSystemBootTime()}");
		
		// Hardware information
		try
		{
			AddHardwareInfo(sb);
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Failed to retrieve hardware info: {ex.Message}");
		}
		
		sb.AppendLine();
		sb.AppendLine("=== RUNTIME INFORMATION ===");
		sb.AppendLine($"Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
		sb.AppendLine($"Process Start Time: {System.Diagnostics.Process.GetCurrentProcess().StartTime}");
		sb.AppendLine($"Total Process Memory: {System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)} MB");
		sb.AppendLine($"GC Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
		sb.AppendLine($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
		sb.AppendLine($"Thread Name: {System.Threading.Thread.CurrentThread.Name ?? "Unnamed"}");
		sb.AppendLine($"Thread Culture: {System.Threading.Thread.CurrentThread.CurrentCulture.Name}");
		sb.AppendLine($"Thread UI Culture: {System.Threading.Thread.CurrentThread.CurrentUICulture.Name}");
		
		return sb.ToString();
	}
	
	private string GetWindowsVersion()
	{
		try
		{
			// Try to get more detailed Windows version info
			using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
			if (key != null)
			{
				var productName = key.GetValue("ProductName") as string;
				var releaseId = key.GetValue("ReleaseId") as string;
				var build = key.GetValue("CurrentBuildNumber") as string;
				
				if (!string.IsNullOrEmpty(productName))
				{
					return $"{productName} ({releaseId ?? "Unknown"}) Build {build ?? "Unknown"}";
				}
			}
		}
		catch
		{
			// Fallback to Environment.OSVersion if registry access fails
		}
		
		return Environment.OSVersion.VersionString;
	}
	
	private DateTime GetSystemBootTime()
	{
		try
		{
			return DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount);
		}
		catch
		{
			return DateTime.MinValue;
		}
	}
	
	private void AddHardwareInfo(StringBuilder sb)
	{
		sb.AppendLine();
		sb.AppendLine("=== HARDWARE INFORMATION ===");
		
		// Get processor information
		try
		{
			var processorCount = Environment.ProcessorCount;
			sb.AppendLine($"Processor Count: {processorCount}");
			
			using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
			if (key != null)
			{
				var processorName = key.GetValue("ProcessorNameString") as string;
				if (!string.IsNullOrEmpty(processorName))
				{
					sb.AppendLine($"Processor: {processorName.Trim()}");
				}
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve processor info: {ex.Message}");
		}
		
		// Get memory information
		try
		{
			var memoryStatus = new NativeMethods.MEMORYSTATUSEX();
			if (NativeMethods.GlobalMemoryStatusEx(memoryStatus))
			{
				var totalPhysicalMem = (double)memoryStatus.ullTotalPhys / (1024 * 1024 * 1024);
				var availPhysicalMem = (double)memoryStatus.ullAvailPhys / (1024 * 1024 * 1024);
				
				sb.AppendLine($"Total Physical Memory: {totalPhysicalMem:F2} GB");
				sb.AppendLine($"Available Physical Memory: {availPhysicalMem:F2} GB");
				sb.AppendLine($"Memory Load: {memoryStatus.dwMemoryLoad}%");
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve memory info: {ex.Message}");
		}
		
		// Get video card information
		try
		{
			using var graphicsKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
			if (graphicsKey != null)
			{
				var gpuName = graphicsKey.GetValue("DriverDesc") as string;
				if (!string.IsNullOrEmpty(gpuName))
				{
					sb.AppendLine($"Graphics Card: {gpuName}");
				}
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve graphics card info: {ex.Message}");
		}
		
		// WPF rendering information
		try
		{
			var renderingTier = (System.Windows.Media.RenderCapability.Tier / 0x10000);
			sb.AppendLine($"WPF Rendering Tier: {renderingTier}");
			sb.AppendLine($"WPF Software Rendering: {RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly}");
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve WPF rendering info: {ex.Message}");
		}
		
		// Installed .NET Runtime versions
		try
		{
			sb.AppendLine();
			sb.AppendLine("=== .NET RUNTIME INFORMATION ===");
			
			using var ndpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");
			if (ndpKey != null)
			{
				foreach (var versionKeyName in ndpKey.GetSubKeyNames())
				{
					if (versionKeyName.StartsWith("v"))
					{
						var versionKey = ndpKey.OpenSubKey(versionKeyName);
						if (versionKey != null)
						{
							var version = versionKey.GetValue("Version", "").ToString();
							var sp = versionKey.GetValue("SP", "").ToString();
							var install = versionKey.GetValue("Install", "").ToString();
							
							if (!string.IsNullOrEmpty(install) && install == "1")
							{
								if (!string.IsNullOrEmpty(sp) && sp != "0")
								{
									sb.AppendLine($".NET Framework {versionKeyName} SP{sp} ({version})");
								}
								else
								{
									sb.AppendLine($".NET Framework {versionKeyName} ({version})");
								}
							}
							
							if (versionKeyName == "v4")
							{
								CheckDotNetFramework4Release(sb, versionKey);
							}
						}
					}
				}
			}
			
			// Check for .NET Core / .NET 5+ runtimes
			try
			{
				var dotnetExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
				if (File.Exists(dotnetExePath))
				{
					var startInfo = new ProcessStartInfo
					{
						FileName = dotnetExePath,
						Arguments = "--list-runtimes",
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true
					};
					
					var process = Process.Start(startInfo);
					if (process != null)
					{
						var output = process.StandardOutput.ReadToEnd();
						process.WaitForExit();
						
						sb.AppendLine("Installed .NET Core / .NET 5+ Runtimes:");
						
						var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var line in lines)
						{
							sb.AppendLine($"  {line.Trim()}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				sb.AppendLine($"Could not retrieve .NET Core version info: {ex.Message}");
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve .NET version info: {ex.Message}");
		}
		
		// Application domain information
		try
		{
			sb.AppendLine();
			sb.AppendLine("=== APP DOMAIN INFORMATION ===");
			var domain = AppDomain.CurrentDomain;
			sb.AppendLine($"Domain Name: {domain.FriendlyName}");
			sb.AppendLine($"Base Directory: {domain.BaseDirectory}");
			sb.AppendLine($"Shadow Copy Enabled: {domain.ShadowCopyFiles}");
			sb.AppendLine($"Setup Information: {domain.SetupInformation.ApplicationBase}");
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve AppDomain info: {ex.Message}");
		}
		
		// Add information about loaded modules
		try
		{
			sb.AppendLine();
			sb.AppendLine("=== LOADED ASSEMBLIES ===");
			
			var currentProcess = Process.GetCurrentProcess();
			var mainModule = currentProcess.MainModule;
			
			sb.AppendLine($"Main Module: {mainModule?.FileName ?? "Unknown"}");
			sb.AppendLine($"Main Module Version: {mainModule?.FileVersionInfo.FileVersion ?? "Unknown"}");
			
			// List first 10 loaded assemblies to avoid making the report too large
			var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			sb.AppendLine($"Total Loaded Assemblies: {loadedAssemblies.Length}");
			sb.AppendLine("First 10 Loaded Assemblies:");
			
			foreach (var assembly in loadedAssemblies.Take(10))
			{
				try
				{
					var assemblyName = assembly.GetName();
					sb.AppendLine($"  {assemblyName.Name}, Version={assemblyName.Version}");
				}
				catch
				{
					sb.AppendLine($"  {assembly} (Could not retrieve detailed information)");
				}
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine($"Could not retrieve loaded modules info: {ex.Message}");
		}
	}
	
	private void CheckDotNetFramework4Release(StringBuilder sb, Microsoft.Win32.RegistryKey versionKey)
	{
		// Check for .NET Framework 4.5 and later versions
		var release = versionKey.GetValue("Release", 0).ToString();
		if (!string.IsNullOrEmpty(release))
		{
			int releaseKey = Convert.ToInt32(release);
			string version45AndLater = GetDotNet45PlusVersion(releaseKey);
			if (!string.IsNullOrEmpty(version45AndLater))
			{
				sb.AppendLine($".NET Framework {version45AndLater}");
			}
		}
	}
	
	private string GetDotNet45PlusVersion(int releaseKey)
	{
		// Values from: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
		if (releaseKey >= 528040)
			return "4.8";
		if (releaseKey >= 461808)
			return "4.7.2";
		if (releaseKey >= 461308)
			return "4.7.1";
		if (releaseKey >= 460798)
			return "4.7";
		if (releaseKey >= 394802)
			return "4.6.2";
		if (releaseKey >= 394254)
			return "4.6.1";
		if (releaseKey >= 393295)
			return "4.6";
		if (releaseKey >= 379893)
			return "4.5.2";
		if (releaseKey >= 378675)
			return "4.5.1";
		if (releaseKey >= 378389)
			return "4.5";
		return "";
	}
	
	// Native methods for memory information
	private static class NativeMethods
	{
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public class MEMORYSTATUSEX
		{
			public uint dwLength;
			public uint dwMemoryLoad;
			public ulong ullTotalPhys;
			public ulong ullAvailPhys;
			public ulong ullTotalPageFile;
			public ulong ullAvailPageFile;
			public ulong ullTotalVirtual;
			public ulong ullAvailVirtual;
			public ulong ullAvailExtendedVirtual;
			
			public MEMORYSTATUSEX()
			{
				this.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
			}
		}
		
		[System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		public static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX lpBuffer);
	}
	
	private void DetailsClick(object sender, RoutedEventArgs e)
	{
		// Create a window to display technical details
		var detailsWindow = new Window
		{
			Title = "Error Technical Details",
			Width = 700,
			Height = 500,
			Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
			WindowStartupLocation = WindowStartupLocation.CenterScreen,
			Owner = this, // Make this window the owner so it stays on top
			MinWidth = 400,
			MinHeight = 300
		};
		
		// Create a DockPanel as the main container
		var dockPanel = new DockPanel
		{
			LastChildFill = true // Makes the last child fill the remaining space
		};
		
		// Create a panel for buttons that will be docked at the bottom
		var buttonPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(10),
			Background = new SolidColorBrush(Color.FromRgb(40, 40, 60))
		};
		
		// Create a button to copy details to the clipboard 
		var copyButton = new Button
		{
			Content = " Copy to Clipboard ",
			FontWeight = FontWeights.SemiBold,
			FontSize = 16,
			Background = new SolidColorBrush(Color.FromRgb(56, 56, 76)),
			Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(255, 145, 15)),
			Padding = new Thickness(10, 5, 10, 5),
			Margin = new Thickness(5)
		};
		
		copyButton.Click += (_, _) =>
		{
			Clipboard.SetText(details);
			// MessageBox.Show("Error details copied to clipboard.", "Copy Successful", MessageBoxButton.OK);
		};
		
		// Create a close button
		var closeButton = new Button
		{
			Content = " Close ",
			FontWeight = FontWeights.SemiBold,
			FontSize = 16,
			Background = new SolidColorBrush(Color.FromRgb(56, 56, 76)),
			Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromRgb(255, 145, 15)),
			Padding = new Thickness(10, 5, 10, 5),
			Margin = new Thickness(5)
		};
		
		closeButton.Click += (_, _) => detailsWindow.Close();
		
		// Add buttons to the button panel
		buttonPanel.Children.Add(copyButton);
		buttonPanel.Children.Add(closeButton);
		
		// Dock the button panel at the bottom
		DockPanel.SetDock(buttonPanel, Dock.Bottom);
		dockPanel.Children.Add(buttonPanel);
		
		// Create a text box to display the details with scroll functionality
		var textBox = new TextBox
		{
			Text = details,
			VerticalContentAlignment = VerticalAlignment.Top,
			IsReadOnly = true,
			TextWrapping = TextWrapping.Wrap,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			FontFamily = new System.Windows.Media.FontFamily("Consolas"),
			FontSize = 12,
			Background = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(10)
		};
		
		// The text box will be the last child and will fill the remaining space
		dockPanel.Children.Add(textBox);
		
		// Set the DockPanel as the window content
		detailsWindow.Content = dockPanel;
		
		// Add a handler for window resizing
		detailsWindow.SizeChanged += (_, _) => textBox.Focus();
		
		// Show the details window as a dialog
		detailsWindow.ShowDialog();
	}
	
	private void ManualReportClick(object sender, RoutedEventArgs e)
	{
		// Prepare error details for the GitHub issue
		var errorText = Uri.EscapeDataString(details);
		
		// Open the GitHub issue page with prefilled information
		var issueUrl = $"https://github.com/VAST-THE-DOGE/FoxyBrowser716/issues/new?title=Manual%20Bug%20Report&labels=bug&body=%3D%3D%3D%20USER%20NOTE%20%3D%3D%3D%0D%0DType%20Here...%0D%0D%0D%0D%0D{errorText}";
		
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = issueUrl,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Could not open the GitHub issue page: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
	
	private void AutoReportClick(object sender, RoutedEventArgs e)
	{
		// TODO: Implement automatic error reporting to a server or API
		// This would typically involve sending the error details to a logging service
		throw new NotImplementedException();
	}
	
	private void ContinueClick(object sender, RoutedEventArgs e)
	{
		// Mark the exception as handled if it's a dispatcher exception
		if (dispatcherEx != null)
		{
			dispatcherEx.Handled = true;
		}
		
		// Mark the task exception as observed if it's an unobserved task exception
		if (unobservedEx != null)
		{
			unobservedEx.SetObserved();
		}
		
		// Close this window and let the application continue
		DialogResult = true;
		Close();
	}
	
	private void QuitClick(object sender, RoutedEventArgs e)
	{
		// Mark exceptions as handled/observed before quitting
		if (dispatcherEx != null)
		{
			dispatcherEx.Handled = true;
		}
		
		if (unobservedEx != null)
		{
			unobservedEx.SetObserved();
		}
		
		// Find the window that caused the error
		Window? sourceWindow = null;
		
		// For dispatcher exceptions, try to find the window that caused the exception
		if (dispatcherEx != null)
		{
			// The sender of the dispatcher exception might be the window
			var dispatcher = dispatcherEx.Dispatcher;
			foreach (Window window in Application.Current.Windows)
			{
				if (window.Dispatcher == dispatcher && window != this)
				{
					sourceWindow = window;
					break;
				}
			}
		}
		
		// If we found a source window, close it
		if (sourceWindow != null)
		{
			// Close this error window first
			DialogResult = false;
			Close();
			
			// Then close the source window
			sourceWindow.Close();
		}
		else
		{
			// If we couldn't identify a specific window, ask the user what they want to do
			var result = MessageBox.Show(
				"Do you want to close the entire application?\n\nYes - Close the entire application (all windows)\nNo - Just dismiss this error and continue",
				"Close Application?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
			
			if (result == MessageBoxResult.Yes)
			{
				// Close this window
				DialogResult = false;
				Close();
				
				// Shutdown the entire application
				Application.Current.Shutdown();
			}
			else
			{
				// Just close this error window and continue
				DialogResult = true;
				Close();
			}
		}
	}
}