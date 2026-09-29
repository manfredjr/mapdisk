using System.Runtime.InteropServices;
using System.Security.Principal;

namespace MapDisk.Nucleo;

/// <summary>
/// Administrador e privilégio de backup. Com o privilégio ligado, o CreateFile com
/// FILE_FLAG_BACKUP_SEMANTICS que o leitor já usa lê qualquer pasta local. Só leitura: o
/// programa nunca pede acesso de escrita.
/// </summary>
public static partial class Privilegios
{
    private const uint AjustarPrivilegios = 0x0020;
    private const uint Consultar = 0x0008;
    private const uint Ligado = 0x00000002;
    private const int NemTodosAtribuidos = 1300;

    public static bool EhAdministrador()
    {
        using var identidade = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identidade).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Liga o SeBackupPrivilege neste processo. Só dá certo com o processo elevado.</summary>
    public static bool LigarBackup()
    {
        if (!OpenProcessToken(GetCurrentProcess(), AjustarPrivilegios | Consultar, out var token))
        {
            return false;
        }

        try
        {
            if (!LookupPrivilegeValue(null, "SeBackupPrivilege", out var luid))
            {
                return false;
            }

            var novo = new PrivilegiosDoToken { Quantidade = 1, Luid = luid, Atributos = Ligado };
            if (!AdjustTokenPrivileges(token, false, ref novo, 0, 0, 0))
            {
                return false;
            }

            return Marshal.GetLastPInvokeError() != NemTodosAtribuidos;
        }
        finally
        {
            CloseHandle(token);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint Baixo;
        public int Alto;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrivilegiosDoToken
    {
        public uint Quantidade;
        public Luid Luid;
        public uint Atributos;
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint objeto);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(nint processo, uint acesso, out nint token);

    [LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LookupPrivilegeValue(string? sistema, string nome, out Luid luid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AdjustTokenPrivileges(nint token, [MarshalAs(UnmanagedType.Bool)] bool desligarTodos, ref PrivilegiosDoToken novo, uint tamanhoAnterior, nint anterior, nint tamanhoDevolvido);
}
