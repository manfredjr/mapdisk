namespace MapDisk.Nucleo;

/// <summary>Pastas do sistema e a pasta do registro de ações. Os testes passam os seus.</summary>
public sealed record LocaisProtegidos(IReadOnlyList<string> Pastas, string PastaRegistro)
{
    public static LocaisProtegidos DoSistema() => new(
        new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        }.Where(p => p.Length > 0).ToList(),
        RegistroAcoes.PastaPadrao);
}

/// <summary>O que o programa nunca move nem apaga (regra 1 e spec, seção 7, passo 3).</summary>
public static class Protecao
{
    private static readonly string[] NomesDoSistema = ["System Volume Information", "$Recycle.Bin"];

    /// <summary>O motivo do bloqueio, para o botão e a confirmação. Null quando pode agir.</summary>
    public static string? Motivo(string caminho, bool ehPasta, MarcaArquivo marcas, LocaisProtegidos locais)
    {
        var c = Limpo(caminho);
        if (ehPasta && string.Equals(c, Limpo(Volumes.RaizDe(caminho)), StringComparison.OrdinalIgnoreCase))
        {
            return "É a raiz da unidade.";
        }

        if ((marcas & MarcaArquivo.Sistema) != 0)
        {
            return "É arquivo do sistema.";
        }

        foreach (var nome in NomesDoSistema)
        {
            if (c.Split('\\').Any(parte => string.Equals(parte, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return $"É pasta do sistema ({nome}).";
            }
        }

        foreach (var pasta in locais.Pastas)
        {
            if (Dentro(c, pasta))
            {
                return $"É pasta do sistema ({Limpo(pasta)}).";
            }
        }

        return Dentro(locais.PastaRegistro, c) ? "Contém o registro de ações do MapDisk." : null;
    }

    /// <summary>O caminho é a própria pasta ou fica abaixo dela.</summary>
    public static bool Dentro(string caminho, string pasta)
    {
        var c = Limpo(caminho);
        var p = Limpo(pasta);
        return string.Equals(c, p, StringComparison.OrdinalIgnoreCase)
            || c.StartsWith(p + @"\", StringComparison.OrdinalIgnoreCase);
    }

    // Sem a barra do fim: "C:\" vira "C:" e "\\srv\dados\" vira "\\srv\dados".
    private static string Limpo(string caminho) => caminho.TrimEnd('\\');
}
