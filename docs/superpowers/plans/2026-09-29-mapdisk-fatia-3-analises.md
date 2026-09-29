# Plano da fatia 3: painel de análises e memória

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** ao lado da árvore, um painel com quatro abas responde direto a pergunta "o que eu trato?": os maiores arquivos, os arquivos antigos (com o total em GB), o resumo por tipo e o resumo por usuário, sempre sobre a pasta selecionada. Junto vem a tentativa de trazer a memória para a meta.

**Arquitetura:** as análises ficam no núcleo, em `src/mapdisk.nucleo/analises/`, como funções que recebem uma pasta da árvore já lida e devolvem resultados prontos para mostrar. Nada é lido do disco de novo. O `PainelAnalises` guarda a aba, a idade escolhida e as linhas de cada aba, e calcula fora da tela. A janela ganha o painel à direita, com divisória arrastável.

**Tecnologia:** C# com .NET 8, WPF, xUnit.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, R11 a R14 e a fatia 3 da seção 11. Pendência de memória em `docs/superpowers/pendencias.md`.

## Restrições globais

- Regras do produto do `AGENTS.md`. Nesta fatia pesam a 3 (nunca 0 onde não houve leitura: a aba avisa quando há pastas sem leitura na conta) e a 7 (as análises usam a árvore em memória, sem ler o disco de novo).
- Continua só lendo: nada é apagado nem movido.
- Testes só dentro da pasta de saída dos testes. Resultado de teste manual no computador do Manfred fica na conversa.
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Sobre o que as abas calculam | A pasta selecionada na árvore. Sem seleção, a raiz mostrada | Spec, seção 6 |
| Quando calculam | Ao abrir a aba, ao trocar a seleção e ao fim da varredura. Durante a varredura, a aba diz "Aguarde o fim da varredura" | Número parcial numa lista de "maiores" engana |
| Quantos maiores arquivos | 100. A escolha do número fica para a tela de Opções da fatia 7 | R11 |
| Hard link repetido | Fica fora das listas e das somas das análises, como fica fora dos totais da árvore | Regra 9 |
| Data que conta como "antigo" | Última modificação do arquivo. Arquivo sem data não entra | É a data que o sistema de arquivos dá sem abrir o arquivo (regra 7) |
| Pasta de perfis | Uma pasta chamada `Users` na raiz de uma unidade, ou a própria pasta selecionada quando ela se chama `Users` | R14. Perfil sem acesso aparece como "sem acesso", nunca como 0 |
| Pastas sem leitura na conta | Quando a pasta analisada tem pastas sem acesso ou com erro dentro, a aba mostra "N pastas sem leitura não entram nesta conta" | Regra 3 |
| Memória | Nomes de arquivo iguais passam a ser guardados uma vez só por varredura. A mudança só fica se a medida mostrar ganho | Pendência da fatia 1 |

## Git desta fatia

1. O plano entra pelo ramo `fatia-3-analises`, num Pull Request só do plano, junto com a "Situação do projeto" do README corrigida.
2. O código sai do `main` atualizado, no ramo `analises`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/analises/arquivo-encontrado.cs` | Um arquivo achado por uma análise, com a pasta onde está e o caminho |
| `src/mapdisk.nucleo/analises/categorias.cs` | Categoria de cada extensão |
| `src/mapdisk.nucleo/analises/analises.cs` | Maiores arquivos, arquivos antigos, por tipo, por usuário |
| `src/mapdisk.nucleo/painel/painel-analises.cs` | Estado das abas e linhas prontas para a tela |
| `src/mapdisk.nucleo/painel/painel-principal.cs` | Dono do `PainelAnalises`, avisa quando a varredura termina |
| `src/mapdisk.nucleo/varredura/conjunto-nomes.cs` | Nomes de arquivo guardados uma vez só (tarefa 6) |
| `src/mapdisk/janela-principal.xaml` e `.xaml.cs` | Painel de análises à direita |

---

### Tarefa 1: arquivo encontrado e maiores arquivos

**Arquivos:** novos `arquivo-encontrado.cs` e `analises.cs`. Teste: novo `testes/mapdisk.testes/analises-testes.cs`.

**Interfaces:**
- Produz: `sealed record ArquivoEncontrado(NoPasta Pasta, ArquivoInfo Arquivo)` com `string Caminho`; `static class Analises` com `IReadOnlyList<ArquivoEncontrado> MaioresArquivos(NoPasta pasta, int quantos = 100)` e `IEnumerable<ArquivoEncontrado> TodosOsArquivos(NoPasta pasta)`.

- [ ] **Passo 1: criar o ramo do código**

```bash
git switch main
git pull
git switch -c analises
```

- [ ] **Passo 2: teste que falha, `analises-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class AnalisesTestes
{
    internal static readonly DateTime Hoje = new(2026, 9, 29);

    /// <summary>
    /// C:\ com video.mp4 (5000, 2023), Users\ana\foto.jpg (300, 2026), Users\ana\copia.jpg
    /// (hard link repetido de 300), Users\bruno\caixa.pst (2000, 2025), Users\bruno\setup.exe
    /// (100, sem data) e Windows sem acesso.
    /// </summary>
    internal static NoPasta Exemplo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([new("video.mp4", 5000, 8192, new DateTime(2023, 1, 10), MarcaArquivo.Nenhuma)], [users, windows]);
        windows.MarcarSemAcesso("acesso negado");
        var ana = new NoPasta("ana", users);
        var bruno = new NoPasta("bruno", users);
        users.Preencher([], [ana, bruno]);
        ana.Preencher(
            [
                new("foto.jpg", 300, 4096, new DateTime(2026, 9, 1), MarcaArquivo.Nenhuma),
                new("copia.jpg", 300, 4096, new DateTime(2026, 9, 1), MarcaArquivo.LinkRepetido),
            ],
            []);
        bruno.Preencher(
            [
                new("caixa.pst", 2000, 4096, new DateTime(2025, 6, 1), MarcaArquivo.Nenhuma),
                new("setup.exe", 100, 4096, DateTime.MinValue, MarcaArquivo.Nenhuma),
            ],
            []);
        return raiz;
    }

    [Fact]
    public void Maiores_arquivos_do_maior_para_o_menor_sem_hard_link_repetido()
    {
        var maiores = Analises.MaioresArquivos(Exemplo(), 3);

        Assert.Equal(["video.mp4", "caixa.pst", "foto.jpg"], maiores.Select(m => m.Arquivo.Nome));
        Assert.Equal(@"C:\Users\bruno\caixa.pst", maiores[1].Caminho);
    }

    [Fact]
    public void Maiores_arquivos_de_uma_subpasta()
    {
        var bruno = Exemplo().Subpastas[0].Subpastas[1];

        Assert.Equal(["caixa.pst", "setup.exe"], Analises.MaioresArquivos(bruno).Select(m => m.Arquivo.Nome));
    }
}
```

Rodar: `dotnet test mapdisk.sln -c Release`. Esperado: erro de compilação.

- [ ] **Passo 3: implementação**

`arquivo-encontrado.cs`:

```csharp
namespace MapDisk.Nucleo;

