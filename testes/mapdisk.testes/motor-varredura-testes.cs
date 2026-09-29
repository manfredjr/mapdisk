namespace MapDisk.Testes;

public class MotorVarreduraTestes
{
    private static ResultadoVarredura Varrer(string caminho, CancellationToken cancelar = default) =>
        new MotorVarredura().Iniciar(Alvo.Normalizar(caminho, out _)!, cancelar).Conclusao.GetAwaiter().GetResult();

    [Fact]
    public void Soma_a_arvore_inteira()
    {
        using var t = new PastaTeste();
        t.Arquivo("4.bin", 10);
        t.Arquivo(@"a\1.bin", 1000);
        t.Arquivo(@"a\b\2.bin", 2000);
        t.Arquivo(@"c\3.bin", 3000);

        var r = Varrer(t.Raiz);

        Assert.False(r.Cancelada);
        Assert.Equal(6010, r.Raiz.Tamanho);
        Assert.Equal(4, r.Raiz.ArquivosTotal);
        Assert.Equal(3, r.Raiz.PastasTotal);
        Assert.Equal(EstadoPasta.Lida, r.Raiz.Estado);
        Assert.NotNull(r.Volume);
    }

    [Fact]
    public void Hard_link_conta_uma_vez()
    {
        using var t = new PastaTeste();
        t.Arquivo("x.bin", 4096);
        t.HardLink(@"outra\y.bin", "x.bin");

        var r = Varrer(t.Raiz);

        Assert.Equal(4096, r.Raiz.Tamanho);
        Assert.Equal(2, r.Raiz.ArquivosTotal);
    }

    [Fact]
    public void Juncao_nao_e_seguida()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"dados\grande.bin", 10_000);
        t.Juncao("atalho", "dados");

        var r = Varrer(t.Raiz);

        Assert.Equal(10_000, r.Raiz.Tamanho);
        var atalho = r.Raiz.Subpastas.Single(s => s.Nome == "atalho");
        Assert.Equal(EstadoPasta.Link, atalho.Estado);
        Assert.EndsWith("dados", atalho.DestinoLink);
    }

    [Fact]
    public void Pasta_sem_acesso_e_contada_e_nao_vira_zero()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 100);
        t.NegarLeitura("fechada");

        var r = Varrer(t.Raiz);

        Assert.Equal(1, r.Raiz.PastasSemAcesso);
        Assert.Equal(EstadoPasta.SemAcesso, r.Raiz.Subpastas.Single(s => s.Nome == "fechada").Estado);
        Assert.Equal(100, r.Raiz.Tamanho);
    }

    [Fact]
    public void Caminho_longo_entra_na_soma()
    {
        using var t = new PastaTeste();
        t.Arquivo(Path.Combine(Path.Combine(Enumerable.Repeat(new string('q', 60), 5).ToArray()), "fundo.bin"), 77);

        Assert.Equal(77, Varrer(t.Raiz).Raiz.Tamanho);
    }

    [Fact]
    public void Cancelada_antes_de_comecar_termina_como_cancelada()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 1);
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        var r = Varrer(t.Raiz, cancelar.Token);

        Assert.True(r.Cancelada);
        Assert.Equal(EstadoPasta.Pendente, r.Raiz.Estado);
    }

    [Fact]
    public void Alvo_que_nao_existe_marca_erro_na_raiz()
    {
        using var t = new PastaTeste();

        var r = Varrer(t.Caminho("nao-existe"));

        Assert.Equal(EstadoPasta.ErroLeitura, r.Raiz.Estado);
        Assert.Equal("pasta não encontrada", r.Raiz.Motivo);
    }

    [Fact]
    public async Task Raiz_existe_desde_o_inicio_para_a_tela()
    {
        using var t = new PastaTeste();
        var alvo = Alvo.Normalizar(t.Raiz, out _)!;

        var varredura = new MotorVarredura().Iniciar(alvo, CancellationToken.None);

        Assert.Equal(alvo, varredura.Raiz.Nome);
        Assert.Same(varredura.Raiz, (await varredura.Conclusao).Raiz);
    }

    [Fact]
    public async Task Muitas_varreduras_seguidas_dao_sempre_o_mesmo_total()
    {
        using var t = new PastaTeste();
        for (var a = 0; a < 12; a++)
        {
            for (var b = 0; b < 12; b++)
            {
                for (var c = 0; c < 6; c++)
                {
                    t.Arquivo(Path.Combine($"a{a}", $"b{b}", $"c{c}", "x.bin"), 3);
                }

                t.Arquivo(Path.Combine($"a{a}", $"b{b}", "y.bin"), 5);
            }
        }

        var alvo = Alvo.Normalizar(t.Raiz, out _)!;
        for (var i = 0; i < 30; i++)
        {
            var r = await new MotorVarredura(16).Iniciar(alvo, CancellationToken.None).Conclusao;
            Assert.False(r.Cancelada);
            Assert.Equal(12 * 12 * 7, r.Raiz.ArquivosTotal);
            Assert.Equal(12 + (12 * 12) + (12 * 12 * 6), r.Raiz.PastasTotal);
            Assert.Equal((12 * 12 * 6 * 3) + (12 * 12 * 5), r.Raiz.Tamanho);
        }
    }

    [Fact]
    public void Varredura_completa_nao_deixa_pasta_sem_leitura()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"a\b\c\x.bin", 1);

        Assert.Equal(0, Varrer(t.Raiz).PastasNaoLidas);
    }

    [Fact]
    public async Task Reler_uma_pasta_soma_so_o_que_mudou()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"a\1.bin", 100);
        t.Arquivo(@"b\2.bin", 10);
        var motor = new MotorVarredura();
        var r = await motor.Iniciar(Alvo.Normalizar(t.Raiz, out _)!, CancellationToken.None).Conclusao;
        var a = r.Raiz.Subpastas.Single(s => s.Nome == "a");
        t.Arquivo(@"a\3.bin", 1000);

        var relida = await motor.Reler(a, CancellationToken.None).Conclusao;

        Assert.Equal(1110, r.Raiz.Tamanho);
        Assert.Equal(3, r.Raiz.ArquivosTotal);
        Assert.Equal(2, r.Raiz.PastasTotal);
        Assert.Same(relida.Raiz, r.Raiz.Subpastas.Single(s => s.Nome == "a"));
        Assert.Equal(EstadoPasta.Lida, relida.Raiz.Estado);
    }

    [Fact]
    public async Task Link_nao_e_relido()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"dados\x.bin", 500);
        t.Juncao("atalho", "dados");
        var motor = new MotorVarredura();
        var r = await motor.Iniciar(Alvo.Normalizar(t.Raiz, out _)!, CancellationToken.None).Conclusao;
        var atalho = r.Raiz.Subpastas.Single(s => s.Nome == "atalho");

        await motor.Reler(atalho, CancellationToken.None).Conclusao;

        Assert.Equal(500, r.Raiz.Tamanho);
        Assert.Same(atalho, r.Raiz.Subpastas.Single(s => s.Nome == "atalho"));
    }
}
