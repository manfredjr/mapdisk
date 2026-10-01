namespace MapDisk.Testes;

public class PaginaTestes
{
    private static string Publico(string relativo) =>
        Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "public", relativo);

    [Theory]
    [InlineData("index.html")]
    [InlineData("privacidade.html")]
    public void Paginas_nao_carregam_nada_de_fora(string pagina)
    {
        var html = File.ReadAllText(Publico(pagina));
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("@import", File.ReadAllText(Publico(Path.Combine("css", "site.css"))));
        Assert.Contains("<link rel=\"stylesheet\" href=\"css/site.css\">", html);
        Assert.Equal(1, html.Split("rel=\"stylesheet\"").Length - 1);
        Assert.Contains("href=\"privacidade.html\"", File.ReadAllText(Publico("index.html")));
    }

    [Fact]
    public void Pagina_tem_download_licenca_e_imagens()
    {
        var html = File.ReadAllText(Publico("index.html"));
        Assert.Contains("href=\"https://github.com/manfredjr/mapdisk/releases/latest/download/mapdisk.exe\"", html);
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
        Assert.DoesNotContain("treesize", File.ReadAllText(Publico("privacidade.html")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Publicacao_confere_a_pagina_e_nao_expoe_a_conta()
    {
        var raiz = CaracteresProibidosTestes.RaizDoRepositorio();
        var htaccess = File.ReadAllText(Publico(".htaccess"));
        Assert.Contains("Options -Indexes", htaccess);
        Assert.Contains("script-src 'self'", htaccess);
        Assert.DoesNotContain("unsafe-eval", htaccess);
        var deploy = File.ReadAllText(Path.Combine(raiz, ".cpanel.yml"));
        Assert.Contains("test -f $REPO/public/index.html", deploy);
        Assert.Contains("$HOME/repositories/mapdisk", deploy);
        Assert.DoesNotContain("/home/", deploy);
    }
}
