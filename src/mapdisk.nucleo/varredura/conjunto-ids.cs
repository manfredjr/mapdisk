namespace MapDisk.Nucleo;

/// <summary>
/// Identificadores de arquivo já contados, para o hard link somar uma vez. Dividido em faixas
/// com trava própria: ocupa menos memória que um dicionário concorrente e aguenta as tarefas
/// da varredura ao mesmo tempo.
/// </summary>
internal sealed class ConjuntoIds
{
    private const int Faixas = 64;

    private readonly HashSet<long>[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new HashSet<long>()).ToArray();

    public bool Acrescentar(long id)
    {
        var faixa = _faixas[(int)((ulong)id % Faixas)];
        lock (faixa)
        {
            return faixa.Add(id);
        }
    }
}
