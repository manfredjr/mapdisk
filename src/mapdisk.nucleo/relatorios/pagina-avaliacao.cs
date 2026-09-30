using System.Globalization;
using System.Net;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>
/// Página para o cliente decidir item por item. Um arquivo só: logo, estilo e código dentro,
/// sem carregar nada da internet (regra 6). "Salvar resposta" baixa um JSON; "Imprimir" dá o PDF.
/// </summary>
public static class PaginaAvaliacao
{
    public const string FormatoResposta = "mapdisk-avaliacao-resposta";

    private const string Estilo =
        "body{font-family:Segoe UI,Arial,sans-serif;color:#202020;background:#F4F4F4;margin:0}" +
        "header{display:flex;gap:16px;align-items:center;background:#006B2D;color:#fff;padding:12px 20px}" +
        "header img{height:56px;background:#fff;border-radius:8px;padding:4px}header h1{margin:0;font-size:22px}header p{margin:4px 0 0}" +
        "main{max-width:1100px;margin:0 auto;padding:16px 20px}h2{color:#006B2D;border-left:4px solid #43A92C;padding-left:8px}" +
        "table{width:100%;border-collapse:collapse;background:#fff}th,td{border:1px solid #ddd;padding:6px;vertical-align:top;font-size:13px}" +
        "th{background:#eef6ea;text-align:left}td.n{text-align:right;white-space:nowrap}.opcoes label{display:block;white-space:nowrap}" +
        "input[type=text],textarea{width:100%;box-sizing:border-box;margin:2px 0}.resumo{display:flex;gap:24px;flex-wrap:wrap}" +
        ".autorizacao{font-size:12px;color:#444}.botoes button{background:#0F8F2F;color:#fff;border:0;border-radius:16px;padding:8px 16px;font-size:14px;cursor:pointer}" +
        "footer{margin-top:24px;font-size:12px;color:#666}" +
        "@media print{body{background:#fff}.botoes{display:none}header{background:#fff;color:#006B2D;border-bottom:2px solid #006B2D}" +
        "input[type=text],textarea{border:0;border-bottom:1px solid #999}tr{page-break-inside:avoid}}";

    // Sem acento e sem caractere especial: o teste de caracteres proibidos lê este arquivo.
    private const string Codigo =
        "function salvar(){var nome=document.getElementById('decidido').value.trim();" +
        "if(!nome){alert('Escreva o seu nome em Quem decide antes de salvar.');return;}" +
        "var itens=[];document.querySelectorAll('tr.item').forEach(function(tr){" +
        "var m=tr.querySelector('input[type=radio]:checked');" +
        "itens.push({numero:Number(tr.dataset.numero),caminho:tr.dataset.caminho,bytes:tr.dataset.bytes,modificacao:tr.dataset.mod," +
        "decisao:m?m.value:'',destino:tr.querySelector('.destino').value,observacao:tr.querySelector('.obs').value});});" +
        "var r={formato:FORMATO,versao:1,relatorio:REL,pasta:PASTA,decididoPor:nome,data:new Date().toISOString()," +
        "observacao:document.getElementById('obsgeral').value,itens:itens};" +
        "var b=new Blob([JSON.stringify(r,null,1)],{type:'application/json'});var a=document.createElement('a');" +
        "a.href=URL.createObjectURL(b);a.download='avaliacao-'+REL+'-resposta.json';document.body.appendChild(a);a.click();a.remove();}";

