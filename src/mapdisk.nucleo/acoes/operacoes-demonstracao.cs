namespace MapDisk.Nucleo;

/// <summary>Modo de demonstração: o fluxo inteiro na tela, sem tocar no disco.</summary>
public sealed class OperacoesDemonstracao : IOperacoesArquivo
{
    private const long Gb = 1024L * 1024 * 1024;

    public string? Mudanca(ItemAcao item) => null;

    public long? Livre(string pasta) => 500 * Gb;

    public bool MesmoVolume(string origem, string pastaDestino) =>
        string.Equals(Volumes.RaizDe(origem), Volumes.RaizDe(pastaDestino), StringComparison.OrdinalIgnoreCase);

    public void EnviarParaLixeira(string caminho)
    {
    }

    public void ExcluirDefinitivo(string caminho, bool ehPasta)
    {
    }

    public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar)
    {
    }
}

/// <summary>Registro da demonstração e dos testes: fica na memória.</summary>
public sealed class RegistroEmMemoria : IRegistroAcoes
{
    public List<string> Linhas { get; } = [];

    public string Local => "memória (modo de demonstração)";

    public void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado) =>
        Linhas.Add($"{acao}|{origem}|{destino}|{bytes}|{resultado}");
}
