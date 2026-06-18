using Avalonia.Controls;
using Avalonia.Input;
using VstDeleter.ViewModels;

namespace VstDeleter.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();
        vm.SetTopLevel(this);
        DataContext = vm;

        // Przeciąganie okna za pasek tytułu
        if (this.FindControl<Border>("TitleBarDragZone") is { } drag)
        {
            drag.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(e);
            };
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}