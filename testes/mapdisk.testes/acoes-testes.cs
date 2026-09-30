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
}
