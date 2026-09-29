namespace MapDisk.Testes;

public class VolumesTestes
{
    [Fact]
    public void Le_o_volume_da_pasta_dos_testes()
    {
        var volume = Volumes.Ler(AppContext.BaseDirectory);

        Assert.NotNull(volume);
        Assert.True(volume.Total > 0);
        Assert.InRange(volume.Livre, 0, volume.Total);
        Assert.True(volume.Cluster > 0 && (volume.Cluster & (volume.Cluster - 1)) == 0, "cluster deve ser potência de 2");
        Assert.False(string.IsNullOrEmpty(volume.SistemaArquivos));
    }

    [Fact]
    public void Lista_a_unidade_dos_testes()
    {
        var raiz = Volumes.RaizDe(AppContext.BaseDirectory);
        Assert.Contains(Volumes.ListarUnidades(), v => string.Equals(v.Raiz, raiz, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Raiz_de_caminho_local_e_de_rede()
    {
        Assert.Equal(@"C:\", Volumes.RaizDe(@"C:\Users\Ana"));
        Assert.Equal(@"\\srv\dados\", Volumes.RaizDe(@"\\srv\dados\a\b"));
    }

    [Fact]
    public void Descricao_da_unidade()
    {
        var volume = new InfoVolume(@"C:\", "Sistema", "NTFS", 373L << 30, 952L << 30, 4096);
        Assert.Equal(@"C:\  Sistema  373,0 GB livres de 952,0 GB", volume.Descricao);
    }
}
