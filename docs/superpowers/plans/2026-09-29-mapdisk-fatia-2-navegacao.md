# Plano da fatia 2: navegação, elevação e acertos da fatia 1

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada passo tem caixa de seleção (`- [ ]`). Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** o técnico navega pela árvore (abrir uma pasta como raiz, voltar, avançar, subir, abrir até N níveis), reusa os últimos alvos, atualiza só uma pasta, usa o menu de contexto e varre como administrador lendo todas as pastas locais. Junto vêm os acertos que o teste da fatia 1 pediu: conferência do fim da varredura, memória abaixo da meta e rótulo repetido.

**Arquitetura:** a regra fica no núcleo, testada: `NoPasta` ganha desconto e troca de subpasta para reler uma ramificação; o motor ganha `Reler`; a `ArvoreVisivel` ganha histórico de raízes e abertura por níveis; o painel ganha últimos alvos, atualizar pasta e elevação por dependências injetadas; `Privilegios` e `Elevacao` cuidam do Windows. O aplicativo só liga botões, menu e teclas.

**Tecnologia:** C# com .NET 8, WPF, xUnit, P/Invoke por `LibraryImport` para `OpenProcessToken`, `LookupPrivilegeValueW`, `AdjustTokenPrivileges` e `SHObjectProperties`.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, fatia 2 da seção 11, e a decisão "Leitura como administrador" da seção 3. Pendências da fatia 1 em `docs/superpowers/pendencias.md`.

## Restrições globais

- Regras do produto 1 a 9 do `AGENTS.md`. Nesta fatia pesam a 3 (nunca 0 onde não houve leitura), a 4 (elevação só pelo botão), a 6 (nada sai da máquina) e a 7 (não pesar no servidor).
- Esta fatia continua só lendo. O privilégio de backup é ligado só para leitura: `CreateFile` com `FILE_LIST_DIRECTORY` e `FILE_FLAG_BACKUP_SEMANTICS`, sem nenhum acesso de escrita.
- Testes só dentro da pasta de saída dos testes. Nenhum teste lê, move ou apaga fora dela.
- Resultado de teste manual no computador do Manfred fica na conversa, nunca em arquivo, commit ou PR.
- Tudo em português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso.
- Commit pela `.superpowers/rascunho/commit.sh`, com verbo na 3ª pessoa, título sem acento e `Autores: Manfred Heil Junior`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Leitura como administrador | Privilégio de backup (`SeBackupPrivilege`) ligado no processo elevado | Decisão do Manfred em 29/09/2026 |
| Como pedir a elevação | O botão reabre o próprio `.exe` com `--elevado "<alvo>"` e o verbo `runas`. A janela elevada já começa varrendo | Regra 4. Recusa no UAC não é erro: a janela avisa e segue sem administrador |
| Processo já elevado por fora | Se o técnico abrir o `.exe` como administrador, o privilégio de backup é ligado do mesmo jeito, na janela e na linha de comando | Mesmo resultado de leitura, sem depender do botão |
| Últimos alvos | Até 10, do mais recente para o mais antigo, em `%LOCALAPPDATA%\MapDisk\alvos.txt` | Fica só na máquina (regra 6). Caminho de cliente nunca vai para o repositório |
| Abrir uma pasta como raiz | Reusa a árvore já lida, sem varrer de novo. Voltar e avançar percorrem as raízes abertas | Instantâneo e sem pesar no disco |
| Atualizar esta pasta | Tira da árvore os totais da pasta, lê só ela de novo e soma o novo resultado acima | R2. Link não é relido |
| Conferência do fim da varredura | Ao terminar sem interrupção, o motor conta as pastas que ficaram sem leitura. Se houver alguma, a janela e a linha de comando avisam | Pendência da fatia 1: uma varredura veio com menos arquivos e não se repetiu |
| Memória | Buffer de leitura e listas reaproveitados por tarefa, conjunto de identificadores em faixas com trava, coletor de lixo em modo de economia | Meta da spec: menos de 300 MB para 1 milhão de arquivos |
| Rótulo repetido | Pasta sem acesso e pasta não lida ficam sem rótulo: a coluna do valor já diz isso | Pendência da fatia 1 |

## Git desta fatia

1. O plano entra pelo ramo `fatia-2-navegacao`, num Pull Request só do plano, junto com a correção do README e a decisão na spec.
2. O código sai do `main` atualizado, no ramo `navegacao`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | O que muda |
|---|---|
| `src/mapdisk.nucleo/arvore/no-pasta.cs` | `ContarNaoLidas`, `DescontarAcima`, `TrocarSubpasta` |
| `src/mapdisk.nucleo/varredura/motor-varredura.cs` | `PastasNaoLidas` no resultado, `Reler` no motor e na interface, listas por tarefa, conjunto de identificadores em faixas |
| `src/mapdisk.nucleo/varredura/conjunto-ids.cs` | Novo: conjunto de identificadores em faixas com trava |
| `src/mapdisk.nucleo/varredura/leitor-pasta.cs` | Buffer reaproveitado por tarefa |
| `src/mapdisk.nucleo/painel/linha-arvore.cs` | `Nivel` ajustável, nome da raiz com caminho completo, `Caminho`, sem rótulo repetido |
| `src/mapdisk.nucleo/painel/arvore-visivel.cs` | Níveis relativos à raiz, `AbrirAqui`, `Voltar`, `Avancar`, `Subir`, `AbrirNiveis`, `Substituir` |
| `src/mapdisk.nucleo/painel/ultimos-alvos.cs` | Novo: `ItemAlvo`, `IHistoricoAlvos`, `HistoricoAlvosArquivo`, `HistoricoEmMemoria`, `UltimosAlvos` |
| `src/mapdisk.nucleo/painel/dependencias-painel.cs` | Novo: dependências do painel |
| `src/mapdisk.nucleo/painel/painel-principal.cs` | `Opcoes`, `AtualizarPasta`, navegação, elevação, aviso de pastas não lidas |
| `src/mapdisk.nucleo/sistema/privilegios.cs` | Novo: administrador e privilégio de backup |
| `src/mapdisk.nucleo/sistema/elevacao.cs` | Novo: argumentos e reabertura elevada |
| `src/mapdisk.nucleo/linha-de-comando/executor-cli.cs` | Aviso de pastas não lidas |
| `src/mapdisk/programa.cs`, `modo-linha-de-comando.cs` | Privilégio de backup e `--elevado` |
| `src/mapdisk/janela-principal.xaml` e `.xaml.cs` | Botões de navegação e de administrador, "Abrir até", menu de contexto, Shift+F5 |
| `src/mapdisk/mapdisk.csproj` | Coletor de lixo em modo de economia |
| `src/mapdisk/shell.cs` | Novo: Explorer, área de transferência e janela de propriedades |
| `ferramentas/medir-memoria.ps1` | Novo: pico de memória de uma varredura pela linha de comando |

---

### Tarefa 1: conferência do fim da varredura

**Arquivos:** `src/mapdisk.nucleo/arvore/no-pasta.cs`, `src/mapdisk.nucleo/varredura/motor-varredura.cs`, `src/mapdisk.nucleo/painel/painel-principal.cs`, `src/mapdisk.nucleo/linha-de-comando/executor-cli.cs`. Testes: `no-pasta-testes.cs`, `motor-varredura-testes.cs`, `painel-principal-testes.cs`, `executor-cli-testes.cs`.

