using GestaoFrota.App.Services;
using GestaoFrota.App.ViewModels;
using GestaoFrota.App.Views;
using Microsoft.Extensions.Logging;

namespace GestaoFrota.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ---------- Infraestrutura local ----------
        builder.Services.AddSingleton<ITokenStore, TokenStore>();
        builder.Services.AddSingleton<ILocalDatabaseService, LocalDatabaseService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();

        // HttpClient "público" (sem token), usado só para o login Google.
        builder.Services.AddHttpClient<IAuthService, GoogleAuthService>(client =>
        {
            client.BaseAddress = new Uri(AppConfig.ApiBaseUrl);
        });

        // HttpClient autenticado: anexa o JWT automaticamente via AuthHeaderHandler.
        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services
            .AddHttpClient("ApiAutenticada", client => client.BaseAddress = new Uri(AppConfig.ApiBaseUrl))
            .AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiAutenticada"));

        builder.Services.AddScoped<ICategoriaService, CategoriaService>();
        builder.Services.AddScoped<IFormaPagamentoService, FormaPagamentoService>();
        builder.Services.AddScoped<IUsuarioService, UsuarioService>();
        builder.Services.AddSingleton<IMovimentacaoLocalStore, MovimentacaoLocalStore>();
        builder.Services.AddScoped<IMovimentacaoService, MovimentacaoService>();
        builder.Services.AddScoped<IDividaService, DividaService>();

        // ---------- ViewModels ----------
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CategoriasViewModel>();
        builder.Services.AddTransient<FormasPagamentoViewModel>();
        builder.Services.AddTransient<UsuariosViewModel>();
        builder.Services.AddTransient<FinanceiroViewModel>();
        builder.Services.AddTransient<NovoLancamentoViewModel>();
        builder.Services.AddTransient<DividasViewModel>();

        // ---------- Páginas ----------
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CategoriasPage>();
        builder.Services.AddTransient<FormasPagamentoPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<UsuariosPage>();
        builder.Services.AddTransient<FinanceiroPage>();
        builder.Services.AddTransient<NovoLancamentoPage>();
        builder.Services.AddTransient<DividasPage>();

        return builder.Build();
    }
}
