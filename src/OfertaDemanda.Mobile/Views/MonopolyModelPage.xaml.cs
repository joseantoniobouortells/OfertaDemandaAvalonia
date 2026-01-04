using System;

namespace OfertaDemanda.Mobile.Views;

public partial class MonopolyModelPage : ContentView
{
    public MonopolyModelPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MonopolyModelPage InitializeComponent failed: {ex}");
            throw;
        }
    }
}
