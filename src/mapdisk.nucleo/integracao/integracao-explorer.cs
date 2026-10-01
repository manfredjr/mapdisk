using Microsoft.Win32;

namespace MapDisk.Nucleo;

/// <summary>Chaves do registro do usuário atual. Os testes usam a versão em memória.</summary>
public interface IChavesUsuario
{
    void Gravar(string chave, string? nome, string valor);

    string? Ler(string chave, string? nome);

    void ApagarArvore(string chave);
}

/// <summary>HKCU de verdade. Não precisa de administrador (regra 4).</summary>
public sealed class ChavesUsuarioWindows : IChavesUsuario
{
    public void Gravar(string chave, string? nome, string valor)
    {
        using var k = Registry.CurrentUser.CreateSubKey(chave, writable: true);
        k.SetValue(nome ?? string.Empty, valor, RegistryValueKind.String);
    }

    public string? Ler(string chave, string? nome)
    {
        using var k = Registry.CurrentUser.OpenSubKey(chave);
        return k?.GetValue(nome ?? string.Empty) as string;
    }

    public void ApagarArvore(string chave) => Registry.CurrentUser.DeleteSubKeyTree(chave, throwOnMissingSubKey: false);
}

public sealed class ChavesEmMemoria : IChavesUsuario
{
    private readonly Dictionary<(string Chave, string Nome), string> _valores = [];

    public IReadOnlyCollection<string> Todas => _valores.Keys.Select(k => k.Chave).Distinct().ToList();

    public void Gravar(string chave, string? nome, string valor) => _valores[(chave.ToLowerInvariant(), nome ?? string.Empty)] = valor;

    public string? Ler(string chave, string? nome) =>
        _valores.TryGetValue((chave.ToLowerInvariant(), nome ?? string.Empty), out var v) ? v : null;

    public void ApagarArvore(string chave)
    {
        var prefixo = chave.ToLowerInvariant();
        foreach (var k in _valores.Keys.Where(k => k.Chave == prefixo || k.Chave.StartsWith(prefixo + "\\", StringComparison.Ordinal)).ToList())
        {
            _valores.Remove(k);
        }
    }
}

public enum EstadoIntegracao
{
    Desligada,
    Ligada,

    /// <summary>Ligada, mas apontando para outro .exe (o programa mudou de lugar).</summary>
    OutroLocal,
}

/// <summary>Item "Analisar com MapDisk" no menu clássico do Explorer (R19). Sai por completo ao desligar (regra 5).</summary>
public static class IntegracaoExplorer
{
    public const string Texto = "Analisar com MapDisk";

    public static readonly string[] Chaves =
    [
        @"Software\Classes\Directory\shell\MapDisk",
        @"Software\Classes\Drive\shell\MapDisk",
        @"Software\Classes\Directory\Background\shell\MapDisk",
    ];

    public static string ComandoDe(string exe, string marcador) => $"\"{exe}\" {Abertura.Argumento} \"{marcador}\"";

    public static void Ligar(IChavesUsuario chaves, string exe)
    {
        foreach (var chave in Chaves)
        {
            var marcador = chave.Contains(@"\Background\", StringComparison.Ordinal) ? "%V" : "%1";
            chaves.Gravar(chave, null, Texto);
            chaves.Gravar(chave, "Icon", exe + ",0");
            chaves.Gravar(chave + @"\command", null, ComandoDe(exe, marcador));
        }
    }

    public static void Desligar(IChavesUsuario chaves)
    {
        foreach (var chave in Chaves)
        {
            chaves.ApagarArvore(chave);
        }
    }

    /// <summary>O .exe que o item chama hoje, ou null quando desligado.</summary>
    public static string? ExeRegistrado(IChavesUsuario chaves)
    {
        var comando = chaves.Ler(Chaves[0] + @"\command", null);
        if (comando is null || !comando.StartsWith('"'))
        {
            return null;
        }

        var fim = comando.IndexOf('"', 1);
        return fim > 1 ? comando[1..fim] : null;
    }

    public static EstadoIntegracao Estado(IChavesUsuario chaves, string exe) => ExeRegistrado(chaves) switch
    {
        null => EstadoIntegracao.Desligada,
        var e when string.Equals(e, exe, StringComparison.OrdinalIgnoreCase) => EstadoIntegracao.Ligada,
        _ => EstadoIntegracao.OutroLocal,
    };
}
