namespace MapDisk.Testes;

public class ExecutorCliTestes
{
    private static int Rodar(IMotorVarredura motor, out string saida, out string erro, params string[] args)
    {
        var s = new StringWriter();
        var e = new StringWriter();
        var codigo = ExecutorCli.Executar(ArgumentosCli.Interpretar(args), motor, s, e, CancellationToken.None);
        saida = s.ToString();
        erro = e.ToString();
        return codigo;
    }

    [Fact]
    public void Varrer_mostra_total_avisos_e_maiores_itens()
    {
        var codigo = Rodar(Demonstracao.Motor(), out var saida, out _, "varrer", "C:");

        Assert.Equal(ExecutorCli.CodigoSucesso, codigo);
        Assert.Contains("Varredura concluída", saida);
        Assert.Contains("Atenção: 1 pasta sem acesso", saida);
        Assert.Contains(@"Maiores itens em C:\", saida);
        var linhas = saida.Split(Environment.NewLine);
        Assert.Contains(linhas, l => l.Contains("Dados") && l.Contains("GB"));
        Assert.Contains(linhas, l => l.Contains("System Volume Information") && l.Contains("sem acesso"));
    }

    [Fact]
    public void Grava_o_csv_pedido()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"teste-{Guid.NewGuid():N}.csv");
        try
        {
            var codigo = Rodar(Demonstracao.Motor(), out var saida, out _, "varrer", "C:", "--csv", arquivo);

            Assert.Equal(ExecutorCli.CodigoSucesso, codigo);
            Assert.Contains("CSV gravado em", saida);
            Assert.Equal(ExportadorCsv.Cabecalho, File.ReadLines(arquivo).First());
        }
        finally
        {
            File.Delete(arquivo);
        }
    }

    [Fact]
    public void Argumento_invalido_devolve_1_com_o_motivo()
    {
        var codigo = Rodar(Demonstracao.Motor(), out _, out var erro, "varrer");

        Assert.Equal(ExecutorCli.CodigoArgumentos, codigo);
        Assert.Contains("Falta o alvo", erro);
        Assert.Contains("--ajuda", erro);
    }

    [Fact]
    public void Alvo_que_nao_pode_ser_lido_devolve_2()
    {
        var alvo = Path.Combine(AppContext.BaseDirectory, "nao-existe");

        var codigo = Rodar(new MotorVarredura(), out _, out var erro, "varrer", alvo);

        Assert.Equal(ExecutorCli.CodigoSemLeitura, codigo);
        Assert.Contains("pasta não encontrada", erro);
    }

    [Fact]
    public void Ajuda_e_versao_devolvem_0()
    {
        Assert.Equal(0, Rodar(Demonstracao.Motor(), out var ajuda, out _, "--ajuda"));
        Assert.Contains("mapdisk varrer", ajuda);
        Assert.Equal(0, Rodar(Demonstracao.Motor(), out var versao, out _, "--versao"));
        Assert.StartsWith("MapDisk - MT 0.1.0", versao);
    }
}
