using System.Globalization;
using SQLite;

namespace GestaoFrota.App.Models;

public enum TipoMovimentacao { Receita = 1, Despesa = 2 }

public enum StatusMovimentacao { Pendente = 1, Pago = 2 }

public static class Formato
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", Br);

    public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", Br);

    /// <summary>Aceita "1.234,56" ou "1234.56". Devolve 0 se não for número.</summary>
    public static decimal LerDinheiro(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 0;
        var limpo = texto.Replace("R$", "").Trim();
        if (decimal.TryParse(limpo, NumberStyles.Number, Br, out var valor)) return valor;
        return decimal.TryParse(limpo, NumberStyles.Number, CultureInfo.InvariantCulture, out valor) ? valor : 0;
    }
}

/// <summary>Lançamento financeiro. Também é a tabela local (cache offline).</summary>
public class Movimentacao
{
    [PrimaryKey] public Guid Id { get; set; } = Guid.NewGuid();
    public TipoMovimentacao Tipo { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;
    public string? Descricao { get; set; }
    public StatusMovimentacao Status { get; set; }
    public DateTime DataVencimento { get; set; } = DateTime.Today;
    public DateTime? DataPagamento { get; set; }
    public string? ComprovanteUrl { get; set; }
    public Guid CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;
    public Guid FormaPagamentoId { get; set; }
    public string FormaPagamentoNome { get; set; } = string.Empty;
    public bool Estornada { get; set; }

    /// <summary>True quando foi criado offline e ainda não chegou à API.</summary>
    [Indexed] public bool PendenteSincronizacao { get; set; }

    /// <summary>Caminho da foto guardada no aparelho, ainda não enviada.</summary>
    public string? ComprovanteLocal { get; set; }

    [Ignore] public string Titulo => string.IsNullOrWhiteSpace(Descricao) ? CategoriaNome : Descricao!;
    [Ignore] public string ValorTexto => (Tipo == TipoMovimentacao.Receita ? "+ " : "- ") + Formato.Moeda(Valor);
    [Ignore] public string SituacaoTexto => Status == StatusMovimentacao.Pago
        ? (Tipo == TipoMovimentacao.Receita ? "Recebido" : "Pago")
        : "Pendente";
    [Ignore] public string QuandoTexto => Status == StatusMovimentacao.Pendente
        ? "Vence em " + Formato.Data(DataVencimento)
        : Formato.Data(DataPagamento ?? Data);
    [Ignore] public string Detalhe => $"{CategoriaNome} · {FormaPagamentoNome} · {QuandoTexto}"
        + (PendenteSincronizacao ? " · aguardando envio" : "");
}

public class ProjecaoMes
{
    public string Mes { get; set; } = string.Empty;          // "2026-11"
    public decimal Pendentes { get; set; }
    public decimal SaldoPrevisto { get; set; }

    public string MesTexto => Mes.Length == 7 ? $"{Mes[5..]}/{Mes[..4]}" : Mes;
    public string PendentesTexto => Formato.Moeda(Pendentes);
    public string SaldoTexto => Formato.Moeda(SaldoPrevisto);
}

public class Divida
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public int NumeroParcelas { get; set; }
    public int ParcelasPagas { get; set; }
    public decimal ValorParcela { get; set; }
    public decimal SaldoDevedor { get; set; }
    public decimal ValorQuitacao { get; set; }
    public DateTime DataInicio { get; set; }

    public string ParcelasTexto => $"{NumeroParcelas} parcelas de {Formato.Moeda(ValorParcela)}";
    public string DetalheTexto => $"1ª parcela em {Formato.Data(DataInicio)} · saldo devedor {Formato.Moeda(SaldoDevedor)}";
    public string QuitacaoTexto => "Quitação antecipada: " + Formato.Moeda(ValorQuitacao);
}
