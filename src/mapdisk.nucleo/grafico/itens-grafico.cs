namespace MapDisk.Nucleo;

public enum TipoItemGrafico
{
    Pasta,
    Arquivos,
    Outros,
}

/// <summary>Um bloco ou uma fatia: uma subpasta, os arquivos soltos da pasta ou o resto agrupado.</summary>
public sealed record ItemGrafico(string Nome, long Valor, TipoItemGrafico Tipo, NoPasta? Pasta);

/// <summary>Itens do maior para o menor, o total deles e as subpastas sem leitura, que ficam fora do desenho.</summary>
public sealed record ConteudoGrafico(IReadOnlyList<ItemGrafico> Itens, long Total, IReadOnlyList<string> NaoLidas);

/// <summary>
/// O que o gráfico desenha de uma pasta: um nível abaixo dela, no mesmo critério da árvore.
/// Pasta sem leitura não vira item de valor 0 (regra 3), e link não soma (regra 9).
/// </summary>
public static class ItensGrafico
{
    public const string NomeArquivos = "Arquivos nesta pasta";

    public static ConteudoGrafico DaPasta(NoPasta pasta, ModoExibicao modo, int maximo)
    {
        var itens = new List<ItemGrafico>();
        var naoLidas = new List<string>();
        foreach (var sub in pasta.Subpastas)
        {
            if (sub.Estado == EstadoPasta.Link)
            {
                continue;
            }

            if (sub.Estado != EstadoPasta.Lida)
            {
                naoLidas.Add(sub.Nome);
                continue;
            }

            var valor = Valor(sub, modo);
            if (valor > 0)
            {
                itens.Add(new ItemGrafico(sub.Nome, valor, TipoItemGrafico.Pasta, sub));
            }
        }

        var proprios = modo switch
        {
            ModoExibicao.Alocado => pasta.AlocadoProprio,
            ModoExibicao.Contagem => pasta.Arquivos.Count,
            _ => pasta.TamanhoProprio,
        };
        if (proprios > 0)
        {
            itens.Add(new ItemGrafico(NomeArquivos, proprios, TipoItemGrafico.Arquivos, null));
        }

        itens.Sort((a, b) => b.Valor.CompareTo(a.Valor));
        if (itens.Count > maximo)
        {
            var resto = itens.Skip(maximo - 1).ToList();
            itens = itens.Take(maximo - 1).ToList();
            itens.Add(new ItemGrafico(
                Formatador.Plural(resto.Count, "outro item", "outros itens"),
                resto.Sum(r => r.Valor),
                TipoItemGrafico.Outros,
                null));
        }

        return new ConteudoGrafico(itens, itens.Sum(i => i.Valor), naoLidas);
    }

    private static long Valor(NoPasta pasta, ModoExibicao modo) => modo switch
    {
        ModoExibicao.Alocado => pasta.Alocado,
        ModoExibicao.Contagem => pasta.ArquivosTotal,
        _ => pasta.Tamanho,
    };
}
