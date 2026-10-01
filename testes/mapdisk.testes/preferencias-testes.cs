namespace MapDisk.Testes;

public class PreferenciasTestes
{
    [Fact]
    public void Texto_vai_e_volta_igual()
    {
        var p = new Preferencias(MaioresArquivos: 250, DuplicadosMinimoMb: 10, SugerirPastas: 5, SugerirArquivos: 7, SugerirAnos: 3, SugerirMinimoMb: 500);
        Assert.Equal(p, Preferencias.DeTexto(p.ParaTexto()));
    }

    [Fact]
    public void Valor_estranho_ou_fora_da_faixa_volta_ao_padrao()
    {
        var p = Preferencias.DeTexto(["maiores-arquivos=5", "duplicados-minimo-mb=abc", "sugerir-anos=0", "outra=1", "sem igual", "sugerir-pastas=30"]);
        Assert.Equal(100, p.MaioresArquivos);
        Assert.Equal(1, p.DuplicadosMinimoMb);
        Assert.Equal(2, p.SugerirAnos);
        Assert.Equal(30, p.SugerirPastas);
        Assert.True(Preferencias.Valido("maiores-arquivos", " 50 "));
        Assert.False(Preferencias.Valido("maiores-arquivos", "2000"));
        Assert.False(Preferencias.Valido("nao-existe", "1"));
    }

    [Fact]
    public void Criterios_saem_em_bytes_e_voltam()
    {
        var p = new Preferencias(SugerirMinimoMb: 200);
        Assert.Equal(200L * 1024 * 1024, p.Criterios.TamanhoMinimo);
        var c = new CriteriosSugestao(1, 2, 3, 4L * 1024 * 1024);
        Assert.Equal(c, p.ComCriterios(c).Criterios);
    }

    [Fact]
    public void Arquivo_grava_le_e_tolera_falta()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "preferencias-" + Guid.NewGuid().ToString("N"), "opcoes.txt");
        var armazem = new ArquivoPreferencias(arquivo);
        Assert.Equal(new Preferencias(), armazem.Ler());
        armazem.Gravar(new Preferencias(MaioresArquivos: 300));
        Assert.Equal(300, armazem.Ler().MaioresArquivos);
        Directory.Delete(Path.GetDirectoryName(arquivo)!, true);
    }

    [Fact]
    public void Esquecer_tira_so_aquele_alvo()
    {
        Assert.Equal([@"D:\"], UltimosAlvos.Esquecer([@"C:\Dados", @"D:\"], @"c:\dados"));
    }

    [Fact]
    public void Painel_aplica_e_grava_as_preferencias()
    {
        var armazem = new PreferenciasEmMemoria();
        armazem.Gravar(new Preferencias(MaioresArquivos: 20, DuplicadosMinimoMb: 8));
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Preferencias = armazem,
        });
        Assert.Equal(20, painel.Analises.QuantosMaiores);
        Assert.Equal(8, painel.Analises.Duplicados.TamanhoMinimoMb);

        painel.GravarPreferencias(new Preferencias(MaioresArquivos: 40));
        Assert.Equal(40, armazem.Ler().MaioresArquivos);
        Assert.Equal(40, painel.Analises.QuantosMaiores);
    }

    [Fact]
    public void Painel_esquece_e_limpa_os_alvos()
    {
        var historico = new HistoricoEmMemoria();
        historico.Gravar([@"E:\Fotos", @"F:\"]);
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Historico = historico,
        });
        painel.EsquecerAlvo(@"E:\Fotos");
        Assert.Equal([@"F:\"], painel.UltimosUsados);
        Assert.DoesNotContain(painel.Opcoes, o => o.Caminho == @"E:\Fotos");
        painel.LimparAlvos();
        Assert.Empty(painel.UltimosUsados);
    }

    [Fact]
    public async Task Analises_usam_a_quantidade_de_maiores()
    {
        var raiz = (await Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao).Raiz;
        var analises = new PainelAnalises { QuantosMaiores = 3 };
        await analises.CalcularAsync(raiz, new DateTime(2026, 10, 1));
        Assert.Equal(3, analises.Maiores.Count);
    }
}
