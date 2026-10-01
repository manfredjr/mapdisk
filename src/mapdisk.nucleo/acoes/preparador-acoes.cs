namespace MapDisk.Nucleo;

public sealed record AvaliacaoSelecao(IReadOnlyList<ItemAcao> Itens, string? Bloqueio, TipoAcao Remocao)
{
    public bool PodeRemover => Bloqueio is null;

    public bool PodeMover => Bloqueio is null;

    public string TextoRemover => Remocao == TipoAcao.Lixeira ? "Enviar para a Lixeira" : "Excluir definitivamente";
}

public sealed record Confirmacao(string Titulo, string Texto, IReadOnlyList<string> Avisos, bool PedeExcluir, string TextoBotao);

/// <summary>
/// Da seleção da tela ao pedido: quais itens, o que bloqueia, se há Lixeira, o destino e os
/// textos da confirmação. Os textos de confirmação são os das seções 5 e 9 da verificação jurídica.
/// </summary>
public sealed class PreparadorAcoes
{
    private readonly LocaisProtegidos _locais;
    private readonly Func<string, DriveType> _tipoDaUnidade;
    private readonly IOperacoesArquivo _operacoes;
    private readonly bool _demonstracao;

    public PreparadorAcoes(LocaisProtegidos locais, Func<string, DriveType> tipoDaUnidade, IOperacoesArquivo operacoes, bool demonstracao)
    {
        _locais = locais;
        _tipoDaUnidade = tipoDaUnidade;
        _operacoes = operacoes;
        _demonstracao = demonstracao;
    }

    /// <summary>A seleção da tela em itens. A linha "[N arquivos]" não é item: volta no Grupo.</summary>
    public static (IReadOnlyList<ItemAcao> Itens, string? Grupo) ItensDaSelecao(IEnumerable<object> selecionados)
    {
        var itens = new List<ItemAcao>();
        string? grupo = null;
        foreach (var s in selecionados)
        {
            switch (s)
            {
                case LinhaArvore { Tipo: TipoLinha.GrupoArquivos } linhaGrupo:
                    grupo ??= linhaGrupo.Nome;
                    break;
                case LinhaArvore { Tipo: TipoLinha.Pasta } pasta:
                    itens.Add(ItemAcao.DaPasta(pasta.Pasta));
                    break;
                case LinhaArvore arquivo:
                    itens.Add(ItemAcao.DoArquivo(arquivo.Pasta, arquivo.Arquivo));
                    break;
                case LinhaDuplicado duplicado:
                    itens.Add(ItemAcao.DoArquivo(duplicado.Encontrado.Pasta, duplicado.Encontrado.Arquivo));
                    break;
                case LinhaArquivo linha:
                    itens.Add(ItemAcao.DoArquivo(linha.Encontrado.Pasta, linha.Encontrado.Arquivo));
                    break;
                case LinhaResumo { Pasta: { } perfil }:
                    itens.Add(ItemAcao.DaPasta(perfil));
                    break;
            }
        }

        return (itens, grupo);
    }

    public AvaliacaoSelecao Avaliar(IEnumerable<object> selecionados)
    {
        var lista = selecionados.ToList();

        // Sempre sobra uma cópia (regra 1).
        var todasAsCopias = lista.OfType<LinhaDuplicado>().GroupBy(d => d.Grupo).FirstOrDefault(g => g.Count() >= g.First().ArquivosNoGrupo);
        if (todasAsCopias is not null)
        {
            return Bloqueado($"Todas as cópias do grupo {todasAsCopias.Key} estão selecionadas. Deixe ao menos uma.");
        }

        var (itens, grupo) = ItensDaSelecao(lista);
        return grupo is not null ? Bloqueado($"{grupo}: abra o grupo e selecione os arquivos.") : AvaliarItens(itens);
    }

    /// <summary>Itens já convertidos, da seleção ou da resposta do cliente, pelas mesmas regras.</summary>
    public AvaliacaoSelecao AvaliarItens(IReadOnlyList<ItemAcao> itens)
    {
        // Item dentro de outra pasta selecionada sai: a ação sobre a pasta já o leva.
        var unicos = itens.DistinctBy(i => i.Caminho, StringComparer.OrdinalIgnoreCase).ToList();
        unicos = unicos
            .Where(i => !unicos.Any(o => o != i && o.EhPasta && Protecao.Dentro(i.Caminho, o.Caminho)))
            .ToList();
        if (unicos.Count == 0)
        {
            return Bloqueado("Selecione pastas ou arquivos na árvore ou nas listas.");
        }

        foreach (var item in unicos)
        {
            if (Protecao.Motivo(item.Caminho, item.EhPasta, item.Marcas, _locais) is { } motivo)
            {
                return Bloqueado($"{item.Caminho}: {motivo}", unicos);
            }

            if (item.EhPasta && item.Pasta.Estado == EstadoPasta.Link)
            {
                return Bloqueado($"{item.Nome}: é link. Trate pelo Explorer.", unicos);
            }

            if (item.EhPasta && item.Pasta.Estado == EstadoPasta.Excluida)
            {
                return Bloqueado($"{item.Nome}: pasta excluída da varredura. Use Atualizar esta pasta para ler antes de agir.", unicos);
            }

            if (item.EhPasta && item.Pasta.Estado != EstadoPasta.Lida)
            {
                return Bloqueado($"{item.Nome}: pasta sem leitura. Atualize ou varra como administrador antes.", unicos);
            }
        }

        var comLixeira = unicos.Count(i => Lixeiras.Existe(i.Caminho, _tipoDaUnidade));
        if (comLixeira > 0 && comLixeira < unicos.Count)
        {
            return Bloqueado("A seleção mistura lugares com e sem Lixeira. Selecione um lugar de cada vez.", unicos);
        }

        return new AvaliacaoSelecao(unicos, null, comLixeira > 0 ? TipoAcao.Lixeira : TipoAcao.Excluir);
    }

