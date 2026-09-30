namespace MapDisk.Nucleo;

public sealed record ItemAvaliacao(int Numero, ItemAcao Item, string Motivo)
{
    public string Tipo => Item.EhPasta ? "Pasta" : "Arquivo";

    public long Arquivos => Item.EhPasta ? Item.Pasta.ArquivosTotal : 1;
}

/// <summary>
/// Os candidatos que o cliente vai avaliar. Pasta sem leitura não entra (regra 3). O que está
/// dentro de uma pasta da lista não entra de novo: a decisão sobre a pasta já o leva.
/// </summary>
public sealed class ListaAvaliacao
{
    private readonly List<(ItemAcao Item, List<string> Motivos)> _itens = [];

    public IReadOnlyList<ItemAvaliacao> Itens => _itens
        .OrderByDescending(i => i.Item.Tamanho)
        .Select((i, n) => new ItemAvaliacao(n + 1, i.Item, string.Join("; ", i.Motivos)))
        .ToList();

    public long Total => _itens.Sum(i => i.Item.Tamanho);

    public void Acrescentar(ItemAcao item, string motivo)
    {
        if (item.EhPasta && item.Pasta.Estado != EstadoPasta.Lida)
        {
            return;
        }

        var existente = _itens.FindIndex(i => Igual(i.Item, item));
        if (existente >= 0)
        {
            if (!_itens[existente].Motivos.Contains(motivo))
            {
                _itens[existente].Motivos.Add(motivo);
            }

            return;
        }

        if (_itens.Any(i => i.Item.EhPasta && Protecao.Dentro(item.Caminho, i.Item.Caminho)))
        {
            return;
        }

        if (item.EhPasta)
        {
            _itens.RemoveAll(i => Protecao.Dentro(i.Item.Caminho, item.Caminho));
        }

        _itens.Add((item, [motivo]));
    }

    public void Tirar(IEnumerable<int> numeros)
    {
        var sair = Itens.Where(i => numeros.Contains(i.Numero)).Select(i => i.Item.Caminho).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _itens.RemoveAll(i => sair.Contains(i.Item.Caminho));
    }

    public void Limpar() => _itens.Clear();

    private static bool Igual(ItemAcao a, ItemAcao b) => string.Equals(a.Caminho, b.Caminho, StringComparison.OrdinalIgnoreCase);
}
