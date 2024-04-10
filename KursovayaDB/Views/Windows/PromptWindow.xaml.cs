using FullControls.SystemComponents;
using System.Windows;

namespace KursovayaDB.Views.Windows;

public partial class PromptWindow : FlexWindow
{
    public PromptWindow()
    {
        InitializeComponent();
    }

    public bool Result { get; private set; }
    public bool WasClosedByUser { get; private set; }
    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        WasClosedByUser = true;
        Close();
    }

    private void BtnLast_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        WasClosedByUser = true;
        Close();
    }

    private void FlexWindow_Closed(object sender, EventArgs e)
    {
        if (!WasClosedByUser)
        {
            Result = false;
        }
    }
}
