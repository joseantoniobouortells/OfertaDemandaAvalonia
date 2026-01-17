namespace OfertaDemanda.Desktop.ViewModels;

public abstract record NavigationItem(string Title, string Icon);

public sealed record PerfectCompetitionNavigationItem(string Title, string Icon) : NavigationItem(Title, Icon);

public sealed record MonopolyNavigationItem(string Title, string Icon) : NavigationItem(Title, Icon);

public sealed record SettingsNavigationItem(string Title, string Icon) : NavigationItem(Title, Icon);

public sealed record AboutNavigationItem(string Title, string Icon) : NavigationItem(Title, Icon);
