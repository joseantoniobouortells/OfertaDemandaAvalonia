using System;

namespace OfertaDemanda.Mobile.Views;

public partial class MonopolyWelfarePage : ContentView
{
    public MonopolyWelfarePage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MonopolyWelfarePage InitializeComponent failed: {ex}");
            throw;
        }
    }
}
