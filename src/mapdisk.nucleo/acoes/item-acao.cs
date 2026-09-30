namespace MapDisk.Nucleo;

public enum TipoAcao
{
    Lixeira,
    Mover,
    Excluir,
}

/// <summary>
/// Um item de uma ação, com o tamanho e a data da varredura, para a conferência na hora.
/// Em item de pasta, Pasta é a própria pasta. Em item de arquivo, a pasta onde ele está.
/// </summary>
public sealed record ItemAcao(string Caminho, bool EhPasta, long Tamanho, DateTime Modificacao, MarcaArquivo Marcas, NoPasta Pasta, ArquivoInfo? Arquivo)
{
    public string Nome => EhPasta ? Pasta.Nome : Arquivo!.Value.Nome;

    public static ItemAcao DaPasta(NoPasta pasta) =>
        new(pasta.CaminhoCompleto(), true, pasta.Tamanho, pasta.ModificacaoPropria, MarcaArquivo.Nenhuma, pasta, null);

    public static ItemAcao DoArquivo(NoPasta pai, ArquivoInfo arquivo) =>
        new(Path.Combine(pai.CaminhoCompleto(), arquivo.Nome), false, arquivo.Tamanho, arquivo.Modificacao, arquivo.Marcas, pai, arquivo);
}

/// <summary>Onde o Windows garante a Lixeira: só em unidade fixa local.</summary>
public static class Lixeiras
{
    public static bool Existe(string caminho, Func<string, DriveType> tipoDaUnidade) =>
        !Alvo.EhRede(caminho) && tipoDaUnidade(Volumes.RaizDe(caminho)) == DriveType.Fixed;

    public static DriveType TipoDaUnidade(string raiz)
    {
        try
        {
            return new DriveInfo(raiz).DriveType;
        }
        catch (ArgumentException)
        {
            return DriveType.Unknown;
        }
    }
}
