# Plano da fatia 6: relatório para o cliente avaliar

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** o técnico monta uma lista de pastas e arquivos candidatos, gera uma página HTML (imprimível em PDF) e uma planilha Excel para o cliente decidir item por item (Apagar, Mover, Manter, Conversar), e depois lê a resposta de volta: os itens são selecionados para as ações da fatia 5, sempre com a confirmação de sempre.

**Arquitetura:** tudo que decide fica no núcleo, em `src/mapdisk.nucleo/relatorios/`: a lista e o "Sugerir", a página, a planilha (gravada e lida à mão, sem pacote), a resposta e o casamento com a árvore. O estado das duas janelas novas fica em `painel/`, sem WPF. A janela principal ganha o botão "Relatório do cliente", com o menu Gerar e Ler resposta, e passa a agir sobre uma lista de itens vinda da resposta.

**Tecnologia:** C# com .NET 8, WPF, xUnit, `System.IO.Compression`, `System.Xml.Linq` e `System.Text.Json`. Sem pacote NuGet novo.

Desenho: `docs/superpowers/specs/2026-09-30-relatorio-cliente-design.md` (R20). Ações: fatia 5. Textos: seção 4.2 da verificação jurídica.

## Restrições globais

- Regras do produto do `AGENTS.md`. Pesam a 1 (a resposta só seleciona; agir passa pela confirmação da fatia 5), a 3 (pasta sem leitura nunca entra na lista), a 6 (a página não carrega nada da internet; os arquivos ficam onde o técnico escolher) e a 7 (tudo sai da árvore em memória).
- Relatório de máquina de cliente nunca entra no repositório. Testes só com árvores fictícias e a de demonstração, e arquivos só dentro da pasta de saída dos testes.
- Textos do cliente (apresentação e autorização) passam pela `legal-br` e pela `humanizar-ptbr` antes de entrar no código.
- Português, sem caracteres proibidos (inclusive no HTML e no código JavaScript gerado), nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Formato da planilha | `.xlsx` montado à mão: um zip com o XML das abas, textos na própria célula, lista de opções na coluna Decisão | Sem pacote NuGet. O Excel e o LibreOffice abrem |
| Leitura da planilha | Aceita textos compartilhados (como o Excel salva) e textos na célula (como o LibreOffice salva) | Os dois programas que o cliente pode usar |
| Data da varredura na resposta | Vai como texto com os "ticks" do .NET, na aba Controle e nos atributos da página | Excel e JavaScript arredondam números grandes e quebrariam a conferência "mudou depois do relatório" |
| Número do relatório | Data e hora da geração, `aaaammdd-hhmmss` | Único na prática e legível para o cliente |
| Nome dos arquivos | `avaliacao-<ultima parte da pasta>-<aaaa-mm-dd>.html` e `.xlsx`, em minúsculas, sem acento, espaço vira hífen | Convenção do projeto. O técnico escolhe a pasta e confirma se já existir |
| Ordem da lista | Do maior para o menor; o número (#1, #2...) segue essa ordem | O cliente vê primeiro o que mais pesa |
| Motivo de cada item | O critério que o trouxe: "entre as 20 maiores pastas", "entre os 50 maiores arquivos", "sem alteração há mais de 2 anos", "imagem de disco", "compactado e backup", "instalador" ou "escolhido pelo técnico". Vários critérios juntam com "; " | O cliente entende por que o item está ali |
| Seleção reaproveitada | A conversão da seleção da tela em itens sai do `PreparadorAcoes` para um método próprio, usado pelas ações e pelo relatório | Uma regra só para o que é item |
| Botão | "Relatório do cliente", no cabeçalho do cartão Pastas, com o menu "Gerar relatório..." e "Ler resposta..." | O cabeçalho já tem três botões |
| Resposta que não casa | Item que não existe mais: "não encontrado". Tamanho ou data diferentes: "mudou depois do relatório". Os dois ficam fora das ações | Regra 1 |
| Varredura antes de ler | Se nenhum item casa, o aviso diz para varrer a pasta do relatório primeiro | O casamento é pelo caminho, na árvore atual |
| Mover pela resposta | O técnico escolhe os itens na janela da resposta e a pasta de destino; o destino escrito pelo cliente aparece na lista | Cada cliente escreve o destino de um jeito |

## Git desta fatia

1. O plano entra pelo ramo `fatia-6-plano`, num Pull Request só do plano, com a linha da fatia 6 atualizada no README.
2. O código sai do `main` atualizado, no ramo `relatorio-cliente`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/acoes/preparador-acoes.cs` | `ItensDaSelecao` separado, `AvaliarItens` |
| `src/mapdisk.nucleo/relatorios/avaliacao.cs` | Modelo do relatório, lista de avaliação e textos para o cliente |
| `src/mapdisk.nucleo/relatorios/sugestao.cs` | Critérios e o "Sugerir" |
| `src/mapdisk.nucleo/relatorios/pagina-avaliacao.cs` | Página HTML |
| `src/mapdisk.nucleo/relatorios/planilha-avaliacao.cs` | Gravar e ler a planilha |
| `src/mapdisk.nucleo/relatorios/resposta-avaliacao.cs` | Resposta, leitura do JSON da página, números e casamento com a árvore |
| `src/mapdisk.nucleo/painel/painel-relatorio.cs` | Estado da janela do relatório e da janela da resposta |
| `src/mapdisk.nucleo/painel/painel-principal.cs` | `AvaliarItens` para agir sobre a lista da resposta |
| `src/mapdisk.nucleo/mapdisk.nucleo.csproj` | Logo da MT embutido no núcleo, para a página |
| `src/mapdisk/janela-relatorio.xaml`, `janela-resposta.xaml` e `.cs` | As duas janelas |
| `src/mapdisk/janela-principal.xaml` e `.cs` | Botão com menu e a ação sobre itens da resposta |
| `testes/mapdisk.testes/relatorio-cliente-testes.cs` | Testes desta fatia |

---

### Tarefa 1: seleção em itens, separada

**Arquivos:** `src/mapdisk.nucleo/acoes/preparador-acoes.cs`, `src/mapdisk.nucleo/painel/painel-principal.cs`. Teste: novo `testes/mapdisk.testes/relatorio-cliente-testes.cs`.

**Interfaces:**
- Produz: `static (IReadOnlyList<ItemAcao> Itens, string? Grupo) PreparadorAcoes.ItensDaSelecao(IEnumerable<object> selecionados)`, em que `Grupo` é o nome da linha "[N arquivos]" quando ela foi selecionada; `AvaliacaoSelecao PreparadorAcoes.AvaliarItens(IReadOnlyList<ItemAcao> itens)`; `void PainelPrincipal.AvaliarItens(IReadOnlyList<ItemAcao> itens)`.

- [ ] **Passo 1: criar o ramo do código**

```bash
git checkout main
git pull
git checkout -b relatorio-cliente
```

- [ ] **Passo 2: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class RelatorioClienteTestes
{
    internal static ArvoreVisivel ArvoreAberta()
    {
        var arvore = new ArvoreVisivel();
        arvore.Carregar(AnalisesTestes.Exemplo());
        arvore.AbrirNiveis(3);
        return arvore;
    }

    internal static LinhaArvore LinhaDe(ArvoreVisivel arvore, string nome) => arvore.Linhas.First(l => l.Nome == nome);

    [Fact]
    public void Itens_da_selecao_convertem_linhas_e_apontam_o_grupo()
    {
        var arvore = ArvoreAberta();

        var (itens, grupo) = PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "bruno"), LinhaDe(arvore, "Users")]);

        Assert.Equal([@"C:\Users\bruno", @"C:\Users"], itens.Select(i => i.Caminho));
        Assert.Null(grupo);
        Assert.Equal("[1 arquivo]", PreparadorAcoes.ItensDaSelecao([LinhaDe(arvore, "[1 arquivo]")]).Grupo);
    }

    [Fact]
    public void Painel_avalia_itens_vindos_da_resposta()
    {
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Locais = AcoesTestes.Protegidos with { PastaRegistro = @"E:\registro" },
            TipoDaUnidade = _ => DriveType.Fixed,
        });
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];

        painel.AvaliarItens([ItemAcao.DaPasta(bruno)]);

        Assert.True(painel.PodeRemover);
        Assert.Equal(TipoAcao.Lixeira, painel.Selecao.Remocao);
    }
}
```

- [ ] **Passo 3: rodar e ver falhar.** Run: `dotnet test mapdisk.sln -c Release --filter RelatorioClienteTestes`. Expected: FAIL na compilação.

- [ ] **Passo 4: implementar.** Em `preparador-acoes.cs`, o `switch` do começo de `Avaliar` vira:

```csharp
    /// <summary>A seleção da tela em itens. A linha "[N arquivos]" não é item: volta no Grupo.</summary>
    public static (IReadOnlyList<ItemAcao> Itens, string? Grupo) ItensDaSelecao(IEnumerable<object> selecionados)
    {
        var itens = new List<ItemAcao>();
        string? grupo = null;
        foreach (var s in selecionados)
        {
            switch (s)
            {
                case LinhaArvore { Tipo: TipoLinha.GrupoArquivos } linhaGrupo:
                    grupo ??= linhaGrupo.Nome;
                    break;
                case LinhaArvore { Tipo: TipoLinha.Pasta } pasta:
                    itens.Add(ItemAcao.DaPasta(pasta.Pasta));
                    break;
                case LinhaArvore arquivo:
                    itens.Add(ItemAcao.DoArquivo(arquivo.Pasta, arquivo.Arquivo));
                    break;
                case LinhaArquivo linha:
                    itens.Add(ItemAcao.DoArquivo(linha.Encontrado.Pasta, linha.Encontrado.Arquivo));
                    break;
                case LinhaResumo { Pasta: { } perfil }:
                    itens.Add(ItemAcao.DaPasta(perfil));
                    break;
            }
        }

        return (itens, grupo);
    }

    public AvaliacaoSelecao Avaliar(IEnumerable<object> selecionados)
    {
        var (itens, grupo) = ItensDaSelecao(selecionados);
        return grupo is not null ? Bloqueado($"{grupo}: abra o grupo e selecione os arquivos.") : AvaliarItens(itens);
    }

    public AvaliacaoSelecao AvaliarItens(IReadOnlyList<ItemAcao> itens)
    {
        // Daqui para baixo, o corpo atual de Avaliar a partir de "Item dentro de outra pasta selecionada sai".
    }
```

O corpo de `AvaliarItens` é o trecho atual de `Avaliar` que começa em `var unicos = itens.DistinctBy(...)` e vai até o `return` final, sem mudança. Em `painel-principal.cs`:

```csharp
    /// <summary>Itens vindos da resposta do cliente, avaliados pelas mesmas regras da seleção.</summary>
    public void AvaliarItens(IReadOnlyList<ItemAcao> itens)
    {
        Selecao = Acoes.AvaliarItens(itens);
        Avisar();
    }
```

- [ ] **Passo 5: rodar e ver passar.** Run: `dotnet test mapdisk.sln -c Release`. Expected: PASS em todos, inclusive os da fatia 5.

- [ ] **Passo 6: commit** `Separa a selecao em itens para o relatorio`.

---

### Tarefa 2: lista de avaliação e "Sugerir"

**Arquivos:** novos `src/mapdisk.nucleo/relatorios/avaliacao.cs` e `src/mapdisk.nucleo/relatorios/sugestao.cs`. Teste: acrescentar em `relatorio-cliente-testes.cs`.

**Interfaces:**
- Consome: `ItemAcao`, `Protecao.Dentro`, `Analises.MaioresArquivos`, `Analises.TodosOsArquivos`, `Categorias`.
- Produz: `sealed record ItemAvaliacao(int Numero, ItemAcao Item, string Motivo)` com `string Tipo` ("Pasta" ou "Arquivo") e `long Arquivos`; `sealed class ListaAvaliacao` com `Acrescentar(ItemAcao, string motivo)`, `Tirar(IEnumerable<int> numeros)`, `Limpar()`, `IReadOnlyList<ItemAvaliacao> Itens` e `long Total`; `sealed record CriteriosSugestao(int MaioresPastas = 20, int MaioresArquivos = 50, int AnosSemAlteracao = 2, long TamanhoMinimo = 100 * 1024 * 1024)`; `static class Sugestao` com `void Sugerir(ListaAvaliacao lista, NoPasta pasta, CriteriosSugestao criterios, DateTime hoje)`; `const string Sugestao.EscolhidoPeloTecnico = "escolhido pelo técnico"`.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Lista_numera_do_maior_para_o_menor_e_junta_os_motivos()
    {
        var raiz = AnalisesTestes.Exemplo();
        var bruno = raiz.Subpastas[0].Subpastas[1];
        var lista = new ListaAvaliacao();

        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "compactado e backup");

        Assert.Equal(["video.mp4", "caixa.pst"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal([1, 2], lista.Itens.Select(i => i.Numero));
        Assert.Equal("entre os 50 maiores arquivos; compactado e backup", lista.Itens[1].Motivo);
        Assert.Equal(7000, lista.Total);
    }

    [Fact]
    public void Pasta_na_lista_leva_o_que_esta_dentro_e_pasta_sem_leitura_nao_entra()
    {
        var raiz = AnalisesTestes.Exemplo();
        var users = raiz.Subpastas[0];
        var bruno = users.Subpastas[1];
        var lista = new ListaAvaliacao();

        lista.Acrescentar(ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), "x");
        lista.Acrescentar(ItemAcao.DaPasta(users), "y");
        lista.Acrescentar(ItemAcao.DaPasta(bruno), "z");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[1]), "w");

        Assert.Equal([@"C:\Users"], lista.Itens.Select(i => i.Item.Caminho));
        Assert.Equal("Pasta", lista.Itens[0].Tipo);
        Assert.Equal(4, lista.Itens[0].Arquivos);
    }

    [Fact]
    public void Tirar_pelo_numero()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "x");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[0]), "y");

        lista.Tirar([1]);

        Assert.Equal(["Users"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal(1, lista.Itens[0].Numero);
    }

    [Fact]
    public void Sugerir_usa_os_criterios()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        var criterios = new CriteriosSugestao(MaioresPastas: 1, MaioresArquivos: 1, AnosSemAlteracao: 2, TamanhoMinimo: 1000);

        Sugestao.Sugerir(lista, raiz, criterios, AnalisesTestes.Hoje);

        Assert.Equal(["video.mp4", "Users"], lista.Itens.Select(i => i.Item.Nome));
        Assert.Equal("entre os 1 maiores arquivos; sem alteração há mais de 2 anos", lista.Itens[0].Motivo);
        Assert.Equal("entre as 1 maiores pastas", lista.Itens[1].Motivo);
    }
