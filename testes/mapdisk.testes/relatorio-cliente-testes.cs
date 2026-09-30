namespace MapDisk.Testes;

public class RelatorioClienteTestes
{
    internal static ArvoreVisivel ArvoreAberta()
    {
        var arvore = new ArvoreVisivel();
        arvore.Carregar(AnalisesTestes.Exemplo());
        arvore.AbrirNiveis(3);
        return arvore;
    }

    internal static LinhaArvore LinhaDe(ArvoreVisivel arvore, string nome) => arvore.Linhas.First(l => l.Nome == nome);

    [Fact]
    public void Itens_da_selecao_convertem_linhas_e_apontam_o_grupo()
    {
        var arvore = ArvoreAberta();

        var (itens, grupo) = PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "bruno"), LinhaDe(arvore, "Users")]);

        Assert.Equal([@"C:\Users\bruno", @"C:\Users"], itens.Select(i => i.Caminho));
        Assert.Null(grupo);
        Assert.Equal("[1 arquivo]", PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "[1 arquivo]")]).Grupo);
    }

    [Fact]
    public void Painel_avalia_itens_vindos_da_resposta()
    {
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Locais = AcoesTestes.Protegidos with { PastaRegistro = @"E:\registro" },
            TipoDaUnidade = _ => DriveType.Fixed,
        });
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];

        painel.AvaliarItens([ItemAcao.DaPasta(bruno)]);

        Assert.True(painel.PodeRemover);
        Assert.Equal(TipoAcao.Lixeira, painel.Selecao.Remocao);
    }
}
