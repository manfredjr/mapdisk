using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MapDisk.Nucleo;

/// <summary>O relatório para o cliente avaliar (R20): cabeçalho, itens numerados e a pasta, para o resumo.</summary>
public sealed record Avaliacao(string Numero, string Cliente, string Tecnico, string Mensagem, NoPasta Pasta, DateTime Gerado, long? Livre, IReadOnlyList<ItemAvaliacao> Itens)
{
    public long Total => Itens.Sum(i => i.Item.Tamanho);

    public static string NumeroDe(DateTime gerado) => gerado.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

    /// <summary>"avaliacao-dados-2026-09-30": última parte da pasta, em minúsculas, sem acento, com hífen.</summary>
    public string NomeDoArquivo
    {
        get
        {
            var parte = Pasta.CaminhoCompleto().TrimEnd('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "pasta";
            var semAcento = new string(parte.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            var simples = Regex.Replace(semAcento.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            return $"avaliacao-{(simples.Length == 0 ? "pasta" : simples)}-{Gerado.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        }
    }
}

/// <summary>Textos para o cliente. Revisados pela legal-br (verificação jurídica, seção 10) e pela humanizar-ptbr.</summary>
public static class TextosAvaliacao
{
    public const string Autorizacao =
        "Ao devolver esta resposta com o seu nome, você declara que pode decidir sobre estes arquivos e autoriza a MT - Manfred Tecnologia a apagar ou mover os itens marcados, como indicado.";

    public static readonly (string Valor, string Rotulo)[] Opcoes =
        [("apagar", "Apagar"), ("mover", "Mover"), ("manter", "Manter"), ("conversar", "Conversar")];

    public static string Apresentacao(string pasta) =>
        $"Este relatório mostra pastas e arquivos que ocupam espaço em {pasta}. Para cada item, marque o que fazer: Apagar, Mover (diga para onde), Manter ou Conversar. " +
        "O que ficar sem marca fica como está. Nada é apagado nem movido antes da sua resposta, e o técnico da MT confere cada item antes de agir. " +
        "Em pastas de rede, o que for apagado não passa pela Lixeira e só volta por uma cópia de segurança.";
}

public sealed record ItemAvaliacao(int Numero, ItemAcao Item, string Motivo)
{
    public string Tipo => Item.EhPasta ? "Pasta" : "Arquivo";

    public long Arquivos => Item.EhPasta ? Item.Pasta.ArquivosTotal : 1;
}

/// <summary>
/// Os candidatos que o cliente vai avaliar. Pasta sem leitura não entra (regra 3). O que está
/// dentro de uma pasta da lista não entra de novo: a decisão sobre a pasta já o leva. Com os
/// locais protegidos, o que as ações bloqueiam também não entra: o cliente não decide sobre o
/// que não pode ser feito.
/// </summary>
public sealed class ListaAvaliacao(LocaisProtegidos? locais = null)
{
    private readonly List<(ItemAcao Item, List<string> Motivos)> _itens = [];

    public IReadOnlyList<ItemAvaliacao> Itens => _itens
        .OrderByDescending(i => i.Item.Tamanho)
        .Select((i, n) => new ItemAvaliacao(n + 1, i.Item, string.Join("; ", i.Motivos)))
        .ToList();

    public long Total => _itens.Sum(i => i.Item.Tamanho);

    public void Acrescentar(ItemAcao item, string motivo)
    {
        if (item.EhPasta && item.Pasta.Estado != EstadoPasta.Lida)
        {
            return;
        }

        if (locais is not null && Protecao.Motivo(item.Caminho, item.EhPasta, item.Marcas, locais) is not null)
        {
            return;
        }

        var existente = _itens.FindIndex(i => Igual(i.Item, item));
        if (existente >= 0)
        {
            if (!_itens[existente].Motivos.Contains(motivo))
            {
                _itens[existente].Motivos.Add(motivo);
            }

            return;
        }

        if (_itens.Any(i => i.Item.EhPasta && Protecao.Dentro(item.Caminho, i.Item.Caminho)))
        {
            return;
        }

        if (item.EhPasta)
        {
            _itens.RemoveAll(i => Protecao.Dentro(i.Item.Caminho, item.Caminho));
        }

        _itens.Add((item, [motivo]));
    }

    public void Tirar(IEnumerable<int> numeros)
    {
        var sair = Itens.Where(i => numeros.Contains(i.Numero)).Select(i => i.Item.Caminho).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _itens.RemoveAll(i => sair.Contains(i.Item.Caminho));
    }

    public void Limpar() => _itens.Clear();

    private static bool Igual(ItemAcao a, ItemAcao b) => string.Equals(a.Caminho, b.Caminho, StringComparison.OrdinalIgnoreCase);
}
