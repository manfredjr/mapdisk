namespace MapDisk.Testes;

public class ArgumentosTestes
{
    private static ArgumentosCli A(params string[] args) => ArgumentosCli.Interpretar(args);

    [Fact]
    public void Sem_argumentos_abre_a_janela()
    {
        var a = A();
        Assert.Equal(ComandoCli.Janela, a.Comando);
        Assert.True(a.Valido);
    }

    [Theory]
    [InlineData("--ajuda", ComandoCli.Ajuda)]
    [InlineData("/?", ComandoCli.Ajuda)]
    [InlineData("--versao", ComandoCli.Versao)]
    public void Ajuda_e_versao(string arg, ComandoCli esperado)
    {
        Assert.Equal(esperado, A(arg).Comando);
    }

    [Fact]
    public void Varrer_com_alvo_csv_e_top()
    {
        var a = A("varrer", "c:", "--csv", "saida.csv", "--top", "30");

        Assert.True(a.Valido, string.Join("; ", a.Erros));
        Assert.Equal(ComandoCli.Varrer, a.Comando);
        Assert.Equal(@"C:\", a.Caminho);
        Assert.Equal("saida.csv", a.Csv);
        Assert.Equal(30, a.Top);
    }

    [Fact]
    public void Varrer_caminho_de_rede()
    {
        Assert.Equal(@"\\srv\dados\", A("varrer", @"\\srv\dados").Caminho);
    }

    [Theory]
    [InlineData(new[] { "varrer" }, "Falta o alvo")]
    [InlineData(new[] { "varrer", "c:", "d:" }, "um alvo por vez")]
    [InlineData(new[] { "varrer", "Dados" }, "caminho completo")]
    [InlineData(new[] { "varrer", "c:", "--top", "0" }, "de 1 a 1000")]
    [InlineData(new[] { "varrer", "c:", "--top", "abc" }, "de 1 a 1000")]
    [InlineData(new[] { "varrer", "c:", "--csv" }, "Falta o valor de --csv")]
    [InlineData(new[] { "varrer", "c:", "--csv", "saida.txt" }, "terminar em .csv")]
    [InlineData(new[] { "--csv", "x.csv" }, "pedem o comando varrer")]
    [InlineData(new[] { "varrer", "c:", "--apagar" }, "Opção desconhecida: --apagar")]
    [InlineData(new[] { "apagar", "c:" }, "Comando desconhecido: apagar")]
    [InlineData(new[] { "varrer", "c:", "--ajuda" }, "só um comando")]
    public void Recusa_o_que_nao_existe_explicando(string[] args, string trecho)
    {
        var a = A(args);
        Assert.False(a.Valido);
        Assert.Contains(a.Erros, e => e.Contains(trecho, StringComparison.Ordinal));
    }

    [Fact]
    public void A_ajuda_diz_que_a_linha_de_comando_so_le()
    {
        Assert.Contains("nunca apaga nem move", ArgumentosCli.TextoAjuda);
    }
}
