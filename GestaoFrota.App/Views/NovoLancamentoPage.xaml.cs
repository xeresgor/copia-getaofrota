using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class NovoLancamentoPage : ContentPage
{
    private readonly NovoLancamentoViewModel _viewModel;

    public NovoLancamentoPage(NovoLancamentoViewModel viewModel)
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
