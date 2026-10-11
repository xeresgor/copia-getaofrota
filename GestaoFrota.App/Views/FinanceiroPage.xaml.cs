using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class FinanceiroPage : ContentPage
{
    private readonly FinanceiroViewModel _viewModel;

    public FinanceiroPage(FinanceiroViewModel viewModel)
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
