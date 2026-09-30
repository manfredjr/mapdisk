using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using MapDisk.Nucleo;
using Microsoft.Win32;

namespace MapDisk;

/// <summary>Só liga os controles ao PainelPrincipal. Toda regra fica no núcleo.</summary>
public partial class JanelaPrincipal : Window
{
    private readonly PainelPrincipal _painel;
    private readonly DispatcherTimer _relogio = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private int _concluidasVistas;
    private NoPasta? _pastaAnalisada;
    private bool _mostrarAnalises = true;

    public JanelaPrincipal(PainelPrincipal painel, string sufixoTitulo, bool varrerAoAbrir)
    {
        _painel = painel;
        DataContext = painel;
        InitializeComponent();
        if (varrerAoAbrir)
        {
            Loaded += (_, _) => AoVarrer(this, new RoutedEventArgs());
        }

        Title = $"MapDisk - MT {ExecutorCli.Versao}{sufixoTitulo}";
        _relogio.Tick += (_, _) =>
        {
            _painel.Tique();
            MostrarErro();
            if (_painel.VarredurasConcluidas != _concluidasVistas)
            {
                _concluidasVistas = _painel.VarredurasConcluidas;
                _pastaAnalisada = null;
                _ = AtualizarAnalises();
            }
        };
        _relogio.Start();
    }

    private void AoVarrer(object sender, RoutedEventArgs e)
    {
        _painel.Varrer();
        MostrarErro();
    }

    private void AoParar(object sender, RoutedEventArgs e) => _painel.Parar();

    private void AoSelecionarNaArvore(object sender, SelectionChangedEventArgs e) => _ = AtualizarAnalises();

    // Calcula as análises da pasta selecionada, ou da raiz mostrada, com a varredura parada.
    private async Task AtualizarAnalises()
    {
        if (!_mostrarAnalises || _painel.Estado != EstadoPainel.Parado)
        {
            return;
        }

        var pasta = _painel.PastaDasAnalises(Tabela.SelectedItem as LinhaArvore);
        if (pasta is null || pasta == _pastaAnalisada)
        {
            return;
        }

        _pastaAnalisada = pasta;
        await _painel.Analises.CalcularAsync(pasta, DateTime.Now);
    }

    private async void AoMudarIdade(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<Idade>(tag, out var idade))
        {
            await _painel.Analises.DefinirIdadeAsync(idade, DateTime.Now);
        }
    }

    private void AoMostrarAnalises(object sender, RoutedEventArgs e)
    {
        _mostrarAnalises = sender is CheckBox { IsChecked: true };
        if (ColunaPainel is null)
        {
            return;
        }

        ColunaPainel.Width = _mostrarAnalises ? new GridLength(520) : new GridLength(0);
        ColunaDivisoria.Width = _mostrarAnalises ? new GridLength(12) : new GridLength(0);
        if (_mostrarAnalises)
        {
            _pastaAnalisada = null;
            _ = AtualizarAnalises();
        }
    }

    // A linha da lista de análise em que o menu foi aberto: o menu é preso à própria linha.
    private static object? ItemDoMenu(object sender) =>
        ((sender as MenuItem)?.Parent as ContextMenu)?.PlacementTarget is FrameworkElement { DataContext: var linha } ? linha : null;

    private void AoMostrarArquivoNoExplorer(object sender, RoutedEventArgs e)
    {
        if (ItemDoMenu(sender) is LinhaArquivo linha)
        {
            Shell.MostrarNoExplorer(linha.Caminho, ehArquivo: true);
        }
    }

    private void AoCopiarCaminhoDoArquivo(object sender, RoutedEventArgs e)
    {
        if (ItemDoMenu(sender) is LinhaArquivo linha)
        {
            Shell.CopiarCaminho(linha.Caminho);
        }
    }

    private void AoAbrirPastaDoArquivo(object sender, RoutedEventArgs e)
    {
        if (ItemDoMenu(sender) is LinhaArquivo linha)
        {
            _painel.AbrirAqui(linha.Encontrado.Pasta);
        }
    }

    private void AoAbrirPerfil(object sender, RoutedEventArgs e)
    {
        if (ItemDoMenu(sender) is LinhaResumo { Pasta: { } pasta })
        {
            _painel.AbrirAqui(pasta);
        }
    }

    private void AoElevar(object sender, RoutedEventArgs e)
    {
        _painel.Elevar();
        MostrarErro();
    }

    private void AoVoltar(object sender, RoutedEventArgs e) => _painel.Voltar();

    private void AoAvancar(object sender, RoutedEventArgs e) => _painel.Avancar();

    private void AoSubir(object sender, RoutedEventArgs e) => _painel.Subir();

    private void AoMudarNiveis(object sender, SelectionChangedEventArgs e)
    {
        if (CampoNiveis?.SelectedItem is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var niveis))
        {
            _painel.AbrirNiveis(niveis);
        }
    }

    private void AoMostrarNoExplorer(object sender, RoutedEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore linha)
        {
            Shell.MostrarNoExplorer(linha.Caminho, linha.Tipo == TipoLinha.Arquivo);
        }
    }

    private void AoCopiarCaminho(object sender, RoutedEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore linha)
        {
            Shell.CopiarCaminho(linha.Caminho);
        }
    }

    private void AoAbrirAqui(object sender, RoutedEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore { Tipo: TipoLinha.Pasta } linha)
        {
            _painel.AbrirAqui(linha.Pasta);
        }
    }

    private void AoAtualizarPasta(object sender, RoutedEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore { Tipo: TipoLinha.Pasta } linha)
        {
            _painel.AtualizarPasta(linha.Pasta);
        }
    }

    private void AoPropriedades(object sender, RoutedEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore linha)
        {
            Shell.Propriedades(new WindowInteropHelper(this).Handle, linha.Caminho);
        }
    }

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

        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            AoAtualizarPasta(sender, e);
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Back:
                _painel.Voltar();
                e.Handled = true;
                break;
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
