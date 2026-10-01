using System.Globalization;
using System.IO;
using System.Windows;
using MapDisk.Nucleo;
using Microsoft.Win32;

namespace MapDisk;

/// <summary>Monta a lista do relatório para o cliente e grava a página e a planilha. As regras ficam no PainelRelatorio.</summary>
public partial class JanelaRelatorio : Window
{
    private const long Mb = 1024 * 1024;
    private readonly PainelRelatorio _painel;
    private readonly Func<IEnumerable<object>> _selecaoAtual;
    private readonly Action<CriteriosSugestao> _guardarCriterios;

    /// <param name="guardarCriterios">Guarda os critérios usados no Sugerir para o próximo relatório.</param>
    public JanelaRelatorio(PainelRelatorio painel, Func<IEnumerable<object>> selecaoAtual, Action<CriteriosSugestao> guardarCriterios)
    {
        _painel = painel;
        _selecaoAtual = selecaoAtual;
        _guardarCriterios = guardarCriterios;
        DataContext = painel;
        InitializeComponent();
        var c = painel.Criterios;
        CampoPastas.Text = c.MaioresPastas.ToString(CultureInfo.InvariantCulture);
        CampoArquivos.Text = c.MaioresArquivos.ToString(CultureInfo.InvariantCulture);
        CampoAnos.Text = c.AnosSemAlteracao.ToString(CultureInfo.InvariantCulture);
        CampoMinimo.Text = (c.TamanhoMinimo / Mb).ToString(CultureInfo.InvariantCulture);
    }

    private void AoSugerir(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CampoPastas.Text, out var pastas) || !int.TryParse(CampoArquivos.Text, out var arquivos)
            || !int.TryParse(CampoAnos.Text, out var anos) || !long.TryParse(CampoMinimo.Text, out var minimo)
            || pastas < 0 || arquivos < 0 || anos < 1 || minimo < 0)
        {
            MessageBox.Show(this, "Use números inteiros nos critérios. Anos sem alteração começa em 1.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _painel.Criterios = new CriteriosSugestao(pastas, arquivos, anos, minimo * Mb);
        _guardarCriterios(_painel.Criterios);
        _painel.Sugerir(DateTime.Now);
    }

    private void AoAcrescentar(object sender, RoutedEventArgs e) => _painel.AcrescentarSelecao(_selecaoAtual());

    private void AoTirar(object sender, RoutedEventArgs e) =>
        _painel.Tirar(Lista.SelectedItems.Cast<LinhaRelatorio>().Select(l => l.Numero).ToList());

    private void AoGerar(object sender, RoutedEventArgs e)
    {
        if (_painel.MotivoParaNaoGerar is { } motivo)
        {
            MessageBox.Show(this, motivo, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var agora = DateTime.Now;
        var dialogo = new SaveFileDialog
        {
            Title = "Onde gravar o relatório",
            FileName = _painel.NomeSugerido(agora) + ".html",
            Filter = "Página HTML (*.html)|*.html",
            OverwritePrompt = true,
        };
        if (dialogo.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var (pagina, planilha) = _painel.Gerar(dialogo.FileName, agora);
            if (MessageBox.Show(this, $"Relatório gravado:\n\n{pagina}\n{planilha}\n\nMostrar no Explorer?", "MapDisk - MT",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Shell.MostrarNoExplorer(pagina, ehArquivo: true);
            }
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Não foi possível gravar o relatório: {erro.Message}", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AoFechar(object sender, RoutedEventArgs e) => Close();
}
