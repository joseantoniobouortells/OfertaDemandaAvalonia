using System;

namespace OfertaDemanda.Mobile.Views;

public partial class ElasticityPage : ContentView
{
    public ElasticityPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ElasticityPage InitializeComponent failed: {ex}");
            throw;
        }
    }
}