```

No exemplo, o `video.mp4` é de 10/01/2023 (mais de 2 anos antes de 29/09/2026) e tem 5.000 bytes. A pasta `Users` leva o `caixa.pst`, então ele não aparece sozinho.

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar `avaliacao.cs`** (modelo e lista; os textos para o cliente entram na tarefa 3)

```csharp
namespace MapDisk.Nucleo;

public sealed record ItemAvaliacao(int Numero, ItemAcao Item, string Motivo)
{
    public string Tipo => Item.EhPasta ? "Pasta" : "Arquivo";

    public long Arquivos => Item.EhPasta ? Item.Pasta.ArquivosTotal : 1;
}

/// <summary>
/// Os candidatos que o cliente vai avaliar. Pasta sem leitura não entra (regra 3). O que está
/// dentro de uma pasta da lista não entra de novo: a decisão sobre a pasta já o leva.
/// </summary>
public sealed class ListaAvaliacao
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
```

- [ ] **Passo 4: implementar `sugestao.cs`**

```csharp
namespace MapDisk.Nucleo;

public sealed record CriteriosSugestao(int MaioresPastas = 20, int MaioresArquivos = 50, int AnosSemAlteracao = 2, long TamanhoMinimo = 100 * 1024 * 1024);

/// <summary>O "Sugerir": põe na lista o que costuma liberar espaço. Pastas primeiro, para os arquivos dentro delas não repetirem.</summary>
public static class Sugestao
{
    public const string EscolhidoPeloTecnico = "escolhido pelo técnico";

    private static readonly Categoria[] QueCostumamSobrar = [Categoria.ImagemDeDisco, Categoria.CompactadoEBackup, Categoria.Instalador];

    public static void Sugerir(ListaAvaliacao lista, NoPasta pasta, CriteriosSugestao criterios, DateTime hoje)
    {
        foreach (var sub in pasta.Subpastas.Where(s => s.Estado == EstadoPasta.Lida && s.Tamanho > 0).OrderByDescending(s => s.Tamanho).Take(criterios.MaioresPastas))
        {
            lista.Acrescentar(ItemAcao.DaPasta(sub), $"entre as {criterios.MaioresPastas} maiores pastas");
        }

        foreach (var a in Analises.MaioresArquivos(pasta, criterios.MaioresArquivos))
        {
            lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), $"entre os {criterios.MaioresArquivos} maiores arquivos");
        }

        var limite = hoje.AddYears(-criterios.AnosSemAlteracao);
        var anos = criterios.AnosSemAlteracao == 1 ? "1 ano" : $"{criterios.AnosSemAlteracao} anos";
        foreach (var a in Analises.TodosOsArquivos(pasta).Where(a => a.Arquivo.Tamanho >= criterios.TamanhoMinimo))
        {
            if (a.Arquivo.Modificacao != DateTime.MinValue && a.Arquivo.Modificacao < limite)
            {
                lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), $"sem alteração há mais de {anos}");
            }

            var categoria = Categorias.De(a.Arquivo.Nome);
            if (QueCostumamSobrar.Contains(categoria))
            {
                lista.Acrescentar(ItemAcao.DoArquivo(a.Pasta, a.Arquivo), Categorias.Nome(categoria).ToLowerInvariant());
            }
        }
    }
}
```

`Analises.TodosOsArquivos` já deixa de fora o hard link repetido.

- [ ] **Passo 5: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 6: commit** `Cria a lista de avaliacao e o Sugerir`.

---

### Tarefa 3: textos para o cliente e página HTML

**Arquivos:** `avaliacao.cs` (modelo e textos), novo `src/mapdisk.nucleo/relatorios/pagina-avaliacao.cs`, `src/mapdisk.nucleo/mapdisk.nucleo.csproj`. Teste: acrescentar em `relatorio-cliente-testes.cs`.

**Interfaces:**
- Produz: `sealed record Avaliacao(string Numero, string Cliente, string Tecnico, string Mensagem, NoPasta Pasta, DateTime Gerado, long? Livre, IReadOnlyList<ItemAvaliacao> Itens)` com `static string NumeroDe(DateTime)`, `string NomeDoArquivo` (sem extensão) e `long Total`; `static class TextosAvaliacao` com `Apresentacao`, `Autorizacao` e `Opcoes`; `static class PaginaAvaliacao` com `string Gerar(Avaliacao)`, `const string FormatoResposta = "mapdisk-avaliacao-resposta"`.

- [ ] **Passo 1: textos jurídicos.** Rodar a `legal-br` sobre a proposta abaixo, a partir da seção 4.2 da verificação jurídica (a MT age como operadora, por instrução do cliente), e registrar como seção 10 da verificação, com a data. Depois, `humanizar-ptbr`. A versão que sair entra em `TextosAvaliacao`; os testes usam as constantes, não o texto literal.

Proposta de apresentação:

> Este relatório mostra pastas e arquivos que ocupam espaço em {pasta}. Para cada item, marque o que fazer: Apagar, Mover (diga para onde), Manter ou Conversar. O que ficar sem marca fica como está. Nada é apagado nem movido antes de a sua resposta chegar e de o técnico da MT conferir a lista com você.

Proposta de autorização, ao lado do campo "Quem decide":

> Ao devolver esta resposta com o seu nome, você declara que pode decidir sobre estes arquivos e autoriza a MT - Manfred Tecnologia a apagar ou mover os itens marcados, como indicado.

- [ ] **Passo 2: logo no núcleo.** Em `mapdisk.nucleo.csproj`:

```xml
  <!-- Logo da MT dentro da página do relatório para o cliente, sem buscar nada fora -->
  <ItemGroup>
    <EmbeddedResource Include="..\mapdisk\recursos\mt-logo.png" LogicalName="mt-logo.png" />
  </ItemGroup>
