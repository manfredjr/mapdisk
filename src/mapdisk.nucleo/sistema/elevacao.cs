using System.ComponentModel;
using System.Diagnostics;

namespace MapDisk.Nucleo;

public enum ResultadoElevacao
{
    Aberta,

    /// <summary>O técnico disse não no aviso do Windows, ou o Windows não deixou.</summary>
    Recusada,
}

/// <summary>Reabre o programa como administrador, já varrendo o alvo.</summary>
public static class Elevacao
{
    public const string Argumento = "--elevado";

    private const int CanceladaPeloUsuario = 1223;

    /// <summary>
    /// Argumentos da nova janela. A barra final é dobrada: sem isso, o Windows lê "C:\" entre
    /// aspas como aspas escapadas.
    /// </summary>
    public static string Argumentos(string alvo) => $"{Argumento} \"{(alvo.EndsWith('\\') ? alvo + "\\" : alvo)}\"";

    public static bool EhPedido(IReadOnlyList<string> args, out string? alvo)
    {
        alvo = args is [Argumento, var a] ? a : null;
        return alvo is not null;
    }

    public static ResultadoElevacao Reabrir(string alvo)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, Argumentos(alvo))
            {
                UseShellExecute = true,
                Verb = "runas",
            });
            return ResultadoElevacao.Aberta;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == CanceladaPeloUsuario)
        {
            return ResultadoElevacao.Recusada;
        }
    }
}
