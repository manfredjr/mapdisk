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

    [Fact]
    public void Blocos_tem_area_proporcional_e_cobrem_a_area_toda()
    {
        long[] valores = [6, 6, 4, 3, 2, 2, 1];

        var r = Treemap.Dispor(valores, 600, 400);

        Assert.Equal(valores.Length, r.Count);
        Assert.Equal(240000, r.Sum(b => b.Area), 3);
        for (var i = 0; i < valores.Length; i++)
        {
            Assert.Equal(240000d * valores[i] / 24, r[i].Area, 3);
        }
    }

    [Fact]
    public void Blocos_ficam_dentro_da_area_sem_se_sobrepor()
    {
        var r = Treemap.Dispor([50, 30, 10, 5, 3, 1, 1], 300, 200);

        foreach (var b in r)
        {
            Assert.True(b.X >= -1e-6 && b.Y >= -1e-6 && b.X + b.Largura <= 300 + 1e-6 && b.Y + b.Altura <= 200 + 1e-6);
        }

        for (var i = 0; i < r.Count; i++)
        {
            for (var j = i + 1; j < r.Count; j++)
            {
                var largura = Math.Min(r[i].X + r[i].Largura, r[j].X + r[j].Largura) - Math.Max(r[i].X, r[j].X);
                var altura = Math.Min(r[i].Y + r[i].Altura, r[j].Y + r[j].Altura) - Math.Max(r[i].Y, r[j].Y);
                Assert.False(largura > 1e-6 && altura > 1e-6, $"blocos {i} e {j} se sobrepõem");
            }
        }
    }

    [Fact]
    public void Blocos_evitam_retangulos_finos()
    {
        var r = Treemap.Dispor([6, 6, 4, 3, 2, 2, 1], 600, 400);

        Assert.All(r, b => Assert.True(Math.Max(b.Largura / b.Altura, b.Altura / b.Largura) < 3));
    }

    [Fact]
    public void Sem_valores_ou_sem_area_nao_ha_blocos()
    {
        Assert.Empty(Treemap.Dispor([], 300, 200));
        Assert.Empty(Treemap.Dispor([10, 5], 0, 200));
    }

    [Fact]
    public void Fatias_somam_uma_volta_proporcional_aos_valores()
    {
        var f = Pizza.Fatias([30, 10], 100);

        Assert.Equal(0, f[0].Inicio, 6);
        Assert.Equal(270, f[0].Fim, 6);
        Assert.Equal(270, f[1].Inicio, 6);
        Assert.Equal(360, f[1].Fim, 6);
    }

    [Fact]
    public void Caminho_da_fatia_no_formato_do_wpf()
    {
        var f = Pizza.Fatias([1, 1], 50);

        Assert.Equal("M 50,50 L 50,0 A 50,50 0 0 1 50,100 Z", f[0].Caminho);
        Assert.Equal("M 50,50 L 50,100 A 50,50 0 0 1 50,0 Z", f[1].Caminho);
    }

    [Fact]
    public void Fatia_maior_que_meia_volta_usa_o_arco_grande()
    {
        var f = Pizza.Fatias([3, 1], 50);

        Assert.Equal("M 50,50 L 50,0 A 50,50 0 1 1 0,50 Z", f[0].Caminho);
    }

    [Fact]
    public void Um_item_so_e_o_circulo_inteiro()
    {
        var f = Pizza.Fatias([7], 50);

        Assert.Equal("M 50,0 A 50,50 0 1 1 50,100 A 50,50 0 1 1 50,0 Z", Assert.Single(f).Caminho);
    }
}
