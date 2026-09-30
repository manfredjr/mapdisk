namespace MapDisk.Nucleo;

public sealed record CriteriosSugestao(int MaioresPastas = 20, int MaioresArquivos = 50, int AnosSemAlteracao = 2, long TamanhoMinimo = 100 * 1024 * 1024);

/// <summary>O "Sugerir": põe na lista o que costuma liberar espaço. Pastas primeiro, para os arquivos dentro delas não repetirem.</summary>
public static class Sugestao
{
    public const string EscolhidoPeloTecnico = "escolhido pelo técnico";

    private static readonly Categoria[] QueCostumamSobrar = [Categoria.ImagemDeDisco, Categoria.CompactadoEBackup, Categoria.Instalador];

    public static void Sugerir(ListaAvaliacao lista, NoPasta pasta, CriteriosSugestao criterios, DateTime hoje)
    {
        foreach (var sub in pasta.Subpastas.Where(s => s.Estado == EstadoPasta.Lida && s.Tamanho > 0).OrderByDescending(s => s.Tamanho).Take(criterios.MaioresPastas))
        {
            lista.Acrescentar(ItemAcao.DaPasta(sub), $"entre as {criterios.MaioresPastas} maiores pastas");
        }

        foreach (var a in Analises.MaioresArquivos(pasta, criterios.MaioresArquivos))
        {
            lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), $"entre os {criterios.MaioresArquivos} maiores arquivos");
        }

        var limite = hoje.AddYears(-criterios.AnosSemAlteracao);
        var anos = criterios.AnosSemAlteracao == 1 ? "1 ano" : $"{criterios.AnosSemAlteracao} anos";
        foreach (var a in Analises.TodosOsArquivos(pasta).Where(a => a.Arquivo.Tamanho >= criterios.TamanhoMinimo))
        {
            if (a.Arquivo.Modificacao != DateTime.MinValue && a.Arquivo.Modificacao < limite)
            {
                lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), $"sem alteração há mais de {anos}");
            }

            var categoria = Categorias.De(a.Arquivo.Nome);
            if (QueCostumamSobrar.Contains(categoria))
            {
                lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), Categorias.Nome(categoria).ToLowerInvariant());
            }
        }
    }
}
