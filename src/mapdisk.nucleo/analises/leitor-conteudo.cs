using System.Buffers;
using System.Security.Cryptography;

namespace MapDisk.Nucleo;

/// <summary>Resumo (SHA-256) dos primeiros bytes de um arquivo. long.MaxValue é o arquivo inteiro.</summary>
public interface ILeitorConteudo
{
    byte[] Resumo(string caminho, long bytes, CancellationToken cancelar);
}

/// <summary>
/// Lê o conteúdo em partes de 1 MB, sem guardar nada além do resumo. É a única leitura de
/// conteúdo do programa, e só roda quando o técnico pede os duplicados (regra 7).
/// </summary>
public sealed class LeitorConteudo : ILeitorConteudo
{
    public const int Parte = 1024 * 1024;

    public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar)
    {
        using var fluxo = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, Parte, FileOptions.SequentialScan);
        using var resumo = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(Parte);
        try
        {
            long lidos = 0;
            int n;
            while (lidos < bytes && (n = fluxo.Read(buffer, 0, (int)Math.Min(Parte, bytes - lidos))) > 0)
            {
                cancelar.ThrowIfCancellationRequested();
                resumo.AppendData(buffer, 0, n);
                lidos += n;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return resumo.GetHashAndReset();
    }
}

/// <summary>
/// Demonstração: não abre arquivo. O resumo é o nome; como a primeira etapa já separou por
/// tamanho, arquivos de mesmo nome e mesmo tamanho viram grupo.
/// </summary>
public sealed class LeitorDemonstracao : ILeitorConteudo
{
    public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar) =>
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(caminho)));
}
