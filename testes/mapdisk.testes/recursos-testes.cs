namespace MapDisk.Testes;

public class RecursosTestes
{
    private static string App(string relativo) =>
        Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "src", "mapdisk", relativo);

    [Fact]
    public void Fonte_vai_com_a_licenca()
    {
        foreach (var fonte in new[] { "montserrat-regular.ttf", "montserrat-semibold.ttf", "montserrat-extrabold.ttf", "ofl.txt" })
        {
            Assert.True(File.Exists(App(Path.Combine("recursos", "fontes", fonte))), fonte);
        }
    }

    [Fact]
    public void Tema_tem_as_cores_oficiais_da_mt()
    {
        var tema = File.ReadAllText(App(Path.Combine("tema", "tema-mt.xaml")));
        foreach (var cor in new[] { "#006B2D", "#0F8F2F", "#43A92C", "#9AD52B", "#202020", "#F4F4F4" })
        {
            Assert.Contains(cor, tema);
        }

        Assert.DoesNotContain("mapnet", tema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Janela_mostra_o_logo_da_mt()
    {
        Assert.True(File.Exists(App(Path.Combine("recursos", "mt-logo.png"))));
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("component/recursos/mt-logo.png", janela);
    }

    [Fact]
    public void Manifesto_roda_sem_administrador()
    {
        var manifesto = File.ReadAllText(App("app.manifest"));
        Assert.Contains("level=\"asInvoker\"", manifesto);
    }
}
