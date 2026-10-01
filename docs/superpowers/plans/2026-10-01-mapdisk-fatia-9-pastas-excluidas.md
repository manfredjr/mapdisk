# Plano da fatia 9: pastas excluídas da varredura

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** o técnico guarda uma lista de pastas que a varredura não lê, por nome (`node_modules`, `$Recycle.Bin`) ou por caminho completo (`D:\Backup\Veeam`). A pasta excluída aparece na árvore como "excluída", fora dos totais, contada na barra de status e citada nos relatórios. Nunca aparece como zero.

**Arquitetura:**
- **Regra:** fica em `varredura/regras-exclusao.cs`, sem WPF.
- **Motor:** recebe as regras antes de cada varredura. Ao montar as subpastas de uma pasta lida, marca as que batem com uma regra com o estado novo `EstadoPasta.Excluida`, do mesmo jeito que já marca o link.
- **Contagem:** a pasta excluída não é lida, não soma e entra num contador próprio, como "sem acesso" e "erro".
- **Gravação:** a lista vai num arquivo do perfil, `%LOCALAPPDATA%\MapDisk\excluidas.txt`, e é editada na tela de Opções.

**Tecnologia:** C# com .NET 8, WPF, xUnit. Sem pacote NuGet novo.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, seção 6 (Opções: "pastas excluídas da varredura") e regra 3 do produto. Pendência: "Pastas excluídas da varredura", adiada para depois da versão 1 por decisão do Manfred em 01/10/2026.

## Restrições globais

- Regras do produto do `AGENTS.md`:
  - **regra 3:** pasta excluída nunca vira 0. Ela aparece como "excluída", é contada e citada no total;
  - **regra 2:** a linha de comando continua só lendo;
  - **regra 7:** excluir pasta só diminui o que se lê.
- A pasta escolhida como alvo nunca é excluída: a regra vale só para o que está abaixo dela. Assim, "Atualizar esta pasta" numa pasta excluída lê essa pasta, que é o jeito de olhar dentro dela sem mudar a lista.
- Testes só dentro da pasta de saída dos testes. Resultado de teste no computador do Manfred fica na conversa.
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Tipos de regra | **Nome:** sem `\` e sem `:`, vale para toda pasta com esse nome, em qualquer nível. **Caminho:** completo, como `D:\Backup\Veeam` ou `\\servidor\dados\arquivo-morto`, vale só para essa pasta | Cobre os dois usos: pasta que se repete (`node_modules`, `.git`) e uma pasta grande conhecida de um servidor |
| Comparação | Sem diferença entre maiúscula e minúscula, como no Windows. Barra final ignorada | Igual ao Explorer |
| Curinga (`*`, `?`) | Fora. A regra com curinga é recusada com "Use o nome da pasta ou o caminho completo, sem * nem ?" | Curinga mal escrito exclui demais sem o técnico perceber, e a regra 3 pede que o técnico saiba o que ficou fora |
| Raiz de unidade | Recusada como regra: "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer" | Excluir `C:\` não faz sentido como subpasta |
| Quantas regras | Até 100. Regra repetida não entra | Lista para ler na tela |
| Lista padrão | Vazia | Nada fica fora sem o técnico pedir |
| Onde fica | `%LOCALAPPDATA%\MapDisk\excluidas.txt`, uma regra por linha. Linha inválida é ignorada na leitura | Mesmo modelo dos últimos alvos |
| Quando vale | Na próxima varredura. A tela de Opções avisa isso | A árvore já lida não muda sozinha |
| Alvo | Nunca excluído, mesmo que bata com uma regra | Quem escolheu varrer a pasta quer ver a pasta |
| Na árvore | Texto "excluída" nas colunas de valor e rótulo "excluída da varredura (regra: `node_modules`)". Não soma em nada | Regra 3 |
| Barra de status | "N pastas excluídas" ao lado de "sem acesso" e "erro de leitura" | Regra 3 |
| Análises e gráfico | Aviso "N pastas excluídas da varredura não entram nesta conta". No gráfico, linha própria: "Excluídas da varredura, fora do gráfico: ..." | Não confundir com "sem leitura" |
| Ações | Bloqueadas na pasta excluída: "pasta excluída da varredura. Use Atualizar esta pasta para ler antes de agir" | Sem tamanho lido, a confirmação não teria total certo |
| Relatórios | O relatório do técnico, o CSV e a página do relatório para o cliente citam as pastas excluídas, com estado "excluída" e a regra | O cliente e o técnico precisam saber o que ficou fora da conta |
| Linha de comando | `--excluir <nome ou caminho>`, que pode repetir. A linha de comando **não** usa a lista das Opções | O que a linha de comando lê fica todo na própria linha, sem depender de arquivo escondido no perfil |
| Duplicados e Sugerir | Ignoram as pastas excluídas, porque elas não foram lidas | Sem mudança de código, só teste |

## Git desta fatia

1. O plano entra pelo ramo `fatia-9-plano`, num Pull Request só do plano, com a linha da fatia 9 no README.
2. O código sai do `main` atualizado, no ramo `pastas-excluidas`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/varredura/regras-exclusao.cs` | `RegrasExclusao`, validação, armazém em arquivo e em memória |
| `src/mapdisk.nucleo/arvore/no-pasta.cs` | `EstadoPasta.Excluida`, `MarcarExcluida`, `PastasExcluidas` |
| `src/mapdisk.nucleo/varredura/motor-varredura.cs` | `IMotorVarredura.Exclusoes`, marcação das subpastas |
| `src/mapdisk.nucleo/painel/*` | Regras nas dependências e no painel, barra de status, linha da árvore, análises e gráfico |
| `src/mapdisk.nucleo/acoes/preparador-acoes.cs` | Bloqueio com motivo próprio |
| `src/mapdisk.nucleo/relatorios/*`, `grafico/itens-grafico.cs` | Estado "excluída" no CSV, no relatório do técnico e na página do relatório para o cliente |
| `src/mapdisk.nucleo/linha-de-comando/*` | `--excluir` |
| `src/mapdisk/janela-opcoes.xaml` e `.cs` | Seção "Pastas excluídas da varredura" |
| `testes/mapdisk.testes/regras-exclusao-testes.cs` | Testes desta fatia |

