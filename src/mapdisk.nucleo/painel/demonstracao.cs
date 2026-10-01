namespace MapDisk.Nucleo;

/// <summary>
/// Dados de exemplo para a janela com --demonstracao, para prints da página e do README. Nomes
/// fictícios, nada lido do disco.
/// </summary>
public static class Demonstracao
{
    public const string Argumento = "--demonstracao";

    private const long Gb = 1024L * 1024 * 1024;

    private static readonly InfoVolume _volume = new(@"C:\", "Sistema", "NTFS", 373 * Gb, 952 * Gb, 4096);

    // Nada toca no disco: os nomes fictícios podem existir de verdade na máquina.
    public static PainelPrincipal Painel() => new(new DependenciasPainel
    {
        Motor = Motor(),
        ListarUnidades = () => [_volume],
        Operacoes = new OperacoesDemonstracao(),
        Registro = new RegistroEmMemoria(),
        Leitor = new LeitorDemonstracao(),
        Exclusoes = ExclusoesDaDemonstracao(),
        Demonstracao = true,
    });

    private static ExclusoesEmMemoria ExclusoesDaDemonstracao()
    {
        var exclusoes = new ExclusoesEmMemoria();
        exclusoes.Gravar(RegrasExclusao.De([Lixeira]));
        return exclusoes;
    }

    public static IMotorVarredura Motor() => new MotorDemonstracao();

    // A pasta da Lixeira entra excluída, para a tela de exemplo mostrar o estado "excluída".
    private const string Lixeira = "$Recycle.Bin";

    internal static NoPasta Montar(string alvo)
    {
        var d = new DateTime(2026, 9, 25, 10, 0, 0);
        var raiz = new NoPasta(alvo, null, d);
        var users = new NoPasta("Users", raiz, d);
        var windows = new NoPasta("Windows", raiz, d);
        var programas = new NoPasta("Program Files", raiz, d);
        var dados = new NoPasta("Dados", raiz, d);
        var sistema = new NoPasta("System Volume Information", raiz, d);
        var lixeira = new NoPasta(Lixeira, raiz, d);
        raiz.Preencher(
            [
                new ArquivoInfo("pagefile.sys", 16 * Gb, 16 * Gb, d, MarcaArquivo.Sistema),
                new ArquivoInfo("hiberfil.sys", 12 * Gb, 12 * Gb, d, MarcaArquivo.Sistema),
            ],
            [users, windows, programas, dados, sistema, lixeira]);
        sistema.MarcarSemAcesso("acesso negado");
        lixeira.MarcarExcluida(Lixeira);

        var ana = new NoPasta("ana.souza", users, d);
        var bruno = new NoPasta("bruno.lima", users, d);
        var publico = new NoPasta("Public", users, d);
        users.Preencher([], [ana, bruno, publico]);
        Encher(ana, d, ("Videos", 48 * Gb, 212), ("Downloads", 19 * Gb, 1840), ("Documents", 6 * Gb, 5230));
        Encher(bruno, d, ("Desktop", 9 * Gb, 830), ("AppData", 14 * Gb, 22000));
        Encher(publico, d, ("Documents", 1 * Gb, 40));
        Encher(windows, d, ("System32", 9 * Gb, 19000), ("WinSxS", 11 * Gb, 60000), ("Temp", 3 * Gb, 2400));
        Encher(programas, d, ("Office", 4 * Gb, 5600), ("Sistemas", 7 * Gb, 9800));
        // "Backup antigo" repete dois arquivos de "Backup", para a aba Duplicados mostrar grupos.
        Encher(dados, d, ("Backup", 120 * Gb, 48), ("Fotos", 31 * Gb, 15000), ("Backup antigo", 5 * Gb, 2));
        return raiz;
    }

    private static void Encher(NoPasta pasta, DateTime data, params (string Nome, long Bytes, int Arquivos)[] filhas)
    {
        var subpastas = filhas.Select(f => new NoPasta(f.Nome, pasta, data)).ToArray();
        pasta.Preencher([], subpastas);
        for (var i = 0; i < filhas.Length; i++)
        {
            var (nome, bytes, quantos) = filhas[i];
            var extensao = Extensao(nome);
            var cada = bytes / quantos;
            var alocado = (cada + 4095) / 4096 * 4096;
            subpastas[i].Preencher(
                Enumerable.Range(1, quantos)
                    .Select(n => new ArquivoInfo($"arquivo-{n:D5}{extensao}", cada, alocado, data.AddDays(-n), MarcaArquivo.Nenhuma))
                    .ToArray(),
                []);
        }
    }

    // Extensão de exemplo conforme a pasta, para o resumo por tipo da demonstração ter categorias.
    private static string Extensao(string pasta) => pasta switch
    {
        "Videos" => ".mp4",
        "Fotos" => ".jpg",
        "Downloads" => ".zip",
        "Documents" or "Desktop" => ".pdf",
        "Backup" or "Backup antigo" => ".bak",
        "Office" or "Sistemas" => ".dll",
        "AppData" => ".pst",
        _ => ".dat",
    };

    private sealed class MotorDemonstracao : IMotorVarredura
    {
        public Varredura Iniciar(string alvo, CancellationToken cancelar)
        {
            var raiz = Montar(alvo);
            return new Varredura(raiz).Comecar(_ => Task.FromResult(new ResultadoVarredura
            {
                Raiz = raiz,
                Volume = _volume,
                Duracao = TimeSpan.FromSeconds(12.4),
                Cancelada = false,
            }));
        }
    }
}
