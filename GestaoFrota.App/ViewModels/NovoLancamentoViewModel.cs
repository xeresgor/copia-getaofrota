using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class NovoLancamentoViewModel : BaseViewModel
{
    private readonly IMovimentacaoService _servico;
    private readonly ICategoriaService _categorias;
    private readonly IFormaPagamentoService _formas;
    private List<Categoria> _todasCategorias = new();
    private FileResult? _comprovante;

    public ObservableCollection<Categoria> CategoriasDoTipo { get; } = new();
    public ObservableCollection<FormaPagamento> Formas { get; } = new();
    public List<TipoMovimentacao> Tipos { get; } = new() { TipoMovimentacao.Receita, TipoMovimentacao.Despesa };
    public List<StatusMovimentacao> Situacoes { get; } = new() { StatusMovimentacao.Pendente, StatusMovimentacao.Pago };

    [ObservableProperty] private TipoMovimentacao tipo = TipoMovimentacao.Despesa;
    [ObservableProperty] private string valorTexto = string.Empty;
    [ObservableProperty] private string? descricao;
    [ObservableProperty] private Categoria? categoriaSelecionada;
    [ObservableProperty] private FormaPagamento? formaSelecionada;
    [ObservableProperty] private StatusMovimentacao status = StatusMovimentacao.Pendente;
    [ObservableProperty] private DateTime dataVencimento = DateTime.Today;
    [ObservableProperty] private DateTime dataPagamento = DateTime.Today;
    [ObservableProperty] private string comprovanteTexto = "Nenhum comprovante anexado.";

    public bool EstaPago => Status == StatusMovimentacao.Pago;

    public NovoLancamentoViewModel(IMovimentacaoService servico, ICategoriaService categorias, IFormaPagamentoService formas)
    {
        _servico = servico;
        _categorias = categorias;
        _formas = formas;
    }

    partial void OnTipoChanged(TipoMovimentacao value) => FiltrarCategorias();

    partial void OnStatusChanged(StatusMovimentacao value) => OnPropertyChanged(nameof(EstaPago));

    private void FiltrarCategorias()
    {
        CategoriasDoTipo.Clear();
        var tipoCategoria = Tipo == TipoMovimentacao.Receita ? TipoCategoria.Receita : TipoCategoria.Despesa;
        foreach (var c in _todasCategorias.Where(c => c.Ativo && c.Tipo == tipoCategoria)) CategoriasDoTipo.Add(c);
        CategoriaSelecionada = null;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        _todasCategorias = await _categorias.ListarAsync();
        FiltrarCategorias();

        Formas.Clear();
        foreach (var f in (await _formas.ListarAsync()).Where(f => f.Ativo)) Formas.Add(f);
    }

    [RelayCommand]
    private async Task TirarFotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                MensagemErro = "Este aparelho não tem câmera disponível.";
                return;
            }

            Anexar(await MediaPicker.Default.CapturePhotoAsync());
        }
        catch (Exception erro)
        {
            MensagemErro = "Não foi possível abrir a câmera: " + erro.Message;
        }
    }

    [RelayCommand]
    private async Task EscolherDaGaleriaAsync()
    {
        try
        {
            Anexar(await MediaPicker.Default.PickPhotoAsync());
        }
        catch (Exception erro)
        {
            MensagemErro = "Não foi possível abrir a galeria: " + erro.Message;
        }
    }

    private void Anexar(FileResult? foto)
    {
        if (foto is null) return;
        _comprovante = foto;
        ComprovanteTexto = "Anexado: " + foto.FileName;
        MensagemErro = null;
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        MensagemErro = null;
        var valor = Formato.LerDinheiro(ValorTexto);

        if (valor <= 0) { MensagemErro = "O valor deve ser maior que zero."; return; }
        if (CategoriaSelecionada is null) { MensagemErro = "Escolha uma categoria."; return; }
        if (FormaSelecionada is null) { MensagemErro = "Escolha uma forma de pagamento."; return; }

        Carregando = true;

        try
        {
            await _servico.CriarAsync(new Movimentacao
            {
                Tipo = Tipo,
                Valor = valor,
                Descricao = string.IsNullOrWhiteSpace(Descricao) ? null : Descricao.Trim(),
                CategoriaId = CategoriaSelecionada.Id,
                CategoriaNome = CategoriaSelecionada.Nome,
                FormaPagamentoId = FormaSelecionada.Id,
                FormaPagamentoNome = FormaSelecionada.Nome,
                Status = Status,
                DataVencimento = DataVencimento,
                DataPagamento = Status == StatusMovimentacao.Pago ? DataPagamento : null
            }, _comprovante);

            await Shell.Current.GoToAsync("..");
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
