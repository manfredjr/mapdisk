namespace MapDisk.Nucleo;

public enum Idade
{
    SeisMeses,
    UmAno,
    DoisAnos,
    CincoAnos,
}

public sealed record ResumoAntigos(long Quantidade, long Tamanho, IReadOnlyList<ArquivoEncontrado> Maiores);

public sealed record ResumoTipo(string Nome, long Quantidade, long Tamanho);

public sealed record ResumoUsuarios(NoPasta? PastaDePerfis, IReadOnlyList<NoPasta> Perfis);

/// <summary>
/// Análises sobre uma pasta já varrida. Usam só a árvore em memória: nada é lido do disco de
/// novo. Hard link repetido fica de fora, como fica fora dos totais da árvore.
/// </summary>
public static class Analises
{
    public static IEnumerable<ArquivoEncontrado> TodosOsArquivos(NoPasta pasta)
    {
        var pilha = new Stack<NoPasta>();
        pilha.Push(pasta);
        while (pilha.Count > 0)
        {
            var no = pilha.Pop();
            foreach (var arquivo in no.Arquivos)
            {
                if (arquivo.Soma)
                {
                    yield return new ArquivoEncontrado(no, arquivo);
                }
            }

            foreach (var sub in no.Subpastas)
            {
                pilha.Push(sub);
            }
        }
    }

    public static IReadOnlyList<ArquivoEncontrado> MaioresArquivos(NoPasta pasta, int quantos = 100) =>
        Maiores(TodosOsArquivos(pasta), quantos);

    public static DateTime Limite(Idade idade, DateTime hoje) => idade switch
    {
        Idade.SeisMeses => hoje.Date.AddMonths(-6),
        Idade.UmAno => hoje.Date.AddYears(-1),
        Idade.DoisAnos => hoje.Date.AddYears(-2),
        _ => hoje.Date.AddYears(-5),
    };

    /// <summary>Arquivos sem alteração desde antes do limite. Arquivo sem data não entra.</summary>
    public static ResumoAntigos ArquivosAntigos(NoPasta pasta, Idade idade, DateTime hoje, int quantos = 100)
    {
        var limite = Limite(idade, hoje);
        long quantidade = 0;
        long tamanho = 0;
        var antigos = TodosOsArquivos(pasta)
            .Where(a => a.Arquivo.Modificacao != DateTime.MinValue && a.Arquivo.Modificacao < limite)
            .Select(a =>
            {
                quantidade++;
                tamanho += a.Arquivo.Tamanho;
                return a;
            });
        var maiores = Maiores(antigos, quantos);
        return new ResumoAntigos(quantidade, tamanho, maiores);
    }

    public static IReadOnlyList<ResumoTipo> PorCategoria(NoPasta pasta) =>
        Agrupar(pasta, a => Categorias.Nome(Categorias.De(a.Nome)), int.MaxValue);

    public static IReadOnlyList<ResumoTipo> PorExtensao(NoPasta pasta, int quantas = 30) =>
        Agrupar(pasta, a => Categorias.Extensao(a.Nome), quantas);

    /// <summary>
    /// Ranking das pastas de perfil: a pasta Users na raiz da unidade, ou a própria pasta
    /// quando ela se chama Users. Link fica de fora. Perfil sem leitura vai para o fim, sem número.
    /// </summary>
    public static ResumoUsuarios PorUsuario(NoPasta pasta)
    {
        var perfis = EhUsers(pasta)
            ? pasta
            : pasta.Pai is null ? pasta.Subpastas.FirstOrDefault(EhUsers) : null;
        if (perfis is null)
        {
            return new ResumoUsuarios(null, []);
        }

        var lista = perfis.Subpastas
            .Where(p => p.Estado != EstadoPasta.Link)
            .OrderBy(p => p.Estado == EstadoPasta.Lida ? 0 : 1)
            .ThenByDescending(p => p.Tamanho)
            .ThenBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new ResumoUsuarios(perfis, lista);
    }

    private static bool EhUsers(NoPasta pasta) => string.Equals(pasta.Nome, "Users", StringComparison.OrdinalIgnoreCase);

    // Guarda só os N maiores enquanto passa pelos arquivos: não ordena a lista inteira.
    private static IReadOnlyList<ArquivoEncontrado> Maiores(IEnumerable<ArquivoEncontrado> arquivos, int quantos)
    {
        var fila = new PriorityQueue<ArquivoEncontrado, long>();
        foreach (var a in arquivos)
        {
            if (fila.Count < quantos)
            {
                fila.Enqueue(a, a.Arquivo.Tamanho);
            }
            else if (fila.TryPeek(out _, out var menor) && a.Arquivo.Tamanho > menor)
            {
                fila.EnqueueDequeue(a, a.Arquivo.Tamanho);
            }
        }

        var lista = new List<ArquivoEncontrado>(fila.Count);
        while (fila.TryDequeue(out var a, out _))
        {
            lista.Add(a);
        }

        lista.Reverse();
        return lista;
    }

    private static IReadOnlyList<ResumoTipo> Agrupar(NoPasta pasta, Func<ArquivoInfo, string> chave, int quantos)
    {
        var grupos = new Dictionary<string, (long Quantidade, long Tamanho)>();
        foreach (var a in TodosOsArquivos(pasta))
        {
            var k = chave(a.Arquivo);
            grupos.TryGetValue(k, out var g);
            grupos[k] = (g.Quantidade + 1, g.Tamanho + a.Arquivo.Tamanho);
        }

        return grupos
            .Select(g => new ResumoTipo(g.Key, g.Value.Quantidade, g.Value.Tamanho))
            .OrderByDescending(r => r.Tamanho)
            .ThenBy(r => r.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(quantos)
            .ToList();
    }
}
