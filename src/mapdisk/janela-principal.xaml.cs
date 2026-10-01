using System.Collections;
using System.IO;
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
    private NoPasta? _raizVista;
    private IList? _ultimaSelecao;
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
            else if (_painel.Arvore.Raiz != _raizVista)
            {
                // Abrir aqui, Voltar, Avançar e Subir trocam a raiz, às vezes sem mudar a seleção.
                _raizVista = _painel.Arvore.Raiz;
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

    private void AoSelecionarNaArvore(object sender, SelectionChangedEventArgs e)
    {
        AoSelecionarParaAcao(sender, e);
        _ = AtualizarAnalises();
    }

    // A última lista em que o técnico selecionou algo é a que vale para os botões de ação.
    private void AoSelecionarParaAcao(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lista)
        {
            _ultimaSelecao = lista.SelectedItems;
            _painel.AvaliarSelecao(lista.SelectedItems.Cast<object>());
        }
    }

    private void AoTeclarNaLista(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            AoRemover(sender, e);
            e.Handled = true;
        }
    }

    private async void AoRemover(object sender, RoutedEventArgs e)
    {
        _painel.AvaliarSelecao(_ultimaSelecao?.Cast<object>() ?? []);
        if (_painel.PodeRemover)
        {
            await Agir(new PedidoAcao(_painel.Selecao.Remocao, _painel.Selecao.Itens, null));
        }
        else if (_painel.MotivoBloqueio.Length > 0)
        {
            MessageBox.Show(this, _painel.MotivoBloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void AoMover(object sender, RoutedEventArgs e)
    {
        _painel.AvaliarSelecao(_ultimaSelecao?.Cast<object>() ?? []);
        if (!_painel.PodeMover)
        {
            if (_painel.MotivoBloqueio.Length > 0)
            {
                MessageBox.Show(this, _painel.MotivoBloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return;
        }

        var itens = _painel.Selecao.Itens;
        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta de destino" };
        if (dialogo.ShowDialog(this) != true)
        {
            return;
        }

        if (_painel.Acoes.BloqueioDestino(itens, dialogo.FolderName) is { } bloqueio)
        {
            MessageBox.Show(this, bloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await Agir(new PedidoAcao(TipoAcao.Mover, itens, dialogo.FolderName));
    }

    // Conferência na hora, confirmação, execução com andamento e o que muda depois.
    private async Task Agir(PedidoAcao pedido)
    {
        var mudancas = _painel.Acoes.Mudancas(pedido.Itens);
        if (mudancas.Count > 0 && MessageBox.Show(this,
                $"Estes itens mudaram desde a varredura:\n\n{string.Join("\n", mudancas.Take(10))}\n\nSeguir mesmo assim?",
                "MapDisk - MT", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            return;
        }

        if (new JanelaConfirmacao(_painel.Acoes.Confirmar(pedido), pedido) { Owner = this }.ShowDialog() != true)
        {
            return;
        }

        var andamento = new JanelaAndamento(_painel.Executor, pedido, _painel.LocalDoRegistro) { Owner = this };
        andamento.ShowDialog();
        if (andamento.Resumo is { } resumo)
        {
            _painel.Concluir(pedido, resumo);
            _pastaAnalisada = null;
            await AtualizarAnalises();
        }
    }

    private void AoAbrirMenuRelatorio(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } botao)
        {
            menu.PlacementTarget = botao;
            menu.IsOpen = true;
        }
    }

    private void AoGerarRelatorio(object sender, RoutedEventArgs e)
    {
        if (_painel.Arvore.Raiz is not { } raiz || _painel.Estado != EstadoPainel.Parado)
        {
            MessageBox.Show(this, "Varra a pasta primeiro e espere o fim da varredura.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var relatorio = new PainelRelatorio(raiz, Volumes.Ler(raiz.CaminhoCompleto())?.Livre, Environment.UserName, _painel.Locais,
            _painel.Analises.Duplicados.Resultado)
        {
            Criterios = _painel.Preferencias.Criterios,
        };
        new JanelaRelatorio(relatorio, () => _ultimaSelecao?.Cast<object>().ToList() ?? [],
            c => _painel.GravarPreferencias(_painel.Preferencias.ComCriterios(c))) { Owner = this }.ShowDialog();
    }

    private void AoLerResposta(object sender, RoutedEventArgs e)
    {
        var raiz = _painel.Arvore.Raiz;
        while (raiz?.Pai is { } pai)
        {
            raiz = pai;
        }

        if (raiz is null || _painel.Estado != EstadoPainel.Parado)
        {
            MessageBox.Show(this, "Varra a pasta do relatório antes de ler a resposta.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        new JanelaResposta(new PainelResposta(raiz), AgirSobre) { Owner = this }.ShowDialog();
    }

    // Itens vindos da resposta do cliente: as mesmas regras e a mesma confirmação da seleção.
    private async Task AgirSobre(IReadOnlyList<ItemAcao> itens, bool mover)
    {
        _painel.AvaliarItens(itens);
        if (!_painel.PodeRemover)
        {
            MessageBox.Show(this, _painel.MotivoBloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!mover)
        {
            await Agir(new PedidoAcao(_painel.Selecao.Remocao, _painel.Selecao.Itens, null));
            return;
        }

        var selecionados = _painel.Selecao.Itens;
        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta de destino" };
        if (dialogo.ShowDialog(this) != true)
        {
            return;
        }

        if (_painel.Acoes.BloqueioDestino(selecionados, dialogo.FolderName) is { } bloqueio)
        {
            MessageBox.Show(this, bloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await Agir(new PedidoAcao(TipoAcao.Mover, selecionados, dialogo.FolderName));
    }

    private void AoAbrirOpcoes(object sender, RoutedEventArgs e)
    {
        if (new JanelaOpcoes(_painel) { Owner = this }.ShowDialog() == true)
        {
            // A quantidade de maiores arquivos pode ter mudado: calcula de novo.
            _pastaAnalisada = null;
            _ = AtualizarAnalises();
        }
    }

    private void AoExportarHtml(object sender, RoutedEventArgs e)
    {
        if (PastaParaExportar() is not { } pasta
            || Destino("Onde gravar o relatório", RelatorioTecnico.NomeDoArquivo(pasta, DateTime.Now) + ".html", "Página HTML (*.html)|*.html") is not { } arquivo)
        {
            return;
        }

        var dados = new DadosRelatorio(pasta, DateTime.Now, Environment.MachineName, _painel.Volume, _painel.Interrompida,
            _painel.Preferencias.MaioresArquivos, _painel.Analises.Duplicados.Resultado);
        if (Gravar(() => RelatorioTecnico.Gravar(dados, arquivo))
            && MessageBox.Show(this, "Relatório gravado. Abrir agora?", "MapDisk - MT", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Shell.AbrirNoNavegador(arquivo);
        }
    }

    private void AoExportarCsv(object sender, RoutedEventArgs e)
    {
        if (PastaParaExportar() is not { } pasta
            || Destino("Onde gravar a planilha", RelatorioTecnico.NomeDoArquivo(pasta, DateTime.Now) + ".csv", "CSV (*.csv)|*.csv") is not { } arquivo)
        {
            return;
        }

        if (Gravar(() => ExportadorCsv.Gravar(pasta, arquivo)))
        {
            Shell.MostrarNoExplorer(arquivo, ehArquivo: true);
        }
    }

    // A mesma pasta das análises: a selecionada na árvore, ou a raiz mostrada.
    private NoPasta? PastaParaExportar()
    {
        if (_painel.Estado != EstadoPainel.Parado || _painel.PastaDasAnalises(Tabela.SelectedItem as LinhaArvore) is not { } pasta)
        {
            MessageBox.Show(this, "Varra uma unidade ou pasta antes de exportar e espere o fim da varredura.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        return pasta;
    }

    private string? Destino(string titulo, string nome, string filtro)
    {
        var dialogo = new SaveFileDialog { Title = titulo, FileName = nome, Filter = filtro, OverwritePrompt = true };
        return dialogo.ShowDialog(this) == true ? dialogo.FileName : null;
    }

    private bool Gravar(Action gravar)
    {
        try
        {
            gravar();
            return true;
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Não foi possível gravar: {erro.Message}", "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

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

    // O arquivo da linha em que o menu foi aberto, seja de Maiores, Antigos ou Duplicados.
    private static ArquivoEncontrado? ArquivoDoMenu(object sender) => ItemDoMenu(sender) switch
    {
        LinhaArquivo l => l.Encontrado,
        LinhaDuplicado d => d.Encontrado,
        _ => null,
    };

    private async void AoProcurarDuplicados(object sender, RoutedEventArgs e)
    {
        if (_painel.Estado != EstadoPainel.Parado || _painel.PastaDasAnalises(Tabela.SelectedItem as LinhaArvore) is not { } pasta)
        {
            MessageBox.Show(this, "Varra a pasta primeiro e espere o fim da varredura.", "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _painel.Analises.Duplicados.ProcurarAsync(pasta);
    }

    private void AoCancelarDuplicados(object sender, RoutedEventArgs e) => _painel.Analises.Duplicados.Cancelar();

    private void AoSelecionarCopias(object sender, RoutedEventArgs e)
    {
        var copias = _painel.Analises.Duplicados.Copias().ToHashSet();
        ListaDuplicados.SelectedItems.Clear();
        foreach (var linha in ListaDuplicados.Items.Cast<LinhaDuplicado>().Where(copias.Contains))
        {
            ListaDuplicados.SelectedItems.Add(linha);
        }

        ListaDuplicados.Focus();
    }

    private void AoMostrarArquivoNoExplorer(object sender, RoutedEventArgs e)
    {
        if (ArquivoDoMenu(sender) is { } arquivo)
        {
            Shell.MostrarNoExplorer(arquivo.Caminho, ehArquivo: true);
        }
    }

    private void AoCopiarCaminhoDoArquivo(object sender, RoutedEventArgs e)
    {
        if (ArquivoDoMenu(sender) is { } arquivo)
        {
            Shell.CopiarCaminho(arquivo.Caminho);
        }
    }

    private void AoAbrirPastaDoArquivo(object sender, RoutedEventArgs e)
    {
        if (ArquivoDoMenu(sender) is { } arquivo)
        {
            _painel.AbrirAqui(arquivo.Pasta);
        }
    }

    private void AoAbrirPerfil(object sender, RoutedEventArgs e)
    {
        if (ItemDoMenu(sender) is LinhaResumo { Pasta: { } pasta })
        {
            _painel.AbrirAqui(pasta);
        }
    }

    private void AoClicarLogo(object sender, RoutedEventArgs e) => Shell.AbrirNoNavegador(Sobre.SiteMt);

    private void AoAbrirSobre(object sender, RoutedEventArgs e) => new JanelaSobre { Owner = this }.ShowDialog();

    private void AoMudarGrafico(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<TipoGrafico>(tag, out var tipo))
        {
            _painel.Analises.Grafico.DefinirTipo(tipo);
        }
    }

    private void AoRedimensionarGrafico(object sender, SizeChangedEventArgs e) =>
        _painel.Analises.Grafico.Redimensionar(e.NewSize.Width, e.NewSize.Height);

    // Clique duplo num bloco ou numa fatia de pasta abre a pasta na árvore. Voltar retorna.
    private void AoClicarNoGrafico(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not FrameworkElement { DataContext: var item })
        {
            return;
        }

        var pasta = item switch
        {
            BlocoGrafico b => b.Pasta,
            FatiaGrafico f => f.Pasta,
            _ => null,
        };
        if (pasta is not null)
        {
            _painel.AbrirAqui(pasta);
            e.Handled = true;
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
            _painel.Analises.Grafico.DefinirModo(modo);
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
            case Key.Delete:
                AoRemover(sender, e);
                e.Handled = true;
                break;
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
