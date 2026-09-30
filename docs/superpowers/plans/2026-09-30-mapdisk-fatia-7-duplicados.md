# Plano da fatia 7: duplicados

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** uma aba "Duplicados" no painel de análises acha arquivos com o mesmo conteúdo dentro da pasta selecionada, só quando o técnico pede, e mostra quanto espaço as cópias ocupam. O técnico seleciona as cópias e trata pelas ações da fatia 5, sempre deixando ao menos uma. As cópias também entram no "Sugerir" do relatório para o cliente.

**Arquitetura:** a busca fica no núcleo, em `src/mapdisk.nucleo/analises/duplicados.cs`, em três etapas (R15): mesmo tamanho (sem ler o disco), resumo do primeiro 1 MB e resumo do arquivo inteiro. A leitura do conteúdo fica atrás de `ILeitorConteudo`, para os testes contarem leituras e a demonstração não ler o disco. O estado da aba fica em `painel/painel-duplicados.cs`, sem WPF.

**Tecnologia:** C# com .NET 8, WPF, xUnit, `System.Security.Cryptography` (SHA-256, do próprio .NET). Sem pacote NuGet novo.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, R15, seções 5 (fluxo, item 4), 6 (aba Duplicados) e 10 (só o Duplicados lê conteúdo, sem guardar nada dele). Relatório para o cliente: `2026-09-30-relatorio-cliente-design.md`, seção 10.

## Restrições globais

- Regras do produto do `AGENTS.md`. Pesam a 1 (sempre sobra uma cópia; agir passa pela confirmação da fatia 5), a 7 (o conteúdo só é lido quando o técnico pede; paralelismo com teto, menor em rede; arquivo só na nuvem nunca é lido) e a 9 (hard link repetido não é duplicado: é o mesmo arquivo).
- Do conteúdo, o programa guarda só o resumo (SHA-256) na memória, enquanto o resultado estiver na tela.
- Testes só dentro da pasta de saída dos testes. Resultado de teste no computador do Manfred fica na conversa.
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Quando roda | Só pelo botão "Procurar duplicados" da aba, sobre a pasta das análises (a selecionada, ou a raiz mostrada). Cancelar a qualquer momento | Regra 7 e spec, seção 5 |
| Tamanho mínimo | 1 MB por padrão, editável na aba. Arquivo vazio nunca entra | Arquivo pequeno repetido quase não libera espaço e multiplica leituras |
| Resumo | SHA-256 do próprio .NET | Sem pacote. Colisão na prática não existe |
| Etapas | 1) mesmo tamanho, pela árvore; 2) resumo do primeiro 1 MB; 3) resumo do arquivo inteiro, só para arquivos maiores que 1 MB que passaram na etapa 2 | R15. Arquivo de até 1 MB já foi lido inteiro na etapa 2 |
| O que não entra | Hard link repetido, link, arquivo só na nuvem e arquivo do sistema (R6) | Regras 7 e 9; o arquivo do sistema as ações bloqueiam |
| Paralelismo | 4 leituras ao mesmo tempo em disco local, 2 em caminho de rede | Regra 7 |
| Arquivo que não abre | Fica fora do grupo e entra na conta "N arquivos não puderam ser lidos" | Regra 3: não afirmar o que não foi lido |
| Resultado | Grupos numerados do que mais ocupa em cópias para o que menos; cada linha é um arquivo, com o número do grupo e a cor de fundo alternando por grupo | Lista simples, que a seleção múltipla e as ações já entendem |
| "Selecionar as cópias" | Seleciona, em cada grupo, todos menos o mais antigo pela data de alteração (empate: o de caminho menor na ordem alfabética) | O mais antigo costuma ser o original |
| Sempre sobra uma | Se todas as cópias de um grupo estão selecionadas, as ações bloqueiam: "Todas as cópias do grupo N estão selecionadas. Deixe ao menos uma." | Regra 1 |
| Depois da ação | O que saiu some dos grupos; grupo com menos de dois arquivos sai | A lista continua certa sem procurar de novo |
| Nova varredura | Limpa o resultado | Os caminhos podem ter mudado |
| Relatório para o cliente | O "Sugerir" acrescenta, de cada grupo dentro da pasta do relatório, as cópias menos a mais antiga, com o motivo "cópia repetida de <caminho mantido>". Só se a busca já rodou | Pendência da fatia 6 |
| Demonstração | Um leitor que não abre arquivo: o resumo é o próprio tamanho, então arquivos de mesmo tamanho viram grupo. A árvore de demonstração ganha uma cópia para aparecer um grupo | A demonstração nunca toca no disco |

## Git desta fatia

