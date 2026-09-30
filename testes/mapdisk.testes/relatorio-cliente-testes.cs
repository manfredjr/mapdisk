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

    [Fact]
    public void Planilha_preenchida_como_o_excel_salva_e_lida_de_volta()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("avaliacao.xlsx");
        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);
        PreencherComoExcel(arquivo, decididoPor: "Ana Cliente", i11: "Apagar", i12: "Mover", j12: "arquivo morto");

        var r = PlanilhaAvaliacao.Ler(arquivo);

        Assert.Equal("20260930-140509", r.Relatorio);
        Assert.Equal("Ana Cliente", r.DecididoPor);
        Assert.Equal(@"C:\", r.Pasta);
        Assert.Equal([Decisao.Apagar, Decisao.Mover], r.Itens.Select(i => i.Decisao));
        Assert.Equal("arquivo morto", r.Itens[1].Destino);
        Assert.Equal(@"C:\video.mp4", r.Itens[0].Caminho);
        Assert.Equal(new DateTime(2023, 1, 10).Ticks, r.Itens[0].Ticks);
    }

    // Faz o que o Excel faz ao salvar: textos na tabela de textos compartilhados.
    private static void PreencherComoExcel(string arquivo, string decididoPor, string i11, string i12, string j12)
    {
        using var zip = System.IO.Compression.ZipFile.Open(arquivo, System.IO.Compression.ZipArchiveMode.Update);
        var entrada = zip.GetEntry("xl/worksheets/sheet1.xml")!;
        System.Xml.Linq.XDocument aba;
        using (var leitura = entrada.Open())
        {
            aba = System.Xml.Linq.XDocument.Load(leitura);
        }

        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        string[] textos = [decididoPor, i11, i12, j12];
        void Por(string referencia, int indice)
        {
            var numeroLinha = int.Parse(referencia[1..], System.Globalization.CultureInfo.InvariantCulture);
            var linha = aba.Descendants(ns + "row").First(l => (int)l.Attribute("r")! == numeroLinha);
            linha.Add(new System.Xml.Linq.XElement(ns + "c", new System.Xml.Linq.XAttribute("r", referencia), new System.Xml.Linq.XAttribute("t", "s"),
                new System.Xml.Linq.XElement(ns + "v", indice)));
        }

        Por("B6", 0);
        Por("I11", 1);
        Por("I12", 2);
        Por("J12", 3);
        entrada.Delete();
        using (var escrita = zip.CreateEntry("xl/worksheets/sheet1.xml").Open())
        {
            aba.Save(escrita);
        }

        var compartilhados = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement(ns + "sst",
            textos.Select(t => new System.Xml.Linq.XElement(ns + "si", new System.Xml.Linq.XElement(ns + "t", t)))));
        using var saida = zip.CreateEntry("xl/sharedStrings.xml").Open();
        compartilhados.Save(saida);
    }

    [Fact]
    public void Planilha_sem_a_aba_controle_explica()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("sem-controle.xlsx");
        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);
        using (var zip = System.IO.Compression.ZipFile.Open(arquivo, System.IO.Compression.ZipArchiveMode.Update))
        {
            var livro = LerEntrada(zip, "xl/workbook.xml");
            zip.GetEntry("xl/workbook.xml")!.Delete();
            using var escrita = new StreamWriter(zip.CreateEntry("xl/workbook.xml").Open());
            escrita.Write(livro.Replace("<sheet name=\"Controle\" sheetId=\"2\" state=\"hidden\" r:id=\"rId2\"/>", string.Empty));
        }

        var erro = Assert.Throws<FormatException>(() => PlanilhaAvaliacao.Ler(arquivo));

        Assert.Equal("A planilha não tem a aba Controle. Use a planilha gerada pelo MapDisk.", erro.Message);
    }

    [Fact]
    public void Arquivo_que_nao_e_planilha_explica()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Arquivo("falsa.xlsx", 100);

        var erro = Assert.Throws<FormatException>(() => PlanilhaAvaliacao.Ler(arquivo));

        Assert.Equal("A planilha está danificada ou não é a do relatório.", erro.Message);
    }

    [Fact]
    public void Resposta_da_pagina_em_json()
    {
        var json = """
            {"formato":"mapdisk-avaliacao-resposta","versao":1,"relatorio":"20260930-140509","pasta":"C:\\","decididoPor":"Ana","data":"2026-10-01T10:00:00Z",
             "observacao":"ok","itens":[{"numero":1,"caminho":"C:\\video.mp4","bytes":"5000","modificacao":"638091648000000000",
             "decisao":"apagar","destino":"","observacao":""},{"numero":2,"caminho":"C:\\Users","bytes":"2400","modificacao":"0",
             "decisao":"","destino":"","observacao":"ver com o financeiro"}]}
            """;

        var r = RespostaAvaliacao.LerJson(json);

        Assert.Equal("Ana", r.DecididoPor);
        Assert.Equal(@"C:\", r.Pasta);
        Assert.Equal([Decisao.Apagar, Decisao.SemDecisao], r.Itens.Select(i => i.Decisao));
        Assert.Equal(5000, r.Itens[0].Bytes);
        Assert.Equal("ver com o financeiro", r.Itens[1].Observacao);
    }

    [Fact]
    public void Json_de_outro_formato_e_recusado()
    {
        var erro = Assert.Throws<FormatException>(() => RespostaAvaliacao.LerJson("{\"formato\":\"outro\"}"));

        Assert.Equal("Este arquivo não é uma resposta de relatório do MapDisk.", erro.Message);
    }

    [Theory]
    [InlineData("1, 3, 7-9", new[] { 1, 3, 7, 8, 9 })]
    [InlineData("2", new[] { 2 })]
    [InlineData(" 4 ; 5 ", new[] { 4, 5 })]
    public void Numeros_com_intervalos(string texto, int[] esperado)
    {
        Assert.Equal(esperado, RespostaAvaliacao.Numeros(texto));
    }

    [Fact]
    public void Numeros_invalidos_explicam()
    {
        Assert.Equal("Use números e intervalos, como 1, 3, 7-9.", Assert.Throws<FormatException>(() => RespostaAvaliacao.Numeros("um")).Message);
    }

    [Fact]
    public void Casar_acha_os_itens_e_aponta_o_que_mudou_ou_sumiu()
    {
        var raiz = AnalisesTestes.Exemplo();
        var video = raiz.Arquivos[0];
        var resposta = new RespostaAvaliacao("1", @"C:\", "Ana", "", "",
        [
            new DecisaoItem(1, @"C:\video.mp4", video.Tamanho, video.Modificacao.Ticks, Decisao.Apagar, "", ""),
            new DecisaoItem(2, @"C:\Users\bruno", 999, 0, Decisao.Apagar, "", ""),
            new DecisaoItem(3, @"C:\Users\carla", 10, 0, Decisao.Apagar, "", ""),
        ]);

        var c = RespostaAvaliacao.Casar(resposta, raiz);

        Assert.Null(c[0].Problema);
        Assert.Equal("video.mp4", c[0].Item!.Nome);
        Assert.Equal("mudou depois do relatório", c[1].Problema);
        Assert.Equal("não encontrado", c[2].Problema);
        Assert.Null(c[2].Item);
    }

    [Fact]
    public void Marcar_pelos_numeros_muda_so_esses()
    {
        var r = new RespostaAvaliacao("1", @"C:\", "", "", "",
            [new DecisaoItem(1, "a", 1, 0, Decisao.SemDecisao, "", ""), new DecisaoItem(2, "b", 1, 0, Decisao.Manter, "", "")]);

        var nova = r.ComDecisao([2], Decisao.Apagar);

        Assert.Equal([Decisao.SemDecisao, Decisao.Apagar], nova.Itens.Select(i => i.Decisao));
    }
}
