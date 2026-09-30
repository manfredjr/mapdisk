using System.IO;
using System.Windows;
using System.Windows.Controls;
using MapDisk.Nucleo;
using Microsoft.Win32;

namespace MapDisk;

/// <summary>Lê a resposta do cliente e leva os itens escolhidos para as ações, pela confirmação de sempre.</summary>
public partial class JanelaResposta : Window
{
    private readonly PainelResposta _painel;
    private readonly Func<IReadOnlyList<ItemAcao>, bool, Task> _agir;

    public JanelaResposta(PainelResposta painel, Func<IReadOnlyList<ItemAcao>, bool, Task> agir)
    {
        _painel = painel;
        _agir = agir;
        DataContext = painel;
        InitializeComponent();
    }

    private void AoAbrir(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFileDialog
        {
            Title = "Resposta do cliente",
            Filter = "Resposta do relatório (*.xlsx;*.json)|*.xlsx;*.json",
        };
        if (dialogo.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _painel.Ler(dialogo.FileName);
        }
        catch (Exception erro) when (erro is FormatException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, erro.Message, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AoMarcar(object sender, RoutedEventArgs e)
    {
        if (CampoDecisao.SelectedItem is not ComboBoxItem { Tag: string tag } || !Enum.TryParse<Decisao>(tag, out var decisao))
        {
            return;
        }

        try
        {
            _painel.MarcarPorNumeros(CampoNumeros.Text, decisao);
        }
        catch (FormatException erro)
        {
            MessageBox.Show(this, erro.Message, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AoSelecionarApagar(object sender, RoutedEventArgs e) => Selecionar(Decisao.Apagar);

    private void AoSelecionarMover(object sender, RoutedEventArgs e) => Selecionar(Decisao.Mover);

    private void Selecionar(Decisao decisao)
    {
        var numeros = _painel.NumerosDe(decisao).ToHashSet();
        Lista.SelectedItems.Clear();
        foreach (var linha in Lista.Items.Cast<LinhaResposta>().Where(l => numeros.Contains(l.Numero)))
        {
            Lista.SelectedItems.Add(linha);
        }

        Lista.Focus();
    }

    private async void AoApagar(object sender, RoutedEventArgs e) => await Agir(mover: false);

    private async void AoMover(object sender, RoutedEventArgs e) => await Agir(mover: true);

    // Só os itens achados e iguais ao relatório vão para as ações. O resto aparece na coluna Situação.
    private async Task Agir(bool mover)
    {
        var itens = _painel.ItensDe(Lista.SelectedItems.Cast<LinhaResposta>());
        if (itens.Count == 0)
        {
            MessageBox.Show(this, "Nenhum item selecionado pode ser tratado: confira a coluna Situação.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _agir(itens, mover);
    }

    private void AoFechar(object sender, RoutedEventArgs e) => Close();
}
