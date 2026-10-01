namespace MapDisk.Testes;

public class RegrasExclusaoTestes
{
    [Fact]
    public void Nome_vale_em_qualquer_nivel_sem_diferenca_de_maiuscula()
    {
        var r = RegrasExclusao.De(["node_modules"]);
        Assert.Equal("node_modules", r.Motivo(@"D:\Projetos\site\node_modules", "node_modules"));
        Assert.Equal("node_modules", r.Motivo(@"D:\a\b\NODE_MODULES", "NODE_MODULES"));
        Assert.Null(r.Motivo(@"D:\Projetos\node_modules_velho", "node_modules_velho"));
    }

    [Fact]
    public void Caminho_vale_so_para_aquela_pasta()
    {
        var r = RegrasExclusao.De([@"D:\Backup\Veeam\"]);
        Assert.Equal(@"D:\Backup\Veeam", r.Motivo(@"d:\backup\veeam", "veeam"));
        Assert.Null(r.Motivo(@"E:\Backup\Veeam", "Veeam"));
        Assert.Null(r.Motivo(@"D:\Backup\Veeam2", "Veeam2"));
    }

    [Theory]
    [InlineData("", "Escreva o nome da pasta ou o caminho completo.")]
    [InlineData("*.tmp", "Use o nome da pasta ou o caminho completo, sem * nem ?")]
    [InlineData(@"C:\", "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.")]
    [InlineData(@"\\servidor\dados", "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.")]
    [InlineData(@"Projetos\velho", "Use só o nome da pasta, como node_modules, ou o caminho completo, como D:\\Backup.")]
    public void Regra_invalida_diz_o_motivo(string texto, string erro)
    {
        Assert.Null(RegrasExclusao.Validar(texto, out var motivo));
        Assert.Equal(erro, motivo);
    }

    [Fact]
    public void Repetida_e_invalida_ficam_de_fora_e_o_teto_e_100()
    {
        var r = RegrasExclusao.De(["node_modules", "NODE_MODULES", "*.tmp", ".git"]);
        Assert.Equal(["node_modules", ".git"], r.Regras);
        Assert.Equal(100, RegrasExclusao.De(Enumerable.Range(0, 150).Select(i => $"pasta{i}")).Regras.Count);
    }

    [Fact]
    public void Arquivo_grava_e_le_uma_regra_por_linha()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "exclusoes-" + Guid.NewGuid().ToString("N"), "excluidas.txt");
        var armazem = new ArquivoExclusoes(arquivo);
        Assert.Empty(armazem.Ler().Regras);
        armazem.Gravar(RegrasExclusao.De(["node_modules", @"D:\Backup"]));
        Assert.Equal(["node_modules", @"D:\Backup"], armazem.Ler().Regras);
        Directory.Delete(Path.GetDirectoryName(arquivo)!, true);
    }

    [Fact]
    public async Task Varredura_nao_le_a_pasta_excluida_e_conta_ela()
    {
        using var p = new PastaTeste();
        p.Arquivo(@"site\index.html", 1000);
        p.Arquivo(@"site\node_modules\pacote\a.js", 50_000);
        p.Arquivo(@"backup\velho.zip", 70_000);
        var motor = new MotorVarredura { Exclusoes = RegrasExclusao.De(["node_modules", p.Caminho("backup")]) };

        var raiz = (await motor.Iniciar(p.Raiz, CancellationToken.None).Conclusao).Raiz;

        var site = raiz.Subpastas.Single(s => s.Nome == "site");
        var modulos = site.Subpastas.Single();
        Assert.Equal(EstadoPasta.Excluida, modulos.Estado);
        Assert.Equal("node_modules", modulos.Motivo);
        Assert.Empty(modulos.Subpastas);
        Assert.Equal(1000, site.Tamanho);
        Assert.Equal(EstadoPasta.Excluida, raiz.Subpastas.Single(s => s.Nome == "backup").Estado);
        Assert.Equal(2, raiz.PastasExcluidas);
        Assert.Equal(0, raiz.PastasSemAcesso + raiz.PastasComErro);
    }

    [Fact]
    public async Task Alvo_nunca_e_excluido_e_atualizar_le_a_pasta_excluida()
    {
        using var p = new PastaTeste();
        p.Arquivo(@"node_modules\a.js", 5000);
        var motor = new MotorVarredura { Exclusoes = RegrasExclusao.De(["node_modules"]) };

        var direto = (await motor.Iniciar(p.Caminho("node_modules"), CancellationToken.None).Conclusao).Raiz;
        Assert.Equal(EstadoPasta.Lida, direto.Estado);
        Assert.Equal(5000, direto.Tamanho);

        var raiz = (await motor.Iniciar(p.Raiz, CancellationToken.None).Conclusao).Raiz;
        var excluida = raiz.Subpastas.Single();
        var relida = (await motor.Reler(excluida, CancellationToken.None).Conclusao).Raiz;
        Assert.Equal(EstadoPasta.Lida, relida.Estado);
        Assert.Equal(5000, raiz.Tamanho);
        Assert.Equal(0, raiz.PastasExcluidas);
    }

    [Fact]
    public void Painel_passa_as_regras_ao_motor_e_grava()
    {
        var motor = new MotorVarredura();
        var armazem = new ExclusoesEmMemoria();
        armazem.Gravar(RegrasExclusao.De(["node_modules"]));
        var painel = new PainelPrincipal(new DependenciasPainel { Motor = motor, ListarUnidades = () => [], Exclusoes = armazem });
        Assert.Equal(["node_modules"], motor.Exclusoes.Regras);

        painel.GravarExclusoes(RegrasExclusao.De([".git"]));
        Assert.Equal([".git"], armazem.Ler().Regras);
        Assert.Equal([".git"], motor.Exclusoes.Regras);
        Assert.Equal([".git"], painel.Exclusoes.Regras);
    }

    [Fact]
    public void Linha_grafico_e_analises_tratam_a_pasta_excluida()
    {
        var raiz = ArvoreComExcluida();

        var linha = new LinhaArvore(TipoLinha.Pasta, raiz.Subpastas[1]);
        Assert.True(linha.SemValor);
        Assert.Equal("excluída da varredura (regra: node_modules)", linha.Rotulo);

        var grafico = ItensGrafico.DaPasta(raiz, ModoExibicao.Tamanho, 20);
        Assert.Equal(["node_modules"], grafico.Excluidas);
        Assert.Empty(grafico.NaoLidas);
        Assert.Equal(1, raiz.PastasExcluidas);
        Assert.Equal(1000, raiz.Tamanho);
    }

    [Fact]
    public async Task Analises_avisam_e_demonstracao_mostra_a_excluida()
    {
        var analises = new PainelAnalises();
        await analises.CalcularAsync(ArvoreComExcluida(), new DateTime(2026, 10, 1));
        Assert.Equal("1 pasta excluída da varredura não entra nesta conta", analises.Aviso);

        var demo = (await Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao).Raiz;
        Assert.Equal(1, demo.PastasExcluidas);
        Assert.Equal(["$Recycle.Bin"], Demonstracao.Painel().Exclusoes.Regras);
    }

    private static NoPasta ArvoreComExcluida()
    {
        var raiz = new NoPasta(@"D:\", null);
        var dados = new NoPasta("Dados", raiz);
        var modulos = new NoPasta("node_modules", raiz);
        raiz.Preencher([], [dados, modulos]);
        dados.Preencher([new ArquivoInfo("a.txt", 1000, 4096, new DateTime(2026, 1, 1), MarcaArquivo.Nenhuma)], []);
        modulos.MarcarExcluida("node_modules");
        return raiz;
    }

    [Fact]
    public void Csv_e_relatorios_citam_a_pasta_excluida()
    {
        var raiz = new NoPasta(@"D:\", null);
        var modulos = new NoPasta("node_modules", raiz);
        raiz.Preencher([], [modulos]);
        modulos.MarcarExcluida("node_modules");

        var csv = new StringWriter();
        ExportadorCsv.Gravar(raiz, csv);
        Assert.Contains(@"D:\node_modules;excluída;", csv.ToString());

        var html = System.Net.WebUtility.HtmlDecode(RelatorioTecnico.Gerar(new DadosRelatorio(raiz, DateTime.Now, "PC", null, false, 10, null)));
        Assert.Contains("1 pasta excluída da varredura não entra nesta conta", html);
        Assert.Contains("excluída da varredura (regra: node_modules)", html);
    }
}
