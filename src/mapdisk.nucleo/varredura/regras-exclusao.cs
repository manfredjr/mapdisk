namespace MapDisk.Nucleo;

/// <summary>
/// Pastas que a varredura não lê. Nome (sem barra nem dois-pontos) vale em qualquer nível;
/// caminho completo vale só para aquela pasta. Sem curinga: o técnico precisa saber
/// exatamente o que ficou fora da conta (regra 3).
/// </summary>
public sealed class RegrasExclusao
{
    public const int Maximo = 100;

    public static readonly RegrasExclusao Nenhuma = new([]);

    private readonly HashSet<string> _nomes;
    private readonly HashSet<string> _caminhos;

    private RegrasExclusao(IReadOnlyList<string> regras)
    {
        Regras = regras;
        _nomes = new HashSet<string>(regras.Where(r => !EhCaminho(r)), StringComparer.OrdinalIgnoreCase);
        _caminhos = new HashSet<string>(regras.Where(EhCaminho), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Regras { get; }

    public static RegrasExclusao De(IEnumerable<string> textos)
    {
        var regras = new List<string>();
        foreach (var texto in textos)
        {
            if (Validar(texto, out _) is { } regra
                && !regras.Contains(regra, StringComparer.OrdinalIgnoreCase)
                && regras.Count < Maximo)
            {
                regras.Add(regra);
            }
        }

        return new RegrasExclusao(regras);
    }

    /// <summary>A regra que exclui esta pasta, ou null.</summary>
    public string? Motivo(string caminhoDaPasta, string nome)
    {
        if (_nomes.Count > 0 && _nomes.TryGetValue(nome, out var porNome))
        {
            return porNome;
        }

        return _caminhos.Count > 0 && _caminhos.TryGetValue(caminhoDaPasta.TrimEnd('\\'), out var porCaminho) ? porCaminho : null;
    }

    /// <summary>A regra limpa, pronta para guardar, ou null com o motivo.</summary>
    public static string? Validar(string texto, out string? erro)
    {
        erro = null;
        var t = texto.Trim().Trim('"').Trim();
        if (t.Length == 0)
        {
            erro = "Escreva o nome da pasta ou o caminho completo.";
            return null;
        }

        if (t.IndexOfAny(['*', '?']) >= 0)
        {
            erro = "Use o nome da pasta ou o caminho completo, sem * nem ?";
            return null;
        }

        if (!t.Contains('\\') && !t.Contains('/') && !t.Contains(':'))
        {
            if (t.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                erro = "O nome tem um caractere que o Windows não aceita em pasta.";
                return null;
            }

            return t;
        }

        var caminho = Alvo.Normalizar(t, out _);
        if (caminho is null || !(caminho.Length >= 3 && caminho[1] == ':' || caminho.StartsWith(@"\\", StringComparison.Ordinal)))
        {
            erro = "Use só o nome da pasta, como node_modules, ou o caminho completo, como D:\\Backup.";
            return null;
        }

        // Alvo.Normalizar devolve a raiz da unidade e a do compartilhamento terminando em barra.
        if (caminho.EndsWith('\\'))
        {
            erro = "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.";
            return null;
        }

        return caminho.TrimEnd('\\');
    }

    private static bool EhCaminho(string regra) => regra.Contains('\\');
}

public interface IArmazemExclusoes
{
    RegrasExclusao Ler();

    void Gravar(RegrasExclusao regras);
}

/// <summary>Uma regra por linha, só nesta máquina. Falha de leitura ou de gravação deixa a lista vazia.</summary>
public sealed class ArquivoExclusoes(string arquivo) : IArmazemExclusoes
{
    public static ArquivoExclusoes Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "excluidas.txt"));

    public RegrasExclusao Ler()
    {
        try
        {
            return File.Exists(arquivo) ? RegrasExclusao.De(File.ReadAllLines(arquivo)) : RegrasExclusao.Nenhuma;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return RegrasExclusao.Nenhuma;
        }
    }

    public void Gravar(RegrasExclusao regras)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, regras.Regras);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class ExclusoesEmMemoria : IArmazemExclusoes
{
    private RegrasExclusao _atual = RegrasExclusao.Nenhuma;

    public RegrasExclusao Ler() => _atual;

    public void Gravar(RegrasExclusao regras) => _atual = regras;
}