**Interfaces:**
- Produz: `int NoPasta.ContarNaoLidas()`; `int ResultadoVarredura.PastasNaoLidas { get; init; }`, com padrão 0.

- [ ] **Passo 1: criar o ramo do código**

```bash
git switch main
git pull
git switch -c navegacao
```

- [ ] **Passo 2: testes que falham**

Em `no-pasta-testes.cs`:

```csharp
    [Fact]
    public void Conta_as_pastas_que_ficaram_sem_leitura()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", raiz);
        var c = new NoPasta("c", a);
        raiz.Preencher([], [a, b]);
        a.Preencher([], [c]);

        Assert.Equal(2, raiz.ContarNaoLidas());
        Assert.Equal(1, a.ContarNaoLidas());
    }
```

Em `motor-varredura-testes.cs`:

```csharp
    [Fact]
    public void Varredura_completa_nao_deixa_pasta_sem_leitura()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"a\b\c\x.bin", 1);

        Assert.Equal(0, Varrer(t.Raiz).PastasNaoLidas);
    }
```

Em `painel-principal-testes.cs`:

```csharp
    [Fact]
    public void Fim_com_pastas_nao_lidas_avisa()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        motor.Fim.SetResult(new ResultadoVarredura
        {
            Raiz = motor.Raiz!,
            Volume = null,
            Duracao = TimeSpan.FromSeconds(1),
            Cancelada = false,
            PastasNaoLidas = 3,
        });

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Contains("3 pastas não foram lidas", p.Erro);
        Assert.Contains("Atualizar", p.Erro);
    }
```

Em `executor-cli-testes.cs`, um motor que devolve pastas não lidas e o teste:

```csharp
    private sealed class MotorComPastasNaoLidas : IMotorVarredura
    {
        public Varredura Iniciar(string alvo, CancellationToken cancelar)
        {
            var raiz = new NoPasta(alvo, null);
            raiz.Preencher([], []);
            return new Varredura(raiz).Comecar(_ => Task.FromResult(new ResultadoVarredura
            {
                Raiz = raiz,
                Volume = null,
                Duracao = TimeSpan.FromSeconds(1),
                Cancelada = false,
                PastasNaoLidas = 2,
            }));
        }
    }

    [Fact]
    public void Fim_com_pastas_nao_lidas_avisa_na_linha_de_comando()
    {
        Rodar(new MotorComPastasNaoLidas(), out var saida, out _, "varrer", "D:");

        Assert.Contains("Atenção: 2 pastas não foram lidas", saida);
    }
```

Rodar: `dotnet test mapdisk.sln -c Release`. Esperado: erro de compilação (`ContarNaoLidas` e `PastasNaoLidas` não existem).

- [ ] **Passo 3: implementação**

Em `NoPasta`:

```csharp
    /// <summary>Pastas desta subárvore que ficaram sem leitura, contando esta.</summary>
    public int ContarNaoLidas()
    {
        var quantas = 0;
        var pilha = new Stack<NoPasta>();
        pilha.Push(this);
        while (pilha.Count > 0)
        {
            var no = pilha.Pop();
            if (no.Estado == EstadoPasta.Pendente)
            {
                quantas++;
                continue;
            }

            foreach (var sub in no.Subpastas)
            {
                pilha.Push(sub);
            }
        }

        return quantas;
    }
```

Em `ResultadoVarredura`:

```csharp
    /// <summary>Pastas que ficaram sem leitura numa varredura que não foi interrompida. Deve ser 0.</summary>
    public int PastasNaoLidas { get; init; }
```

No fim de `MotorVarredura.Executar`, trocar o `return` por:

```csharp
        var cancelada = Volatile.Read(ref pendentes) > 0;
        return new ResultadoVarredura
        {
            Raiz = raiz,
            Volume = volume,
            Duracao = relogio.Elapsed,
            Cancelada = cancelada,
            PastasNaoLidas = cancelada ? 0 : raiz.ContarNaoLidas(),
        };
```

Em `PainelPrincipal.Concluir`, depois do teste de raiz sem leitura:

```csharp
            else if (!r.Cancelada && r.PastasNaoLidas > 0)
            {
                Erro = $"A varredura terminou, mas {Formatador.Plural(r.PastasNaoLidas, "pasta não foi lida", "pastas não foram lidas")}. Clique em Atualizar para varrer de novo.";
            }
```

Em `ExecutorCli.Executar`, depois dos avisos de sem acesso e de erro:

```csharp
        if (!r.Cancelada && r.PastasNaoLidas > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(r.PastasNaoLidas, "pasta não foi lida", "pastas não foram lidas")}. Rode de novo para conferir.");
        }
```

- [ ] **Passo 4: rodar e ver passar.** Todos os testes verdes.

- [ ] **Passo 5: commit** `Confere no fim da varredura se alguma pasta ficou sem leitura`.

---

### Tarefa 2: memória abaixo da meta

**Arquivos:** novo `src/mapdisk.nucleo/varredura/conjunto-ids.cs`; `leitor-pasta.cs`; `motor-varredura.cs`; `src/mapdisk/mapdisk.csproj`; novo `ferramentas/medir-memoria.ps1`. Teste: novo `testes/mapdisk.testes/conjunto-ids-testes.cs`.

**Interfaces:**
- Produz: `internal sealed class ConjuntoIds` com `bool Acrescentar(long id)`.

- [ ] **Passo 1: medir antes.** Criar `ferramentas/medir-memoria.ps1` (sem acento):

```powershell
# Mede o pico de memoria de uma varredura pela linha de comando.
# Uso: powershell -File ferramentas\medir-memoria.ps1 C:
# O resultado fica so na tela. Nao grave em arquivo do repositorio.
param([Parameter(Mandatory = $true)][string]$Alvo)
$exe = Join-Path $PSScriptRoot '..\publicar\mapdisk.exe'
$saida = Join-Path $PSScriptRoot '..\.superpowers\rascunho\medir-memoria.txt'
$p = Start-Process -FilePath $exe -ArgumentList 'varrer', $Alvo -RedirectStandardOutput $saida -PassThru -WindowStyle Hidden
$pico = 0
while (-not $p.HasExited) {
    try { $p.Refresh(); if ($p.PeakWorkingSet64 -gt $pico) { $pico = $p.PeakWorkingSet64 } } catch { }
    Start-Sleep -Milliseconds 200
}
"Pico de memoria: {0:N0} MB" -f ($pico / 1MB)
```

Rodar `ferramentas\publicar.cmd` e `powershell -File ferramentas\medir-memoria.ps1 C:`. Anotar o número só na conversa.

- [ ] **Passo 2: teste que falha, `conjunto-ids-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class ConjuntoIdsTestes
{
    [Fact]
    public void Primeira_vez_aceita_e_repeticao_recusa()
    {
        var ids = new ConjuntoIds();

        Assert.True(ids.Acrescentar(42));
        Assert.False(ids.Acrescentar(42));
        Assert.True(ids.Acrescentar(-42));
    }

    [Fact]
    public void Muitas_tarefas_ao_mesmo_tempo_nao_perdem_nem_repetem()
    {
        var ids = new ConjuntoIds();
        var aceitos = 0;

        Parallel.For(0, 200_000, i =>
        {
            if (ids.Acrescentar(i % 100_000))
            {
                Interlocked.Increment(ref aceitos);
            }
        });

        Assert.Equal(100_000, aceitos);
    }
}
```

- [ ] **Passo 3: implementação**

