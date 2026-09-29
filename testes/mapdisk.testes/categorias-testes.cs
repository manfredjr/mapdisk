namespace MapDisk.Testes;

public class CategoriasTestes
{
    [Theory]
    [InlineData("filme.MP4", Categoria.Video)]
    [InlineData("foto.jpeg", Categoria.Imagem)]
    [InlineData("musica.flac", Categoria.Audio)]
    [InlineData("caixa.PST", Categoria.Email)]
    [InlineData("arquivo.ost", Categoria.Email)]
    [InlineData("windows.iso", Categoria.ImagemDeDisco)]
    [InlineData("maquina.vhdx", Categoria.ImagemDeDisco)]
    [InlineData("copia.zip", Categoria.CompactadoEBackup)]
    [InlineData("banco.bak", Categoria.CompactadoEBackup)]
    [InlineData("setup.msi", Categoria.Instalador)]
    [InlineData("contrato.pdf", Categoria.Documento)]
    [InlineData("planilha.xlsx", Categoria.Documento)]
    [InlineData("pagefile.sys", Categoria.Outros)]
    [InlineData("sem-extensao", Categoria.Outros)]
    public void Categoria_pela_extensao(string nome, Categoria esperada)
    {
        Assert.Equal(esperada, Categorias.De(nome));
    }

    [Fact]
    public void Nomes_em_portugues()
    {
        Assert.Equal("E-mail (.pst, .ost)", Categorias.Nome(Categoria.Email));
        Assert.Equal(".mp4", Categorias.Extensao("filme.MP4"));
        Assert.Equal("(sem extensão)", Categorias.Extensao("sem-extensao"));
    }
}
