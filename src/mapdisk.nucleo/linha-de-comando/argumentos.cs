using System.Globalization;

namespace MapDisk.Nucleo;

public enum ComandoCli
{
    Janela,
    Ajuda,
    Versao,
    Varrer,
    Integrar,
    RemoverIntegracao,
}

/// <summary>Argumentos da linha de comando já interpretados. A linha de comando só lê.</summary>
public sealed class ArgumentosCli
{
    public const string TextoAjuda = """
        MapDisk - MT: onde está o espaço ocupado no disco
        https://mapdisk.manfred.com.br

        Uso:
          mapdisk                          abre a janela
          mapdisk varrer <alvo> [opções]   varre e mostra os maiores itens
          mapdisk --integrar               põe "Analisar com MapDisk" no menu do Explorer
          mapdisk --remover-integracao     tira o item do menu do Explorer
          mapdisk --ajuda                  mostra esta ajuda
          mapdisk --versao                 mostra a versão

        <alvo> é uma unidade (C:), uma pasta (D:\Dados) ou um caminho de rede
        (\\servidor\pasta).

        Opções de varrer:
          --csv <arquivo.csv>         grava todas as pastas num arquivo CSV
          --relatorio <arquivo.html>  grava o relatório com a marca da MT e o gráfico
          --top <n>                   quantos itens mostrar e pôr em cada lista do
                                      relatório, de 1 a 1000 (padrão: 10)

        Exemplos:
          mapdisk varrer C:
          mapdisk varrer D:\ --relatorio d.html
          mapdisk varrer \\servidor\dados --csv dados.csv --top 30

        A linha de comando só lê. Ela nunca apaga nem move arquivos.
        """;

    public ComandoCli Comando { get; private set; } = ComandoCli.Janela;

    /// <summary>O alvo de varrer, já normalizado.</summary>
    public string? Caminho { get; private set; }

    public string? Csv { get; private set; }

    public string? Relatorio { get; private set; }

    public int Top { get; private set; } = 10;

    public List<string> Erros { get; } = [];

    public bool Valido => Erros.Count == 0;

    public static ArgumentosCli Interpretar(IReadOnlyList<string> args)
    {
        var a = new ArgumentosCli();
        var comandos = 0;
        var topDado = false;
        var alvos = new List<string>();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i].Trim();
            switch (arg.ToLowerInvariant())
            {
                case "--ajuda" or "-h" or "--help" or "-?" or "/?":
                    a.Comando = ComandoCli.Ajuda;
                    comandos++;
                    break;
                case "--versao" or "--version":
                    a.Comando = ComandoCli.Versao;
                    comandos++;
                    break;
                case "varrer":
                    a.Comando = ComandoCli.Varrer;
                    comandos++;
                    break;
                case "--integrar":
                    a.Comando = ComandoCli.Integrar;
                    comandos++;
                    break;
                case "--remover-integracao":
                    a.Comando = ComandoCli.RemoverIntegracao;
                    comandos++;
                    break;
                case "--relatorio":
                    a.Relatorio = Valor(args, ref i, arg, a.Erros);
                    break;
                case "--csv":
                    a.Csv = Valor(args, ref i, arg, a.Erros);
                    break;
                case "--top":
                    topDado = true;
                    if (Numero(Valor(args, ref i, arg, a.Erros), 1, 1000, arg, a.Erros) is int top)
                    {
                        a.Top = top;
                    }

                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        a.Erros.Add($"Opção desconhecida: {arg}");
                    }
                    else
                    {
                        alvos.Add(arg);
                    }

                    break;
            }
        }

        if (comandos > 1)
        {
            a.Erros.Add("Use só um comando por vez: varrer, --integrar, --remover-integracao, --ajuda ou --versao.");
        }

        if (a.Comando == ComandoCli.Varrer)
        {
            if (alvos.Count == 0)
            {
                a.Erros.Add("Falta o alvo. Exemplo: mapdisk varrer C:");
            }
            else if (alvos.Count > 1)
            {
                a.Erros.Add("Informe um alvo por vez.");
            }
            else
            {
                a.Caminho = Alvo.Normalizar(alvos[0], out var erro);
                if (a.Caminho is null)
                {
                    a.Erros.Add(erro!);
                }
            }

            if (a.Csv is { } csv && !csv.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                a.Erros.Add("O arquivo de --csv tem que terminar em .csv.");
            }

            if (a.Relatorio is { } rel && !rel.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                a.Erros.Add("O arquivo de --relatorio tem que terminar em .html.");
            }
        }
        else
        {
            if (alvos.Count > 0)
            {
                a.Erros.Add($"Comando desconhecido: {alvos[0]}. Use varrer ou --ajuda.");
            }

            if (a.Csv != null || a.Relatorio != null || topDado)
            {
                a.Erros.Add("As opções --csv, --relatorio e --top pedem o comando varrer.");
            }
        }

        return a;
    }

    private static string? Valor(IReadOnlyList<string> args, ref int i, string opcao, List<string> erros)
    {
        if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            i++;
            return args[i];
        }

        erros.Add($"Falta o valor de {opcao}.");
        return null;
    }

    private static int? Numero(string? texto, int minimo, int maximo, string opcao, List<string> erros)
    {
        if (texto is null)
        {
            return null;
        }

        if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= minimo && n <= maximo)
        {
            return n;
        }

        erros.Add($"Valor inválido em {opcao}: {texto}. Use um número de {minimo} a {maximo}.");
        return null;
    }
}