1. O plano entra pelo ramo `fatia-7-plano`, num Pull Request só do plano, com a linha da fatia 7 no README.
2. O código sai do `main` atualizado, no ramo `duplicados`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/analises/leitor-conteudo.cs` | `ILeitorConteudo`, o leitor real e o da demonstração |
| `src/mapdisk.nucleo/analises/duplicados.cs` | As três etapas e o resultado |
| `src/mapdisk.nucleo/painel/painel-duplicados.cs` | Estado da aba, linhas, "Selecionar as cópias" e o que sai depois da ação |
| `src/mapdisk.nucleo/painel/painel-analises.cs` | Dono do `PainelDuplicados` |
| `src/mapdisk.nucleo/painel/painel-principal.cs`, `dependencias-painel.cs`, `demonstracao.cs` | Leitor nas dependências, `Concluir` tira dos grupos, demonstração com cópia |
| `src/mapdisk.nucleo/acoes/preparador-acoes.cs` | Linha de duplicado vira item; bloqueio de todas as cópias |
| `src/mapdisk.nucleo/relatorios/sugestao.cs` e `painel/painel-relatorio.cs` | Cópias no "Sugerir" |
| `src/mapdisk/janela-principal.xaml` e `.cs` | Aba Duplicados e menu das linhas |
| `testes/mapdisk.testes/duplicados-testes.cs` | Testes desta fatia |

---

### Tarefa 1: leitor de conteúdo e as três etapas

**Arquivos:** novos `src/mapdisk.nucleo/analises/leitor-conteudo.cs` e `src/mapdisk.nucleo/analises/duplicados.cs`. Teste: novo `testes/mapdisk.testes/duplicados-testes.cs`.

**Interfaces:**
- Consome: `Analises.TodosOsArquivos`, `ArquivoEncontrado`, `MarcaArquivo`, `Alvo.EhRede`.
- Produz: `interface ILeitorConteudo { byte[] Resumo(string caminho, long bytes, CancellationToken cancelar); }` (resumo SHA-256 dos primeiros `bytes`; `long.MaxValue` é o arquivo inteiro); `sealed class LeitorConteudo : ILeitorConteudo` com `const int Parte = 1024 * 1024`; `sealed class LeitorDemonstracao : ILeitorConteudo`; `sealed record OpcoesDuplicados(long TamanhoMinimo = 1024 * 1024, int Paralelos = 4)` com `static OpcoesDuplicados Para(NoPasta pasta, long tamanhoMinimo)`; `sealed record GrupoDuplicados(int Numero, long Tamanho, IReadOnlyList<ArquivoEncontrado> Arquivos)` com `long Repetido` e `ArquivoEncontrado Mantido`; `sealed record ResultadoDuplicados(NoPasta Pasta, IReadOnlyList<GrupoDuplicados> Grupos, int NaoLidos, bool Cancelado)` com `long TotalRepetido`; `readonly record struct ProgressoDuplicados(string Etapa, int Feitos, int Total)`; `static class Duplicados` com `Task<ResultadoDuplicados> ProcurarAsync(NoPasta pasta, OpcoesDuplicados opcoes, ILeitorConteudo leitor, IProgress<ProgressoDuplicados>? progresso, CancellationToken cancelar)`.

- [ ] **Passo 1: criar o ramo do código**

```bash
git checkout main
git pull
git checkout -b duplicados
```

- [ ] **Passo 2: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class DuplicadosTestes
{
    private const int Mb = 1024 * 1024;

    private static NoPasta Varrer(string caminho) =>
        new MotorVarredura().Iniciar(Alvo.Normalizar(caminho, out _)!, CancellationToken.None).Conclusao.GetAwaiter().GetResult().Raiz;

    private static void Gravar(string caminho, int tamanho, byte valor, byte ultimo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        var dados = Enumerable.Repeat(valor, tamanho).ToArray();
        dados[^1] = ultimo;
        File.WriteAllBytes(caminho, dados);
    }

    // Conta as leituras e falha de propósito no caminho pedido.
    private sealed class LeitorContado(string? falharEm = null) : ILeitorConteudo
    {
        private readonly LeitorConteudo _real = new();

        public List<(string Nome, long Bytes)> Leituras { get; } = [];

        public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar)
        {
            lock (Leituras)
            {
                Leituras.Add((Path.GetFileName(caminho), bytes));
            }

            if (falharEm is not null && caminho.EndsWith(falharEm, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("em uso");
            }

            return _real.Resumo(caminho, bytes, cancelar);
        }
    }

    [Fact]
    public async Task Acha_so_os_iguais_de_verdade_nas_tres_etapas()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 2 * Mb, 1, 1);
        Gravar(pasta.Caminho(@"copia\a-copia.bin"), 2 * Mb, 1, 1);
        Gravar(pasta.Caminho("fim-diferente.bin"), 2 * Mb, 1, 9);
        Gravar(pasta.Caminho("inicio-diferente.bin"), 2 * Mb, 7, 1);
        Gravar(pasta.Caminho("unico.bin"), 3 * Mb, 1, 1);
        var raiz = Varrer(pasta.Raiz);
        var leitor = new LeitorContado();

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(), leitor, null, CancellationToken.None);

        var grupo = Assert.Single(r.Grupos);
        Assert.Equal(["a-copia.bin", "a.bin"], grupo.Arquivos.Select(a => a.Arquivo.Nome).Order());
        Assert.Equal(2 * Mb, grupo.Repetido);
        Assert.Equal(1, grupo.Numero);
        Assert.DoesNotContain(leitor.Leituras, l => l.Nome == "unico.bin");
        Assert.DoesNotContain(leitor.Leituras, l => l.Nome == "inicio-diferente.bin" && l.Bytes == long.MaxValue);
        Assert.Contains(leitor.Leituras, l => l.Nome == "fim-diferente.bin" && l.Bytes == long.MaxValue);
    }

    [Fact]
    public async Task Arquivo_pequeno_e_lido_uma_vez_e_o_minimo_filtra()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("x.txt"), 100, 3, 3);
        Gravar(pasta.Caminho(@"sub\y.txt"), 100, 3, 3);
        var raiz = Varrer(pasta.Raiz);
        var leitor = new LeitorContado();

        var comMinimo = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(), leitor, null, CancellationToken.None);
        var semMinimo = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);

        Assert.Empty(comMinimo.Grupos);
        Assert.Single(semMinimo.Grupos);
        Assert.Equal(2, leitor.Leituras.Count);
    }

    [Fact]
    public async Task Arquivo_que_nao_abre_fica_fora_e_e_contado()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("b.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("c.bin"), 100, 1, 1);
        var raiz = Varrer(pasta.Raiz);

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), new LeitorContado("c.bin"), null, CancellationToken.None);

        Assert.Equal(2, Assert.Single(r.Grupos).Arquivos.Count);
        Assert.Equal(1, r.NaoLidos);
    }

    [Fact]
    public async Task Hard_link_nuvem_e_sistema_nao_entram()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        pasta.HardLink("a-link.bin", "a.bin");
        var raiz = Varrer(pasta.Raiz);
        var nuvem = new NoPasta(@"D:\", null);
        nuvem.Preencher(
            [
                new ArquivoInfo("n1.bin", 100, 0, new DateTime(2020, 1, 1), MarcaArquivo.NaNuvem),
                new ArquivoInfo("n2.bin", 100, 0, new DateTime(2020, 1, 1), MarcaArquivo.NaNuvem),
                new ArquivoInfo("pagefile.sys", 100, 100, new DateTime(2020, 1, 1), MarcaArquivo.Sistema),
                new ArquivoInfo("swapfile.sys", 100, 100, new DateTime(2020, 1, 1), MarcaArquivo.Sistema),
            ],
            []);
        var leitor = new LeitorContado();

        var r1 = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);
        var r2 = await Duplicados.ProcurarAsync(nuvem, new OpcoesDuplicados(TamanhoMinimo: 1), leitor, null, CancellationToken.None);

        Assert.Empty(r1.Grupos);
        Assert.Empty(r2.Grupos);
        Assert.Empty(leitor.Leituras);
    }

    [Fact]
    public async Task Cancelar_devolve_sem_grupos()
    {
        using var pasta = new PastaTeste();
        Gravar(pasta.Caminho("a.bin"), 100, 1, 1);
        Gravar(pasta.Caminho("b.bin"), 100, 1, 1);
        var raiz = Varrer(pasta.Raiz);
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        var r = await Duplicados.ProcurarAsync(raiz, new OpcoesDuplicados(TamanhoMinimo: 1), new LeitorConteudo(), null, cancelar.Token);

        Assert.True(r.Cancelado);
        Assert.Empty(r.Grupos);
    }

    [Fact]
    public void Rede_le_menos_ao_mesmo_tempo()
    {
        Assert.Equal(2, OpcoesDuplicados.Para(new NoPasta(@"\\srv\dados", null), Mb).Paralelos);
        Assert.Equal(4, OpcoesDuplicados.Para(new NoPasta(@"D:\", null), Mb).Paralelos);
    }
}
```