```

- [ ] **Passo 3: escrever os testes**

```csharp
    internal static Avaliacao Exemplo()
    {
        var raiz = AnalisesTestes.Exemplo();
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, raiz.Arquivos[0]), "entre os 50 maiores arquivos");
        lista.Acrescentar(ItemAcao.DaPasta(raiz.Subpastas[0]), Sugestao.EscolhidoPeloTecnico);
        var gerado = new DateTime(2026, 9, 30, 14, 5, 9);
        return new Avaliacao(Avaliacao.NumeroDe(gerado), "Cliente Exemplo", "tecnico", "Veja o que pode sair.", raiz, gerado, 1000, lista.Itens);
    }

    [Fact]
    public void Numero_e_nome_do_arquivo()
    {
        var a = Exemplo();

        Assert.Equal("20260930-140509", a.Numero);
        Assert.Equal("avaliacao-c-2026-09-30", a.NomeDoArquivo);
        Assert.Equal("avaliacao-dados-publicos-2026-09-30",
            (a with { Pasta = new NoPasta(@"\\srv\Dados Públicos", null) }).NomeDoArquivo);
    }

    [Fact]
    public void Pagina_traz_os_itens_as_opcoes_e_nao_carrega_nada_de_fora()
    {
        var html = PaginaAvaliacao.Gerar(Exemplo());

        Assert.Contains("Cliente Exemplo", html);
        Assert.Contains("data-numero=\"1\"", html);
        Assert.Contains("data-caminho=\"C:\\video.mp4\"", html);
        Assert.Contains("value=\"apagar\"", html);
        Assert.Contains("value=\"conversar\"", html);
        Assert.Contains(PaginaAvaliacao.FormatoResposta, html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.DoesNotMatch("src=\"(?!data:)", html);
        Assert.DoesNotContain("url(", html);
        Assert.DoesNotMatch("<input[^>]*checked", html);
    }

    [Fact]
    public void Pagina_escapa_o_que_vem_da_arvore()
    {
        var raiz = new NoPasta(@"D:\", null);
        var arquivo = new ArquivoInfo("<script>x</script>.txt", 10, 10, new DateTime(2020, 1, 1), MarcaArquivo.Nenhuma);
        raiz.Preencher([arquivo], []);
        var lista = new ListaAvaliacao();
        lista.Acrescentar(ItemAcao.DoArquivo(raiz, arquivo), "x");

        var html = PaginaAvaliacao.Gerar(new Avaliacao("1", "A & B", "t", "", raiz, DateTime.Now, null, lista.Itens));

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("A &amp; B", html);
    }
```

- [ ] **Passo 4: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 5: implementar o modelo e os textos em `avaliacao.cs`**

```csharp
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
            return $"avaliacao-{(simples.Length == 0 ? "pasta" : simples)}-{Gerado:yyyy-MM-dd}";
        }
    }
}

/// <summary>Textos para o cliente. Revisados pela legal-br (verificação jurídica, seção 10) e pela humanizar-ptbr.</summary>
public static class TextosAvaliacao
{
    public static string Apresentacao(string pasta) =>
        $"Este relatório mostra pastas e arquivos que ocupam espaço em {pasta}. Para cada item, marque o que fazer: Apagar, Mover (diga para onde), Manter ou Conversar. O que ficar sem marca fica como está. Nada é apagado nem movido antes de a sua resposta chegar e de o técnico da MT conferir a lista com você.";

    public const string Autorizacao =
        "Ao devolver esta resposta com o seu nome, você declara que pode decidir sobre estes arquivos e autoriza a MT - Manfred Tecnologia a apagar ou mover os itens marcados, como indicado.";

    public static readonly (string Valor, string Rotulo)[] Opcoes =
        [("apagar", "Apagar"), ("mover", "Mover"), ("manter", "Manter"), ("conversar", "Conversar")];
}
```

Os dois textos são a proposta do passo 1. Se a `legal-br` ou a `humanizar-ptbr` pedirem outra redação, ela entra aqui antes do commit; os testes usam as constantes. Os `using` do arquivo: `System.Globalization`, `System.Text` e `System.Text.RegularExpressions`.

- [ ] **Passo 6: implementar `pagina-avaliacao.cs`**

```csharp
using System.Globalization;
using System.Net;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>
/// Página para o cliente decidir item por item. Um arquivo só: logo, estilo e código dentro,
/// sem carregar nada da internet (regra 6). "Salvar resposta" baixa um JSON; "Imprimir" dá o PDF.
/// </summary>
public static class PaginaAvaliacao
{
    public const string FormatoResposta = "mapdisk-avaliacao-resposta";

    public static string Gerar(Avaliacao a)
    {
        var s = new StringBuilder();
        s.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        s.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.Append($"<title>Avaliação de espaço - {H(a.Cliente)}</title><style>{Estilo}</style></head><body>");
        s.Append($"<header><img alt=\"MT - Manfred Tecnologia\" src=\"data:image/png;base64,{Logo()}\"><div><h1>Avaliação de espaço</h1>");
        s.Append($"<p>{H(a.Cliente)} | {H(a.Pasta.CaminhoCompleto())} | varredura de {a.Gerado:dd/MM/yyyy} | relatório {H(a.Numero)}</p></div></header>");
        s.Append($"<main><p class=\"apresentacao\">{H(TextosAvaliacao.Apresentacao(a.Pasta.CaminhoCompleto()))}</p>");
        if (a.Mensagem.Length > 0)
        {
            s.Append($"<p class=\"mensagem\"><b>Mensagem do técnico:</b> {H(a.Mensagem)}</p>");
        }

        Resumo(s, a);
        s.Append("<h2>Itens para decidir</h2><table><thead><tr><th>Nº</th><th>Item</th><th>Tamanho</th><th>Última alteração</th><th>Por que está aqui</th><th>Decisão</th><th>Destino (se Mover) e observação</th></tr></thead><tbody>");
        foreach (var i in a.Itens)
        {
            s.Append($"<tr class=\"item\" data-numero=\"{i.Numero}\" data-caminho=\"{H(i.Item.Caminho)}\" data-bytes=\"{i.Item.Tamanho.ToString(CultureInfo.InvariantCulture)}\" data-mod=\"{i.Item.Modificacao.Ticks.ToString(CultureInfo.InvariantCulture)}\">");
            s.Append($"<td>{i.Numero}</td><td><b>{H(i.Item.Nome)}</b><br><small>{H(i.Tipo)} | {H(i.Item.Caminho)}</small></td>");
            s.Append($"<td class=\"n\">{H(Formatador.Tamanho(i.Item.Tamanho))}</td><td>{H(Formatador.Data(i.Item.Modificacao))}</td><td>{H(i.Motivo)}</td><td class=\"opcoes\">");
            foreach (var (valor, rotulo) in TextosAvaliacao.Opcoes)
            {
                s.Append($"<label><input type=\"radio\" name=\"d{i.Numero}\" value=\"{valor}\"> {rotulo}</label>");
            }

            s.Append("</td><td><input class=\"destino\" type=\"text\" placeholder=\"Destino\"><input class=\"obs\" type=\"text\" placeholder=\"Observação\"></td></tr>");
        }

        s.Append("</tbody></table>");
        s.Append("<h2>Sua resposta</h2><p><label>Observação geral<br><textarea id=\"obsgeral\" rows=\"3\"></textarea></label></p>");
        s.Append($"<p><label>Quem decide (nome e cargo)<br><input id=\"decidido\" type=\"text\"></label></p><p class=\"autorizacao\">{H(TextosAvaliacao.Autorizacao)}</p>");
        s.Append("<p class=\"botoes\"><button onclick=\"salvar()\">Salvar resposta</button> <button onclick=\"window.print()\">Imprimir ou salvar em PDF</button></p>");
        s.Append($"<footer>Gerado pelo MapDisk - MT {H(ExecutorCli.Versao)}, da MT - Manfred Tecnologia (<a href=\"{Sobre.SiteMt}\">www.manfred.com.br</a>). Técnico: {H(a.Tecnico)}.</footer></main>");
        s.Append($"<script>var REL=\"{H(a.Numero)}\",FORMATO=\"{FormatoResposta}\";{Codigo}</script></body></html>");
        return s.ToString();
    }

