using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum EstadoPainel
{
    Parado,
    Varrendo,
    Cancelando,
}

/// <summary>
/// Estado e comandos da janela principal, sem nenhum tipo do WPF. A janela chama Tique a cada
/// 250 ms. É no Tique que os números se atualizam e que a varredura é fechada quando termina.
/// </summary>
public sealed class PainelPrincipal : INotifyPropertyChanged
{
    private readonly IMotorVarredura _motor;
    private readonly Func<IReadOnlyList<InfoVolume>> _listarUnidades;
    private CancellationTokenSource? _cancelar;
    private Varredura? _varredura;
    private InfoVolume? _volume;
    private string? _ultimoAlvo;

    public PainelPrincipal(IMotorVarredura motor, Func<IReadOnlyList<InfoVolume>> listarUnidades)
    {
        _motor = motor;
        _listarUnidades = listarUnidades;
        Unidades = listarUnidades();
        TextoAlvo = Unidades.FirstOrDefault()?.Raiz ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static PainelPrincipal Padrao() => new(new MotorVarredura(), Volumes.ListarUnidades);

    public ArvoreVisivel Arvore { get; } = new();

    public IReadOnlyList<InfoVolume> Unidades { get; private set; }

    public string TextoAlvo { get; set; }

    public EstadoPainel Estado { get; private set; }

    public bool PodeVarrer => Estado == EstadoPainel.Parado;

    public bool PodeParar => Estado == EstadoPainel.Varrendo;

    public bool PodeAtualizar => Estado == EstadoPainel.Parado && _ultimoAlvo != null;

    public bool Varrendo => Estado != EstadoPainel.Parado;

    public string TextoEstado { get; private set; } = "Escolha uma unidade, pasta ou caminho de rede e clique em Varrer.";

    public string TextoVolume { get; private set; } = string.Empty;

    public string TextoTotais { get; private set; } = string.Empty;

    public string TextoSemLeitura { get; private set; } = string.Empty;

    /// <summary>Mensagem para a janela mostrar numa caixa. A janela chama LimparErro depois.</summary>
    public string? Erro { get; private set; }

    public void AtualizarUnidades()
    {
        Unidades = _listarUnidades();
        Avisar();
    }

    public bool Varrer()
    {
        if (!PodeVarrer)
        {
            return false;
        }

        var alvo = Alvo.Normalizar(TextoAlvo, out var erro);
        if (alvo is null)
        {
            Erro = erro;
            Avisar();
            return false;
        }

        Iniciar(alvo);
        return true;
    }

    public void Atualizar()
    {
        if (PodeAtualizar)
        {
            Iniciar(_ultimoAlvo!);
        }
    }

    /// <summary>Lê de novo só esta pasta (Shift+F5). A raiz é varrida inteira. Link não é relido.</summary>
    public bool AtualizarPasta(NoPasta pasta)
    {
        if (!PodeVarrer || pasta.Estado == EstadoPasta.Link)
        {
            return false;
        }

        if (pasta.Pai is null)
        {
            Atualizar();
            return true;
        }

        Erro = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Reler(pasta, _cancelar.Token);
        Arvore.Substituir(pasta, _varredura.Raiz);
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
        return true;
    }

    public void Parar()
    {
        if (Estado != EstadoPainel.Varrendo)
        {
            return;
        }

        Estado = EstadoPainel.Cancelando;
        TextoEstado = "Parando...";
        _cancelar!.Cancel();
        Avisar();
    }

    public void LimparErro() => Erro = null;

    public bool PodeVoltar => Arvore.PodeVoltar;

    public bool PodeAvancar => Arvore.PodeAvancar;

    public bool PodeSubir => Arvore.PodeSubir;

    public void AbrirAqui(NoPasta pasta) => Navegar(() => Arvore.AbrirAqui(pasta));

    public void Voltar() => Navegar(Arvore.Voltar);

    public void Avancar() => Navegar(Arvore.Avancar);

    public void Subir() => Navegar(Arvore.Subir);

    public void AbrirNiveis(int niveis) => Navegar(() => Arvore.AbrirNiveis(niveis));

    // Navegar muda a raiz mostrada: os totais da barra passam a ser os dessa raiz.
    private void Navegar(Action acao)
    {
        acao();
        AtualizarTextos();
    }

    public void Tique()
    {
        if (_varredura is null)
        {
            return;
        }

        if (_varredura.Conclusao.IsCompleted)
        {
            Concluir();
            return;
        }

        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void Iniciar(string alvo)
    {
        _ultimoAlvo = alvo;
        TextoAlvo = alvo;
        Erro = null;
        _volume = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Iniciar(alvo, _cancelar.Token);
        Arvore.Carregar(_varredura.Raiz);
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
    }

    private void Concluir()
    {
        var varredura = _varredura!;
        _varredura = null;

        // Primeiro o estado, depois qualquer aviso: os botões voltam ao normal antes da caixa de erro.
        Estado = EstadoPainel.Parado;
        if (varredura.Conclusao.IsFaulted)
        {
            TextoEstado = "A varredura parou por um erro.";
            Erro = $"A varredura parou por um erro: {varredura.Conclusao.Exception!.GetBaseException().Message}";
        }
        else
        {
            var r = varredura.Conclusao.Result;
            _volume = r.Volume;
            TextoEstado = r.Cancelada
                ? $"Varredura interrompida em {Formatador.Duracao(r.Duracao)}. Os números mostram só o que foi lido até ali."
                : $"Varredura concluída em {Formatador.Duracao(r.Duracao)}.";
            if (r.Raiz.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura)
            {
                Erro = $"Não foi possível ler {r.Raiz.Nome}: {r.Raiz.Motivo}.";
            }
            else if (!r.Cancelada && r.PastasNaoLidas > 0)
            {
                Erro = $"A varredura terminou, mas {Formatador.Plural(r.PastasNaoLidas, "pasta não foi lida", "pastas não foram lidas")}. Clique em Atualizar para varrer de novo.";
            }
        }

        _cancelar?.Dispose();
        _cancelar = null;
        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        if (Arvore.Raiz is { } raiz)
        {
            if (_varredura is not null && Estado == EstadoPainel.Varrendo)
            {
                TextoEstado = $"Varrendo {_varredura.PastaAtual}";
            }

            TextoTotais = $"{Formatador.Plural(raiz.ArquivosTotal, "arquivo", "arquivos")} em {Formatador.Plural(raiz.PastasTotal, "pasta", "pastas")}";
            var partes = new List<string>();
            if (raiz.PastasSemAcesso > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasSemAcesso, "pasta sem acesso", "pastas sem acesso"));
            }

            if (raiz.PastasComErro > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasComErro, "pasta com erro de leitura", "pastas com erro de leitura"));
            }

            TextoSemLeitura = string.Join(" | ", partes);
        }

        TextoVolume = _volume is { } v
            ? $"Livre: {Formatador.Tamanho(v.Livre)} de {Formatador.Tamanho(v.Total)} | Cluster {Formatador.Tamanho(v.Cluster)} ({v.SistemaArquivos})"
            : string.Empty;
        Avisar();
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
