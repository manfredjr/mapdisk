using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace MapDisk;

/// <summary>Ações de leitura no Windows: mostrar no Explorer, copiar o caminho e abrir as propriedades.</summary>
internal static partial class Shell
{
    private const uint PorCaminho = 2;

    public static void MostrarNoExplorer(string caminho, bool ehArquivo) =>
        Process.Start("explorer.exe", ehArquivo ? $"/select,\"{caminho}\"" : $"\"{caminho}\"");

    public static void CopiarCaminho(string caminho) => Clipboard.SetText(caminho);

    /// <summary>Abre o site da MT no navegador padrão. Só quando o técnico clica no logo.</summary>
    public static void AbrirSiteMt() => Process.Start(new ProcessStartInfo("https://manfred.com.br") { UseShellExecute = true });

    public static void Propriedades(nint janela, string caminho) => SHObjectProperties(janela, PorCaminho, caminho, null);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SHObjectProperties(nint janela, uint tipo, string objeto, string? pagina);
}
