namespace MapDisk.Nucleo;

/// <summary>
/// Guarda cada nome de arquivo uma vez só por varredura. Numa unidade de trabalho, o mesmo
/// nome se repete milhares de vezes (index.js, desktop.ini, thumbs.db). Dividido em faixas com
/// trava, como o conjunto de identificadores.
/// </summary>
internal sealed class ConjuntoNomes
{
    private const int Faixas = 64;

    private readonly HashSet<string>[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new HashSet<string>(StringComparer.Ordinal)).ToArray();

    public string Guardar(ReadOnlySpan<char> nome)
    {
        var texto = new string(nome);
        var faixa = _faixas[(int)((uint)StringComparer.Ordinal.GetHashCode(texto) % Faixas)];
        lock (faixa)
        {
            if (faixa.TryGetValue(texto, out var existente))
            {
                return existente;
            }

            faixa.Add(texto);
            return texto;
        }
    }
}
