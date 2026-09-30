using System.ComponentModel;
using System.Text;

namespace MapDisk.Nucleo;

public sealed record LinhaRelatorio(int Numero, string Nome, string Tipo, string Caminho, string TextoTamanho, string TextoArquivos, string TextoData, string Motivo);

/// <summary>Estado da janela "Relatório para o cliente", sem WPF.</summary>
public sealed class PainelRelatorio : INotifyPropertyChanged
{
    private readonly NoPasta _pasta;
    private readonly long? _livre;
    private readonly ListaAvaliacao _lista;

    public PainelRelatorio(NoPasta pasta, long? livre, string tecnico, LocaisProtegidos? locais = null)
    {
        _lista = new ListaAvaliacao(locais);
        _pasta = pasta;
        _livre = livre;
        Tecnico = tecnico;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CriteriosSugestao Criterios { get; set; } = new();

    public string Cliente { get; set; } = string.Empty;

    public string Tecnico { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public string Titulo => $"Relatório para o cliente: {_pasta.CaminhoCompleto()}";

    public IReadOnlyList<LinhaRelatorio> Linhas => _lista.Itens.Select(i => new LinhaRelatorio(
        i.Numero, i.Item.Nome, i.Tipo, i.Item.Caminho, Formatador.Tamanho(i.Item.Tamanho),
        i.Item.EhPasta ? Formatador.Numero(i.Arquivos) : string.Empty, Formatador.Data(i.Item.Modificacao), i.Motivo)).ToList();

    public string TextoTotal => $"{Formatador.Plural(_lista.Itens.Count, "item", "itens")}, {Formatador.Tamanho(_lista.Total)}";

    public string? MotivoParaNaoGerar => _lista.Itens.Count == 0
        ? "Monte a lista com Sugerir ou Acrescentar a seleção."
        : string.IsNullOrWhiteSpace(Cliente) ? "Escreva o nome do cliente." : null;

    public void Sugerir(DateTime hoje)
    {
        Sugestao.Sugerir(_lista, _pasta, Criterios, hoje);
        Avisar();
    }

    public void AcrescentarSelecao(IEnumerable<object> selecionados)
    {
        foreach (var item in PreparadorAcoes.ItensDaSelecao(selecionados).Itens)
        {
            _lista.Acrescentar(item, Sugestao.EscolhidoPeloTecnico);
        }

        Avisar();
    }

    public void Tirar(IEnumerable<int> numeros)
    {
        _lista.Tirar(numeros);
        Avisar();
    }

    public Avaliacao Montar(DateTime agora) =>
        new(Avaliacao.NumeroDe(agora), Cliente.Trim(), Tecnico.Trim(), Mensagem.Trim(), _pasta, agora, _livre, _lista.Itens);

    public string NomeSugerido(DateTime agora) => Montar(agora).NomeDoArquivo;

    /// <summary>Grava a página no caminho escolhido e a planilha ao lado, com o mesmo nome.</summary>
    public (string Pagina, string Planilha) Gerar(string arquivoPagina, DateTime agora)
    {
        var avaliacao = Montar(agora);
        var planilha = Path.ChangeExtension(arquivoPagina, ".xlsx");
        File.WriteAllText(arquivoPagina, PaginaAvaliacao.Gerar(avaliacao), new UTF8Encoding(false));
        PlanilhaAvaliacao.Gravar(avaliacao, planilha);
        return (arquivoPagina, planilha);
    }

    public void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

public sealed record LinhaResposta(int Numero, string Nome, string Caminho, string TextoTamanho, string TextoDecisao, string Destino, string Observacao, string Situacao, ItemCasado Casado);

/// <summary>Estado da janela "Resposta do cliente", sem WPF. Só seleciona: agir passa pela confirmação.</summary>
public sealed class PainelResposta : INotifyPropertyChanged
{
    private readonly NoPasta _raiz;
    private RespostaAvaliacao? _resposta;
    private IReadOnlyList<ItemCasado> _casados = [];

    public PainelResposta(NoPasta raiz) => _raiz = raiz;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Cabecalho => _resposta is not { } r
        ? "Escolha a planilha ou o arquivo de resposta."
        : $"Resposta do relatório {r.Relatorio}, decidida por {(r.DecididoPor.Length > 0 ? r.DecididoPor : "(sem nome)")} em {(r.Data.Length > 0 ? r.Data : "(sem data)")}";

    public string? Aviso => _resposta is not null && _casados.Count > 0 && _casados.All(c => c.Item is null)
        ? $"Nenhum item foi encontrado na varredura atual. Varra a pasta do relatório antes: {_resposta.Pasta}"
        : null;

    public IReadOnlyList<LinhaResposta> Linhas => _casados.Select(c => new LinhaResposta(
        c.Decisao.Numero,
        Path.GetFileName(c.Decisao.Caminho.TrimEnd('\\')),
        c.Decisao.Caminho,
        Formatador.Tamanho(c.Item?.Tamanho ?? c.Decisao.Bytes),
        c.Decisao.Decisao == Decisao.SemDecisao ? "sem marca" : c.Decisao.Decisao.ToString(),
        c.Decisao.Destino,
        c.Decisao.Observacao,
        c.Problema ?? "ok",
        c)).ToList();

    public string Resumo => string.Join(" | ",
        Grupo("Apagar", d => d == Decisao.Apagar, comTamanho: true),
        Grupo("Mover", d => d == Decisao.Mover, comTamanho: true),
        Grupo("Conversar", d => d == Decisao.Conversar, comTamanho: false),
        Grupo("Manter ou sem marca", d => d is Decisao.Manter or Decisao.SemDecisao, comTamanho: false));

    public void Ler(string arquivo)
    {
        _resposta = RespostaAvaliacao.Ler(arquivo);
        _casados = RespostaAvaliacao.Casar(_resposta, _raiz);
        Avisar();
    }

    public void MarcarPorNumeros(string texto, Decisao decisao)
    {
        if (_resposta is null)
        {
            return;
        }

        _resposta = _resposta.ComDecisao(RespostaAvaliacao.Numeros(texto), decisao);
        _casados = RespostaAvaliacao.Casar(_resposta, _raiz);
        Avisar();
    }

    public IReadOnlyList<int> NumerosDe(Decisao decisao) =>
        _casados.Where(c => c.Decisao.Decisao == decisao).Select(c => c.Decisao.Numero).ToList();

    /// <summary>Só os itens achados e iguais ao relatório. O resto fica fora das ações (regra 1).</summary>
    public IReadOnlyList<ItemAcao> ItensDe(IEnumerable<LinhaResposta> linhas) =>
        linhas.Where(l => l.Casado.Problema is null && l.Casado.Item is not null).Select(l => l.Casado.Item!).ToList();

    private string Grupo(string nome, Func<Decisao, bool> filtro, bool comTamanho)
    {
        var itens = _casados.Where(c => filtro(c.Decisao.Decisao)).ToList();
        var texto = $"{nome}: {Formatador.Plural(itens.Count, "item", "itens")}";
        return comTamanho && itens.Count > 0 ? $"{texto} ({Formatador.Tamanho(itens.Sum(c => c.Item?.Tamanho ?? c.Decisao.Bytes))})" : texto;
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
