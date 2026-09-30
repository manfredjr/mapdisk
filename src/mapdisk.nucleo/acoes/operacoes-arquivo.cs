using System.Runtime.InteropServices;

namespace MapDisk.Nucleo;

/// <summary>O que as ações fazem no disco. Os testes e a demonstração trocam por versões próprias.</summary>
public interface IOperacoesArquivo
{
    /// <summary>O que mudou desde a varredura, ou null quando está igual.</summary>
    string? Mudanca(ItemAcao item);

    /// <summary>Espaço livre no volume da pasta, ou null quando o volume não responde.</summary>
    long? Livre(string pasta);

    bool MesmoVolume(string origem, string pastaDestino);

    void EnviarParaLixeira(string caminho);

    void ExcluirDefinitivo(string caminho, bool ehPasta);

    void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar);
}

public sealed class OperacoesArquivo : IOperacoesArquivo
{
    // Atributos que dizem que o arquivo está só na nuvem: copiar forçaria o download (regra 7).
    private const FileAttributes SoNaNuvem = FileAttributes.Offline | (FileAttributes)0x00040000 | (FileAttributes)0x00400000;

    private const uint ApagarArquivo = 3;
    private const ushort Silencioso = 0x0004;
    private const ushort SemConfirmacao = 0x0010;
    private const ushort PermitirDesfazer = 0x0040;
    private const ushort SemTelaDeErro = 0x0400;
    private const ushort AvisarSeApagarDireto = 0x4000;

    private readonly Func<string, string, bool> _mesmoVolume;

    public OperacoesArquivo(Func<string, string, bool>? mesmoVolume = null)
    {
        _mesmoVolume = mesmoVolume ?? ((a, b) => string.Equals(Volumes.RaizDe(a), Volumes.RaizDe(b), StringComparison.OrdinalIgnoreCase));
    }

    public string? Mudanca(ItemAcao item)
    {
        if (item.EhPasta)
        {
            var pasta = new DirectoryInfo(item.Caminho);
            if (!pasta.Exists)
            {
                return "não existe mais";
            }

            // A raiz da varredura não tem data da varredura: confere só se existe.
            return item.Modificacao != default && pasta.LastWriteTime != item.Modificacao ? "mudou desde a varredura" : null;
        }

        var arquivo = new FileInfo(item.Caminho);
        if (!arquivo.Exists)
        {
            return "não existe mais";
        }

        return arquivo.Length != item.Tamanho || arquivo.LastWriteTime != item.Modificacao ? "mudou desde a varredura" : null;
    }

    public long? Livre(string pasta) => Volumes.Ler(pasta)?.Livre;

    public bool MesmoVolume(string origem, string pastaDestino) => _mesmoVolume(origem, pastaDestino);

    /// <summary>
    /// Lixeira pelo Shell do Windows. O pedido de aviso faz o próprio Windows perguntar antes de
    /// apagar direto, quando o item não cabe na Lixeira. Roda numa thread STA (ExecutorAcoes).
    /// </summary>
    public void EnviarParaLixeira(string caminho)
    {
        var operacao = new OperacaoShell
        {
            Funcao = ApagarArquivo,
            De = caminho + "\0\0",
            Opcoes = (ushort)(Silencioso | SemConfirmacao | PermitirDesfazer | SemTelaDeErro | AvisarSeApagarDireto),
        };
        var codigo = SHFileOperation(ref operacao);
        if (operacao.Abortada)
        {
            throw new OperationCanceledException("Cancelado no aviso do Windows.");
        }

        if (codigo != 0)
        {
            throw new IOException($"O Windows não enviou para a Lixeira (código {codigo}).");
        }
    }

    public void ExcluirDefinitivo(string caminho, bool ehPasta)
    {
        if (!ehPasta)
        {
            File.SetAttributes(caminho, FileAttributes.Normal);
            File.Delete(caminho);
            return;
        }

        RecusarLinkOuNuvem(caminho);
        foreach (var arquivo in Directory.EnumerateFiles(caminho, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(arquivo) & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(arquivo, FileAttributes.Normal);
            }
        }

        Directory.Delete(caminho, recursive: true);
    }

