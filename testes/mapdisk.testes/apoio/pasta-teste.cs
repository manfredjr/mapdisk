using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MapDisk.Testes;

/// <summary>
/// Árvore de teste dentro da pasta de saída dos testes. Nunca toca fora dela. No fim, tira as
/// regras de acesso negado que o teste criou e apaga tudo.
/// </summary>
internal sealed class PastaTeste : IDisposable
{
    private readonly List<string> _negadas = [];
    private readonly List<string> _juncoes = [];

    public PastaTeste()
    {
        Raiz = Path.Combine(AppContext.BaseDirectory, "arvores-teste", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Raiz);
    }

    public string Raiz { get; }

    public string Caminho(string relativo) => Path.Combine(Raiz, relativo);

    public string Pasta(string relativo)
    {
        var caminho = Caminho(relativo);
        Directory.CreateDirectory(caminho);
        return caminho;
    }

    public string Arquivo(string relativo, int bytes, DateTime? modificacao = null)
    {
        var caminho = Caminho(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllBytes(caminho, new byte[bytes]);
        if (modificacao is { } data)
        {
            File.SetLastWriteTime(caminho, data);
        }

        return caminho;
    }

    public string Juncao(string relativo, string destinoRelativo)
    {
        var caminho = Caminho(relativo);
        Rodar($"mklink /J \"{caminho}\" \"{Caminho(destinoRelativo)}\"");
        _juncoes.Add(caminho);
        return caminho;
    }

    public string HardLink(string relativo, string existenteRelativo)
    {
        var caminho = Caminho(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        Rodar($"mklink /H \"{caminho}\" \"{Caminho(existenteRelativo)}\"");
        return caminho;
    }

    public string NegarLeitura(string relativo)
    {
        var caminho = Pasta(relativo);
        var info = new DirectoryInfo(caminho);
        var regras = info.GetAccessControl();
        regras.AddAccessRule(Negacao());
        info.SetAccessControl(regras);
        _negadas.Add(caminho);
        return caminho;
    }

    public void Dispose()
    {
        foreach (var caminho in _negadas)
        {
            var info = new DirectoryInfo(caminho);
            var regras = info.GetAccessControl();
            regras.RemoveAccessRule(Negacao());
            info.SetAccessControl(regras);
        }

        // A junção sai antes, sozinha: apaga só o link, sem entrar no destino.
        foreach (var juncao in _juncoes)
        {
            try
            {
                Directory.Delete(juncao);
            }
            catch (IOException)
            {
            }
        }

        try
        {
            Directory.Delete(@"\\?\" + Raiz, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static FileSystemAccessRule Negacao() =>
        new(WindowsIdentity.GetCurrent().User!, FileSystemRights.ListDirectory, AccessControlType.Deny);

    private static void Rodar(string comando)
    {
        using var processo = Process.Start(new ProcessStartInfo("cmd.exe", "/c " + comando)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        processo.WaitForExit();
        if (processo.ExitCode != 0)
        {
            throw new InvalidOperationException($"Falhou: {comando}{Environment.NewLine}{processo.StandardError.ReadToEnd()}");
        }
    }
}
