using System;
using System.Windows;

namespace FactionsLauncher
{
    public partial class App : Application
    {
        public static Exception CriticalError(string message)
        {
            MessageBox.Show(message, "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
            return new ApplicationException();
        }

        public static void Notify(string message)
        {
            MessageBox.Show(message, "Launcher", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
