using FullControls.SystemComponents;
using KursovayaDB.ViewModel;

namespace KursovayaDB.Views;

public partial class WelcomeWindow : FullWindow
{
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = new WelcomeWindowViewModel(mainFrame);
    }
}
