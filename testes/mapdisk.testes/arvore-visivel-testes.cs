namespace MapDisk.Testes;

public class ArvoreVisivelTestes
{
    private static readonly DateTime _d = new(2026, 9, 1);

    /// <summary>
    /// C:\ com dois arquivos soltos (510 bytes), Users (Ana 300, Bruno 100) e Windows sem acesso.
    /// Total 910.
    /// </summary>
    private static NoPasta Exemplo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher(
            [new("pagefile.sys", 500, 500, _d, MarcaArquivo.Sistema), new("setup.log", 10, 4096, _d, MarcaArquivo.Nenhuma)],
            [users, windows]);
        var ana = new NoPasta("Ana", users);
        var bruno = new NoPasta("Bruno", users);
        users.Preencher([], [ana, bruno]);
        ana.Preencher([new("video.mp4", 300, 300, _d, MarcaArquivo.Nenhuma)], []);
        bruno.Preencher([new("foto.jpg", 100, 100, _d, MarcaArquivo.Nenhuma)], []);
        windows.MarcarSemAcesso("acesso negado");
        return raiz;
    }

    private static ArvoreVisivel Carregada(out NoPasta raiz)
    {
        raiz = Exemplo();
        var arvore = new ArvoreVisivel();
        arvore.Carregar(raiz);
        return arvore;
    }

    private static LinhaArvore Linha(ArvoreVisivel a, string nome) => a.Linhas.Single(l => l.Nome == nome);

    [Fact]
    public void Carregar_mostra_a_raiz_aberta_com_filhos_do_maior_para_o_menor()
    {
        var a = Carregada(out _);

        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Windows"], a.Linhas.Select(l => l.Nome));
        Assert.Equal([0, 1, 1, 1], a.Linhas.Select(l => l.Nivel));
        Assert.True(a.Linhas[0].Expandida);
    }

    [Fact]
    public void Pasta_sem_acesso_mostra_sem_acesso_e_nunca_zero()
    {
        var a = Carregada(out _);
        var windows = Linha(a, "Windows");

        Assert.True(windows.SemValor);
        Assert.Equal("sem acesso", windows.TextoTamanho);
        Assert.Equal("sem acesso", windows.TextoValor);
        Assert.Equal("sem acesso", windows.Rotulo);
        Assert.Equal("", windows.TextoPorcentagem);
        Assert.Equal("1 pasta sem leitura dentro", Linha(a, @"C:\").Rotulo);
    }

    [Fact]
    public void Expandir_e_recolher()
    {
        var a = Carregada(out _);

        a.Expandir(Linha(a, "Users"));
        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Ana", "Bruno", "Windows"], a.Linhas.Select(l => l.Nome));
        Assert.Equal(2, Linha(a, "Ana").Nivel);

        a.Recolher(Linha(a, "Users"));
        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Windows"], a.Linhas.Select(l => l.Nome));
    }

    [Fact]
    public void Grupo_de_arquivos_abre_com_o_rotulo_do_sistema()
    {
        var a = Carregada(out _);

        a.Alternar(Linha(a, "[2 arquivos]"));

        var pagefile = Linha(a, "pagefile.sys");
        Assert.Equal(2, pagefile.Nivel);
        Assert.Equal("arquivo do sistema", pagefile.Rotulo);
        Assert.Equal(510, Linha(a, "[2 arquivos]").Tamanho);
    }

    [Fact]
    public void Porcentagem_da_pasta_pai()
    {
        var a = Carregada(out _);
        a.Expandir(Linha(a, "Users"));

        Assert.Equal("100,0 %", Linha(a, @"C:\").TextoPorcentagem);
        Assert.Equal("44,0 %", Linha(a, "Users").TextoPorcentagem);
        Assert.Equal("75,0 %", Linha(a, "Ana").TextoPorcentagem);
        Assert.Equal(45, Linha(a, "Ana").LarguraBarra);
    }

    [Fact]
    public void Ordenar_por_nome_e_clicar_de_novo_inverte()
    {
        var a = Carregada(out _);

        a.Ordenar(ColunaOrdem.Nome);
        var crescente = a.Linhas.Select(l => l.Nome).ToList();
        Assert.True(crescente.IndexOf("Users") < crescente.IndexOf("Windows"));

        a.Ordenar(ColunaOrdem.Nome);
        var decrescente = a.Linhas.Select(l => l.Nome).ToList();
        Assert.True(decrescente.IndexOf("Windows") < decrescente.IndexOf("Users"));
    }

    [Fact]
    public void Modo_alocado_muda_o_valor_e_a_ordem()
    {
        var a = Carregada(out _);

        a.DefinirModo(ModoExibicao.Alocado);

        Assert.Equal("4,5 KB", Linha(a, "[2 arquivos]").TextoValor);
        Assert.Equal("400 Bytes", Linha(a, "Users").TextoValor);
        Assert.Equal("[2 arquivos]", a.Linhas[1].Nome);
    }

    [Fact]
    public void Modo_contagem_e_modo_porcentagem()
    {
        var a = Carregada(out _);

        a.DefinirModo(ModoExibicao.Contagem);
        Assert.Equal("2", Linha(a, "Users").TextoValor);

        a.DefinirModo(ModoExibicao.Porcentagem);
        Assert.Equal("44,0 %", Linha(a, "Users").TextoValor);
    }

    [Fact]
    public void Unidade_fixa()
    {
        var a = Carregada(out _);

        a.DefinirUnidade(UnidadeExibicao.KB);

        Assert.Equal("0,4 KB", Linha(a, "Users").TextoTamanho);
    }

    [Fact]
    public void Atualizar_mostra_o_que_a_varredura_leu_depois()
    {
        var raiz = new NoPasta(@"D:\", null);
        var a = new ArvoreVisivel();
        a.Carregar(raiz);
        Assert.Single(a.Linhas);

        var x = new NoPasta("x", raiz);
        raiz.Preencher([], [x]);
        a.Atualizar();

        Assert.Equal([@"D:\", "x"], a.Linhas.Select(l => l.Nome));
    }

    [Fact]
    public void Pasta_ainda_nao_lida_mostra_nao_lida_e_nunca_zero()
    {
        var raiz = new NoPasta(@"C:\", null);
        var lida = new NoPasta("Lida", raiz);
        var pendente = new NoPasta("Pendente", raiz);
        raiz.Preencher([], [lida, pendente]);
        lida.Preencher([new("a.bin", 100, 4096, _d, MarcaArquivo.Nenhuma)], []);
        var a = new ArvoreVisivel();
        a.Carregar(raiz);

        var linha = Linha(a, "Pendente");
        Assert.True(linha.SemValor);
        Assert.Equal("não lida", linha.TextoTamanho);
        Assert.Equal("não lida", linha.TextoValor);
        Assert.Equal("", linha.TextoArquivos);
        Assert.Equal("Pendente", a.Linhas[^1].Nome);
    }

    [Fact]
    public void A_mesma_linha_continua_depois_de_atualizar()
    {
        var a = Carregada(out _);
        var users = Linha(a, "Users");

        a.Atualizar();
        a.DefinirModo(ModoExibicao.Alocado);

        Assert.Same(users, Linha(a, "Users"));
    }
}
