﻿using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using FoxyBrowser716.ErrorHandling;

namespace FoxyBrowser716;

public partial class App : Application
{
    private static Mutex? _mutex;
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // you see nothing here:
		if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
			Task.Run(async () => { AllocConsole(); Console.OutputEncoding = Encoding.UTF8; Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.DarkCyan; Console.Title = "MikuBrowser01"; Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String("4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qCm4qOE4qOg4qC04qCS4qCS4qCJ4qCS4qC24qOE4qGP4qO24qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjoOKhnuKjoeKggOKioOKhgOKggOKipuKhgOKiueKjt+KjvOKjhOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7jiob/ioIHio7/ioJ/io7fio7/ioqbioYjio4fioIDiorvio7/io4jiorfioYTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qO/4qGH4qO24qO74qOA4qCY4qO/4qOA4qO54qO/4qOw4qO84qGf4qOv4qCf4qO/4qGE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjv+Khh+Kjv+Khv+Kgv+Kgi+KgieKgi+Kgm+Kiv+Kjv+Kjv+Khh+KgiOKipuKhuOKjvuKjhuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjio7/ioYfioJvio7fio4TioIDioIDioIDioIDio7jioJ/ioIPioInioIDioIjiorfio6niobvio4TioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK44qO/4qC54qGE4qCI4qCZ4qCS4qKk4qO04qO+4qCJ4qCB4qOA4qGA4qCA4qCA4qCA4qC54qOf4qKu4qGz4qOE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjvOKjv+KggOKjv+KggOKjgOKjtOKjv+Khj+KjgOKhrOKgn+KggeKgiOKjhuKggOKggOKggOKgmOKip+KhieKiv+Kjp+KhgOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioqDiob/io7/ioIDiorvio6vioInio73ioZ/ioInioYXiooDio4bioIDioqDio7/ioIDioIDioIDioIDioIDioLnio6bioYnioLvioqbioYDioYTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO44qKX4qO/4qKA4qG34qCB4qO44qG/4qCB4qCA4qCA4qK44qO/4qCD4qOE4qOI4qGG4qCA4qCA4qCA4qCA4qCA4qCI4qCr4qOx4qCm4qOM4qGT4qKk4qOA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKisOKjh+KjvuKjv+Kin+Khh+KigOKjv+Khh+KggOKigOKjsOKiuOKhjOKiv+Kjv+Kiu+Khn+Kjg+KggOKggOKggOKgsOKhguKgoOKgjOKgsuKipOKjmeKju+Kgm+Kgk+KgtuKipuKhpOKjhOKjgOKhgOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio6Dio7/io7vio7/ioI/io77ioIPiorjior3ioYfioIDioY/ioIHiobbioIHioIjio7/io47iobfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioInioInioJvioJLioLbioqTio4DioIjioLvio4TioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKg4qO04qO/4qCf4qCB4qG84qCA4qO/4qCA4qGf4qK44qGH4qKw4qCD4qK44qCH4qCA4qCA4qK54qO34qO/4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK74qO34qOE4qCY4qOn4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKigOKjgOKjpOKjvuKgn+Kgi+KggOKjoOKhvOKigeKjvOKjv+KioOKjp+KivuKhh+KgmOKhgOKguOKhh+KggOKggOKgmOKjv+Kjv+Kjt+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKhieKgmuKip+KgueKjt+KggArioIDioIDioIDioIDioIDioIDioIDioIDioIDiooDio6DiobTioJbioovio73iob/ioKXioJbioJrioIniooHio7Tio77io7/ioIPioojioIDiorjioIfioIDioIHioIDiorPioYDioIDioIDio7/io7/io73ioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7jioIDioIDioJjioqfioYfioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qOA4qOk4qO+4qCb4qCB4qOg4qG84qCL4qCB4qCA4qKA4qOA4qKA4qO04qO/4qO/4qCf4qCB4qCA4qCA4qO34qCL4qCC4qCA4qCA4qCA4qCA4qCz4qGA4qCA4qK54qO/4qO/4qGG4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qG/4qCA4qCA4qCA4qK44qCn4qCACuKggOKggOKigOKhoOKgluKgi+KjqeKgtuKii+KhtOKgi+KggeKggOKggOKgv+KjreKhv+Kjv+Kgv+Kgn+KiieKjt+KjhOKggOKigOKjvOKjv+KhhOKggOKggOKggOKggOKggOKigOKjueKghOKggOKiueKjv+Khh+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgg+KggOKggOKggOKggOKgkOKhpwrioIDioIDio7jio4Tio7Tio5vio6HioJ7ioIvioIDioIDioIDioIDioIDioIDioIDio63io6Tio7Tio77io7/io7/io7/io7fio7/io7/io7/io7fio6Tio7Tio7bio77io7/io7/io7/io4bioIDioojio7/io7fio6TioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioLjiopEK4qCA4qCw4qOP4qG/4qKr4qGe4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCI4qC74qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qO/4qOm4qO54qC/4qC/4qCf4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKACuKggOKigOKjv+KhteKgi+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgieKgmeKjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kju+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kjv+Kgm+KggOKggOKggOKggOKggOKggeKggOKggOKggOKggOKggOKggOKggOKigOKhnOKggOKggOKggOKggOKggOKggOKigOKgjgrioqDiorjioZ/ioLnioYDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjioafioKTiopnioJvioIvioJvioJvioL/ioL/ior/ioZ/ioJvioInioIHioIDioIDioIDioIDioIDioKDioJDioIDioIDioIDioIDioIDioIDioIDiooDiobziooDioIDioITioIDioIDioYDioYDioIDioIAK4qK44qOO4qGH4qCA4qCz4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK34qCA4qGA4qKw4qGF4qCA4qCA4qCA4qCA4qCA4qGd4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qO+4qG/4qKL4qCe4qCA4qKg4qOu4qCO4qCA4qCA4qCACuKggOKiv+Kjt+KggOKggOKgiOKgguKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgmOKjv+KjtuKjv+Kjh+KggOKggOKggOKggOKjsOKgg+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKioOKhvuKgi+KjoOKgi+KigOKjtOKgn+KggeKggOKggOKggOKggArioIDioJjio4/ioqfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorvio7/io7/io7/iornio7vio7/io7/ioIfioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7DioJ/ioqHioJ7io4Hio7TioJ/ioIHioIDioIDioIDioIDioIDioIAK4qCA4qCA4qC44qGE4qCz4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCz4qKE4qGA4qCA4qCA4qCY4qO/4qO/4qGv4qK24qKn4qO/4qGP4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKA4qG+4qKB4qO04qC/4qCa4qCJ4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKgueKhhOKgmeKipuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgmeKgouKjhOKgiOKiv+Kjv+Khr+KjneKjvuKjv+KggeKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjoOKjtOKhr+KiluKhv+KgieKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioLnioYTioIDioJHiooTioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIniorvio7/io7/io73io7/ioYfioIDioIDioIDioIDioIDioIDioIDio4Dio4Dio6TiorTio7bioL/ioIvioIHio7DioIvioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCY4qKm4qGA4qCA4qCZ4qCi4qOE4qGA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO/4qO74qK/4qO/4qGN4qCR4qCS4qCS4qOS4qO+4qO34qC/4qCb4qOL4qG14qCa4qCB4qCA4qOg4qCe4qCB4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKgmeKgouKjhOKhgOKggOKgmeKgk+KgkuKggOKggOKjgOKhgOKggOKggOKggOKggOKggOKggOKigOKjv+Kjv+Kjv+Kjv+Kjt+KhmuKgm+KgieKggeKggOKggOKgkOKgi+KigOKhgOKigOKhpOKgnuKggeKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIjioInioInioJnioIvioJvioJvio7nio7/io7/io7/io7/io7/io7fioYDioIDioIDioIDioIDioIDioIDioJvioIvioInioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO/4qO/4qO74qO/4qO/4qO/4qO/4qOH4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjv+Kjv+Kjv+Kjv+Kiv+Kjv+Kjv+Kjv+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiooDio7/io7/io7/ioY/ioLjio7/io7/io7/ioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qK44qO/4qO/4qO/4qCB4qCA4qK54qO/4qO/4qGE4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKiuOKjv+Kjv+Khj+KggOKggOKgiOKjv+Kjv+KhhuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiorjio7/io7/ioIDioIDioIDioIDiorzio7/io7fioYDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qO84qO/4qO/4qCA4qCA4qCA4qCA4qO84qO/4qO/4qGH4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKgsOKjv+Kjv+Kjv+KggOKggOKggOKigOKjv+Kjv+Kjv+Khh+KggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDio7/io7/io7/ioYbioIDioIDio77io7/io7/io7/io6fioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIAK4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qKw4qO/4qO/4qO/4qO34qCA4qCA4qC44qO/4qO/4qO/4qCf4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCA4qCACuKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKjvuKjv+Kjv+Kjv+Kju+Khh+KggOKggOKgiOKgi+KgieKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggOKggArioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDioIDiornio7/io7/io5/ioJ8="))); await Task.Delay(500); Console.WriteLine(InfoGetter.AppName); await Task.Delay(500); await Task.Delay(500); Console.WriteLine("Lead Dev: Vast The Doge (William Herbert)"); await Task.Delay(500); Console.WriteLine("Contributors: FoxyGuy716"); await Task.Delay(500); Console.WriteLine(InfoGetter.GitHubURL); await Task.Delay(500); });
        
        //need this to be able to compete with unoptimized games to prevent freezes in some sites like youtube music:
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

        
#if DEBUG
        _mutex = new Mutex(true, "DEBUG_FoxyBrowser716_Mutex", out var isNewInstance);
#else
        _mutex = new Mutex(true, "FoxyBrowser716_Mutex", out var isNewInstance);
#endif

        if (!isNewInstance)
        {
            SendMessage($"NewWindow|{e.Args.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))}");
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        Current.Exit += OnApplicationExit;

        ServerManager.RunServer(e);
    }

    private void OnApplicationExit(object sender, ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
    }

    private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var errorPopup = new ErrorPopup(e);
            errorPopup.ShowDialog();
        });
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex) return;
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            var errorPopup = new ErrorPopup(ex);
            errorPopup.ShowDialog();
        });
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var errorPopup = new ErrorPopup(e);
            errorPopup.ShowDialog();
        });
        e.SetObserved();
    }

    static void SendMessage(string message)
    {
#if DEBUG
        using var client = new NamedPipeClientStream(".", "DEBUG_FoxyBrowser716_Pipe", PipeDirection.Out);
#else
        using var client = new NamedPipeClientStream(".", "FoxyBrowser716_Pipe", PipeDirection.Out);
#endif
        client.Connect(1000);
        using var writer = new System.IO.StreamWriter(client);
        writer.WriteLine(message);
        writer.Flush();
    }
}