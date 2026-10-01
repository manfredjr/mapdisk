namespace MapDisk.Nucleo;

/// <summary>A linha de comando sem console: recebe onde escrever, para ser testada. Só lê.</summary>
public static class ExecutorCli
{
    public const int CodigoSucesso = 0;
    public const int CodigoArgumentos = 1;
    public const int CodigoSemLeitura = 2;
    public const int CodigoFalha = 3;
    public const int CodigoCancelado = 4;

    public static string Versao => typeof(ExecutorCli).Assembly.GetName().Version!.ToString(3);

    /// <summary>As chaves e o .exe só mudam nos testes; o padrão é o registro do usuário e este programa.</summary>
    public static int Executar(ArgumentosCli argumentos, IMotorVarredura motor, TextWriter saida, TextWriter erro, CancellationToken cancelar, IChavesUsuario? chaves = null, string? exe = null)
    {
        if (!argumentos.Valido)
        {
            foreach (var e in argumentos.Erros)
            {
                erro.WriteLine(e);
            }

            erro.WriteLine("Use --ajuda para ver as opções.");
            return CodigoArgumentos;
        }

        switch (argumentos.Comando)
        {
            case ComandoCli.Versao:
                saida.WriteLine($"MapDisk - MT {Versao}");
                return CodigoSucesso;
            case ComandoCli.Ajuda or ComandoCli.Janela:
                saida.WriteLine(ArgumentosCli.TextoAjuda);
                return CodigoSucesso;
            case ComandoCli.Integrar:
                IntegracaoExplorer.Ligar(chaves ?? new ChavesUsuarioWindows(), exe ?? Environment.ProcessPath!);
                saida.WriteLine("Item \"Analisar com MapDisk\" ligado no menu das pastas e unidades do Explorer.");
                saida.WriteLine("No Windows 11, ele fica em \"Mostrar mais opções\". Se o mapdisk.exe mudar de lugar, rode --integrar de novo.");
                return CodigoSucesso;
            case ComandoCli.RemoverIntegracao:
                IntegracaoExplorer.Desligar(chaves ?? new ChavesUsuarioWindows());
                saida.WriteLine("Item \"Analisar com MapDisk\" tirado do menu do Explorer.");
                return CodigoSucesso;
        }

        saida.WriteLine($"Varrendo {argumentos.Caminho}...");
        var r = motor.Iniciar(argumentos.Caminho!, cancelar).Conclusao.GetAwaiter().GetResult();
        var raiz = r.Raiz;
        if (raiz.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura)
        {
            erro.WriteLine($"Não foi possível ler {raiz.Nome}: {raiz.Motivo}.");
            return CodigoSemLeitura;
        }

        saida.WriteLine(r.Cancelada
            ? "Varredura interrompida. Os números mostram só o que foi lido até ali."
            : $"Varredura concluída em {Formatador.Duracao(r.Duracao)}.");
        saida.WriteLine($"Total: {Formatador.Tamanho(raiz.Tamanho)} ({Formatador.Tamanho(raiz.Alocado)} alocados) em {Formatador.Plural(raiz.ArquivosTotal, "arquivo", "arquivos")} e {Formatador.Plural(raiz.PastasTotal, "pasta", "pastas")}.");
        if (r.Volume is { } v)
        {
            saida.WriteLine($"Unidade: {Formatador.Tamanho(v.Livre)} livres de {Formatador.Tamanho(v.Total)} ({v.SistemaArquivos}).");
        }

        if (raiz.PastasSemAcesso > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(raiz.PastasSemAcesso, "pasta sem acesso", "pastas sem acesso")}. O total não inclui o que está nelas. Rode como administrador para ler tudo.");
        }

        if (raiz.PastasComErro > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(raiz.PastasComErro, "pasta com erro de leitura", "pastas com erro de leitura")}. O total não inclui o que está nelas.");
        }

        if (!r.Cancelada && r.PastasNaoLidas > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(r.PastasNaoLidas, "pasta não foi lida", "pastas não foram lidas")}. Rode de novo para conferir.");
        }

        saida.WriteLine();
        saida.WriteLine($"Maiores itens em {raiz.Nome}:");
        foreach (var linha in MaioresItens(raiz, argumentos.Top))
        {
            saida.WriteLine(linha);
        }

        if (argumentos.Csv is { } csv)
        {
            try
            {
                ExportadorCsv.Gravar(raiz, csv);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                erro.WriteLine($"Não foi possível gravar o CSV: {e.Message}");
                return CodigoFalha;
            }

            saida.WriteLine();
            saida.WriteLine($"CSV gravado em {Path.GetFullPath(csv)}");
        }

        if (argumentos.Relatorio is { } relatorio)
        {
            try
            {
                RelatorioTecnico.Gravar(new DadosRelatorio(raiz, DateTime.Now, Environment.MachineName, r.Volume, r.Cancelada, argumentos.Top, null), relatorio);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                erro.WriteLine($"Não foi possível gravar o relatório: {e.Message}");
                return CodigoFalha;
            }

            saida.WriteLine();
            saida.WriteLine($"Relatório gravado em {Path.GetFullPath(relatorio)}");
        }

        return r.Cancelada ? CodigoCancelado : CodigoSucesso;
    }

    private static IEnumerable<string> MaioresItens(NoPasta raiz, int quantos)
    {
        var itens = raiz.Subpastas
            .Select(p => (p.Nome, p.Tamanho, Texto: p.Estado switch
            {
                EstadoPasta.Pendente => "não lida",
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.ErroLeitura => "erro",
                EstadoPasta.Link => "link",
                _ => Formatador.Tamanho(p.Tamanho),
            }, Lida: p.Estado == EstadoPasta.Lida))
            .ToList();
        if (raiz.Arquivos.Count > 0)
        {
            itens.Add(($"[{Formatador.Plural(raiz.Arquivos.Count, "arquivo", "arquivos")}]", raiz.TamanhoProprio, Formatador.Tamanho(raiz.TamanhoProprio), true));
        }

        foreach (var (nome, tamanho, texto, lida) in itens.OrderByDescending(i => i.Tamanho).ThenBy(i => i.Nome, StringComparer.CurrentCultureIgnoreCase).Take(quantos))
        {
            var porcentagem = lida && raiz.Tamanho > 0 ? Formatador.Porcentagem((double)tamanho / raiz.Tamanho) : string.Empty;
            yield return $"  {texto,12}  {porcentagem,8}  {nome}";
        }
    }
}