    private static void Resumo(StringBuilder s, Avaliacao a)
    {
        var p = a.Pasta;
        s.Append($"<h2>Resumo da pasta</h2><p>Total lido: <b>{H(Formatador.Tamanho(p.Tamanho))}</b> em {H(Formatador.Plural(p.ArquivosTotal, "arquivo", "arquivos"))}.");
        if (a.Livre is { } livre)
        {
            s.Append($" Espaço livre no disco: <b>{H(Formatador.Tamanho(livre))}</b>.");
        }

        var semLeitura = p.PastasSemAcesso + p.PastasComErro;
        if (semLeitura > 0)
        {
            s.Append($" {H(Formatador.Plural(semLeitura, "pasta não pôde ser lida e não entra", "pastas não puderam ser lidas e não entram"))} nesta conta.");
        }

        s.Append("</p><div class=\"resumo\"><div><h3>Maiores pastas</h3><ul>");
        foreach (var sub in p.Subpastas.Where(x => x.Estado == EstadoPasta.Lida).OrderByDescending(x => x.Tamanho).Take(5))
        {
            s.Append($"<li>{H(sub.Nome)}: {H(Formatador.Tamanho(sub.Tamanho))}</li>");
        }

        s.Append("</ul></div><div><h3>Por tipo</h3><ul>");
        foreach (var t in Analises.PorCategoria(p).Take(5))
        {
            s.Append($"<li>{H(t.Nome)}: {H(Formatador.Tamanho(t.Tamanho))}</li>");
        }

        var antigos = Analises.ArquivosAntigos(p, Idade.DoisAnos, a.Gerado);
        s.Append($"</ul></div><div><h3>Sem alteração há mais de 2 anos</h3><p>{H(Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos"))}, {H(Formatador.Tamanho(antigos.Tamanho))}</p></div></div>");
    }

    private static string H(string texto) => WebUtility.HtmlEncode(texto);

    private static string Logo()
    {
        using var fluxo = typeof(PaginaAvaliacao).Assembly.GetManifestResourceStream("mt-logo.png")!;
        using var memoria = new MemoryStream();
        fluxo.CopyTo(memoria);
        return Convert.ToBase64String(memoria.ToArray());
    }

    private const string Estilo =
        "body{font-family:Segoe UI,Arial,sans-serif;color:#202020;background:#F4F4F4;margin:0}" +
        "header{display:flex;gap:16px;align-items:center;background:#006B2D;color:#fff;padding:12px 20px}" +
        "header img{height:56px;background:#fff;border-radius:8px;padding:4px}header h1{margin:0;font-size:22px}header p{margin:4px 0 0}" +
        "main{max-width:1100px;margin:0 auto;padding:16px 20px}h2{color:#006B2D;border-left:4px solid #43A92C;padding-left:8px}" +
        "table{width:100%;border-collapse:collapse;background:#fff}th,td{border:1px solid #ddd;padding:6px;vertical-align:top;font-size:13px}" +
        "th{background:#eef6ea;text-align:left}td.n{text-align:right;white-space:nowrap}.opcoes label{display:block;white-space:nowrap}" +
        "input[type=text],textarea{width:100%;box-sizing:border-box;margin:2px 0}.resumo{display:flex;gap:24px;flex-wrap:wrap}" +
        ".autorizacao{font-size:12px;color:#444}.botoes button{background:#0F8F2F;color:#fff;border:0;border-radius:16px;padding:8px 16px;font-size:14px;cursor:pointer}" +
        "footer{margin-top:24px;font-size:12px;color:#666}" +
        "@media print{body{background:#fff}.botoes{display:none}header{background:#fff;color:#006B2D;border-bottom:2px solid #006B2D}" +
        "input[type=text],textarea{border:0;border-bottom:1px solid #999}tr{page-break-inside:avoid}}";

    // Sem acento e sem caractere especial: o teste de caracteres proibidos lê este arquivo.
    private const string Codigo =
        "function salvar(){var nome=document.getElementById('decidido').value.trim();" +
        "if(!nome){alert('Escreva o seu nome em Quem decide antes de salvar.');return;}" +
        "var itens=[];document.querySelectorAll('tr.item').forEach(function(tr){" +
        "var m=tr.querySelector('input[type=radio]:checked');" +
        "itens.push({numero:Number(tr.dataset.numero),caminho:tr.dataset.caminho,bytes:tr.dataset.bytes,modificacao:tr.dataset.mod," +
        "decisao:m?m.value:'',destino:tr.querySelector('.destino').value,observacao:tr.querySelector('.obs').value});});" +
        "var r={formato:FORMATO,versao:1,relatorio:REL,decididoPor:nome,data:new Date().toISOString()," +
        "observacao:document.getElementById('obsgeral').value,itens:itens};" +
        "var b=new Blob([JSON.stringify(r,null,1)],{type:'application/json'});var a=document.createElement('a');" +
        "a.href=URL.createObjectURL(b);a.download='avaliacao-'+REL+'-resposta.json';document.body.appendChild(a);a.click();a.remove();}";
}
```

`Sobre.SiteMt` e `ExecutorCli.Versao` já existem no núcleo. O único endereço de fora é o link do rodapé, que só abre se o cliente clicar.

- [ ] **Passo 7: rodar e ver passar.** Expected: PASS em todos, inclusive o teste de caracteres proibidos.

- [ ] **Passo 8: commit** `Cria a pagina do relatorio para o cliente`, com a seção 10 da verificação jurídica.

---

### Tarefa 4: gravar a planilha

**Arquivos:** novo `src/mapdisk.nucleo/relatorios/planilha-avaliacao.cs`. Teste: acrescentar em `relatorio-cliente-testes.cs`.

**Interfaces:**
- Produz: `static class PlanilhaAvaliacao` com `void Gravar(Avaliacao a, string arquivo)`, `const int LinhaCabecalho = 10`, `const string AbaAvaliacao = "Avaliação"`, `const string AbaControle = "Controle"`.

Leiaute da aba Avaliação: A1 título; A2 e B2 cliente; A3 e B3 pasta; A4 e B4 data da varredura; A5 e B5 número do relatório; A6 "Decidido por" e B6 vazia; A7 "Data da decisão" e B7 vazia; A8 o texto de autorização; linha 10 com o cabeçalho Nº, Nome, Tipo, Caminho, Tamanho, Arquivos, Última alteração, Motivo, Decisão, Destino (se Mover), Observação (colunas A a K); itens da linha 11 em diante. Aba Controle, oculta: A1 "relatorio", B1 o número; linha 2 com o cabeçalho; da linha 3 em diante, número, caminho, bytes e ticks, todos como texto.

- [ ] **Passo 1: escrever o teste**

```csharp
    [Fact]
    public void Planilha_tem_as_duas_abas_e_a_lista_de_opcoes()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("avaliacao.xlsx");

        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);

        using var zip = System.IO.Compression.ZipFile.OpenRead(arquivo);
        var livro = LerEntrada(zip, "xl/workbook.xml");
        Assert.Contains("name=\"Avaliação\"", livro);
        Assert.Contains("name=\"Controle\" sheetId=\"2\" state=\"hidden\"", livro);
        var aba = LerEntrada(zip, "xl/worksheets/sheet1.xml");
        Assert.Contains("<formula1>\"Apagar,Mover,Manter,Conversar\"</formula1>", aba);
        Assert.Contains("sqref=\"I11:I12\"", aba);
        Assert.Contains("Cliente Exemplo", aba);
        var controle = LerEntrada(zip, "xl/worksheets/sheet2.xml");
        Assert.Contains("20260930-140509", controle);
        Assert.Contains(new DateTime(2023, 1, 10).Ticks.ToString(), controle);
    }

    internal static string LerEntrada(System.IO.Compression.ZipArchive zip, string nome)
    {
        using var leitor = new StreamReader(zip.GetEntry(nome)!.Open());
        return leitor.ReadToEnd();
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar a gravação**

```csharp
using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace MapDisk.Nucleo;

/// <summary>
/// Planilha .xlsx montada à mão, sem pacote: um zip com o XML das abas. A aba Avaliação é a do
/// cliente, com a lista de opções na coluna Decisão. A aba Controle, oculta, guarda o caminho,
/// os bytes e a data de cada item para casar a resposta.
/// </summary>
public static class PlanilhaAvaliacao
{
    public const int LinhaCabecalho = 10;
    public const string AbaAvaliacao = "Avaliação";
    public const string AbaControle = "Controle";

    private static readonly string[] Cabecalho =
        ["Nº", "Nome", "Tipo", "Caminho", "Tamanho", "Arquivos", "Última alteração", "Motivo", "Decisão", "Destino (se Mover)", "Observação"];

    private static readonly UTF8Encoding SemBom = new(false);

    public static void Gravar(Avaliacao a, string arquivo)
    {
        using var fluxo = new FileStream(arquivo, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fluxo, ZipArchiveMode.Create);
        Escrever(zip, "[Content_Types].xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        Escrever(zip, "_rels/.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Escrever(zip, "xl/workbook.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            $"<sheets><sheet name=\"{AbaAvaliacao}\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"{AbaControle}\" sheetId=\"2\" state=\"hidden\" r:id=\"rId2\"/></sheets></workbook>");
        Escrever(zip, "xl/_rels/workbook.xml.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        Escrever(zip, "xl/styles.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs>" +
            "<cellXfs count=\"2\"><xf/><xf fontId=\"1\" applyFont=\"1\"/></cellXfs></styleSheet>");
        Escrever(zip, "xl/worksheets/sheet1.xml", Avaliacao(a));
        Escrever(zip, "xl/worksheets/sheet2.xml", Controle(a));
    }

    private static string Avaliacao(Avaliacao a)
    {
        var linhas = new StringBuilder();
        Linha(linhas, 1, Texto("A1", "Avaliação de espaço - MapDisk - MT", negrito: true));
        Linha(linhas, 2, Texto("A2", "Cliente", true) + Texto("B2", a.Cliente));
        Linha(linhas, 3, Texto("A3", "Pasta", true) + Texto("B3", a.Pasta.CaminhoCompleto()));
        Linha(linhas, 4, Texto("A4", "Varredura", true) + Texto("B4", a.Gerado.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        Linha(linhas, 5, Texto("A5", "Relatório", true) + Texto("B5", a.Numero));
        Linha(linhas, 6, Texto("A6", "Decidido por", true));
        Linha(linhas, 7, Texto("A7", "Data da decisão", true));
        Linha(linhas, 8, Texto("A8", TextosAvaliacao.Autorizacao));
        Linha(linhas, LinhaCabecalho, string.Concat(Cabecalho.Select((c, n) => Texto($"{Coluna(n)}{LinhaCabecalho}", c, true))));
        var r = LinhaCabecalho + 1;
        foreach (var i in a.Itens)
        {
            Linha(linhas, r,
                Numero($"A{r}", i.Numero) + Texto($"B{r}", i.Item.Nome) + Texto($"C{r}", i.Tipo) + Texto($"D{r}", i.Item.Caminho) +
                Texto($"E{r}", Formatador.Tamanho(i.Item.Tamanho)) + Numero($"F{r}", i.Arquivos) + Texto($"G{r}", Formatador.Data(i.Item.Modificacao)) +
                Texto($"H{r}", i.Motivo));
            r++;
        }

        var ultima = Math.Max(LinhaCabecalho + 1, r - 1);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{LinhaCabecalho}\" topLeftCell=\"A{LinhaCabecalho + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
            "<cols><col min=\"1\" max=\"1\" width=\"6\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"30\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"9\" customWidth=\"1\"/>" +
            "<col min=\"4\" max=\"4\" width=\"50\" customWidth=\"1\"/><col min=\"5\" max=\"7\" width=\"14\" customWidth=\"1\"/><col min=\"8\" max=\"8\" width=\"36\" customWidth=\"1\"/>" +
            "<col min=\"9\" max=\"9\" width=\"12\" customWidth=\"1\"/><col min=\"10\" max=\"11\" width=\"30\" customWidth=\"1\"/></cols>" +
            $"<sheetData>{linhas}</sheetData>" +
            $"<dataValidations count=\"1\"><dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" sqref=\"I{LinhaCabecalho + 1}:I{ultima}\">" +
            "<formula1>\"Apagar,Mover,Manter,Conversar\"</formula1></dataValidation></dataValidations></worksheet>";
    }

    private static string Controle(Avaliacao a)
    {
        var linhas = new StringBuilder();
        Linha(linhas, 1, Texto("A1", "relatorio") + Texto("B1", a.Numero));
        Linha(linhas, 2, Texto("A2", "numero") + Texto("B2", "caminho") + Texto("C2", "bytes") + Texto("D2", "ticks"));
        var r = 3;
        foreach (var i in a.Itens)
        {
            Linha(linhas, r, Texto($"A{r}", i.Numero.ToString(CultureInfo.InvariantCulture)) + Texto($"B{r}", i.Item.Caminho) +
                Texto($"C{r}", i.Item.Tamanho.ToString(CultureInfo.InvariantCulture)) + Texto($"D{r}", i.Item.Modificacao.Ticks.ToString(CultureInfo.InvariantCulture)));
            r++;
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            $"<sheetData>{linhas}</sheetData></worksheet>";
    }

    private static void Linha(StringBuilder s, int numero, string celulas) => s.Append($"<row r=\"{numero}\">{celulas}</row>");

    private static string Texto(string referencia, string texto, bool negrito = false) =>
        $"<c r=\"{referencia}\" t=\"inlineStr\"{(negrito ? " s=\"1\"" : string.Empty)}><is><t xml:space=\"preserve\">{SecurityElement.Escape(texto)}</t></is></c>";

    private static string Numero(string referencia, long numero) => $"<c r=\"{referencia}\"><v>{numero}</v></c>";

    private static string Coluna(int indice) => ((char)('A' + indice)).ToString();

    private static void Escrever(ZipArchive zip, string nome, string conteudo)
    {
        using var escritor = new StreamWriter(zip.CreateEntry(nome, CompressionLevel.Optimal).Open(), SemBom);
        escritor.Write(conteudo);
    }
}
```

O `XDocument` e os outros `using` servem à leitura, na tarefa 5.

- [ ] **Passo 4: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 5: commit** `Cria a planilha do relatorio para o cliente`. A abertura no Excel de verdade, sem aviso de reparo, fica no "Como testar" do Pull Request, para o Manfred.

---

### Tarefa 5: resposta, planilha lida e casamento com a árvore

**Arquivos:** novo `src/mapdisk.nucleo/relatorios/resposta-avaliacao.cs`; `planilha-avaliacao.cs` ganha `Ler`. Teste: acrescentar em `relatorio-cliente-testes.cs`.

**Interfaces:**
- Produz: `enum Decisao { SemDecisao, Apagar, Mover, Manter, Conversar }`; `sealed record DecisaoItem(int Numero, string Caminho, long Bytes, long Ticks, Decisao Decisao, string Destino, string Observacao)`; `sealed record RespostaAvaliacao(string Relatorio, string Pasta, string DecididoPor, string Data, string Observacao, IReadOnlyList<DecisaoItem> Itens)` com `static RespostaAvaliacao LerJson(string texto)`, `static RespostaAvaliacao Ler(string arquivo)` (pela extensão: `.xlsx` ou `.json`), `static IReadOnlyList<int> Numeros(string texto)` e `RespostaAvaliacao ComDecisao(IEnumerable<int> numeros, Decisao decisao)`; `sealed record ItemCasado(DecisaoItem Decisao, ItemAcao? Item, string? Problema)`; `static IReadOnlyList<ItemCasado> RespostaAvaliacao.Casar(RespostaAvaliacao resposta, NoPasta raiz)`; `static RespostaAvaliacao PlanilhaAvaliacao.Ler(string arquivo)`. Leitura inválida lança `FormatException` com a mensagem para o técnico.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Planilha_preenchida_como_o_excel_salva_e_lida_de_volta()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("avaliacao.xlsx");
        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);
        PreencherComoExcel(arquivo, decididoPor: "Ana Cliente", i11: "Apagar", j12: "arquivo morto", i12: "Mover");

        var r = PlanilhaAvaliacao.Ler(arquivo);

        Assert.Equal("20260930-140509", r.Relatorio);
        Assert.Equal("Ana Cliente", r.DecididoPor);
        Assert.Equal([Decisao.Apagar, Decisao.Mover], r.Itens.Select(i => i.Decisao));
        Assert.Equal("arquivo morto", r.Itens[1].Destino);
        Assert.Equal(@"C:\video.mp4", r.Itens[0].Caminho);
        Assert.Equal(new DateTime(2023, 1, 10).Ticks, r.Itens[0].Ticks);
    }

    // Faz o que o Excel faz ao salvar: textos na tabela de textos compartilhados.
    private static void PreencherComoExcel(string arquivo, string decididoPor, string i11, string i12, string j12)
    {
        using var zip = System.IO.Compression.ZipFile.Open(arquivo, System.IO.Compression.ZipArchiveMode.Update);
        var entrada = zip.GetEntry("xl/worksheets/sheet1.xml")!;
        System.Xml.Linq.XDocument aba;
        using (var leitura = entrada.Open())
        {
            aba = System.Xml.Linq.XDocument.Load(leitura);
        }

        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        string[] textos = [decididoPor, i11, i12, j12];
        void Por(string referencia, int indice)
        {
            var numeroLinha = int.Parse(referencia[1..]);
            var linha = aba.Descendants(ns + "row").First(l => (int)l.Attribute("r")! == numeroLinha);
            linha.Add(new System.Xml.Linq.XElement(ns + "c", new System.Xml.Linq.XAttribute("r", referencia), new System.Xml.Linq.XAttribute("t", "s"),
                new System.Xml.Linq.XElement(ns + "v", indice)));
        }

        Por("B6", 0);
        Por("I11", 1);
        Por("I12", 2);
        Por("J12", 3);
        entrada.Delete();
        using (var escrita = zip.CreateEntry("xl/worksheets/sheet1.xml").Open())
        {
            aba.Save(escrita);
        }

        var compartilhados = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement(ns + "sst",
            textos.Select(t => new System.Xml.Linq.XElement(ns + "si", new System.Xml.Linq.XElement(ns + "t", t)))));
        using var saida = zip.CreateEntry("xl/sharedStrings.xml").Open();
        compartilhados.Save(saida);
    }

    [Fact]
    public void Resposta_da_pagina_em_json()
    {
        var json = """
            {"formato":"mapdisk-avaliacao-resposta","versao":1,"relatorio":"20260930-140509","decididoPor":"Ana","data":"2026-10-01T10:00:00Z",
             "observacao":"ok","itens":[{"numero":1,"caminho":"C:\\video.mp4","bytes":"5000","modificacao":"638091648000000000",
             "decisao":"apagar","destino":"","observacao":""},{"numero":2,"caminho":"C:\\Users","bytes":"2400","modificacao":"0",
             "decisao":"","destino":"","observacao":"ver com o financeiro"}]}
            """;

        var r = RespostaAvaliacao.LerJson(json);

        Assert.Equal("Ana", r.DecididoPor);
        Assert.Equal([Decisao.Apagar, Decisao.SemDecisao], r.Itens.Select(i => i.Decisao));
        Assert.Equal(5000, r.Itens[0].Bytes);
        Assert.Equal("ver com o financeiro", r.Itens[1].Observacao);
    }

    [Fact]
    public void Json_de_outro_formato_e_recusado()
    {
        var erro = Assert.Throws<FormatException>(() => RespostaAvaliacao.LerJson("{\"formato\":\"outro\"}"));

        Assert.Equal("Este arquivo não é uma resposta de relatório do MapDisk.", erro.Message);
    }

    [Theory]
    [InlineData("1, 3, 7-9", new[] { 1, 3, 7, 8, 9 })]
    [InlineData("2", new[] { 2 })]
    [InlineData(" 4 ; 5 ", new[] { 4, 5 })]
    public void Numeros_com_intervalos(string texto, int[] esperado)
    {
        Assert.Equal(esperado, RespostaAvaliacao.Numeros(texto));
    }

    [Fact]
    public void Numeros_invalidos_explicam()
    {
        Assert.Equal("Use números e intervalos, como 1, 3, 7-9.", Assert.Throws<FormatException>(() => RespostaAvaliacao.Numeros("um")).Message);
    }

    [Fact]
    public void Casar_acha_os_itens_e_aponta_o_que_mudou_ou_sumiu()
    {
        var raiz = AnalisesTestes.Exemplo();
        var video = raiz.Arquivos[0];
        var resposta = new RespostaAvaliacao("1", @"C:\", "Ana", "", "",
        [
            new DecisaoItem(1, @"C:\video.mp4", video.Tamanho, video.Modificacao.Ticks, Decisao.Apagar, "", ""),
            new DecisaoItem(2, @"C:\Users\bruno", 999, 0, Decisao.Apagar, "", ""),
            new DecisaoItem(3, @"C:\Users\carla", 10, 0, Decisao.Apagar, "", ""),
        ]);

        var c = RespostaAvaliacao.Casar(resposta, raiz);

        Assert.Null(c[0].Problema);
        Assert.Equal("video.mp4", c[0].Item!.Nome);
        Assert.Equal("mudou depois do relatório", c[1].Problema);
        Assert.Equal("não encontrado", c[2].Problema);
        Assert.Null(c[2].Item);
    }

    [Fact]
    public void Marcar_pelos_numeros_muda_so_esses()
    {
        var r = new RespostaAvaliacao("1", @"C:\", "", "", "",
            [new DecisaoItem(1, "a", 1, 0, Decisao.SemDecisao, "", ""), new DecisaoItem(2, "b", 1, 0, Decisao.Manter, "", "")]);

        var nova = r.ComDecisao([2], Decisao.Apagar);

        Assert.Equal([Decisao.SemDecisao, Decisao.Apagar], nova.Itens.Select(i => i.Decisao));
    }
