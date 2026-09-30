namespace MapDisk.Testes;

public class AnalisesTestes
{
    internal static readonly DateTime Hoje = new(2026, 9, 29);

    /// <summary>
    /// C:\ com video.mp4 (5000, 2023), Users\ana\foto.jpg (300, 2026), Users\ana\copia.jpg
    /// (hard link repetido de 300), Users\bruno\caixa.pst (2000, 2025), Users\bruno\setup.exe
    /// (100, sem data) e Windows sem acesso.
    /// </summary>
    internal static NoPasta Exemplo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([new("video.mp4", 5000, 8192, new DateTime(2023, 1, 10), MarcaArquivo.Nenhuma)], [users, windows]);
        windows.MarcarSemAcesso("acesso negado");
        var ana = new NoPasta("ana", users);
        var bruno = new NoPasta("bruno", users);
        users.Preencher([], [ana, bruno]);
        ana.Preencher(
            [
                new("foto.jpg", 300, 4096, new DateTime(2026, 9, 1), MarcaArquivo.Nenhuma),
                new("copia.jpg", 300, 4096, new DateTime(2026, 9, 1), MarcaArquivo.LinkRepetido),
            ],
            []);
        bruno.Preencher(
            [
                new("caixa.pst", 2000, 4096, new DateTime(2025, 6, 1), MarcaArquivo.Nenhuma),
                new("setup.exe", 100, 4096, DateTime.MinValue, MarcaArquivo.Nenhuma),
            ],
            []);
        return raiz;
    }

    [Fact]
    public void Maiores_arquivos_do_maior_para_o_menor_sem_hard_link_repetido()
    {
        var maiores = Analises.MaioresArquivos(Exemplo(), 3);

        Assert.Equal(["video.mp4", "caixa.pst", "foto.jpg"], maiores.Select(m => m.Arquivo.Nome));
        Assert.Equal(@"C:\Users\bruno\caixa.pst", maiores[1].Caminho);
    }

    [Fact]
    public void Maiores_arquivos_de_uma_subpasta()
    {
        var bruno = Exemplo().Subpastas[0].Subpastas[1];

        Assert.Equal(["caixa.pst", "setup.exe"], Analises.MaioresArquivos(bruno).Select(m => m.Arquivo.Nome));
    }

    [Theory]
    [InlineData(Idade.SeisMeses, 2026, 3, 29)]
    [InlineData(Idade.UmAno, 2025, 9, 29)]
    [InlineData(Idade.DoisAnos, 2024, 9, 29)]
    [InlineData(Idade.CincoAnos, 2021, 9, 29)]
    public void Limite_de_cada_idade(Idade idade, int ano, int mes, int dia)
    {
        Assert.Equal(new DateTime(ano, mes, dia), Analises.Limite(idade, Hoje));
    }

    [Fact]
    public void Antigos_somam_quantidade_e_tamanho_e_ignoram_arquivo_sem_data()
    {
        var um = Analises.ArquivosAntigos(Exemplo(), Idade.UmAno, Hoje);
        var dois = Analises.ArquivosAntigos(Exemplo(), Idade.DoisAnos, Hoje);

        Assert.Equal(2, um.Quantidade);
        Assert.Equal(7000, um.Tamanho);
        Assert.Equal(["video.mp4", "caixa.pst"], um.Maiores.Select(m => m.Arquivo.Nome));
        Assert.Equal(1, dois.Quantidade);
        Assert.Equal(5000, dois.Tamanho);
    }

    [Fact]
    public void Por_categoria_do_maior_para_o_menor()
    {
        var tipos = Analises.PorCategoria(Exemplo());

        Assert.Equal(["Vídeo", "E-mail (.pst, .ost)", "Imagem", "Instalador"], tipos.Select(t => t.Nome));
        Assert.Equal(5000, tipos[0].Tamanho);
        Assert.Equal(1, tipos[2].Quantidade);
    }

    [Fact]
    public void Por_extensao()
    {
        var extensoes = Analises.PorExtensao(Exemplo());

        Assert.Equal([".mp4", ".pst", ".jpg", ".exe"], extensoes.Select(e => e.Nome));
    }

    [Fact]
    public void Por_usuario_acha_a_pasta_users_na_raiz_da_unidade()
    {
        var r = Analises.PorUsuario(Exemplo());

        Assert.Equal(@"C:\Users", r.PastaDePerfis!.CaminhoCompleto());
        Assert.Equal(["bruno", "ana"], r.Perfis.Select(p => p.Nome));
    }

    [Fact]
    public void Por_usuario_na_propria_pasta_users()
    {
        var users = Exemplo().Subpastas[0];

        Assert.Same(users, Analises.PorUsuario(users).PastaDePerfis);
    }

    [Fact]
    public void Por_usuario_sem_pasta_de_perfis()
    {
        var bruno = Exemplo().Subpastas[0].Subpastas[1];

        var r = Analises.PorUsuario(bruno);

        Assert.Null(r.PastaDePerfis);
        Assert.Empty(r.Perfis);
    }

    [Fact]
    public void Perfil_sem_acesso_entra_no_fim_e_nao_como_zero()
    {
        var raiz = new NoPasta(@"D:\", null);
        var users = new NoPasta("Users", raiz);
        raiz.Preencher([], [users]);
        var ana = new NoPasta("ana", users);
        var fechado = new NoPasta("fechado", users);
        users.Preencher([], [fechado, ana]);
        ana.Preencher([new("a.bin", 10, 4096, Hoje, MarcaArquivo.Nenhuma)], []);
        fechado.MarcarSemAcesso("acesso negado");

        var perfis = Analises.PorUsuario(raiz).Perfis;

        Assert.Equal(["ana", "fechado"], perfis.Select(p => p.Nome));
        Assert.Equal(EstadoPasta.SemAcesso, perfis[1].Estado);
    }
}
