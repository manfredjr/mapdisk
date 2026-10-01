using System.ComponentModel;

namespace MapDisk.Nucleo;

public sealed record LinhaDuplicado(int Grupo, int ArquivosNoGrupo, ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, bool Alternada);

/// <summary>Estado da aba Duplicados, sem WPF. A busca só roda pelo botão (regra 7).</summary>
public sealed class PainelDuplicados : INotifyPropertyChanged
{
    private const long Mb = 1024 * 1024;
    private readonly ILeitorConteudo _leitor;
    private CancellationTokenSource? _cancelar;
    private List<GrupoDuplicados> _grupos = [];
    private string _progresso = string.Empty;

    public PainelDuplicados(ILeitorConteudo leitor) => _leitor = leitor;

    public event PropertyChangedEventHandler? PropertyChanged;

    public long TamanhoMinimoMb { get; set; } = 1;

    public bool Procurando { get; private set; }

    public ResultadoDuplicados? Resultado { get; private set; }

    public IReadOnlyList<LinhaDuplicado> Linhas => _grupos.SelectMany(g => g.Arquivos.Select(a => new LinhaDuplicado(
        g.Numero, g.Arquivos.Count, a, a.Arquivo.Nome, a.Pasta.CaminhoCompleto(), Formatador.Tamanho(g.Tamanho),
        Formatador.Data(a.Arquivo.Modificacao), g.Numero % 2 == 0))).ToList();

    public string TextoEstado
    {
        get
        {
            if (Procurando)
            {
                return _progresso;
            }

            if (Resultado is not { } r)
            {
                return "Clique em Procurar duplicados. A busca lê o conteúdo só dos arquivos de mesmo tamanho.";
            }

            if (r.Cancelado)
            {
                return "Busca cancelada. Nada foi mostrado.";
            }

            var texto = _grupos.Count == 0
                ? "Nenhum arquivo repetido acima do tamanho mínimo."
                : $"{Formatador.Plural(_grupos.Count, "grupo", "grupos")} com {Formatador.Tamanho(_grupos.Sum(g => g.Repetido))} em cópias";
            return r.NaoLidos > 0
                ? $"{texto} {Formatador.Plural(r.NaoLidos, "arquivo não pôde ser lido e ficou", "arquivos não puderam ser lidos e ficaram")} fora."
                : texto;
        }
    }

    public async Task ProcurarAsync(NoPasta pasta)
    {
        Cancelar();
        var cancelar = _cancelar = new CancellationTokenSource();
        Procurando = true;
        _progresso = "Separando os arquivos de mesmo tamanho...";
        Avisar();
        var progresso = new Progress<ProgressoDuplicados>(p =>
        {
            if (_cancelar == cancelar && Procurando)
            {
                _progresso = $"{p.Etapa}: {p.Feitos} de {p.Total}";
                Avisar();
            }
        });
        var opcoes = OpcoesDuplicados.Para(pasta, Math.Max(1, TamanhoMinimoMb * Mb));
        var r = await Task.Run(() => Duplicados.ProcurarAsync(pasta, opcoes, _leitor, progresso, cancelar.Token));
        if (_cancelar != cancelar)
        {
            return;
        }

        Resultado = r;
        _grupos = r.Grupos.ToList();
        Procurando = false;
        Avisar();
    }

    public void Cancelar() => _cancelar?.Cancel();

    public void Limpar()
    {
        Cancelar();
        _cancelar = null;
        Resultado = null;
        _grupos = [];
        Procurando = false;
        Avisar();
    }

    /// <summary>Em cada grupo, todos menos o que fica (o mais antigo).</summary>
    public IReadOnlyList<LinhaDuplicado> Copias()
    {
        var manter = _grupos.Select(g => g.Mantido).ToHashSet();
        return Linhas.Where(l => !manter.Contains(l.Encontrado)).ToList();
    }

    /// <summary>Tira o que a ação removeu. Grupo com menos de dois arquivos sai.</summary>
    public void Tirar(IEnumerable<ItemAcao> itens)
    {
        var caminhos = itens.Select(i => i.Caminho).ToList();
        _grupos = _grupos
            .Select(g => g with { Arquivos = g.Arquivos.Where(a => !caminhos.Any(c => Protecao.Dentro(a.Caminho, c))).ToList() })
            .Where(g => g.Arquivos.Count > 1)
            .ToList();
        Avisar();
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