```

O `modificacao` do JSON desse teste é um número qualquer: a leitura só o guarda, e a conferência de data fica no teste de casamento.

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar `resposta-avaliacao.cs`**

```csharp
using System.Globalization;
using System.Text.Json;

namespace MapDisk.Nucleo;

public enum Decisao
{
    SemDecisao,
    Apagar,
    Mover,
    Manter,
    Conversar,
}

public sealed record DecisaoItem(int Numero, string Caminho, long Bytes, long Ticks, Decisao Decisao, string Destino, string Observacao);

public sealed record ItemCasado(DecisaoItem Decisao, ItemAcao? Item, string? Problema);

/// <summary>A resposta do cliente, vinda da planilha ou da página. Ela só seleciona: agir passa pela confirmação (regra 1).</summary>
public sealed record RespostaAvaliacao(string Relatorio, string Pasta, string DecididoPor, string Data, string Observacao, IReadOnlyList<DecisaoItem> Itens)
{
    public static RespostaAvaliacao Ler(string arquivo) => Path.GetExtension(arquivo).ToLowerInvariant() switch
    {
        ".xlsx" => PlanilhaAvaliacao.Ler(arquivo),
        ".json" => LerJson(File.ReadAllText(arquivo)),
        _ => throw new FormatException("Escolha a planilha (.xlsx) ou o arquivo de resposta da página (.json)."),
    };

    public static RespostaAvaliacao LerJson(string texto)
    {
        try
        {
            using var doc = JsonDocument.Parse(texto);
            var r = doc.RootElement;
            if (!r.TryGetProperty("formato", out var formato) || formato.GetString() != PaginaAvaliacao.FormatoResposta)
            {
                throw new FormatException("Este arquivo não é uma resposta de relatório do MapDisk.");
            }

            var itens = r.GetProperty("itens").EnumerateArray().Select(i => new DecisaoItem(
                i.GetProperty("numero").GetInt32(),
                i.GetProperty("caminho").GetString() ?? string.Empty,
                Longo(i.GetProperty("bytes")),
                Longo(i.GetProperty("modificacao")),
                DecisaoDe(i.GetProperty("decisao").GetString()),
                i.GetProperty("destino").GetString() ?? string.Empty,
                i.GetProperty("observacao").GetString() ?? string.Empty)).ToList();
            return new RespostaAvaliacao(
                r.GetProperty("relatorio").GetString() ?? string.Empty,
                r.TryGetProperty("pasta", out var pasta) ? pasta.GetString() ?? string.Empty : string.Empty,
                r.GetProperty("decididoPor").GetString() ?? string.Empty,
                r.GetProperty("data").GetString() ?? string.Empty,
                r.TryGetProperty("observacao", out var obs) ? obs.GetString() ?? string.Empty : string.Empty,
                itens);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new FormatException("O arquivo de resposta está incompleto ou foi alterado.", e);
        }
    }

