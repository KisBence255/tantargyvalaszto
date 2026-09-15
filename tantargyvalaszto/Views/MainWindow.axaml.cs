using Avalonia.Controls;
using tantargyvalaszto.ViewModels;

namespace tantargyvalaszto.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}