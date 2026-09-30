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
}
