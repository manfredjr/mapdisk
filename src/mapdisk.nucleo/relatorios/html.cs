using System.Net;

namespace MapDisk.Nucleo;

/// <summary>O que os dois relatórios HTML usam: escape e o logo da MT embutido.</summary>
public static class Html
{
    public static string H(string texto) => WebUtility.HtmlEncode(texto);

    /// <summary>Texto dentro de aspas no JavaScript: barra, aspas e o fim de script escapados.</summary>
    public static string Js(string texto) => texto.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("<", "\\u003c");

    public static string Logo()
    {
        using var fluxo = typeof(Html).Assembly.GetManifestResourceStream("mt-logo.png")!;
        using var memoria = new MemoryStream();
        fluxo.CopyTo(memoria);
        return Convert.ToBase64String(memoria.ToArray());
    }
}