`conjunto-ids.cs`:

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Identificadores de arquivo já contados, para o hard link somar uma vez. Dividido em faixas
/// com trava própria: ocupa menos memória que um dicionário concorrente e aguenta as tarefas
/// da varredura ao mesmo tempo.
/// </summary>
internal sealed class ConjuntoIds
{
    private const int Faixas = 64;

    private readonly HashSet<long>[] _faixas = Enumerable.Range(0, Faixas).Select(_ => new HashSet<long>()).ToArray();

    public bool Acrescentar(long id)
    {
        var faixa = _faixas[(int)((ulong)id % Faixas)];
        lock (faixa)
        {
            return faixa.Add(id);
        }
    }
}
```

Em `MotorVarredura.Executar`, trocar o dicionário:

```csharp
        var vistos = new ConjuntoIds();
        Func<long, bool> primeiraVez = Alvo.EhRede(caminho) ? _ => true : vistos.Acrescentar;
```

Em `LeitorPasta`, reaproveitar o buffer por tarefa, no lugar de `new byte[64 * 1024]` a cada pasta:

```csharp
    [ThreadStatic]
    private static byte[]? _buffer;
```

```csharp
        var buffer = _buffer ??= new byte[64 * 1024];
```

No trabalhador de `MotorVarredura.Executar`, criar as listas uma vez por tarefa e limpar a cada pasta: declarar `var arquivos = new List<ArquivoInfo>(); var entradas = new List<EntradaPasta>();` antes do `foreach` e passar para `LerPasta`, que faz `arquivos.Clear(); entradas.Clear();` no começo.

Em `src/mapdisk/mapdisk.csproj`, dentro do primeiro `PropertyGroup`:

```xml
    <!-- Coletor de lixo em modo de economia: menos memoria reservada numa varredura grande -->
    <ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>
```

e um `ItemGroup`:

```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="System.GC.ConserveMemory" Value="7" />
  </ItemGroup>
```

- [ ] **Passo 4: rodar e ver passar.** Todos os testes verdes.

- [ ] **Passo 5: medir depois.** Rodar de novo o `publicar.cmd` e o `medir-memoria.ps1` no mesmo alvo. Anotar só na conversa. Se ficar acima de 300 MB, a pendência continua com o número novo descrito sem dados do computador, e a medida seguinte (por exemplo, guardar o nome do arquivo de outro jeito) entra na fatia 3.

- [ ] **Passo 6: commit** `Reduz a memoria da varredura`.

---

### Tarefa 3: atualizar só uma pasta

**Arquivos:** `no-pasta.cs`, `motor-varredura.cs`, `arvore-visivel.cs`, `painel-principal.cs`. Testes: `no-pasta-testes.cs`, `motor-varredura-testes.cs`, `arvore-visivel-testes.cs`, `painel-principal-testes.cs`.

**Interfaces:**
- Produz: `internal void NoPasta.DescontarAcima()`, `internal void NoPasta.TrocarSubpasta(NoPasta antiga, NoPasta nova)`; em `IMotorVarredura`, `Varredura Reler(NoPasta pasta, CancellationToken cancelar)`, com implementação padrão que não relê nada; `void ArvoreVisivel.Substituir(NoPasta antiga, NoPasta nova)`; `bool PainelPrincipal.AtualizarPasta(NoPasta pasta)`.

- [ ] **Passo 1: testes que falham**

Em `no-pasta-testes.cs`:

```csharp
    [Fact]
    public void Descontar_e_trocar_deixam_os_totais_certos()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", raiz);
        raiz.Preencher([], [a, b]);
        var fechada = new NoPasta("fechada", a);
        a.Preencher([Arq("1.bin", 100, 4096)], [fechada]);
        fechada.MarcarSemAcesso("acesso negado");
        b.Preencher([Arq("2.bin", 10, 4096)], []);

        a.DescontarAcima();
        var nova = new NoPasta("a", raiz);
        raiz.TrocarSubpasta(a, nova);
        nova.Preencher([Arq("1.bin", 300, 4096)], []);

        Assert.Equal(310, raiz.Tamanho);
        Assert.Equal(2, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.PastasTotal);
        Assert.Equal(0, raiz.PastasSemAcesso);
        Assert.Same(nova, raiz.Subpastas[0]);
    }
```

Em `motor-varredura-testes.cs`:

```csharp
    [Fact]
    public async Task Reler_uma_pasta_soma_so_o_que_mudou()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"a\1.bin", 100);
        t.Arquivo(@"b\2.bin", 10);
        var motor = new MotorVarredura();
        var r = await motor.Iniciar(Alvo.Normalizar(t.Raiz, out _)!, CancellationToken.None).Conclusao;
        var a = r.Raiz.Subpastas.Single(s => s.Nome == "a");
        t.Arquivo(@"a\3.bin", 1000);

        var relida = await motor.Reler(a, CancellationToken.None).Conclusao;

        Assert.Equal(1110, r.Raiz.Tamanho);
        Assert.Equal(3, r.Raiz.ArquivosTotal);
        Assert.Equal(2, r.Raiz.PastasTotal);
        Assert.Same(relida.Raiz, r.Raiz.Subpastas.Single(s => s.Nome == "a"));
        Assert.Equal(EstadoPasta.Lida, relida.Raiz.Estado);
    }

    [Fact]
    public async Task Link_nao_e_relido()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"dados\x.bin", 500);
        t.Juncao("atalho", "dados");
        var motor = new MotorVarredura();
        var r = await motor.Iniciar(Alvo.Normalizar(t.Raiz, out _)!, CancellationToken.None).Conclusao;
        var atalho = r.Raiz.Subpastas.Single(s => s.Nome == "atalho");

        await motor.Reler(atalho, CancellationToken.None).Conclusao;

        Assert.Equal(500, r.Raiz.Tamanho);
        Assert.Same(atalho, r.Raiz.Subpastas.Single(s => s.Nome == "atalho"));
    }
```

Em `arvore-visivel-testes.cs`:

```csharp
    [Fact]
    public void Substituir_mantem_a_pasta_aberta()
    {
        var a = Carregada(out var raiz);
        var users = raiz.Subpastas.Single(s => s.Nome == "Users");
        a.Expandir(Linha(a, "Users"));

        users.DescontarAcima();
        var nova = new NoPasta("Users", raiz);
        raiz.TrocarSubpasta(users, nova);
        var carla = new NoPasta("Carla", nova);
        nova.Preencher([], [carla]);
        carla.Preencher([new("x.bin", 50, 50, _d, MarcaArquivo.Nenhuma)], []);
        a.Substituir(users, nova);

        Assert.Contains(a.Linhas, l => l.Nome == "Carla");
        Assert.DoesNotContain(a.Linhas, l => l.Nome == "Ana");
    }
```

Em `painel-principal-testes.cs`, o `MotorFalso` ganha o `Reler`:

```csharp
        public NoPasta? Relida { get; private set; }

        public Varredura Reler(NoPasta pasta, CancellationToken cancelar)
        {
            Relida = pasta;
            Token = cancelar;
            return new Varredura(pasta).Comecar(_ => Fim.Task);
        }
```

e o teste:

```csharp
    [Fact]
    public void Atualizar_pasta_rele_so_ela()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        var a = new NoPasta("a", motor.Raiz);
        motor.Raiz!.Preencher([], [a]);
        a.Preencher([], []);
        motor.Fim.SetResult(Resultado(motor.Raiz));
        p.Tique();

        Assert.True(p.AtualizarPasta(a));

        Assert.Same(a, motor.Relida);
        Assert.Equal(EstadoPainel.Varrendo, p.Estado);
    }