/// <summary>Um arquivo achado por uma análise, com a pasta da árvore onde ele está.</summary>
public sealed record ArquivoEncontrado(NoPasta Pasta, ArquivoInfo Arquivo)
{
    public string Caminho => Alvo.Juntar(Pasta.CaminhoCompleto(), Arquivo.Nome);
}
```

`analises.cs`:

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Análises sobre uma pasta já varrida. Usam só a árvore em memória: nada é lido do disco de
/// novo. Hard link repetido fica de fora, como fica fora dos totais da árvore.
/// </summary>
public static class Analises
{
    public static IEnumerable<ArquivoEncontrado> TodosOsArquivos(NoPasta pasta)
    {
        var pilha = new Stack<NoPasta>();
        pilha.Push(pasta);
        while (pilha.Count > 0)
        {
            var no = pilha.Pop();
            foreach (var arquivo in no.Arquivos)
            {
                if (arquivo.Soma)
                {
                    yield return new ArquivoEncontrado(no, arquivo);
                }
            }

            foreach (var sub in no.Subpastas)
            {
                pilha.Push(sub);
            }
        }
    }

    public static IReadOnlyList<ArquivoEncontrado> MaioresArquivos(NoPasta pasta, int quantos = 100) =>
        Maiores(TodosOsArquivos(pasta), quantos);

    // Guarda só os N maiores enquanto passa pelos arquivos: não ordena a lista inteira.
    private static IReadOnlyList<ArquivoEncontrado> Maiores(IEnumerable<ArquivoEncontrado> arquivos, int quantos)
    {
        var fila = new PriorityQueue<ArquivoEncontrado, long>();
        foreach (var a in arquivos)
        {
            if (fila.Count < quantos)
            {
                fila.Enqueue(a, a.Arquivo.Tamanho);
            }
            else if (fila.TryPeek(out _, out var menor) && a.Arquivo.Tamanho > menor)
            {
                fila.EnqueueDequeue(a, a.Arquivo.Tamanho);
            }
        }

        var lista = new List<ArquivoEncontrado>(fila.Count);
        while (fila.TryDequeue(out var a, out _))
        {
            lista.Add(a);
        }

        lista.Reverse();
        return lista;
    }
}
```

- [ ] **Passo 4: rodar e ver passar. Commit** `Cria a analise dos maiores arquivos`.

---

### Tarefa 2: arquivos antigos

**Arquivos:** `analises.cs`. Teste: `analises-testes.cs`.

**Interfaces:**
- Produz: `enum Idade { SeisMeses, UmAno, DoisAnos, CincoAnos }`; `static DateTime Analises.Limite(Idade idade, DateTime hoje)`; `sealed record ResumoAntigos(long Quantidade, long Tamanho, IReadOnlyList<ArquivoEncontrado> Maiores)`; `static ResumoAntigos Analises.ArquivosAntigos(NoPasta pasta, Idade idade, DateTime hoje, int quantos = 100)`.

- [ ] **Passo 1: testes que falham**

