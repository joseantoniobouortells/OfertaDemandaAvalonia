using Microsoft.Extensions.DependencyInjection;
using OfertaDemanda.Mobile.ViewModels;

using System;

namespace OfertaDemanda.Mobile.Views;

public partial class FirmPage : ContentView
{
    public FirmPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FirmPage InitializeComponent failed: {ex}");
            throw;
        }
    }
}
