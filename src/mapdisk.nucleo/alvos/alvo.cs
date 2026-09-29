namespace MapDisk.Nucleo;

/// <summary>O que o técnico digita ou escolhe para varrer: unidade, pasta ou caminho de rede.</summary>
public static class Alvo
{
    /// <summary>
    /// Deixa o alvo num formato só: tira espaços e aspas, troca "/" por "\", aceita "C:" como
    /// "C:\" e caminho com prefixo \\?\. Raiz de unidade e de compartilhamento terminam em "\";
    /// pasta, não. Devolve null com o motivo em erro quando o alvo não serve.
    /// </summary>
    public static string? Normalizar(string? texto, out string? erro)
    {
        erro = null;
        var t = texto?.Trim().Trim('"').Trim().Replace('/', '\\');
        if (string.IsNullOrEmpty(t))
        {
            erro = "Informe uma unidade, pasta ou caminho de rede.";
            return null;
        }

        if (t.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            t = @"\\" + t[8..];
        }
        else if (t.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            t = t[4..];
        }

        if (t.Length == 2 && char.IsAsciiLetter(t[0]) && t[1] == ':')
        {
            t += "\\";
        }

        if (EhRede(t))
        {
            var partes = t[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2)
            {
                erro = @"Caminho de rede incompleto. Use \\servidor\compartilhamento.";
                return null;
            }

            return @"\\" + string.Join('\\', partes) + (partes.Length == 2 ? "\\" : string.Empty);
        }

        if (!Path.IsPathFullyQualified(t))
        {
            erro = $@"Use um caminho completo, como C:\ ou D:\Dados. Recebido: {t}";
            return null;
        }

        var cheio = Path.GetFullPath(t);
        var raiz = Path.GetPathRoot(cheio)!;
        return cheio.Length > raiz.Length ? cheio.TrimEnd('\\') : raiz.ToUpperInvariant();
    }

    public static bool EhRede(string caminho) => caminho.StartsWith(@"\\", StringComparison.Ordinal)
        && !caminho.StartsWith(@"\\?\", StringComparison.Ordinal);

    /// <summary>Caminho com o prefixo \\?\, para as APIs do Win32 aceitarem mais de 260 caracteres.</summary>
    public static string Longo(string caminho) =>
        caminho.StartsWith(@"\\?\", StringComparison.Ordinal) ? caminho
        : EhRede(caminho) ? @"\\?\UNC\" + caminho[2..]
        : @"\\?\" + caminho;

    public static string Juntar(string pasta, string nome) => pasta.EndsWith('\\') ? pasta + nome : pasta + "\\" + nome;
}
