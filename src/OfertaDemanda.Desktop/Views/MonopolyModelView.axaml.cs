using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OfertaDemanda.Desktop.Views;

public partial class MonopolyModelView : UserControl
{
    public MonopolyModelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
