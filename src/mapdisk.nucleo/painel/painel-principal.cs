using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum EstadoPainel
{
    Parado,
    Varrendo,
    Cancelando,
}

/// <summary>
/// Estado e comandos da janela principal, sem nenhum tipo do WPF. A janela chama Tique a cada
/// 250 ms. É no Tique que os números se atualizam e que a varredura é fechada quando termina.
/// </summary>
public sealed class PainelPrincipal : INotifyPropertyChanged
{
    private readonly IMotorVarredura _motor;
    private readonly Func<IReadOnlyList<InfoVolume>> _listarUnidades;
    private readonly IHistoricoAlvos _historico;
    private readonly Func<string, ResultadoElevacao> _elevar;
    private CancellationTokenSource? _cancelar;
    private Varredura? _varredura;
    private InfoVolume? _volume;
    private string? _ultimoAlvo;

    public PainelPrincipal(DependenciasPainel dependencias)
    {
        _motor = dependencias.Motor;
        _listarUnidades = dependencias.ListarUnidades;
        _historico = dependencias.Historico;
        _elevar = dependencias.Elevar;
        Administrador = dependencias.Administrador;
        Acoes = new PreparadorAcoes(dependencias.Locais, dependencias.TipoDaUnidade, dependencias.Operacoes, dependencias.Demonstracao);
        Executor = new ExecutorAcoes(dependencias.Operacoes, dependencias.Registro);
        LocalDoRegistro = dependencias.Registro.Local;
        Unidades = _listarUnidades();
        TextoAlvo = Unidades.FirstOrDefault()?.Raiz ?? string.Empty;
        MontarOpcoes();
    }

    public PainelPrincipal(IMotorVarredura motor, Func<IReadOnlyList<InfoVolume>> listarUnidades)
        : this(new DependenciasPainel { Motor = motor, ListarUnidades = listarUnidades })
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static PainelPrincipal Padrao() => new(new DependenciasPainel
    {
        Motor = new MotorVarredura(),
        ListarUnidades = Volumes.ListarUnidades,
        Historico = HistoricoAlvosArquivo.Padrao(),
        Administrador = Privilegios.EhAdministrador(),
        Elevar = Elevacao.Reabrir,
        Operacoes = new OperacoesArquivo(),
        Registro = RegistroAcoes.Padrao(),
    });

    /// <summary>O processo já roda como administrador.</summary>
    public bool Administrador { get; }

    /// <summary>Unidades e, depois delas, os últimos alvos usados.</summary>
    public IReadOnlyList<ItemAlvo> Opcoes { get; private set; } = [];

    public ArvoreVisivel Arvore { get; } = new();

    public IReadOnlyList<InfoVolume> Unidades { get; private set; }

    public string TextoAlvo { get; set; }

    public EstadoPainel Estado { get; private set; }

    public bool PodeVarrer => Estado == EstadoPainel.Parado;

    public bool PodeParar => Estado == EstadoPainel.Varrendo;

    public bool PodeAtualizar => Estado == EstadoPainel.Parado && _ultimoAlvo != null;

    public bool Varrendo => Estado != EstadoPainel.Parado;

    public string TextoEstado { get; private set; } = "Escolha uma unidade, pasta ou caminho de rede e clique em Varrer.";

    public string TextoVolume { get; private set; } = string.Empty;

    public string TextoTotais { get; private set; } = string.Empty;

    public string TextoSemLeitura { get; private set; } = string.Empty;

    /// <summary>Mensagem para a janela mostrar numa caixa. A janela chama LimparErro depois.</summary>
    public string? Erro { get; private set; }

    public void AtualizarUnidades()
    {
        Unidades = _listarUnidades();
        MontarOpcoes();
        Avisar();
    }

    public bool Varrer()
    {
        if (!PodeVarrer)
        {
            return false;
        }

        var alvo = Alvo.Normalizar(TextoAlvo, out var erro);
        if (alvo is null)
        {
            Erro = erro;
            Avisar();
            return false;
        }

        Iniciar(alvo);
        return true;
    }

    public void Atualizar()
    {
        if (PodeAtualizar)
        {
            Iniciar(_ultimoAlvo!);
        }
    }

    /// <summary>Lê de novo só esta pasta (Shift+F5). A raiz é varrida inteira. Link não é relido.</summary>
    public bool AtualizarPasta(NoPasta pasta)
    {
        if (!PodeVarrer || pasta.Estado == EstadoPasta.Link)
        {
            return false;
        }

        if (pasta.Pai is null)
        {
            Atualizar();
            return true;
        }

        Erro = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Reler(pasta, _cancelar.Token);
        Arvore.Substituir(pasta, _varredura.Raiz);
        Analises.Aguardar();
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
        return true;
    }

