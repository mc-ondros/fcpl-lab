using Avalonia.Controls;

namespace Capstone.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ArcadeViewModel();
    }
}
