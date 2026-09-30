namespace MapDisk.Testes;

public class SobreTestes
{
    [Fact]
    public void Licenca_embutida_e_o_arquivo_license_do_repositorio()
    {
        var arquivo = File.ReadAllText(Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "LICENSE"));
        var embutida = Sobre.LerLicenca();
        Assert.Equal(arquivo.ReplaceLineEndings(), embutida.ReplaceLineEndings());
        Assert.Contains("GNU GENERAL PUBLIC LICENSE", embutida);
        Assert.Contains("Version 3, 29 June 2007", embutida);
    }

    [Fact]
    public void Avisos_seguem_a_gpl_e_o_texto_juridico_aprovado()
    {
        // Seção 0 da GPL-3.0: copyright, ausência de garantia, direito de redistribuir e como ver a licença.
        Assert.StartsWith("Copyright (c) 2026 MANFRED TECNOLOGIA LTDA", Sobre.Copyright);
        Assert.Contains("redistribuí-lo e modificá-lo", Sobre.SoftwareLivre);
        Assert.Contains("\"Ver a licença\"", Sobre.SoftwareLivre);

        // Texto "Licença e garantias", igual ao da seção 5 da verificação jurídica.
        var verificacao = File.ReadAllText(Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(),
            "docs", "legal", "verificacao-distribuicao-e-lgpd-2026-09-28.md"));
        Assert.Contains("> Licença e garantias. " + Sobre.LicencaEGarantias, verificacao);
    }

    [Fact]
    public void Copyright_e_o_mesmo_dos_metadados_do_exe()
    {
        var props = File.ReadAllText(Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "Directory.Build.props"));
        Assert.Contains($"<Copyright>{Sobre.Copyright}</Copyright>", props);
    }

    [Fact]
    public void Enderecos_sao_do_repositorio_e_do_site_da_mt()
    {
        Assert.Equal("https://github.com/manfredjr/mapdisk", Sobre.Repositorio);
        Assert.Equal(Sobre.Repositorio + "/releases", Sobre.Versoes);
        Assert.Equal("https://www.manfred.com.br", Sobre.SiteMt);
    }
}
