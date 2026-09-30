using System.ComponentModel;
using System.Windows;
using MapDisk.Nucleo;

namespace MapDisk;

/// <summary>Executa a ação com barra e Cancelar, e mostra o resumo no fim.</summary>
public partial class JanelaAndamento : Window
{
    private readonly ExecutorAcoes _executor;
    private readonly PedidoAcao _pedido;
    private readonly string _localDoRegistro;
    private readonly CancellationTokenSource _cancelar = new();
    private bool _rodando;

    public JanelaAndamento(ExecutorAcoes executor, PedidoAcao pedido, string localDoRegistro)
    {
        InitializeComponent();
        _executor = executor;
        _pedido = pedido;
        _localDoRegistro = localDoRegistro;
        var titulo = pedido.Acao switch
        {
            TipoAcao.Lixeira => "Enviando para a Lixeira",
            TipoAcao.Mover => "Movendo",
            _ => "Excluindo definitivamente",
        };
        Title = $"{titulo} - MapDisk - MT";
        TextoTitulo.Text = titulo;
        Barra.Maximum = pedido.Itens.Count;
        Loaded += async (_, _) => await Executar();
    }

    /// <summary>O resumo, depois que a ação terminou. Null se não chegou a rodar.</summary>
    public ResumoAcao? Resumo { get; private set; }

    private async Task Executar()
    {
        _rodando = true;
        var progresso = new Progress<ProgressoAcao>(p =>
        {
            Barra.Value = p.Feitos;
            TextoAtual.Text = p.Atual;
        });
        Resumo = await _executor.ExecutarAsync(_pedido, progresso, _cancelar.Token);
        _rodando = false;
        MostrarResumo(Resumo);
    }

    private void MostrarResumo(ResumoAcao resumo)
    {
        PainelAndamento.Visibility = Visibility.Collapsed;
        PainelResumo.Visibility = Visibility.Visible;
        BotaoCancelar.Visibility = Visibility.Collapsed;
        BotaoFechar.Visibility = Visibility.Visible;
        BotaoFechar.Focus();

        var texto = $"{Formatador.Plural(resumo.Ok, "item deu certo", "itens deram certo")} ({Formatador.Tamanho(resumo.BytesOk)}).";
        if (resumo.Falhas > 0)
        {
            texto += $" {Formatador.Plural(resumo.Falhas, "item não foi feito", "itens não foram feitos")}.";
        }

        if (resumo.Cancelada)
        {
            texto += " Cancelado: os itens seguintes ficaram como estavam.";
        }

        TextoResumo.Text = texto;
        if (resumo.RegistroFalhou)
        {
            TextoRegistro.Text = $"O registro de ações não pôde ser gravado em {_localDoRegistro}. A ação parou antes do próximo item.";
            TextoRegistro.Visibility = Visibility.Visible;
        }

        var falhas = resumo.Resultados.Where(r => !r.Ok).Select(r => new { r.Item.Caminho, r.Resultado }).ToList();
        ListaFalhas.ItemsSource = falhas;
        ListaFalhas.Visibility = falhas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AoCancelar(object sender, RoutedEventArgs e)
    {
        _cancelar.Cancel();
        BotaoCancelar.IsEnabled = false;
        TextoAtual.Text = "Cancelando: o item atual termina e os seguintes ficam como estão.";
    }

    private void AoFechar(object sender, RoutedEventArgs e) => Close();

    // Fechar pelo X durante a execução cancela e espera o item atual terminar.
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_rodando)
        {
            e.Cancel = true;
            AoCancelar(this, new RoutedEventArgs());
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancelar.Dispose();
        base.OnClosed(e);
    }
}
