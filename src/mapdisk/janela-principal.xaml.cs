using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MapDisk.Nucleo;
using Microsoft.Win32;

namespace MapDisk;

/// <summary>Só liga os controles ao PainelPrincipal. Toda regra fica no núcleo.</summary>
public partial class JanelaPrincipal : Window
{
    private readonly PainelPrincipal _painel;
    private readonly DispatcherTimer _relogio = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public JanelaPrincipal(PainelPrincipal painel, string sufixoTitulo)
    {
        _painel = painel;
        DataContext = painel;
        InitializeComponent();
        Title = $"MapDisk - MT {ExecutorCli.Versao}{sufixoTitulo}";
        _relogio.Tick += (_, _) =>
        {
            _painel.Tique();
            MostrarErro();
        };
        _relogio.Start();
    }

    private void AoVarrer(object sender, RoutedEventArgs e)
    {
        _painel.Varrer();
        MostrarErro();
    }

    private void AoParar(object sender, RoutedEventArgs e) => _painel.Parar();

    private void AoAtualizar(object sender, RoutedEventArgs e) => _painel.Atualizar();

    private void AoAbrirUnidades(object? sender, EventArgs e) => _painel.AtualizarUnidades();

    private void AoProcurar(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta para varrer" };
        if (dialogo.ShowDialog(this) == true)
        {
            CampoAlvo.Text = dialogo.FolderName;
        }
    }

    private void AoTeclarNoAlvo(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AoVarrer(sender, e);
            e.Handled = true;
        }
    }

    private void AoMudarModo(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<ModoExibicao>(tag, out var modo))
        {
            _painel.Arvore.DefinirModo(modo);
        }
    }

    private void AoMudarUnidade(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<UnidadeExibicao>(tag, out var unidade))
        {
            _painel.Arvore.DefinirUnidade(unidade);
        }
    }

    private void AoClicarCabecalho(object sender, RoutedEventArgs e)
    {
        if (sender is GridViewColumnHeader { Tag: string tag } && Enum.TryParse<ColunaOrdem>(tag, out var coluna))
        {
            _painel.Arvore.Ordenar(coluna);
        }
    }

    private void AoAlternar(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LinhaArvore linha })
        {
            _painel.Arvore.Alternar(linha);
        }
    }

    private void AoClicarDuasVezes(object sender, MouseButtonEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore linha)
        {
            _painel.Arvore.Alternar(linha);
        }
    }

    private void AoTeclarNaTabela(object sender, KeyEventArgs e)
    {
        if (Tabela.SelectedItem is not LinhaArvore linha)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Right:
                _painel.Arvore.Expandir(linha);
                e.Handled = true;
                break;
            case Key.Left:
                _painel.Arvore.Recolher(linha);
                e.Handled = true;
                break;
            case Key.Enter:
                _painel.Arvore.Alternar(linha);
                e.Handled = true;
                break;
        }
    }

    private void MostrarErro()
    {
        if (_painel.Erro is not { } erro)
        {
            return;
        }

        _painel.LimparErro();
        MessageBox.Show(this, erro, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