```csharp
    [Theory]
    [InlineData(Idade.SeisMeses, 2026, 3, 29)]
    [InlineData(Idade.UmAno, 2025, 9, 29)]
    [InlineData(Idade.DoisAnos, 2024, 9, 29)]
    [InlineData(Idade.CincoAnos, 2021, 9, 29)]
    public void Limite_de_cada_idade(Idade idade, int ano, int mes, int dia)
    {
        Assert.Equal(new DateTime(ano, mes, dia), Analises.Limite(idade, Hoje));
    }

    [Fact]
    public void Antigos_somam_quantidade_e_tamanho_e_ignoram_arquivo_sem_data()
    {
        var um = Analises.ArquivosAntigos(Exemplo(), Idade.UmAno, Hoje);
        var dois = Analises.ArquivosAntigos(Exemplo(), Idade.DoisAnos, Hoje);

        Assert.Equal(2, um.Quantidade);
        Assert.Equal(7000, um.Tamanho);
        Assert.Equal(["video.mp4", "caixa.pst"], um.Maiores.Select(m => m.Arquivo.Nome));
        Assert.Equal(1, dois.Quantidade);
        Assert.Equal(5000, dois.Tamanho);
    }
```

- [ ] **Passo 2: implementação**, em `analises.cs`:

```csharp
public enum Idade
{
    SeisMeses,
    UmAno,
    DoisAnos,
    CincoAnos,
}

public sealed record ResumoAntigos(long Quantidade, long Tamanho, IReadOnlyList<ArquivoEncontrado> Maiores);
```

e dentro de `Analises`:

```csharp
    public static DateTime Limite(Idade idade, DateTime hoje) => idade switch
    {
        Idade.SeisMeses => hoje.Date.AddMonths(-6),
        Idade.UmAno => hoje.Date.AddYears(-1),
        Idade.DoisAnos => hoje.Date.AddYears(-2),
        _ => hoje.Date.AddYears(-5),
    };

    /// <summary>Arquivos sem alteração desde antes do limite. Arquivo sem data não entra.</summary>
    public static ResumoAntigos ArquivosAntigos(NoPasta pasta, Idade idade, DateTime hoje, int quantos = 100)
    {
        var limite = Limite(idade, hoje);
        long quantidade = 0;
        long tamanho = 0;
        var antigos = TodosOsArquivos(pasta)
            .Where(a => a.Arquivo.Modificacao != DateTime.MinValue && a.Arquivo.Modificacao < limite)
            .Select(a =>
            {
                quantidade++;
                tamanho += a.Arquivo.Tamanho;
                return a;
            });
        var maiores = Maiores(antigos, quantos);
        return new ResumoAntigos(quantidade, tamanho, maiores);
    }
```

- [ ] **Passo 3: rodar e ver passar. Commit** `Cria a analise dos arquivos antigos`.

---

### Tarefa 3: resumo por tipo

**Arquivos:** novo `categorias.cs`; `analises.cs`. Teste: novo `categorias-testes.cs`; `analises-testes.cs`.

**Interfaces:**
- Produz: `enum Categoria { Video, Imagem, Audio, Email, ImagemDeDisco, CompactadoEBackup, Instalador, Documento, Outros }`; `static class Categorias` com `Categoria De(string nomeDoArquivo)`, `string Nome(Categoria)`, `string Extensao(string nomeDoArquivo)`; `sealed record ResumoTipo(string Nome, long Quantidade, long Tamanho)`; `static IReadOnlyList<ResumoTipo> Analises.PorCategoria(NoPasta pasta)` e `static IReadOnlyList<ResumoTipo> Analises.PorExtensao(NoPasta pasta, int quantas = 30)`.

- [ ] **Passo 1: testes que falham, `categorias-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class CategoriasTestes
{
    [Theory]
    [InlineData("filme.MP4", Categoria.Video)]
    [InlineData("foto.jpeg", Categoria.Imagem)]
    [InlineData("musica.flac", Categoria.Audio)]
    [InlineData("caixa.PST", Categoria.Email)]
    [InlineData("arquivo.ost", Categoria.Email)]
    [InlineData("windows.iso", Categoria.ImagemDeDisco)]
    [InlineData("maquina.vhdx", Categoria.ImagemDeDisco)]
    [InlineData("copia.zip", Categoria.CompactadoEBackup)]
    [InlineData("banco.bak", Categoria.CompactadoEBackup)]
    [InlineData("setup.msi", Categoria.Instalador)]
    [InlineData("contrato.pdf", Categoria.Documento)]
    [InlineData("planilha.xlsx", Categoria.Documento)]
    [InlineData("pagefile.sys", Categoria.Outros)]
    [InlineData("sem-extensao", Categoria.Outros)]
    public void Categoria_pela_extensao(string nome, Categoria esperada)
    {
        Assert.Equal(esperada, Categorias.De(nome));
    }

    [Fact]
    public void Nomes_em_portugues()
    {
        Assert.Equal("E-mail (.pst, .ost)", Categorias.Nome(Categoria.Email));
        Assert.Equal(".mp4", Categorias.Extensao("filme.MP4"));
        Assert.Equal("(sem extensão)", Categorias.Extensao("sem-extensao"));
    }
}
```

Em `analises-testes.cs`:

