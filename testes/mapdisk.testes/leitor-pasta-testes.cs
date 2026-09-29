namespace MapDisk.Testes;

public class LeitorPastaTestes
{
    private static ResultadoLeitura Ler(string caminho, out List<ArquivoInfo> arquivos, out List<EntradaPasta> subpastas, out string? motivo, Func<long, bool>? primeiraVez = null, bool raizDoVolume = false)
    {
        arquivos = [];
        subpastas = [];
        return LeitorPasta.Ler(caminho, raizDoVolume, primeiraVez ?? (_ => true), arquivos, subpastas, out motivo);
    }

    [Fact]
    public void Le_arquivos_com_tamanho_e_subpastas()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 1000, new DateTime(2026, 3, 4, 5, 6, 0));
        t.Pasta("sub");

        Assert.Equal(ResultadoLeitura.Lida, Ler(t.Raiz, out var arquivos, out var subpastas, out _));

        var a = Assert.Single(arquivos);
        Assert.Equal("a.bin", a.Nome);
        Assert.Equal(1000, a.Tamanho);
        Assert.Equal(new DateTime(2026, 3, 4, 5, 6, 0), a.Modificacao);
        var sub = Assert.Single(subpastas);
        Assert.Equal("sub", sub.Nome);
        Assert.False(sub.EhLink);
    }

    [Fact]
    public void Alocado_vem_do_sistema_de_arquivos_em_clusters()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 5000);
        var cluster = Volumes.Ler(t.Raiz)!.Cluster;

        Ler(t.Raiz, out var arquivos, out _, out _);

        var alocado = Assert.Single(arquivos).Alocado;
        Assert.True(alocado >= 5000, $"alocado {alocado}");
        Assert.Equal(0, alocado % cluster);
    }

    [Fact]
    public void Juncao_vem_como_link()
    {
        using var t = new PastaTeste();
        t.Pasta("dados");
        t.Juncao("atalho", "dados");

        Ler(t.Raiz, out _, out var subpastas, out _);

        Assert.True(subpastas.Single(s => s.Nome == "atalho").EhLink);
        Assert.False(subpastas.Single(s => s.Nome == "dados").EhLink);
    }

    [Fact]
    public void Hard_link_repetido_e_marcado()
    {
        using var t = new PastaTeste();
        t.Arquivo("x.bin", 4096);
        t.HardLink("y.bin", "x.bin");
        var vistos = new HashSet<long>();

        Ler(t.Raiz, out var arquivos, out _, out _, vistos.Add);

        Assert.Equal(2, arquivos.Count);
        Assert.Single(arquivos, a => a.Marcas.HasFlag(MarcaArquivo.LinkRepetido));
    }

    [Fact]
    public void Pasta_negada_vem_sem_acesso()
    {
        using var t = new PastaTeste();
        var negada = t.NegarLeitura("fechada");

        Assert.Equal(ResultadoLeitura.SemAcesso, Ler(negada, out _, out _, out var motivo));
        Assert.Equal("acesso negado", motivo);
    }

    [Fact]
    public void Pasta_que_nao_existe_vem_com_erro()
    {
        using var t = new PastaTeste();

        Assert.Equal(ResultadoLeitura.Erro, Ler(t.Caminho("nao-existe"), out _, out _, out var motivo));
        Assert.Equal("pasta não encontrada", motivo);
    }

    [Fact]
    public void Caminho_longo_e_lido()
    {
        using var t = new PastaTeste();
        var relativo = Path.Combine(Enumerable.Repeat(new string('p', 60), 5).ToArray());
        t.Arquivo(Path.Combine(relativo, "fundo.bin"), 10);
        var caminho = t.Caminho(relativo);
        Assert.True(caminho.Length > 260);

        Assert.Equal(ResultadoLeitura.Lida, Ler(caminho, out var arquivos, out _, out _));
        Assert.Equal(10, Assert.Single(arquivos).Tamanho);
    }

    [Fact]
    public void Arquivo_de_sistema_so_na_raiz_do_volume()
    {
        using var t = new PastaTeste();
        t.Arquivo("pagefile.sys", 10);

        Ler(t.Raiz, out var naPasta, out _, out _);
        Ler(t.Raiz, out var naRaiz, out _, out _, raizDoVolume: true);

        Assert.False(Assert.Single(naPasta).Marcas.HasFlag(MarcaArquivo.Sistema));
        Assert.True(Assert.Single(naRaiz).Marcas.HasFlag(MarcaArquivo.Sistema));
    }
}