    public void Parar()
    {
        if (Estado != EstadoPainel.Varrendo)
        {
            return;
        }

        Estado = EstadoPainel.Cancelando;
        TextoEstado = "Parando...";
        _cancelar!.Cancel();
        Avisar();
    }

    public void LimparErro() => Erro = null;

    public PainelAnalises Analises { get; } = new();

    /// <summary>
    /// Quantas varreduras terminaram. A janela compara com o último número visto para recalcular
    /// as análises: uma varredura rápida começa e termina entre duas batidas do relógio.
    /// </summary>
    public int VarredurasConcluidas { get; private set; }

    /// <summary>A pasta que as análises mostram: a selecionada, ou a raiz mostrada.</summary>
    public NoPasta? PastaDasAnalises(LinhaArvore? selecionada) =>
        selecionada is { Tipo: TipoLinha.Pasta, Pasta.Estado: EstadoPasta.Lida } linha ? linha.Pasta : Arvore.Raiz;

    public bool MostrarElevar => !Administrador;

    public bool PodeElevar => !Administrador && Estado == EstadoPainel.Parado;

    /// <summary>Reabre o programa como administrador, varrendo o alvo. A recusa no aviso do Windows não é erro de programa.</summary>
    public void Elevar()
    {
        if (!PodeElevar)
        {
            return;
        }

        string? erro = null;
        var alvo = _ultimoAlvo ?? Alvo.Normalizar(TextoAlvo, out erro);
        if (alvo is null)
        {
            Erro = erro;
            Avisar();
            return;
        }

        if (_elevar(alvo) == ResultadoElevacao.Aberta)
        {
            TextoEstado = "A varredura como administrador abriu em outra janela.";
        }
        else
        {
            Erro = "O Windows não confirmou a elevação. A varredura segue sem administrador.";
        }

        Avisar();
    }

    private long _sessaoLixeira;
    private long _sessaoMovido;
    private long _sessaoExcluido;

    public PreparadorAcoes Acoes { get; }

    public ExecutorAcoes Executor { get; }

    public string LocalDoRegistro { get; }

    public AvaliacaoSelecao Selecao { get; private set; } = new([], "Selecione pastas ou arquivos na árvore ou nas listas.", TipoAcao.Lixeira);

    public bool PodeRemover => Estado == EstadoPainel.Parado && Selecao.PodeRemover;

    public bool PodeMover => Estado == EstadoPainel.Parado && Selecao.PodeMover;

    public string TextoRemover => Selecao.TextoRemover;

    public string MotivoBloqueio => Estado != EstadoPainel.Parado ? "Aguarde o fim da varredura." : Selecao.Bloqueio ?? string.Empty;

    /// <summary>Quantas ações terminaram. A janela compara para recalcular as análises.</summary>
    public int AcoesConcluidas { get; private set; }

    public string TextoSessao { get; private set; } = string.Empty;

    public void AvaliarSelecao(IEnumerable<object> selecionados)
    {
        Selecao = Acoes.Avaliar(selecionados);
        Avisar();
    }

    /// <summary>Itens vindos da resposta do cliente, avaliados pelas mesmas regras da seleção.</summary>
    public void AvaliarItens(IReadOnlyList<ItemAcao> itens)
    {
        Selecao = Acoes.AvaliarItens(itens);
        Avisar();
    }

