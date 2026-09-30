using System.Globalization;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>Registro das ações. Quem chama não age se a gravação falhar (regra 1).</summary>
public interface IRegistroAcoes
{
    string Local { get; }

    void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado);
}

/// <summary>
/// Uma linha por chamada, com tabulação entre os campos, para abrir no Excel. Guarda só o que a
/// verificação jurídica pede (seção 4.3): data e hora, usuário do Windows, ação, origem,
/// destino, bytes e resultado.
/// </summary>
public sealed class RegistroAcoes : IRegistroAcoes
{
    public const string Cabecalho = "data e hora\tusuário\tação\torigem\tdestino\tbytes\tresultado";

    private static readonly UTF8Encoding SemBom = new(false);
    private readonly Func<DateTime> _agora;
    private readonly string _usuario;

    public RegistroAcoes(string arquivo, Func<DateTime> agora, string usuario)
    {
        Local = arquivo;
        _agora = agora;
        _usuario = usuario;
    }

    public string Local { get; }

    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk");

    public static RegistroAcoes Padrao() => new(
        Path.Combine(PastaPadrao, "acoes.log"),
        () => DateTime.Now,
        $@"{Environment.UserDomainName}\{Environment.UserName}");

    public void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Local)!);
        var linha = string.Join('\t',
            _agora().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            _usuario,
            acao switch { TipoAcao.Lixeira => "lixeira", TipoAcao.Mover => "mover", _ => "excluir" },
            origem,
            destino ?? string.Empty,
            bytes.ToString(CultureInfo.InvariantCulture),
            Limpo(resultado));
        var texto = File.Exists(Local) ? linha + Environment.NewLine : Cabecalho + Environment.NewLine + linha + Environment.NewLine;
        File.AppendAllText(Local, texto, SemBom);
    }

    private static string Limpo(string texto) => texto.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