```csharp
    [Fact]
    public void Por_categoria_do_maior_para_o_menor()
    {
        var tipos = Analises.PorCategoria(Exemplo());

        Assert.Equal(["Vídeo", "E-mail (.pst, .ost)", "Imagem", "Instalador"], tipos.Select(t => t.Nome));
        Assert.Equal(5000, tipos[0].Tamanho);
        Assert.Equal(1, tipos[2].Quantidade);
    }

    [Fact]
    public void Por_extensao()
    {
        var extensoes = Analises.PorExtensao(Exemplo());

        Assert.Equal([".mp4", ".pst", ".jpg", ".exe"], extensoes.Select(e => e.Nome));
    }
```

- [ ] **Passo 2: implementação**

`categorias.cs`:

```csharp
namespace MapDisk.Nucleo;

public enum Categoria
{
    Video,
    Imagem,
    Audio,
    Email,
    ImagemDeDisco,
    CompactadoEBackup,
    Instalador,
    Documento,
    Outros,
}

/// <summary>Categoria de cada arquivo pela extensão, para o resumo por tipo (R13).</summary>
public static class Categorias
{
    private static readonly Dictionary<string, Categoria> _porExtensao = Montar(
        (Categoria.Video, [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".mpg", ".mpeg", ".webm", ".flv", ".ts"]),
        (Categoria.Imagem, [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".heic", ".webp", ".raw", ".cr2", ".nef", ".psd"]),
        (Categoria.Audio, [".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg", ".wma"]),
        (Categoria.Email, [".pst", ".ost"]),
        (Categoria.ImagemDeDisco, [".iso", ".img", ".vhd", ".vhdx", ".vmdk", ".wim", ".esd"]),
        (Categoria.CompactadoEBackup, [".zip", ".rar", ".7z", ".tar", ".gz", ".bak", ".bkf", ".tib", ".vbk"]),
        (Categoria.Instalador, [".exe", ".msi", ".msix", ".appx", ".cab"]),
        (Categoria.Documento, [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".txt", ".csv", ".rtf"]));

    public static Categoria De(string nomeDoArquivo) =>
        _porExtensao.TryGetValue(Path.GetExtension(nomeDoArquivo), out var categoria) ? categoria : Categoria.Outros;

    public static string Nome(Categoria categoria) => categoria switch
    {
        Categoria.Video => "Vídeo",
        Categoria.Imagem => "Imagem",
        Categoria.Audio => "Áudio",
        Categoria.Email => "E-mail (.pst, .ost)",
        Categoria.ImagemDeDisco => "Imagem de disco",
        Categoria.CompactadoEBackup => "Compactado e backup",
        Categoria.Instalador => "Instalador",
        Categoria.Documento => "Documento",
        _ => "Outros",
    };

    public static string Extensao(string nomeDoArquivo) =>
        Path.GetExtension(nomeDoArquivo) is { Length: > 0 } extensao ? extensao.ToLowerInvariant() : "(sem extensão)";

    private static Dictionary<string, Categoria> Montar(params (Categoria Categoria, string[] Extensoes)[] grupos)
    {
        var mapa = new Dictionary<string, Categoria>(StringComparer.OrdinalIgnoreCase);
        foreach (var (categoria, extensoes) in grupos)
        {
            foreach (var extensao in extensoes)
            {
                mapa[extensao] = categoria;
            }
        }

        return mapa;
    }
}
```

Em `analises.cs`:

```csharp
public sealed record ResumoTipo(string Nome, long Quantidade, long Tamanho);
```

```csharp
    public static IReadOnlyList<ResumoTipo> PorCategoria(NoPasta pasta) =>
        Agrupar(pasta, a => Categorias.Nome(Categorias.De(a.Nome)), int.MaxValue);

    public static IReadOnlyList<ResumoTipo> PorExtensao(NoPasta pasta, int quantas = 30) =>
        Agrupar(pasta, a => Categorias.Extensao(a.Nome), quantas);

    private static IReadOnlyList<ResumoTipo> Agrupar(NoPasta pasta, Func<ArquivoInfo, string> chave, int quantos)
    {
        var grupos = new Dictionary<string, (long Quantidade, long Tamanho)>();
        foreach (var a in TodosOsArquivos(pasta))
        {
            var k = chave(a.Arquivo);
            grupos.TryGetValue(k, out var g);
            grupos[k] = (g.Quantidade + 1, g.Tamanho + a.Arquivo.Tamanho);
        }

        return grupos
            .Select(g => new ResumoTipo(g.Key, g.Value.Quantidade, g.Value.Tamanho))
            .OrderByDescending(r => r.Tamanho)
            .ThenBy(r => r.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(quantos)
            .ToList();
    }
```

- [ ] **Passo 3: rodar e ver passar. Commit** `Cria o resumo por tipo`.

---

### Tarefa 4: resumo por usuário

**Arquivos:** `analises.cs`. Teste: `analises-testes.cs`.

**Interfaces:**
- Produz: `sealed record ResumoUsuarios(NoPasta? PastaDePerfis, IReadOnlyList<NoPasta> Perfis)`; `static ResumoUsuarios Analises.PorUsuario(NoPasta pasta)`.

- [ ] **Passo 1: testes que falham**