    /// <summary>Tira da árvore o que deu certo, soma a sessão e relê o destino do mover, se estiver na árvore.</summary>
    public void Concluir(PedidoAcao pedido, ResumoAcao resumo)
    {
        foreach (var r in resumo.Resultados.Where(r => r.Ok))
        {
            Arvore.Remover(r.Item);
        }

        switch (pedido.Acao)
        {
            case TipoAcao.Lixeira:
                _sessaoLixeira += resumo.BytesOk;
                break;
            case TipoAcao.Mover:
                _sessaoMovido += resumo.BytesOk;
                break;
            default:
                _sessaoExcluido += resumo.BytesOk;
                break;
        }

        var partes = new List<string>();
        if (_sessaoLixeira > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoLixeira)} para a Lixeira");
        }

        if (_sessaoMovido > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoMovido)} movidos");
        }

        if (_sessaoExcluido > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoExcluido)} excluídos");
        }

        TextoSessao = partes.Count == 0 ? string.Empty : $"Nesta sessão: {string.Join(", ", partes)}";
        AcoesConcluidas++;
        Selecao = Acoes.Avaliar([]);
        AtualizarTextos();

        var raizDaVarredura = Arvore.Raiz;
        while (raizDaVarredura?.Pai is { } pai)
        {
            raizDaVarredura = pai;
        }

        if (pedido.Acao == TipoAcao.Mover && resumo.Ok > 0 && raizDaVarredura?.Encontrar(pedido.Destino!) is { } destino)
        {
            AtualizarPasta(destino);
        }
    }

    public bool PodeVoltar => Arvore.PodeVoltar;

    public bool PodeAvancar => Arvore.PodeAvancar;

    public bool PodeSubir => Arvore.PodeSubir;

    public void AbrirAqui(NoPasta pasta) => Navegar(() => Arvore.AbrirAqui(pasta));

    public void Voltar() => Navegar(Arvore.Voltar);

    public void Avancar() => Navegar(Arvore.Avancar);

    public void Subir() => Navegar(Arvore.Subir);

    public void AbrirNiveis(int niveis) => Navegar(() => Arvore.AbrirNiveis(niveis));

    // Navegar muda a raiz mostrada: os totais da barra passam a ser os dessa raiz.
    private void Navegar(Action acao)
    {
        acao();
        AtualizarTextos();
    }

    public void Tique()
    {
        if (_varredura is null)
        {
            return;
        }

        if (_varredura.Conclusao.IsCompleted)
        {
            Concluir();
            return;
        }

        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void Iniciar(string alvo)
    {
        _ultimoAlvo = alvo;
        _historico.Gravar(UltimosAlvos.Acrescentar(_historico.Ler(), alvo));
        MontarOpcoes();
        TextoAlvo = alvo;
        Erro = null;
        _volume = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Iniciar(alvo, _cancelar.Token);
        Arvore.Carregar(_varredura.Raiz);
        Analises.Aguardar();
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
    }

    private void Concluir()
    {
        var varredura = _varredura!;
        _varredura = null;

        VarredurasConcluidas++;

        // Primeiro o estado, depois qualquer aviso: os botões voltam ao normal antes da caixa de erro.
        Estado = EstadoPainel.Parado;
        if (varredura.Conclusao.IsFaulted)
        {
            TextoEstado = "A varredura parou por um erro.";
            Erro = $"A varredura parou por um erro: {varredura.Conclusao.Exception!.GetBaseException().Message}";
        }
        else
        {
            var r = varredura.Conclusao.Result;
            _volume = r.Volume;
            TextoEstado = r.Cancelada
                ? $"Varredura interrompida em {Formatador.Duracao(r.Duracao)}. Os números mostram só o que foi lido até ali."
                : $"Varredura concluída em {Formatador.Duracao(r.Duracao)}.";
            if (r.Raiz.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura)
            {
                Erro = $"Não foi possível ler {r.Raiz.Nome}: {r.Raiz.Motivo}.";
            }
            else if (!r.Cancelada && r.PastasNaoLidas > 0)
            {
                Erro = $"A varredura terminou, mas {Formatador.Plural(r.PastasNaoLidas, "pasta não foi lida", "pastas não foram lidas")}. Clique em Atualizar para varrer de novo.";
            }
        }

        _cancelar?.Dispose();
        _cancelar = null;
        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        if (Arvore.Raiz is { } raiz)
        {
            if (_varredura is not null && Estado == EstadoPainel.Varrendo)
            {
                TextoEstado = $"Varrendo {_varredura.PastaAtual}";
            }

            TextoTotais = $"{Formatador.Plural(raiz.ArquivosTotal, "arquivo", "arquivos")} em {Formatador.Plural(raiz.PastasTotal, "pasta", "pastas")}";
            var partes = new List<string>();
            if (raiz.PastasSemAcesso > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasSemAcesso, "pasta sem acesso", "pastas sem acesso"));
            }

            if (raiz.PastasComErro > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasComErro, "pasta com erro de leitura", "pastas com erro de leitura"));
            }

            TextoSemLeitura = string.Join(" | ", partes);
        }

        TextoVolume = _volume is { } v
            ? $"Livre: {Formatador.Tamanho(v.Livre)} de {Formatador.Tamanho(v.Total)} | Cluster {Formatador.Tamanho(v.Cluster)} ({v.SistemaArquivos})"
            : string.Empty;
        Avisar();
    }

    private void MontarOpcoes()
    {
        var unidades = Unidades.Select(u => new ItemAlvo(u.Raiz, u.Descricao)).ToList();
        var recentes = _historico.Ler()
            .Where(a => !unidades.Any(u => string.Equals(u.Caminho, a, StringComparison.OrdinalIgnoreCase)))
            .Select(a => new ItemAlvo(a, $"{a}  (usado antes)"));
        Opcoes = unidades.Concat(recentes).ToList();
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