O teste de hard link usa o `HardLink` do `PastaTeste`. A varredura marca a segunda entrada como `LinkRepetido`, e `TodosOsArquivos` já a deixa de fora; com uma só, não há grupo.

- [ ] **Passo 3: rodar e ver falhar.** Run: `dotnet test mapdisk.sln -c Release --filter DuplicadosTestes`. Expected: FAIL na compilação.

- [ ] **Passo 4: implementar `leitor-conteudo.cs`**

```csharp
using System.Buffers;
using System.Security.Cryptography;

namespace MapDisk.Nucleo;

/// <summary>Resumo (SHA-256) dos primeiros bytes de um arquivo. long.MaxValue é o arquivo inteiro.</summary>
public interface ILeitorConteudo
{
    byte[] Resumo(string caminho, long bytes, CancellationToken cancelar);
}

/// <summary>
/// Lê o conteúdo em partes de 1 MB, sem guardar nada além do resumo. É a única leitura de
/// conteúdo do programa, e só roda quando o técnico pede os duplicados (regra 7).
/// </summary>
public sealed class LeitorConteudo : ILeitorConteudo
{
    public const int Parte = 1024 * 1024;

    public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar)
    {
        using var fluxo = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, Parte, FileOptions.SequentialScan);
        using var resumo = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(Parte);
        try
        {
            long lidos = 0;
            int n;
            while (lidos < bytes && (n = fluxo.Read(buffer, 0, (int)Math.Min(Parte, bytes - lidos))) > 0)
            {
                cancelar.ThrowIfCancellationRequested();
                resumo.AppendData(buffer, 0, n);
                lidos += n;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return resumo.GetHashAndReset();
    }
}

/// <summary>Demonstração: não abre arquivo. O resumo é o tamanho, então arquivos de mesmo tamanho viram grupo.</summary>
public sealed class LeitorDemonstracao(Func<string, long> tamanhoDe) : ILeitorConteudo
{
    public byte[] Resumo(string caminho, long bytes, CancellationToken cancelar) => BitConverter.GetBytes(tamanhoDe(caminho));
}
```

- [ ] **Passo 5: implementar `duplicados.cs`**