```csharp
    [Fact]
    public void Por_usuario_acha_a_pasta_users_na_raiz_da_unidade()
    {
        var r = Analises.PorUsuario(Exemplo());

        Assert.Equal(@"C:\Users", r.PastaDePerfis!.CaminhoCompleto());
        Assert.Equal(["bruno", "ana"], r.Perfis.Select(p => p.Nome));
    }

    [Fact]
    public void Por_usuario_na_propria_pasta_users()
    {
        var users = Exemplo().Subpastas[0];

        Assert.Same(users, Analises.PorUsuario(users).PastaDePerfis);
    }

    [Fact]
    public void Por_usuario_sem_pasta_de_perfis()
    {
        var bruno = Exemplo().Subpastas[0].Subpastas[1];

        var r = Analises.PorUsuario(bruno);

        Assert.Null(r.PastaDePerfis);
        Assert.Empty(r.Perfis);
    }

    [Fact]
    public void Perfil_sem_acesso_entra_no_fim_e_nao_como_zero()
    {
        var raiz = new NoPasta(@"D:\", null);
        var users = new NoPasta("Users", raiz);
        raiz.Preencher([], [users]);
        var ana = new NoPasta("ana", users);
        var fechado = new NoPasta("fechado", users);
        users.Preencher([], [fechado, ana]);
        ana.Preencher([new("a.bin", 10, 4096, Hoje, MarcaArquivo.Nenhuma)], []);
        fechado.MarcarSemAcesso("acesso negado");

        var perfis = Analises.PorUsuario(raiz).Perfis;

        Assert.Equal(["ana", "fechado"], perfis.Select(p => p.Nome));
        Assert.Equal(EstadoPasta.SemAcesso, perfis[1].Estado);
    }
```

- [ ] **Passo 2: implementação**

```csharp
public sealed record ResumoUsuarios(NoPasta? PastaDePerfis, IReadOnlyList<NoPasta> Perfis);
```

```csharp
    /// <summary>
    /// Ranking das pastas de perfil: a pasta Users na raiz da unidade, ou a própria pasta
    /// quando ela se chama Users. Link fica de fora. Perfil sem leitura vai para o fim, sem número.
    /// </summary>
    public static ResumoUsuarios PorUsuario(NoPasta pasta)
    {
        var perfis = EhUsers(pasta)
            ? pasta
            : pasta.Pai is null ? pasta.Subpastas.FirstOrDefault(EhUsers) : null;
        if (perfis is null)
        {
            return new ResumoUsuarios(null, []);
        }

        var lista = perfis.Subpastas
            .Where(p => p.Estado != EstadoPasta.Link)
            .OrderBy(p => p.Estado == EstadoPasta.Lida ? 0 : 1)
            .ThenByDescending(p => p.Tamanho)
            .ThenBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new ResumoUsuarios(perfis, lista);
    }

    private static bool EhUsers(NoPasta pasta) => string.Equals(pasta.Nome, "Users", StringComparison.OrdinalIgnoreCase);
```

- [ ] **Passo 3: rodar e ver passar. Commit** `Cria o resumo por usuario`.

---

### Tarefa 5: painel de análises

**Arquivos:** novo `src/mapdisk.nucleo/painel/painel-analises.cs`; `painel-principal.cs`. Teste: novo `painel-analises-testes.cs`.

**Interfaces:**
- Produz: `enum AbaAnalise { MaioresArquivos, ArquivosAntigos, PorTipo, PorUsuario }`; `sealed record LinhaArquivo(ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, string Caminho)`; `sealed record LinhaResumo(string Nome, string TextoQuantidade, string TextoTamanho, string TextoPorcentagem, double LarguraBarra, NoPasta? Pasta)`; `sealed class PainelAnalises : INotifyPropertyChanged` com `Aba`, `Idade`, `Aviso`, `Titulo`, `Maiores`, `Antigos`, `TextoAntigos`, `Categorias`, `Extensoes`, `Usuarios`, `Task CalcularAsync(NoPasta pasta, DateTime hoje)`, `void Aguardar()`, `void DefinirAba(AbaAnalise)`, `Task DefinirIdadeAsync(Idade, DateTime hoje)`; em `PainelPrincipal`, `PainelAnalises Analises` e `NoPasta? PastaDasAnalises`.

