namespace MapDisk.Nucleo;

/// <summary>Uma opção da lista de alvos da janela: unidade ou alvo usado antes.</summary>
public sealed record ItemAlvo(string Caminho, string Descricao);

public interface IHistoricoAlvos
{
    IReadOnlyList<string> Ler();

    void Gravar(IReadOnlyList<string> alvos);
}

/// <summary>
/// Últimos alvos num arquivo do perfil do usuário, só nesta máquina. Falha de leitura ou de
/// gravação não atrapalha a varredura: a lista só fica vazia.
/// </summary>
public sealed class HistoricoAlvosArquivo(string arquivo) : IHistoricoAlvos
{
    public static HistoricoAlvosArquivo Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "alvos.txt"));

    public IReadOnlyList<string> Ler()
    {
        try
        {
            return File.Exists(arquivo)
                ? File.ReadAllLines(arquivo).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
                : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public void Gravar(IReadOnlyList<string> alvos)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, alvos);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class HistoricoEmMemoria : IHistoricoAlvos
{
    private IReadOnlyList<string> _alvos = [];

    public IReadOnlyList<string> Ler() => _alvos;

    public void Gravar(IReadOnlyList<string> alvos) => _alvos = alvos.ToList();
}

public static class UltimosAlvos
{
    public const int Maximo = 10;

    public static IReadOnlyList<string> Acrescentar(IReadOnlyList<string> atuais, string alvo) =>
        new[] { alvo }
            .Concat(atuais.Where(a => !string.Equals(a, alvo, StringComparison.OrdinalIgnoreCase)))
            .Take(Maximo)
            .ToList();

    public static IReadOnlyList<string> Esquecer(IReadOnlyList<string> atuais, string alvo) =>
        atuais.Where(a => !string.Equals(a, alvo, StringComparison.OrdinalIgnoreCase)).ToList();
}