    public static Decisao DecisaoDe(string? texto) => texto?.Trim().ToLowerInvariant() switch
    {
        "apagar" => Decisao.Apagar,
        "mover" => Decisao.Mover,
        "manter" => Decisao.Manter,
        "conversar" => Decisao.Conversar,
        _ => Decisao.SemDecisao,
    };

    /// <summary>"1, 3, 7-9" em 1, 3, 7, 8, 9. Vírgula ou ponto e vírgula separam.</summary>
    public static IReadOnlyList<int> Numeros(string texto)
    {
        var numeros = new List<int>();
        foreach (var parte in texto.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var faixa = parte.Split('-', StringSplitOptions.TrimEntries);
            if (faixa.Length == 1 && int.TryParse(faixa[0], out var um))
            {
                numeros.Add(um);
            }
            else if (faixa.Length == 2 && int.TryParse(faixa[0], out var de) && int.TryParse(faixa[1], out var ate) && de <= ate)
            {
                numeros.AddRange(Enumerable.Range(de, ate - de + 1));
            }
            else
            {
                throw new FormatException("Use números e intervalos, como 1, 3, 7-9.");
            }
        }

        return numeros;
    }

    public RespostaAvaliacao ComDecisao(IEnumerable<int> numeros, Decisao decisao)
    {
        var marcar = numeros.ToHashSet();
        return this with { Itens = Itens.Select(i => marcar.Contains(i.Numero) ? i with { Decisao = decisao } : i).ToList() };
    }

    /// <summary>Casa cada item pelo caminho com a varredura atual. Tamanho ou data diferentes: fica fora das ações.</summary>
    public static IReadOnlyList<ItemCasado> Casar(RespostaAvaliacao resposta, NoPasta raiz) => resposta.Itens.Select(d =>
    {
        ItemAcao? item = null;
        if (raiz.Encontrar(d.Caminho) is { Estado: EstadoPasta.Lida } pasta)
        {
            item = ItemAcao.DaPasta(pasta);
        }
        else if (raiz.Encontrar(Path.GetDirectoryName(d.Caminho) ?? string.Empty) is { } pai
            && pai.Arquivos.FirstOrDefault(a => string.Equals(a.Nome, Path.GetFileName(d.Caminho), StringComparison.OrdinalIgnoreCase)) is { Nome: not null } arquivo)
        {
            item = ItemAcao.DoArquivo(pai, arquivo);
        }

        if (item is null)
        {
            return new ItemCasado(d, null, "não encontrado");
        }

        var mudou = item.Tamanho != d.Bytes || (d.Ticks != 0 && item.Modificacao.Ticks != d.Ticks);
        return new ItemCasado(d, item, mudou ? "mudou depois do relatório" : null);
    }).ToList();

    private static long Longo(JsonElement e) => e.ValueKind == JsonValueKind.Number
        ? e.GetInt64()
        : long.Parse(e.GetString() ?? "0", CultureInfo.InvariantCulture);
}
```

`raiz.Encontrar` é o da fatia 5. O teste de casamento usa `Ticks` 0 na pasta `bruno` e, mesmo assim, ela aparece como "mudou" pelo tamanho (999 contra 2.100). Na pasta, a data da varredura é a da própria pasta (`ModificacaoPropria`).

- [ ] **Passo 4: implementar `PlanilhaAvaliacao.Ler`**

```csharp
    public static RespostaAvaliacao Ler(string arquivo)
    {
        try
        {
            using var zip = ZipFile.OpenRead(arquivo);
            var textos = LerTextos(zip);
            var abas = LerAbas(zip);
            if (!abas.TryGetValue(AbaControle, out var controle))
            {
                throw new PlanilhaSemControleException("A planilha não tem a aba Controle. Use a planilha gerada pelo MapDisk.");
            }

            var avaliacao = abas.TryGetValue(AbaAvaliacao, out var caminho) ? caminho : abas.First(a => a.Key != AbaControle).Value;
            var c = LerCelulas(zip, controle, textos);
            var a = LerCelulas(zip, avaliacao, textos);
            var itens = new List<DecisaoItem>();
            for (var r = 3; c.TryGetValue($"A{r}", out var numeroTexto); r++)
            {
                var numero = int.Parse(numeroTexto, CultureInfo.InvariantCulture);
                var linha = LinhaCabecalho + numero;
                itens.Add(new DecisaoItem(
                    numero,
                    c.GetValueOrDefault($"B{r}", string.Empty),
                    long.Parse(c.GetValueOrDefault($"C{r}", "0"), CultureInfo.InvariantCulture),
                    long.Parse(c.GetValueOrDefault($"D{r}", "0"), CultureInfo.InvariantCulture),
                    RespostaAvaliacao.DecisaoDe(a.GetValueOrDefault($"I{linha}")),
                    a.GetValueOrDefault($"J{linha}", string.Empty),
                    a.GetValueOrDefault($"K{linha}", string.Empty)));
            }

            return new RespostaAvaliacao(c.GetValueOrDefault("B1", string.Empty), a.GetValueOrDefault("B3", string.Empty),
                a.GetValueOrDefault("B6", string.Empty), a.GetValueOrDefault("B7", string.Empty), string.Empty, itens);
        }
        catch (PlanilhaSemControleException e)
        {
            throw new FormatException(e.Message);
        }
        catch (Exception e) when (e is InvalidDataException or System.Xml.XmlException or FormatException or OverflowException or KeyNotFoundException or NullReferenceException)
        {
            throw new FormatException("A planilha está danificada ou não é a do relatório.", e);
        }
    }

    // Separa a planilha sem a aba Controle das planilhas danificadas, que têm outra mensagem.
    private sealed class PlanilhaSemControleException(string mensagem) : Exception(mensagem);

    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace NsR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace NsRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    private static List<string> LerTextos(ZipArchive zip)
    {
        if (zip.GetEntry("xl/sharedStrings.xml") is not { } entrada)
        {
            return [];
        }

        using var fluxo = entrada.Open();
        return XDocument.Load(fluxo).Descendants(Ns + "si").Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value))).ToList();
    }

    // Nome da aba no caminho do XML dela, pelo livro e pelas relações.
    private static Dictionary<string, string> LerAbas(ZipArchive zip)
    {
        XDocument livro;
        XDocument relacoes;
        using (var f = zip.GetEntry("xl/workbook.xml")!.Open())
        {
            livro = XDocument.Load(f);
        }

        using (var f = zip.GetEntry("xl/_rels/workbook.xml.rels")!.Open())
        {
            relacoes = XDocument.Load(f);
        }

        var alvos = relacoes.Descendants(NsRel + "Relationship").ToDictionary(r => (string)r.Attribute("Id")!, r => (string)r.Attribute("Target")!);
        return livro.Descendants(Ns + "sheet").ToDictionary(
            s => (string)s.Attribute("name")!,
            s =>
            {
                var alvo = alvos[(string)s.Attribute(NsR + "id")!];
                return alvo.StartsWith('/') ? alvo.TrimStart('/') : "xl/" + alvo;
            });
    }

    private static Dictionary<string, string> LerCelulas(ZipArchive zip, string caminho, List<string> textos)
    {
        using var fluxo = zip.GetEntry(caminho)!.Open();
        var celulas = new Dictionary<string, string>();
        foreach (var c in XDocument.Load(fluxo).Descendants(Ns + "c"))
        {
            var referencia = (string?)c.Attribute("r");
            if (referencia is null)
            {
                continue;
            }

            var tipo = (string?)c.Attribute("t");
            var valor = (string?)c.Element(Ns + "v");
            celulas[referencia] = tipo switch
            {
                "s" when int.TryParse(valor, out var i) && i < textos.Count => textos[i],
                "inlineStr" => string.Concat(c.Descendants(Ns + "t").Select(t => t.Value)),
                _ => valor ?? string.Empty,
            };
        }

        return celulas;
    }
```

Teste que acompanha, no passo 1 desta tarefa:

```csharp
    [Fact]
    public void Planilha_sem_a_aba_controle_explica()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("sem-controle.xlsx");
        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);
        using (var zip = System.IO.Compression.ZipFile.Open(arquivo, System.IO.Compression.ZipArchiveMode.Update))
        {
            var livro = LerEntrada(zip, "xl/workbook.xml");
            zip.GetEntry("xl/workbook.xml")!.Delete();
            using var escrita = new StreamWriter(zip.CreateEntry("xl/workbook.xml").Open());
            escrita.Write(livro.Replace("<sheet name=\"Controle\" sheetId=\"2\" state=\"hidden\" r:id=\"rId2\"/>", string.Empty));
        }

        var erro = Assert.Throws<FormatException>(() => PlanilhaAvaliacao.Ler(arquivo));

        Assert.Equal("A planilha não tem a aba Controle. Use a planilha gerada pelo MapDisk.", erro.Message);
    }
