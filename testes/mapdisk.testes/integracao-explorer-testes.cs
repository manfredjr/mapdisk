namespace MapDisk.Testes;

public class IntegracaoExplorerTestes
{
    private const string Exe = @"E:\Ferramentas\mapdisk.exe";

    [Fact]
    public void Ligar_grava_as_tres_chaves_no_usuario()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, Exe);
        foreach (var raiz in IntegracaoExplorer.Chaves)
        {
            Assert.StartsWith(@"Software\Classes\", raiz);
            Assert.Equal("Analisar com MapDisk", chaves.Ler(raiz, null));
            Assert.Equal(Exe + ",0", chaves.Ler(raiz, "Icon"));
            Assert.Contains("--abrir", chaves.Ler(raiz + @"\command", null));
        }

        Assert.Equal("\"" + Exe + "\" --abrir \"%V\"", chaves.Ler(@"Software\Classes\Directory\Background\shell\MapDisk\command", null));
        Assert.Equal("\"" + Exe + "\" --abrir \"%1\"", chaves.Ler(@"Software\Classes\Drive\shell\MapDisk\command", null));
    }

    [Fact]
    public void Desligar_tira_tudo()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, Exe);
        IntegracaoExplorer.Desligar(chaves);
        Assert.Empty(chaves.Todas);
        Assert.Equal(EstadoIntegracao.Desligada, IntegracaoExplorer.Estado(chaves, Exe));
    }

    [Fact]
    public void Estado_reconhece_outro_exe()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, @"F:\antigo\mapdisk.exe");
        Assert.Equal(EstadoIntegracao.OutroLocal, IntegracaoExplorer.Estado(chaves, Exe));
        Assert.Equal(@"F:\antigo\mapdisk.exe", IntegracaoExplorer.ExeRegistrado(chaves));
        IntegracaoExplorer.Ligar(chaves, Exe);
        Assert.Equal(EstadoIntegracao.Ligada, IntegracaoExplorer.Estado(chaves, Exe));
    }
}
