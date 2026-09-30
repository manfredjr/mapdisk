using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum AbaAnalise
{
    Grafico,
    MaioresArquivos,
    ArquivosAntigos,
    PorTipo,
    PorUsuario,
}

public sealed record LinhaArquivo(ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, string Caminho);

public sealed record LinhaResumo(string Nome, string TextoQuantidade, string TextoTamanho, string TextoPorcentagem, double LarguraBarra, NoPasta? Pasta);

/// <summary>
/// Estado do painel de análises, sem tipos do WPF. Calcula fora da tela e troca as listas de
/// uma vez, para a janela não travar numa pasta com milhões de arquivos.
/// </summary>
public sealed class PainelAnalises : INotifyPropertyChanged
{
    private const double LarguraMaximaBarra = 80;

    private NoPasta? _pasta;

    public PainelAnalises(ILeitorConteudo? leitor = null) => Duplicados = new PainelDuplicados(leitor ?? new LeitorConteudo());

    /// <summary>A aba Duplicados. O resultado diz de qual pasta é e só some numa nova varredura.</summary>
    public PainelDuplicados Duplicados { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AbaAnalise Aba { get; private set; }

    /// <summary>A aba Gráfico: blocos e pizza da mesma pasta das outras abas.</summary>
    public PainelGrafico Grafico { get; } = new();

    public Idade Idade { get; private set; } = Idade.UmAno;

    public string Titulo { get; private set; } = string.Empty;

    /// <summary>Aviso do topo do painel: aguarde, ou pastas sem leitura fora da conta.</summary>
    public string Aviso { get; private set; } = "Varra uma unidade ou pasta para ver as análises.";

    public IReadOnlyList<LinhaArquivo> Maiores { get; private set; } = [];

    public IReadOnlyList<LinhaArquivo> Antigos { get; private set; } = [];

    public string TextoAntigos { get; private set; } = string.Empty;

    public IReadOnlyList<LinhaResumo> Categorias { get; private set; } = [];

    public IReadOnlyList<LinhaResumo> Extensoes { get; private set; } = [];

    public IReadOnlyList<LinhaResumo> Usuarios { get; private set; } = [];

    public string TextoUsuarios { get; private set; } = string.Empty;

    public void DefinirAba(AbaAnalise aba)
    {
        Aba = aba;
        Avisar();
    }

    public void Aguardar()
    {
        _pasta = null;
        Aviso = "Aguarde o fim da varredura.";
        Maiores = Antigos = [];
        Categorias = Extensoes = Usuarios = [];
        TextoAntigos = TextoUsuarios = string.Empty;
        Grafico.Limpar();
        Duplicados.Limpar();
        Avisar();
    }

    public async Task CalcularAsync(NoPasta pasta, DateTime hoje)
    {
        _pasta = pasta;
        var idade = Idade;
        var r = await Task.Run(() => (
            Maiores: Analises.MaioresArquivos(pasta),
            Antigos: Analises.ArquivosAntigos(pasta, idade, hoje),
            Categorias: Analises.PorCategoria(pasta),
            Extensoes: Analises.PorExtensao(pasta),
            Usuarios: Analises.PorUsuario(pasta)));
        if (_pasta != pasta)
        {
            return;
        }

        Grafico.Carregar(pasta);
        Titulo = $"Análise de {pasta.CaminhoCompleto()}";
        var semLeitura = pasta.PastasSemAcesso + pasta.PastasComErro;
        Aviso = semLeitura > 0
            ? $"{Formatador.Plural(semLeitura, "pasta sem leitura não entra", "pastas sem leitura não entram")} nesta conta"
            : string.Empty;
        Maiores = r.Maiores.Select(Linha).ToList();
        AplicarAntigos(r.Antigos, idade);
        var total = pasta.Tamanho;
        Categorias = r.Categorias.Select(c => Resumo(c.Nome, c.Quantidade, c.Tamanho, total, null)).ToList();
        Extensoes = r.Extensoes.Select(c => Resumo(c.Nome, c.Quantidade, c.Tamanho, total, null)).ToList();
        AplicarUsuarios(r.Usuarios);
        Avisar();
    }

    public async Task DefinirIdadeAsync(Idade idade, DateTime hoje)
    {
        Idade = idade;
        if (_pasta is { } pasta)
        {
            var antigos = await Task.Run(() => Analises.ArquivosAntigos(pasta, idade, hoje));
            if (_pasta == pasta && Idade == idade)
            {
                AplicarAntigos(antigos, idade);
            }
        }

        Avisar();
    }

    private void AplicarAntigos(ResumoAntigos antigos, Idade idade)
    {
        Antigos = antigos.Maiores.Select(Linha).ToList();
        var ha = idade switch
        {
            Idade.SeisMeses => "6 meses",
            Idade.UmAno => "1 ano",
            Idade.DoisAnos => "2 anos",
            _ => "5 anos",
        };
        TextoAntigos = $"{Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos")} sem alteração há mais de {ha}, somando {Formatador.Tamanho(antigos.Tamanho)}";
    }

    private void AplicarUsuarios(ResumoUsuarios usuarios)
    {
        if (usuarios.PastaDePerfis is not { } perfis)
        {
            Usuarios = [];
            TextoUsuarios = @"Esta pasta não tem a pasta de perfis (Users). Varra a unidade inteira, como C:\, para ver o ranking por usuário.";
            return;
        }

        TextoUsuarios = $"Perfis em {perfis.CaminhoCompleto()}";
        Usuarios = usuarios.Perfis
            .Select(p => p.Estado == EstadoPasta.Lida
                ? Resumo(p.Nome, p.ArquivosTotal, p.Tamanho, perfis.Tamanho, p)
                : new LinhaResumo(p.Nome, string.Empty, "sem acesso", string.Empty, 0, p))
            .ToList();
    }

    private static LinhaArquivo Linha(ArquivoEncontrado a) => new(
        a,
        a.Arquivo.Nome,
        a.Pasta.CaminhoCompleto(),
        Formatador.Tamanho(a.Arquivo.Tamanho),
        Formatador.Data(a.Arquivo.Modificacao),
        a.Caminho);

    private static LinhaResumo Resumo(string nome, long quantidade, long tamanho, long total, NoPasta? pasta)
    {
        var fracao = total > 0 ? Math.Clamp((double)tamanho / total, 0, 1) : 0;
        return new LinhaResumo(
            nome,
            Formatador.Plural(quantidade, "arquivo", "arquivos"),
            Formatador.Tamanho(tamanho),
            Formatador.Porcentagem(fracao),
            Math.Round(fracao * LarguraMaximaBarra, 1),
            pasta);
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