```

- [ ] **Passo 5: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 6: commit** `Le a resposta do cliente e casa com a arvore`. A planilha salva pelo Excel de verdade é lida no teste do Manfred, no Pull Request. Se ele achar um jeito de salvar que a leitura não entende, o caso vira teste e correção no mesmo PR.

---

### Tarefa 6: estado das janelas do relatório e da resposta

**Arquivos:** novo `src/mapdisk.nucleo/painel/painel-relatorio.cs`. Teste: acrescentar em `relatorio-cliente-testes.cs`.

**Interfaces:**
- Consome: tarefas 1 a 5.
- Produz: `sealed record LinhaRelatorio(int Numero, string Nome, string Tipo, string Caminho, string TextoTamanho, string TextoArquivos, string TextoData, string Motivo)`; `sealed class PainelRelatorio : INotifyPropertyChanged` com `PainelRelatorio(NoPasta pasta, long? livre, string tecnico)`, `CriteriosSugestao Criterios { get; set; }`, `string Cliente`, `string Tecnico`, `string Mensagem`, `IReadOnlyList<LinhaRelatorio> Linhas`, `string TextoTotal`, `string? MotivoParaNaoGerar`, `void Sugerir(DateTime hoje)`, `void AcrescentarSelecao(IEnumerable<object>)`, `void Tirar(IEnumerable<int>)`, `Avaliacao Montar(DateTime agora)`, `string NomeSugerido(DateTime agora)` e `(string Pagina, string Planilha) Gerar(string arquivoPagina, DateTime agora)`; `sealed record LinhaResposta(int Numero, string Nome, string Caminho, string TextoTamanho, string TextoDecisao, string Destino, string Observacao, string Situacao, ItemCasado Casado)`; `sealed class PainelResposta : INotifyPropertyChanged` com `PainelResposta(NoPasta raiz)`, `void Ler(string arquivo)`, `void MarcarPorNumeros(string texto, Decisao decisao)`, `string Cabecalho`, `string Resumo`, `string? Aviso`, `IReadOnlyList<LinhaResposta> Linhas`, `IReadOnlyList<int> NumerosDe(Decisao)` e `IReadOnlyList<ItemAcao> ItensDe(IEnumerable<LinhaResposta>)`.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Painel_do_relatorio_monta_a_lista_e_pede_o_cliente()
    {
        var arvore = ArvoreAberta();
        var p = new PainelRelatorio(arvore.Raiz!, null, "tecnico");

        Assert.Equal("Monte a lista com Sugerir ou Acrescentar a seleção.", p.MotivoParaNaoGerar);
        p.AcrescentarSelecao([LinhaDe(arvore, "bruno")]);
        Assert.Equal("Escreva o nome do cliente.", p.MotivoParaNaoGerar);
        p.Cliente = "Cliente Exemplo";
        Assert.Null(p.MotivoParaNaoGerar);
        Assert.Equal("bruno", p.Linhas[0].Nome);
        Assert.Equal(Sugestao.EscolhidoPeloTecnico, p.Linhas[0].Motivo);
        Assert.Equal("1 item, 2,1 KB", p.TextoTotal);
    }

    [Fact]
    public void Painel_do_relatorio_grava_a_pagina_e_a_planilha()
    {
        using var pasta = new PastaTeste();
        var arvore = ArvoreAberta();
        var p = new PainelRelatorio(arvore.Raiz!, null, "tecnico") { Cliente = "Cliente Exemplo" };
        p.AcrescentarSelecao([LinhaDe(arvore, "bruno")]);
        var agora = new DateTime(2026, 9, 30, 14, 5, 9);

        var (pagina, planilha) = p.Gerar(pasta.Caminho(p.NomeSugerido(agora) + ".html"), agora);

        Assert.EndsWith("avaliacao-c-2026-09-30.html", pagina);
        Assert.EndsWith("avaliacao-c-2026-09-30.xlsx", planilha);
        Assert.True(File.Exists(pagina) && File.Exists(planilha));
    }

    [Fact]
    public void Painel_da_resposta_agrupa_e_da_os_itens_para_agir()
    {
        using var pasta = new PastaTeste();
        var raiz = AnalisesTestes.Exemplo();
        var arquivo = pasta.Caminho("r.json");
        var video = raiz.Arquivos[0];
        File.WriteAllText(arquivo, $$"""
            {"formato":"mapdisk-avaliacao-resposta","versao":1,"relatorio":"20260930-140509","decididoPor":"Ana","data":"2026-10-01",
             "itens":[{"numero":1,"caminho":"C:\\video.mp4","bytes":"{{video.Tamanho}}","modificacao":"{{video.Modificacao.Ticks}}","decisao":"apagar","destino":"","observacao":""},
                      {"numero":2,"caminho":"C:\\Users","bytes":"1","modificacao":"0","decisao":"conversar","destino":"","observacao":""}]}
            """);
        var p = new PainelResposta(raiz);

        p.Ler(arquivo);

        Assert.Equal("Resposta do relatório 20260930-140509, decidida por Ana em 2026-10-01", p.Cabecalho);
        Assert.Equal("Apagar: 1 item (4,9 KB) | Mover: 0 itens | Conversar: 1 item | Manter ou sem marca: 0 itens", p.Resumo);
        Assert.Equal([1], p.NumerosDe(Decisao.Apagar));
        Assert.Equal("mudou depois do relatório", p.Linhas[1].Situacao);
        Assert.Equal(["video.mp4"], p.ItensDe(p.Linhas).Select(i => i.Nome));
    }

    [Fact]
    public void Painel_da_resposta_marca_pelos_numeros_e_avisa_sem_varredura()
    {
        var raiz = AnalisesTestes.Exemplo();
        var p = new PainelResposta(new NoPasta(@"D:\", null));
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("r.xlsx");
        PlanilhaAvaliacao.Gravar(Exemplo(), arquivo);

        p.Ler(arquivo);
        p.MarcarPorNumeros("1", Decisao.Apagar);

        Assert.Equal([1], p.NumerosDe(Decisao.Apagar));
        Assert.Equal(@"Nenhum item foi encontrado na varredura atual. Varra a pasta do relatório antes: C:\", p.Aviso);
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar**

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public sealed record LinhaRelatorio(int Numero, string Nome, string Tipo, string Caminho, string TextoTamanho, string TextoArquivos, string TextoData, string Motivo);

/// <summary>Estado da janela "Relatório para o cliente", sem WPF.</summary>
public sealed class PainelRelatorio : INotifyPropertyChanged
{
    private readonly NoPasta _pasta;
    private readonly long? _livre;
    private readonly ListaAvaliacao _lista = new();

    public PainelRelatorio(NoPasta pasta, long? livre, string tecnico)
    {
        _pasta = pasta;
        _livre = livre;
        Tecnico = tecnico;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CriteriosSugestao Criterios { get; set; } = new();

    public string Cliente { get; set; } = string.Empty;

    public string Tecnico { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public string Titulo => $"Relatório para o cliente: {_pasta.CaminhoCompleto()}";

    public IReadOnlyList<LinhaRelatorio> Linhas => _lista.Itens.Select(i => new LinhaRelatorio(
        i.Numero, i.Item.Nome, i.Tipo, i.Item.Caminho, Formatador.Tamanho(i.Item.Tamanho),
        i.Item.EhPasta ? Formatador.Numero(i.Arquivos) : string.Empty, Formatador.Data(i.Item.Modificacao), i.Motivo)).ToList();

    public string TextoTotal => $"{Formatador.Plural(_lista.Itens.Count, "item", "itens")}, {Formatador.Tamanho(_lista.Total)}";

    public string? MotivoParaNaoGerar => _lista.Itens.Count == 0
        ? "Monte a lista com Sugerir ou Acrescentar a seleção."
        : string.IsNullOrWhiteSpace(Cliente) ? "Escreva o nome do cliente." : null;

    public void Sugerir(DateTime hoje)
    {
        Sugestao.Sugerir(_lista, _pasta, Criterios, hoje);
        Avisar();
    }

    public void AcrescentarSelecao(IEnumerable<object> selecionados)
    {
        foreach (var item in PreparadorAcoes.ItensDaSelecao(selecionados).Itens)
        {
            _lista.Acrescentar(item, Sugestao.EscolhidoPeloTecnico);
        }

        Avisar();
    }

    public void Tirar(IEnumerable<int> numeros)
    {
        _lista.Tirar(numeros);
        Avisar();
    }

    public Avaliacao Montar(DateTime agora) =>
        new(Avaliacao.NumeroDe(agora), Cliente.Trim(), Tecnico.Trim(), Mensagem.Trim(), _pasta, agora, _livre, _lista.Itens);

    public string NomeSugerido(DateTime agora) => Montar(agora).NomeDoArquivo;

    /// <summary>Grava a página no caminho escolhido e a planilha ao lado, com o mesmo nome.</summary>
    public (string Pagina, string Planilha) Gerar(string arquivoPagina, DateTime agora)
    {
        var avaliacao = Montar(agora);
        var planilha = Path.ChangeExtension(arquivoPagina, ".xlsx");
        File.WriteAllText(arquivoPagina, PaginaAvaliacao.Gerar(avaliacao), new System.Text.UTF8Encoding(false));
        PlanilhaAvaliacao.Gravar(avaliacao, planilha);
        return (arquivoPagina, planilha);
    }

    public void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

public sealed record LinhaResposta(int Numero, string Nome, string Caminho, string TextoTamanho, string TextoDecisao, string Destino, string Observacao, string Situacao, ItemCasado Casado);

/// <summary>Estado da janela "Resposta do cliente", sem WPF. Só seleciona: agir passa pela confirmação.</summary>
public sealed class PainelResposta : INotifyPropertyChanged
{
    private readonly NoPasta _raiz;
    private RespostaAvaliacao? _resposta;
    private IReadOnlyList<ItemCasado> _casados = [];

    public PainelResposta(NoPasta raiz) => _raiz = raiz;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Cabecalho => _resposta is not { } r ? "Escolha a planilha ou o arquivo de resposta."
        : $"Resposta do relatório {r.Relatorio}, decidida por {(r.DecididoPor.Length > 0 ? r.DecididoPor : "(sem nome)")} em {(r.Data.Length > 0 ? r.Data : "(sem data)")}";

    public string? Aviso => _resposta is not null && _casados.Count > 0 && _casados.All(c => c.Item is null)
        ? $"Nenhum item foi encontrado na varredura atual. Varra a pasta do relatório antes: {_resposta.Pasta}"
        : null;

    public IReadOnlyList<LinhaResposta> Linhas => _casados.Select(c => new LinhaResposta(
        c.Decisao.Numero,
        Path.GetFileName(c.Decisao.Caminho.TrimEnd('\\')),
        c.Decisao.Caminho,
        Formatador.Tamanho(c.Item?.Tamanho ?? c.Decisao.Bytes),
        c.Decisao.Decisao switch { Decisao.SemDecisao => "sem marca", var d => d.ToString() },
        c.Decisao.Destino,
        c.Decisao.Observacao,
        c.Problema ?? "ok",
        c)).ToList();

    public string Resumo
    {
        get
        {
            string Grupo(string nome, Func<Decisao, bool> filtro, bool comTamanho)
            {
                var itens = _casados.Where(c => filtro(c.Decisao.Decisao)).ToList();
                var texto = $"{nome}: {Formatador.Plural(itens.Count, "item", "itens")}";
                return comTamanho && itens.Count > 0 ? $"{texto} ({Formatador.Tamanho(itens.Sum(c => c.Item?.Tamanho ?? c.Decisao.Bytes))})" : texto;
            }

            return string.Join(" | ",
                Grupo("Apagar", d => d == Decisao.Apagar, true),
                Grupo("Mover", d => d == Decisao.Mover, true),
                Grupo("Conversar", d => d == Decisao.Conversar, false),
                Grupo("Manter ou sem marca", d => d is Decisao.Manter or Decisao.SemDecisao, false));
        }
    }

    public void Ler(string arquivo)
    {
        _resposta = RespostaAvaliacao.Ler(arquivo);
        _casados = RespostaAvaliacao.Casar(_resposta, _raiz);
        Avisar();
    }

    public void MarcarPorNumeros(string texto, Decisao decisao)
    {
        if (_resposta is null)
        {
            return;
        }

        _resposta = _resposta.ComDecisao(RespostaAvaliacao.Numeros(texto), decisao);
        _casados = RespostaAvaliacao.Casar(_resposta, _raiz);
        Avisar();
    }

    public IReadOnlyList<int> NumerosDe(Decisao decisao) =>
        _casados.Where(c => c.Decisao.Decisao == decisao).Select(c => c.Decisao.Numero).ToList();

    /// <summary>Só os itens achados e iguais ao relatório. O resto fica fora das ações (regra 1).</summary>
    public IReadOnlyList<ItemAcao> ItensDe(IEnumerable<LinhaResposta> linhas) =>
        linhas.Where(l => l.Casado.Problema is null && l.Casado.Item is not null).Select(l => l.Casado.Item!).ToList();

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
```

