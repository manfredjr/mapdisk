using System.Globalization;
using System.Text.Json;

namespace MapDisk.Nucleo;

public enum Decisao
{
    SemDecisao,
    Apagar,
    Mover,
    Manter,
    Conversar,
}

public sealed record DecisaoItem(int Numero, string Caminho, long Bytes, long Ticks, Decisao Decisao, string Destino, string Observacao);

public sealed record ItemCasado(DecisaoItem Decisao, ItemAcao? Item, string? Problema);

/// <summary>A resposta do cliente, vinda da planilha ou da página. Ela só seleciona: agir passa pela confirmação (regra 1).</summary>
public sealed record RespostaAvaliacao(string Relatorio, string Pasta, string DecididoPor, string Data, string Observacao, IReadOnlyList<DecisaoItem> Itens)
{
    public static RespostaAvaliacao Ler(string arquivo) => Path.GetExtension(arquivo).ToLowerInvariant() switch
    {
        ".xlsx" => PlanilhaAvaliacao.Ler(arquivo),
        ".json" => LerJson(File.ReadAllText(arquivo)),
        _ => throw new FormatException("Escolha a planilha (.xlsx) ou o arquivo de resposta da página (.json)."),
    };

    public static RespostaAvaliacao LerJson(string texto)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(texto);
        }
        catch (JsonException e)
        {
            throw new FormatException("O arquivo de resposta está incompleto ou foi alterado.", e);
        }

        using (doc)
        {
            var r = doc.RootElement;
            if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("formato", out var formato) || formato.GetString() != PaginaAvaliacao.FormatoResposta)
            {
                throw new FormatException("Este arquivo não é uma resposta de relatório do MapDisk.");
            }

            try
            {
                var itens = r.GetProperty("itens").EnumerateArray().Select(i => new DecisaoItem(
                    i.GetProperty("numero").GetInt32(),
                    i.GetProperty("caminho").GetString() ?? string.Empty,
                    Longo(i.GetProperty("bytes")),
                    Longo(i.GetProperty("modificacao")),
                    DecisaoDe(i.GetProperty("decisao").GetString()),
                    Texto(i, "destino"),
                    Texto(i, "observacao"))).ToList();
                return new RespostaAvaliacao(Texto(r, "relatorio"), Texto(r, "pasta"), Texto(r, "decididoPor"), Texto(r, "data"), Texto(r, "observacao"), itens);
            }
            catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
            {
                throw new FormatException("O arquivo de resposta está incompleto ou foi alterado.", e);
            }
        }
    }

    public static Decisao DecisaoDe(string? texto) => texto?.Trim().ToLowerInvariant() switch
    {
        "apagar" => Decisao.Apagar,
        "mover" => Decisao.Mover,
        "manter" => Decisao.Manter,
        "conversar" => Decisao.Conversar,
        _ => Decisao.SemDecisao,
    };

    /// <summary>"1, 3, 7-9" em 1, 3, 7, 8, 9. Vírgula ou ponto e vírgula separam.</summary>
    public static IReadOnlyList<int> Numeros(string texto)
    {
        var numeros = new List<int>();
        foreach (var parte in texto.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var faixa = parte.Split('-', StringSplitOptions.TrimEntries);
            if (faixa.Length == 1 && int.TryParse(faixa[0], out var um))
            {
                numeros.Add(um);
            }
            else if (faixa.Length == 2 && int.TryParse(faixa[0], out var de) && int.TryParse(faixa[1], out var ate) && de <= ate)
            {
                numeros.AddRange(Enumerable.Range(de, ate - de + 1));
            }
            else
            {
                throw new FormatException("Use números e intervalos, como 1, 3, 7-9.");
            }
        }

        return numeros;
    }

    public RespostaAvaliacao ComDecisao(IEnumerable<int> numeros, Decisao decisao)
    {
        var marcar = numeros.ToHashSet();
        return this with { Itens = Itens.Select(i => marcar.Contains(i.Numero) ? i with { Decisao = decisao } : i).ToList() };
    }

    /// <summary>Casa cada item pelo caminho com a varredura atual. Tamanho ou data diferentes: fica fora das ações.</summary>
    public static IReadOnlyList<ItemCasado> Casar(RespostaAvaliacao resposta, NoPasta raiz) => resposta.Itens.Select(d =>
    {
        ItemAcao? item = null;
        if (raiz.Encontrar(d.Caminho) is { Estado: EstadoPasta.Lida } pasta)
        {
            item = ItemAcao.DaPasta(pasta);
        }
        else if (Path.GetDirectoryName(d.Caminho) is { } caminhoDoPai && raiz.Encontrar(caminhoDoPai) is { } pai)
        {
            var nome = Path.GetFileName(d.Caminho);
            foreach (var arquivo in pai.Arquivos)
            {
                if (string.Equals(arquivo.Nome, nome, StringComparison.OrdinalIgnoreCase))
                {
                    item = ItemAcao.DoArquivo(pai, arquivo);
                    break;
                }
            }
        }

        if (item is null)
        {
            return new ItemCasado(d, null, "não encontrado");
        }

        var mudou = item.Tamanho != d.Bytes || (d.Ticks != 0 && item.Modificacao.Ticks != d.Ticks);
        return new ItemCasado(d, item, mudou ? "mudou depois do relatório" : null);
    }).ToList();

    private static string Texto(JsonElement e, string nome) =>
        e.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() ?? string.Empty : string.Empty;

    private static long Longo(JsonElement e) => e.ValueKind == JsonValueKind.Number
        ? e.GetInt64()
        : long.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture);
}