---

### Tarefa 1: regras de exclusão

**Arquivos:**
- Criar: `src/mapdisk.nucleo/varredura/regras-exclusao.cs`
- Teste: `testes/mapdisk.testes/regras-exclusao-testes.cs`

**Interfaces:**
- Produz:
  - `RegrasExclusao` (classe imutável), com `Nenhuma`, `De(IEnumerable<string>)`, `Regras`, `Motivo(string caminhoDaPasta, string nome)` e `Validar(string texto, out string? erro)`;
  - `IArmazemExclusoes` (`Ler()`, `Gravar(RegrasExclusao)`), com as implementações `ArquivoExclusoes` e `ExclusoesEmMemoria`.

- [ ] **Passo 1: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class RegrasExclusaoTestes
{
    [Fact]
    public void Nome_vale_em_qualquer_nivel_sem_diferenca_de_maiuscula()
    {
        var r = RegrasExclusao.De(["node_modules"]);
        Assert.Equal("node_modules", r.Motivo(@"D:\Projetos\site\node_modules", "node_modules"));
        Assert.Equal("node_modules", r.Motivo(@"D:\a\b\NODE_MODULES", "NODE_MODULES"));
        Assert.Null(r.Motivo(@"D:\Projetos\node_modules_velho", "node_modules_velho"));
    }

    [Fact]
    public void Caminho_vale_so_para_aquela_pasta()
    {
        var r = RegrasExclusao.De([@"D:\Backup\Veeam\"]);
        Assert.Equal(@"D:\Backup\Veeam", r.Motivo(@"d:\backup\veeam", "veeam"));
        Assert.Null(r.Motivo(@"E:\Backup\Veeam", "Veeam"));
        Assert.Null(r.Motivo(@"D:\Backup\Veeam2", "Veeam2"));
    }

    [Theory]
    [InlineData("", "Escreva o nome da pasta ou o caminho completo.")]
    [InlineData("*.tmp", "Use o nome da pasta ou o caminho completo, sem * nem ?")]
    [InlineData(@"C:\", "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.")]
    [InlineData(@"Projetos\velho", "Use só o nome da pasta, como node_modules, ou o caminho completo, como D:\\Backup.")]
    public void Regra_invalida_diz_o_motivo(string texto, string erro)
    {
        Assert.Null(RegrasExclusao.Validar(texto, out var motivo));
        Assert.Equal(erro, motivo);
    }

    [Fact]
    public void Repetida_e_invalida_ficam_de_fora_e_o_teto_e_100()
    {
        var r = RegrasExclusao.De(["node_modules", "NODE_MODULES", "*.tmp", ".git"]);
        Assert.Equal(["node_modules", ".git"], r.Regras);
        Assert.Equal(100, RegrasExclusao.De(Enumerable.Range(0, 150).Select(i => $"pasta{i}")).Regras.Count);
    }

    [Fact]
    public void Arquivo_grava_e_le_uma_regra_por_linha()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "exclusoes-" + Guid.NewGuid().ToString("N"), "excluidas.txt");
        var armazem = new ArquivoExclusoes(arquivo);
        Assert.Empty(armazem.Ler().Regras);
        armazem.Gravar(RegrasExclusao.De(["node_modules", @"D:\Backup"]));
        Assert.Equal(["node_modules", @"D:\Backup"], armazem.Ler().Regras);
        Directory.Delete(Path.GetDirectoryName(arquivo)!, true);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RegrasExclusaoTestes`
Esperado: não compila.

- [ ] **Passo 3: criar `regras-exclusao.cs`**

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Pastas que a varredura não lê. Nome (sem barra nem dois-pontos) vale em qualquer nível;
/// caminho completo vale só para aquela pasta. Sem curinga: o técnico precisa saber
/// exatamente o que ficou fora da conta (regra 3).
/// </summary>
public sealed class RegrasExclusao
{
    public const int Maximo = 100;

    public static readonly RegrasExclusao Nenhuma = new([]);

    private readonly HashSet<string> _nomes;
    private readonly HashSet<string> _caminhos;

    private RegrasExclusao(IReadOnlyList<string> regras)
    {
        Regras = regras;
        _nomes = new HashSet<string>(regras.Where(r => !EhCaminho(r)), StringComparer.OrdinalIgnoreCase);
        _caminhos = new HashSet<string>(regras.Where(EhCaminho), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Regras { get; }

    public static RegrasExclusao De(IEnumerable<string> textos)
    {
        var regras = new List<string>();
        foreach (var texto in textos)
        {
            if (Validar(texto, out _) is { } regra
                && !regras.Contains(regra, StringComparer.OrdinalIgnoreCase)
                && regras.Count < Maximo)
            {
                regras.Add(regra);
            }
        }

        return new RegrasExclusao(regras);
    }

    /// <summary>A regra que exclui esta pasta, ou null.</summary>
    public string? Motivo(string caminhoDaPasta, string nome)
    {
        if (_nomes.Count > 0 && _nomes.TryGetValue(nome, out var porNome))
        {
            return porNome;
        }

        return _caminhos.Count > 0 && _caminhos.TryGetValue(caminhoDaPasta.TrimEnd('\\'), out var porCaminho) ? porCaminho : null;
    }

    /// <summary>A regra limpa, pronta para guardar, ou null com o motivo.</summary>
    public static string? Validar(string texto, out string? erro)
    {
        erro = null;
        var t = texto.Trim().Trim('"').Trim();
        if (t.Length == 0)
        {
            erro = "Escreva o nome da pasta ou o caminho completo.";
            return null;
        }

        if (t.IndexOfAny(['*', '?']) >= 0)
        {
            erro = "Use o nome da pasta ou o caminho completo, sem * nem ?";
            return null;
        }

        if (!t.Contains('\\') && !t.Contains('/') && !t.Contains(':'))
        {
            if (t.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                erro = "O nome tem um caractere que o Windows não aceita em pasta.";
                return null;
            }

            return t;
        }

        var caminho = Alvo.Normalizar(t, out _);
        if (caminho is null || !(caminho.Length >= 3 && caminho[1] == ':' || caminho.StartsWith(@"\\", StringComparison.Ordinal)))
        {
            erro = "Use só o nome da pasta, como node_modules, ou o caminho completo, como D:\\Backup.";
            return null;
        }

        if (string.Equals(caminho.TrimEnd('\\'), Volumes.RaizDe(caminho).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            erro = "A raiz da unidade não pode ser excluída. Escolha outra unidade para varrer.";
            return null;
        }

        return caminho.TrimEnd('\\');
    }

    private static bool EhCaminho(string regra) => regra.Contains('\\');
}

public interface IArmazemExclusoes
{
    RegrasExclusao Ler();

    void Gravar(RegrasExclusao regras);
}

/// <summary>Uma regra por linha, só nesta máquina. Falha de leitura ou de gravação deixa a lista vazia.</summary>
public sealed class ArquivoExclusoes(string arquivo) : IArmazemExclusoes
{
    public static ArquivoExclusoes Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "excluidas.txt"));

    public RegrasExclusao Ler()
    {
        try
        {
            return File.Exists(arquivo) ? RegrasExclusao.De(File.ReadAllLines(arquivo)) : RegrasExclusao.Nenhuma;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return RegrasExclusao.Nenhuma;
        }
    }

    public void Gravar(RegrasExclusao regras)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, regras.Regras);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class ExclusoesEmMemoria : IArmazemExclusoes
{
    private RegrasExclusao _atual = RegrasExclusao.Nenhuma;

    public RegrasExclusao Ler() => _atual;

    public void Gravar(RegrasExclusao regras) => _atual = regras;
}
```

Conferir em `alvos/alvo.cs` e `unidades/volumes.cs` o comportamento de `Alvo.Normalizar` (se devolve o caminho com barra final na raiz) e de `Volumes.RaizDe` em caminho de rede. O teste do caminho `\\servidor\pasta` entra se `Volumes.RaizDe` devolver o compartilhamento: a regra igual ao próprio compartilhamento também é recusada como raiz.

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes.

- [ ] **Passo 5: commit**

```bash
sh .superpowers/rascunho/commit.sh "Cria as regras de pastas excluidas da varredura" "Nome vale em qualquer nivel e caminho completo vale so para aquela pasta, sem curinga, para o tecnico saber o que ficou fora da conta. A lista fica num arquivo do perfil, uma regra por linha." src/mapdisk.nucleo/varredura/regras-exclusao.cs testes/mapdisk.testes/regras-exclusao-testes.cs
```

---

### Tarefa 2: estado "excluída" na árvore e no motor

**Arquivos:**
- Modificar: `src/mapdisk.nucleo/arvore/no-pasta.cs`, `src/mapdisk.nucleo/varredura/motor-varredura.cs`
- Teste: `testes/mapdisk.testes/regras-exclusao-testes.cs`, que cresce

**Interfaces:**
- Produz:
  - `EstadoPasta.Excluida`;
  - `NoPasta.MarcarExcluida(string regra)`, que grava a regra em `Motivo`;
  - `NoPasta.PastasExcluidas`;
  - `IMotorVarredura.Exclusoes`, com valor padrão `RegrasExclusao.Nenhuma` e que o `MotorVarredura` guarda.

- [ ] **Passo 1: escrever os testes, com a `PastaTeste` (`testes/mapdisk.testes/apoio/pasta-teste.cs`)**

```csharp
    [Fact]
    public async Task Varredura_nao_le_a_pasta_excluida_e_conta_ela()
    {
        using var p = new PastaTeste();
        p.Arquivo(@"site\index.html", 1000);
        p.Arquivo(@"site\node_modules\pacote\a.js", 50_000);
        p.Arquivo(@"backup\velho.zip", 70_000);
        var motor = new MotorVarredura { Exclusoes = RegrasExclusao.De(["node_modules", p.Caminho("backup")]) };

        var raiz = (await motor.Iniciar(p.Raiz, CancellationToken.None).Conclusao).Raiz;

        var site = raiz.Subpastas.Single(s => s.Nome == "site");
        var modulos = site.Subpastas.Single();
        Assert.Equal(EstadoPasta.Excluida, modulos.Estado);
        Assert.Equal("node_modules", modulos.Motivo);
        Assert.Empty(modulos.Subpastas);
        Assert.Equal(1000, site.Tamanho);
        Assert.Equal(EstadoPasta.Excluida, raiz.Subpastas.Single(s => s.Nome == "backup").Estado);
        Assert.Equal(2, raiz.PastasExcluidas);
        Assert.Equal(0, raiz.PastasSemAcesso + raiz.PastasComErro);
    }

    [Fact]
    public async Task Alvo_nunca_e_excluido_e_atualizar_le_a_pasta_excluida()
    {
        using var p = new PastaTeste();
        p.Arquivo(@"node_modules\a.js", 5000);
        var motor = new MotorVarredura { Exclusoes = RegrasExclusao.De(["node_modules"]) };

        var direto = (await motor.Iniciar(p.Caminho("node_modules"), CancellationToken.None).Conclusao).Raiz;
        Assert.Equal(EstadoPasta.Lida, direto.Estado);
        Assert.Equal(5000, direto.Tamanho);

        var raiz = (await motor.Iniciar(p.Raiz, CancellationToken.None).Conclusao).Raiz;
        var excluida = raiz.Subpastas.Single();
        var relida = (await motor.Reler(excluida, CancellationToken.None).Conclusao).Raiz;
        Assert.Equal(EstadoPasta.Lida, relida.Estado);
        Assert.Equal(5000, raiz.Tamanho);
        Assert.Equal(0, raiz.PastasExcluidas);
    }
```

Conferir em `pasta-teste.cs` se `Arquivo` cria as pastas do caminho; se não criar, chamar `p.Pasta(...)` antes.

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RegrasExclusaoTestes`
Esperado: não compila (`Exclusoes`, `Excluida` e `PastasExcluidas` não existem).

- [ ] **Passo 3: árvore**

Em `no-pasta.cs`:
- `EstadoPasta` ganha, depois de `Link`:

```csharp
    /// <summary>Pasta que bate com uma regra das Opções. Não é lida e não soma; aparece como "excluída".</summary>
    Excluida,
```

- Campo `private long _excluidas;` e propriedade:

```csharp
    /// <summary>Pastas excluídas da varredura nesta subárvore, contando esta.</summary>
    public long PastasExcluidas => Interlocked.Read(ref _excluidas);
```

- `Somar` ganha o parâmetro `long excluidas`, depois de `comErro`, e soma em `no._excluidas`. Todas as chamadas atuais passam `0` nesse lugar.
- `DescontarAcima` lê `var excluidas = PastasExcluidas;` e desconta `-excluidas` igual aos outros contadores.
- Método novo:

```csharp
    public void MarcarExcluida(string regra)
    {
        Motivo = regra;
        _estado = EstadoPasta.Excluida;
        Somar(0, 0, 0, 0, 0, 0, 1, ModificacaoPropria.Ticks);
    }
```

- [ ] **Passo 4: motor**

Em `motor-varredura.cs`:
- Na interface `IMotorVarredura`:

```csharp
    /// <summary>Pastas que a próxima varredura não lê. O padrão não exclui nada.</summary>
    RegrasExclusao Exclusoes
    {
        get => RegrasExclusao.Nenhuma;
        set { }
    }
```

- `MotorVarredura` ganha `public RegrasExclusao Exclusoes { get; set; } = RegrasExclusao.Nenhuma;`.
- `Comecar` lê `var regras = Exclusoes;` e passa adiante até `LerPasta`. As regras valem do começo ao fim de uma varredura, mesmo que as Opções mudem no meio.
- Em `LerPasta`, ao criar cada subpasta:

```csharp
            if (entrada.EhLink)
            {
                sub.MarcarLink(DestinoDoLink(Alvo.Juntar(caminho, entrada.Nome)));
            }
            else if (regras.Motivo(Alvo.Juntar(caminho, entrada.Nome), entrada.Nome) is { } regra)
            {
                sub.MarcarExcluida(regra);
            }
```

- `MarcarExcluida` é chamado antes do `no.Preencher(...)`. A soma do contador sobe pela árvore como a do link, e a pasta nunca entra na fila, porque a fila só recebe `Pendente`.

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes, inclusive os testes de varredura que já existem.

- [ ] **Passo 6: commit**

```bash
sh .superpowers/rascunho/commit.sh "Marca as pastas excluidas na varredura" "A pasta que bate com uma regra nao e lida, nao soma e entra num contador proprio, como sem acesso e erro. O alvo nunca e excluido, entao Atualizar esta pasta le a pasta excluida." src/mapdisk.nucleo/arvore/no-pasta.cs src/mapdisk.nucleo/varredura/motor-varredura.cs testes/mapdisk.testes/regras-exclusao-testes.cs
```

---

### Tarefa 3: painel, árvore, análises, gráfico e ações

**Arquivos:**
- Modificar:
  - `src/mapdisk.nucleo/painel/dependencias-painel.cs`, `painel-principal.cs`, `linha-arvore.cs`, `painel-analises.cs`, `painel-grafico.cs`;
  - `src/mapdisk.nucleo/grafico/itens-grafico.cs`;
  - `src/mapdisk.nucleo/acoes/preparador-acoes.cs`;
  - `src/mapdisk.nucleo/painel/demonstracao.cs`.
- Teste: `testes/mapdisk.testes/regras-exclusao-testes.cs`

**Interfaces:**
- Produz:
  - `DependenciasPainel.Exclusoes` (`IArmazemExclusoes`, padrão em memória; o `Padrao()` liga o arquivo);
  - em `PainelPrincipal`: `Exclusoes` (as regras atuais) e `GravarExclusoes(RegrasExclusao)`;
  - `ConteudoGrafico.Excluidas` (lista de nomes) e `PainelGrafico.TextoExcluidas`.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Painel_passa_as_regras_ao_motor_e_grava()
    {
        var motor = new MotorVarredura();
        var armazem = new ExclusoesEmMemoria();
        armazem.Gravar(RegrasExclusao.De(["node_modules"]));
        var painel = new PainelPrincipal(new DependenciasPainel { Motor = motor, ListarUnidades = () => [], Exclusoes = armazem });
        Assert.Equal(["node_modules"], motor.Exclusoes.Regras);

        painel.GravarExclusoes(RegrasExclusao.De([".git"]));
        Assert.Equal([".git"], armazem.Ler().Regras);
        Assert.Equal([".git"], motor.Exclusoes.Regras);
    }

    [Fact]
    public void Linha_grafico_analises_e_acoes_tratam_a_pasta_excluida()
    {
        var raiz = new NoPasta(@"D:\", null);
        var dados = new NoPasta("Dados", raiz);
        var modulos = new NoPasta("node_modules", raiz);
        raiz.Preencher([], [dados, modulos]);
        dados.Preencher([new ArquivoInfo("a.txt", 1000, 4096, new DateTime(2026, 1, 1), MarcaArquivo.Nenhuma)], []);
        modulos.MarcarExcluida("node_modules");

        var linha = LinhaArvore.DaPasta(modulos);
        Assert.True(linha.SemValor);
        Assert.Equal("excluída da varredura (regra: node_modules)", linha.Rotulo);

        var grafico = ItensGrafico.DaPasta(raiz, ModoExibicao.Tamanho, 20);
        Assert.Equal(["node_modules"], grafico.Excluidas);
        Assert.DoesNotContain("node_modules", grafico.NaoLidas);
    }
```

O nome do método que cria uma `LinhaArvore` a partir de uma pasta (`DaPasta` acima) deve ser conferido em `linha-arvore.cs`, e o teste usa o que existir. O teste do bloqueio da ação usa o mesmo padrão dos testes de ações em `acoes-testes.cs`, com a mensagem abaixo.

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RegrasExclusaoTestes`
Esperado: não compila.

- [ ] **Passo 3: implementar**

- `dependencias-painel.cs`: `public IArmazemExclusoes Exclusoes { get; init; } = new ExclusoesEmMemoria();`. No `Padrao()` do painel, `Exclusoes = ArquivoExclusoes.Padrao(),`.
- `painel-principal.cs`:
  - no construtor, `_exclusoes = dependencias.Exclusoes; _motor.Exclusoes = _exclusoes.Ler();`;
  - `public RegrasExclusao Exclusoes => _motor.Exclusoes;`;
  - `public void GravarExclusoes(RegrasExclusao regras) { _exclusoes.Gravar(regras); _motor.Exclusoes = regras; }`;
  - em `AtualizarTextos`, a lista `partes` ganha `Formatador.Plural(raiz.PastasExcluidas, "pasta excluída", "pastas excluídas")` quando maior que zero.
- `linha-arvore.cs`:
  - `SemValor` inclui `EstadoPasta.Excluida`;
  - `Rotulo` ganha `EstadoPasta.Excluida => $"excluída da varredura (regra: {Pasta.Motivo})"`;
  - o texto das colunas ganha `EstadoPasta.Excluida => "excluída"`.
- `painel-analises.cs`: depois do aviso de sem leitura, se `pasta.PastasExcluidas > 0`, acrescentar a frase `"{N pastas excluídas da varredura} não entram nesta conta"`. Junta com a de sem leitura por `". "` quando houver as duas.
- `itens-grafico.cs`: `ConteudoGrafico` ganha `IReadOnlyList<string> Excluidas`. No laço, `EstadoPasta.Excluida` vai para `excluidas` e não para `naoLidas`.
- `painel-grafico.cs`: `TextoExcluidas = c.Excluidas.Count == 0 ? "" : $"Excluídas da varredura, fora do gráfico: {string.Join(", ", c.Excluidas)}"`. A janela mostra o texto abaixo do `TextoNaoLidas`, com o mesmo estilo.
- `preparador-acoes.cs`: antes do bloqueio genérico de pasta sem leitura:

```csharp
            if (item.EhPasta && item.Pasta.Estado == EstadoPasta.Excluida)
            {
                return Bloqueado($"{item.Nome}: pasta excluída da varredura. Use Atualizar esta pasta para ler antes de agir.", unicos);
            }
```

- `demonstracao.cs`: a árvore de exemplo ganha uma pasta `node_modules` em `Dados\Projetos`, já marcada como excluída, para a tela de demonstração mostrar o estado e servir às imagens.

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes. Os testes de demonstração que contam pastas podem precisar do número novo. Ajustar só a contagem, nunca a regra.

- [ ] **Passo 5: commit**

```bash
sh .superpowers/rascunho/commit.sh "Mostra as pastas excluidas na tela" "A arvore mostra excluida com a regra, a barra de status conta, as analises e o grafico avisam que ficaram fora, e as acoes bloqueiam com o caminho para ler a pasta. A demonstracao ganha uma pasta excluida." src/mapdisk.nucleo testes/mapdisk.testes
```

---

### Tarefa 4: relatórios, CSV e linha de comando

**Arquivos:**
- Modificar:
  - `src/mapdisk.nucleo/relatorios/exportador-csv.cs`, `relatorio-tecnico.cs`, `pagina-avaliacao.cs`;
  - `src/mapdisk.nucleo/linha-de-comando/argumentos.cs`, `executor-cli.cs`.
- Teste: `regras-exclusao-testes.cs`, `argumentos-testes.cs`, `executor-cli-testes.cs`

**Interfaces:**
- Produz: `ArgumentosCli.Excluir` (`IReadOnlyList<string>`, já validadas).

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Csv_e_relatorios_citam_a_pasta_excluida()
    {
        var raiz = new NoPasta(@"D:\", null);
        var modulos = new NoPasta("node_modules", raiz);
        raiz.Preencher([], [modulos]);
        modulos.MarcarExcluida("node_modules");

        var csv = new StringWriter();
        ExportadorCsv.Gravar(raiz, csv);
        Assert.Contains(@"D:\node_modules;excluída;", csv.ToString());

        var html = System.Net.WebUtility.HtmlDecode(RelatorioTecnico.Gerar(new DadosRelatorio(raiz, DateTime.Now, "PC", null, false, 10, null)));
        Assert.Contains("1 pasta excluída da varredura não entra nesta conta", html);
        Assert.Contains("excluída da varredura (regra: node_modules)", html);
    }
```

Em `argumentos-testes.cs`:

```csharp
    [Fact]
    public void Le_excluir_repetido_e_recusa_regra_invalida()
    {
        var a = ArgumentosCli.Interpretar(["varrer", "D:", "--excluir", "node_modules", "--excluir", @"D:\Backup"]);
        Assert.Equal(["node_modules", @"D:\Backup"], a.Excluir);
        Assert.Contains("--excluir *.tmp: Use o nome da pasta ou o caminho completo, sem * nem ?", ArgumentosCli.Interpretar(["varrer", "D:", "--excluir", "*.tmp"]).Erros);
    }
```

Em `executor-cli-testes.cs`, varrer uma `PastaTeste` com `--excluir node_modules` e conferir a linha `Atenção: 1 pasta excluída da varredura, fora do total.` e o item `excluída` na lista dos maiores.

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "RegrasExclusaoTestes|ArgumentosTestes|ExecutorCliTestes"`
Esperado: FALHA.

- [ ] **Passo 3: implementar**

- `exportador-csv.cs`: `Estado` ganha `EstadoPasta.Excluida => "excluída"`. A pasta excluída sai sem números, como as outras sem leitura.
- `relatorio-tecnico.cs`:
  - no `Resumo`, depois do aviso de sem leitura, se `p.PastasExcluidas > 0`, um segundo `<p class="aviso">` com `"{N pasta excluída da varredura não entra nesta conta}"`;
  - em `MaioresPastas`, as excluídas entram nas linhas sem número, com `excluída da varredura (regra: X)`.
- `pagina-avaliacao.cs`: no `Resumo`, a frase de "não puderam ser lidas" ganha a irmã "N pastas foram excluídas da varredura pelo técnico e não entram nesta conta". O texto passa pela `humanizar-ptbr`. Fica perto do texto já revisado pela `legal-br` e não muda o sentido dele.
- `argumentos.cs`:
  - `case "--excluir":` valida com `RegrasExclusao.Validar`. Se for válida, acrescenta em `_excluir`; se não for, grava o erro `"--excluir {texto}: {motivo}"`;
  - `Excluir` sai como lista. A opção também pede o comando `varrer`, como `--csv`;
  - a ajuda ganha a linha `--excluir <nome ou caminho>   não lê esta pasta (pode repetir)`.
- `executor-cli.cs`:
  - antes de `Iniciar`, `motor.Exclusoes = RegrasExclusao.De(argumentos.Excluir);`;
  - depois dos avisos de sem acesso, a linha `Atenção: {N pasta excluída da varredura}, fora do total.`;
  - em `MaioresItens`, `EstadoPasta.Excluida => "excluída"`.

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes.

- [ ] **Passo 5: commit**

```bash
sh .superpowers/rascunho/commit.sh "Leva as pastas excluidas aos relatorios e a linha de comando" "CSV, relatorio do tecnico e pagina do relatorio para o cliente dizem o que ficou fora da conta. A linha de comando ganha --excluir, que pode repetir e nao depende da lista das Opcoes." src/mapdisk.nucleo testes/mapdisk.testes
```

---

### Tarefa 5: seção na tela de Opções

**Arquivos:**
- Modificar: `src/mapdisk/janela-opcoes.xaml` e `.cs`, `src/mapdisk/janela-principal.xaml` (texto das excluídas no gráfico)
- Teste: `testes/mapdisk.testes/recursos-testes.cs`

- [ ] **Passo 1: escrever o teste**

```csharp
    [Fact]
    public void Opcoes_tem_as_pastas_excluidas()
    {
        var opcoes = File.ReadAllText(App("janela-opcoes.xaml"));
        foreach (var nome in new[] { "ListaExcluidas", "CampoExcluir", "TextoErroExcluir" })
        {
            Assert.Contains($"x:Name=\"{nome}\"", opcoes);
        }

        Assert.Contains("Click=\"AoEscolherExcluida\"", opcoes);
        Assert.Contains("{Binding TextoExcluidas}", File.ReadAllText(App("janela-principal.xaml")));
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RecursosTestes`
Esperado: FALHA.

- [ ] **Passo 3: implementar**

- **Cartão novo em `janela-opcoes.xaml`,** "Pastas excluídas da varredura", entre "Análises" e "Relatório para o cliente". Ele tem:
  - o texto fixo: "A varredura não lê estas pastas. Nome, como node_modules, vale em qualquer nível. Caminho completo, como D:\Backup\Veeam, vale só para aquela pasta. Pasta excluída aparece como "excluída", fora dos totais. Vale na próxima varredura.";
  - `ListBox x:Name="ListaExcluidas"`, com 90 de altura;
  - linha com `TextBox x:Name="CampoExcluir"` e os botões "Acrescentar" (`AoAcrescentarExcluida`), "Escolher pasta..." (`AoEscolherExcluida`) e "Tirar" (`AoTirarExcluida`);
  - `TextBlock x:Name="TextoErroExcluir"`, em vermelho suave, para o motivo da recusa.
- **`janela-opcoes.xaml.cs`:**
  - `MostrarExcluidas()` preenche a lista com `_painel.Exclusoes.Regras`;
  - `AoAcrescentarExcluida` valida com `RegrasExclusao.Validar`. Se for inválida, mostra o motivo em `TextoErroExcluir`. Se for válida, chama `_painel.GravarExclusoes(RegrasExclusao.De(_painel.Exclusoes.Regras.Append(regra)))`, limpa o campo e atualiza a lista. Se já tiver 100 regras, avisa "Limite de 100 pastas excluídas.";
  - `AoEscolherExcluida` abre `Microsoft.Win32.OpenFolderDialog` e acrescenta o caminho escolhido, pelo mesmo caminho;
  - `AoTirarExcluida` grava a lista sem a regra selecionada;
  - as regras gravam na hora, como "Esquecer" dos últimos alvos, sem depender do "Salvar".
- **`janela-principal.xaml`:** abaixo do `TextoNaoLidas` do gráfico, um `TextBlock` com `{Binding TextoExcluidas}` e o mesmo estilo.
- Todos os textos novos passam pela `humanizar-ptbr`.

- [ ] **Passo 4: rodar build e testes**

Rodar: `dotnet build mapdisk.sln -c Release` e `dotnet test mapdisk.sln -c Release`
Esperado: sem aviso e todos verdes.

- [ ] **Passo 5: conferir na tela, em demonstração**

- Gerar o `.exe` com `ferramentas\publicar.cmd` e abrir com `--demonstracao`. A pasta `node_modules` da demonstração aparece como "excluída", e a barra de baixo mostra "1 pasta excluída".
- Em Opções, acrescentar `*.tmp`: aparece o motivo da recusa. Acrescentar `.git` e tirar em seguida. A demonstração grava em memória.
- Em pasta de teste dentro de `.superpowers/rascunho/`, rodar `mapdisk varrer <pasta> --excluir node_modules --relatorio <arquivo>` e conferir o relatório. Apagar a pasta de teste no fim.

- [ ] **Passo 6: commit**

```bash
sh .superpowers/rascunho/commit.sh "Liga as pastas excluidas na tela de Opcoes" "O tecnico acrescenta por nome, por caminho ou escolhendo a pasta, e tira da lista. A regra invalida diz o motivo, e a lista vale na proxima varredura." src/mapdisk testes/mapdisk.testes/recursos-testes.cs
```

---

### Tarefa 6: documentação

- [ ] **Passo 1:** README, seção Uso:
  - em **Opções**, o item "Pastas excluídas da varredura", com o arquivo `excluidas.txt`;
  - na linha de comando, `--excluir`;
  - a linha da fatia 9 com "código em [#N] | Aguardando o teste do Manfred".
- [ ] **Passo 2:** pendências:
  - fechar "Pastas excluídas da varredura" com "Fechado: entrou na fatia 9, PR #N";
  - nova seção "Fatia 9", com o teste do Manfred num disco real com pastas excluídas por nome e por caminho.
- [ ] **Passo 3:** portões do `AGENTS.md` e commit:

```bash
sh .superpowers/rascunho/commit.sh "Traz a documentacao da fatia 9" "README com as pastas excluidas nas Opcoes e o --excluir. Pendencia da versao 1 fechada." README.md docs/superpowers/pendencias.md
```

- [ ] **Passo 4:** abrir o PR "Fatia 9: pastas excluídas da varredura", com "O que muda", "Como testar" (Opções, varredura de uma pasta com `node_modules` ou `.git`, "Atualizar esta pasta" na excluída, relatório e CSV) e a linha de autores.

---

## Conferência do plano contra o desenho

| Requisito | Tarefa |
|---|---|
| Seção 6, Opções: pastas excluídas da varredura | 1, 2 e 5 |
| Regra 3: nunca 0 onde não houve leitura | 2, 3 e 4 (estado próprio, contagem e aviso em toda tela e relatório) |
| Regra 2: linha de comando só lê | 4 (`--excluir` só reduz o que se lê) |
| Pendência adiada da versão 1 | 6 |
