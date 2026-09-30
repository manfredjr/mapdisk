namespace MapDisk.Testes;

public class RelatorioClienteTestes
{
    internal static ArvoreVisivel ArvoreAberta()
    {
        var arvore = new ArvoreVisivel();
        arvore.Carregar(AnalisesTestes.Exemplo());
        arvore.AbrirNiveis(3);
        return arvore;
    }

    internal static LinhaArvore LinhaDe(ArvoreVisivel arvore, string nome) => arvore.Linhas.First(l => l.Nome == nome);

    [Fact]
    public void Itens_da_selecao_convertem_linhas_e_apontam_o_grupo()
    {
        var arvore = ArvoreAberta();

        var (itens, grupo) = PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "bruno"), LinhaDe(arvore, "Users")]);

        Assert.Equal([@"C:\Users\bruno", @"C:\Users"], itens.Select(i => i.Caminho));
        Assert.Null(grupo);
        Assert.Equal("[1 arquivo]", PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "[1 arquivo]")]).Grupo);
    }

    [Fact]
    public void Painel_avalia_itens_vindos_da_resposta()
    {
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Locais = AcoesTestes.Protegidos with { PastaRegistro = @"E:\registro" },
            TipoDaUnidade = _ => DriveType.Fixed,
        });
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];

        painel.AvaliarItens([ItemAcao.DaPasta(bruno)]);

        Assert.True(painel.PodeRemover);
        Assert.Equal(TipoAcao.Lixeira, painel.Selecao.Remocao);
    }

    [Fact]
    public void Lista_numera_do_maior_para_o_menor_e_junta_os_motivos()
    {
        var raiz = AnalisesTestes.Exemplo();
        var bruno = raiz.Subpastas[0].Subpastas[1];
        var lista = new ListaAvaliacao();

        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "compactado e backup");

        Assert.Equal(["video.mp4", "caixa.pst"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal([1, 2], lista.Itens.Select(i => i.Numero));
        Assert.Equal("entre os 50 maiores arquivos; compactado e backup", lista.Itens[1].Motivo);
        Assert.Equal(7000, lista.Total);
    }

    [Fact]
    public void Pasta_na_lista_leva_o_que_esta_dentro_e_pasta_sem_leitura_nao_entra()
    {
        var raiz = AnalisesTestes.Exemplo();
        var users = raiz.Subpastas[0];
        var bruno = users.Subpastas[1];
        var lista = new ListaAvaliacao();

        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "x");
        lista.Acrescentar(ItemAcao.DaPasta(users), "y");
        lista.Acrescentar(ItemAcao.DaPasta(bruno), "z");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[1]), "w");

        Assert.Equal([@"C:\Users"], lista.Itens.Select(i => i.Item.Caminho));
        Assert.Equal("Pasta", lista.Itens[0].Tipo);
        Assert.Equal(4, lista.Itens[0].Arquivos);
    }

    [Fact]
    public void Tirar_pelo_numero()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "x");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[0]), "y");

        lista.Tirar([1]);

        Assert.Equal(["Users"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal(1, lista.Itens[0].Numero);
    }

    [Fact]
    public void Sugerir_usa_os_criterios()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        var criterios = new CriteriosSugestao(MaioresPastas: 1, MaioresArquivos: 1, AnosSemAlteracao: 2, TamanhoMinimo: 1000);

        Sugestao.Sugerir(lista, raiz, criterios, AnalisesTestes.Hoje);

        Assert.Equal(["video.mp4", "Users"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal("entre os 1 maiores arquivos; sem alteração há mais de 2 anos", lista.Itens[0].Motivo);
        Assert.Equal("entre as 1 maiores pastas", lista.Itens[1].Motivo);
    }

    internal static Avaliacao Exemplo()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[0]), Sugestao.EscolhidoPeloTecnico);
        var gerado = new DateTime(2026, 9, 30, 14, 5, 9);
        return new Avaliacao(Avaliacao.NumeroDe(gerado), "Cliente Exemplo", "tecnico", "Veja o que pode sair.", raiz, gerado, 1000, lista.Itens);
    }

    [Fact]
    public void Numero_e_nome_do_arquivo()
    {
        var a = Exemplo();

        Assert.Equal("20260930-140509", a.Numero);
        Assert.Equal("avaliacao-c-2026-09-30", a.NomeDoArquivo);
        Assert.Equal("avaliacao-dados-publicos-2026-09-30",
            (a with { Pasta = new NoPasta(@"\\srv\Dados Públicos", null) }).NomeDoArquivo);
    }

    [Fact]
    public void Pagina_traz_os_itens_as_opcoes_e_nao_carrega_nada_de_fora()
    {
        var html = PaginaAvaliacao.Gerar(Exemplo());

        Assert.Contains("Cliente Exemplo", html);
        Assert.Contains("data-numero=\"1\"", html);
        Assert.Contains("data-caminho=\"C:\\video.mp4\"", html);
        Assert.Contains("value=\"apagar\"", html);
        Assert.Contains("value=\"conversar\"", html);
        Assert.Contains(PaginaAvaliacao.FormatoResposta, html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(TextosAvaliacao.Autorizacao), html);
        Assert.DoesNotMatch("src=\"(?!data:)", html);
        Assert.DoesNotContain("url(", html);
        Assert.DoesNotMatch("<input[^>]*checked", html);
    }

    [Fact]
    public void Pagina_escapa_o_que_vem_da_arvore()
    {
        var raiz = new NoPasta(@"D:\", null);
        var arquivo = new ArquivoInfo("<script>x</script>.txt", 10, 10, new DateTime(2020, 1, 1), MarcaArquivo.Nenhuma);
        raiz.Preencher([arquivo], []);
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, arquivo), "x");

        var html = PaginaAvaliacao.Gerar(new Avaliacao("1", "A & B", "t", "", raiz, DateTime.Now, null, lista.Itens));

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("A &amp; B", html);
    }

    [Fact]
    public void Planilha_tem_as_duas_abas_e_a_lista_de_opcoes()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("avaliacao.xlsx");

        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);

        using var zip = System.IO.Compression.ZipFile.OpenRead(arquivo);
        var livro = LerEntrada(zip, "xl/workbook.xml");
        Assert.Contains("name=\"Avaliação\"", livro);
        Assert.Contains("name=\"Controle\" sheetId=\"2\" state=\"hidden\"", livro);
        var aba = LerEntrada(zip, "xl/worksheets/sheet1.xml");
        Assert.Contains("<formula1>\"Apagar,Mover,Manter,Conversar\"</formula1>", aba);
        Assert.Contains("sqref=\"I11:I12\"", aba);
        Assert.Contains("Cliente Exemplo", aba);
        var controle = LerEntrada(zip, "xl/worksheets/sheet2.xml");
        Assert.Contains("20260930-140509", controle);
        Assert.Contains(new DateTime(2023, 1, 10).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture), controle);
    }

    internal static string LerEntrada(System.IO.Compression.ZipArchive zip, string nome)
    {
        using var leitor = new StreamReader(zip.GetEntry(nome)!.Open());
        return leitor.ReadToEnd();
    }
}
