using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OfertaDemanda.Desktop.Views;

public partial class FirmView : UserControl
{
    public FirmView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
