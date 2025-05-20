using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace FoxyBrowser716;

// layout plan:
// Server Manager (holds everything)
// breaks down into instances and windows



/// <summary>
/// Browser works as a server-client system to easily manage and sync data across windows
/// </summary>
public class ServerManager
{
	public static ServerManager Context { get; private set; }

	public static readonly string InstanceFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Instances");
	
	public InstanceManager DefaultBrowserManager { get; private set; }
	
	public List<InstanceManager> AllBrowserManagers = [];
	
	public List<MainWindow> BrowserWindows = [];
	
	private ServerManager()
	{ /*TODO*/ }

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllocConsole();

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool FreeConsole();

	
	public static void RunServer(StartupEventArgs e)
	{
		// you see nothing here:
		if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
			Task.Run(async () => { AllocConsole(); Console.OutputEncoding = Encoding.UTF8; Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.DarkCyan; Console.Title = "MikuBrowser01"; var s = Encoding.UTF8.GetString(Convert.FromBase64String("4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qCm4qOE4qOg4qC04qCS4qCS4qCJ4qCS4qC24qOE4qGP4qO24qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjoOKhnuKjoeKggOKioOKhgOKggOKipuKhgOKiueKjt+KjvOKjhOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7jiob/ioIHio7/ioJ/io7fio7/ioqbioYjio4fioIDiorvio7/io4jiorfioYTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qO/4qGH4qO24qO74qOA4qCY4qO/4qOA4qO54qO/4qOw4qO84qGf4qOv4qCf4qO/4qGE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjv+Khh+Kjv+Khv+Kgv+Kgi+KgieKgi+Kgm+Kiv+Kjv+Kjv+Khh+KgiOKipuKhuOKjvuKjhuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjio7/ioYfioJvio7fio4TioIDioIDioIDioIDio7jioJ/ioIPioInioIDioIjiorfio6niobvio4TioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK44qO/4qC54qGE4qCI4qCZ4qCS4qKk4qO04qO+4qCJ4qCB4qOA4qGA4qCA4qCA4qCA4qC54qOf4qKu4qGz4qOE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjvOKjv+KggOKjv+KggOKjgOKjtOKjv+Khj+KjgOKhrOKgn+KggeKgiOKjhuKggOKggOKggOKgmOKip+KhieKiv+Kjp+KhgOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioqDiob/io7/ioIDiorvio6vioInio73ioZ/ioInioYXiooDio4bioIDioqDio7/ioIDioIDioIDioIDioIDioLnio6bioYnioLvioqbioYDioYTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO44qKX4qO/4qKA4qG34qCB4qO44qG/4qCB4qCA4qCA4qK44qO/4qCD4qOE4qOI4qGG4qCA4qCA4qCA4qCA4qCA4qCI4qCr4qOx4qCm4qOM4qGT4qKk4qOA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKisOKjh+KjvuKjv+Kin+Khh+KigOKjv+Khh+KggOKigOKjsOKiuOKhjOKiv+Kjv+Kiu+Khn+Kjg+KggOKggOKggOKgsOKhguKgoOKgjOKgsuKipOKjmeKju+Kgm+Kgk+KgtuKipuKhpOKjhOKjgOKhgOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio6Dio7/io7vio7/ioI/io77ioIPiorjior3ioYfioIDioY/ioIHiobbioIHioIjio7/io47iobfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioInioInioJvioJLioLbioqTio4DioIjioLvio4TioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qO04qO/4qCf4qCB4qG84qCA4qO/4qCA4qGf4qK44qGH4qKw4qCD4qK44qCH4qCA4qCA4qK54qO34qO/4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK74qO34qOE4qCY4qOn4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKigOKjgOKjpOKjvuKgn+Kgi+KggOKjoOKhvOKigeKjvOKjv+KioOKjp+KivuKhh+KgmOKhgOKguOKhh+KggOKggOKgmOKjv+Kjv+Kjt+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKhieKgmuKip+KgueKjt+KggArioIDioIDioIDioIDioIDioIDioIDioIDioIDiooDio6DiobTioJbioovio73iob/ioKXioJbioJrioIniooHio7Tio77io7/ioIPioojioIDiorjioIfioIDioIHioIDiorPioYDioIDioIDio7/io7/io73ioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7jioIDioIDioJjioqfioYfioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qOA4qOk4qO+4qCb4qCB4qOg4qG84qCL4qCB4qCA4qKA4qOA4qKA4qO04qO/4qO/4qCf4qCB4qCA4qCA4qO34qCL4qCC4qCA4qCA4qCA4qCA4qCz4qGA4qCA4qK54qO/4qO/4qGG4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qG/4qCA4qCA4qCA4qK44qCn4qCACuKggOKggOKigOKhoOKgluKgi+KjqeKgtuKii+KhtOKgi+KggeKggOKggOKgv+KjreKhv+Kjv+Kgv+Kgn+KiieKjt+KjhOKggOKigOKjvOKjv+KhhOKggOKggOKggOKggOKggOKigOKjueKghOKggOKiueKjv+Khh+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgg+KggOKggOKggOKggOKgkOKhpwrioIDioIDio7jio4Tio7Tio5vio6HioJ7ioIvioIDioIDioIDioIDioIDioIDioIDio63io6Tio7Tio77io7/io7/io7/io7fio7/io7/io7/io7fio6Tio7Tio7bio77io7/io7/io7/io4bioIDioojio7/io7fio6TioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioLjiopEK4qCA4qCw4qOP4qG/4qKr4qGe4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCI4qC74qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qOm4qO54qC/4qC/4qCf4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKACuKggOKigOKjv+KhteKgi+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgieKgmeKjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kju+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kgm+KggOKggOKggOKggOKggOKggeKggOKggOKggOKggOKggOKggOKggOKigOKhnOKggOKggOKggOKggOKggOKggOKigOKgjgrioqDiorjioZ/ioLnioYDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjioafioKTiopnioJvioIvioJvioJvioL/ioL/ior/ioZ/ioJvioInioIHioIDioIDioIDioIDioIDioKDioJDioIDioIDioIDioIDioIDioIDioIDiooDiobziooDioIDioITioIDioIDioYDioYDioIDioIAK4qK44qOO4qGH4qCA4qCz4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK34qCA4qGA4qKw4qGF4qCA4qCA4qCA4qCA4qCA4qGd4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qO+4qG/4qKL4qCe4qCA4qKg4qOu4qCO4qCA4qCA4qCACuKggOKiv+Kjt+KggOKggOKgiOKgguKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgmOKjv+KjtuKjv+Kjh+KggOKggOKggOKggOKjsOKgg+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKioOKhvuKgi+KjoOKgi+KigOKjtOKgn+KggeKggOKggOKggOKggArioIDioJjio4/ioqfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorvio7/io7/io7/iornio7vio7/io7/ioIfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7DioJ/ioqHioJ7io4Hio7TioJ/ioIHioIDioIDioIDioIDioIDioIAK4qCA4qCA4qC44qGE4qCz4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCz4qKE4qGA4qCA4qCA4qCY4qO/4qO/4qGv4qK24qKn4qO/4qGP4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qG+4qKB4qO04qC/4qCa4qCJ4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKgueKhhOKgmeKipuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgmeKgouKjhOKgiOKiv+Kjv+Khr+KjneKjvuKjv+KggeKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjoOKjtOKhr+KiluKhv+KgieKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioLnioYTioIDioJHiooTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIniorvio7/io7/io73io7/ioYfioIDioIDioIDioIDioIDioIDioIDio4Dio4Dio6TiorTio7bioL/ioIvioIHio7DioIvioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCY4qKm4qGA4qCA4qCZ4qCi4qOE4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO/4qO74qK/4qO/4qGN4qCR4qCS4qCS4qOS4qO+4qO34qC/4qCb4qOL4qG14qCa4qCB4qCA4qOg4qCe4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKgmeKgouKjhOKhgOKggOKgmeKgk+KgkuKggOKggOKjgOKhgOKggOKggOKggOKggOKggOKggOKigOKjv+Kjv+Kjv+Kjv+Kjt+KhmuKgm+KgieKggeKggOKggOKgkOKgi+KigOKhgOKigOKhpOKgnuKggeKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIjioInioInioJnioIvioJvioJvio7nio7/io7/io7/io7/io7/io7fioYDioIDioIDioIDioIDioIDioIDioJvioIvioInioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO/4qO/4qO74qO/4qO/4qO/4qO/4qOH4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjv+Kjv+Kjv+Kjv+Kiv+Kjv+Kjv+Kjv+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiooDio7/io7/io7/ioY/ioLjio7/io7/io7/ioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK44qO/4qO/4qO/4qCB4qCA4qK54qO/4qO/4qGE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjv+Kjv+Khj+KggOKggOKgiOKjv+Kjv+KhhuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjio7/io7/ioIDioIDioIDioIDiorzio7/io7fioYDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO84qO/4qO/4qCA4qCA4qCA4qCA4qO84qO/4qO/4qGH4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgsOKjv+Kjv+Kjv+KggOKggOKggOKigOKjv+Kjv+Kjv+Khh+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7/io7/io7/ioYbioIDioIDio77io7/io7/io7/io6fioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKw4qO/4qO/4qO/4qO34qCA4qCA4qC44qO/4qO/4qO/4qCf4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjvuKjv+Kjv+Kjv+Kju+Khh+KggOKggOKgiOKgi+KgieKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiornio7/io7/io5/ioJ8=")); Console.WriteLine(s);await Task.Delay(5000);Console.WriteLine("oo-ee-oo");await Task.Delay(3500);Console.WriteLine("oo-ee-oo");await Task.Delay(3500);Console.WriteLine("oo-ee-oo");await Task.Delay(4000);Console.WriteLine("oo-ee-oo");await Task.Delay(2000);Console.WriteLine("Miku, Miku, you can call me Miku");await Task.Delay(3250);Console.WriteLine("Blue hair, blue tie, hiding in your Wi-Fi");await Task.Delay(3700);Console.WriteLine("Open secrets, anyone can find me");await Task.Delay(3500);Console.WriteLine("Hear your music running through my mind");await Task.Delay(3000);Console.WriteLine("I'm thinking Miku, Miku oo-ee-oo");await Task.Delay(3750);Console.WriteLine("I'm thinking Miku, Miku oo-ee-oo");await Task.Delay(3750);Console.WriteLine("I'm thinking Miku, Miku oo-ee-oo");await Task.Delay(3750);Console.WriteLine("I'm thinking Miku, Miku oo-ee-oo");await Task.Delay(3750);for (var i=0;i<400;i++){ Console.WriteLine(s);await Task.Delay(251-((i/16)*10));}FreeConsole();});
		
		// list of tasks to do
		List<Task> tasks = [];
		
		// start a server instance
		Context = new ServerManager();
		Task.Run(() => Context.StartPipeServer());

		foreach (var path in Directory.GetDirectories(InstanceFolderPath))
		{
			var instanceName = path.Split(@"\")[^1];
			if (instanceName == "Default") continue;
			
			Context.AllBrowserManagers.Add(new InstanceManager(instanceName));
		}
		
		// initialize that new server instance
		Context.DefaultBrowserManager = new InstanceManager("Default");
		Context.AllBrowserManagers.Add(Context.DefaultBrowserManager);
		tasks.Add(Context.DefaultBrowserManager.Initialize());
			
		// start the first browser window of the instance
		if (e.Args.All(string.IsNullOrWhiteSpace))
		{
			Application.Current.Dispatcher.Invoke(() => 
			{
				var firstWindow = new MainWindow(Context.DefaultBrowserManager);
				Context.BrowserWindows.Add(firstWindow);
				firstWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
				firstWindow.Show();
			});
		}
		else
		{
			var url = e.Args.First(s => !string.IsNullOrWhiteSpace(s));
			Application.Current.Dispatcher.Invoke(async () => 
			{ 
				var newWindow = new MainWindow(Context.DefaultBrowserManager); 
				await newWindow._initTask; 
				newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url)); 
				Context.BrowserWindows.Add(newWindow);
				newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
				newWindow.Show(); 
			});
		}
	}
	
	private void StartPipeServer()
	{
		while (true)
		{
			using var server = new NamedPipeServerStream("FoxyBrowser716_Pipe");
			server.WaitForConnection();
			using var reader = new StreamReader(server);
			var message = reader.ReadLine();
			if (message?.StartsWith("NewWindow|")??false)
			{
				var url = message.Replace("NewWindow|", "");
				Application.Current.Dispatcher.Invoke(async () => { 
					var newWindow = new MainWindow(Context.DefaultBrowserManager);
					if (!string.IsNullOrWhiteSpace(url))
					{
						await newWindow._initTask;
						newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
					}
					Context.BrowserWindows.Add(newWindow);
					newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
					newWindow.Show(); 
				});
			}
		}
	}

	public async Task<bool> TryTabTransfer(WebsiteTab tab, double left, double top)
	{
		var pos = new Point(left, top);

		foreach (var window in
		         (from window in BrowserWindows
			         let da = window.GetLeftBarDropArea()
			         let leftBoundS = da.X + 25
			         let topBoundS = da.Y + 25
			         let rightBoundS = da.X + da.Width + 50
			         let bottomBoundS = da.Y + da.Height + 50
			         where pos.X >= leftBoundS && pos.X <= rightBoundS && pos.Y >= topBoundS && pos.Y <= bottomBoundS
			         select window))
		{
			//TODO: modify this to keep the tab dragging
			await window.TabManager.TransferTab(tab);
			return true;
		}
		
		return false;
	}
	
	public async Task CreateWindowFromTab(WebsiteTab tab, Rect finalRect, bool fullscreen, InstanceManager? instance = null)
	{
		var newWindow = new MainWindow(instance ?? Context.DefaultBrowserManager)
		{
			Top = finalRect.Y,
			Left = finalRect.X,
		};
		if (finalRect is { Height: > 50, Width: > 280 })
		{
			newWindow.Height = finalRect.Height;
			newWindow.Width = finalRect.Width;
		}
		
		await newWindow._initTask; 
		newWindow.TabManager.SwapActiveTabTo(await newWindow.TabManager.TransferTab(tab)); 
		Context.BrowserWindows.Add(newWindow);
		newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
		newWindow.Show();

		if (fullscreen)
		{
			newWindow.WindowState = WindowState.Maximized;
		}
	}
	
	public async Task CreateWindow(Rect? finalRect = null, bool? fullscreen = null, InstanceManager? instance = null)
	{
		var newWindow = new MainWindow(instance ?? Context.DefaultBrowserManager);
		if (finalRect is { } fR)
		{
			newWindow.Top = fR.Y;
			newWindow.Left = fR.X;
		}
		
		if (finalRect is { Height: > 50, Width: > 280 } fR2)
		{
			newWindow.Height = fR2.Height;
			newWindow.Width = fR2.Width;
		}
		
		await newWindow._initTask; 
		Context.BrowserWindows.Add(newWindow);
		newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
		newWindow.Show();

		if (fullscreen is true)
		{
			newWindow.WindowState = WindowState.Maximized;
		}
	}
	
	private MainWindow? _lastOpenedWindow;

	public bool DoPositionUpdate(double left, double top)
	{
	    var pos = new Point(left, top);
	    var inExactDropArea = false;

	    foreach (var window in BrowserWindows)
	    {
	        var da = window.GetLeftBarDropArea();
	        
	        var leftBoundS = da.X;
	        var topBoundS = da.Y;
	        var rightBoundS = da.X + da.Width;
	        var bottomBoundS = da.Y + da.Height;
	        
	        var leftBoundB = da.X - 50;
	        var topBoundB = da.Y - 50;
	        var rightBoundB = da.X + da.Width + 100;
	        var bottomBoundB = da.Y + da.Height + 100;
	        
	        if (pos.X >= leftBoundS && pos.X <= rightBoundS && pos.Y >= topBoundS && pos.Y <= bottomBoundS)
	        {
	            inExactDropArea = true;
	        }
	        
	        if (pos.X >= leftBoundB && pos.X <= rightBoundB && pos.Y >= topBoundB && pos.Y <= bottomBoundB)
	        {
	            if (!window.SideOpen)
	            {
	                window.OpenSideBar();
	                _lastOpenedWindow = window;
	            }
	        }
	        else
	        {
	            if (window.SideOpen)
	            {
	                window.CloseSideBar();
	                if (_lastOpenedWindow == window)
	                {
	                    _lastOpenedWindow = null;
	                }
	            }
	        }
	    }

	    if (_lastOpenedWindow != null)
	    {
	        var lastDa = _lastOpenedWindow.GetLeftBarDropArea();
	        var lastLeftBoundB = lastDa.X - 50;
	        var lastTopBoundB = lastDa.Y - 50;
	        var lastRightBoundB = lastDa.X + lastDa.Width + 100;
	        var lastBottomBoundB = lastDa.Y + lastDa.Height + 100;
	        
	        if (!(pos.X >= lastLeftBoundB && pos.X <= lastRightBoundB && pos.Y >= lastTopBoundB && pos.Y <= lastBottomBoundB))
	        {
	            _lastOpenedWindow.CloseSideBar();
	            _lastOpenedWindow = null;
	        }
	    }

	    return inExactDropArea;
	}
}