using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum ModoExibicao
{
    Tamanho,
    Alocado,
    Contagem,
    Porcentagem,
}

public enum ColunaOrdem
{
    /// <summary>O valor do modo de exibição. É a ordem padrão, do maior para o menor.</summary>
    Valor,
    Nome,
    Tamanho,
    Alocado,
    Arquivos,
    Pastas,
    Modificacao,
}

public enum TipoLinha
{
    Pasta,

    /// <summary>A linha "[N arquivos]", que junta os arquivos soltos de uma pasta.</summary>
    GrupoArquivos,
    Arquivo,
}

/// <summary>Uma linha da árvore na tela. Os textos são calculados pela ArvoreVisivel.</summary>
public sealed class LinhaArvore : INotifyPropertyChanged
{
    private const double LarguraMaximaBarra = 60;

    internal LinhaArvore(TipoLinha tipo, NoPasta pasta, ArquivoInfo arquivo = default)
    {
        Tipo = tipo;
        Pasta = pasta;
        Arquivo = arquivo;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TipoLinha Tipo { get; }

    /// <summary>A pasta da linha. No grupo e no arquivo, a pasta onde eles estão.</summary>
    public NoPasta Pasta { get; }

    public ArquivoInfo Arquivo { get; }

    /// <summary>Nível abaixo da raiz mostrada. Muda quando outra pasta vira a raiz.</summary>
    public int Nivel { get; internal set; }

    public bool Expandida { get; internal set; }

    public bool PodeExpandir => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Subpastas.Count > 0 || Pasta.Arquivos.Count > 0,
        TipoLinha.GrupoArquivos => true,
        _ => false,
    };

    /// <summary>Sinal do botão de abrir e fechar: "+", "-" ou nada.</summary>
    public string Sinal => !PodeExpandir ? string.Empty : Expandida ? "-" : "+";

    public double Recuo => Nivel * 16;

    public string Nome => Tipo switch
    {
        TipoLinha.Pasta => Nivel == 0 ? Pasta.CaminhoCompleto() : Pasta.Nome,
        TipoLinha.GrupoArquivos => $"[{Formatador.Plural(Pasta.Arquivos.Count, "arquivo", "arquivos")}]",
        _ => Arquivo.Nome,
    };

    /// <summary>Caminho completo, para mostrar no Explorer e copiar.</summary>
    public string Caminho => Tipo == TipoLinha.Arquivo
        ? Alvo.Juntar(Pasta.CaminhoCompleto(), Arquivo.Nome)
        : Pasta.CaminhoCompleto();

    /// <summary>
    /// Pasta sem número de verdade: ainda não lida, sem acesso, com erro ou link. Nunca mostra 0.
    /// A pasta ainda não lida aparece durante a varredura e depois de uma varredura interrompida.
    /// </summary>
    public bool SemValor => Tipo == TipoLinha.Pasta
        && Pasta.Estado is EstadoPasta.Pendente or EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura or EstadoPasta.Link;

    public string Rotulo => Tipo switch
    {
        // Sem acesso e não lida ficam sem rótulo: a coluna do valor já diz isso.
        TipoLinha.Pasta => Pasta.Estado switch
        {
            EstadoPasta.ErroLeitura => $"erro de leitura: {Pasta.Motivo}",
            EstadoPasta.Link => Pasta.DestinoLink is { } destino ? $"link para {destino}" : "link",
            EstadoPasta.Lida when Pasta.PastasSemAcesso + Pasta.PastasComErro is var n and > 0
                => $"{Formatador.Plural(n, "pasta", "pastas")} sem leitura dentro",
            _ => string.Empty,
        },
        TipoLinha.Arquivo => RotuloDoArquivo(Arquivo.Marcas),
        _ => string.Empty,
    };

