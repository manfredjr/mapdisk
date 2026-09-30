using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace MapDisk.Nucleo;

/// <summary>Leitura da planilha devolvida pelo cliente: aceita o jeito do Excel (textos compartilhados) e o do LibreOffice (texto na célula).</summary>
public static partial class PlanilhaAvaliacao
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace NsR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace NsRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static RespostaAvaliacao Ler(string arquivo)
    {
        try
        {
            using var zip = ZipFile.OpenRead(arquivo);
            var textos = LerTextos(zip);
            var abas = LerAbas(zip);
            if (!abas.TryGetValue(AbaControle, out var controle))
            {
                throw new PlanilhaSemControleException();
            }

            var avaliacao = abas.TryGetValue(AbaAvaliacao, out var caminho) ? caminho : abas.First(a => a.Key != AbaControle).Value;
            var c = LerCelulas(zip, controle, textos);
            var a = LerCelulas(zip, avaliacao, textos);
            var itens = new List<DecisaoItem>();
            for (var r = 3; c.TryGetValue($"A{r}", out var numeroTexto); r++)
            {
                var numero = int.Parse(numeroTexto, CultureInfo.InvariantCulture);
                var linha = LinhaCabecalho + numero;
                itens.Add(new DecisaoItem(
                    numero,
                    c.GetValueOrDefault($"B{r}", string.Empty),
                    long.Parse(c.GetValueOrDefault($"C{r}", "0"), CultureInfo.InvariantCulture),
                    long.Parse(c.GetValueOrDefault($"D{r}", "0"), CultureInfo.InvariantCulture),
                    RespostaAvaliacao.DecisaoDe(a.GetValueOrDefault($"I{linha}")),
                    a.GetValueOrDefault($"J{linha}", string.Empty),
                    a.GetValueOrDefault($"K{linha}", string.Empty)));
            }

            return new RespostaAvaliacao(c.GetValueOrDefault("B1", string.Empty), a.GetValueOrDefault("B3", string.Empty),
                a.GetValueOrDefault("B6", string.Empty), a.GetValueOrDefault("B7", string.Empty), string.Empty, itens);
        }
        catch (PlanilhaSemControleException)
        {
            throw new FormatException("A planilha não tem a aba Controle. Use a planilha gerada pelo MapDisk.");
        }
        catch (Exception e) when (e is InvalidDataException or XmlException or FormatException or OverflowException or KeyNotFoundException or InvalidOperationException or NullReferenceException)
        {
            throw new FormatException("A planilha está danificada ou não é a do relatório.", e);
        }
    }

    private static List<string> LerTextos(ZipArchive zip)
    {
        if (zip.GetEntry("xl/sharedStrings.xml") is not { } entrada)
        {
            return [];
        }

        using var fluxo = entrada.Open();
        return XDocument.Load(fluxo).Descendants(Ns + "si").Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value))).ToList();
    }

    // Nome de cada aba no caminho do XML dela, pelo livro e pelas relações.
    private static Dictionary<string, string> LerAbas(ZipArchive zip)
    {
        XDocument livro;
        XDocument relacoes;
        using (var f = zip.GetEntry("xl/workbook.xml")!.Open())
        {
            livro = XDocument.Load(f);
        }

        using (var f = zip.GetEntry("xl/_rels/workbook.xml.rels")!.Open())
        {
            relacoes = XDocument.Load(f);
        }

        var alvos = relacoes.Descendants(NsRel + "Relationship").ToDictionary(r => (string)r.Attribute("Id")!, r => (string)r.Attribute("Target")!);
        return livro.Descendants(Ns + "sheet").ToDictionary(
            s => (string)s.Attribute("name")!,
            s =>
            {
                var alvo = alvos[(string)s.Attribute(NsR + "id")!];
                return alvo.StartsWith('/') ? alvo.TrimStart('/') : "xl/" + alvo;
            });
    }

    private static Dictionary<string, string> LerCelulas(ZipArchive zip, string caminho, List<string> textos)
    {
        using var fluxo = zip.GetEntry(caminho)!.Open();
        var celulas = new Dictionary<string, string>();
        foreach (var c in XDocument.Load(fluxo).Descendants(Ns + "c"))
        {
            if ((string?)c.Attribute("r") is not { } referencia)
            {
                continue;
            }

            var valor = (string?)c.Element(Ns + "v");
            celulas[referencia] = (string?)c.Attribute("t") switch
            {
                "s" when int.TryParse(valor, out var i) && i < textos.Count => textos[i],
                "inlineStr" => string.Concat(c.Descendants(Ns + "t").Select(t => t.Value)),
                _ => valor ?? string.Empty,
            };
        }

        return celulas;
    }

    // Separa a planilha sem a aba Controle das planilhas danificadas, que têm outra mensagem.
    private sealed class PlanilhaSemControleException : Exception;
}
