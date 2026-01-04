using System;

namespace OfertaDemanda.Mobile.Views;

public partial class MarketPage : ContentView
{
    public MarketPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MarketPage InitializeComponent failed: {ex}");
            throw;
        }
    }
}
