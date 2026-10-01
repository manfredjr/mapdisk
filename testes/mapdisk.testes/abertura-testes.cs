namespace MapDisk.Testes;

public class AberturaTestes
{
    [Theory]
    [InlineData("C:\"", @"C:\")]
    [InlineData(@"D:\Dados", @"D:\Dados")]
    [InlineData(@"\\servidor\pasta", @"\\servidor\pasta")]
    public void Corrige_a_aspa_que_o_explorer_deixa(string recebido, string esperado) =>
        Assert.Equal(esperado, Abertura.Corrigir(recebido));

    [Fact]
    public void Reconhece_o_pedido()
    {
        Assert.True(Abertura.EhPedido(["--abrir", "C:\""], out var alvo));
        Assert.Equal(@"C:\", alvo);
        Assert.False(Abertura.EhPedido(["varrer", "C:"], out _));
    }
}
