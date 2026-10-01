using System.Collections.Concurrent;

namespace MapDisk.Nucleo;

public sealed record OpcoesDuplicados(long TamanhoMinimo = 1024 * 1024, int Paralelos = 4)
{
    /// <summary>Em caminho de rede, menos leituras ao mesmo tempo, para não pesar no servidor (regra 7).</summary>
    public static OpcoesDuplicados Para(NoPasta pasta, long tamanhoMinimo) =>
        new(tamanhoMinimo, Alvo.EhRede(pasta.CaminhoCompleto()) ? 2 : 4);
}

public sealed record GrupoDuplicados(int Numero, long Tamanho, IReadOnlyList<ArquivoEncontrado> Arquivos)
{
    /// <summary>O que as cópias ocupam além do primeiro arquivo.</summary>
    public long Repetido => Tamanho * (Arquivos.Count - 1);

    /// <summary>O que fica: o mais antigo pela data de alteração; no empate, o de caminho menor.</summary>
    public ArquivoEncontrado Mantido => Arquivos
        .OrderBy(a => a.Arquivo.Modificacao)
        .ThenBy(a => a.Caminho, StringComparer.OrdinalIgnoreCase)
        .First();
}

public sealed record ResultadoDuplicados(NoPasta Pasta, IReadOnlyList<GrupoDuplicados> Grupos, int NaoLidos, bool Cancelado)
{
    public long TotalRepetido => Grupos.Sum(g => g.Repetido);
}

public readonly record struct ProgressoDuplicados(string Etapa, int Feitos, int Total);

/// <summary>
/// Duplicados em três etapas (R15): mesmo tamanho, pela árvore, sem ler o disco; resumo do
/// primeiro 1 MB; resumo do arquivo inteiro, só para quem é maior que 1 MB e passou na etapa 2.
/// </summary>
public static class Duplicados
{
    private const MarcaArquivo ForaDaBusca = MarcaArquivo.NaNuvem | MarcaArquivo.Link | MarcaArquivo.Sistema;

    public static async Task<ResultadoDuplicados> ProcurarAsync(
        NoPasta pasta, OpcoesDuplicados opcoes, ILeitorConteudo leitor, IProgress<ProgressoDuplicados>? progresso, CancellationToken cancelar)
    {
        var minimo = Math.Max(1, opcoes.TamanhoMinimo);
        var porTamanho = Analises.TodosOsArquivos(pasta)
            .Where(a => a.Arquivo.Tamanho >= minimo && (a.Arquivo.Marcas & ForaDaBusca) == 0)
            .GroupBy(a => a.Arquivo.Tamanho)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .ToList();
        var naoLidos = new int[1];
        try
        {
            var porInicio = await Separar(porTamanho, LeitorConteudo.Parte, "Conferindo o primeiro 1 MB", opcoes, leitor, progresso, naoLidos, cancelar);
            var pequenos = porInicio.Where(g => g[0].Arquivo.Tamanho <= LeitorConteudo.Parte).ToList();
            var grandes = porInicio.Where(g => g[0].Arquivo.Tamanho > LeitorConteudo.Parte).ToList();
            var porInteiro = await Separar(grandes, long.MaxValue, "Conferindo o arquivo inteiro", opcoes, leitor, progresso, naoLidos, cancelar);
            var grupos = pequenos.Concat(porInteiro)
                .OrderByDescending(g => g[0].Arquivo.Tamanho * (g.Count - 1))
                .Select((g, n) => new GrupoDuplicados(n + 1, g[0].Arquivo.Tamanho, g.OrderBy(a => a.Caminho, StringComparer.OrdinalIgnoreCase).ToList()))
                .ToList();
            return new ResultadoDuplicados(pasta, grupos, naoLidos[0], false);
        }
        catch (OperationCanceledException)
        {
            return new ResultadoDuplicados(pasta, [], naoLidos[0], true);
        }
    }

    // Lê cada arquivo dos grupos e separa os grupos pelo resumo. Arquivo que não abre sai e é contado.
    private static async Task<List<List<ArquivoEncontrado>>> Separar(
        List<List<ArquivoEncontrado>> grupos, long bytes, string etapa, OpcoesDuplicados opcoes, ILeitorConteudo leitor,
        IProgress<ProgressoDuplicados>? progresso, int[] naoLidos, CancellationToken cancelar)
    {
        var arquivos = grupos.SelectMany(g => g).ToList();
        var resumos = new ConcurrentDictionary<ArquivoEncontrado, string>();
        var feitos = 0;
        cancelar.ThrowIfCancellationRequested();
        await Parallel.ForEachAsync(arquivos, new ParallelOptions { MaxDegreeOfParallelism = opcoes.Paralelos, CancellationToken = cancelar }, (a, ct) =>
        {
            try
            {
                resumos[a] = Convert.ToHexString(leitor.Resumo(a.Caminho, bytes, ct));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Interlocked.Increment(ref naoLidos[0]);
            }

            progresso?.Report(new ProgressoDuplicados(etapa, Interlocked.Increment(ref feitos), arquivos.Count));
            return ValueTask.CompletedTask;
        });
        return grupos
            .SelectMany(g => g.Where(resumos.ContainsKey).GroupBy(a => resumos[a]).Where(x => x.Count() > 1).Select(x => x.ToList()))
            .ToList();
    }
}
