using GestaoFrota.App.Models;
using SQLite;

namespace GestaoFrota.App.Services;

/// <summary>Banco SQLite local dos lançamentos (Offline-First).</summary>
public interface IMovimentacaoLocalStore
{
    Task<List<Movimentacao>> ListarAsync();
    Task<List<Movimentacao>> ListarPendentesAsync();
    Task SalvarAsync(Movimentacao movimentacao);
    Task RemoverAsync(Guid id);
    Task SubstituirDoServidorAsync(IEnumerable<Movimentacao> doServidor);
}

public class MovimentacaoLocalStore : IMovimentacaoLocalStore
{
    private SQLiteAsyncConnection? _conexao;

    private async Task<SQLiteAsyncConnection> ObterAsync()
    {
        if (_conexao is not null) return _conexao;
        var caminho = Path.Combine(FileSystem.AppDataDirectory, "gestaofrota_financeiro.db3");
        _conexao = new SQLiteAsyncConnection(caminho);
        await _conexao.CreateTableAsync<Movimentacao>();
        return _conexao;
    }

    public async Task<List<Movimentacao>> ListarAsync() =>
        await (await ObterAsync()).Table<Movimentacao>().OrderByDescending(m => m.Data).ToListAsync();

    public async Task<List<Movimentacao>> ListarPendentesAsync() =>
        await (await ObterAsync()).Table<Movimentacao>().Where(m => m.PendenteSincronizacao).ToListAsync();

    public async Task SalvarAsync(Movimentacao movimentacao) =>
        await (await ObterAsync()).InsertOrReplaceAsync(movimentacao);

    public async Task RemoverAsync(Guid id) =>
        await (await ObterAsync()).DeleteAsync<Movimentacao>(id);

    /// <summary>Troca o cache pelo que veio da API, sem apagar o que ainda espera envio.</summary>
    public async Task SubstituirDoServidorAsync(IEnumerable<Movimentacao> doServidor)
    {
        var conexao = await ObterAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM Movimentacao WHERE PendenteSincronizacao = 0");
            foreach (var m in doServidor) tran.InsertOrReplace(m);
        });
    }
}
