using System.Globalization;

namespace MapDisk.Nucleo;

/// <summary>Escolhas do técnico que valem entre sessões, só nesta máquina.</summary>
public sealed record Preferencias(
    int MaioresArquivos = 100,
    long DuplicadosMinimoMb = 1,
    int SugerirPastas = 20,
    int SugerirArquivos = 50,
    int SugerirAnos = 2,
    long SugerirMinimoMb = 100)
{
    private const long Mb = 1024 * 1024;

    private static readonly Dictionary<string, (long Minimo, long Maximo)> Faixas = new()
    {
        ["maiores-arquivos"] = (10, 1000),
        ["duplicados-minimo-mb"] = (1, 102400),
        ["sugerir-pastas"] = (0, 1000),
        ["sugerir-arquivos"] = (0, 1000),
        ["sugerir-anos"] = (1, 50),
        ["sugerir-minimo-mb"] = (0, 1048576),
    };

    public CriteriosSugestao Criterios => new(SugerirPastas, SugerirArquivos, SugerirAnos, SugerirMinimoMb * Mb);

    public Preferencias ComCriterios(CriteriosSugestao c) => this with
    {
        SugerirPastas = c.MaioresPastas,
        SugerirArquivos = c.MaioresArquivos,
        SugerirAnos = c.AnosSemAlteracao,
        SugerirMinimoMb = c.TamanhoMinimo / Mb,
    };

    public IReadOnlyList<string> ParaTexto() =>
    [
        $"maiores-arquivos={N(MaioresArquivos)}",
        $"duplicados-minimo-mb={N(DuplicadosMinimoMb)}",
        $"sugerir-pastas={N(SugerirPastas)}",
        $"sugerir-arquivos={N(SugerirArquivos)}",
        $"sugerir-anos={N(SugerirAnos)}",
        $"sugerir-minimo-mb={N(SugerirMinimoMb)}",
    ];

    /// <summary>Linha que não se entende fica de fora; valor fora da faixa fica com o padrão.</summary>
    public static Preferencias DeTexto(IEnumerable<string> linhas)
    {
        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linha in linhas)
        {
            var i = linha.IndexOf('=');
            if (i > 0)
            {
                valores[linha[..i].Trim()] = linha[(i + 1)..].Trim();
            }
        }

        var p = new Preferencias();
        return p with
        {
            MaioresArquivos = (int)Ler(valores, "maiores-arquivos", p.MaioresArquivos),
            DuplicadosMinimoMb = Ler(valores, "duplicados-minimo-mb", p.DuplicadosMinimoMb),
            SugerirPastas = (int)Ler(valores, "sugerir-pastas", p.SugerirPastas),
            SugerirArquivos = (int)Ler(valores, "sugerir-arquivos", p.SugerirArquivos),
            SugerirAnos = (int)Ler(valores, "sugerir-anos", p.SugerirAnos),
            SugerirMinimoMb = Ler(valores, "sugerir-minimo-mb", p.SugerirMinimoMb),
        };
    }

    /// <summary>Confere um valor digitado na tela de Opções.</summary>
    public static bool Valido(string nome, string texto) =>
        Faixas.TryGetValue(nome, out var f)
        && long.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
        && n >= f.Minimo && n <= f.Maximo;

    private static long Ler(Dictionary<string, string> valores, string nome, long padrao) =>
        valores.TryGetValue(nome, out var texto) && Valido(nome, texto)
            ? long.Parse(texto.Trim(), CultureInfo.InvariantCulture)
            : padrao;

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);
}

public interface IArmazemPreferencias
{
    Preferencias Ler();

    void Gravar(Preferencias preferencias);
}

/// <summary>Opções num arquivo do perfil do usuário. Falha de leitura ou de gravação deixa os padrões.</summary>
public sealed class ArquivoPreferencias(string arquivo) : IArmazemPreferencias
{
    public static ArquivoPreferencias Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "opcoes.txt"));

    public Preferencias Ler()
    {
        try
        {
            return File.Exists(arquivo) ? Preferencias.DeTexto(File.ReadAllLines(arquivo)) : new Preferencias();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new Preferencias();
        }
    }

    public void Gravar(Preferencias preferencias)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, preferencias.ParaTexto());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class PreferenciasEmMemoria : IArmazemPreferencias
{
    private Preferencias _atual = new();

    public Preferencias Ler() => _atual;

    public void Gravar(Preferencias preferencias) => _atual = preferencias;
}
