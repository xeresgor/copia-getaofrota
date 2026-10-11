using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IMovimentacaoService
{
    Task<List<Movimentacao>> ListarAsync();

    /// <summary>Salva na API; sem internet, guarda no aparelho e envia depois.</summary>
    Task<Movimentacao> CriarAsync(Movimentacao nova, FileResult? comprovante);

    Task<int> SincronizarPendentesAsync();
}

public class MovimentacaoService : IMovimentacaoService
{
    private readonly HttpClient _http;
    private readonly IMovimentacaoLocalStore _local;
    private readonly IConnectivityService _conectividade;

    public MovimentacaoService(HttpClient http, IMovimentacaoLocalStore local, IConnectivityService conectividade)
    {
        _http = http;
        _local = local;
        _conectividade = conectividade;
    }

    public async Task<List<Movimentacao>> ListarAsync()
    {
        if (_conectividade.EstaOnline())
        {
            try
            {
                await SincronizarPendentesAsync();
                var daApi = await _http.GetFromJsonAsync<List<Movimentacao>>("movimentacoes");
                if (daApi is not null) await _local.SubstituirDoServidorAsync(daApi);
            }
            catch (HttpRequestException)
            {
                // API fora do ar: segue com o que está no aparelho.
            }
        }

        return await _local.ListarAsync();
    }

    public async Task<Movimentacao> CriarAsync(Movimentacao nova, FileResult? comprovante)
    {
        if (nova.Valor <= 0)
            throw new InvalidOperationException("O valor deve ser maior que zero.");

        if (nova.Status == StatusMovimentacao.Pago && nova.DataPagamento is null)
            throw new InvalidOperationException("Informe a data do pagamento.");

        if (nova.Status == StatusMovimentacao.Pendente)
            nova.DataPagamento = null;

        if (comprovante is not null)
            nova.ComprovanteLocal = await GuardarFotoAsync(comprovante);

        if (!_conectividade.EstaOnline())
        {
            nova.PendenteSincronizacao = true;
            await _local.SalvarAsync(nova);
            return nova;
        }

        var salva = await EnviarAsync(nova);
        await _local.SalvarAsync(salva);
        return salva;
    }

    public async Task<int> SincronizarPendentesAsync()
    {
        var enviados = 0;

        foreach (var pendente in await _local.ListarPendentesAsync())
        {
            var salva = await EnviarAsync(pendente);
            await _local.RemoverAsync(pendente.Id);   // o Id definitivo é o do servidor
            await _local.SalvarAsync(salva);
            enviados++;
        }

        return enviados;
    }

    private async Task<Movimentacao> EnviarAsync(Movimentacao m)
    {
        var resposta = await _http.PostAsJsonAsync("movimentacoes", new
        {
            tipo = m.Tipo,
            valor = m.Valor,
            data = m.Data,
            descricao = m.Descricao,
            categoriaId = m.CategoriaId,
            formaPagamentoId = m.FormaPagamentoId,
            status = m.Status,
            dataVencimento = m.DataVencimento,
            dataPagamento = m.DataPagamento
        });

        await GarantirSucessoAsync(resposta);

        var salva = await resposta.Content.ReadFromJsonAsync<Movimentacao>()
            ?? throw new InvalidOperationException("Resposta inválida da API.");

        if (!string.IsNullOrEmpty(m.ComprovanteLocal) && File.Exists(m.ComprovanteLocal))
        {
            salva.ComprovanteUrl = await EnviarComprovanteAsync(salva.Id, m.ComprovanteLocal);
            File.Delete(m.ComprovanteLocal);
        }

        return salva;
    }

    private async Task<string?> EnviarComprovanteAsync(Guid id, string caminho)
    {
        await using var fluxo = File.OpenRead(caminho);
        using var conteudo = new MultipartFormDataContent
        {
            { new StreamContent(fluxo), "arquivo", Path.GetFileName(caminho) }
        };

        var resposta = await _http.PostAsync($"movimentacoes/{id}/comprovante", conteudo);
        await GarantirSucessoAsync(resposta);

        var corpo = await resposta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return corpo is not null && corpo.TryGetValue("comprovanteUrl", out var url) ? url : null;
    }

    private static async Task<string> GuardarFotoAsync(FileResult foto)
    {
        var destino = Path.Combine(FileSystem.AppDataDirectory, $"comprovante_{Guid.NewGuid()}{Path.GetExtension(foto.FileName)}");
        await using var origem = await foto.OpenReadAsync();
        await using var arquivo = File.Create(destino);
        await origem.CopyToAsync(arquivo);
        return destino;
    }

    /// <summary>Mostra a mensagem que a API devolve ({ "mensagem": "..." }).</summary>
    internal static async Task GarantirSucessoAsync(HttpResponseMessage resposta)
    {
        if (resposta.IsSuccessStatusCode) return;

        string? mensagem = null;
        try
        {
            var corpo = await resposta.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            corpo?.TryGetValue("mensagem", out mensagem);
        }
        catch
        {
            // corpo sem JSON: usa a mensagem padrão abaixo
        }

        throw new InvalidOperationException(mensagem ?? $"Erro {(int)resposta.StatusCode} ao falar com a API.");
    }
}
