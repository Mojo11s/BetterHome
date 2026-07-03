using System.Configuration;
using System.Data;
using System.Windows;

namespace BetterHome;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "runtime-error.txt");
            System.IO.File.WriteAllText(path, e.Exception.ToString());
            e.Handled = true;
        };
    }
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "startup-error.txt");
            System.IO.File.WriteAllText(path, ex.ToString());
            MessageBox.Show(ex.Message, "BetterHome couldn't start");
            Shutdown(1);
        }
    }
}

