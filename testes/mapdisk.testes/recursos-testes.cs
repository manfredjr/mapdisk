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
        Assert.Contains("Click=\"AoClicarLogo\"", janela);
    }

    [Fact]
    public void Janela_tem_o_botao_sobre_com_os_avisos_da_licenca()
    {
        Assert.Contains("Click=\"AoAbrirSobre\"", File.ReadAllText(App("janela-principal.xaml")));
        var sobre = File.ReadAllText(App("janela-sobre.xaml"));
        foreach (var campo in new[] { "Sobre.Copyright", "Sobre.SoftwareLivre", "Sobre.LicencaEGarantias", "Sobre.Versoes", "Sobre.Repositorio", "CampoLicenca" })
        {
            Assert.Contains(campo, sobre);
        }
    }

    [Fact]
    public void Manifesto_roda_sem_administrador()
    {
        var manifesto = File.ReadAllText(App("app.manifest"));
        Assert.Contains("level=\"asInvoker\"", manifesto);
    }

    [Fact]
    public void Aba_grafico_e_a_primeira_do_painel()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("SizeChanged=\"AoRedimensionarGrafico\"", janela);
        Assert.Contains("MouseLeftButtonDown=\"AoClicarNoGrafico\"", janela);
        Assert.True(janela.IndexOf("Header=\"Gráfico\"", StringComparison.Ordinal)
            < janela.IndexOf("Header=\"Maiores arquivos\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Janela_tem_as_acoes_com_confirmacao()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Click=\"AoRemover\"", janela);
        Assert.Contains("Click=\"AoMover\"", janela);
        Assert.Contains("Click=\"AoAbrirRegistro\"", janela);
        Assert.Contains("SelectionMode=\"Extended\"", janela);
        Assert.Contains("ToolTipService.ShowOnDisabled=\"True\"", janela);
        Assert.Contains("{Binding TextoSessao}", janela);
        var confirmacao = File.ReadAllText(App("janela-confirmacao.xaml"));
        Assert.Contains("x:Name=\"CampoExcluir\"", confirmacao);
        Assert.Contains("IsCancel=\"True\"", confirmacao);
    }

    [Fact]
    public void Janela_tem_o_relatorio_para_o_cliente()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Click=\"AoAbrirMenuRelatorio\"", janela);
        Assert.Contains("Click=\"AoGerarRelatorio\"", janela);
        Assert.Contains("Click=\"AoLerResposta\"", janela);
        Assert.True(File.Exists(App("janela-relatorio.xaml")));
        Assert.True(File.Exists(App("janela-resposta.xaml")));
    }
}
