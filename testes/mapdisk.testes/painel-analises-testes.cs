namespace MapDisk.Testes;

public class PainelAnalisesTestes
{
    [Fact]
    public async Task Calcula_as_quatro_abas_com_textos_em_portugues()
    {
        var p = new PainelAnalises();

        await p.CalcularAsync(AnalisesTestes.Exemplo(), AnalisesTestes.Hoje);

        Assert.Equal(@"Análise de C:\", p.Titulo);
        Assert.Equal("video.mp4", p.Maiores[0].Nome);
        Assert.Equal(@"C:\", p.Maiores[0].Pasta);
        Assert.Equal("4,9 KB", p.Maiores[0].TextoTamanho);
        Assert.Equal("2 arquivos sem alteração há mais de 1 ano, somando 6,8 KB", p.TextoAntigos);
        Assert.Equal("Vídeo", p.Categorias[0].Nome);
        Assert.Equal("1 arquivo", p.Categorias[0].TextoQuantidade);
        Assert.Equal(["bruno", "ana"], p.Usuarios.Select(u => u.Nome));
        Assert.Equal("1 pasta sem leitura não entra nesta conta", p.Aviso);
    }

    [Fact]
    public async Task Trocar_a_idade_recalcula_os_antigos()
    {
        var p = new PainelAnalises();
        await p.CalcularAsync(AnalisesTestes.Exemplo(), AnalisesTestes.Hoje);

        await p.DefinirIdadeAsync(Idade.DoisAnos, AnalisesTestes.Hoje);

        Assert.Equal("1 arquivo sem alteração há mais de 2 anos, somando 4,9 KB", p.TextoAntigos);
    }

    [Fact]
    public async Task Sem_pasta_de_perfis_explica()
    {
        var p = new PainelAnalises();
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];

        await p.CalcularAsync(bruno, AnalisesTestes.Hoje);

        Assert.Empty(p.Usuarios);
        Assert.Contains("pasta de perfis", p.TextoUsuarios);
    }

    [Fact]
    public void Durante_a_varredura_pede_para_aguardar()
    {
        var p = new PainelAnalises();

        p.Aguardar();

        Assert.Equal("Aguarde o fim da varredura.", p.Aviso);
        Assert.Empty(p.Maiores);
    }

    [Fact]
    public void Pasta_das_analises_e_a_selecionada_ou_a_raiz()
    {
        var p = Demonstracao.Painel();
        p.Varrer();
        p.Tique();
        var users = p.Arvore.Linhas.Single(l => l.Nome == "Users");

        Assert.Same(users.Pasta, p.PastaDasAnalises(users));
        Assert.Same(p.Arvore.Raiz, p.PastaDasAnalises(null));
    }
}
