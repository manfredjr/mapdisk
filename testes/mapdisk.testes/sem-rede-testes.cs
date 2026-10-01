namespace MapDisk.Testes;

/// <summary>Regra 6 do produto: o programa não envia nada para fora da máquina.</summary>
public class SemRedeTestes
{
    private static readonly string[] ClassesDeRede =
    [
        "HttpClient", "WebClient", "WebRequest", "HttpWebRequest", "TcpClient", "UdpClient", "Socket", "SmtpClient",
        "System.Net.Http", "System.Net.Sockets", "System.Net.Mail", "Dns.",
    ];

    [Fact]
    public void Programa_nao_usa_classe_de_rede()
    {
        var src = Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "src");
        var achados = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj"))
            .SelectMany(f => ClassesDeRede.Where(c => File.ReadAllText(f).Contains(c, StringComparison.Ordinal)).Select(c => $"{Path.GetFileName(f)}: {c}"))
            .ToList();
        Assert.Empty(achados);
    }
}
