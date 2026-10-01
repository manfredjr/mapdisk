using System.Globalization;
using System.Text;
using static MapDisk.Nucleo.Html;

namespace MapDisk.Nucleo;

public sealed record DadosRelatorio(NoPasta Pasta, DateTime Gerado, string Computador, InfoVolume? Volume, bool Interrompida, int Quantos, ResultadoDuplicados? Duplicados);

/// <summary>
/// Relatório do técnico (R17): um arquivo HTML só, com logo, estilo e gráfico dentro, sem nada
/// da internet (regra 6). Pasta sem leitura aparece com o motivo, nunca com zero (regra 3).
/// </summary>
public static class RelatorioTecnico
{
    private const int LarguraGrafico = 960;
    private const int AlturaGrafico = 360;
    private const int MaximoNoGrafico = 20;

    private const string Estilo =
        "body{font-family:Segoe UI,Arial,sans-serif;color:#202020;background:#F4F4F4;margin:0}" +
        "header{display:flex;gap:16px;align-items:center;background:#006B2D;color:#fff;padding:12px 20px}" +
        "header img{height:56px;background:#fff;border-radius:8px;padding:4px}header h1{margin:0;font-size:22px}header p{margin:4px 0 0}" +
        "main{max-width:1100px;margin:0 auto;padding:16px 20px}h2{color:#006B2D;border-left:4px solid #43A92C;padding-left:8px}" +
        ".aviso{background:#fff4d6;border:1px solid #e0b100;padding:8px 12px}" +
        "table{width:100%;border-collapse:collapse;background:#fff}th,td{border:1px solid #ddd;padding:6px;font-size:13px;vertical-align:top}" +
        "th{background:#eef6ea;text-align:left}td.n{text-align:right;white-space:nowrap}svg{max-width:100%;height:auto;background:#fff}" +
        ".cor{display:inline-block;width:12px;height:12px;margin-right:6px;vertical-align:middle}" +
        "footer{margin-top:24px;font-size:12px;color:#666}" +
        "@media print{body{background:#fff}header{background:#fff;color:#006B2D;border-bottom:2px solid #006B2D}tr{page-break-inside:avoid}svg{page-break-inside:avoid}}";

