namespace MapDisk.Nucleo;

/// <summary>O que o painel usa de fora. Os testes trocam cada peça por uma falsa.</summary>
public sealed class DependenciasPainel
{
    public required IMotorVarredura Motor { get; init; }

    public required Func<IReadOnlyList<InfoVolume>> ListarUnidades { get; init; }

    public IHistoricoAlvos Historico { get; init; } = new HistoricoEmMemoria();

    /// <summary>O processo já roda como administrador.</summary>
    public bool Administrador { get; init; }

    /// <summary>Reabre o programa como administrador varrendo o alvo.</summary>
    public Func<string, ResultadoElevacao> Elevar { get; init; } = _ => ResultadoElevacao.Recusada;

    /// <summary>Operações no disco. O padrão não toca no disco: só o Padrao() do painel liga as reais.</summary>
    public IOperacoesArquivo Operacoes { get; init; } = new OperacoesDemonstracao();

    /// <summary>Leitura de conteúdo para os duplicados. A demonstração troca por uma que não abre arquivo.</summary>
    public ILeitorConteudo Leitor { get; init; } = new LeitorConteudo();

    /// <summary>Opções guardadas. O padrão fica só na memória; o Padrao() do painel liga o arquivo.</summary>
    public IArmazemPreferencias Preferencias { get; init; } = new PreferenciasEmMemoria();

    /// <summary>Pastas excluídas da varredura. O padrão fica só na memória; o Padrao() do painel liga o arquivo.</summary>
    public IArmazemExclusoes Exclusoes { get; init; } = new ExclusoesEmMemoria();

    public IRegistroAcoes Registro { get; init; } = new RegistroEmMemoria();

    public LocaisProtegidos Locais { get; init; } = LocaisProtegidos.DoSistema();

    public Func<string, DriveType> TipoDaUnidade { get; init; } = Lixeiras.TipoDaUnidade;

    /// <summary>Modo --demonstracao: a confirmação avisa que nada é feito.</summary>
    public bool Demonstracao { get; init; }
}
