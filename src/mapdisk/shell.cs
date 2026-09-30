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

    /// <summary>
    /// Abre o endereço no navegador padrão, só quando o técnico clica. O programa em si não manda nada
    /// para a internet. Se o Windows não tiver navegador, o endereço vai para a área de transferência.
    /// </summary>
    public static void AbrirNoNavegador(string endereco)
    {
        try
        {
            Process.Start(new ProcessStartInfo(endereco) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Clipboard.SetText(endereco);
            MessageBox.Show($"Não foi possível abrir o navegador. O endereço foi copiado:\n{endereco}", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public static void Propriedades(nint janela, string caminho) => SHObjectProperties(janela, PorCaminho, caminho, null);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SHObjectProperties(nint janela, uint tipo, string objeto, string? pagina);
}