    public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar)
    {
        var destino = Path.Combine(pastaDestino, Path.GetFileName(origem.TrimEnd('\\')));
        if (File.Exists(destino) || Directory.Exists(destino))
        {
            throw new IOException("Já existe um item com esse nome no destino.");
        }

        if (MesmoVolume(origem, pastaDestino))
        {
            if (ehPasta)
            {
                Directory.Move(origem, destino);
            }
            else
            {
                File.Move(origem, destino);
            }

            return;
        }

        if (ehPasta)
        {
            RecusarLinkOuNuvem(origem);
        }

        try
        {
            if (ehPasta)
            {
                CopiarPasta(origem, destino, cancelar);
            }
            else
            {
                cancelar.ThrowIfCancellationRequested();
                File.Copy(origem, destino);
            }

            Conferir(origem, destino, ehPasta);
        }
        catch
        {
            ApagarCopia(destino, ehPasta);
            throw;
        }

        // A cópia foi conferida. Só agora a origem sai.
        try
        {
            ExcluirDefinitivo(origem, ehPasta);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"A cópia está em {destino}, mas a origem não saiu por inteiro: {e.Message}", e);
        }
    }

    private static void RecusarLinkOuNuvem(string pasta)
    {
        foreach (var entrada in new DirectoryInfo(pasta).EnumerateFileSystemInfos("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
        {
            if ((entrada.Attributes & (FileAttributes.ReparsePoint | SoNaNuvem)) != 0)
            {
                throw new IOException($"Contém link ou arquivo só na nuvem ({entrada.FullName}). Trate esta pasta pelo Explorer.");
            }
        }
    }

    private static void CopiarPasta(string origem, string destino, CancellationToken cancelar)
    {
        Directory.CreateDirectory(destino);
        foreach (var arquivo in Directory.EnumerateFiles(origem))
        {
            cancelar.ThrowIfCancellationRequested();
            File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)));
        }

        foreach (var sub in Directory.EnumerateDirectories(origem))
        {
            cancelar.ThrowIfCancellationRequested();
            CopiarPasta(sub, Path.Combine(destino, Path.GetFileName(sub)), cancelar);
        }
    }

    // Mesma quantidade de arquivos e mesmo tamanho em cada um.
    private static void Conferir(string origem, string destino, bool ehPasta)
    {
        if (!ehPasta)
        {
            if (new FileInfo(origem).Length != new FileInfo(destino).Length)
            {
                throw new IOException("A cópia não ficou do mesmo tamanho da origem.");
            }

            return;
        }

        var arquivos = Directory.GetFiles(origem, "*", SearchOption.AllDirectories);
        if (arquivos.Length != Directory.GetFiles(destino, "*", SearchOption.AllDirectories).Length)
        {
            throw new IOException("A cópia não ficou com a mesma quantidade de arquivos da origem.");
        }

        foreach (var arquivo in arquivos)
        {
            var copia = Path.Combine(destino, Path.GetRelativePath(origem, arquivo));
            if (!File.Exists(copia) || new FileInfo(copia).Length != new FileInfo(arquivo).Length)
            {
                throw new IOException($"A cópia de {arquivo} não confere.");
            }
        }
    }

    // A cópia parcial é do próprio programa: apagar não perde dado do cliente.
    private static void ApagarCopia(string destino, bool ehPasta)
    {
        try
        {
            if (ehPasta && Directory.Exists(destino))
            {
                foreach (var arquivo in Directory.EnumerateFiles(destino, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(arquivo, FileAttributes.Normal);
                }

                Directory.Delete(destino, recursive: true);
            }
            else if (!ehPasta && File.Exists(destino))
            {
                File.SetAttributes(destino, FileAttributes.Normal);
                File.Delete(destino);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Fica para o técnico: a origem está intacta.
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OperacaoShell
    {
        public nint Janela;
        public uint Funcao;
        public string De;
        public string? Para;
        public ushort Opcoes;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Abortada;
        public nint Nomes;
        public string? Titulo;
    }

    // A estrutura leva texto, que o LibraryImport não converte sozinho.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref OperacaoShell operacao);
}
