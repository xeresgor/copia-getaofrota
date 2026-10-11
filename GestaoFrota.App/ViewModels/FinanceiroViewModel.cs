using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;
using GestaoFrota.App.Views;

namespace GestaoFrota.App.ViewModels;

/// <summary>Painel financeiro: saldo atual (só pagos), projeção por mês e lista de lançamentos.</summary>
public partial class FinanceiroViewModel : BaseViewModel
{
    private readonly IMovimentacaoService _servico;

    public ObservableCollection<Movimentacao> Lancamentos { get; } = new();
    public ObservableCollection<ProjecaoMes> Projecao { get; } = new();

    [ObservableProperty]
    private string saldoTexto = Formato.Moeda(0);

    public FinanceiroViewModel(IMovimentacaoService servico)
    {
        _servico = servico;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var todos = (await _servico.ListarAsync()).Where(m => !m.Estornada).ToList();

            Lancamentos.Clear();
            foreach (var m in todos.OrderByDescending(m => m.Data)) Lancamentos.Add(m);

            // Saldo atual: só Pago/Recebido.
            var saldo = Resultado(todos.Where(m => m.Status == StatusMovimentacao.Pago));
            SaldoTexto = Formato.Moeda(saldo);

            // Projeção: saldo atual + pendentes, mês a mês pelo vencimento.
            Projecao.Clear();
            var acumulado = saldo;

            foreach (var mes in todos.Where(m => m.Status == StatusMovimentacao.Pendente)
                         .GroupBy(m => m.DataVencimento.ToString("yyyy-MM"))
                         .OrderBy(g => g.Key))
            {
                var total = Resultado(mes);
                acumulado += total;
                Projecao.Add(new ProjecaoMes { Mes = mes.Key, Pendentes = total, SaldoPrevisto = acumulado });
            }
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

    private static decimal Resultado(IEnumerable<Movimentacao> lista) =>
        lista.Sum(m => m.Tipo == TipoMovimentacao.Receita ? m.Valor : -m.Valor);

    [RelayCommand]
    private Task NovoLancamentoAsync() => Shell.Current.GoToAsync(nameof(NovoLancamentoPage));
}
