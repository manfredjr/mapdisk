namespace MapDisk.Nucleo;

/// <summary>
/// Textos e endereços da janela Sobre. Os avisos de licença seguem a GPL-3.0 (seção 0, "Appropriate
/// Legal Notices") e o texto "Licença e garantias" de docs/legal/verificacao-distribuicao-e-lgpd-2026-09-28.md.
/// </summary>
public static class Sobre
{
    public const string SiteMt = "https://www.manfred.com.br";

    public const string Repositorio = "https://github.com/manfredjr/mapdisk";

    public const string Versoes = "https://github.com/manfredjr/mapdisk/releases";

    public const string Descricao = "Analisador de espaço em disco para Windows.";

    public const string Autoria = "Desenvolvido por Manfred Heil Junior, da MT - Manfred Tecnologia.";

    public const string Copyright = "Copyright (c) 2026 MANFRED TECNOLOGIA LTDA";

    public const string SoftwareLivre =
        "Este programa é software livre: você pode redistribuí-lo e modificá-lo nos termos da GNU General Public " +
        "License, versão 3 (GPL-3.0), publicada pela Free Software Foundation. O botão \"Ver a licença\" mostra o texto completo.";

    public const string LicencaEGarantias =
        "O MapDisk - MT é distribuído gratuitamente sob a GPL-3.0. Os tamanhos mostrados dependem do que o sistema de " +
        "arquivos informa e das permissões da conta que roda o programa, e podem ficar incompletos quando alguma pasta " +
        "não pode ser lida. A licença não inclui promessa de funcionamento em todo computador nem serviço de suporte " +
        "técnico. Quem apaga ou move arquivos pelo programa decide o que tratar e deve ter cópia de segurança do que for " +
        "importante. As disposições da GPL-3.0 sobre garantias e responsabilidade aplicam-se nos limites permitidos pela " +
        "legislação brasileira e não restringem direitos assegurados ao consumidor por lei.";

    public const string Fonte = "A fonte Montserrat vai embutida no programa sob a SIL Open Font License 1.1.";

    public static string Titulo => $"MapDisk - MT, versão {ExecutorCli.Versao}";

    /// <summary>O texto completo da GPL-3.0, embutido no programa: abre sem internet.</summary>
    public static string LerLicenca()
    {
        using var fluxo = typeof(Sobre).Assembly.GetManifestResourceStream("licenca.txt")
            ?? throw new InvalidOperationException("A licença não foi embutida no programa.");
        using var leitor = new StreamReader(fluxo);
        return leitor.ReadToEnd();
    }
}
