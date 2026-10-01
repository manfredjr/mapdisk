namespace MapDisk.Nucleo;

/// <summary>
/// Guarda cada nome de arquivo uma vez só por varredura. Numa unidade de trabalho, o mesmo
/// nome se repete milhares de vezes (index.js, desktop.ini, thumbs.db). Cada faixa é uma tabela
/// aberta só de referências, com 8 bytes por posição, e o texto do nome só é criado quando o
/// nome é novo. Dividido em faixas com trava, como o conjunto de identificadores.
/// </summary>
internal sealed class ConjuntoNomes
{
    private const int Faixas = 64;

    private readonly Faixa[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new Faixa()).ToArray();

    public string Guardar(ReadOnlySpan<char> nome)
    {
        if (nome.IsEmpty)
        {
            return string.Empty;
        }

        var resumo = (uint)string.GetHashCode(nome);
        var faixa = _faixas[(int)(resumo % Faixas)];
        lock (faixa)
        {
            return faixa.Guardar(nome, resumo / Faixas);
        }
    }

    private sealed class Faixa
    {
        private string?[] _tabela = new string?[64];
        private int _quantos;

        public string Guardar(ReadOnlySpan<char> nome, uint resumo)
        {
            var mascara = _tabela.Length - 1;
            var i = (int)(resumo & (uint)mascara);
            while (_tabela[i] is { } existente)
            {
                if (nome.SequenceEqual(existente))
                {
                    return existente;
                }

                i = (i + 1) & mascara;
            }

            var novo = new string(nome);
            _tabela[i] = novo;
            if (++_quantos * 4 > _tabela.Length * 3)
            {
                Crescer();
            }

            return novo;
        }

        private void Crescer()
        {
            var antiga = _tabela;
            _tabela = new string?[antiga.Length * 2];
            var mascara = _tabela.Length - 1;
            foreach (var nome in antiga)
            {
                if (nome is not null)
                {
                    var i = (int)(((uint)string.GetHashCode(nome.AsSpan()) / Faixas) & (uint)mascara);
                    while (_tabela[i] is not null)
                    {
                        i = (i + 1) & mascara;
                    }

                    _tabela[i] = nome;
                }
            }
        }
    }
}
