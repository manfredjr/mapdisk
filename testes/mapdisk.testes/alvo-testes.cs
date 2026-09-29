namespace MapDisk.Testes;

public class AlvoTestes
{
    [Theory]
    [InlineData("c:", @"C:\")]
    [InlineData(@" ""D:\Dados\"" ", @"D:\Dados")]
    [InlineData("D:/Dados/Fotos/", @"D:\Dados\Fotos")]
    [InlineData(@"\\srv\dados", @"\\srv\dados\")]
    [InlineData(@"\\srv\dados\pasta\", @"\\srv\dados\pasta")]
    [InlineData(@"\\?\C:\Dados", @"C:\Dados")]
    [InlineData(@"\\?\UNC\srv\dados\x", @"\\srv\dados\x")]
    public void Normaliza_o_alvo(string texto, string esperado)
    {
        Assert.Equal(esperado, Alvo.Normalizar(texto, out var erro));
        Assert.Null(erro);
    }

    [Theory]
    [InlineData("", "Informe")]
    [InlineData("   ", "Informe")]
    [InlineData(@"\\srv", "incompleto")]
    [InlineData("Dados", "caminho completo")]
    [InlineData("C:Dados", "caminho completo")]
    public void Recusa_alvo_invalido_explicando(string texto, string trecho)
    {
        Assert.Null(Alvo.Normalizar(texto, out var erro));
        Assert.Contains(trecho, erro);
    }

    [Fact]
    public void Prefixo_longo_e_juncao_de_caminhos()
    {
        Assert.Equal(@"\\?\C:\a", Alvo.Longo(@"C:\a"));
        Assert.Equal(@"\\?\UNC\srv\d\", Alvo.Longo(@"\\srv\d\"));
        Assert.Equal(@"\\?\C:\a", Alvo.Longo(@"\\?\C:\a"));
        Assert.Equal(@"C:\Users", Alvo.Juntar(@"C:\", "Users"));
        Assert.Equal(@"C:\Users\Ana", Alvo.Juntar(@"C:\Users", "Ana"));
        Assert.True(Alvo.EhRede(@"\\srv\d\"));
        Assert.False(Alvo.EhRede(@"C:\"));
    }
}
