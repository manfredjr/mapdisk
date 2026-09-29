namespace MapDisk.Testes;

/// <summary>
/// O ícone do .exe fica em DIB (bitmap sem compressão) em todos os tamanhos, como no MapNet:
/// o ícone só com PNG dentro trouxe bloqueio no CronoAula.
/// </summary>
public class IconeTestes
{
    private static string Raiz => CaracteresProibidosTestes.RaizDoRepositorio();

    private static string Icone => Path.Combine(Raiz, "src", "mapdisk", "recursos", "mapdisk.ico");

    [Fact]
    public void Icone_tem_os_tamanhos_do_windows_todos_em_dib()
    {
        var dados = File.ReadAllBytes(Icone);
        var quantos = BitConverter.ToUInt16(dados, 4);
        var lados = new List<int>();
        for (var i = 0; i < quantos; i++)
        {
            var entrada = 6 + (16 * i);
            lados.Add(dados[entrada] == 0 ? 256 : dados[entrada]);
            var inicio = BitConverter.ToInt32(dados, entrada + 12);
            Assert.Equal(40, BitConverter.ToInt32(dados, inicio));
        }

        foreach (var lado in new[] { 16, 32, 48, 256 })
        {
            Assert.Contains(lado, lados);
        }
    }

    [Fact]
    public void Exe_e_janela_usam_o_icone()
    {
        var projeto = File.ReadAllText(Path.Combine(Raiz, "src", "mapdisk", "mapdisk.csproj"));
        var janela = File.ReadAllText(Path.Combine(Raiz, "src", "mapdisk", "janela-principal.xaml"));

        Assert.Contains(@"<ApplicationIcon>recursos\mapdisk.ico</ApplicationIcon>", projeto);
        Assert.Contains("recursos/mapdisk.ico", janela);
    }
}
