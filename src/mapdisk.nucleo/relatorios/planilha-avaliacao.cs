using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>
/// Planilha .xlsx montada à mão, sem pacote: um zip com o XML das abas. A aba Avaliação é a do
/// cliente, com a lista de opções na coluna Decisão. A aba Controle, oculta, guarda o caminho,
/// os bytes e a data de cada item para casar a resposta.
/// </summary>
public static partial class PlanilhaAvaliacao
{
    public const int LinhaCabecalho = 10;
    public const string AbaAvaliacao = "Avaliação";
    public const string AbaControle = "Controle";

    private static readonly string[] Cabecalho =
        ["Nº", "Nome", "Tipo", "Caminho", "Tamanho", "Arquivos", "Última alteração", "Motivo", "Decisão", "Destino (se Mover)", "Observação"];

    private static readonly UTF8Encoding SemBom = new(false);

    public static void Gravar(Avaliacao a, string arquivo)
    {
        using var fluxo = new FileStream(arquivo, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fluxo, ZipArchiveMode.Create);
        Escrever(zip, "[Content_Types].xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        Escrever(zip, "_rels/.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Escrever(zip, "xl/workbook.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            $"<sheets><sheet name=\"{AbaAvaliacao}\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"{AbaControle}\" sheetId=\"2\" state=\"hidden\" r:id=\"rId2\"/></sheets></workbook>");
        Escrever(zip, "xl/_rels/workbook.xml.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        Escrever(zip, "xl/styles.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs>" +
            "<cellXfs count=\"2\"><xf/><xf fontId=\"1\" applyFont=\"1\"/></cellXfs></styleSheet>");
        Escrever(zip, "xl/worksheets/sheet1.xml", Avaliacao(a));
        Escrever(zip, "xl/worksheets/sheet2.xml", Controle(a));
    }

    private static string Avaliacao(Avaliacao a)
    {
        var linhas = new StringBuilder();
        Linha(linhas, 1, Texto("A1", "Avaliação de espaço - MapDisk - MT", negrito: true));
        Linha(linhas, 2, Texto("A2", "Cliente", true) + Texto("B2", a.Cliente));
        Linha(linhas, 3, Texto("A3", "Pasta", true) + Texto("B3", a.Pasta.CaminhoCompleto()));
        Linha(linhas, 4, Texto("A4", "Varredura", true) + Texto("B4", a.Gerado.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        Linha(linhas, 5, Texto("A5", "Relatório", true) + Texto("B5", a.Numero));
        Linha(linhas, 6, Texto("A6", "Decidido por", true));
        Linha(linhas, 7, Texto("A7", "Data da decisão", true));
        Linha(linhas, 8, Texto("A8", TextosAvaliacao.Autorizacao));
        Linha(linhas, LinhaCabecalho, string.Concat(Cabecalho.Select((c, n) => Texto($"{Coluna(n)}{LinhaCabecalho}", c, true))));
        var r = LinhaCabecalho + 1;
        foreach (var i in a.Itens)
        {
            Linha(linhas, r,
                Numero($"A{r}", i.Numero) + Texto($"B{r}", i.Item.Nome) + Texto($"C{r}", i.Tipo) + Texto($"D{r}", i.Item.Caminho) +
                Texto($"E{r}", Formatador.Tamanho(i.Item.Tamanho)) + Numero($"F{r}", i.Arquivos) + Texto($"G{r}", Formatador.Data(i.Item.Modificacao)) +
                Texto($"H{r}", i.Motivo));
            r++;
        }

        var ultima = Math.Max(LinhaCabecalho + 1, r - 1);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{LinhaCabecalho}\" topLeftCell=\"A{LinhaCabecalho + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
            "<cols><col min=\"1\" max=\"1\" width=\"6\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"30\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"9\" customWidth=\"1\"/>" +
            "<col min=\"4\" max=\"4\" width=\"50\" customWidth=\"1\"/><col min=\"5\" max=\"7\" width=\"14\" customWidth=\"1\"/><col min=\"8\" max=\"8\" width=\"36\" customWidth=\"1\"/>" +
            "<col min=\"9\" max=\"9\" width=\"12\" customWidth=\"1\"/><col min=\"10\" max=\"11\" width=\"30\" customWidth=\"1\"/></cols>" +
            $"<sheetData>{linhas}</sheetData>" +
            $"<dataValidations count=\"1\"><dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" sqref=\"I{LinhaCabecalho + 1}:I{ultima}\">" +
            "<formula1>\"Apagar,Mover,Manter,Conversar\"</formula1></dataValidation></dataValidations></worksheet>";
    }

    private static string Controle(Avaliacao a)
    {
        var linhas = new StringBuilder();
        Linha(linhas, 1, Texto("A1", "relatorio") + Texto("B1", a.Numero));
        Linha(linhas, 2, Texto("A2", "numero") + Texto("B2", "caminho") + Texto("C2", "bytes") + Texto("D2", "ticks"));
        var r = 3;
        foreach (var i in a.Itens)
        {
            Linha(linhas, r, Texto($"A{r}", i.Numero.ToString(CultureInfo.InvariantCulture)) + Texto($"B{r}", i.Item.Caminho) +
                Texto($"C{r}", i.Item.Tamanho.ToString(CultureInfo.InvariantCulture)) + Texto($"D{r}", i.Item.Modificacao.Ticks.ToString(CultureInfo.InvariantCulture)));
            r++;
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            $"<sheetData>{linhas}</sheetData></worksheet>";
    }

    private static void Linha(StringBuilder s, int numero, string celulas) => s.Append($"<row r=\"{numero}\">{celulas}</row>");

    private static string Texto(string referencia, string texto, bool negrito = false) =>
        $"<c r=\"{referencia}\" t=\"inlineStr\"{(negrito ? " s=\"1\"" : string.Empty)}><is><t xml:space=\"preserve\">{SecurityElement.Escape(texto)}</t></is></c>";

    private static string Numero(string referencia, long numero) => $"<c r=\"{referencia}\"><v>{numero}</v></c>";

    private static string Coluna(int indice) => ((char)('A' + indice)).ToString();

    private static void Escrever(ZipArchive zip, string nome, string conteudo)
    {
        using var escritor = new StreamWriter(zip.CreateEntry(nome, CompressionLevel.Optimal).Open(), SemBom);
        escritor.Write(conteudo);
    }
}
