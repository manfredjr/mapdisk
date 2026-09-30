namespace MapDisk.Nucleo;

/// <summary>
/// Cores do gráfico, a partir das cores da MT. As subpastas recebem os verdes em ciclo; os
/// arquivos soltos e o resto agrupado ficam em cinza, para não parecerem subpasta.
/// </summary>
public static class Paleta
{
    public static readonly string[] Cores =
    [
        "#006B2D", "#43A92C", "#0F8F2F", "#9AD52B", "#3B6E4A", "#7FBF5F", "#2E8B57", "#B5D98A",
    ];

    public const string CorArquivos = "#8C8C8C";

    public const string CorOutros = "#BDBDBD";

    private static readonly HashSet<string> Claras = ["#9AD52B", "#7FBF5F", "#B5D98A", CorOutros];

    public static string Cor(int posicaoDaPasta, TipoItemGrafico tipo) => tipo switch
    {
        TipoItemGrafico.Arquivos => CorArquivos,
        TipoItemGrafico.Outros => CorOutros,
        _ => Cores[posicaoDaPasta % Cores.Length],
    };

    /// <summary>Grafite sobre as cores claras, branco sobre as escuras.</summary>
    public static string CorTexto(string cor) => Claras.Contains(cor) ? "#202020" : "#FFFFFF";
}
