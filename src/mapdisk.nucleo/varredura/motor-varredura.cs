using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapDisk.Nucleo;

public sealed class ResultadoVarredura
{
    public required NoPasta Raiz { get; init; }

    public required InfoVolume? Volume { get; init; }

    public required TimeSpan Duracao { get; init; }

    /// <summary>Interrompida antes do fim. Os números mostram só o que foi lido até ali.</summary>
    public required bool Cancelada { get; init; }

    /// <summary>Pastas que ficaram sem leitura numa varredura que não foi interrompida. Deve ser 0.</summary>
    public int PastasNaoLidas { get; init; }
}

/// <summary>Uma varredura em andamento. A raiz existe desde o início e vai sendo preenchida.</summary>
public sealed class Varredura
{
    private string _pastaAtual;

    public Varredura(NoPasta raiz)
    {
        Raiz = raiz;
        _pastaAtual = raiz.Nome;
    }

    public NoPasta Raiz { get; }

    public string PastaAtual
    {
        get => Volatile.Read(ref _pastaAtual);
        internal set => Volatile.Write(ref _pastaAtual, value);
    }

    public Task<ResultadoVarredura> Conclusao { get; private set; } = Task.FromResult<ResultadoVarredura>(null!);

    public Varredura Comecar(Func<Varredura, Task<ResultadoVarredura>> executar)
    {
        Conclusao = executar(this);
        return this;
    }
}

public interface IMotorVarredura
{
    /// <summary>Começa a varrer o alvo, já normalizado, e devolve na hora, com a raiz para a tela.</summary>
    Varredura Iniciar(string alvo, CancellationToken cancelar);

    /// <summary>Lê de novo só esta pasta. O padrão não relê nada e devolve a mesma pasta.</summary>
    Varredura Reler(NoPasta pasta, CancellationToken cancelar) =>
        new Varredura(pasta).Comecar(_ => Task.FromResult(new ResultadoVarredura
        {
            Raiz = pasta,
            Volume = null,
            Duracao = TimeSpan.Zero,
            Cancelada = false,
        }));
}

/// <summary>
/// Varredura com várias tarefas lendo pastas ao mesmo tempo, com teto: até 16 em disco local e
/// 4 em caminho de rede, para não pesar no servidor. Só lê.
/// </summary>
public sealed class MotorVarredura(int? tarefas = null) : IMotorVarredura
{
    public Varredura Iniciar(string alvo, CancellationToken cancelar) => Comecar(new NoPasta(alvo, null), alvo, cancelar);

    /// <summary>
    /// Lê de novo só esta pasta: tira da árvore o que ela somava, troca pela pasta nova e varre
    /// só ela. A raiz é varrida inteira. Link não é relido.
    /// </summary>
    public Varredura Reler(NoPasta pasta, CancellationToken cancelar)
    {
        if (pasta.Estado == EstadoPasta.Link)
        {
            return ((IMotorVarredura)new MotorDeNada()).Reler(pasta, cancelar);
        }

        if (pasta.Pai is null)
        {
            return Iniciar(pasta.Nome, cancelar);
        }

        var caminho = pasta.CaminhoCompleto();
        var nova = new NoPasta(pasta.Nome, pasta.Pai, pasta.ModificacaoPropria);
        pasta.DescontarAcima();
        pasta.Pai.TrocarSubpasta(pasta, nova);
        return Comecar(nova, caminho, cancelar);
    }

    private Varredura Comecar(NoPasta raiz, string caminho, CancellationToken cancelar)
    {
        var quantas = tarefas ?? (Alvo.EhRede(caminho) ? 4 : Math.Clamp(Environment.ProcessorCount, 4, 16));
        return new Varredura(raiz).Comecar(v => Task.Run(() => Executar(v, caminho, quantas, cancelar), CancellationToken.None));
    }

