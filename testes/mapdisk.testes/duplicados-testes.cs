namespace MapDisk.Testes;

public class DuplicadosTestes
{
    private const int Mb = 1024 * 1024;

    private static NoPasta Varrer(string caminho) =>
        new MotorVarredura().Iniciar(Alvo.Normalizar(caminho, out _)!, CancellationToken.None).Conclusao.GetAwaiter().GetResult().Raiz;

    private static void Gravar(string caminho, int tamanho, byte valor, byte ultimo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        var dados = Enumerable.Repeat(valor, tamanho).ToArray();
        dados[^1] = ultimo;
        File.WriteAllBytes(caminho, dados);
    }

    // Conta as leituras e falha de propósito no caminho pedido.
    private sealed class LeitorContado(string? falharEm = null) : ILeitorConteudo
    {
        private readonly LeitorConteudo _real = new();

        public List<(string Nome, long Bytes)> Leituras { get; } = [];

        public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar)
        {
            lock (Leituras)
            {
                Leituras.Add((Path.GetFileName(caminho), bytes));
            }

            if (falharEm is not null && caminho.EndsWith(falharEm, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("em uso");
            }

            return _real.Resumo(caminho, bytes, cancelar);
        }
    }

    [Fact]
    public async Task Acha_so_os_iguais_de_verdade_nas_tres_etapas()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 2 * Mb, 1, 1);
        Gravar(pasta.Caminho(@"copia\a-copia.bin"), 2 * Mb, 1, 1);
        Gravar(pasta.Caminho("fim-diferente.bin"), 2 * Mb, 1, 9);
        Gravar(pasta.Caminho("inicio-diferente.bin"), 2 * Mb, 7, 1);
        Gravar(pasta.Caminho("unico.bin"), 3 * Mb, 1, 1);
        var raiz = Varrer(pasta.Raiz);
        var leitor = new LeitorContado();

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(), leitor, null, CancellationToken.None);

        var grupo = Assert.Single(r.Grupos);
        Assert.Equal(["a-copia.bin", "a.bin"], grupo.Arquivos.Select(a => a.Arquivo.Nome).Order());
        Assert.Equal(2 * Mb, grupo.Repetido);
        Assert.Equal(1, grupo.Numero);
        Assert.DoesNotContain(leitor.Leituras, l => l.Nome == "unico.bin");
        Assert.DoesNotContain(leitor.Leituras, l => l.Nome == "inicio-diferente.bin" && l.Bytes == long.MaxValue);
        Assert.Contains(leitor.Leituras, l => l.Nome == "fim-diferente.bin" && l.Bytes == long.MaxValue);
    }

    [Fact]
    public async Task Arquivo_pequeno_e_lido_uma_vez_e_o_minimo_filtra()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("x.txt"), 100, 3, 3);
        Gravar(pasta.Caminho(@"sub\y.txt"), 100, 3, 3);
        var raiz = Varrer(pasta.Raiz);
        var leitor = new LeitorContado();

        var comMinimo = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(), leitor, null, CancellationToken.None);
        var semMinimo = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);

        Assert.Empty(comMinimo.Grupos);
        Assert.Single(semMinimo.Grupos);
        Assert.Equal(2, leitor.Leituras.Count);
    }

    [Fact]
    public async Task Arquivo_que_nao_abre_fica_fora_e_e_contado()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("b.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("c.bin"), 100, 1, 1);
        var raiz = Varrer(pasta.Raiz);

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), new LeitorContado("c.bin"), null, CancellationToken.None);

        Assert.Equal(2, Assert.Single(r.Grupos).Arquivos.Count);
        Assert.Equal(1, r.NaoLidos);
    }

    [Fact]
    public async Task Hard_link_nuvem_e_sistema_nao_entram()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        pasta.HardLink("a-link.bin", "a.bin");
        var raiz = Varrer(pasta.Raiz);
        var nuvem = new NoPasta(@"D:\", null);
        nuvem.Preencher(
            [
                new ArquivoInfo("n1.bin", 100, 0, new DateTime(2020, 1, 1), MarcaArquivo.NaNuvem),
                new ArquivoInfo("n2.bin", 100, 0, new DateTime(2020, 1, 1), MarcaArquivo.NaNuvem),
                new ArquivoInfo("pagefile.sys", 100, 100, new DateTime(2020, 1, 1), MarcaArquivo.Sistema),
                new ArquivoInfo("swapfile.sys", 100, 100, new DateTime(2020, 1, 1), MarcaArquivo.Sistema),
            ],
            []);
        var leitor = new LeitorContado();

        var r1 = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);
        var r2 = await Duplicados.ProcurarAsync(nuvem, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);

        Assert.Empty(r1.Grupos);
        Assert.Empty(r2.Grupos);
        Assert.Empty(leitor.Leituras);
    }

    [Fact]
    public async Task Cancelar_devolve_sem_grupos()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("b.bin"), 100, 1, 1);
        var raiz = Varrer(pasta.Raiz);
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), new LeitorConteudo(), null, cancelar.Token);

        Assert.True(r.Cancelado);
        Assert.Empty(r.Grupos);
    }

    [Fact]
    public void Rede_le_menos_ao_mesmo_tempo()
    {
        Assert.Equal(2, OpcoesDuplicados.Para(new NoPasta(@"\\srv\dados", null), Mb).Paralelos);
        Assert.Equal(4, OpcoesDuplicados.Para(new NoPasta(@"D:\", null), Mb).Paralelos);
    }

    private static async Task<(PainelDuplicados Painel, PastaTeste Pasta)> PainelComGrupo()
    {
        var pasta = new PastaTeste();
        Gravar(pasta.Caminho("velho.bin"), 100, 1, 1);
        File.SetLastWriteTime(pasta.Caminho("velho.bin"), new DateTime(2020, 1, 1));
        Gravar(pasta.Caminho(@"b\novo.bin"), 100, 1, 1);
        Gravar(pasta.Caminho(@"c\novo2.bin"), 100, 1, 1);
        var p = new PainelDuplicados(new LeitorConteudo()) { TamanhoMinimoMb = 0 };
        await p.ProcurarAsync(Varrer(pasta.Raiz));
        return (p, pasta);
    }

    [Fact]
    public async Task Painel_mostra_o_grupo_e_seleciona_as_copias_menos_a_mais_antiga()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            Assert.Equal(3, p.Linhas.Count);
            Assert.All(p.Linhas, l => Assert.Equal(1, l.Grupo));
            Assert.Equal("1 grupo com 200 Bytes em cópias", p.TextoEstado);
            Assert.Equal(["novo.bin", "novo2.bin"], p.Copias().Select(l => l.Nome).Order());
        }
    }

    [Fact]
    public async Task Tirar_depois_da_acao_desfaz_o_grupo_que_ficou_com_um()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            p.Tirar(p.Copias().Select(l => ItemAcao.DoArquivo(l.Encontrado.Pasta, l.Encontrado.Arquivo)));

            Assert.Empty(p.Linhas);
            Assert.Equal("Nenhum arquivo repetido acima do tamanho mínimo.", p.TextoEstado);
        }
    }

    [Fact]
    public async Task Selecionar_todas_as_copias_de_um_grupo_bloqueia()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            var preparador = new PreparadorAcoes(new LocaisProtegidos([], @"Z:\registro"), _ => DriveType.Fixed, new OperacoesDemonstracao(), false);

            Assert.Equal("Todas as cópias do grupo 1 estão selecionadas. Deixe ao menos uma.", preparador.Avaliar(p.Linhas).Bloqueio);
            Assert.Null(preparador.Avaliar(p.Copias()).Bloqueio);
            Assert.Equal(2, preparador.Avaliar(p.Copias()).Itens.Count);
        }
    }

    [Fact]
    public void Antes_de_procurar_explica_o_que_a_busca_faz()
    {
        var p = new PainelDuplicados(new LeitorConteudo());

        Assert.Equal("Clique em Procurar duplicados. A busca lê o conteúdo só dos arquivos de mesmo tamanho.", p.TextoEstado);
        Assert.False(p.Procurando);
    }

    [Fact]
    public async Task Demonstracao_mostra_os_dois_grupos_sem_abrir_arquivo()
    {
        var raiz = (await Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao).Raiz;

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(), new LeitorDemonstracao(), null, CancellationToken.None);

        Assert.Equal(2, r.Grupos.Count);
        Assert.All(r.Grupos, g => Assert.Equal(2, g.Arquivos.Count));
        Assert.Equal(5L * 1024 * 1024 * 1024, r.TotalRepetido);
    }

    [Fact]
    public async Task Sugerir_acrescenta_as_copias_e_mantem_a_mais_antiga()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            var lista = new ListaAvaliacao();
            var raiz = p.Resultado!.Pasta;

            Sugestao.Sugerir(lista, raiz, new CriteriosSugestao(MaioresPastas: 0, MaioresArquivos: 0, TamanhoMinimo: long.MaxValue), DateTime.Now, p.Resultado);

            Assert.Equal(["novo.bin", "novo2.bin"], lista.Itens.Select(i => i.Item.Nome).Order());
            Assert.All(lista.Itens, i => Assert.Equal($"cópia repetida de {p.Resultado.Grupos[0].Mantido.Caminho}", i.Motivo));
            Assert.EndsWith("velho.bin", p.Resultado.Grupos[0].Mantido.Caminho);
        }
    }
}
