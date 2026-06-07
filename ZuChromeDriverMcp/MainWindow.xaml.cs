using System.Windows;
using ZuChromeDriverMcp.ViewModels;

namespace ZuChromeDriverMcp;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Closing += (_, _) => viewModel.Settings.PersistToDisk();
    }
}