    /// <summary>Nome sugerido, sem extensão: "espaco-" mais a pasta e a data. A raiz "C:\" vira "c".</summary>
    public static string NomeDoArquivo(NoPasta pasta, DateTime agora)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var nome = new string(pasta.Nome.Where(c => !invalidos.Contains(c) && c != ':').ToArray()).Trim().ToLowerInvariant();
        return $"espaco-{(nome.Length > 0 ? nome : "pasta")}-{agora.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}";
    }

    public static void Gravar(DadosRelatorio dados, string arquivo) =>
        File.WriteAllText(arquivo, Gerar(dados), new UTF8Encoding(false));

    public static string Gerar(DadosRelatorio d)
    {
        var p = d.Pasta;
        var caminho = p.CaminhoCompleto();
        var s = new StringBuilder();
        s.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        s.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.Append($"<title>Espaço em disco - {H(caminho)}</title><style>{Estilo}</style></head><body>");
        s.Append($"<header><img alt=\"MT - Manfred Tecnologia\" src=\"data:image/png;base64,{Logo()}\"><div><h1>Relatório de espaço em disco</h1>");
        s.Append($"<p>{H(d.Computador)} | {H(caminho)} | {d.Gerado.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}</p></div></header><main>");
        if (d.Interrompida)
        {
            s.Append("<p class=\"aviso\">A varredura foi interrompida antes do fim. Os números mostram só o que foi lido até ali.</p>");
        }

        Resumo(s, d);
        Grafico(s, p);
        MaioresPastas(s, p, d.Quantos);
        Arquivos(s, "Maiores arquivos", Analises.MaioresArquivos(p, d.Quantos));
        Tipos(s, p);
        var antigos = Analises.ArquivosAntigos(p, Idade.DoisAnos, d.Gerado, d.Quantos);
        s.Append($"<h2>Sem alteração há mais de 2 anos</h2><p>{H(Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos"))}, {H(Formatador.Tamanho(antigos.Tamanho))}.</p>");
        Arquivos(s, null, antigos.Maiores);
        Usuarios(s, p);
        if (d.Duplicados is { } dup)
        {
            Duplicados(s, dup, d.Quantos);
        }

        s.Append($"<footer>Gerado pelo MapDisk - MT {H(ExecutorCli.Versao)}, da MT - Manfred Tecnologia (<a href=\"{Sobre.SiteMt}\">www.manfred.com.br</a>).</footer>");
        s.Append("</main></body></html>");
        return s.ToString();
    }

    private static void Resumo(StringBuilder s, DadosRelatorio d)
    {
        var p = d.Pasta;
        s.Append($"<h2>Resumo</h2><p>Total lido: <b>{H(Formatador.Tamanho(p.Tamanho))}</b> ({H(Formatador.Tamanho(p.Alocado))} alocados) ");
        s.Append($"em {H(Formatador.Plural(p.ArquivosTotal, "arquivo", "arquivos"))} e {H(Formatador.Plural(p.PastasTotal, "pasta", "pastas"))}.");
        if (d.Volume is { } v)
        {
            s.Append($" Unidade {H(v.Raiz)}: {H(Formatador.Tamanho(v.Livre))} livres de {H(Formatador.Tamanho(v.Total))} ({H(v.SistemaArquivos)}).");
        }

        s.Append("</p>");
        var semLeitura = p.PastasSemAcesso + p.PastasComErro;
        if (semLeitura > 0)
        {
            s.Append($"<p class=\"aviso\">{H(Formatador.Plural(semLeitura, "pasta não pôde ser lida e não entra nesta conta", "pastas não puderam ser lidas e não entram nesta conta"))}. ");
            s.Append("O botão \"Varrer como administrador\" lê as pastas locais que ficaram sem acesso.</p>");
        }
    }

    private static void Grafico(StringBuilder s, NoPasta p)
    {
        var c = ItensGrafico.DaPasta(p, ModoExibicao.Tamanho, MaximoNoGrafico);
        if (c.Itens.Count == 0)
        {
            return;
        }

        var blocos = Treemap.Dispor(c.Itens.Select(i => i.Valor).ToList(), LarguraGrafico, AlturaGrafico);
        var cores = Paleta.CoresDe(c.Itens);
        s.Append($"<h2>Gráfico</h2><svg viewBox=\"0 0 {LarguraGrafico} {AlturaGrafico}\" role=\"img\" aria-label=\"Gráfico em blocos\">");
        for (var i = 0; i < blocos.Count; i++)
        {
            var b = blocos[i];
            s.Append(FormattableString.Invariant($"<rect x=\"{b.X:0.#}\" y=\"{b.Y:0.#}\" width=\"{b.Largura:0.#}\" height=\"{b.Altura:0.#}\" fill=\"{cores[i]}\" stroke=\"#fff\"><title>{H(c.Itens[i].Nome)}</title></rect>"));
            if (b.Largura >= 90 && b.Altura >= 28)
            {
                s.Append(FormattableString.Invariant($"<text x=\"{b.X + 6:0.#}\" y=\"{b.Y + 18:0.#}\" fill=\"{Paleta.CorTexto(cores[i])}\" font-size=\"13\">{H(c.Itens[i].Nome)}</text>"));
            }
        }

        s.Append("</svg><table><thead><tr><th>Item</th><th>Tamanho</th><th>% da pasta</th></tr></thead><tbody>");
        for (var i = 0; i < c.Itens.Count; i++)
        {
            var item = c.Itens[i];
            s.Append($"<tr><td><span class=\"cor\" style=\"background:{cores[i]}\"></span>{H(item.Nome)}</td><td class=\"n\">{H(Formatador.Tamanho(item.Valor))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(c.Total > 0 ? (double)item.Valor / c.Total : 0))}</td></tr>");
        }

        s.Append("</tbody></table>");
        if (c.NaoLidas.Count > 0)
        {
            s.Append($"<p>Fora do gráfico, sem leitura: {H(string.Join(", ", c.NaoLidas))}.</p>");
        }
    }

    private static void MaioresPastas(StringBuilder s, NoPasta p, int quantos)
    {
        s.Append("<h2>Maiores pastas</h2><table><thead><tr><th>Pasta</th><th>Tamanho</th><th>% da pasta</th><th>Arquivos</th><th>Última alteração</th></tr></thead><tbody>");
        var lidas = p.Subpastas.Where(x => x.Estado == EstadoPasta.Lida).OrderByDescending(x => x.Tamanho).Take(quantos);
        var semLeitura = p.Subpastas.Where(x => x.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura or EstadoPasta.Pendente);
        foreach (var x in lidas)
        {
            s.Append($"<tr><td>{H(x.Nome)}</td><td class=\"n\">{H(Formatador.Tamanho(x.Tamanho))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(p.Tamanho > 0 ? (double)x.Tamanho / p.Tamanho : 0))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Numero(x.ArquivosTotal))}</td><td>{H(Formatador.Data(x.UltimaModificacao))}</td></tr>");
        }

        foreach (var x in semLeitura)
        {
            var estado = x.Estado switch
            {
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.ErroLeitura => "erro de leitura",
                _ => "não lida",
            };
            s.Append($"<tr><td>{H(x.Nome)}</td><td colspan=\"4\">{estado}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Arquivos(StringBuilder s, string? titulo, IReadOnlyList<ArquivoEncontrado> arquivos)
    {
        if (titulo is not null)
        {
            s.Append($"<h2>{H(titulo)}</h2>");
        }

        if (arquivos.Count == 0)
        {
            s.Append("<p>Nenhum.</p>");
            return;
        }

        s.Append("<table><thead><tr><th>Arquivo</th><th>Pasta</th><th>Tamanho</th><th>Última alteração</th></tr></thead><tbody>");
        foreach (var a in arquivos)
        {
            s.Append($"<tr><td>{H(a.Arquivo.Nome)}</td><td>{H(a.Pasta.CaminhoCompleto())}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Tamanho(a.Arquivo.Tamanho))}</td><td>{H(Formatador.Data(a.Arquivo.Modificacao))}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Tipos(StringBuilder s, NoPasta p)
    {
        s.Append("<h2>Por tipo</h2><table><thead><tr><th>Tipo</th><th>Arquivos</th><th>Tamanho</th><th>% da pasta</th></tr></thead><tbody>");
        foreach (var t in Analises.PorCategoria(p))
        {
            s.Append($"<tr><td>{H(t.Nome)}</td><td class=\"n\">{H(Formatador.Numero(t.Quantidade))}</td><td class=\"n\">{H(Formatador.Tamanho(t.Tamanho))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(p.Tamanho > 0 ? (double)t.Tamanho / p.Tamanho : 0))}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Usuarios(StringBuilder s, NoPasta p)
    {
        var u = Analises.PorUsuario(p);
        if (u.PastaDePerfis is null)
        {
            return;
        }

        s.Append("<h2>Por usuário</h2><table><thead><tr><th>Perfil</th><th>Tamanho</th></tr></thead><tbody>");
        foreach (var perfil in u.Perfis)
        {
            var valor = perfil.Estado == EstadoPasta.Lida ? Formatador.Tamanho(perfil.Tamanho) : "sem acesso";
            s.Append($"<tr><td>{H(perfil.Nome)}</td><td class=\"n\">{H(valor)}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Duplicados(StringBuilder s, ResultadoDuplicados r, int quantos)
    {
        s.Append("<h2>Duplicados</h2>");
        if (r.Grupos.Count == 0)
        {
            s.Append("<p>Nenhum arquivo repetido acima do tamanho mínimo da busca.</p>");
            return;
        }

        var copias = r.Grupos.Sum(g => g.Repetido);
        s.Append($"<p>{H(Formatador.Plural(r.Grupos.Count, "grupo", "grupos"))} com {H(Formatador.Tamanho(copias))} em cópias.</p>");
        s.Append("<table><thead><tr><th>Grupo</th><th>Arquivo</th><th>Tamanho</th></tr></thead><tbody>");
        foreach (var g in r.Grupos.Take(quantos))
        {
            foreach (var a in g.Arquivos)
            {
                s.Append($"<tr><td>{g.Numero}</td><td>{H(a.Caminho)}</td><td class=\"n\">{H(Formatador.Tamanho(g.Tamanho))}</td></tr>");
            }
        }

        s.Append("</tbody></table>");
    }
}
