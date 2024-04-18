using FullControls.SystemComponents;
using PriceAnalysis.ViewModel;

namespace PriceAnalysis.Views;

public partial class WelcomeWindow : FullWindow
{
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = new WelcomeWindowViewModel(mainFrame);
    }
}
