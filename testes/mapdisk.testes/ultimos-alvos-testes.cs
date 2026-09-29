namespace MapDisk.Testes;

public class UltimosAlvosTestes
{
    [Fact]
    public void Mais_recente_primeiro_sem_repetir_e_com_limite()
    {
        IReadOnlyList<string> lista = [];
        for (var i = 0; i < 12; i++)
        {
            lista = UltimosAlvos.Acrescentar(lista, $@"D:\p{i}");
        }

        lista = UltimosAlvos.Acrescentar(lista, @"d:\P5");

        Assert.Equal(10, lista.Count);
        Assert.Equal(@"d:\P5", lista[0]);
        Assert.Equal(@"D:\p11", lista[1]);
        Assert.DoesNotContain(@"D:\p5", lista);
    }

    [Fact]
    public void Arquivo_grava_e_le_de_volta()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"alvos-{Guid.NewGuid():N}.txt");
        try
        {
            var historico = new HistoricoAlvosArquivo(arquivo);
            Assert.Empty(historico.Ler());

            historico.Gravar([@"C:\", @"\srv\dados\"]);

            Assert.Equal([@"C:\", @"\srv\dados\"], new HistoricoAlvosArquivo(arquivo).Ler());
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
