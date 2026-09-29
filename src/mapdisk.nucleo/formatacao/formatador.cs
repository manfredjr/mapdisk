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
        _ => bytes switch
        {
            1 => "1 Byte",
            < 1024 => $"{Numero(bytes)} Bytes",
            < 1024L * 1024 => Com(bytes / Kb, "KB"),
            < 1024L * 1024 * 1024 => Com(bytes / Mb, "MB"),
            < 1024L * 1024 * 1024 * 1024 => Com(bytes / Gb, "GB"),
            _ => Com(bytes / Tb, "TB"),
        },
    };

    public static string Porcentagem(double fracao) => Com(fracao * 100, "%");

    public static string Numero(long n) => n.ToString("N0", PtBr);

    public static string Data(DateTime d) => d == DateTime.MinValue ? string.Empty : d.ToString("dd/MM/yyyy", PtBr);

    public static string Duracao(TimeSpan t) => t.TotalSeconds < 60
        ? $"{t.TotalSeconds.ToString("0.0", PtBr)} s"
        : $"{(int)t.TotalMinutes} min {t.Seconds} s";

    public static string Plural(long n, string um, string varios) => n == 1 ? $"1 {um}" : $"{Numero(n)} {varios}";

    private static string Com(double valor, string sufixo) => $"{valor.ToString("#,##0.0", PtBr)} {sufixo}";
}