    public long Tamanho => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Tamanho,
        TipoLinha.GrupoArquivos => Pasta.TamanhoProprio,
        _ => Arquivo.Tamanho,
    };

    public long Alocado => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Alocado,
        TipoLinha.GrupoArquivos => Pasta.AlocadoProprio,
        _ => Arquivo.Alocado,
    };

    public long Arquivos => Tipo switch
    {
        TipoLinha.Pasta => Pasta.ArquivosTotal,
        TipoLinha.GrupoArquivos => Pasta.Arquivos.Count,
        _ => 1,
    };

    public long Pastas => Tipo == TipoLinha.Pasta ? Pasta.PastasTotal : 0;

    public DateTime Modificacao => Tipo switch
    {
        TipoLinha.Pasta => Pasta.UltimaModificacao,
        TipoLinha.GrupoArquivos => Pasta.Arquivos.Count == 0 ? DateTime.MinValue : Pasta.Arquivos.Max(a => a.Modificacao),
        _ => Arquivo.Modificacao,
    };

    /// <summary>Fração do valor da pasta-pai, de 0 a 1. Na raiz, 1.</summary>
    public double Fracao { get; private set; }

    public double LarguraBarra => Math.Round(Fracao * LarguraMaximaBarra, 1);

    public string TextoValor { get; private set; } = string.Empty;

    public string TextoTamanho { get; private set; } = string.Empty;

    public string TextoAlocado { get; private set; } = string.Empty;

    public string TextoArquivos { get; private set; } = string.Empty;

    public string TextoPastas { get; private set; } = string.Empty;

    public string TextoPorcentagem { get; private set; } = string.Empty;

    public string TextoModificacao { get; private set; } = string.Empty;

    public long Valor(ModoExibicao modo) => modo switch
    {
        ModoExibicao.Alocado => Alocado,
        ModoExibicao.Contagem => Arquivos,
        _ => Tamanho,
    };

    /// <summary>Recalcula os textos com os números de agora e avisa a tela.</summary>
    internal void Atualizar(ModoExibicao modo, UnidadeExibicao unidade, long valorDoPai)
    {
        if (SemValor)
        {
            var texto = Pasta.Estado switch
            {
                EstadoPasta.Pendente => "não lida",
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.Link => "link",
                _ => "erro",
            };
            Fracao = 0;
            TextoValor = TextoTamanho = TextoAlocado = texto;
            TextoArquivos = TextoPastas = TextoPorcentagem = string.Empty;
        }
        else
        {
            var valor = Valor(modo);
            Fracao = valorDoPai > 0 ? Math.Clamp((double)valor / valorDoPai, 0, 1) : (Nivel == 0 ? 1 : 0);
            TextoTamanho = Formatador.Tamanho(Tamanho, unidade);
            TextoAlocado = Formatador.Tamanho(Alocado, unidade);
            TextoArquivos = Tipo == TipoLinha.Arquivo ? string.Empty : Formatador.Numero(Arquivos);
            TextoPastas = Tipo == TipoLinha.Pasta ? Formatador.Numero(Pastas) : string.Empty;
            TextoPorcentagem = Formatador.Porcentagem(Fracao);
            TextoValor = modo switch
            {
                ModoExibicao.Alocado => TextoAlocado,
                ModoExibicao.Contagem => Formatador.Numero(Arquivos),
                ModoExibicao.Porcentagem => TextoPorcentagem,
                _ => TextoTamanho,
            };
        }

        TextoModificacao = Formatador.Data(Modificacao);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private static string RotuloDoArquivo(MarcaArquivo marcas)
    {
        var partes = new List<string>();
        if (marcas.HasFlag(MarcaArquivo.Sistema))
        {
            partes.Add("arquivo do sistema");
        }

        if (marcas.HasFlag(MarcaArquivo.NaNuvem))
        {
            partes.Add("na nuvem");
        }

        if (marcas.HasFlag(MarcaArquivo.LinkRepetido))
        {
            partes.Add("hard link, contado uma vez");
        }

        if (marcas.HasFlag(MarcaArquivo.Link))
        {
            partes.Add("link");
        }

        return string.Join(", ", partes);
    }
}
