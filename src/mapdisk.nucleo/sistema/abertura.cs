namespace MapDisk.Nucleo;

/// <summary>Pedido do Explorer: abrir a janela já varrendo a pasta clicada.</summary>
public static class Abertura
{
    public const string Argumento = "--abrir";

    /// <summary>O Explorer manda a raiz da unidade como "C:\", e o Windows lê a barra antes da aspa como aspa escapada.</summary>
    public static string Corrigir(string alvo) => alvo.EndsWith('"') ? alvo[..^1] + "\\" : alvo;

    public static bool EhPedido(IReadOnlyList<string> args, out string? alvo)
    {
        alvo = args is [var a, var b] && string.Equals(a, Argumento, StringComparison.OrdinalIgnoreCase) ? Corrigir(b) : null;
        return alvo is not null;
    }
}
