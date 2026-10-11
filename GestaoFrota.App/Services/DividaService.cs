using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IDividaService
{
    Task<List<Divida>> ListarAsync();

    /// <summary>A API gera as N parcelas pendentes numa transação. Exige conexão.</summary>
    Task<Divida> CriarAsync(string descricao, decimal valorTotal, int parcelas, DateTime primeiroVencimento, decimal valorQuitacao);
}

public class DividaService : IDividaService
{
    private readonly HttpClient _http;
    private readonly IConnectivityService _conectividade;

    public DividaService(HttpClient http, IConnectivityService conectividade)
    {
        _http = http;
        _conectividade = conectividade;
    }

    public async Task<List<Divida>> ListarAsync()
    {
        if (!_conectividade.EstaOnline()) return new List<Divida>();
        return await _http.GetFromJsonAsync<List<Divida>>("dividas") ?? new List<Divida>();
    }

    public async Task<Divida> CriarAsync(string descricao, decimal valorTotal, int parcelas, DateTime primeiroVencimento, decimal valorQuitacao)
    {
        if (!_conectividade.EstaOnline())
            throw new InvalidOperationException("Cadastrar dívidas exige conexão com a internet.");

        var resposta = await _http.PostAsJsonAsync("dividas", new
        {
            descricao,
            valorTotal,
            numeroParcelas = parcelas,
            dataInicio = primeiroVencimento,
            valorQuitacao
        });

        await MovimentacaoService.GarantirSucessoAsync(resposta);

        return await resposta.Content.ReadFromJsonAsync<Divida>()
            ?? throw new InvalidOperationException("Resposta inválida da API.");
    }
}
