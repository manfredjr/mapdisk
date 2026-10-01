namespace MapDisk.Testes;

public class PaginaTestes
{
    private static string Publico(string relativo) =>
        Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "public", relativo);

    [Fact]
    public void Pagina_nao_carrega_nada_de_fora()
    {
        var html = File.ReadAllText(Publico("index.html"));
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("<link rel=\"stylesheet\"", html);
    }

    [Fact]
    public void Pagina_tem_download_licenca_e_imagens()
    {
        var html = File.ReadAllText(Publico("index.html"));
        Assert.Contains("https://github.com/manfredjr/mapdisk/releases/latest", html);
        Assert.Contains("GPL-3.0", html);
        Assert.Contains("https://www.manfred.com.br", html);
        foreach (var imagem in new[] { "mt-logo.png", "mapdisk.png", "janela.png", "relatorio.png" })
        {
            Assert.Contains($"imagens/{imagem}", html);
            Assert.True(File.Exists(Publico(Path.Combine("imagens", imagem))), imagem);
        }
    }

    [Fact]
    public void Pagina_nao_usa_marca_de_terceiro()
    {
        Assert.DoesNotContain("treesize", File.ReadAllText(Publico("index.html")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Publicacao_confere_a_pagina_e_nao_expoe_a_conta()
    {
        var raiz = CaracteresProibidosTestes.RaizDoRepositorio();
        var htaccess = File.ReadAllText(Publico(".htaccess"));
        Assert.Contains("Options -Indexes", htaccess);
        Assert.Contains("script-src 'none'", htaccess);
        var deploy = File.ReadAllText(Path.Combine(raiz, ".cpanel.yml"));
        Assert.Contains("test -f $REPO/public/index.html", deploy);
        Assert.Contains("$HOME/repositories/mapdisk", deploy);
        Assert.DoesNotContain("/home/", deploy);
    }
}