- [ ] **Passo 1: testes que falham, `painel-analises-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class PainelAnalisesTestes
{
    [Fact]
    public async Task Calcula_as_quatro_abas_com_textos_em_portugues()
    {
        var p = new PainelAnalises();

        await p.CalcularAsync(AnalisesTestes.Exemplo(), AnalisesTestes.Hoje);

        Assert.Equal(@"Análise de C:\", p.Titulo);
        Assert.Equal("video.mp4", p.Maiores[0].Nome);
        Assert.Equal(@"C:\", p.Maiores[0].Pasta);
        Assert.Equal("4,9 KB", p.Maiores[0].TextoTamanho);
        Assert.Equal("2 arquivos sem alteração há mais de 1 ano, somando 6,8 KB", p.TextoAntigos);
        Assert.Equal("Vídeo", p.Categorias[0].Nome);
        Assert.Equal("1 arquivo", p.Categorias[0].TextoQuantidade);
        Assert.Equal(["bruno", "ana"], p.Usuarios.Select(u => u.Nome));
        Assert.Equal("1 pasta sem leitura não entra nesta conta", p.Aviso);
    }

    [Fact]
    public async Task Trocar_a_idade_recalcula_os_antigos()
    {
        var p = new PainelAnalises();
        await p.CalcularAsync(AnalisesTestes.Exemplo(), AnalisesTestes.Hoje);

        await p.DefinirIdadeAsync(Idade.DoisAnos, AnalisesTestes.Hoje);

        Assert.Equal("1 arquivo sem alteração há mais de 2 anos, somando 4,9 KB", p.TextoAntigos);
    }

    [Fact]
    public async Task Sem_pasta_de_perfis_explica()
    {
        var p = new PainelAnalises();
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];

        await p.CalcularAsync(bruno, AnalisesTestes.Hoje);

        Assert.Empty(p.Usuarios);
        Assert.Contains("pasta de perfis", p.TextoUsuarios);
    }

    [Fact]
    public void Durante_a_varredura_pede_para_aguardar()
    {
        var p = new PainelAnalises();

        p.Aguardar();

        Assert.Equal("Aguarde o fim da varredura.", p.Aviso);
        Assert.Empty(p.Maiores);
    }
}
```

- [ ] **Passo 2: implementação**

`painel-analises.cs`:

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum AbaAnalise
{
    MaioresArquivos,
    ArquivosAntigos,
    PorTipo,
    PorUsuario,
}

public sealed record LinhaArquivo(ArquivoEncontrado Encontrado, string Nome, string Pasta, string TextoTamanho, string TextoData, string Caminho);

public sealed record LinhaResumo(string Nome, string TextoQuantidade, string TextoTamanho, string TextoPorcentagem, double LarguraBarra, NoPasta? Pasta);

/// <summary>
/// Estado do painel de análises, sem tipos do WPF. Calcula fora da tela e troca as listas de
/// uma vez, para a janela não travar numa pasta com milhões de arquivos.
/// </summary>
public sealed class PainelAnalises : INotifyPropertyChanged
{
    private const double LarguraMaximaBarra = 80;

    private NoPasta? _pasta;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AbaAnalise Aba { get; private set; }

    public Idade Idade { get; private set; } = Idade.UmAno;

    public string Titulo { get; private set; } = "Análises";

    /// <summary>Aviso do topo do painel: aguarde, ou pastas sem leitura fora da conta.</summary>
    public string Aviso { get; private set; } = "Varra uma unidade ou pasta para ver as análises.";

    public IReadOnlyList<LinhaArquivo> Maiores { get; private set; } = [];

    public IReadOnlyList<LinhaArquivo> Antigos { get; private set; } = [];

    public string TextoAntigos { get; private set; } = string.Empty;

    public IReadOnlyList<LinhaResumo> Categorias { get; private set; } = [];

    public IReadOnlyList<LinhaResumo> Extensoes { get; private set; } = [];

    public IReadOnlyList<LinhaResumo> Usuarios { get; private set; } = [];

    public string TextoUsuarios { get; private set; } = string.Empty;

    public void DefinirAba(AbaAnalise aba)
    {
        Aba = aba;
        Avisar();
    }

    public void Aguardar()
    {
        _pasta = null;
        Aviso = "Aguarde o fim da varredura.";
        Maiores = Antigos = [];
        Categorias = Extensoes = Usuarios = [];
        TextoAntigos = TextoUsuarios = string.Empty;
        Avisar();
    }

    public async Task CalcularAsync(NoPasta pasta, DateTime hoje)
    {
        _pasta = pasta;
        var idade = Idade;
        var r = await Task.Run(() => (
            Maiores: Analises.MaioresArquivos(pasta),
            Antigos: Analises.ArquivosAntigos(pasta, idade, hoje),
            Categorias: Analises.PorCategoria(pasta),
            Extensoes: Analises.PorExtensao(pasta),
            Usuarios: Analises.PorUsuario(pasta)));
        if (_pasta != pasta)
        {
            return;
        }

        Titulo = $"Análise de {pasta.CaminhoCompleto()}";
        var semLeitura = pasta.PastasSemAcesso + pasta.PastasComErro;
        Aviso = semLeitura > 0
            ? $"{Formatador.Plural(semLeitura, "pasta sem leitura não entra", "pastas sem leitura não entram")} nesta conta"
            : string.Empty;
        Maiores = r.Maiores.Select(Linha).ToList();
        AplicarAntigos(r.Antigos, idade);
        var total = pasta.Tamanho;
        Categorias = r.Categorias.Select(c => Resumo(c.Nome, c.Quantidade, c.Tamanho, total, null)).ToList();
        Extensoes = r.Extensoes.Select(c => Resumo(c.Nome, c.Quantidade, c.Tamanho, total, null)).ToList();
        AplicarUsuarios(r.Usuarios);
        Avisar();
    }

    public async Task DefinirIdadeAsync(Idade idade, DateTime hoje)
    {
        Idade = idade;
        if (_pasta is { } pasta)
        {
            var antigos = await Task.Run(() => Analises.ArquivosAntigos(pasta, idade, hoje));
            if (_pasta == pasta && Idade == idade)
            {
                AplicarAntigos(antigos, idade);
            }
        }

        Avisar();
    }

