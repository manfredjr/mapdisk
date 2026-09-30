namespace MapDisk.Nucleo;

/// <summary>Um arquivo achado por uma análise, com a pasta da árvore onde ele está.</summary>
public sealed record ArquivoEncontrado(NoPasta Pasta, ArquivoInfo Arquivo)
{
    public string Caminho => Alvo.Juntar(Pasta.CaminhoCompleto(), Arquivo.Nome);
}
