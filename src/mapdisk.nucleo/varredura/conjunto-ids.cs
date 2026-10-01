namespace MapDisk.Nucleo;

/// <summary>
/// Identificadores de arquivo já contados, para o hard link somar uma vez. Recebe um
/// identificador por arquivo da unidade, então cada byte conta: em vez de HashSet, que gasta
/// cerca de 20 bytes por item, cada faixa é uma tabela aberta só de números, com 8 bytes por
/// posição. As faixas têm trava própria e aguentam as tarefas da varredura ao mesmo tempo.
/// </summary>
internal sealed class ConjuntoIds
{
    private const int Faixas = 64;

    private readonly Faixa[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new Faixa()).ToArray();
    private int _zero;

    public bool Acrescentar(long id)
    {
        // O zero marca posição vazia na tabela, então fica numa marca à parte.
        if (id == 0)
        {
            return Interlocked.Exchange(ref _zero, 1) == 0;
        }

        var mistura = Misturar(id);
        var faixa = _faixas[(int)(mistura % Faixas)];
        lock (faixa)
        {
            return faixa.Acrescentar(id, mistura / Faixas);
        }
    }

    // Espalha os identificadores, que no NTFS vêm em sequência, pelas faixas e posições.
    private static ulong Misturar(long id)
    {
        var x = (ulong)id;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    private sealed class Faixa
    {
        private long[] _tabela = new long[64];
        private int _quantos;

        public bool Acrescentar(long id, ulong mistura)
        {
            var mascara = _tabela.Length - 1;
            var i = (int)(mistura & (ulong)mascara);
            while (_tabela[i] != 0)
            {
                if (_tabela[i] == id)
                {
                    return false;
                }

                i = (i + 1) & mascara;
            }

            _tabela[i] = id;
            if (++_quantos * 4 > _tabela.Length * 3)
            {
                Crescer();
            }

            return true;
        }

        private void Crescer()
        {
            var antiga = _tabela;
            _tabela = new long[antiga.Length * 2];
            var mascara = _tabela.Length - 1;
            foreach (var id in antiga)
            {
                if (id != 0)
                {
                    var i = (int)((Misturar(id) / Faixas) & (ulong)mascara);
                    while (_tabela[i] != 0)
                    {
                        i = (i + 1) & mascara;
                    }

                    _tabela[i] = id;
                }
            }
        }
    }
}
