namespace MapDisk.Nucleo;

public enum Categoria
{
    Video,
    Imagem,
    Audio,
    Email,
    ImagemDeDisco,
    CompactadoEBackup,
    Instalador,
    Documento,
    Outros,
}

/// <summary>Categoria de cada arquivo pela extensão, para o resumo por tipo (R13).</summary>
public static class Categorias
{
    private static readonly Dictionary<string, Categoria> _porExtensao = Montar(
        (Categoria.Video, [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".mpg", ".mpeg", ".webm", ".flv", ".ts"]),
        (Categoria.Imagem, [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".heic", ".webp", ".raw", ".cr2", ".nef", ".psd"]),
        (Categoria.Audio, [".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".wma"]),
        (Categoria.Email, [".pst", ".ost"]),
        (Categoria.ImagemDeDisco, [".iso", ".img", ".vhd", ".vhdx", ".vmdk", ".wim", ".esd"]),
        (Categoria.CompactadoEBackup, [".zip", ".rar", ".7z", ".tar", ".gz", ".bak", ".bkf", ".tib", ".vbk"]),
        (Categoria.Instalador, [".exe", ".msi", ".msix", ".appx", ".cab"]),
        (Categoria.Documento, [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".txt", ".csv", ".rtf"]));

    public static Categoria De(string nomeDoArquivo) =>
        _porExtensao.TryGetValue(Path.GetExtension(nomeDoArquivo), out var categoria) ? categoria : Categoria.Outros;

    public static string Nome(Categoria categoria) => categoria switch
    {
        Categoria.Video => "Vídeo",
        Categoria.Imagem => "Imagem",
        Categoria.Audio => "Áudio",
        Categoria.Email => "E-mail (.pst, .ost)",
        Categoria.ImagemDeDisco => "Imagem de disco",
        Categoria.CompactadoEBackup => "Compactado e backup",
        Categoria.Instalador => "Instalador",
        Categoria.Documento => "Documento",
        _ => "Outros",
    };

    public static string Extensao(string nomeDoArquivo) =>
        Path.GetExtension(nomeDoArquivo) is { Length: > 0 } extensao ? extensao.ToLowerInvariant() : "(sem extensão)";

    private static Dictionary<string, Categoria> Montar(params (Categoria Categoria, string[] Extensoes)[] grupos)
    {
        var mapa = new Dictionary<string, Categoria>(StringComparer.OrdinalIgnoreCase);
        foreach (var (categoria, extensoes) in grupos)
        {
            foreach (var extensao in extensoes)
            {
                mapa[extensao] = categoria;
            }
        }

        return mapa;
    }
}