    private void AplicarAntigos(ResumoAntigos antigos, Idade idade)
    {
        Antigos = antigos.Maiores.Select(Linha).ToList();
        var ha = idade switch
        {
            Idade.SeisMeses => "6 meses",
            Idade.UmAno => "1 ano",
            Idade.DoisAnos => "2 anos",
            _ => "5 anos",
        };
        TextoAntigos = $"{Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos")} sem alteração há mais de {ha}, somando {Formatador.Tamanho(antigos.Tamanho)}";
    }

    private void AplicarUsuarios(ResumoUsuarios usuarios)
    {
        if (usuarios.PastaDePerfis is not { } perfis)
        {
            Usuarios = [];
            TextoUsuarios = @"Esta pasta não tem a pasta de perfis (Users). Varra a unidade inteira, como C:\, para ver o ranking por usuário.";
            return;
        }

        TextoUsuarios = $"Perfis em {perfis.CaminhoCompleto()}";
        Usuarios = usuarios.Perfis
            .Select(p => p.Estado == EstadoPasta.Lida
                ? Resumo(p.Nome, p.ArquivosTotal, p.Tamanho, perfis.Tamanho, p)
                : new LinhaResumo(p.Nome, string.Empty, "sem acesso", string.Empty, 0, p))
            .ToList();
    }

    private static LinhaArquivo Linha(ArquivoEncontrado a) => new(
        a,
        a.Arquivo.Nome,
        a.Pasta.CaminhoCompleto(),
        Formatador.Tamanho(a.Arquivo.Tamanho),
        Formatador.Data(a.Arquivo.Modificacao),
        a.Caminho);

    private static LinhaResumo Resumo(string nome, long quantidade, long tamanho, long total, NoPasta? pasta)
    {
        var fracao = total > 0 ? Math.Clamp((double)tamanho / total, 0, 1) : 0;
        return new LinhaResumo(
            nome,
            Formatador.Plural(quantidade, "arquivo", "arquivos"),
            Formatador.Tamanho(tamanho),
            Formatador.Porcentagem(fracao),
            Math.Round(fracao * LarguraMaximaBarra, 1),
            pasta);
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
```

Em `PainelPrincipal`:

```csharp
    public PainelAnalises Analises { get; } = new();

    /// <summary>A pasta que as análises mostram: a selecionada, ou a raiz mostrada.</summary>
    public NoPasta? PastaDasAnalises(LinhaArvore? selecionada) =>
        selecionada is { Tipo: TipoLinha.Pasta, Pasta.Estado: EstadoPasta.Lida } linha ? linha.Pasta : Arvore.Raiz;
```

Em `Iniciar` e em `AtualizarPasta`, chamar `Analises.Aguardar()`. A janela chama `CalcularAsync` quando a varredura termina (`Estado` volta a `Parado`), quando a seleção muda e quando uma aba é aberta.

- [ ] **Passo 3: rodar e ver passar. Commit** `Cria o painel de analises`.

---

### Tarefa 6: memória, com medida antes e depois

**Arquivos:** novo `src/mapdisk.nucleo/varredura/conjunto-nomes.cs`; `leitor-pasta.cs`; `motor-varredura.cs`. Teste: novo `conjunto-nomes-testes.cs`.

**Interfaces:**
- Produz: `internal sealed class ConjuntoNomes` com `string Guardar(ReadOnlySpan<char> nome)`, que devolve sempre a mesma instância para nomes iguais.

- [ ] **Passo 1: medir antes.** `ferramentas\publicar.cmd` e `powershell -File ferramentas\medir-memoria.ps1 C:` duas vezes. Número só na conversa.

- [ ] **Passo 2: teste que falha, `conjunto-nomes-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class ConjuntoNomesTestes
{
    [Fact]
    public void Nomes_iguais_devolvem_a_mesma_instancia()
    {
        var nomes = new ConjuntoNomes();

        var a = nomes.Guardar("index.js".AsSpan());
        var b = nomes.Guardar("index.js".ToCharArray());

        Assert.Same(a, b);
        Assert.Equal("index.js", a);
        Assert.NotSame(a, nomes.Guardar("INDEX.JS".AsSpan()));
    }

    [Fact]
    public void Muitas_tarefas_ao_mesmo_tempo()
    {
        var nomes = new ConjuntoNomes();
        var guardados = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 10_000, i => guardados.Add(nomes.Guardar($"n{i % 10}".AsSpan())));

        Assert.Equal(10, guardados.Distinct(ReferenceEqualityComparer.Instance).Count());
    }
}
```

- [ ] **Passo 3: implementação**

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Guarda cada nome de arquivo uma vez só por varredura. Numa unidade de trabalho, o mesmo
/// nome se repete milhares de vezes (index.js, desktop.ini, thumbs.db). Dividido em faixas com
/// trava, como o conjunto de identificadores.
/// </summary>
internal sealed class ConjuntoNomes
{
    private const int Faixas = 64;

    private readonly HashSet<string>[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new HashSet<string>(StringComparer.Ordinal)).ToArray();

    public string Guardar(ReadOnlySpan<char> nome)
    {
        var texto = new string(nome);
        var faixa = _faixas[(int)((uint)StringComparer.Ordinal.GetHashCode(texto) % Faixas)];
        lock (faixa)
        {
            if (faixa.TryGetValue(texto, out var existente))
            {
                return existente;
            }

            faixa.Add(texto);
            return texto;
        }
    }
}
```

