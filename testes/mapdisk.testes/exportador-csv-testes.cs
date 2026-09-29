namespace MapDisk.Testes;

public class ExportadorCsvTestes
{
    private static readonly DateTime _d = new(2026, 9, 25, 14, 30, 0);

    private static string[] Linhas()
    {
        var raiz = new NoPasta(@"C:\", null);
        var dados = new NoPasta("Dados", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([new("a.bin", 100, 4096, _d, MarcaArquivo.Nenhuma)], [dados, windows]);
        var estranha = new NoPasta("x;y", dados);
        dados.Preencher([new("b.bin", 300, 4096, _d, MarcaArquivo.Nenhuma)], [estranha]);
        estranha.Preencher([new("c.bin", 50, 4096, _d, MarcaArquivo.Nenhuma)], []);
        windows.MarcarSemAcesso("acesso negado");

        var texto = new StringWriter();
        ExportadorCsv.Gravar(raiz, texto);
        return texto.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void Uma_linha_por_pasta_em_pre_ordem()
    {
        var linhas = Linhas();

        Assert.Equal(ExportadorCsv.Cabecalho, linhas[0]);
        Assert.Equal(@"C:\;lida;450;12288;3;3;100,0;25/09/2026 14:30", linhas[1]);
        Assert.Equal(@"C:\Dados;lida;350;8192;2;1;77,8;25/09/2026 14:30", linhas[2]);
        Assert.Equal(@"""C:\Dados\x;y"";lida;50;4096;1;0;14,3;25/09/2026 14:30", linhas[3]);
        Assert.Equal(5, linhas.Length);
    }

    [Fact]
    public void Pasta_sem_acesso_sai_sem_numeros_e_nunca_com_zero()
    {
        Assert.Equal(@"C:\Windows;sem acesso;;;;;;", Linhas()[4]);
    }

    [Fact]
    public void Arquivo_gravado_em_utf8_com_bom_para_o_excel()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"teste-{Guid.NewGuid():N}.csv");
        try
        {
            ExportadorCsv.Gravar(new NoPasta(@"C:\", null), arquivo);
            var bytes = File.ReadAllBytes(arquivo);
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
