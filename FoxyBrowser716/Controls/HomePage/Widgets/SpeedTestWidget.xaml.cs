using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using Windows.Devices.Geolocation;
using FoxyBrowser716.DataManagement;

namespace FoxyBrowser716.Controls.HomePage.Widgets;

// code adapted from this winforms app:
// https://github.com/ewwink/NetworkSpeed

[WidgetInfo("Speed Test Widget", MaterialIconKind.WifiArrowUpDown, WidgetCategory.Tools)]
public partial class SpeedTestWidget : WidgetBase
{
	protected SpeedTestWidget()
	{
		InitializeComponent();
	}

	private Timer refreshTimer;
	
	/// <summary>
	/// Timer Update (every 1 sec)
	/// </summary>
	private const double timerUpdate = 1000;

	/// <summary>
	/// Interface Storage
	/// </summary>
	private NetworkInterface[] nicArr;

	/// <summary>
	/// Main Timer Object 
	/// (we could use something more efficient such 
	/// as interop calls to HighPerformanceTimers)
	/// </summary>
	private long TotalBytesReceived = 0;
	private long TotalBytesSent = 0;
	private long MaxSpeedDownload = 0;
	private long MaxSpeedUpload = 0;

	private NetworkInterface SelectedNetworkInterface = null;

    protected override async Task Initialize()
    {
	    refreshTimer = new Timer(RefreshTimer_Tick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(2500));
	    ApplyTheme();
    }

    protected override void ApplyTheme()
    {
        OverallBlock.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);
        DetailsBlock.Foreground = new SolidColorBrush(CurrentTheme.SecondaryForegroundColor);
        RootGrid.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        RootGrid.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);
    }

    private void RefreshTimer_Tick(object? state)
    {
	    // if (bestServer is null) return;
	    
	    _ = InitializeNetworkInterface();
	    _ = UpdateNetworkInterface();
    }
    
    private async Task getPublicIP()
    {
	    var publicIP = "0.0.0.0";
	    try
	    {
		    var client = new HttpClient();
		    client.Timeout = TimeSpan.FromSeconds(10);
		    publicIP = await client.GetStringAsync("https://ipecho.net/plain");
	    }
	    catch { }
	    // lblPublicIP.Text = publicIP;

    }
    private void cmbInterface_SelectedIndexChanged(object sender, EventArgs e)
    {
	    foreach (NetworkInterface n in nicArr)
	    {
		    // string adapterName = cmbInterface.SelectedItem.ToString().Split('@')[0].Trim();
		    // if (n.Name == adapterName)
		    // {
			   //  SelectedNetworkInterface = n;
			   //  break;
		    // }
	    }
	    IPv4InterfaceStatistics interfaceStats = SelectedNetworkInterface.GetIPv4Statistics();
	    TotalBytesReceived = interfaceStats.BytesReceived;
	    TotalBytesSent = interfaceStats.BytesSent;
	    _ = getPublicIP();
    }
    
    private String ConvertByteSpeed(long bytes, string suffix, int unit = 1000)
    {
	    if (bytes < unit) { return $"{bytes} B"; }
	    var exp = (int)(Math.Log(bytes) / Math.Log(unit));
	    return $"{bytes / Math.Pow(unit, exp):F1} {("KMGTP")[exp - 1]}{suffix}";
    }

    /// <summary>
    /// Initialize all network interfaces on this computer
    /// </summary>
    private async Task InitializeNetworkInterface()
    {
	    await Task.Run(() =>
	    {
		    nicArr = NetworkInterface.GetAllNetworkInterfaces();
		    // List<string> goodAdapters = new List<string>();

		    foreach (NetworkInterface nicnac in nicArr)
		    {
			    if (nicnac.SupportsMulticast && nicnac.GetIPv4Statistics().UnicastPacketsReceived >= 1 && nicnac.OperationalStatus.ToString() == "Up")
			    {
				    SelectedNetworkInterface = nicnac;
				    return;
			    }
		    }
	    });
    }
    
    private async Task UpdateNetworkInterface()
        {
	        if (SelectedNetworkInterface is  null) return;
                await Task.Run(() =>
                {
                    // Grab the stats for that interface
                    IPv4InterfaceStatistics interfaceStats = SelectedNetworkInterface.GetIPv4Statistics();

                    long speedDownload = interfaceStats.BytesReceived - TotalBytesReceived;
                    TotalBytesReceived = interfaceStats.BytesReceived;
                    if (speedDownload > MaxSpeedDownload) MaxSpeedDownload = speedDownload;

                    long speedUpload = interfaceStats.BytesSent - TotalBytesSent;
                    TotalBytesSent = interfaceStats.BytesSent;
                    if (speedUpload > MaxSpeedUpload) MaxSpeedUpload = speedUpload;

                    string localIP = "127.0.0.1";
                    UnicastIPAddressInformationCollection ipInfo = SelectedNetworkInterface.GetIPProperties().UnicastAddresses;

                    foreach (UnicastIPAddressInformation item in ipInfo)
                    {
                        if (item.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            localIP = item.Address.ToString();
                            break;
                        }
                    }
                    AppServer.UiDispatcherQueue.TryEnqueue(async () =>
                    {
	                    // OverallBlock.Text = $"Ping: {ping} ms";
	                    DetailsBlock.Text = $"Download: {ConvertByteSpeed(speedDownload, "Bps", 1024)} | Upload: {ConvertByteSpeed(speedUpload, "Bps", 1024)}";
                    });
                    // this.Invoke(new Action(() =>
                    // {
	                   //  
                    //     // Update the labels
                    //     lblCurrentDownload.Text = ConvertByteSpeed(speedDownload, "Bps", 1024);
                    //     lblMaxDownload.Text = ConvertByteSpeed(MaxSpeedDownload, "Bps", 1024);
                    //     lblTotalDownload.Text = ConvertByteSpeed(interfaceStats.BytesReceived, "Bps", 1024);
                    //
                    //     lblCurrentUpload.Text = ConvertByteSpeed(speedUpload, "Bps", 1024);
                    //     lblMaxUpload.Text = ConvertByteSpeed(MaxSpeedUpload, "Bps", 1024);
                    //     lblTotalUpload.Text = ConvertByteSpeed(interfaceStats.BytesSent, "Bps", 1024);
                    //     labelIPAddress.Text = localIP;
                    // }));

                });
        }
}
