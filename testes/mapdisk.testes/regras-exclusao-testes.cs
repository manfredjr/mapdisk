namespace MapDisk.Testes;

public class RegrasExclusaoTestes
{
    [Fact]
    public void Nome_vale_em_qualquer_nivel_sem_diferenca_de_maiuscula()
    {
        var r = RegrasExclusao.De(["node_modules"]);
        Assert.Equal("node_modules", r.Motivo(@"D:\Projetos\site\node_modules", "node_modules"));
        Assert.Equal("node_modules", r.Motivo(@"D:\a\b\NODE_MODULES", "NODE_MODULES"));
        Assert.Null(r.Motivo(@"D:\Projetos\node_modules_velho", "node_modules_velho"));
    }

    [Fact]
    public void Caminho_vale_so_para_aquela_pasta()
    {
        var r = RegrasExclusao.De([@"D:\Backup\Veeam\"]);
        Assert.Equal(@"D:\Backup\Veeam", r.Motivo(@"d:\backup\veeam", "veeam"));
        Assert.Null(r.Motivo(@"E:\Backup\Veeam", "Veeam"));
        Assert.Null(r.Motivo(@"D:\Backup\Veeam2", "Veeam2"));
    }

    [Theory]
    [InlineData("", "Escreva o nome da pasta ou o caminho completo.")]
    [InlineData("*.tmp", "Use o nome da pasta ou o caminho completo, sem * nem ?")]
    [InlineData(@"C:\", "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.")]
    [InlineData(@"\\servidor\dados", "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.")]
    [InlineData(@"Projetos\velho", "Use só o nome da pasta, como node_modules, ou o caminho completo, como D:\\Backup.")]
    public void Regra_invalida_diz_o_motivo(string texto, string erro)
    {
        Assert.Null(RegrasExclusao.Validar(texto, out var motivo));
        Assert.Equal(erro, motivo);
    }

    [Fact]
    public void Repetida_e_invalida_ficam_de_fora_e_o_teto_e_100()
    {
        var r = RegrasExclusao.De(["node_modules", "NODE_MODULES", "*.tmp", ".git"]);
        Assert.Equal(["node_modules", ".git"], r.Regras);
        Assert.Equal(100, RegrasExclusao.De(Enumerable.Range(0, 150).Select(i => $"pasta{i}")).Regras.Count);
    }

    [Fact]
    public void Arquivo_grava_e_le_uma_regra_por_linha()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "exclusoes-" + Guid.NewGuid().ToString("N"), "excluidas.txt");
        var armazem = new ArquivoExclusoes(arquivo);
        Assert.Empty(armazem.Ler().Regras);
        armazem.Gravar(RegrasExclusao.De(["node_modules", @"D:\Backup"]));
        Assert.Equal(["node_modules", @"D:\Backup"], armazem.Ler().Regras);
        Directory.Delete(Path.GetDirectoryName(arquivo)!, true);
    }
}
