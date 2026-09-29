using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MapDisk.Nucleo;

public enum ResultadoLeitura
{
    Lida,
    SemAcesso,
    Erro,
}

/// <summary>Subpasta encontrada na leitura de uma pasta.</summary>
public readonly record struct EntradaPasta(string Nome, DateTime Modificacao, bool EhLink);

/// <summary>
/// Lê uma pasta pelo GetFileInformationByHandleEx. Cada chamada devolve um bloco de entradas
/// com nome, tamanho, espaço alocado, atributos, etiqueta de reparse, data e identificador,
/// sem abrir os arquivos. Só lê: nada aqui altera a pasta.
/// </summary>
internal static partial class LeitorPasta
{
    private const uint ListarPasta = 0x0001;
    private const uint CompartilharTudo = 0x0007;
    private const uint AbrirExistente = 3;
    private const uint SemanticaDeBackup = 0x02000000;

    private const int ClasseComIdRecomecar = 11;
    private const int ClasseComId = 10;
    private const int ClasseCompletaRecomecar = 15;
    private const int ClasseCompleta = 14;

    private const int ErroFuncaoInvalida = 1;
    private const int ErroArquivoNaoEncontrado = 2;
    private const int ErroCaminhoNaoEncontrado = 3;
    private const int ErroAcessoNegado = 5;
    private const int ErroSemMaisArquivos = 18;
    private const int ErroNaoSuportado = 50;
    private const int ErroParametroInvalido = 87;

    private const uint AtributoPasta = 0x10;
    private const uint AtributoReparse = 0x400;
    private const uint AtributoOffline = 0x1000;
    private const uint AtributoRecuperarAoAbrir = 0x40000;
    private const uint AtributoRecuperarAoLer = 0x400000;

    private const uint EtiquetaJuncao = 0xA0000003;
    private const uint EtiquetaLinkSimbolico = 0xA000000C;

    // Posições dentro de FILE_ID_BOTH_DIR_INFO e FILE_FULL_DIR_INFO, iguais até o campo EaSize.
    private const int PosProxima = 0;
    private const int PosEscrita = 24;
    private const int PosTamanho = 40;
    private const int PosAlocado = 48;
    private const int PosAtributos = 56;
    private const int PosTamanhoNome = 60;
    private const int PosEtiqueta = 64;
    private const int PosIdComId = 96;
    private const int PosNomeComId = 104;
    private const int PosNomeCompleta = 68;

    private static readonly string[] _arquivosDoSistema = ["pagefile.sys", "hiberfil.sys", "swapfile.sys"];

    // Um buffer por tarefa, reaproveitado a cada pasta: sem isso, cada pasta lida criava 64 KB de lixo.
    [ThreadStatic]
    private static byte[]? _buffer;

    public static unsafe ResultadoLeitura Ler(
        string caminho,
        bool raizDoVolume,
        Func<long, bool> primeiraVez,
        List<ArquivoInfo> arquivos,
        List<EntradaPasta> subpastas,
        out string? motivo)
    {
        motivo = null;
        using var pasta = CreateFile(Alvo.Longo(caminho), ListarPasta, CompartilharTudo, 0, AbrirExistente, SemanticaDeBackup, 0);
        if (pasta.IsInvalid)
        {
            return Falha(Marshal.GetLastPInvokeError(), out motivo);
        }

        var buffer = _buffer ??= new byte[64 * 1024];
        var comId = true;
        var primeira = true;
        fixed (byte* p = buffer)
        {
            while (true)
            {
                var classe = comId
                    ? (primeira ? ClasseComIdRecomecar : ClasseComId)
                    : (primeira ? ClasseCompletaRecomecar : ClasseCompleta);
                if (!GetFileInformationByHandleEx(pasta, classe, p, (uint)buffer.Length))
                {
                    var erro = Marshal.GetLastPInvokeError();
                    if (erro == ErroSemMaisArquivos)
                    {
                        return ResultadoLeitura.Lida;
                    }

                    if (comId && primeira && erro is ErroParametroInvalido or ErroNaoSuportado or ErroFuncaoInvalida)
                    {
                        comId = false;
                        continue;
                    }

                    arquivos.Clear();
                    subpastas.Clear();
                    return Falha(erro, out motivo);
                }

                primeira = false;
                Interpretar(buffer, comId, raizDoVolume, primeiraVez, arquivos, subpastas);
            }
        }
    }