No teste de `ItensDe`, o item 2 tem problema ("mudou depois do relatório") e fica fora, embora a lista passada seja a inteira.

- [ ] **Passo 4: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 5: commit** `Cria o estado das janelas do relatorio e da resposta`.

---

### Tarefa 7: janelas e botão

**Arquivos:** novos `src/mapdisk/janela-relatorio.xaml` e `.xaml.cs`, `src/mapdisk/janela-resposta.xaml` e `.xaml.cs`; mudanças em `src/mapdisk/janela-principal.xaml` e `.xaml.cs`. Teste: acrescentar em `recursos-testes.cs`.

**Interfaces:**
- Consome: `PainelRelatorio`, `PainelResposta`, `PainelPrincipal.AvaliarItens`, o fluxo `Agir` da fatia 5.

- [ ] **Passo 1: teste de recurso**

```csharp
    [Fact]
    public void Janela_tem_o_relatorio_para_o_cliente()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Click=\"AoAbrirMenuRelatorio\"", janela);
        Assert.Contains("Click=\"AoGerarRelatorio\"", janela);
        Assert.Contains("Click=\"AoLerResposta\"", janela);
        Assert.True(File.Exists(App("janela-relatorio.xaml")));
        Assert.True(File.Exists(App("janela-resposta.xaml")));
    }
```

- [ ] **Passo 2: janela do relatório** (`JanelaRelatorio(PainelRelatorio painel, Func<IEnumerable<object>> selecaoAtual)`), no estilo das janelas da fatia 5, largura 960, altura 640, redimensionável:
  - faixa verde com `Titulo`;
  - cartão "Critérios": quatro campos numéricos (maiores pastas, maiores arquivos, anos sem alteração, tamanho mínimo em MB) e o botão **Sugerir**, que monta `Criterios` a partir dos campos e chama `Sugerir(DateTime.Now)`;
  - botões **Acrescentar a seleção** (chama `AcrescentarSelecao(selecaoAtual())`) e **Tirar da lista** (tira os números selecionados na lista);
  - `ListView` com seleção múltipla e as colunas Nº, Nome, Tipo, Tamanho, Arquivos, Última alteração, Motivo e Caminho, ligada a `Linhas`; `TextoTotal` embaixo;
  - campos Cliente, Técnico e "Mensagem para o cliente";
  - **Gerar**: se `MotivoParaNaoGerar` não for nulo, mostra o motivo; senão abre `SaveFileDialog` com `FileName = NomeSugerido(DateTime.Now) + ".html"`, filtro "Página HTML (*.html)|*.html" e `OverwritePrompt = true`, chama `Gerar` e mostra os dois caminhos, com o botão "Mostrar no Explorer" (`Shell.MostrarNoExplorer(pagina, ehArquivo: true)`).

- [ ] **Passo 3: janela da resposta** (`JanelaResposta(PainelResposta painel, Func<IReadOnlyList<ItemAcao>, bool, Task> agir)`, em que o `bool` diz se é mover):
  - faixa verde com "Resposta do cliente"; botão **Abrir resposta...** (`OpenFileDialog` com o filtro "Resposta do relatório (*.xlsx;*.json)|*.xlsx;*.json"), que chama `Ler` e mostra a `FormatException` numa caixa;
  - `Cabecalho`, `Aviso` (em destaque, quando houver) e `Resumo`;
  - `ListView` com seleção múltipla e as colunas Nº, Nome, Tamanho, Decisão, Destino, Observação, Situação e Caminho;
  - **Marcar pelos números**: um campo de texto, uma lista (Apagar, Mover, Conversar, Manter) e o botão Marcar, que chama `MarcarPorNumeros` e mostra a `FormatException` numa caixa;
  - **Selecionar os de Apagar** e **Selecionar os de Mover**: selecionam na lista as linhas com essa decisão;
  - **Apagar selecionados...** e **Mover selecionados...**: chamam `agir(painel.ItensDe(selecionadas), mover)`. As linhas com problema ficam fora; se sobrar nenhuma, a caixa diz "Nenhum item selecionado pode ser tratado: confira a coluna Situação.".

- [ ] **Passo 4: janela principal.**
  - No cabeçalho do cartão Pastas, antes de "Registro de ações": botão **Relatório do cliente** (`Click="AoAbrirMenuRelatorio"`), que abre um `ContextMenu` com "Gerar relatório..." (`AoGerarRelatorio`) e "Ler resposta..." (`AoLerResposta`), preso ao botão (`PlacementTarget` e `IsOpen = true` no clique).
  - Código:

```csharp
    private void AoAbrirMenuRelatorio(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } botao)
        {
            menu.PlacementTarget = botao;
            menu.IsOpen = true;
        }
    }

    private void AoGerarRelatorio(object sender, RoutedEventArgs e)
    {
        if (_painel.Arvore.Raiz is not { } raiz || _painel.Estado != EstadoPainel.Parado)
        {
            MessageBox.Show(this, "Varra a pasta primeiro e espere o fim da varredura.", "MapDisk - MT");
            return;
        }

        var tecnico = Environment.UserName;
        var relatorio = new PainelRelatorio(raiz, Volumes.Ler(raiz.CaminhoCompleto())?.Livre, tecnico);
        new JanelaRelatorio(relatorio, () => _ultimaSelecao?.Cast<object>() ?? []) { Owner = this }.ShowDialog();
    }

    private void AoLerResposta(object sender, RoutedEventArgs e)
    {
        var raiz = _painel.Arvore.Raiz;
        while (raiz?.Pai is { } pai)
        {
            raiz = pai;
        }

        if (raiz is null || _painel.Estado != EstadoPainel.Parado)
        {
            MessageBox.Show(this, "Varra a pasta do relatório antes de ler a resposta.", "MapDisk - MT");
            return;
        }

        new JanelaResposta(new PainelResposta(raiz), AgirSobre) { Owner = this }.ShowDialog();
    }

    // Itens vindos da resposta do cliente: as mesmas regras e a mesma confirmação da seleção.
    private async Task AgirSobre(IReadOnlyList<ItemAcao> itens, bool mover)
    {
        _painel.AvaliarItens(itens);
        if (!_painel.PodeRemover)
        {
            MessageBox.Show(this, _painel.MotivoBloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!mover)
        {
            await Agir(new PedidoAcao(_painel.Selecao.Remocao, _painel.Selecao.Itens, null));
            return;
        }

        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta de destino" };
        if (dialogo.ShowDialog(this) != true)
        {
            return;
        }

        if (_painel.Acoes.BloqueioDestino(_painel.Selecao.Itens, dialogo.FolderName) is { } bloqueio)
        {
            MessageBox.Show(this, bloqueio, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await Agir(new PedidoAcao(TipoAcao.Mover, _painel.Selecao.Itens, dialogo.FolderName));
    }
```

- [ ] **Passo 5: build e conferência pelo agente em `--demonstracao`.**
  - Sugerir, Acrescentar a seleção, Tirar da lista, Gerar para a pasta de saída dos testes.
  - Abrir a página no navegador interno, marcar duas decisões, salvar a resposta (o arquivo baixa na pasta do navegador; mover para a pasta de saída dos testes).
  - Ler a resposta na janela da resposta e conferir o resumo e os grupos. Na demonstração, "Apagar selecionados" segue a confirmação e não toca no disco.
  - Nada disso vai para o repositório.

- [ ] **Passo 6: commit** `Liga o relatorio para o cliente na janela`.

---

### Tarefa 8: documentação e Pull Request

- [ ] **Passo 1:** README: "Uso" com o relatório para o cliente (montar, gerar, o que o cliente faz, ler a resposta) e a linha da fatia 6 na "Situação do projeto".
- [ ] **Passo 2:** pendências da fatia 6: duplicados como critério do "Sugerir" (fatia 7); critérios salvos entre sessões (Opções, fatia 8).
- [ ] **Passo 3:** portões, commit `Traz a documentacao da fatia 6` e Pull Request do ramo `relatorio-cliente`, com "O que muda", "Como testar" (gerar, responder pela página e pela planilha, ler, marcar pelos números e apagar pela confirmação, numa pasta de teste) e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec

| Item da spec | Onde |
|---|---|
| Três formatos (planilha, página, PDF pela página) | Tarefas 3, 4 e 7 |
| Três jeitos de montar a lista (Sugerir, seleção, ajuste) | Tarefas 1, 2, 6 e 7 |
| Critérios padrão editáveis | Tarefas 2 e 7 |
| Opções Apagar, Mover, Manter, Conversar sem pré-marcação | Tarefas 3 e 4 (o teste confere que não há `checked`) |
| Número igual nos três formatos | Tarefas 2, 3 e 4 |
| Item repetido e pasta sem leitura fora | Tarefa 2 |
| Página sem nada de fora | Tarefa 3 |
| Planilha sem pacote, lida do Excel e do LibreOffice | Tarefas 4 e 5 |
| Casamento, "não encontrado", "mudou depois do relatório" | Tarefa 5 |
| Marcar pelos números | Tarefas 5, 6 e 7 |
| Resposta só seleciona, agir pela confirmação | Tarefas 1, 6 e 7 |
| Textos pela `legal-br` e pela `humanizar-ptbr` | Tarefa 3 |
| Testes da seção 9 da spec | Tarefas 2 a 6 |
