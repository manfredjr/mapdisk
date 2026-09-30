namespace MapDisk.Nucleo;

public sealed record PedidoAcao(TipoAcao Acao, IReadOnlyList<ItemAcao> Itens, string? Destino)
{
    public long Total => Itens.Sum(i => i.Tamanho);
}

public sealed record ResultadoItem(ItemAcao Item, bool Ok, string Resultado);

public sealed record ResumoAcao(IReadOnlyList<ResultadoItem> Resultados, bool Cancelada, bool RegistroFalhou)
{
    public long BytesOk => Resultados.Where(r => r.Ok).Sum(r => r.Item.Tamanho);

    public int Ok => Resultados.Count(r => r.Ok);

    public int Falhas => Resultados.Count(r => !r.Ok);
}

public readonly record struct ProgressoAcao(int Feitos, int Total, string Atual);

/// <summary>
/// Faz a ação item por item. Antes de cada item grava "iniciado" no registro; sem essa linha,
/// o item não começa e a execução para (regra 1). Falha de um item não para os outros.
/// </summary>
public sealed class ExecutorAcoes
{
    private readonly IOperacoesArquivo _operacoes;
    private readonly IRegistroAcoes _registro;

    public ExecutorAcoes(IOperacoesArquivo operacoes, IRegistroAcoes registro)
    {
        _operacoes = operacoes;
        _registro = registro;
    }

    /// <summary>A Lixeira do Shell pede thread STA. A execução ganha uma só para ela.</summary>
    public Task<ResumoAcao> ExecutarAsync(PedidoAcao pedido, IProgress<ProgressoAcao>? progresso, CancellationToken cancelar)
    {
        var fim = new TaskCompletionSource<ResumoAcao>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                fim.SetResult(Executar(pedido, progresso, cancelar));
            }
            catch (Exception e)
            {
                fim.SetException(e);
            }
        })
        {
            IsBackground = true,
            Name = "MapDisk ações",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return fim.Task;
    }

    public ResumoAcao Executar(PedidoAcao pedido, IProgress<ProgressoAcao>? progresso, CancellationToken cancelar)
    {
        var resultados = new List<ResultadoItem>();
        for (var n = 0; n < pedido.Itens.Count; n++)
        {
            if (cancelar.IsCancellationRequested)
            {
                return new ResumoAcao(resultados, true, false);
            }

            var item = pedido.Itens[n];
            progresso?.Report(new ProgressoAcao(n, pedido.Itens.Count, item.Caminho));
            if (!Registrar(pedido, item, "iniciado"))
            {
                return new ResumoAcao(resultados, false, true);
            }

            string resultado;
            try
            {
                Agir(pedido, item, cancelar);
                resultado = "ok";
            }
            catch (OperationCanceledException)
            {
                resultado = "cancelado";
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                resultado = $"falhou: {e.Message}";
            }

            resultados.Add(new ResultadoItem(item, resultado == "ok", resultado));
            if (!Registrar(pedido, item, resultado))
            {
                return new ResumoAcao(resultados, false, true);
            }

            if (resultado == "cancelado")
            {
                return new ResumoAcao(resultados, true, false);
            }
        }

        progresso?.Report(new ProgressoAcao(pedido.Itens.Count, pedido.Itens.Count, string.Empty));
        return new ResumoAcao(resultados, false, false);
    }

    private void Agir(PedidoAcao pedido, ItemAcao item, CancellationToken cancelar)
    {
        switch (pedido.Acao)
        {
            case TipoAcao.Lixeira:
                _operacoes.EnviarParaLixeira(item.Caminho);
                break;
            case TipoAcao.Excluir:
                _operacoes.ExcluirDefinitivo(item.Caminho, item.EhPasta);
                break;
            default:
                _operacoes.Mover(item.Caminho, pedido.Destino!, item.EhPasta, cancelar);
                break;
        }
    }

    private bool Registrar(PedidoAcao pedido, ItemAcao item, string resultado)
    {
        try
        {
            _registro.Gravar(pedido.Acao, item.Caminho, pedido.Destino, item.Tamanho, resultado);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
