using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class DividasPage : ContentPage
{
    private readonly DividasViewModel _viewModel;

    public DividasPage(DividasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }
}
