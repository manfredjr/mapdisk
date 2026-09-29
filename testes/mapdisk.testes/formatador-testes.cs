namespace MapDisk.Testes;

public class FormatadorTestes
{
    [Theory]
    [InlineData(0L, "0 Bytes")]
    [InlineData(1L, "1 Byte")]
    [InlineData(1023L, "1.023 Bytes")]
    [InlineData(1024L, "1,0 KB")]
    [InlineData(307_125_862L, "292,9 MB")]
    [InlineData(56_585_533_849L, "52,7 GB")]
    [InlineData(2_199_023_255_552L, "2,0 TB")]
    public void Tamanho_automatico_escolhe_a_unidade(long bytes, string esperado)
    {
        Assert.Equal(esperado, Formatador.Tamanho(bytes));
    }

    [Fact]
    public void Tamanho_em_unidade_fixa()
    {
        Assert.Equal("0,5 GB", Formatador.Tamanho(512L * 1024 * 1024, UnidadeExibicao.GB));
        Assert.Equal("1.536,0 MB", Formatador.Tamanho(1536L * 1024 * 1024, UnidadeExibicao.MB));
        Assert.Equal("4,0 KB", Formatador.Tamanho(4096, UnidadeExibicao.KB));
    }

    [Fact]
    public void Porcentagem_numero_data_duracao_e_plural()
    {
        Assert.Equal("84,1 %", Formatador.Porcentagem(0.841));
        Assert.Equal("100,0 %", Formatador.Porcentagem(1));
        Assert.Equal("1.158.005", Formatador.Numero(1_158_005));
        Assert.Equal("25/09/2026", Formatador.Data(new DateTime(2026, 9, 25, 14, 30, 0)));
        Assert.Equal("", Formatador.Data(DateTime.MinValue));
        Assert.Equal("12,4 s", Formatador.Duracao(TimeSpan.FromSeconds(12.4)));
        Assert.Equal("2 min 5 s", Formatador.Duracao(TimeSpan.FromSeconds(125)));
        Assert.Equal("1 pasta", Formatador.Plural(1, "pasta", "pastas"));
        Assert.Equal("1.200 pastas", Formatador.Plural(1200, "pasta", "pastas"));
        Assert.Equal("0 pastas", Formatador.Plural(0, "pasta", "pastas"));
    }

    [Theory]
    [InlineData(1_073_741_800L, "1,0 GB")]
    [InlineData(1_048_570L, "1,0 MB")]
    [InlineData(1_048_575L, "1,0 MB")]
    [InlineData(1_048_000L, "1.023,4 KB")]
    public void Valor_que_arredonda_para_1024_sobe_de_unidade(long bytes, string esperado)
    {
        Assert.Equal(esperado, Formatador.Tamanho(bytes));
    }
}