```

Esperado: erro de compilação.

- [ ] **Passo 2: implementação**

Em `NoPasta`:

```csharp
    /// <summary>
    /// Tira das pastas acima o que está abaixo desta, para ela ser lida de novo. A própria pasta
    /// continua contada uma vez no total de pastas da pasta-pai. A última modificação não volta.
    /// </summary>
    internal void DescontarAcima()
    {
        var tamanho = Tamanho;
        var alocado = Alocado;
        var arquivos = ArquivosTotal;
        var pastas = PastasTotal;
        var semAcesso = PastasSemAcesso;
        var comErro = PastasComErro;
        for (var no = Pai; no != null; no = no.Pai)
        {
            Interlocked.Add(ref no._tamanho, -tamanho);
            Interlocked.Add(ref no._alocado, -alocado);
            Interlocked.Add(ref no._arquivosTotal, -arquivos);
            Interlocked.Add(ref no._pastasTotal, -pastas);
            Interlocked.Add(ref no._semAcesso, -semAcesso);
            Interlocked.Add(ref no._comErro, -comErro);
        }
    }

    internal void TrocarSubpasta(NoPasta antiga, NoPasta nova)
    {
        var copia = (NoPasta[])Volatile.Read(ref _subpastas).Clone();
        copia[Array.IndexOf(copia, antiga)] = nova;
        Volatile.Write(ref _subpastas, copia);
    }
```

Em `IMotorVarredura`, a implementação padrão, que serve para a demonstração e para os motores falsos dos testes:

```csharp
    /// <summary>Lê de novo só esta pasta. O padrão não relê nada e devolve a mesma pasta.</summary>
    Varredura Reler(NoPasta pasta, CancellationToken cancelar) =>
        new Varredura(pasta).Comecar(_ => Task.FromResult(new ResultadoVarredura
        {
            Raiz = pasta,
            Volume = null,
            Duracao = TimeSpan.Zero,
            Cancelada = false,
        }));
```

Em `MotorVarredura`, separar o caminho da raiz e acrescentar `Reler`:

```csharp
    public Varredura Iniciar(string alvo, CancellationToken cancelar) => Comecar(new NoPasta(alvo, null), alvo, cancelar);

    public Varredura Reler(NoPasta pasta, CancellationToken cancelar)
    {
        if (pasta.Estado == EstadoPasta.Link)
        {
            return ((IMotorVarredura)new MotorDeNada()).Reler(pasta, cancelar);
        }

        if (pasta.Pai is null)
        {
            return Iniciar(pasta.Nome, cancelar);
        }

        var caminho = pasta.CaminhoCompleto();
        var nova = new NoPasta(pasta.Nome, pasta.Pai, pasta.ModificacaoPropria);
        pasta.DescontarAcima();
        pasta.Pai.TrocarSubpasta(pasta, nova);
        return Comecar(nova, caminho, cancelar);
    }

    private Varredura Comecar(NoPasta raiz, string caminho, CancellationToken cancelar)
    {
        var quantas = tarefas ?? (Alvo.EhRede(caminho) ? 4 : Math.Clamp(Environment.ProcessorCount, 4, 16));
        return new Varredura(raiz).Comecar(v => Task.Run(() => Executar(v, caminho, quantas, cancelar), CancellationToken.None));
    }

    private sealed class MotorDeNada : IMotorVarredura
    {
        public Varredura Iniciar(string alvo, CancellationToken cancelar) => throw new NotSupportedException();
    }
```

`Executar` passa a receber `string caminho` e usa esse caminho no lugar de `raiz.Nome` em `Volumes.Ler`, `Alvo.EhRede`, `Volumes.RaizDe` e no primeiro `fila.Add((raiz, caminho))`.

Em `ArvoreVisivel`:

```csharp
    /// <summary>Troca a pasta relida pela nova, mantendo aberta se estava aberta.</summary>
    public void Substituir(NoPasta antiga, NoPasta nova)
    {
        if (_pastasAbertas.Remove(antiga))
        {
            _pastasAbertas.Add(nova);
        }

        if (_gruposAbertos.Remove(antiga))
        {
            _gruposAbertos.Add(nova);
        }

        _linhasPasta.Remove(antiga);
        _linhasGrupo.Remove(antiga);
        if (Raiz == antiga)
        {
            Raiz = nova;
        }

        Atualizar();
    }
```

Em `PainelPrincipal`:

```csharp
    /// <summary>Lê de novo só esta pasta (Shift+F5). A raiz é varrida inteira. Link não é relido.</summary>
    public bool AtualizarPasta(NoPasta pasta)
    {
        if (!PodeVarrer || pasta.Estado == EstadoPasta.Link)
        {
            return false;
        }

        if (pasta.Pai is null)
        {
            Atualizar();
            return true;
        }

        Erro = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Reler(pasta, _cancelar.Token);
        Arvore.Substituir(pasta, _varredura.Raiz);
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
        return true;
    }
