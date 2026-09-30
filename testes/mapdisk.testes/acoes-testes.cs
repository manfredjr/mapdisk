namespace MapDisk.Testes;

public class AcoesTestes
{
    internal static readonly LocaisProtegidos Protegidos = new(
        [@"C:\Windows", @"C:\Program Files", @"C:\Program Files (x86)", @"C:\ProgramData"],
        @"C:\Users\tecnico\AppData\Local\MapDisk");

    [Theory]
    [InlineData(@"C:\", true, "É a raiz da unidade.")]
    [InlineData(@"\\servidor\dados", true, "É a raiz da unidade.")]
    [InlineData(@"C:\Windows", true, @"É pasta do sistema (C:\Windows).")]
    [InlineData(@"C:\Windows\System32\drivers", true, @"É pasta do sistema (C:\Windows).")]
    [InlineData(@"C:\ProgramData\app\log.txt", false, @"É pasta do sistema (C:\ProgramData).")]
    [InlineData(@"D:\System Volume Information", true, "É pasta do sistema (System Volume Information).")]
    [InlineData(@"D:\$Recycle.Bin\S-1-5-21\x.txt", false, "É pasta do sistema ($Recycle.Bin).")]
    [InlineData(@"C:\Users\tecnico", true, "Contém o registro de ações do MapDisk.")]
    [InlineData(@"C:\Users", true, "Contém o registro de ações do MapDisk.")]
    public void Bloqueia_o_que_e_do_sistema(string caminho, bool ehPasta, string motivo)
    {
        Assert.Equal(motivo, Protecao.Motivo(caminho, ehPasta, MarcaArquivo.Nenhuma, Protegidos));
    }

    [Theory]
    [InlineData(@"C:\WindowsAntigo", true)]
    [InlineData(@"C:\Users\ana", true)]
    [InlineData(@"D:\Dados\video.mp4", false)]
    [InlineData(@"\\servidor\dados\backup", true)]
    public void Libera_o_resto(string caminho, bool ehPasta)
    {
        Assert.Null(Protecao.Motivo(caminho, ehPasta, MarcaArquivo.Nenhuma, Protegidos));
    }

    [Fact]
    public void Bloqueia_arquivo_do_sistema_pelo_rotulo()
    {
        Assert.Equal("É arquivo do sistema.", Protecao.Motivo(@"C:\pagefile.sys", false, MarcaArquivo.Sistema, Protegidos));
    }

    [Fact]
    public void Item_de_pasta_e_de_arquivo_tem_caminho_tamanho_e_data()
    {
        var raiz = AnalisesTestes.Exemplo();
        var bruno = raiz.Subpastas[0].Subpastas[1];

        var pasta = ItemAcao.DaPasta(bruno);
        var arquivo = ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]);

        Assert.Equal(@"C:\Users\bruno", pasta.Caminho);
        Assert.True(pasta.EhPasta);
        Assert.Equal(2100, pasta.Tamanho);
        Assert.Equal("bruno", pasta.Nome);
        Assert.Equal(@"C:\Users\bruno\caixa.pst", arquivo.Caminho);
        Assert.False(arquivo.EhPasta);
        Assert.Equal(2000, arquivo.Tamanho);
        Assert.Equal(new DateTime(2025, 6, 1), arquivo.Modificacao);
        Assert.Same(bruno, arquivo.Pasta);
    }

    [Theory]
    [InlineData(@"C:\Dados", DriveType.Fixed, true)]
    [InlineData(@"E:\fotos", DriveType.Removable, false)]
    [InlineData(@"Z:\compartilhado", DriveType.Network, false)]
    [InlineData(@"\\servidor\dados\x", DriveType.Fixed, false)]
    public void Lixeira_so_em_unidade_fixa(string caminho, DriveType tipo, bool temLixeira)
    {
        Assert.Equal(temLixeira, Lixeiras.Existe(caminho, _ => tipo));
    }
}
