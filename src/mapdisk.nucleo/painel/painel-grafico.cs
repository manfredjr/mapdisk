using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum TipoGrafico
{
    Blocos,
    Pizza,
}

public sealed record BlocoGrafico(
    double X, double Y, double Largura, double Altura, string Cor, string CorTexto,
    string Nome, string TextoValor, bool MostrarRotulo, string Dica, NoPasta? Pasta);

public sealed record FatiaGrafico(
    string Caminho, string Cor, string Nome, string TextoValor, string TextoPorcentagem, string Dica, NoPasta? Pasta);

/// <summary>Estado da aba Gráfico, sem tipos do WPF. A janela informa o tamanho da área e desenha as listas.</summary>
public sealed class PainelGrafico : INotifyPropertyChanged
{
    public const int MaximoBlocos = 200;

    public const int MaximoFatias = 10;

    public const double RaioPizza = 110;

    private const double LarguraMinimaRotulo = 70;

    private const double AlturaMinimaRotulo = 34;

    private NoPasta? _pasta;
    private double _largura;
    private double _altura;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TipoGrafico Tipo { get; private set; }

    public ModoExibicao Modo { get; private set; }

    public IReadOnlyList<BlocoGrafico> Blocos { get; private set; } = [];

    public IReadOnlyList<FatiaGrafico> Fatias { get; private set; } = [];

    public string TextoNaoLidas { get; private set; } = string.Empty;

    public string TextoExcluidas { get; private set; } = string.Empty;

    public bool MostrarBlocos => Tipo == TipoGrafico.Blocos;

    public bool MostrarPizza => Tipo == TipoGrafico.Pizza;

    public double DiametroPizza => RaioPizza * 2;

    public void Carregar(NoPasta? pasta)
    {
        _pasta = pasta;
        Montar();
    }

    public void Limpar() => Carregar(null);

    public void DefinirTipo(TipoGrafico tipo)
    {
        Tipo = tipo;
        Montar();
    }

    public void DefinirModo(ModoExibicao modo)
    {
        Modo = modo;
        Montar();
    }

    public void Redimensionar(double largura, double altura)
    {
        _largura = largura;
        _altura = altura;
        if (Tipo == TipoGrafico.Blocos)
        {
            Montar();
        }
    }

    private void Montar()
    {
        Blocos = [];
        Fatias = [];
        TextoNaoLidas = TextoExcluidas = string.Empty;
        if (_pasta is { } pasta)
        {
            var c = ItensGrafico.DaPasta(pasta, Modo, Tipo == TipoGrafico.Blocos ? MaximoBlocos : MaximoFatias);
            TextoNaoLidas = c.NaoLidas.Count == 0 ? string.Empty : $"Sem leitura, fora do gráfico: {string.Join(", ", c.NaoLidas)}";
            TextoExcluidas = c.Excluidas.Count == 0 ? string.Empty : $"Excluídas da varredura, fora do gráfico: {string.Join(", ", c.Excluidas)}";
            var cores = Paleta.CoresDe(c.Itens);
            if (Tipo == TipoGrafico.Blocos)
            {
                var r = Treemap.Dispor(c.Itens.Select(i => i.Valor).ToList(), _largura, _altura);
                Blocos = r.Select((b, n) =>
                {
                    var item = c.Itens[n];
                    var rotulo = b.Largura >= LarguraMinimaRotulo && b.Altura >= AlturaMinimaRotulo;
                    return new BlocoGrafico(b.X, b.Y, b.Largura, b.Altura, cores[n], Paleta.CorTexto(cores[n]),
                        item.Nome, TextoValor(item.Valor), rotulo, Dica(item, c.Total), item.Pasta);
                }).ToList();
            }
            else
            {
                var f = Pizza.Fatias(c.Itens.Select(i => i.Valor).ToList(), RaioPizza);
                Fatias = f.Select((fatia, n) =>
                {
                    var item = c.Itens[n];
                    return new FatiaGrafico(fatia.Caminho, cores[n], item.Nome, TextoValor(item.Valor),
                        Formatador.Porcentagem((double)item.Valor / c.Total), Dica(item, c.Total), item.Pasta);
                }).ToList();
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private string TextoValor(long valor) => Modo == ModoExibicao.Contagem
        ? Formatador.Plural(valor, "arquivo", "arquivos")
        : Formatador.Tamanho(valor);

    private string Dica(ItemGrafico item, long total) =>
        $"{item.Nome}\n{TextoValor(item.Valor)} ({Formatador.Porcentagem((double)item.Valor / total)})";
}
