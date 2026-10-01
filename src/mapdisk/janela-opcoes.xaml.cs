using System.Globalization;
using System.IO;
using System.Windows;
using MapDisk.Nucleo;

namespace MapDisk;

/// <summary>Opções que valem entre sessões, o item do Explorer, os últimos alvos e o registro de ações.</summary>
public partial class JanelaOpcoes : Window
{
    private readonly PainelPrincipal _painel;
    private readonly IChavesUsuario _chaves = new ChavesUsuarioWindows();
    private readonly string _exe = Environment.ProcessPath!;

    public JanelaOpcoes(PainelPrincipal painel)
    {
        _painel = painel;
        InitializeComponent();
        var p = painel.Preferencias;
        CampoMaiores.Text = N(p.MaioresArquivos);
        CampoMinimoDuplicados.Text = N(p.DuplicadosMinimoMb);
        CampoPastas.Text = N(p.SugerirPastas);
        CampoArquivos.Text = N(p.SugerirArquivos);
        CampoAnos.Text = N(p.SugerirAnos);
        CampoMinimo.Text = N(p.SugerirMinimoMb);
        TextoRegistro.Text = painel.LocalDoRegistro;
        MostrarExplorer();
        MostrarAlvos();
    }

    private void MostrarExplorer()
    {
        var estado = IntegracaoExplorer.Estado(_chaves, _exe);
        TextoExplorer.Text = estado switch
        {
            EstadoIntegracao.Ligada => "Ligado para este mapdisk.exe.",
            EstadoIntegracao.OutroLocal => $"O item aponta para outro local: {IntegracaoExplorer.ExeRegistrado(_chaves)}. Clique em Ligar para apontar para este.",
            _ => "Desligado.",
        };
        BotaoDesligar.IsEnabled = estado != EstadoIntegracao.Desligada;
    }

    private void MostrarAlvos() => ListaAlvos.ItemsSource = _painel.UltimosUsados;

    private void AoLigar(object sender, RoutedEventArgs e)
    {
        IntegracaoExplorer.Ligar(_chaves, _exe);
        MostrarExplorer();
    }

    private void AoDesligar(object sender, RoutedEventArgs e)
    {
        IntegracaoExplorer.Desligar(_chaves);
        MostrarExplorer();
    }

    private void AoEsquecer(object sender, RoutedEventArgs e)
    {
        if (ListaAlvos.SelectedItem is string alvo)
        {
            _painel.EsquecerAlvo(alvo);
            MostrarAlvos();
        }
    }

    private void AoLimpar(object sender, RoutedEventArgs e)
    {
        _painel.LimparAlvos();
        MostrarAlvos();
    }

    private void AoAbrirRegistro(object sender, RoutedEventArgs e)
    {
        if (File.Exists(_painel.LocalDoRegistro))
        {
            Shell.MostrarNoExplorer(_painel.LocalDoRegistro, ehArquivo: true);
        }
        else
        {
            MessageBox.Show(this, $"Nenhuma ação registrada ainda. O registro fica em {_painel.LocalDoRegistro}.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AoSalvar(object sender, RoutedEventArgs e)
    {
        var campos = new (string Nome, string Texto)[]
        {
            ("maiores-arquivos", CampoMaiores.Text),
            ("duplicados-minimo-mb", CampoMinimoDuplicados.Text),
            ("sugerir-pastas", CampoPastas.Text),
            ("sugerir-arquivos", CampoArquivos.Text),
            ("sugerir-anos", CampoAnos.Text),
            ("sugerir-minimo-mb", CampoMinimo.Text),
        };
        if (!campos.All(c => Preferencias.Valido(c.Nome, c.Texto)))
        {
            MessageBox.Show(this, "Algum número está fora da faixa. Maiores arquivos vai de 10 a 1000, duplicados a partir de 1 MB e anos sem alteração a partir de 1.",
                "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _painel.GravarPreferencias(Preferencias.DeTexto(campos.Select(c => $"{c.Nome}={c.Texto}")));
        DialogResult = true;
    }

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);
}
