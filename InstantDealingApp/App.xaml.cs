using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace InstantDealingApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.AppendAllText("crash_log.txt", "AppDomain Unhandled: " + args.ExceptionObject?.ToString() + "\n");
            };
            this.DispatcherUnhandledException += (s, args) =>
            {
                File.AppendAllText("crash_log.txt", "Dispatcher Unhandled: " + args.Exception?.ToString() + "\n");
            };
            base.OnStartup(e);
        }
    }
}
