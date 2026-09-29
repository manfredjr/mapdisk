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
}
