using System.Collections.ObjectModel;

namespace MapDisk.Nucleo;

/// <summary>
/// A árvore como lista de linhas visíveis, para uma tabela com rolagem virtual. Guarda quais
/// pastas e grupos estão abertos e remonta a lista a cada mudança, reaproveitando as mesmas
/// linhas, para a seleção da tela não se perder.
/// </summary>
public sealed class ArvoreVisivel
{
    private readonly HashSet<NoPasta> _pastasAbertas = [];
    private readonly HashSet<NoPasta> _gruposAbertos = [];
    private readonly Dictionary<NoPasta, LinhaArvore> _linhasPasta = [];
    private readonly Dictionary<NoPasta, LinhaArvore> _linhasGrupo = [];
    private readonly Dictionary<(NoPasta, int), LinhaArvore> _linhasArquivo = [];

    public ObservableCollection<LinhaArvore> Linhas { get; } = [];

    public NoPasta? Raiz { get; private set; }

    public ModoExibicao Modo { get; private set; }

    public UnidadeExibicao Unidade { get; private set; }

    public ColunaOrdem Ordem { get; private set; } = ColunaOrdem.Valor;

    public bool Decrescente { get; private set; } = true;

    public void Carregar(NoPasta raiz)
    {
        Limpar();
        Raiz = raiz;
        _pastasAbertas.Add(raiz);
        Atualizar();
    }

    public void Limpar()
    {
        Raiz = null;
        _pastasAbertas.Clear();
        _gruposAbertos.Clear();
        _linhasPasta.Clear();
        _linhasGrupo.Clear();
        _linhasArquivo.Clear();
        Linhas.Clear();
    }

    public void Expandir(LinhaArvore linha)
    {
        if (!linha.PodeExpandir)
        {
            return;
        }

        (linha.Tipo == TipoLinha.Pasta ? _pastasAbertas : _gruposAbertos).Add(linha.Pasta);
        Atualizar();
    }

    public void Recolher(LinhaArvore linha)
    {
        if (linha.Tipo == TipoLinha.Arquivo)
        {
            return;
        }

        (linha.Tipo == TipoLinha.Pasta ? _pastasAbertas : _gruposAbertos).Remove(linha.Pasta);
        Atualizar();
    }

    public void Alternar(LinhaArvore linha)
    {
        if (linha.Expandida)
        {
            Recolher(linha);
        }
        else
        {
            Expandir(linha);
        }
    }

    public void DefinirModo(ModoExibicao modo)
    {
        Modo = modo;
        Atualizar();
    }

    public void DefinirUnidade(UnidadeExibicao unidade)
    {
        Unidade = unidade;
        Atualizar();
    }

    /// <summary>Clicar na mesma coluna inverte a ordem. Coluna nova: nome em ordem crescente, números do maior para o menor.</summary>
    public void Ordenar(ColunaOrdem coluna)
    {
        if (coluna == Ordem)
        {
            Decrescente = !Decrescente;
        }
        else
        {
            Ordem = coluna;
            Decrescente = coluna != ColunaOrdem.Nome;
        }

        Atualizar();
    }

    /// <summary>Remonta a lista com os números de agora. A tela chama a cada 250 ms durante a varredura.</summary>
    public void Atualizar()
    {
        if (Raiz is null)
        {
            return;
        }

        var alvo = new List<LinhaArvore>();
        var raiz = LinhaDaPasta(Raiz, 0);
        Acrescentar(raiz, raiz.Valor(Modo), alvo);
        Sincronizar(alvo);
    }

    private void Acrescentar(LinhaArvore linha, long valorDoPai, List<LinhaArvore> alvo)
    {
        linha.Expandida = linha.PodeExpandir && linha.Tipo switch
        {
            TipoLinha.Pasta => _pastasAbertas.Contains(linha.Pasta),
            TipoLinha.GrupoArquivos => _gruposAbertos.Contains(linha.Pasta),
            _ => false,
        };
        linha.Atualizar(Modo, Unidade, valorDoPai);
        alvo.Add(linha);
        if (!linha.Expandida)
        {
            return;
        }

        // Os arquivos de um grupo são comparados com a pasta onde estão, não com o grupo.
        var valorDosFilhos = linha.Tipo == TipoLinha.GrupoArquivos ? valorDoPai : linha.Valor(Modo);
        foreach (var filho in Ordenados(Filhos(linha)))
        {
            Acrescentar(filho, valorDosFilhos, alvo);
        }
    }

