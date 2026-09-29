namespace MapDisk.Nucleo;

public enum EstadoPasta
{
    /// <summary>Ainda não lida. Durante a varredura, e depois dela se foi interrompida.</summary>
    Pendente,
    Lida,
    SemAcesso,
    ErroLeitura,

    /// <summary>Junção ou link simbólico. Não é seguido e não soma.</summary>
    Link,
}

[Flags]
public enum MarcaArquivo
{
    Nenhuma = 0,
    Sistema = 1,
    NaNuvem = 2,
    Link = 4,

    /// <summary>Hard link cujo conteúdo já foi contado em outro lugar do volume.</summary>
    LinkRepetido = 8,
}

/// <summary>Um arquivo lido na varredura. Guarda só o que a tela e as análises usam.</summary>
public readonly record struct ArquivoInfo(string Nome, long Tamanho, long Alocado, DateTime Modificacao, MarcaArquivo Marcas)
{
    /// <summary>Entra nas somas. O hard link repetido aparece na lista, mas não soma de novo.</summary>
    public bool Soma => (Marcas & MarcaArquivo.LinkRepetido) == 0;
}

/// <summary>
/// Uma pasta da varredura. Os totais incluem tudo que está abaixo dela e são atualizados
/// enquanto a varredura anda: quando uma pasta é lida, o que ela tem é somado nela e em todas
/// as pastas acima. Assim a tela mostra números parciais sem esperar o fim.
/// </summary>
public sealed class NoPasta
{
    private static readonly NoPasta[] _semSubpastas = [];
    private static readonly ArquivoInfo[] _semArquivos = [];

    private NoPasta[] _subpastas = _semSubpastas;
    private ArquivoInfo[] _arquivos = _semArquivos;
    private volatile EstadoPasta _estado;
    private long _tamanho;
    private long _alocado;
    private long _arquivosTotal;
    private long _pastasTotal;
    private long _semAcesso;
    private long _comErro;
    private long _modificacao;

    public NoPasta(string nome, NoPasta? pai, DateTime modificacao = default)
    {
        Nome = nome;
        Pai = pai;
        ModificacaoPropria = modificacao;
    }

    /// <summary>Nome da pasta. Na raiz, o caminho completo do alvo.</summary>
    public string Nome { get; }

    public NoPasta? Pai { get; }

    public DateTime ModificacaoPropria { get; }

    public EstadoPasta Estado => _estado;

    public string? Motivo { get; private set; }

    public string? DestinoLink { get; private set; }

    public IReadOnlyList<NoPasta> Subpastas => Volatile.Read(ref _subpastas);

    public IReadOnlyList<ArquivoInfo> Arquivos => Volatile.Read(ref _arquivos);

    /// <summary>Soma dos arquivos que estão direto nesta pasta.</summary>
    public long TamanhoProprio { get; private set; }

    public long AlocadoProprio { get; private set; }

    public long Tamanho => Interlocked.Read(ref _tamanho);

    public long Alocado => Interlocked.Read(ref _alocado);

    public long ArquivosTotal => Interlocked.Read(ref _arquivosTotal);

    /// <summary>Pastas abaixo desta, em todos os níveis.</summary>
    public long PastasTotal => Interlocked.Read(ref _pastasTotal);

    /// <summary>Pastas sem permissão de leitura nesta subárvore, contando esta.</summary>
    public long PastasSemAcesso => Interlocked.Read(ref _semAcesso);

    public long PastasComErro => Interlocked.Read(ref _comErro);

    public DateTime UltimaModificacao => new(Interlocked.Read(ref _modificacao));

    public int Nivel
    {
        get
        {
            var nivel = 0;
            for (var p = Pai; p != null; p = p.Pai)
            {
                nivel++;
            }

            return nivel;
        }
    }

    public string CaminhoCompleto()
    {
        var partes = new Stack<string>();
        var no = this;
        while (no.Pai != null)
        {
            partes.Push(no.Nome);
            no = no.Pai;
        }

        var caminho = no.Nome;
        foreach (var parte in partes)
        {
            caminho = Alvo.Juntar(caminho, parte);
        }

        return caminho;
    }

    /// <summary>Pastas desta subárvore que ficaram sem leitura, contando esta.</summary>
    public int ContarNaoLidas()
    {
        var quantas = 0;
        var pilha = new Stack<NoPasta>();
        pilha.Push(this);
        while (pilha.Count > 0)
        {
            var no = pilha.Pop();
            if (no.Estado == EstadoPasta.Pendente)
            {
                quantas++;
                continue;
            }

            foreach (var sub in no.Subpastas)
            {
                pilha.Push(sub);
            }
        }

        return quantas;
    }

    /// <summary>Grava o que foi lido e soma aqui e acima. Chamado uma vez, pela tarefa que leu a pasta.</summary>
    public void Preencher(ArquivoInfo[] arquivos, NoPasta[] subpastas)
    {
        long tamanho = 0;
        long alocado = 0;
        var recente = ModificacaoPropria.Ticks;
        foreach (var a in arquivos)
        {
            if (a.Soma)
            {
                tamanho += a.Tamanho;
                alocado += a.Alocado;
            }

            recente = Math.Max(recente, a.Modificacao.Ticks);
        }

        TamanhoProprio = tamanho;
        AlocadoProprio = alocado;
        Volatile.Write(ref _arquivos, arquivos);
        Volatile.Write(ref _subpastas, subpastas);
        _estado = EstadoPasta.Lida;
        Somar(tamanho, alocado, arquivos.Length, subpastas.Length, 0, 0, recente);
    }

    public void MarcarSemAcesso(string motivo)
    {
        Motivo = motivo;
        _estado = EstadoPasta.SemAcesso;
        Somar(0, 0, 0, 0, 1, 0, ModificacaoPropria.Ticks);
    }

    public void MarcarErro(string motivo)
    {
        Motivo = motivo;
        _estado = EstadoPasta.ErroLeitura;
        Somar(0, 0, 0, 0, 0, 1, ModificacaoPropria.Ticks);
    }

    public void MarcarLink(string? destino)
    {
        DestinoLink = destino;
        _estado = EstadoPasta.Link;
        Somar(0, 0, 0, 0, 0, 0, ModificacaoPropria.Ticks);
    }

    private void Somar(long tamanho, long alocado, long arquivos, long pastas, long semAcesso, long comErro, long modificacao)
    {
        for (var no = this; no != null; no = no.Pai)
        {
            Interlocked.Add(ref no._tamanho, tamanho);
            Interlocked.Add(ref no._alocado, alocado);
            Interlocked.Add(ref no._arquivosTotal, arquivos);
            Interlocked.Add(ref no._pastasTotal, pastas);
            Interlocked.Add(ref no._semAcesso, semAcesso);
            Interlocked.Add(ref no._comErro, comErro);

            long atual;
            while (modificacao > (atual = Interlocked.Read(ref no._modificacao))
                && Interlocked.CompareExchange(ref no._modificacao, modificacao, atual) != atual)
            {
            }
        }
    }
}