```csharp
using System.Collections.Concurrent;

namespace MapDisk.Nucleo;

public sealed record OpcoesDuplicados(long TamanhoMinimo = 1024 * 1024, int Paralelos = 4)
{
    /// <summary>Em caminho de rede, menos leituras ao mesmo tempo, para não pesar no servidor (regra 7).</summary>
    public static OpcoesDuplicados Para(NoPasta pasta, long tamanhoMinimo) =>
        new(tamanhoMinimo, Alvo.EhRede(pasta.CaminhoCompleto()) ? 2 : 4);
}

public sealed record GrupoDuplicados(int Numero, long Tamanho, IReadOnlyList<ArquivoEncontrado> Arquivos)
{
    /// <summary>O que as cópias ocupam além do primeiro arquivo.</summary>
    public long Repetido => Tamanho * (Arquivos.Count - 1);

    /// <summary>O que fica: o mais antigo pela data de alteração; no empate, o de caminho menor.</summary>
    public ArquivoEncontrado Mantido => Arquivos
        .OrderBy(a => a.Arquivo.Modificacao)
        .ThenBy(a => a.Caminho, StringComparer.OrdinalIgnoreCase)
        .First();
}

public sealed record ResultadoDuplicados(NoPasta Pasta, IReadOnlyList<GrupoDuplicados> Grupos, int NaoLidos, bool Cancelado)
{
    public long TotalRepetido => Grupos.Sum(g => g.Repetido);
}

public readonly record struct ProgressoDuplicados(string Etapa, int Feitos, int Total);

/// <summary>
/// Duplicados em três etapas (R15): mesmo tamanho, pela árvore, sem ler o disco; resumo do
/// primeiro 1 MB; resumo do arquivo inteiro, só para quem é maior que 1 MB e passou na etapa 2.
/// </summary>
public static class Duplicados
{
    private const MarcaArquivo ForaDaBusca = MarcaArquivo.NaNuvem | MarcaArquivo.Link | MarcaArquivo.Sistema;

    public static async Task<ResultadoDuplicados> ProcurarAsync(
        NoPasta pasta, OpcoesDuplicados opcoes, ILeitorConteudo leitor, IProgress<ProgressoDuplicados>? progresso, CancellationToken cancelar)
    {
        var minimo = Math.Max(1, opcoes.TamanhoMinimo);
        var porTamanho = Analises.TodosOsArquivos(pasta)
            .Where(a => a.Arquivo.Tamanho >= minimo && (a.Arquivo.Marcas & ForaDaBusca) == 0)
            .GroupBy(a => a.Arquivo.Tamanho)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .ToList();
        var naoLidos = new int[1];
        try
        {
            var porInicio = await Separar(porTamanho, LeitorConteudo.Parte, "Conferindo o primeiro 1 MB", opcoes, leitor, progresso, naoLidos, cancelar);
            var pequenos = porInicio.Where(g => g[0].Arquivo.Tamanho <= LeitorConteudo.Parte).ToList();
            var grandes = porInicio.Where(g => g[0].Arquivo.Tamanho > LeitorConteudo.Parte).ToList();
            var porInteiro = await Separar(grandes, long.MaxValue, "Conferindo o arquivo inteiro", opcoes, leitor, progresso, naoLidos, cancelar);
            var grupos = pequenos.Concat(porInteiro)
                .OrderByDescending(g => g[0].Arquivo.Tamanho * (g.Count - 1))
                .Select((g, n) => new GrupoDuplicados(n + 1, g[0].Arquivo.Tamanho, g.OrderBy(a => a.Caminho, StringComparer.OrdinalIgnoreCase).ToList()))
                .ToList();
            return new ResultadoDuplicados(pasta, grupos, naoLidos[0], false);
        }
        catch (OperationCanceledException)
        {
            return new ResultadoDuplicados(pasta, [], naoLidos[0], true);
        }
    }

    // Lê cada arquivo dos grupos e separa os grupos pelo resumo. Arquivo que não abre sai e é contado.
    private static async Task<List<List<ArquivoEncontrado>>> Separar(
        List<List<ArquivoEncontrado>> grupos, long bytes, string etapa, OpcoesDuplicados opcoes, ILeitorConteudo leitor,
        IProgress<ProgressoDuplicados>? progresso, int[] naoLidos, CancellationToken cancelar)
    {
        var arquivos = grupos.SelectMany(g => g).ToList();
        var resumos = new ConcurrentDictionary<ArquivoEncontrado, string>();
        var feitos = 0;
        cancelar.ThrowIfCancellationRequested();
        await Parallel.ForEachAsync(arquivos, new ParallelOptions { MaxDegreeOfParallelism = opcoes.Paralelos, CancellationToken = cancelar }, (a, ct) =>
        {
            try
            {
                resumos[a] = Convert.ToHexString(leitor.Resumo(a.Caminho, bytes, ct));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Interlocked.Increment(ref naoLidos[0]);
            }

            progresso?.Report(new ProgressoDuplicados(etapa, Interlocked.Increment(ref feitos), arquivos.Count));
            return ValueTask.CompletedTask;
        });
        return grupos
            .SelectMany(g => g.Where(resumos.ContainsKey).GroupBy(a => resumos[a]).Where(x => x.Count() > 1).Select(x => x.ToList()))
            .ToList();
    }
}
```

