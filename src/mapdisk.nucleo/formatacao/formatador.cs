using System.Globalization;

namespace MapDisk.Nucleo;

/// <summary>Unidade escolhida na tela para os tamanhos.</summary>
public enum UnidadeExibicao
{
    Automatica,
    GB,
    MB,
    KB,
}

/// <summary>Números, tamanhos e datas no formato brasileiro ("52,7 GB", "1.158.005").</summary>
public static class Formatador
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private const double Kb = 1024d;
    private const double Mb = Kb * 1024;
    private const double Gb = Mb * 1024;
    private const double Tb = Gb * 1024;

    public static string Tamanho(long bytes, UnidadeExibicao unidade = UnidadeExibicao.Automatica) => unidade switch
    {
        UnidadeExibicao.GB => Com(bytes / Gb, "GB"),
        UnidadeExibicao.MB => Com(bytes / Mb, "MB"),
        UnidadeExibicao.KB => Com(bytes / Kb, "KB"),
        _ => Automatico(bytes),
    };

    public static string Porcentagem(double fracao) => Com(fracao * 100, "%");

    public static string Numero(long n) => n.ToString("N0", PtBr);

    public static string Data(DateTime d) => d == DateTime.MinValue ? string.Empty : d.ToString("dd/MM/yyyy", PtBr);

    public static string Duracao(TimeSpan t) => t.TotalSeconds < 60
        ? $"{t.TotalSeconds.ToString("0.0", PtBr)} s"
        : $"{(int)t.TotalMinutes} min {t.Seconds} s";

    public static string Plural(long n, string um, string varios) => n == 1 ? $"1 {um}" : $"{Numero(n)} {varios}";

    // Sobe de unidade quando o número arredondado chegaria a 1.024: "1,0 GB", e não "1.024,0 MB".
    private static string Automatico(long bytes)
    {
        if (bytes == 1)
        {
            return "1 Byte";
        }

        if (bytes < 1024)
        {
            return $"{Numero(bytes)} Bytes";
        }

        string[] sufixos = ["KB", "MB", "GB", "TB"];
        var valor = bytes / Kb;
        var i = 0;
        while (i < sufixos.Length - 1 && Math.Round(valor, 1) >= 1024)
        {
            valor /= 1024;
            i++;
        }

        return Com(valor, sufixos[i]);
    }

    private static string Com(double valor, string sufixo) => $"{valor.ToString("#,##0.0", PtBr)} {sufixo}";
}