    private static ResultadoVarredura Executar(Varredura varredura, string caminhoDaRaiz, int tarefas, CancellationToken cancelar)
    {
        var relogio = Stopwatch.StartNew();
        var raiz = varredura.Raiz;
        var volume = Volumes.Ler(caminhoDaRaiz);

        // Hard link: o mesmo identificador de arquivo no volume soma uma vez só. Em caminho de
        // rede o identificador vem do servidor e pode repetir entre discos dele, então não conta.
        var vistos = new ConjuntoIds();
        Func<long, bool> primeiraVez = Alvo.EhRede(caminhoDaRaiz) ? _ => true : vistos.Acrescentar;
        var alvoERaizDoVolume = string.Equals(caminhoDaRaiz, Volumes.RaizDe(caminhoDaRaiz), StringComparison.OrdinalIgnoreCase);

        using var fila = new BlockingCollection<(NoPasta No, string Caminho)>(new ConcurrentStack<(NoPasta, string)>());
        var pendentes = 1;
        fila.Add((raiz, caminhoDaRaiz));

        var trabalhadores = new Task[tarefas];
        for (var i = 0; i < tarefas; i++)
        {
            trabalhadores[i] = Task.Run(
                () =>
                {
                    // As listas vivem uma por tarefa e sao limpas a cada pasta: menos lixo para o coletor.
                    var arquivos = new List<ArquivoInfo>();
                    var entradas = new List<EntradaPasta>();
                    try
                    {
                        foreach (var (no, caminho) in fila.GetConsumingEnumerable(cancelar))
                        {
                            LerPasta(varredura, no, caminho, alvoERaizDoVolume && no == raiz, primeiraVez, arquivos, entradas);
                            foreach (var sub in no.Subpastas)
                            {
                                if (sub.Estado == EstadoPasta.Pendente)
                                {
                                    Interlocked.Increment(ref pendentes);
                                    fila.Add((sub, Alvo.Juntar(caminho, sub.Nome)));
                                }
                            }

                            if (Interlocked.Decrement(ref pendentes) == 0)
                            {
                                fila.CompleteAdding();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                },
                CancellationToken.None);
        }

        Task.WaitAll(trabalhadores);
        var cancelada = Volatile.Read(ref pendentes) > 0;
        return new ResultadoVarredura
        {
            Raiz = raiz,
            Volume = volume,
            Duracao = relogio.Elapsed,
            Cancelada = cancelada,
            PastasNaoLidas = cancelada ? 0 : raiz.ContarNaoLidas(),
        };
    }

    private static void LerPasta(
        Varredura varredura,
        NoPasta no,
        string caminho,
        bool raizDoVolume,
        Func<long, bool> primeiraVez,
        List<ArquivoInfo> arquivos,
        List<EntradaPasta> entradas)
    {
        varredura.PastaAtual = caminho;
        arquivos.Clear();
        entradas.Clear();
        try
        {
            switch (LeitorPasta.Ler(caminho, raizDoVolume, primeiraVez, arquivos, entradas, out var motivo))
            {
                case ResultadoLeitura.SemAcesso:
                    no.MarcarSemAcesso(motivo!);
                    return;
                case ResultadoLeitura.Erro:
                    no.MarcarErro(motivo!);
                    return;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            no.MarcarErro(e.Message);
            return;
        }

        var subpastas = new NoPasta[entradas.Count];
        for (var i = 0; i < entradas.Count; i++)
        {
            var entrada = entradas[i];
            var sub = new NoPasta(entrada.Nome, no, entrada.Modificacao);
            if (entrada.EhLink)
            {
                sub.MarcarLink(DestinoDoLink(Alvo.Juntar(caminho, entrada.Nome)));
            }

            subpastas[i] = sub;
        }

        no.Preencher(arquivos.ToArray(), subpastas);
    }

    private static string? DestinoDoLink(string caminho)
    {
        try
        {
            return new DirectoryInfo(Alvo.Longo(caminho)).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed class MotorDeNada : IMotorVarredura
    {
        public Varredura Iniciar(string alvo, CancellationToken cancelar) => throw new NotSupportedException();
    }
}