No teste do arquivo pequeno, a primeira busca (mínimo de 1 MB) não lê nada, e a segunda (mínimo de 1 byte) lê cada arquivo uma vez, só na etapa 2: duas leituras no total.

- [ ] **Passo 6: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 7: commit** `Cria a busca de duplicados em tres etapas`.

---

### Tarefa 2: estado da aba e a regra de sempre sobrar uma

**Arquivos:** novo `src/mapdisk.nucleo/painel/painel-duplicados.cs`; `painel-analises.cs`, `preparador-acoes.cs`, `painel-principal.cs`, `dependencias-painel.cs`, `demonstracao.cs`. Teste: acrescentar em `duplicados-testes.cs`.

**Interfaces:**
- Consome: tarefa 1, `ItemAcao`, `Protecao.Dentro`.
- Produz: `sealed record LinhaDuplicado(int Grupo, int ArquivosNoGrupo, ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, bool Alternada)`; `sealed class PainelDuplicados : INotifyPropertyChanged` com `PainelDuplicados(ILeitorConteudo leitor)`, `long TamanhoMinimoMb { get; set; } = 1`, `bool Procurando`, `string TextoEstado`, `IReadOnlyList<LinhaDuplicado> Linhas`, `ResultadoDuplicados? Resultado`, `Task ProcurarAsync(NoPasta pasta)`, `void Cancelar()`, `void Limpar()`, `IReadOnlyList<LinhaDuplicado> Copias()` e `void Tirar(IEnumerable<ItemAcao> itens)`; `PainelAnalises(ILeitorConteudo? leitor = null)` com `PainelDuplicados Duplicados`; `DependenciasPainel.Leitor` (padrão `new LeitorConteudo()`); em `PreparadorAcoes.ItensDaSelecao`, o caso `LinhaDuplicado`; em `PreparadorAcoes.Avaliar`, o bloqueio "Todas as cópias do grupo N estão selecionadas. Deixe ao menos uma."; em `PainelPrincipal.Concluir`, `Analises.Duplicados.Tirar(...)` com os itens que deram certo.

- [ ] **Passo 1: escrever os testes**

```csharp
    private static async Task<(PainelDuplicados Painel, PastaTeste Pasta)> PainelComGrupo()
    {
        var pasta = new PastaTeste();
        Gravar(pasta.Caminho("velho.bin"), 100, 1, 1);
        File.SetLastWriteTime(pasta.Caminho("velho.bin"), new DateTime(2020, 1, 1));
        Gravar(pasta.Caminho(@"b\novo.bin"), 100, 1, 1);
        Gravar(pasta.Caminho(@"c\novo2.bin"), 100, 1, 1);
        var p = new PainelDuplicados(new LeitorConteudo()) { TamanhoMinimoMb = 0 };
        await p.ProcurarAsync(Varrer(pasta.Raiz));
        return (p, pasta);
    }

    [Fact]
    public async Task Painel_mostra_o_grupo_e_seleciona_as_copias_menos_a_mais_antiga()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            Assert.Equal(3, p.Linhas.Count);
            Assert.All(p.Linhas, l => Assert.Equal(1, l.Grupo));
            Assert.Equal("1 grupo com 200 Bytes em cópias", p.TextoEstado);
            Assert.Equal(["novo.bin", "novo2.bin"], p.Copias().Select(l => l.Nome).Order());
        }
    }

    [Fact]
    public async Task Tirar_depois_da_acao_desfaz_o_grupo_que_ficou_com_um()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            p.Tirar(p.Copias().Select(l => ItemAcao.DoArquivo(l.Encontrado.Pasta, l.Encontrado.Arquivo)));

            Assert.Empty(p.Linhas);
            Assert.Equal("Nenhum arquivo repetido acima do tamanho mínimo.", p.TextoEstado);
        }
    }

    [Fact]
    public async Task Selecionar_todas_as_copias_de_um_grupo_bloqueia()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            var preparador = new PreparadorAcoes(new LocaisProtegidos([], @"Z:\registro"), _ => DriveType.Fixed, new OperacoesDemonstracao(), false);

            Assert.Equal("Todas as cópias do grupo 1 estão selecionadas. Deixe ao menos uma.", preparador.Avaliar(p.Linhas).Bloqueio);
            Assert.Null(preparador.Avaliar(p.Copias()).Bloqueio);
            Assert.Equal(2, preparador.Avaliar(p.Copias()).Itens.Count);
        }
    }

    [Fact]
    public void Antes_de_procurar_explica_o_que_a_busca_faz()
    {
        var p = new PainelDuplicados(new LeitorConteudo());

        Assert.Equal("Clique em Procurar duplicados. A busca lê o conteúdo só dos arquivos de mesmo tamanho.", p.TextoEstado);
        Assert.False(p.Procurando);
    }
```

`PastaTeste` é `IDisposable`: a tupla devolve a pasta para o teste apagar no fim com o `using`.

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar `painel-duplicados.cs`**

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public sealed record LinhaDuplicado(int Grupo, int ArquivosNoGrupo, ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, bool Alternada);