```

- [ ] **Passo 3: rodar e ver passar.** Todos os testes verdes.

- [ ] **Passo 4: commit** `Cria o atualizar so uma pasta`.

---

### Tarefa 4: navegação na árvore

**Arquivos:** `linha-arvore.cs`, `arvore-visivel.cs`, `painel-principal.cs`. Testes: `arvore-visivel-testes.cs`.

**Interfaces:**
- Produz: `LinhaArvore.Nivel { get; internal set; }`; `string LinhaArvore.Caminho`; em `ArvoreVisivel`: `PodeVoltar`, `PodeAvancar`, `PodeSubir`, `AbrirAqui(NoPasta)`, `Voltar()`, `Avancar()`, `Subir()`, `AbrirNiveis(int niveis)`; no painel, os mesmos comandos chamando a árvore e atualizando os textos.

- [ ] **Passo 1: testes que falham**

```csharp
    [Fact]
    public void Abrir_aqui_usa_a_pasta_como_raiz_sem_varrer_de_novo()
    {
        var a = Carregada(out var raiz);
        var users = raiz.Subpastas.Single(s => s.Nome == "Users");

        a.AbrirAqui(users);

        Assert.Equal([@"C:\Users", "Ana", "Bruno"], a.Linhas.Select(l => l.Nome));
        Assert.Equal([0, 1, 1], a.Linhas.Select(l => l.Nivel));
        Assert.Equal("100,0 %", a.Linhas[0].TextoPorcentagem);
        Assert.True(a.PodeVoltar);
        Assert.True(a.PodeSubir);
    }

    [Fact]
    public void Voltar_avancar_e_subir()
    {
        var a = Carregada(out var raiz);
        var users = raiz.Subpastas.Single(s => s.Nome == "Users");
        a.AbrirAqui(users);

        a.Voltar();
        Assert.Equal(@"C:\", a.Linhas[0].Nome);
        Assert.True(a.PodeAvancar);

        a.Avancar();
        Assert.Equal(@"C:\Users", a.Linhas[0].Nome);

        a.Subir();
        Assert.Equal(@"C:\", a.Linhas[0].Nome);
        Assert.False(a.PodeSubir);
    }

    [Fact]
    public void Abrir_niveis_abre_ate_a_profundidade_pedida()
    {
        var a = Carregada(out _);

        a.AbrirNiveis(2);
        Assert.Contains(a.Linhas, l => l.Nome == "Ana");

        a.AbrirNiveis(1);
        Assert.DoesNotContain(a.Linhas, l => l.Nome == "Ana");
    }

    [Fact]
    public void Caminho_da_linha()
    {
        var a = Carregada(out _);
        a.Expandir(Linha(a, "Users"));
        a.Expandir(Linha(a, "[2 arquivos]"));

        Assert.Equal(@"C:\Users\Ana", Linha(a, "Ana").Caminho);
        Assert.Equal(@"C:\pagefile.sys", Linha(a, "pagefile.sys").Caminho);
    }
```

Esperado: erro de compilação.

- [ ] **Passo 2: implementação**

Em `LinhaArvore`: `public int Nivel { get; internal set; }`; o construtor deixa de receber o nível; o nome da raiz mostra o caminho completo; e o caminho da linha:

```csharp
    public string Nome => Tipo switch
    {
        TipoLinha.Pasta => Nivel == 0 ? Pasta.CaminhoCompleto() : Pasta.Nome,
        TipoLinha.GrupoArquivos => $"[{Formatador.Plural(Pasta.Arquivos.Count, "arquivo", "arquivos")}]",
        _ => Arquivo.Nome,
    };

    /// <summary>Caminho completo, para abrir no Explorer e copiar.</summary>
    public string Caminho => Tipo == TipoLinha.Arquivo
        ? Alvo.Juntar(Pasta.CaminhoCompleto(), Arquivo.Nome)
        : Pasta.CaminhoCompleto();
```

Em `ArvoreVisivel`, o nível passa a ser dado em `Acrescentar(linha, nivel, valorDoPai, alvo)`, que faz `linha.Nivel = nivel;` antes de tudo, e os filhos são acrescentados com `nivel + 1`. Os métodos `LinhaDaPasta`, `LinhaDoGrupo` e `LinhaDoArquivo` deixam de receber o nível. A navegação:

```csharp
    private readonly Stack<NoPasta> _voltar = new();
    private readonly Stack<NoPasta> _avancar = new();

    public bool PodeVoltar => _voltar.Count > 0;

    public bool PodeAvancar => _avancar.Count > 0;

    public bool PodeSubir => Raiz?.Pai is not null;

    /// <summary>Mostra a pasta como raiz, com a árvore já lida. Não varre de novo.</summary>
    public void AbrirAqui(NoPasta pasta)
    {
        if (Raiz is null || pasta == Raiz || pasta.Estado != EstadoPasta.Lida)
        {
            return;
        }

        _voltar.Push(Raiz);
        _avancar.Clear();
        Focar(pasta);
    }

    public void Voltar()
    {
        if (Raiz is not null && _voltar.TryPop(out var anterior))
        {
            _avancar.Push(Raiz);
            Focar(anterior);
        }
    }

    public void Avancar()
    {
        if (Raiz is not null && _avancar.TryPop(out var proxima))
        {
            _voltar.Push(Raiz);
            Focar(proxima);
        }
    }

    public void Subir()
    {
        if (Raiz?.Pai is { } pai)
        {
            AbrirAqui(pai);
        }
    }

    /// <summary>Abre as pastas até o nível pedido abaixo da raiz. 1 deixa só a raiz aberta.</summary>
    public void AbrirNiveis(int niveis)
    {
        if (Raiz is null)
        {
            return;
        }

        _pastasAbertas.Clear();
        _gruposAbertos.Clear();
        var fila = new Queue<(NoPasta No, int Nivel)>();
        fila.Enqueue((Raiz, 0));
        while (fila.TryDequeue(out var item))
        {
            if (item.Nivel >= niveis)
            {
                continue;
            }

            _pastasAbertas.Add(item.No);
            foreach (var sub in item.No.Subpastas)
            {
                fila.Enqueue((sub, item.Nivel + 1));
            }
        }

        Atualizar();
    }

    private void Focar(NoPasta pasta)
    {
        Raiz = pasta;
        _pastasAbertas.Add(pasta);
        Atualizar();
    }
```

`Carregar` e `Limpar` limpam também `_voltar` e `_avancar`. `Substituir` troca a pasta antiga pela nova dentro das duas pilhas, recriando cada pilha com a troca feita.

No `PainelPrincipal`, os comandos da janela: `AbrirAqui(NoPasta)`, `Voltar()`, `Avancar()`, `Subir()` e `AbrirNiveis(int)`, cada um chamando o da árvore e depois `AtualizarTextos()`, e as propriedades `PodeVoltar`, `PodeAvancar` e `PodeSubir` lidas da árvore.

- [ ] **Passo 3: rodar e ver passar.** Todos os testes verdes, inclusive os da fatia 1 que usam `Nivel`.

- [ ] **Passo 4: commit** `Cria a navegacao na arvore`.

---

### Tarefa 5: últimos alvos e dependências do painel

**Arquivos:** novos `src/mapdisk.nucleo/painel/ultimos-alvos.cs` e `src/mapdisk.nucleo/painel/dependencias-painel.cs`; `painel-principal.cs`; `demonstracao.cs`. Testes: novo `ultimos-alvos-testes.cs`; `painel-principal-testes.cs`.

**Interfaces:**
- Produz: `sealed record ItemAlvo(string Caminho, string Descricao)`; `interface IHistoricoAlvos { IReadOnlyList<string> Ler(); void Gravar(IReadOnlyList<string> alvos); }`; `HistoricoAlvosArquivo(string arquivo)` com `static Padrao()`; `HistoricoEmMemoria`; `static class UltimosAlvos` com `Maximo = 10` e `Acrescentar(IReadOnlyList<string>, string)`; `sealed class DependenciasPainel` com `Motor`, `ListarUnidades`, `Historico`, `Administrador`, `Elevar`; construtor `PainelPrincipal(DependenciasPainel)`, mantendo o construtor `(IMotorVarredura, Func<IReadOnlyList<InfoVolume>>)`; `IReadOnlyList<ItemAlvo> PainelPrincipal.Opcoes`.

- [ ] **Passo 1: testes que falham, `ultimos-alvos-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class UltimosAlvosTestes
{
    [Fact]
    public void Mais_recente_primeiro_sem_repetir_e_com_limite()
    {
        IReadOnlyList<string> lista = [];
        for (var i = 0; i < 12; i++)
        {
            lista = UltimosAlvos.Acrescentar(lista, $@"D:\p{i}");
        }

        lista = UltimosAlvos.Acrescentar(lista, @"d:\P5");

        Assert.Equal(10, lista.Count);
        Assert.Equal(@"d:\P5", lista[0]);
        Assert.Equal(@"D:\p11", lista[1]);
        Assert.DoesNotContain(@"D:\p5", lista);
    }

    [Fact]
    public void Arquivo_grava_e_le_de_volta()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"alvos-{Guid.NewGuid():N}.txt");
        try
        {
            var historico = new HistoricoAlvosArquivo(arquivo);
            Assert.Empty(historico.Ler());

            historico.Gravar([@"C:\", @"\\srv\dados\"]);

            Assert.Equal([@"C:\", @"\\srv\dados\"], new HistoricoAlvosArquivo(arquivo).Ler());
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
```

Em `painel-principal-testes.cs`:

```csharp
    [Fact]
    public void Varrer_guarda_o_alvo_nos_ultimos_e_mostra_nas_opcoes()
    {
        var historico = new HistoricoEmMemoria();
        var motor = new MotorFalso();
        var p = new PainelPrincipal(new DependenciasPainel
        {
            Motor = motor,
            ListarUnidades = () => [_c],
            Historico = historico,
        });
        p.TextoAlvo = @"C:\Dados\Clientes";

        p.Varrer();

        Assert.Equal([@"C:\Dados\Clientes"], historico.Ler());
        Assert.Equal([@"C:\", @"C:\Dados\Clientes"], p.Opcoes.Select(o => o.Caminho));
    }
```

Esperado: erro de compilação.

- [ ] **Passo 2: implementação**

`ultimos-alvos.cs`:

```csharp
namespace MapDisk.Nucleo;

/// <summary>Uma opção da lista de alvos da janela: unidade ou alvo usado antes.</summary>
public sealed record ItemAlvo(string Caminho, string Descricao);

public interface IHistoricoAlvos
{
    IReadOnlyList<string> Ler();

    void Gravar(IReadOnlyList<string> alvos);
}

/// <summary>
/// Últimos alvos num arquivo do perfil do usuário, só nesta máquina. Falha de leitura ou de
/// gravação não atrapalha a varredura: a lista só fica vazia.
/// </summary>
public sealed class HistoricoAlvosArquivo(string arquivo) : IHistoricoAlvos
{
    public static HistoricoAlvosArquivo Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "alvos.txt"));

    public IReadOnlyList<string> Ler()
    {
        try
        {
            return File.Exists(arquivo)
                ? File.ReadAllLines(arquivo).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
                : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public void Gravar(IReadOnlyList<string> alvos)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, alvos);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class HistoricoEmMemoria : IHistoricoAlvos
{
    private IReadOnlyList<string> _alvos = [];

    public IReadOnlyList<string> Ler() => _alvos;

    public void Gravar(IReadOnlyList<string> alvos) => _alvos = alvos.ToList();
}

public static class UltimosAlvos
{
    public const int Maximo = 10;

    public static IReadOnlyList<string> Acrescentar(IReadOnlyList<string> atuais, string alvo) =>
        new[] { alvo }
            .Concat(atuais.Where(a => !string.Equals(a, alvo, StringComparison.OrdinalIgnoreCase)))
            .Take(Maximo)
            .ToList();
}
```

`dependencias-painel.cs`:

```csharp
namespace MapDisk.Nucleo;

/// <summary>O que o painel usa de fora. Os testes trocam cada peça por uma falsa.</summary>
public sealed class DependenciasPainel
{
    public required IMotorVarredura Motor { get; init; }

    public required Func<IReadOnlyList<InfoVolume>> ListarUnidades { get; init; }

    public IHistoricoAlvos Historico { get; init; } = new HistoricoEmMemoria();

    /// <summary>O processo já roda como administrador.</summary>
    public bool Administrador { get; init; }

    /// <summary>Reabre o programa como administrador varrendo o alvo.</summary>
    public Func<string, ResultadoElevacao> Elevar { get; init; } = _ => ResultadoElevacao.Recusada;
}
```

`ResultadoElevacao` nasce nesta tarefa, em `src/mapdisk.nucleo/sistema/elevacao.cs`, só com o `enum ResultadoElevacao { Aberta, Recusada }`. O resto da elevação vem na tarefa 6.

No `PainelPrincipal`: o construtor principal recebe `DependenciasPainel` e guarda os campos; o construtor antigo passa a ser `: this(new DependenciasPainel { Motor = motor, ListarUnidades = listarUnidades })`; `Padrao()` monta `new DependenciasPainel { Motor = new MotorVarredura(), ListarUnidades = Volumes.ListarUnidades, Historico = HistoricoAlvosArquivo.Padrao(), Administrador = Privilegios.EhAdministrador(), Elevar = Elevacao.Reabrir }` (as duas últimas vêm da tarefa 6: até lá, `Padrao()` fica sem elas). A lista de opções:

```csharp
    public IReadOnlyList<ItemAlvo> Opcoes { get; private set; } = [];

    private void MontarOpcoes()
    {
        var unidades = Unidades.Select(u => new ItemAlvo(u.Raiz, u.Descricao)).ToList();
        var recentes = _historico.Ler()
            .Where(a => !unidades.Any(u => string.Equals(u.Caminho, a, StringComparison.OrdinalIgnoreCase)))
            .Select(a => new ItemAlvo(a, $"{a}  (usado antes)"));
        Opcoes = unidades.Concat(recentes).ToList();
    }
```

`MontarOpcoes()` roda no construtor e em `AtualizarUnidades()`. Em `Iniciar`, antes de começar: `_historico.Gravar(UltimosAlvos.Acrescentar(_historico.Ler(), alvo)); MontarOpcoes();`.

- [ ] **Passo 3: rodar e ver passar.** Todos os testes verdes.

- [ ] **Passo 4: commit** `Cria a lista dos ultimos alvos`.

---

### Tarefa 6: privilégio de backup e varredura como administrador

**Arquivos:** novo `src/mapdisk.nucleo/sistema/privilegios.cs`; `src/mapdisk.nucleo/sistema/elevacao.cs`; `painel-principal.cs`. Testes: novos `privilegios-testes.cs` e `elevacao-testes.cs`; `painel-principal-testes.cs`.

**Interfaces:**
- Produz: `static partial class Privilegios` com `bool EhAdministrador()` e `bool LigarBackup()`; `static class Elevacao` com `const string Argumento = "--elevado"`, `string Argumentos(string alvo)`, `bool EhPedido(IReadOnlyList<string> args, out string? alvo)`, `ResultadoElevacao Reabrir(string alvo)`; no painel, `Administrador`, `MostrarElevar`, `PodeElevar`, `Elevar()`.

- [ ] **Passo 1: testes que falham**

`privilegios-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class PrivilegiosTestes
{
    [Fact]
    public void Sem_administrador_o_privilegio_de_backup_nao_liga()
    {
        if (Privilegios.EhAdministrador())
        {
            return;
        }

        Assert.False(Privilegios.LigarBackup());
    }
}
```

`elevacao-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class ElevacaoTestes
{
    [Theory]
    [InlineData(@"C:\", @"--elevado ""C:\\""")]
    [InlineData(@"D:\Dados", @"--elevado ""D:\Dados""")]
    [InlineData(@"\\srv\dados\", @"--elevado ""\\srv\dados\\""")]
    public void Argumentos_protegem_a_barra_final(string alvo, string esperado)
    {
        Assert.Equal(esperado, Elevacao.Argumentos(alvo));
    }

    [Fact]
    public void Reconhece_o_pedido_de_elevacao()
    {
        Assert.True(Elevacao.EhPedido(["--elevado", @"C:\"], out var alvo));
        Assert.Equal(@"C:\", alvo);
        Assert.False(Elevacao.EhPedido(["varrer", @"C:\"], out _));
        Assert.False(Elevacao.EhPedido([], out _));
    }
}
```

Em `painel-principal-testes.cs`:

```csharp
    [Fact]
    public void Elevar_reabre_como_administrador_com_o_alvo()
    {
        string? pedido = null;
        var p = new PainelPrincipal(new DependenciasPainel
        {
            Motor = new MotorFalso(),
            ListarUnidades = () => [_c],
            Elevar = alvo =>
            {
                pedido = alvo;
                return ResultadoElevacao.Aberta;
            },
        });
        p.TextoAlvo = "d:";

        p.Elevar();

        Assert.Equal(@"D:\", pedido);
        Assert.Contains("outra janela", p.TextoEstado);
        Assert.True(p.MostrarElevar);
    }

    [Fact]
    public void Elevacao_recusada_avisa_e_segue_sem_administrador()
    {
        var p = new PainelPrincipal(new DependenciasPainel
        {
            Motor = new MotorFalso(),
            ListarUnidades = () => [_c],
            Elevar = _ => ResultadoElevacao.Recusada,
        });

        p.Elevar();

        Assert.Contains("não confirmou", p.Erro);
        Assert.Equal(EstadoPainel.Parado, p.Estado);
    }

    [Fact]
    public void Ja_administrador_nao_mostra_o_botao()
    {
        var p = new PainelPrincipal(new DependenciasPainel
        {
            Motor = new MotorFalso(),
            ListarUnidades = () => [_c],
            Administrador = true,
        });

        Assert.False(p.MostrarElevar);
        Assert.False(p.PodeElevar);
    }
```

Esperado: erro de compilação.

- [ ] **Passo 2: implementação**

`privilegios.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace MapDisk.Nucleo;

/// <summary>
/// Administrador e privilégio de backup. Com o privilégio ligado, o CreateFile com
/// FILE_FLAG_BACKUP_SEMANTICS que o leitor já usa lê qualquer pasta local. Só leitura: o
/// programa nunca pede acesso de escrita.
/// </summary>
public static partial class Privilegios
{
    private const uint AjustarPrivilegios = 0x0020;
    private const uint Consultar = 0x0008;
    private const uint Ligado = 0x00000002;
    private const int NemTodosAtribuidos = 1300;

    public static bool EhAdministrador()
    {
        using var identidade = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identidade).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Liga o SeBackupPrivilege neste processo. Só dá certo com o processo elevado.</summary>
    public static bool LigarBackup()
    {
        if (!OpenProcessToken(GetCurrentProcess(), AjustarPrivilegios | Consultar, out var token))
        {
            return false;
        }

        try
        {
            if (!LookupPrivilegeValue(null, "SeBackupPrivilege", out var luid))
            {
                return false;
            }

            var novo = new PrivilegiosDoToken { Quantidade = 1, Luid = luid, Atributos = Ligado };
            if (!AdjustTokenPrivileges(token, false, ref novo, 0, 0, 0))
            {
                return false;
            }

            return Marshal.GetLastPInvokeError() != NemTodosAtribuidos;
        }
        finally
        {
            CloseHandle(token);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint Baixo;
        public int Alto;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrivilegiosDoToken
    {
        public uint Quantidade;
        public Luid Luid;
        public uint Atributos;
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint objeto);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(nint processo, uint acesso, out nint token);

    [LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LookupPrivilegeValue(string? sistema, string nome, out Luid luid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AdjustTokenPrivileges(nint token, [MarshalAs(UnmanagedType.Bool)] bool desligarTodos, ref PrivilegiosDoToken novo, uint tamanhoAnterior, nint anterior, nint tamanhoDevolvido);
}
```

`elevacao.cs` completo:

```csharp
using System.ComponentModel;
using System.Diagnostics;

namespace MapDisk.Nucleo;

public enum ResultadoElevacao
{
    Aberta,

    /// <summary>O técnico disse não no aviso do Windows, ou o Windows não deixou.</summary>
    Recusada,
}

/// <summary>Reabre o programa como administrador, já varrendo o alvo.</summary>
public static class Elevacao
{
    public const string Argumento = "--elevado";

    private const int CanceladaPeloUsuario = 1223;

    /// <summary>
    /// Argumentos da nova janela. A barra final é dobrada: sem isso, o Windows lê "C:\" entre
    /// aspas como aspas escapadas.
    /// </summary>
    public static string Argumentos(string alvo) => $"{Argumento} \"{(alvo.EndsWith('\\') ? alvo + "\\" : alvo)}\"";

    public static bool EhPedido(IReadOnlyList<string> args, out string? alvo)
    {
        alvo = args is [Argumento, var a] ? a : null;
        return alvo is not null;
    }

    public static ResultadoElevacao Reabrir(string alvo)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, Argumentos(alvo))
            {
                UseShellExecute = true,
                Verb = "runas",
            });
            return ResultadoElevacao.Aberta;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == CanceladaPeloUsuario)
        {
            return ResultadoElevacao.Recusada;
        }
    }
}
```

No `PainelPrincipal`:

```csharp
    public bool Administrador { get; }

    public bool MostrarElevar => !Administrador;

    public bool PodeElevar => !Administrador && Estado == EstadoPainel.Parado;

    public void Elevar()
    {
        if (!PodeElevar)
        {
            return;
        }

        var alvo = _ultimoAlvo ?? Alvo.Normalizar(TextoAlvo, out var erro);
        if (alvo is null)
        {
            Erro = erro;
            Avisar();
            return;
        }

        if (_elevar(alvo) == ResultadoElevacao.Aberta)
        {
            TextoEstado = "A varredura como administrador abriu em outra janela.";
        }
        else
        {
            Erro = "O Windows não confirmou a elevação. A varredura segue sem administrador.";
        }

        Avisar();
    }
```

`Padrao()` passa a passar `Administrador = Privilegios.EhAdministrador()` e `Elevar = Elevacao.Reabrir`.

- [ ] **Passo 3: rodar e ver passar.** Todos os testes verdes.

- [ ] **Passo 4: commit** `Cria a varredura como administrador com o privilegio de backup`.

---

### Tarefa 7: janela, menu de contexto e rótulo repetido

**Arquivos:** `src/mapdisk/programa.cs`, `modo-linha-de-comando.cs`, `janela-principal.xaml`, `janela-principal.xaml.cs`, novo `src/mapdisk/shell.cs`; `linha-arvore.cs`. Testes: `arvore-visivel-testes.cs`.

- [ ] **Passo 1: teste do rótulo que muda.** Em `Pasta_sem_acesso_mostra_sem_acesso_e_nunca_zero`, trocar `Assert.Equal("sem acesso", windows.Rotulo);` por `Assert.Equal("", windows.Rotulo);`. Rodar e ver falhar.

- [ ] **Passo 2: rótulo.** Em `LinhaArvore.Rotulo`, a pasta sem acesso passa a devolver `string.Empty`. A pasta pendente cai no caso padrão, que também fica vazio para ela: o caso padrão passa a ser `EstadoPasta.Lida when ...` para o "sem leitura dentro", e `_ => string.Empty`. Rodar e ver passar.

- [ ] **Passo 3: `programa.cs`.** No começo do `Main`:

```csharp
        if (Privilegios.EhAdministrador())
        {
            Privilegios.LigarBackup();
        }

        var elevado = Elevacao.EhPedido(args, out var alvoElevado);
```

A condição da linha de comando passa a ser `args.Length > 0 && !demonstracao && !elevado`. A janela recebe o sufixo " (administrador)" quando `Privilegios.EhAdministrador()`, e, com `elevado`, o painel começa com `TextoAlvo = alvoElevado!` e a janela chama `Varrer()` no evento `Loaded`: o construtor da janela ganha o parâmetro `bool varrerAoAbrir`.

- [ ] **Passo 4: `modo-linha-de-comando.cs`.** Antes de chamar o executor: `if (Privilegios.EhAdministrador()) { Privilegios.LigarBackup(); }`.

- [ ] **Passo 5: `shell.cs`**

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace MapDisk;

/// <summary>Ações de leitura no Windows: mostrar no Explorer, copiar o caminho e abrir as propriedades.</summary>
internal static partial class Shell
{
    private const uint PorCaminho = 2;

    public static void MostrarNoExplorer(string caminho, bool ehArquivo) =>
        Process.Start("explorer.exe", ehArquivo ? $"/select,\"{caminho}\"" : $"\"{caminho}\"");

    public static void CopiarCaminho(string caminho) => Clipboard.SetText(caminho);

    public static void Propriedades(nint janela, string caminho) => SHObjectProperties(janela, PorCaminho, caminho, null);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SHObjectProperties(nint janela, uint tipo, string objeto, string? pagina);
}
```

- [ ] **Passo 6: `janela-principal.xaml`.**

Na faixa do topo, depois de "Atualizar":

```xml
                    <Button Content="Varrer como administrador" Style="{StaticResource BotaoFaixaContorno}" Click="AoElevar"
                            IsEnabled="{Binding PodeElevar}"
                            Visibility="{Binding MostrarElevar, Converter={StaticResource VisivelSe}}"
                            ToolTip="Abre outra janela como administrador e lê também as pastas protegidas. Só leitura." />
```

A lista de alvos passa a usar as opções: `ItemsSource="{Binding Opcoes}" TextSearch.TextPath="Caminho"`, com o mesmo `ItemTemplate` mostrando `Descricao`.

Na barra de exibição, antes de "Mostrar:":

```xml
                <Button Content="Voltar" Style="{StaticResource BotaoContorno}" Click="AoVoltar" IsEnabled="{Binding PodeVoltar}" Margin="0,0,6,0" />
                <Button Content="Avançar" Style="{StaticResource BotaoContorno}" Click="AoAvancar" IsEnabled="{Binding PodeAvancar}" Margin="0,0,6,0" />
                <Button Content="Subir" Style="{StaticResource BotaoContorno}" Click="AoSubir" IsEnabled="{Binding PodeSubir}" Margin="0,0,12,0" />
                <TextBlock Text="Abrir até:" Style="{StaticResource TituloGrupo}" VerticalAlignment="Center" Margin="0,0,6,0" />
                <ComboBox x:Name="CampoNiveis" Width="90" SelectedIndex="0" SelectionChanged="AoMudarNiveis" Margin="0,0,20,0">
                    <ComboBoxItem Content="1 nível" Tag="1" />
                    <ComboBoxItem Content="2 níveis" Tag="2" />
                    <ComboBoxItem Content="3 níveis" Tag="3" />
                    <ComboBoxItem Content="4 níveis" Tag="4" />
                    <ComboBoxItem Content="5 níveis" Tag="5" />
                </ComboBox>
```

No `ListView`, o menu de contexto:

```xml
            <ListView.ContextMenu>
                <ContextMenu>
                    <MenuItem Header="Mostrar no Explorer" Click="AoMostrarNoExplorer" />
                    <MenuItem Header="Copiar caminho" Click="AoCopiarCaminho" />
                    <Separator />
                    <MenuItem Header="Abrir aqui" Click="AoAbrirAqui" />
                    <MenuItem Header="Atualizar esta pasta" InputGestureText="Shift+F5" Click="AoAtualizarPasta" />
                    <Separator />
                    <MenuItem Header="Propriedades" Click="AoPropriedades" />
                </ContextMenu>
            </ListView.ContextMenu>
```

- [ ] **Passo 7: `janela-principal.xaml.cs`.** Os manipuladores, todos pela linha selecionada (`Tabela.SelectedItem is LinhaArvore linha`):
  - `AoElevar`: `_painel.Elevar(); MostrarErro();`.
  - `AoVoltar`, `AoAvancar`, `AoSubir`: chamam o painel.
  - `AoMudarNiveis`: lê o `Tag` do item escolhido e chama `_painel.AbrirNiveis(n)`.
  - `AoMostrarNoExplorer`: `Shell.MostrarNoExplorer(linha.Caminho, linha.Tipo == TipoLinha.Arquivo)`.
  - `AoCopiarCaminho`: `Shell.CopiarCaminho(linha.Caminho)`.
  - `AoAbrirAqui`: com `linha.Tipo == TipoLinha.Pasta`, `_painel.AbrirAqui(linha.Pasta)`.
  - `AoAtualizarPasta`: com `linha.Tipo == TipoLinha.Pasta`, `_painel.AtualizarPasta(linha.Pasta)`.
  - `AoPropriedades`: `Shell.Propriedades(new WindowInteropHelper(this).Handle, linha.Caminho)`.
  - Em `AoTeclarNaTabela`, `Key.F5` com `Keyboard.Modifiers == ModifierKeys.Shift` chama `AoAtualizarPasta`, e `Key.Back` chama `_painel.Voltar()`.

- [ ] **Passo 8: build, testes e conferência na janela.** `dotnet build` sem aviso e todos os testes verdes. Depois, pelo agente, com o `.exe` do `publicar.cmd`:
  1. `--demonstracao`: "Abrir aqui" em Users, Voltar, Avançar, Subir, "Abrir até" 3 níveis, menu de contexto com "Copiar caminho".
  2. Varredura real sem administrador do C:, "Atualizar esta pasta" numa pasta com Shift+F5, e o total da raiz sem soma dobrada.
  3. "Varrer como administrador": o aviso do Windows aparece. Essa confirmação é do Manfred: o agente não clica em aviso de elevação. Com a confirmação, a janela nova abre com " (administrador)" no título, já varrendo, e a contagem de "sem acesso" cai a quase zero.
  4. Linha de comando num PowerShell elevado, pelo Manfred: `.\mapdisk.exe varrer C: | Out-Host`, com a contagem de sem acesso perto de zero.

  Os números ficam só na conversa.

- [ ] **Passo 9: commit** `Liga a navegacao, o menu de contexto e o administrador na janela`.

---

### Tarefa 8: documentação e Pull Request

- [ ] **Passo 1: pendências.** Em `docs/superpowers/pendencias.md`, fechar os itens da fatia 1 atendidos (últimos alvos, ramificação, botão de administrador, rótulo repetido, conferência da varredura e, se a medida deixar, memória), com a data e sem números do computador do Manfred. Acrescentar a seção da fatia 2 com o que ficar de fora, por exemplo:
  - "Esquecer" um alvo da lista de últimos alvos;
  - leitura como administrador em caminho de rede, que continua com as permissões do servidor.

- [ ] **Passo 2: README.** Na seção "Uso", acrescentar o botão "Varrer como administrador", o menu de contexto e o Shift+F5. Na "Situação do projeto", a linha da fatia 2 com o ramo `navegacao`.

- [ ] **Passo 3: portões e commit** `Traz a documentacao da fatia 2`.

- [ ] **Passo 4: Pull Request** do ramo `navegacao`, com "O que muda", "Como testar" (os passos 8.1 a 8.4 da tarefa 7) e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec e as pendências

| Item | Onde |
|---|---|
| R1 últimos alvos | Tarefa 5 |
| R2 atualizar uma ramificação | Tarefa 3 |
| R5 expandir N níveis, voltar e avançar | Tarefa 4 |
| R7 "Varrer como administrador" | Tarefa 6, com o privilégio de backup decidido em 29/09/2026 |
| R9 menu de contexto sem ações | Tarefa 7 (Mostrar no Explorer, Copiar caminho, Abrir aqui, Atualizar esta pasta, Propriedades) |
| Pendência: conferência da varredura | Tarefa 1 |
| Pendência: memória acima da meta | Tarefa 2 |
| Pendência: rótulo repetido | Tarefa 7 |