    private IEnumerable<LinhaArvore> Filhos(LinhaArvore linha)
    {
        var nivel = linha.Nivel + 1;
        if (linha.Tipo == TipoLinha.GrupoArquivos)
        {
            var arquivos = linha.Pasta.Arquivos;
            for (var i = 0; i < arquivos.Count; i++)
            {
                yield return LinhaDoArquivo(linha.Pasta, i, nivel);
            }

            yield break;
        }

        foreach (var sub in linha.Pasta.Subpastas)
        {
            yield return LinhaDaPasta(sub, nivel);
        }

        if (linha.Pasta.Arquivos.Count > 0)
        {
            yield return LinhaDoGrupo(linha.Pasta, nivel);
        }
    }

    // As chaves são lidas uma vez antes de ordenar: durante a varredura os números mudam a todo instante.
    private List<LinhaArvore> Ordenados(IEnumerable<LinhaArvore> linhas)
    {
        var comChave = linhas.Select(l => (Linha: l, Numero: Chave(l), l.Nome)).ToList();
        var nome = StringComparer.CurrentCultureIgnoreCase;
        var ordem = Ordem == ColunaOrdem.Nome
            ? (Decrescente ? comChave.OrderByDescending(c => c.Nome, nome) : comChave.OrderBy(c => c.Nome, nome))
            : (Decrescente ? comChave.OrderByDescending(c => c.Numero) : comChave.OrderBy(c => c.Numero)).ThenBy(c => c.Nome, nome);
        return ordem.Select(c => c.Linha).ToList();
    }

    private long Chave(LinhaArvore linha) => Ordem switch
    {
        ColunaOrdem.Tamanho => linha.Tamanho,
        ColunaOrdem.Alocado => linha.Alocado,
        ColunaOrdem.Arquivos => linha.Arquivos,
        ColunaOrdem.Pastas => linha.Pastas,
        ColunaOrdem.Modificacao => linha.Modificacao.Ticks,
        ColunaOrdem.Nome => 0,
        _ => linha.SemValor ? -1 : linha.Valor(Modo),
    };

    private LinhaArvore LinhaDaPasta(NoPasta no, int nivel) =>
        _linhasPasta.TryGetValue(no, out var linha) ? linha : _linhasPasta[no] = new LinhaArvore(TipoLinha.Pasta, no, nivel);

    private LinhaArvore LinhaDoGrupo(NoPasta no, int nivel) =>
        _linhasGrupo.TryGetValue(no, out var linha) ? linha : _linhasGrupo[no] = new LinhaArvore(TipoLinha.GrupoArquivos, no, nivel);

    private LinhaArvore LinhaDoArquivo(NoPasta no, int indice, int nivel) =>
        _linhasArquivo.TryGetValue((no, indice), out var linha)
            ? linha
            : _linhasArquivo[(no, indice)] = new LinhaArvore(TipoLinha.Arquivo, no, nivel, no.Arquivos[indice]);

    // Leva a lista da tela à lista nova com o mínimo de mudanças, para a rolagem não pular.
    private void Sincronizar(List<LinhaArvore> alvo)
    {
        for (var i = 0; i < alvo.Count; i++)
        {
            if (i < Linhas.Count && ReferenceEquals(Linhas[i], alvo[i]))
            {
                continue;
            }

            var atual = Linhas.IndexOf(alvo[i]);
            if (atual >= 0)
            {
                Linhas.Move(atual, i);
            }
            else
            {
                Linhas.Insert(i, alvo[i]);
            }
        }

        while (Linhas.Count > alvo.Count)
        {
            Linhas.RemoveAt(Linhas.Count - 1);
        }
    }
}