No `LeitorPasta.Ler` e no `Interpretar`, receber um `ConjuntoNomes? nomes` e trocar `new string(...)` por `nomes?.Guardar(span) ?? new string(span)`, só para arquivos. O motor cria um `ConjuntoNomes` por varredura e passa para o leitor. Os testes antigos do leitor passam `null`.

- [ ] **Passo 4: rodar e ver passar.**

- [ ] **Passo 5: medir depois**, do mesmo jeito do passo 1.
  - **Com ganho:** commit `Guarda uma vez so os nomes de arquivo repetidos` e pendência atualizada, sem números do computador do Manfred.
  - **Sem ganho:** desfazer as mudanças desta tarefa com `git restore` nos arquivos dela, apagar os dois arquivos novos (`conjunto-nomes.cs` e o teste), que são da própria tarefa e nunca entraram em commit, e registrar na pendência o que foi tentado e o resultado.

---

### Tarefa 7: painel na janela

**Arquivos:** `src/mapdisk/janela-principal.xaml` e `.xaml.cs`.

- [ ] **Passo 1: leiaute.** O `ListView` da árvore passa para dentro de um `Grid` com três colunas: árvore (`*`), `GridSplitter` (5 px) e painel (`420`, mínimo 280). Um botão "Análises" na barra de exibição recolhe e mostra o painel (`Visibility` da coluna).

- [ ] **Passo 2: painel.** Um `DockPanel` com `DataContext="{Binding Analises}"`:
  - no topo, o `Titulo` em `TituloGrupo` e o `Aviso` em `PincelTextoSuave`;
  - um `TabControl` com quatro abas:
    1. **Maiores arquivos:** `ListView` com `Maiores`, colunas Tamanho, Nome e Pasta, com a dica mostrando `Caminho`.
    2. **Arquivos antigos:** uma linha com `RadioButton` para 6 meses, 1 ano, 2 anos e 5 anos, o `TextoAntigos` e a lista `Antigos`, com Tamanho, Nome, Data e Pasta.
    3. **Por tipo:** duas listas, `Categorias` e `Extensoes`, com a barra (`LarguraBarra`), Nome, Tamanho, % e quantidade de arquivos.
    4. **Por usuário:** o `TextoUsuarios` e a lista `Usuarios`, com a barra, Nome, Tamanho, % e arquivos.
  - o mesmo menu de contexto nas listas de arquivos: Mostrar no Explorer, Copiar caminho e "Abrir a pasta na árvore" (`AbrirAqui` da pasta do arquivo). Nas listas de usuário, "Abrir aqui".

- [ ] **Passo 3: código.**
  - `Tabela.SelectionChanged` e a troca de aba chamam `AtualizarAnalises()`, que, com o painel parado, faz `await _painel.Analises.CalcularAsync(_painel.PastaDasAnalises(Tabela.SelectedItem as LinhaArvore)!, DateTime.Now)`.
  - O relógio de 250 ms guarda o estado anterior do painel e, quando ele passa de `Varrendo` para `Parado`, chama `AtualizarAnalises()`.
  - Os `RadioButton` da idade chamam `DefinirIdadeAsync`.

- [ ] **Passo 4: build e conferência na janela, pelo agente.**
  - Em `--demonstracao`: as quatro abas, a troca de idade e o "Abrir a pasta na árvore" a partir de um arquivo.
  - Na pasta de testes do projeto: seleção de uma subpasta mudando o título da análise.
  - Números do computador do Manfred só na conversa.

- [ ] **Passo 5: commit** `Liga o painel de analises na janela`.

---

### Tarefa 8: documentação e Pull Request

- [ ] **Passo 1:** README com o painel de análises na seção "Uso" e a linha da fatia 3 na "Situação do projeto".
- [ ] **Passo 2:** pendências da fatia 3, com o que ficar de fora (por exemplo, número de maiores arquivos configurável, que vai para as Opções da fatia 7) e o resultado da tarefa 6.
- [ ] **Passo 3:** portões, commit `Traz a documentacao da fatia 3` e Pull Request do ramo `analises`, com "O que muda", "Como testar" e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec

| Requisito | Onde |
|---|---|
| R11 maiores arquivos (100) | Tarefas 1, 5 e 7. O número configurável fica para as Opções da fatia 7 |
| R12 arquivos antigos com total em GB | Tarefas 2, 5 e 7 |
| R13 resumo por tipo com as categorias da spec | Tarefas 3, 5 e 7 |
| R14 resumo por usuário | Tarefas 4, 5 e 7 |
| Regra 3 nas análises | Aviso de pastas sem leitura e perfil "sem acesso" (tarefas 4 e 5) |
| Pendência de memória | Tarefa 6, com decisão pela medida |
