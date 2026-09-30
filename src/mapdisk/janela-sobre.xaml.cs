using System.Windows;
using System.Windows.Documents;
using MapDisk.Nucleo;

namespace MapDisk;

/// <summary>Nome, versão, autoria, avisos da licença e onde baixar. Os textos ficam no núcleo, em Sobre.</summary>
public partial class JanelaSobre : Window
{
    public JanelaSobre()
    {
        InitializeComponent();
        TextoTitulo.Text = Sobre.Titulo;
    }

    private void AoVerLicenca(object sender, RoutedEventArgs e)
    {
        if (CampoLicenca.Visibility == Visibility.Visible)
        {
            CampoLicenca.Visibility = Visibility.Collapsed;
            BotaoLicenca.Content = "Ver a licença";
            return;
        }

        if (CampoLicenca.Text.Length == 0)
        {
            CampoLicenca.Text = Sobre.LerLicenca();
        }

        CampoLicenca.Visibility = Visibility.Visible;
        BotaoLicenca.Content = "Esconder a licença";
    }

    private void AoAbrirLink(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink { Tag: string endereco })
        {
            Shell.AbrirNoNavegador(endereco);
        }
    }

    private void AoClicarMarcaMt(object sender, RoutedEventArgs e) => Shell.AbrirNoNavegador(Sobre.SiteMt);

    private void AoFechar(object sender, RoutedEventArgs e) => Close();
}
