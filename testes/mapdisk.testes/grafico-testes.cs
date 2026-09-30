namespace MapDisk.Testes;

public class GraficoTestes
{
    private static NoPasta PastaCom(params long[] tamanhos)
    {
        var raiz = new NoPasta(@"D:\", null);
        var subs = tamanhos.Select((_, i) => new NoPasta($"p{i + 1}", raiz)).ToArray();
        raiz.Preencher([], subs);
        for (var i = 0; i < subs.Length; i++)
        {
            subs[i].Preencher([new ArquivoInfo("a.bin", tamanhos[i], tamanhos[i], new DateTime(2026, 1, 1), MarcaArquivo.Nenhuma)], []);
        }

        return raiz;
    }

    [Fact]
    public void Itens_sao_as_subpastas_e_os_arquivos_soltos_do_maior_para_o_menor()
    {
        var c = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Tamanho, 200);

        Assert.Equal([ItensGrafico.NomeArquivos, "Users"], c.Itens.Select(i => i.Nome));
        Assert.Equal([5000L, 2400L], c.Itens.Select(i => i.Valor));
        Assert.Equal(TipoItemGrafico.Arquivos, c.Itens[0].Tipo);
        Assert.Equal("Users", c.Itens[1].Pasta!.Nome);
        Assert.Equal(7400, c.Total);
    }

    [Fact]
    public void Pasta_sem_leitura_fica_fora_e_aparece_pelo_nome()
    {
        var c = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Tamanho, 200);

        Assert.DoesNotContain(c.Itens, i => i.Nome == "Windows");
        Assert.Equal(["Windows"], c.NaoLidas);
    }

    [Fact]
    public void Segue_o_modo_da_arvore()
    {
        var alocado = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Alocado, 200);
        var contagem = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Contagem, 200);
        var porcentagem = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Porcentagem, 200);

        Assert.Equal([12288L, 8192L], alocado.Itens.Select(i => i.Valor));
        Assert.Equal("Users", alocado.Itens[0].Nome);
        Assert.Equal([4L, 1L], contagem.Itens.Select(i => i.Valor));
        Assert.Equal([5000L, 2400L], porcentagem.Itens.Select(i => i.Valor));
    }

    [Fact]
    public void Acima_do_maximo_o_resto_vira_outros_itens()
    {
        var c = ItensGrafico.DaPasta(PastaCom(50, 40, 30, 20, 10), ModoExibicao.Tamanho, 3);

        Assert.Equal(["p1", "p2", "3 outros itens"], c.Itens.Select(i => i.Nome));
        Assert.Equal(60, c.Itens[2].Valor);
        Assert.Equal(TipoItemGrafico.Outros, c.Itens[2].Tipo);
        Assert.Equal(150, c.Total);
    }

    [Fact]
    public void Pasta_vazia_e_link_nao_entram()
    {
        var raiz = new NoPasta(@"D:\", null);
        var vazia = new NoPasta("vazia", raiz);
        var link = new NoPasta("atalho", raiz);
        raiz.Preencher([], [vazia, link]);
        vazia.Preencher([], []);
        link.MarcarLink(@"D:\outro");

        var c = ItensGrafico.DaPasta(raiz, ModoExibicao.Tamanho, 200);

        Assert.Empty(c.Itens);
        Assert.Empty(c.NaoLidas);
        Assert.Equal(0, c.Total);
    }
}
