# App MAUI: lançamentos, dívidas e painel financeiro

Copie as pastas para dentro do GestaoFrota.App do repositório. Os 3 arquivos marcados "substitui" já vêm com as alterações; os demais são novos. Não foi compilado: o .NET não roda no ambiente onde foi escrito.

## Novos
- Models/Financeiro.cs
- Services/MovimentacaoLocalStore.cs, MovimentacaoService.cs, DividaService.cs
- ViewModels/FinanceiroViewModel.cs, NovoLancamentoViewModel.cs, DividasViewModel.cs
- Views/FinanceiroPage, NovoLancamentoPage, DividasPage (.xaml e .xaml.cs)

## Substitui
- AppShell.xaml (abas Financeiro e Dívidas)
- AppShell.xaml.cs (abas só para Administrador e Financeiro; rota da tela de novo lançamento)
- MauiProgram.cs (registro dos serviços, view models e páginas)

Se alguém do grupo mexeu nesses 3 arquivos depois da main, mescle à mão em vez de substituir.

## O que o app faz
- Financeiro: saldo atual (só pagos), projeção por mês de vencimento e lista com situação.
- Novo lançamento: tipo, valor, descrição, categoria, forma, situação, vencimento, data de pagamento (quando pago) e comprovante por câmera ou galeria.
- Dívidas: cadastro com quitação antecipada. A API gera as parcelas pendentes.
- Offline: lançamentos feitos sem internet ficam no SQLite e são enviados quando a conexão volta (ao abrir o Financeiro). Dívidas exigem conexão.
- Motorista e Gestor de frota não veem as abas Financeiro e Dívidas.

## Ajustes manuais para a câmera (Android)
Em Platforms/Android/AndroidManifest.xml, dentro de <manifest>:

    <uses-permission android:name="android.permission.CAMERA" />
    <uses-feature android:name="android.hardware.camera" android:required="false" />
    <queries>
      <intent><action android:name="android.media.action.IMAGE_CAPTURE" /></intent>
    </queries>

A pasta Platforms não veio no repositório que analisei; se ela existir na sua cópia, edite o manifesto que já está lá.

## Para testar no emulador
1. Suba a API local (o app já aponta para http://10.0.2.2:5137/api/ no Android).
2. Entre com um usuário Administrador ou Financeiro.
3. Lance uma despesa pendente e uma entrada paga com foto.
4. Cadastre uma dívida de 3 parcelas: elas aparecem como pendentes no Financeiro e na projeção.
5. Desligue a internet do emulador, faça um lançamento, religue e abra o Financeiro: ele é enviado.

## Limitações conhecidas
- O lançamento criado offline recebe outro Id quando chega ao servidor (a API ainda não aceita Id do cliente).
- Não há edição nem estorno de lançamento.
- O endereço da API e o Client ID do Google continuam em Services/AppConfig.cs.
