using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MnMMaps;

public sealed class PinEditor : Window
{
    private readonly TextBox name;
    private readonly TextBox notes;
    public string PinName => name.Text.Trim();
    public string PinNotes => notes.Text.Trim();

    public PinEditor(Window owner, MapPin? pin = null)
    {
        Owner = owner;
        Title = pin is null ? "Add personal pin" : "Edit personal pin";
        Width = 390;
        Height = 300;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.FromRgb(29, 35, 41));
        Foreground = Brushes.White;
        FontFamily = new FontFamily("Segoe UI");
        var layout = new Grid { Margin = new Thickness(20) };
        foreach (var height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            layout.RowDefinitions.Add(new RowDefinition { Height = height });
        var nameLabel = new TextBlock { Text = "Pin name", Margin = new Thickness(0, 0, 0, 7) };
        name = new TextBox { Text = pin?.Name ?? "", MaxLength = 100, Margin = new Thickness(0, 0, 0, 12) };
        var notesLabel = new TextBlock { Text = "Notes (optional)", Margin = new Thickness(0, 0, 0, 7) };
        notes = new TextBox { Text = pin?.Notes ?? "", MaxLength = 2000, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        var save = new Button { Content = "Save pin", IsDefault = true };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { name.Focus(); return; }
            DialogResult = true;
        };
        actions.Children.Add(cancel);
        actions.Children.Add(save);
        UIElement[] children = [nameLabel, name, notesLabel, notes, actions];
        for (var i = 0; i < children.Length; i++) { Grid.SetRow(children[i], i); layout.Children.Add(children[i]); }
        Content = layout;
        Loaded += (_, _) => { name.Focus(); name.SelectAll(); };
    }
}