/// <summary>Estado da aba Duplicados, sem WPF. A busca só roda pelo botão (regra 7).</summary>
public sealed class PainelDuplicados : INotifyPropertyChanged
{
    private const long Mb = 1024 * 1024;
    private readonly ILeitorConteudo _leitor;
    private CancellationTokenSource? _cancelar;
    private List<GrupoDuplicados> _grupos = [];
    private string _progresso = string.Empty;

    public PainelDuplicados(ILeitorConteudo leitor) => _leitor = leitor;

    public event PropertyChangedEventHandler? PropertyChanged;

    public long TamanhoMinimoMb { get; set; } = 1;

    public bool Procurando { get; private set; }

    public ResultadoDuplicados? Resultado { get; private set; }

    public IReadOnlyList<LinhaDuplicado> Linhas => _grupos.SelectMany(g => g.Arquivos.Select(a => new LinhaDuplicado(
        g.Numero, g.Arquivos.Count, a, a.Arquivo.Nome, a.Pasta.CaminhoCompleto(), Formatador.Tamanho(g.Tamanho),
        Formatador.Data(a.Arquivo.Modificacao), g.Numero % 2 == 0))).ToList();

    public string TextoEstado
    {
        get
        {
            if (Procurando)
            {
                return _progresso;
            }

            if (Resultado is not { } r)
            {
                return "Clique em Procurar duplicados. A busca lê o conteúdo só dos arquivos de mesmo tamanho.";
            }

            if (r.Cancelado)
            {
                return "Busca cancelada. Nada foi mostrado.";
            }

            var texto = _grupos.Count == 0
                ? "Nenhum arquivo repetido acima do tamanho mínimo."
                : $"{Formatador.Plural(_grupos.Count, "grupo", "grupos")} com {Formatador.Tamanho(_grupos.Sum(g => g.Repetido))} em cópias";
            return r.NaoLidos > 0
                ? $"{texto} {Formatador.Plural(r.NaoLidos, "arquivo não pôde ser lido e ficou", "arquivos não puderam ser lidos e ficaram")} fora."
                : texto;
        }
    }

    public async Task ProcurarAsync(NoPasta pasta)
    {
        Cancelar();
        var cancelar = _cancelar = new CancellationTokenSource();
        Procurando = true;
        _progresso = "Separando os arquivos de mesmo tamanho...";
        Avisar();
        var progresso = new Progress<ProgressoDuplicados>(p =>
        {
            _progresso = $"{p.Etapa}: {p.Feitos} de {p.Total}";
            Avisar();
        });
        var r = await Task.Run(() => Duplicados.ProcurarAsync(pasta, OpcoesDuplicados.Para(pasta, Math.Max(1, TamanhoMinimoMb * Mb)), _leitor, progresso, cancelar.Token));
        if (_cancelar != cancelar)
        {
            return;
        }

        Resultado = r;
        _grupos = r.Grupos.ToList();
        Procurando = false;
        Avisar();
    }

    public void Cancelar() => _cancelar?.Cancel();

    public void Limpar()
    {
        Cancelar();
        _cancelar = null;
        Resultado = null;
        _grupos = [];
        Procurando = false;
        Avisar();
    }

    /// <summary>Em cada grupo, todos menos o mais antigo pela data de alteração (empate: o de caminho menor).</summary>
    public IReadOnlyList<LinhaDuplicado> Copias()
    {
        var manter = _grupos.Select(g => g.Mantido).ToHashSet();
        return Linhas.Where(l => !manter.Contains(l.Encontrado)).ToList();
    }