    private static void Interpretar(
        ReadOnlySpan<byte> buffer,
        bool comId,
        bool raizDoVolume,
        Func<long, bool> primeiraVez,
        List<ArquivoInfo> arquivos,
        List<EntradaPasta> subpastas)
    {
        var posNome = comId ? PosNomeComId : PosNomeCompleta;
        var inicio = 0;
        while (true)
        {
            var entrada = buffer[inicio..];
            var proxima = MemoryMarshal.Read<int>(entrada[PosProxima..]);
            var tamanhoNome = MemoryMarshal.Read<int>(entrada[PosTamanhoNome..]);
            var nome = new string(MemoryMarshal.Cast<byte, char>(entrada.Slice(posNome, tamanhoNome)));
            if (nome is not ("." or ".."))
            {
                var escrita = Data(MemoryMarshal.Read<long>(entrada[PosEscrita..]));
                var atributos = MemoryMarshal.Read<uint>(entrada[PosAtributos..]);
                var etiqueta = (atributos & AtributoReparse) != 0 ? MemoryMarshal.Read<uint>(entrada[PosEtiqueta..]) : 0;
                var ehLink = etiqueta is EtiquetaJuncao or EtiquetaLinkSimbolico;
                if ((atributos & AtributoPasta) != 0)
                {
                    subpastas.Add(new EntradaPasta(nome, escrita, ehLink));
                }
                else
                {
                    var marcas = MarcaArquivo.Nenhuma;
                    if (ehLink)
                    {
                        marcas |= MarcaArquivo.Link;
                    }

                    if ((atributos & (AtributoOffline | AtributoRecuperarAoAbrir | AtributoRecuperarAoLer)) != 0)
                    {
                        marcas |= MarcaArquivo.NaNuvem;
                    }

                    if (raizDoVolume && _arquivosDoSistema.Contains(nome, StringComparer.OrdinalIgnoreCase))
                    {
                        marcas |= MarcaArquivo.Sistema;
                    }

                    var id = comId ? MemoryMarshal.Read<long>(entrada[PosIdComId..]) : 0;
                    if (id != 0 && !primeiraVez(id))
                    {
                        marcas |= MarcaArquivo.LinkRepetido;
                    }

                    arquivos.Add(new ArquivoInfo(
                        nome,
                        MemoryMarshal.Read<long>(entrada[PosTamanho..]),
                        MemoryMarshal.Read<long>(entrada[PosAlocado..]),
                        escrita,
                        marcas));
                }
            }

            if (proxima == 0)
            {
                return;
            }

            inicio += proxima;
        }
    }

    private static DateTime Data(long tempoDeArquivo) => tempoDeArquivo is > 0 and < 2_650_000_000_000_000_000
        ? DateTime.FromFileTime(tempoDeArquivo)
        : DateTime.MinValue;

    private static ResultadoLeitura Falha(int erro, out string? motivo)
    {
        if (erro == ErroAcessoNegado)
        {
            motivo = "acesso negado";
            return ResultadoLeitura.SemAcesso;
        }

        motivo = Motivo(erro);
        return ResultadoLeitura.Erro;
    }

    /// <summary>
    /// Motivo em português, sem ponto final: quem mostra a mensagem é que fecha a frase. O
    /// Windows já manda o texto com ponto, e a tela ficava com "encontrado..".
    /// </summary>
    internal static string Motivo(int erro) => erro is ErroArquivoNaoEncontrado or ErroCaminhoNaoEncontrado
        ? "pasta não encontrada"
        : new Win32Exception(erro).Message.Trim().TrimEnd('.');

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFile(string nome, uint acesso, uint compartilhamento, nint seguranca, uint criacao, uint atributos, nint modelo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetFileInformationByHandleEx(SafeFileHandle arquivo, int classe, byte* buffer, uint tamanho);
}
