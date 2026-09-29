using System.Windows;
using System.Windows.Threading;
using MapDisk.Nucleo;

namespace MapDisk;

internal static class Programa
{
    private static bool _erroMostrado;

    /// <summary>
    /// Sem argumentos abre a janela. Com argumentos roda a linha de comando, no mesmo .exe.
    /// O argumento --demonstracao abre a janela com dados de exemplo, sem ler o disco.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        // Processo elevado, pelo botao ou aberto como administrador por fora: le todas as pastas locais.
        var administrador = Privilegios.EhAdministrador();
        if (administrador)
        {
            Privilegios.LigarBackup();
        }

        var demonstracao = args is [Demonstracao.Argumento];
        var elevado = Elevacao.EhPedido(args, out var alvoElevado);
        if (args.Length > 0 && !demonstracao && !elevado)
        {
            return ModoLinhaDeComando.Executar(ArgumentosCli.Interpretar(args));
        }

        var aplicativo = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        aplicativo.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/mapdisk;component/tema/tema-mt.xaml", UriKind.Absolute),
        });
        aplicativo.DispatcherUnhandledException += AoErroNaoTratado;
        if (demonstracao)
        {
            return aplicativo.Run(new JanelaPrincipal(Demonstracao.Painel(), " (demonstração)", varrerAoAbrir: false));
        }

        var painel = PainelPrincipal.Padrao();
        if (elevado)
        {
            painel.TextoAlvo = alvoElevado!;
        }

        return aplicativo.Run(new JanelaPrincipal(painel, administrador ? " (administrador)" : string.Empty, varrerAoAbrir: elevado));
    }

    /// <summary>
    /// Erro que escapou da tela: mostra a mensagem uma vez e fecha o programa. Um erro de
    /// desenho da tela se repete a cada tentativa de redesenhar, e manter o programa aberto
    /// empilharia uma janela de erro atrás da outra.
    /// </summary>
    private static void AoErroNaoTratado(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (_erroMostrado)
        {
            return;
        }

        _erroMostrado = true;
        MessageBox.Show(
            $"Aconteceu um erro inesperado: {e.Exception.Message}\n\nO MapDisk vai fechar. Abra de novo e, se o erro voltar, avise a MT.",
            "MapDisk - MT",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Application.Current.Shutdown(1);
    }
}
