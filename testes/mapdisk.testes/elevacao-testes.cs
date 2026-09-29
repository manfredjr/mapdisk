namespace MapDisk.Testes;

public class ElevacaoTestes
{
    [Theory]
    [InlineData(@"C:\", @"--elevado ""C:\\""")]
    [InlineData(@"D:\Dados", @"--elevado ""D:\Dados""")]
    [InlineData(@"\srv\dados\", @"--elevado ""\srv\dados\\""")]
    public void Argumentos_protegem_a_barra_final(string alvo, string esperado)
    {
        Assert.Equal(esperado, Elevacao.Argumentos(alvo));
    }

    [Fact]
    public void Reconhece_o_pedido_de_elevacao()
    {
        Assert.True(Elevacao.EhPedido(["--elevado", @"C:\"], out var alvo));
        Assert.Equal(@"C:\", alvo);
        Assert.False(Elevacao.EhPedido(["varrer", @"C:\"], out _));
        Assert.False(Elevacao.EhPedido([], out _));
    }
}