    public static string Gerar(Avaliacao a)
    {
        var pasta = a.Pasta.CaminhoCompleto();
        var s = new StringBuilder();
        s.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        s.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.Append($"<title>Avaliação de espaço - {H(a.Cliente)}</title><style>{Estilo}</style></head><body>");
        s.Append($"<header><img alt=\"MT - Manfred Tecnologia\" src=\"data:image/png;base64,{Logo()}\"><div><h1>Avaliação de espaço</h1>");
        s.Append($"<p>{H(a.Cliente)} | {H(pasta)} | varredura de {a.Gerado.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} | relatório {H(a.Numero)}</p></div></header>");
        s.Append($"<main><p class=\"apresentacao\">{H(TextosAvaliacao.Apresentacao(pasta))}</p>");
        if (a.Mensagem.Length > 0)
        {
            s.Append($"<p class=\"mensagem\"><b>Mensagem do técnico:</b> {H(a.Mensagem)}</p>");
        }

        Resumo(s, a);
        s.Append("<h2>Itens para decidir</h2><table><thead><tr><th>Nº</th><th>Item</th><th>Tamanho</th><th>Última alteração</th>");
        s.Append("<th>Por que está aqui</th><th>Decisão</th><th>Destino (se Mover) e observação</th></tr></thead><tbody>");
        foreach (var i in a.Itens)
        {
            s.Append($"<tr class=\"item\" data-numero=\"{i.Numero}\" data-caminho=\"{H(i.Item.Caminho)}\" ");
            s.Append($"data-bytes=\"{i.Item.Tamanho.ToString(CultureInfo.InvariantCulture)}\" data-mod=\"{i.Item.Modificacao.Ticks.ToString(CultureInfo.InvariantCulture)}\">");
            s.Append($"<td>{i.Numero}</td><td><b>{H(i.Item.Nome)}</b><br><small>{H(i.Tipo)} | {H(i.Item.Caminho)}</small></td>");
            s.Append($"<td class=\"n\">{H(Formatador.Tamanho(i.Item.Tamanho))}</td><td>{H(Formatador.Data(i.Item.Modificacao))}</td><td>{H(i.Motivo)}</td><td class=\"opcoes\">");
            foreach (var (valor, rotulo) in TextosAvaliacao.Opcoes)
            {
                s.Append($"<label><input type=\"radio\" name=\"d{i.Numero}\" value=\"{valor}\"> {rotulo}</label>");
            }

            s.Append("</td><td><input class=\"destino\" type=\"text\" placeholder=\"Destino\"><input class=\"obs\" type=\"text\" placeholder=\"Observação\"></td></tr>");
        }

        s.Append("</tbody></table>");
        s.Append("<h2>Sua resposta</h2><p><label>Observação geral<br><textarea id=\"obsgeral\" rows=\"3\"></textarea></label></p>");
        s.Append($"<p><label>Quem decide (nome e cargo)<br><input id=\"decidido\" type=\"text\"></label></p><p class=\"autorizacao\">{H(TextosAvaliacao.Autorizacao)}</p>");
        s.Append("<p class=\"botoes\"><button onclick=\"salvar()\">Salvar resposta</button> <button onclick=\"window.print()\">Imprimir ou salvar em PDF</button></p>");
        s.Append($"<footer>Gerado pelo MapDisk - MT {H(ExecutorCli.Versao)}, da MT - Manfred Tecnologia (<a href=\"{Sobre.SiteMt}\">www.manfred.com.br</a>). Técnico: {H(a.Tecnico)}.</footer></main>");
        s.Append($"<script>var REL=\"{Js(a.Numero)}\",PASTA=\"{Js(pasta)}\",FORMATO=\"{FormatoResposta}\";{Codigo}</script></body></html>");
        return s.ToString();
    }

    private static void Resumo(StringBuilder s, Avaliacao a)
    {
        var p = a.Pasta;
        s.Append($"<h2>Resumo da pasta</h2><p>Total lido: <b>{H(Formatador.Tamanho(p.Tamanho))}</b> em {H(Formatador.Plural(p.ArquivosTotal, "arquivo", "arquivos"))}.");
        if (a.Livre is { } livre)
        {
            s.Append($" Espaço livre no disco: <b>{H(Formatador.Tamanho(livre))}</b>.");
        }

        var semLeitura = p.PastasSemAcesso + p.PastasComErro;
        if (semLeitura > 0)
        {
            s.Append($" {H(Formatador.Plural(semLeitura, "pasta não pôde ser lida e não entra", "pastas não puderam ser lidas e não entram"))} nesta conta.");
        }

        s.Append("</p><div class=\"resumo\"><div><h3>Maiores pastas</h3><ul>");
        foreach (var sub in p.Subpastas.Where(x => x.Estado == EstadoPasta.Lida).OrderByDescending(x => x.Tamanho).Take(5))
        {
            s.Append($"<li>{H(sub.Nome)}: {H(Formatador.Tamanho(sub.Tamanho))}</li>");
        }

        s.Append("</ul></div><div><h3>Por tipo</h3><ul>");
        foreach (var t in Analises.PorCategoria(p).Take(5))
        {
            s.Append($"<li>{H(t.Nome)}: {H(Formatador.Tamanho(t.Tamanho))}</li>");
        }

        var antigos = Analises.ArquivosAntigos(p, Idade.DoisAnos, a.Gerado);
        s.Append($"</ul></div><div><h3>Sem alteração há mais de 2 anos</h3><p>{H(Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos"))}, ");
        s.Append($"{H(Formatador.Tamanho(antigos.Tamanho))}</p></div></div>");
    }

    private static string H(string texto) => WebUtility.HtmlEncode(texto);

    // Texto dentro de aspas no JavaScript: barra, aspas e o fim de script escapados.
    private static string Js(string texto) => texto.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("<", "\\u003c");

    private static string Logo()
    {
        using var fluxo = typeof(PaginaAvaliacao).Assembly.GetManifestResourceStream("mt-logo.png")!;
        using var memoria = new MemoryStream();
        fluxo.CopyTo(memoria);
        return Convert.ToBase64String(memoria.ToArray());
    }
}
