using System.Runtime.InteropServices;

namespace MapDisk.Nucleo;

/// <summary>Uma unidade ou compartilhamento, com o espaço lido na hora.</summary>
public sealed record InfoVolume(string Raiz, string Rotulo, string SistemaArquivos, long Livre, long Total, long Cluster)
{
    /// <summary>Texto da lista de unidades da janela.</summary>
    public string Descricao => $"{Raiz}  {Rotulo}  {Formatador.Tamanho(Livre)} livres de {Formatador.Tamanho(Total)}";
}

public static partial class Volumes
{
    /// <summary>Unidades prontas (fixas, removíveis e de rede mapeadas), na ordem da letra.</summary>
    public static IReadOnlyList<InfoVolume> ListarUnidades() => DriveInfo.GetDrives()
        .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Network && d.IsReady)
        .Select(d => Ler(d.RootDirectory.FullName))
        .OfType<InfoVolume>()
        .ToList();

    /// <summary>
    /// Espaço, sistema de arquivos e cluster do volume onde está o caminho, que pode ser de
    /// unidade ou de compartilhamento. Null quando o volume não responde.
    /// </summary>
    public static unsafe InfoVolume? Ler(string caminho)
    {
        var raiz = RaizDe(caminho);
        if (!GetDiskFreeSpaceEx(raiz, out var livre, out var total, out _))
        {
            return null;
        }

        var rotulo = stackalloc char[261];
        var sistema = stackalloc char[261];
        var temInfo = GetVolumeInformation(raiz, rotulo, 261, out _, out _, out _, sistema, 261);
        var cluster = GetDiskFreeSpace(raiz, out var setoresPorCluster, out var bytesPorSetor, out _, out _)
            ? (long)setoresPorCluster * bytesPorSetor
            : 0;
        return new InfoVolume(
            raiz,
            temInfo ? new string(rotulo) : string.Empty,
            temInfo ? new string(sistema) : string.Empty,
            (long)livre,
            (long)total,
            cluster);
    }

    /// <summary>"C:\" para caminho local, "\\servidor\compartilhamento\" para caminho de rede.</summary>
    public static string RaizDe(string caminho)
    {
        if (Alvo.EhRede(caminho))
        {
            var partes = caminho[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);
            return $@"\\{partes[0]}\{partes[1]}\";
        }

        return Path.GetPathRoot(caminho)!.ToUpperInvariant();
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetDiskFreeSpaceEx(string pasta, out ulong livreParaUsuario, out ulong total, out ulong livreTotal);

    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetDiskFreeSpace(string raiz, out uint setoresPorCluster, out uint bytesPorSetor, out uint clustersLivres, out uint clustersTotais);

    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetVolumeInformation(string raiz, char* rotulo, int tamanhoRotulo, out uint serie, out uint maiorNome, out uint opcoes, char* sistema, int tamanhoSistema);
}
