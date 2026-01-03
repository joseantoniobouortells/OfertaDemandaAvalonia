using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OfertaDemanda.Desktop.Views;

public partial class MarketView : UserControl
{
    public MarketView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