    /// <summary>Tira o que a ação removeu. Grupo com menos de dois arquivos sai.</summary>
    public void Tirar(IEnumerable<ItemAcao> itens)
    {
        var caminhos = itens.Select(i => i.Caminho).ToList();
        _grupos = _grupos
            .Select(g => g with { Arquivos = g.Arquivos.Where(a => !caminhos.Any(c => Protecao.Dentro(a.Caminho, c))).ToList() })
            .Where(g => g.Arquivos.Count > 1)
            .ToList();
        Avisar();
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
```

`Formatador.Tamanho(200)` sai "200 Bytes": abaixo de 1 KB, o formatador escreve em bytes.

- [ ] **Passo 4: ligar ao resto.**
  - `painel-analises.cs`: construtor `public PainelAnalises(ILeitorConteudo? leitor = null) => Duplicados = new PainelDuplicados(leitor ?? new LeitorConteudo());` e a propriedade `public PainelDuplicados Duplicados { get; }`. Em `Aguardar()`, `Duplicados.Limpar();`. A seleção de outra pasta não limpa o resultado: ele diz de qual pasta é.
  - `dependencias-painel.cs`: `public ILeitorConteudo Leitor { get; init; } = new LeitorConteudo();`.
  - `painel-principal.cs`: `Analises` passa a ser criado no construtor, `Analises = new PainelAnalises(dependencias.Leitor);` (a propriedade perde o `= new()`); em `Concluir`, depois do laço que tira da árvore: `Analises.Duplicados.Tirar(resumo.Resultados.Where(r => r.Ok).Select(r => r.Item));`.
  - `preparador-acoes.cs`: em `ItensDaSelecao`, antes do caso `LinhaArquivo`:

```csharp
                case LinhaDuplicado d:
                    itens.Add(ItemAcao.DoArquivo(d.Encontrado.Pasta, d.Encontrado.Arquivo));
                    break;
```

  e em `Avaliar`, antes do `return` que chama `AvaliarItens`:

```csharp
        // Sempre sobra uma cópia (regra 1).
        var lista = selecionados as IList<object> ?? selecionados.ToList();
        var todasAsCopias = lista.OfType<LinhaDuplicado>().GroupBy(d => d.Grupo).FirstOrDefault(g => g.Count() >= g.First().ArquivosNoGrupo);
        if (todasAsCopias is not null)
        {
            return Bloqueado($"Todas as cópias do grupo {todasAsCopias.Key} estão selecionadas. Deixe ao menos uma.");
        }
```

  Para isso, `Avaliar` converte `selecionados` em lista uma vez e passa a lista a `ItensDaSelecao`.
  - `demonstracao.cs`: o painel usa `Leitor = new LeitorDemonstracao(caminho => ...)`, que devolve o tamanho do arquivo na árvore de demonstração (procurar pelo nome na árvore montada), e a árvore ganha uma cópia de um arquivo que já existe, com o mesmo tamanho, em outra pasta (por exemplo, o maior `.zip` de `Dados` repetido em `Users\ana.souza\Downloads`, com uma data mais nova). Conferir na árvore de demonstração que existe ao menos um par de mesmo tamanho acima de 1 MB depois da mudança.

- [ ] **Passo 5: rodar e ver passar.** Run: `dotnet test mapdisk.sln -c Release`. Expected: PASS em todos, inclusive os da fatia 5.

- [ ] **Passo 6: commit** `Cria o estado da aba de duplicados`.

---

### Tarefa 3: cópias no "Sugerir" do relatório para o cliente

**Arquivos:** `src/mapdisk.nucleo/relatorios/sugestao.cs`, `src/mapdisk.nucleo/painel/painel-relatorio.cs`. Teste: acrescentar em `duplicados-testes.cs`.

**Interfaces:**
- Produz: `Sugestao.Sugerir(ListaAvaliacao lista, NoPasta pasta, CriteriosSugestao criterios, DateTime hoje, ResultadoDuplicados? duplicados = null)`; `PainelRelatorio(NoPasta pasta, long? livre, string tecnico, LocaisProtegidos? locais = null, ResultadoDuplicados? duplicados = null)`.

- [ ] **Passo 1: escrever o teste**

```csharp
    [Fact]
    public async Task Sugerir_acrescenta_as_copias_e_mantem_a_mais_antiga()
    {
        var (p, pasta) = await PainelComGrupo();
        using (pasta)
        {
            var lista = new ListaAvaliacao();
            var raiz = p.Resultado!.Pasta;

            Sugestao.Sugerir(lista, raiz, new CriteriosSugestao(MaioresPastas: 0, MaioresArquivos: 0, TamanhoMinimo: long.MaxValue), DateTime.Now, p.Resultado);

            Assert.Equal(["novo.bin", "novo2.bin"], lista.Itens.Select(i => i.Item.Nome).Order());
            Assert.All(lista.Itens, i => Assert.Equal($"cópia repetida de {pasta.Caminho("velho.bin")}", i.Motivo));
        }
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação (a sobrecarga não existe).

- [ ] **Passo 3: implementar.** Em `Sugestao.Sugerir`, parâmetro novo `ResultadoDuplicados? duplicados = null` e, no fim:

```csharp
        // Cópias repetidas, se a busca de duplicados já rodou: fica a mais antiga de cada grupo.
        foreach (var grupo in duplicados?.Grupos ?? [])
        {
            var manter = grupo.Mantido;
            foreach (var copia in grupo.Arquivos.Where(a => a != manter && Protecao.Dentro(a.Caminho, pasta.CaminhoCompleto())))
            {
                lista.Acrescentar(ItemAcao.DoArquivo(copia.Pasta, copia.Arquivo), $"cópia repetida de {manter.Caminho}");
            }
        }
```

  Em `PainelRelatorio`, o parâmetro `duplicados` fica num campo e vai para `Sugestao.Sugerir`. A regra de "mais antiga" é a mesma de `PainelDuplicados.Copias()`, pelo `GrupoDuplicados.Mantido` da tarefa 1.

- [ ] **Passo 4: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 5: commit** `Leva as copias repetidas ao relatorio para o cliente`.

---

### Tarefa 4: aba Duplicados na janela

**Arquivos:** `src/mapdisk/janela-principal.xaml` e `.xaml.cs`. Teste: acrescentar em `recursos-testes.cs`.

- [ ] **Passo 1: teste de recurso**

```csharp
    [Fact]
    public void Janela_tem_a_aba_de_duplicados()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Header=\"Duplicados\"", janela);
        Assert.Contains("Click=\"AoProcurarDuplicados\"", janela);
        Assert.Contains("Click=\"AoSelecionarCopias\"", janela);
        Assert.Contains("x:Key=\"MenuDuplicado\"", janela);
    }
```

- [ ] **Passo 2: aba.** Depois de "Por usuário", `TabItem Header="Duplicados"` com `DataContext="{Binding Duplicados}"`:
  - linha de cima: "Tamanho mínimo (MB)" com `TextBox` ligado a `TamanhoMinimoMb`, botões **Procurar duplicados** (`AoProcurarDuplicados`, desligado durante a busca), **Cancelar** (`AoCancelarDuplicados`, visível só durante a busca) e **Selecionar as cópias** (`AoSelecionarCopias`);
  - `TextoEstado` embaixo dela; `ProgressBar` indeterminada durante a busca;
  - `ListView x:Name="ListaDuplicados"` com `SelectionMode="Extended"`, `SelectionChanged="AoSelecionarParaAcao"`, `KeyDown="AoTeclarNaLista"` e as colunas Grupo, Tamanho, Nome, Última alteração e Pasta; o estilo da linha usa fundo `PincelVerdeFundo` quando `Alternada` é verdadeiro, dica com a pasta e `ContextMenu` = `MenuDuplicado`.
  - Nos recursos da janela, `MenuDuplicado` (`x:Shared="False"`) com Mostrar no Explorer, Copiar caminho, Abrir a pasta na árvore, Apagar... e Mover..., nos mesmos manipuladores de `MenuArquivo`.

- [ ] **Passo 3: código.**

```csharp
    private async void AoProcurarDuplicados(object sender, RoutedEventArgs e)
    {
        if (_painel.Estado != EstadoPainel.Parado || _painel.PastaDasAnalises(Tabela.SelectedItem as LinhaArvore) is not { } pasta)
        {
            MessageBox.Show(this, "Varra a pasta primeiro e espere o fim da varredura.", "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _painel.Analises.Duplicados.ProcurarAsync(pasta);
    }

    private void AoCancelarDuplicados(object sender, RoutedEventArgs e) => _painel.Analises.Duplicados.Cancelar();

    private void AoSelecionarCopias(object sender, RoutedEventArgs e)
    {
        var copias = _painel.Analises.Duplicados.Copias().ToHashSet();
        ListaDuplicados.SelectedItems.Clear();
        foreach (var linha in ListaDuplicados.Items.Cast<LinhaDuplicado>().Where(copias.Contains))
        {
            ListaDuplicados.SelectedItems.Add(linha);
        }

        ListaDuplicados.Focus();
    }

    // O arquivo da linha em que o menu foi aberto, seja de Maiores, Antigos ou Duplicados.
    private static ArquivoEncontrado? ArquivoDoMenu(object sender) => ItemDoMenu(sender) switch
    {
        LinhaArquivo l => l.Encontrado,
        LinhaDuplicado d => d.Encontrado,
        _ => null,
    };
```

  Os manipuladores `AoMostrarArquivoNoExplorer`, `AoCopiarCaminhoDoArquivo` e `AoAbrirPastaDoArquivo` passam a usar `ArquivoDoMenu(sender)` em vez de `ItemDoMenu(sender) is LinhaArquivo`. A comparação `copias.Contains` funciona porque `LinhaDuplicado` é um record: as linhas de `Copias()` e as da lista são iguais por valor.

  Em `AoGerarRelatorio`, `new PainelRelatorio(..., _painel.Locais, _painel.Analises.Duplicados.Resultado)`.

- [ ] **Passo 4: build e conferência pelo agente em `--demonstracao`.** Varrer, abrir a aba Duplicados, Procurar, ver o grupo da cópia acrescentada na tarefa 2, "Selecionar as cópias", "Apagar selecionados" pela confirmação (a demonstração não toca no disco) e o grupo sumir. Selecionar as duas linhas do grupo e ver o bloqueio "Deixe ao menos uma".

- [ ] **Passo 5: conferência pelo agente numa pasta de teste real.** Criar em `testes/mapdisk.testes/bin/Release/.../arvores-teste` uma pasta com cópias de arquivos, varrer pelo programa e procurar duplicados. Só a busca, sem ação. Apagar a pasta no fim.

- [ ] **Passo 6: commit** `Liga a aba de duplicados na janela`.

---

### Tarefa 5: documentação e Pull Request

- [ ] **Passo 1:** README: aba Duplicados no "Uso" (o que faz, que lê o conteúdo só quando você pede, "Selecionar as cópias" e a regra de sempre sobrar uma), o critério novo do relatório para o cliente, e a linha da fatia 7 na "Situação do projeto".
- [ ] **Passo 2:** pendências: fechar "Duplicados como critério do Sugerir"; registrar o que ficou fora (por exemplo, guardar o tamanho mínimo entre sessões, que vai para Opções da fatia 8).
- [ ] **Passo 3:** portões, commit `Traz a documentacao da fatia 7` e Pull Request do ramo `duplicados`, com "O que muda", "Como testar" (uma pasta de teste com cópias, busca, seleção das cópias, Lixeira e o bloqueio de todas as cópias) e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec

| Item | Onde |
|---|---|
| R15, três etapas: mesmo tamanho, primeiro 1 MB, arquivo inteiro | Tarefa 1 |
| R15, sob demanda | Tarefas 2 e 4 (só pelo botão) |
| Seção 6: aba Duplicados no painel de análises | Tarefa 4 |
| Seção 10: só o Duplicados lê conteúdo, sem guardar nada dele | Tarefa 1 (só o resumo, na memória) |
| Regra 1: sempre sobra uma cópia, ação pela confirmação | Tarefa 2 |
| Regra 7: paralelismo com teto e menor em rede; nuvem nunca lida | Tarefa 1 |
| Regra 9: hard link não é duplicado | Tarefa 1 |
| Spec do relatório para o cliente, seção 10: duplicados no "Sugerir" | Tarefa 3 |
