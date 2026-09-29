namespace MapDisk.Testes;

public class NoPastaTestes
{
    private static readonly DateTime _d = new(2026, 9, 1);

    private static ArquivoInfo Arq(string nome, long tamanho, long alocado, MarcaArquivo marcas = MarcaArquivo.Nenhuma, DateTime? data = null) =>
        new(nome, tamanho, alocado, data ?? _d, marcas);

    [Fact]
    public void Preencher_soma_na_pasta_e_em_todas_acima()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", a);
        raiz.Preencher([], [a]);
        a.Preencher([Arq("1.bin", 100, 4096)], [b]);
        b.Preencher([Arq("2.bin", 10, 4096), Arq("3.bin", 20, 4096)], []);

        Assert.Equal(130, raiz.Tamanho);
        Assert.Equal(12288, raiz.Alocado);
        Assert.Equal(3, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.PastasTotal);
        Assert.Equal(1, a.PastasTotal);
        Assert.Equal(30, b.TamanhoProprio);
        Assert.Equal(100, a.TamanhoProprio);
        Assert.Equal(EstadoPasta.Lida, b.Estado);
    }

    [Fact]
    public void Pasta_sem_acesso_fica_marcada_e_contada()
    {
        var raiz = new NoPasta(@"C:\", null);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([], [windows]);
        windows.MarcarSemAcesso("acesso negado");

        Assert.Equal(EstadoPasta.SemAcesso, windows.Estado);
        Assert.Equal("acesso negado", windows.Motivo);
        Assert.Equal(1, raiz.PastasSemAcesso);
        Assert.Equal(0, raiz.PastasComErro);
    }

    [Fact]
    public void Pasta_com_erro_e_contada_separada()
    {
        var raiz = new NoPasta(@"\\srv\d\", null);
        var x = new NoPasta("x", raiz);
        raiz.Preencher([], [x]);
        x.MarcarErro("rede caiu");

        Assert.Equal(EstadoPasta.ErroLeitura, x.Estado);
        Assert.Equal(1, raiz.PastasComErro);
    }

    [Fact]
    public void Hard_link_repetido_aparece_mas_nao_soma()
    {
        var raiz = new NoPasta(@"C:\", null);
        raiz.Preencher([Arq("x.bin", 4096, 4096), Arq("y.bin", 4096, 4096, MarcaArquivo.LinkRepetido)], []);

        Assert.Equal(4096, raiz.Tamanho);
        Assert.Equal(2, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.Arquivos.Count);
    }

    [Fact]
    public void Link_nao_soma_e_guarda_o_destino()
    {
        var raiz = new NoPasta(@"C:\", null);
        var link = new NoPasta("Documents and Settings", raiz);
        link.MarcarLink(@"C:\Users");
        raiz.Preencher([], [link]);

        Assert.Equal(EstadoPasta.Link, link.Estado);
        Assert.Equal(@"C:\Users", link.DestinoLink);
        Assert.Equal(0, raiz.Tamanho);
    }

    [Fact]
    public void Ultima_modificacao_e_a_mais_nova_da_subarvore()
    {
        var raiz = new NoPasta(@"C:\", null, new DateTime(2026, 1, 1));
        var a = new NoPasta("a", raiz, new DateTime(2026, 2, 1));
        raiz.Preencher([Arq("velho.txt", 1, 1, data: new DateTime(2025, 1, 1))], [a]);
        a.Preencher([Arq("novo.txt", 1, 1, data: new DateTime(2026, 9, 20))], []);

        Assert.Equal(new DateTime(2026, 9, 20), raiz.UltimaModificacao);
    }

    [Fact]
    public void Caminho_completo_e_nivel()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var ana = new NoPasta("Ana", users);
        var rede = new NoPasta(@"\\srv\d\", null);
        var x = new NoPasta("x", rede);

        Assert.Equal(@"C:\Users\Ana", ana.CaminhoCompleto());
        Assert.Equal(@"C:\", raiz.CaminhoCompleto());
        Assert.Equal(@"\\srv\d\x", x.CaminhoCompleto());
        Assert.Equal(2, ana.Nivel);
    }

    [Fact]
    public void Soma_certa_com_muitas_tarefas_ao_mesmo_tempo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var filhas = Enumerable.Range(0, 2000).Select(i => new NoPasta($"p{i}", raiz)).ToArray();
        raiz.Preencher([], filhas);

        Parallel.ForEach(filhas, f => f.Preencher([Arq("a", 3, 4096), Arq("b", 7, 4096)], []));

        Assert.Equal(20_000, raiz.Tamanho);
        Assert.Equal(4000, raiz.ArquivosTotal);
        Assert.Equal(2000, raiz.PastasTotal);
    }

    [Fact]
    public void Conta_as_pastas_que_ficaram_sem_leitura()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", raiz);
        var c = new NoPasta("c", a);
        raiz.Preencher([], [a, b]);
        a.Preencher([], [c]);

        Assert.Equal(2, raiz.ContarNaoLidas());
        Assert.Equal(1, a.ContarNaoLidas());
    }

    [Fact]
    public void Descontar_e_trocar_deixam_os_totais_certos()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", raiz);
        raiz.Preencher([], [a, b]);
        var fechada = new NoPasta("fechada", a);
        a.Preencher([Arq("1.bin", 100, 4096)], [fechada]);
        fechada.MarcarSemAcesso("acesso negado");
        b.Preencher([Arq("2.bin", 10, 4096)], []);

        a.DescontarAcima();
        var nova = new NoPasta("a", raiz);
        raiz.TrocarSubpasta(a, nova);
        nova.Preencher([Arq("1.bin", 300, 4096)], []);

        Assert.Equal(310, raiz.Tamanho);
        Assert.Equal(2, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.PastasTotal);
        Assert.Equal(0, raiz.PastasSemAcesso);
        Assert.Same(nova, raiz.Subpastas[0]);
    }
}