    public string? BloqueioDestino(IReadOnlyList<ItemAcao> itens, string destino)
    {
        foreach (var item in itens)
        {
            if (item.EhPasta && Protecao.Dentro(destino, item.Caminho))
            {
                return $"O destino fica dentro de {item.Nome}.";
            }
        }

        if (itens.All(i => string.Equals(Path.GetDirectoryName(i.Caminho.TrimEnd('\\'))?.TrimEnd('\\'), destino.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
        {
            return "Os itens já estão nesta pasta.";
        }

        if (Protecao.Motivo(Path.Combine(destino, "x"), false, MarcaArquivo.Nenhuma, _locais) is { } motivo)
        {
            return $"Destino: {motivo}";
        }

        var entreUnidades = itens.Where(i => !_operacoes.MesmoVolume(i.Caminho, destino)).Sum(i => i.Tamanho);
        if (entreUnidades > 0 && _operacoes.Livre(destino) is { } livre && livre < entreUnidades)
        {
            return $"Faltam {Formatador.Tamanho(entreUnidades - livre)} no destino.";
        }

        return null;
    }

    /// <summary>Os itens que mudaram desde a varredura, com o motivo, para o aviso antes da confirmação.</summary>
    public IReadOnlyList<string> Mudancas(IReadOnlyList<ItemAcao> itens) =>
        itens.Select(i => (i, m: _operacoes.Mudanca(i))).Where(x => x.m is not null).Select(x => $"{x.i.Caminho}: {x.m}").ToList();

    public Confirmacao Confirmar(PedidoAcao pedido)
    {
        var quantos = $"{Formatador.Plural(pedido.Itens.Count, "item", "itens")} ({Formatador.Tamanho(pedido.Total)})";
        var avisos = new List<string>();
        if (_demonstracao)
        {
            avisos.Add("Modo de demonstração: nada é apagado nem movido.");
        }

        if (pedido.Itens.Any(i => i.EhPasta && i.Pasta.Pai is { Nome: var pai } && string.Equals(pai, "Users", StringComparison.OrdinalIgnoreCase)))
        {
            avisos.Add("É a pasta de perfil de um usuário. Apagar a pasta não remove a conta do Windows.");
        }

        var semLeitura = pedido.Itens.Where(i => i.EhPasta).Sum(i => i.Pasta.PastasSemAcesso + i.Pasta.PastasComErro);
        if (semLeitura > 0)
        {
            avisos.Add($"{Formatador.Plural(semLeitura, "pasta sem leitura fica", "pastas sem leitura ficam")} dentro da seleção. O total pode ser maior que o mostrado.");
        }

        return pedido.Acao switch
        {
            TipoAcao.Lixeira => new Confirmacao(
                "Enviar para a Lixeira",
                $"Enviar {quantos} para a Lixeira de {Unidades(pedido)}? Eles podem ser restaurados pela Lixeira enquanto ela não for esvaziada.",
                avisos, false, "Enviar para a Lixeira"),
            TipoAcao.Excluir => new Confirmacao(
                "Excluir definitivamente",
                TextoExclusao(pedido, quantos),
                avisos, true, "Excluir definitivamente"),
            _ => new Confirmacao(
                "Mover",
                $"Mover {quantos} para {pedido.Destino}? A origem só é apagada depois de a cópia ser conferida.",
                avisos, false, "Mover"),
        };
    }

    // "C:" ou "C: e D:".
    private static string Unidades(PedidoAcao pedido) => string.Join(" e ", pedido.Itens
        .Select(i => Volumes.RaizDe(i.Caminho).TrimEnd('\\'))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    // Rede: texto da seção 5 da verificação jurídica. Unidade removível ou mapeada: seção 9.
    private static string TextoExclusao(PedidoAcao pedido, string quantos)
    {
        var local = Path.GetDirectoryName(pedido.Itens[0].Caminho.TrimEnd('\\')) ?? pedido.Itens[0].Caminho;
        var semLixeira = pedido.Itens.All(i => Alvo.EhRede(i.Caminho))
            ? "Pastas de rede não têm Lixeira."
            : "Nesta unidade, o MapDisk não usa a Lixeira.";
        return $"Excluir definitivamente {quantos} de {local}? {semLixeira} Depois de excluídos, os itens só voltam por uma cópia de segurança. Para confirmar, digite EXCLUIR.";
    }

    private static AvaliacaoSelecao Bloqueado(string motivo, IReadOnlyList<ItemAcao>? itens = null) =>
        new(itens ?? [], motivo, TipoAcao.Lixeira);
}
