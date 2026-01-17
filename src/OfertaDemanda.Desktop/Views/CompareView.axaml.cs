using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OfertaDemanda.Desktop.Views;

public partial class CompareView : UserControl
{
    public CompareView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
