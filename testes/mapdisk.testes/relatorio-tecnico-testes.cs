using System.Net;

namespace MapDisk.Testes;

public class RelatorioTecnicoTestes
{
    private static NoPasta Raiz() => Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao.Result.Raiz;

    private static DadosRelatorio Dados(NoPasta pasta, bool interrompida = false, ResultadoDuplicados? duplicados = null) =>
        new(pasta, new DateTime(2026, 10, 1, 14, 30, 0), "ESTACAO-01", new InfoVolume(@"C:\", "Sistema", "NTFS", 50L << 30, 200L << 30, 4096), interrompida, 10, duplicados);

    // O HTML codifica os acentos; os testes de texto leem o que o navegador mostra.
    private static string Texto(DadosRelatorio dados) => WebUtility.HtmlDecode(RelatorioTecnico.Gerar(dados));

    [Fact]
    public void Tem_marca_resumo_grafico_e_listas()
    {
        var html = Texto(Dados(Raiz()));
        Assert.StartsWith("<!doctype html>", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("ESTACAO-01", html);
        Assert.Contains("01/10/2026 14:30", html);
        Assert.Contains("<svg", html);
        foreach (var titulo in new[] { "Resumo", "Gráfico", "Maiores pastas", "Maiores arquivos", "Por tipo", "Sem alteração há mais de 2 anos" })
        {
            Assert.Contains($"<h2>{titulo}</h2>", html);
        }

        Assert.Contains($"{Formatador.Tamanho(50L << 30)} livres de {Formatador.Tamanho(200L << 30)}", html);
    }

    [Fact]
    public void Pasta_sem_acesso_nunca_vira_zero()
    {
        var html = Texto(Dados(Raiz()));
        Assert.Contains("System Volume Information", html);
        Assert.Contains("sem acesso", html);
        Assert.Contains("não entra nesta conta", html);
    }

    [Fact]
    public void Varredura_interrompida_avisa_no_topo()
    {
        var html = Texto(Dados(Raiz(), interrompida: true));
        Assert.Contains("A varredura foi interrompida", html);
    }

    [Fact]
    public void Nao_carrega_nada_de_fora()
    {
        var html = RelatorioTecnico.Gerar(Dados(Raiz()));
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("<link", html);
        Assert.DoesNotContain("<script src", html);
    }

    [Fact]
    public void Nome_com_caractere_de_html_e_escapado()
    {
        var raiz = new NoPasta(@"C:\", null);
        raiz.Preencher([], [new NoPasta("<b>pasta</b>", raiz)]);
        var html = RelatorioTecnico.Gerar(Dados(raiz));
        Assert.DoesNotContain("<b>pasta</b>", html);
        Assert.Contains("&lt;b&gt;pasta&lt;/b&gt;", html);
    }

    [Fact]
    public void Duplicados_entram_so_quando_houve_busca()
    {
        Assert.DoesNotContain("<h2>Duplicados</h2>", Texto(Dados(Raiz())));
        var vazio = new ResultadoDuplicados(Raiz(), [], 0, false);
        Assert.Contains("<h2>Duplicados</h2>", Texto(Dados(Raiz(), duplicados: vazio)));
    }

    [Fact]
    public void Nome_do_arquivo_vem_da_pasta_e_da_data()
    {
        var quando = new DateTime(2026, 10, 1, 9, 5, 0);
        Assert.Equal("espaco-c-20261001-0905", RelatorioTecnico.NomeDoArquivo(new NoPasta(@"C:\", null), quando));
        Assert.Equal("espaco-financeiro-20261001-0905", RelatorioTecnico.NomeDoArquivo(new NoPasta("Financeiro", null), quando));
    }

    [Fact]
    public void Grava_em_utf8()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "relatorio-" + Guid.NewGuid().ToString("N") + ".html");
        RelatorioTecnico.Gravar(Dados(Raiz()), arquivo);
        Assert.Contains("Relatório de espaço em disco", WebUtility.HtmlDecode(File.ReadAllText(arquivo)));
        File.Delete(arquivo);
    }
}
