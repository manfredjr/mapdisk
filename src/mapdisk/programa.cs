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
        var demonstracao = args is [Demonstracao.Argumento];
        if (args.Length > 0 && !demonstracao)
        {
            return ModoLinhaDeComando.Executar(ArgumentosCli.Interpretar(args));
        }

        var aplicativo = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        aplicativo.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/mapdisk;component/tema/tema-mt.xaml", UriKind.Absolute),
        });
        aplicativo.DispatcherUnhandledException += AoErroNaoTratado;
        return aplicativo.Run(demonstracao
            ? new JanelaPrincipal(Demonstracao.Painel(), " (demonstração)")
            : new JanelaPrincipal(PainelPrincipal.Padrao(), string.Empty));
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
