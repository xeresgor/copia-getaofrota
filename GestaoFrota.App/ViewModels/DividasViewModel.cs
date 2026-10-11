using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class DividasViewModel : BaseViewModel
{
    private readonly IDividaService _servico;

    public ObservableCollection<Divida> Dividas { get; } = new();

    [ObservableProperty] private string descricao = string.Empty;
    [ObservableProperty] private string valorTotalTexto = string.Empty;
    [ObservableProperty] private string parcelasTexto = "12";
    [ObservableProperty] private DateTime primeiroVencimento = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string quitacaoTexto = string.Empty;
    [ObservableProperty] private string? mensagemSucesso;

    public DividasViewModel(IDividaService servico)
    {
        _servico = servico;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Carregando = true;

        try
        {
            Dividas.Clear();
            foreach (var d in await _servico.ListarAsync()) Dividas.Add(d);
        }
        catch (Exception erro)
        {
            MensagemErro = erro.Message;
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        MensagemErro = null;
        MensagemSucesso = null;

        var total = Formato.LerDinheiro(ValorTotalTexto);
        var quitacao = Formato.LerDinheiro(QuitacaoTexto);

        if (string.IsNullOrWhiteSpace(Descricao)) { MensagemErro = "Informe a descrição."; return; }
        if (total <= 0) { MensagemErro = "O valor total deve ser maior que zero."; return; }
        if (!int.TryParse(ParcelasTexto, out var parcelas) || parcelas < 1 || parcelas > 600)
        { MensagemErro = "O número de parcelas deve ficar entre 1 e 600."; return; }
        if (quitacao <= 0 || quitacao > total)
        { MensagemErro = "A quitação antecipada deve ser maior que zero e até o valor total."; return; }

        Carregando = true;

        try
        {
            await _servico.CriarAsync(Descricao.Trim(), total, parcelas, PrimeiroVencimento, quitacao);

            Descricao = ValorTotalTexto = QuitacaoTexto = string.Empty;
            MensagemSucesso = $"Dívida salva. {parcelas} parcelas pendentes foram geradas.";
            await CarregarAsync();
        }
        catch (Exception erro)
        {
            MensagemErro = erro.Message;
        }
        finally
        {
            Carregando = false;
        }
    }
}
