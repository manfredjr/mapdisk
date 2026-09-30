using System.Windows;
using MapDisk.Nucleo;

namespace MapDisk;

/// <summary>Confirmação de uma ação: o texto aprovado, os avisos, a lista, o total e o destino.</summary>
public partial class JanelaConfirmacao : Window
{
    private readonly bool _pedeExcluir;

    public JanelaConfirmacao(Confirmacao confirmacao, PedidoAcao pedido)
    {
        InitializeComponent();
        Title = $"{confirmacao.Titulo} - MapDisk - MT";
        TextoTitulo.Text = confirmacao.Titulo;
        TextoPergunta.Text = confirmacao.Texto;
        ListaAvisos.ItemsSource = confirmacao.Avisos;
        ListaItens.ItemsSource = pedido.Itens.Select(i => new { i.Nome, TextoTamanho = Formatador.Tamanho(i.Tamanho), i.Caminho }).ToList();
        TextoTotal.Text = $"Total: {Formatador.Plural(pedido.Itens.Count, "item", "itens")}, {Formatador.Tamanho(pedido.Total)}";
        TextoDestino.Text = pedido.Destino is { } destino ? $"Destino: {destino}" : string.Empty;
        TextoDestino.Visibility = pedido.Destino is null ? Visibility.Collapsed : Visibility.Visible;
        BotaoAgir.Content = confirmacao.TextoBotao;
        _pedeExcluir = confirmacao.PedeExcluir;
        PainelExcluir.Visibility = _pedeExcluir ? Visibility.Visible : Visibility.Collapsed;
        BotaoAgir.IsEnabled = !_pedeExcluir;
        Loaded += (_, _) => BotaoCancelar.Focus();
    }

    private void AoDigitarExcluir(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        BotaoAgir.IsEnabled = CampoExcluir.Text == "EXCLUIR";

    private void AoAgir(object sender, RoutedEventArgs e)
    {
        if (_pedeExcluir && CampoExcluir.Text != "EXCLUIR")
        {
            return;
        }

        DialogResult = true;
    }
}
