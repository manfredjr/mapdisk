using System.Globalization;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>
/// CSV com uma linha por pasta, separado por ponto e vírgula e gravado em UTF-8 com BOM, para
/// o Excel em português abrir direto. Pasta que não foi lida sai sem números, nunca com zero.
/// </summary>
public static class ExportadorCsv
{
    public const string Cabecalho = "Caminho;Estado;Tamanho (bytes);Alocado (bytes);Arquivos;Pastas;% da pasta-pai;Última modificação";

    public static void Gravar(NoPasta raiz, string arquivo)
    {
        using var saida = new StreamWriter(arquivo, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Gravar(raiz, saida);
    }

    public static void Gravar(NoPasta raiz, TextWriter saida)
    {
        saida.WriteLine(Cabecalho);
        var pilha = new Stack<(NoPasta No, string Caminho)>();
        pilha.Push((raiz, raiz.Nome));
        while (pilha.Count > 0)
        {
            var (no, caminho) = pilha.Pop();
            saida.WriteLine(Linha(no, caminho));
            var subpastas = no.Subpastas;
            for (var i = subpastas.Count - 1; i >= 0; i--)
            {
                pilha.Push((subpastas[i], Alvo.Juntar(caminho, subpastas[i].Nome)));
            }
        }
    }

    private static string Linha(NoPasta no, string caminho)
    {
        string[] campos = no.Estado == EstadoPasta.Lida
            ? [caminho, Estado(no), N(no.Tamanho), N(no.Alocado), N(no.ArquivosTotal), N(no.PastasTotal), Porcentagem(no), Data(no.UltimaModificacao)]
            : [caminho, Estado(no), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
        return string.Join(';', campos.Select(Campo));
    }

    private static string Estado(NoPasta no) => no.Estado switch
    {
        EstadoPasta.Lida => "lida",
        EstadoPasta.SemAcesso => "sem acesso",
        EstadoPasta.ErroLeitura => "erro de leitura",
        EstadoPasta.Link => "link",
        _ => "não lida",
    };

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);

    private static string Porcentagem(NoPasta no)
    {
        var fracao = no.Pai is null ? 1d : no.Pai.Tamanho > 0 ? (double)no.Tamanho / no.Pai.Tamanho : 0d;
        return (fracao * 100).ToString("0.0", Formatador.PtBr);
    }

    private static string Data(DateTime d) => d == DateTime.MinValue ? string.Empty : d.ToString("dd/MM/yyyy HH:mm", Formatador.PtBr);

    private static string Campo(string texto) => texto.IndexOfAny([';', '"', '\n', '\r']) >= 0
        ? "\"" + texto.Replace("\"", "\"\"") + "\""
        : texto;
}
