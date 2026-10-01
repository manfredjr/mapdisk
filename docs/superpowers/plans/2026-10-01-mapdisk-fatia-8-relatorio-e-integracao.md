# Plano da fatia 8: relatório do técnico, linha de comando, Explorer, Opções e página

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** fechar a versão 1. O técnico exporta pela janela e pela linha de comando um relatório HTML com a marca da MT e o gráfico (R17, R18). Ele liga, numa tela de Opções, o item "Analisar com MapDisk" no menu do Explorer (R19), e as escolhas dele ficam guardadas entre sessões. A página do programa fica pronta em `public/`, sem publicar.

**Arquitetura:** tudo que tem regra fica no núcleo:
- `relatorios/relatorio-tecnico.cs` monta o HTML;
- `integracao/integracao-explorer.cs` grava e apaga as chaves do Explorer, atrás de uma interface para os testes não tocarem no registro de verdade;
- `painel/preferencias.cs` lê e grava as opções num arquivo de texto do perfil do usuário.

A janela WPF só liga botões e diálogos a essas peças.

**Tecnologia:** C# com .NET 8, WPF, xUnit, `Microsoft.Win32.Registry` (já faz parte do `net8.0-windows`). Sem pacote NuGet novo.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`:
- R11, R17, R18, R19;
- seção 5, módulos `relatorios/`, `linha-de-comando/` e `integracao/`;
- seção 6, Opções e Linha de comando;
- seção 13, onde o menu compacto do Windows 11 fica fora da versão 1.

Pendências que esta fatia fecha:
- quantidade de maiores arquivos configurável;
- imagem do gráfico no relatório;
- registro de ações nas Opções;
- critérios do "Sugerir" guardados;
- tamanho mínimo dos duplicados guardado;
- esquecer um alvo da lista.

## Restrições globais

- Regras do produto do `AGENTS.md`:
  - **2:** a linha de comando só varre, gera relatório e liga ou desliga o item do Explorer. Nunca apaga nem move arquivo.
  - **3:** o relatório nunca mostra 0 onde não houve leitura. Pasta sem acesso sai como "sem acesso", e a varredura interrompida avisa no topo.
  - **4:** nada pede administrador. As chaves ficam em `HKCU`.
  - **5:** o item do Explorer sai por completo quando desligado.
  - **6:** o relatório e a página não carregam nada da internet. Logo, estilo e gráfico vão dentro do arquivo.
- Os testes não gravam no registro do Windows de verdade. Eles usam o armazém de chaves em memória. Arquivos só dentro da pasta de saída dos testes.
- O relatório do técnico fica onde o técnico escolher e nunca entra no git. Imagem de tela da página sai só do modo `--demonstracao`.
- Texto da página e o texto das Opções sobre o Explorer passam pela `humanizar-ptbr`. Os textos jurídicos da página passam pela `legal-br` (licença, ausência de garantia, privacidade).
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Pasta do relatório do técnico | A pasta das análises: a selecionada na árvore, ou a raiz mostrada. Pela linha de comando, o alvo | Igual às análises e ao relatório para o cliente. O técnico já sabe onde está |
| Conteúdo do relatório | Cabeçalho com o logo da MT, o computador, a pasta e a data. Depois vêm: resumo com totais e avisos de leitura; o gráfico em blocos de um nível, em SVG, com legenda; as maiores pastas; os maiores arquivos; os tipos; os arquivos sem alteração há mais de 2 anos; o resumo por usuário, quando há `Users`; e os duplicados, quando a busca já rodou. Rodapé com versão e site | O que o técnico mostra ao cliente ou guarda na OS. O gráfico é a pendência da fatia 4 |
| Quantos itens por lista | Pela janela, o número de "Maiores arquivos" das Opções (padrão 100). Pela linha de comando, o `--top` (padrão 10) | O `--top` já existe e quer dizer "quantos mostrar" |
| PDF | Pelo "Imprimir" do navegador. O CSS de impressão tira as cores de fundo grandes e não quebra linha de tabela entre páginas | Sem biblioteca de PDF. O relatório para o cliente já faz assim |
| CSV pela janela | "Exportar" no cartão das pastas, com duas opções: "Relatório HTML..." e "Planilha CSV...". O CSV sai do `ExportadorCsv` que já existe, a partir da pasta das análises | Spec, seção 6: grupo Exportar |
| Linha de comando | `varrer <alvo> --relatorio <arquivo.html>`, junto ou não com `--csv`. `--integrar` e `--remover-integracao` ligam e desligam o item do Explorer | Spec, seção 6 |
| Item do Explorer | Chaves `HKCU\Software\Classes\Directory\shell\MapDisk`, `...\Drive\shell\MapDisk` e `...\Directory\Background\shell\MapDisk`. Cada uma tem texto "Analisar com MapDisk", ícone do próprio `.exe` e comando `"<exe>" --abrir "%1"`, ou `"%V"` no fundo da pasta | Sem administrador (regra 4). No Windows 11, aparece em "Mostrar mais opções". O menu compacto exige MSIX e assinatura (seção 13) |
| `.exe` que mudou de lugar | Opções mostra para onde o item aponta. Se não é este `.exe`, avisa "O item aponta para outro local" e o botão "Ligar" regrava | O programa roda de pendrive ou de pasta de rede (regra 5) |
| Abrir pelo Explorer | Novo argumento `--abrir <alvo>`: abre a janela e já varre o alvo. Aspas que sobram no fim, como em `C:\"` (o Explorer manda a raiz da unidade assim), viram barra | Sem essa correção, `"C:\"` chega como `C:"` |
| Onde as opções ficam | `%LOCALAPPDATA%\MapDisk\opcoes.txt`, uma linha `nome=valor` por opção. Valor estranho ou fora da faixa volta ao padrão. Falha de leitura ou de gravação não atrapalha: valem os padrões | Mesmo modelo dos últimos alvos. Arquivo legível pelo técnico |
| O que as Opções guardam | Maiores arquivos (10 a 1000, padrão 100). Tamanho mínimo dos duplicados (1 a 102400 MB, padrão 1). Os quatro critérios do "Sugerir": pastas (0 a 1000, padrão 20), arquivos (0 a 1000, padrão 50), anos (1 a 50, padrão 2) e tamanho mínimo (0 a 1048576 MB, padrão 100) | As pendências das fatias 3, 6 e 7 |
| Critérios do "Sugerir" | A janela do relatório para o cliente começa com os critérios guardados. Ao clicar em "Sugerir", grava os critérios usados | O técnico ajusta onde usa, sem precisar abrir Opções |
| Tela de Opções | Botão "Opções" na faixa do topo, ao lado de "Sobre". Seções: "Menu do Explorer" (estado, Ligar, Desligar), "Análises" (maiores arquivos, mínimo dos duplicados), "Relatório para o cliente" (os quatro critérios), "Últimos alvos" (lista, Esquecer, Limpar a lista) e "Registro de ações" (o caminho e o botão Abrir) | Spec, seção 6, mais as pendências |
| Lugar do registro de ações | Continua fixo em `%LOCALAPPDATA%\MapDisk\acoes.log`. As Opções mostram o caminho e abrem a pasta | Mudar de lugar no meio do uso partiria o histórico em dois arquivos. Fica como está, sem pendência |
| Pastas excluídas da varredura | **Fica para depois da versão 1**, por decisão do Manfred em 01/10/2026. Vai para as pendências | Pasta pulada teria que aparecer como "excluída" em todo total e relatório (regra 3). Mexe no motor, na árvore e nas contas, e é a parte mais arriscada da versão |
| Página | `public/index.html`, um arquivo só com o estilo dentro, mais `public/imagens/` (logo da MT, ícone do MapDisk e duas telas do modo `--demonstracao`). Download pelo link da última versão no GitHub Releases. Fica pronta no repositório. A publicação no cPanel é outro passo, com autorização | `AGENTS.md`, Publicação. O roteiro do cPanel é lido na hora de publicar, com pedido para ler fora da pasta |

## Git desta fatia

1. O plano entra pelo ramo `fatia-8-plano`, num Pull Request só do plano, com a linha da fatia 8 no README.
2. O código sai do `main` atualizado, no ramo `relatorio-e-integracao`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/painel/preferencias.cs` | `Preferencias`, leitura e gravação em texto, armazém em arquivo e em memória |
| `src/mapdisk.nucleo/painel/ultimos-alvos.cs` | `UltimosAlvos.Esquecer` |
| `src/mapdisk.nucleo/painel/painel-principal.cs`, `dependencias-painel.cs`, `painel-analises.cs` | Preferências aplicadas, alvos esquecidos, volume e interrupção expostos para o relatório |
| `src/mapdisk.nucleo/relatorios/html.cs` | `Html.H`, `Html.Js` e `Html.Logo`, tirados da `PaginaAvaliacao` para os dois relatórios usarem |
| `src/mapdisk.nucleo/relatorios/relatorio-tecnico.cs` | O HTML do técnico |
| `src/mapdisk.nucleo/grafico/paleta.cs` | `Paleta.CoresDe`, tirada do `PainelGrafico` |
| `src/mapdisk.nucleo/integracao/integracao-explorer.cs` | `IChavesUsuario`, chaves do Windows e em memória, ligar, desligar e estado |
| `src/mapdisk.nucleo/linha-de-comando/argumentos.cs` e `executor-cli.cs` | `--relatorio`, `--integrar`, `--remover-integracao`, ajuda |
| `src/mapdisk.nucleo/sistema/abertura.cs` | `--abrir <alvo>` e a correção das aspas do Explorer |
| `src/mapdisk/programa.cs` | `--abrir` abre a janela já varrendo |
| `src/mapdisk/janela-principal.xaml` e `.cs` | Botão Opções, menu Exportar |
| `src/mapdisk/janela-opcoes.xaml` e `.cs` | Tela de Opções |
| `src/mapdisk/janela-relatorio.xaml.cs` | Critérios guardados |
| `public/index.html`, `public/imagens/*` | Página |
| `testes/mapdisk.testes/preferencias-testes.cs`, `relatorio-tecnico-testes.cs`, `integracao-explorer-testes.cs`, `abertura-testes.cs`, `pagina-testes.cs` | Testes desta fatia |
| `testes/mapdisk.testes/argumentos-testes.cs`, `executor-cli-testes.cs`, `recursos-testes.cs` | Testes que crescem |

---

### Tarefa 1: preferências guardadas e últimos alvos esquecidos

**Arquivos:**
- Criar: `src/mapdisk.nucleo/painel/preferencias.cs`
- Modificar: `src/mapdisk.nucleo/painel/ultimos-alvos.cs`, `dependencias-painel.cs`, `painel-principal.cs`, `painel-analises.cs`
- Teste: `testes/mapdisk.testes/preferencias-testes.cs`

**Interfaces:**
- Produz:
  - `Preferencias` (record), com `Criterios`, `ComCriterios(CriteriosSugestao)`, `ParaTexto()`, `DeTexto(IEnumerable<string>)` e `Valido(nome, texto)`;
  - `IArmazemPreferencias` (`Ler()`, `Gravar(Preferencias)`), `ArquivoPreferencias` e `PreferenciasEmMemoria`;
  - `UltimosAlvos.Esquecer(atuais, alvo)`;
  - em `PainelPrincipal`: `Preferencias`, `GravarPreferencias(Preferencias)`, `UltimosUsados`, `EsquecerAlvo(string)`, `LimparAlvos()`, `Volume`, `Interrompida`;
  - `PainelAnalises.QuantosMaiores`.

- [ ] **Passo 1: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class PreferenciasTestes
{
    [Fact]
    public void Texto_vai_e_volta_igual()
    {
        var p = new Preferencias(MaioresArquivos: 250, DuplicadosMinimoMb: 10, SugerirPastas: 5, SugerirArquivos: 7, SugerirAnos: 3, SugerirMinimoMb: 500);
        Assert.Equal(p, Preferencias.DeTexto(p.ParaTexto()));
    }

    [Fact]
    public void Valor_estranho_ou_fora_da_faixa_volta_ao_padrao()
    {
        var p = Preferencias.DeTexto(["maiores-arquivos=5", "duplicados-minimo-mb=abc", "sugerir-anos=0", "outra=1", "sem igual", "sugerir-pastas=30"]);
        Assert.Equal(100, p.MaioresArquivos);
        Assert.Equal(1, p.DuplicadosMinimoMb);
        Assert.Equal(2, p.SugerirAnos);
        Assert.Equal(30, p.SugerirPastas);
        Assert.True(Preferencias.Valido("maiores-arquivos", " 50 "));
        Assert.False(Preferencias.Valido("maiores-arquivos", "2000"));
        Assert.False(Preferencias.Valido("nao-existe", "1"));
    }

    [Fact]
    public void Criterios_saem_em_bytes_e_voltam()
    {
        var p = new Preferencias(SugerirMinimoMb: 200);
        Assert.Equal(200L * 1024 * 1024, p.Criterios.TamanhoMinimo);
        var c = new CriteriosSugestao(1, 2, 3, 4L * 1024 * 1024);
        Assert.Equal(c, p.ComCriterios(c).Criterios);
    }

    [Fact]
    public void Arquivo_grava_le_e_tolera_falta()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "preferencias-" + Guid.NewGuid().ToString("N"), "opcoes.txt");
        var armazem = new ArquivoPreferencias(arquivo);
        Assert.Equal(new Preferencias(), armazem.Ler());
        armazem.Gravar(new Preferencias(MaioresArquivos: 300));
        Assert.Equal(300, armazem.Ler().MaioresArquivos);
        Directory.Delete(Path.GetDirectoryName(arquivo)!, true);
    }

    [Fact]
    public void Esquecer_tira_so_aquele_alvo()
    {
        Assert.Equal([@"D:\"], UltimosAlvos.Esquecer([@"C:\Dados", @"D:\"], @"c:\dados"));
    }

    [Fact]
    public void Painel_aplica_e_grava_as_preferencias()
    {
        var armazem = new PreferenciasEmMemoria();
        armazem.Gravar(new Preferencias(MaioresArquivos: 20, DuplicadosMinimoMb: 8));
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Preferencias = armazem,
        });
        Assert.Equal(20, painel.Analises.QuantosMaiores);
        Assert.Equal(8, painel.Analises.Duplicados.TamanhoMinimoMb);

        painel.GravarPreferencias(new Preferencias(MaioresArquivos: 40));
        Assert.Equal(40, armazem.Ler().MaioresArquivos);
        Assert.Equal(40, painel.Analises.QuantosMaiores);
    }

    [Fact]
    public void Painel_esquece_e_limpa_os_alvos()
    {
        var historico = new HistoricoEmMemoria();
        historico.Gravar([@"E:\Fotos", @"F:\"]);
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Historico = historico,
        });
        painel.EsquecerAlvo(@"E:\Fotos");
        Assert.Equal([@"F:\"], painel.UltimosUsados);
        Assert.DoesNotContain(painel.Opcoes, o => o.Caminho == @"E:\Fotos");
        painel.LimparAlvos();
        Assert.Empty(painel.UltimosUsados);
    }

    [Fact]
    public async Task Analises_usam_a_quantidade_de_maiores()
    {
        var raiz = Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao.Result.Raiz;
        var analises = new PainelAnalises { QuantosMaiores = 3 };
        await analises.CalcularAsync(raiz, new DateTime(2026, 10, 1));
        Assert.Equal(3, analises.Maiores.Count);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter PreferenciasTestes`
Esperado: não compila, `Preferencias` não existe.

- [ ] **Passo 3: criar `preferencias.cs`**

```csharp
using System.Globalization;

namespace MapDisk.Nucleo;

/// <summary>Escolhas do técnico que valem entre sessões, só nesta máquina.</summary>
public sealed record Preferencias(
    int MaioresArquivos = 100,
    long DuplicadosMinimoMb = 1,
    int SugerirPastas = 20,
    int SugerirArquivos = 50,
    int SugerirAnos = 2,
    long SugerirMinimoMb = 100)
{
    private const long Mb = 1024 * 1024;

    public CriteriosSugestao Criterios => new(SugerirPastas, SugerirArquivos, SugerirAnos, SugerirMinimoMb * Mb);

    public Preferencias ComCriterios(CriteriosSugestao c) => this with
    {
        SugerirPastas = c.MaioresPastas,
        SugerirArquivos = c.MaioresArquivos,
        SugerirAnos = c.AnosSemAlteracao,
        SugerirMinimoMb = c.TamanhoMinimo / Mb,
    };

    public IReadOnlyList<string> ParaTexto() =>
    [
        $"maiores-arquivos={N(MaioresArquivos)}",
        $"duplicados-minimo-mb={N(DuplicadosMinimoMb)}",
        $"sugerir-pastas={N(SugerirPastas)}",
        $"sugerir-arquivos={N(SugerirArquivos)}",
        $"sugerir-anos={N(SugerirAnos)}",
        $"sugerir-minimo-mb={N(SugerirMinimoMb)}",
    ];

    /// <summary>Linha que não se entende fica de fora; valor fora da faixa fica com o padrão.</summary>
    public static Preferencias DeTexto(IEnumerable<string> linhas)
    {
        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linha in linhas)
        {
            var i = linha.IndexOf('=');
            if (i > 0)
            {
                valores[linha[..i].Trim()] = linha[(i + 1)..].Trim();
            }
        }

        var p = new Preferencias();
        return p with
        {
            MaioresArquivos = (int)Ler(valores, "maiores-arquivos", p.MaioresArquivos),
            DuplicadosMinimoMb = Ler(valores, "duplicados-minimo-mb", p.DuplicadosMinimoMb),
            SugerirPastas = (int)Ler(valores, "sugerir-pastas", p.SugerirPastas),
            SugerirArquivos = (int)Ler(valores, "sugerir-arquivos", p.SugerirArquivos),
            SugerirAnos = (int)Ler(valores, "sugerir-anos", p.SugerirAnos),
            SugerirMinimoMb = Ler(valores, "sugerir-minimo-mb", p.SugerirMinimoMb),
        };
    }

    /// <summary>Confere um valor digitado na tela de Opções.</summary>
    public static bool Valido(string nome, string texto) =>
        Faixas.TryGetValue(nome, out var f)
        && long.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
        && n >= f.Minimo && n <= f.Maximo;

    private static readonly Dictionary<string, (long Minimo, long Maximo)> Faixas = new()
    {
        ["maiores-arquivos"] = (10, 1000),
        ["duplicados-minimo-mb"] = (1, 102400),
        ["sugerir-pastas"] = (0, 1000),
        ["sugerir-arquivos"] = (0, 1000),
        ["sugerir-anos"] = (1, 50),
        ["sugerir-minimo-mb"] = (0, 1048576),
    };

    private static long Ler(Dictionary<string, string> valores, string nome, long padrao) =>
        valores.TryGetValue(nome, out var texto) && Valido(nome, texto)
            ? long.Parse(texto.Trim(), CultureInfo.InvariantCulture)
            : padrao;

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);
}

public interface IArmazemPreferencias
{
    Preferencias Ler();

    void Gravar(Preferencias preferencias);
}

/// <summary>Opções num arquivo do perfil do usuário. Falha de leitura ou de gravação deixa os padrões.</summary>
public sealed class ArquivoPreferencias(string arquivo) : IArmazemPreferencias
{
    public static ArquivoPreferencias Padrao() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk", "opcoes.txt"));

    public Preferencias Ler()
    {
        try
        {
            return File.Exists(arquivo) ? Preferencias.DeTexto(File.ReadAllLines(arquivo)) : new Preferencias();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new Preferencias();
        }
    }

    public void Gravar(Preferencias preferencias)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllLines(arquivo, preferencias.ParaTexto());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class PreferenciasEmMemoria : IArmazemPreferencias
{
    private Preferencias _atual = new();

    public Preferencias Ler() => _atual;

    public void Gravar(Preferencias preferencias) => _atual = preferencias;
}
```

- [ ] **Passo 4: esquecer um alvo, em `ultimos-alvos.cs`, dentro de `UltimosAlvos`**

```csharp
    public static IReadOnlyList<string> Esquecer(IReadOnlyList<string> atuais, string alvo) =>
        atuais.Where(a => !string.Equals(a, alvo, StringComparison.OrdinalIgnoreCase)).ToList();
```

- [ ] **Passo 5: ligar no painel**

`dependencias-painel.cs`:

```csharp
    /// <summary>Opções guardadas. O padrão fica só na memória; o Padrao() do painel liga o arquivo.</summary>
    public IArmazemPreferencias Preferencias { get; init; } = new PreferenciasEmMemoria();
```

`painel-analises.cs`:
- Acrescentar `public int QuantosMaiores { get; set; } = 100;`.
- Em `CalcularAsync`, ler `var quantos = QuantosMaiores;` antes do `Task.Run` e trocar `Analises.MaioresArquivos(pasta)` por `Analises.MaioresArquivos(pasta, quantos)`.

`painel-principal.cs`:
- `Padrao()` ganha `Preferencias = ArquivoPreferencias.Padrao(),`.
- No construtor, depois de criar `Analises`:

```csharp
        _preferencias = dependencias.Preferencias;
        Aplicar(_preferencias.Ler());
```

- Membros novos:

```csharp
    private readonly IArmazemPreferencias _preferencias;

    public Preferencias Preferencias { get; private set; } = new();

    /// <summary>Volume da última varredura, para o relatório do técnico.</summary>
    public InfoVolume? Volume => _volume;

    /// <summary>A última varredura foi interrompida antes do fim.</summary>
    public bool Interrompida { get; private set; }

    public IReadOnlyList<string> UltimosUsados => _historico.Ler();

    public void GravarPreferencias(Preferencias preferencias)
    {
        _preferencias.Gravar(preferencias);
        Aplicar(preferencias);
    }

    public void EsquecerAlvo(string alvo)
    {
        _historico.Gravar(UltimosAlvos.Esquecer(_historico.Ler(), alvo));
        MontarOpcoes();
        Avisar();
    }

    public void LimparAlvos()
    {
        _historico.Gravar([]);
        MontarOpcoes();
        Avisar();
    }

    private void Aplicar(Preferencias p)
    {
        Preferencias = p;
        Analises.QuantosMaiores = p.MaioresArquivos;
        Analises.Duplicados.TamanhoMinimoMb = p.DuplicadosMinimoMb;
    }
```

- Onde a conclusão grava `_volume = r.Volume;`, acrescentar `Interrompida = r.Cancelada;`.

- [ ] **Passo 6: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes.

- [ ] **Passo 7: commit**

```bash
sh .superpowers/rascunho/commit.sh "Guarda as opcoes do tecnico e esquece alvos" "Quantidade de maiores arquivos, minimo dos duplicados e criterios do Sugerir passam a valer entre sessoes, num arquivo do perfil. Fecha as pendencias das fatias 3, 6 e 7." src/mapdisk.nucleo/painel testes/mapdisk.testes/preferencias-testes.cs
```

---

### Tarefa 2: relatório HTML do técnico

**Arquivos:**
- Criar: `src/mapdisk.nucleo/relatorios/html.cs`, `src/mapdisk.nucleo/relatorios/relatorio-tecnico.cs`
- Modificar: `src/mapdisk.nucleo/relatorios/pagina-avaliacao.cs` (usa `Html`), `src/mapdisk.nucleo/grafico/paleta.cs` (`CoresDe`), `src/mapdisk.nucleo/painel/painel-grafico.cs` (usa `Paleta.CoresDe`)
- Teste: `testes/mapdisk.testes/relatorio-tecnico-testes.cs`

**Interfaces:**
- Consome: `Analises.*`, `ItensGrafico.DaPasta`, `Treemap.Dispor`, `Paleta`, `Formatador`, `ResultadoDuplicados`, `InfoVolume`.
- Produz:
  - `Html.H(string)`, `Html.Js(string)`, `Html.Logo()`;
  - `Paleta.CoresDe(IReadOnlyList<ItemGrafico>)`;
  - `DadosRelatorio(NoPasta Pasta, DateTime Gerado, string Computador, InfoVolume? Volume, bool Interrompida, int Quantos, ResultadoDuplicados? Duplicados)`;
  - `RelatorioTecnico.Gerar(DadosRelatorio)`, que devolve o texto;
  - `RelatorioTecnico.Gravar(DadosRelatorio, string arquivo)`, que grava em UTF-8 sem BOM.

- [ ] **Passo 1: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class RelatorioTecnicoTestes
{
    private static NoPasta Raiz() => Demonstracao.Motor().Iniciar(@"C:\", CancellationToken.None).Conclusao.Result.Raiz;

    private static DadosRelatorio Dados(NoPasta pasta, bool interrompida = false, ResultadoDuplicados? duplicados = null) =>
        new(pasta, new DateTime(2026, 10, 1, 14, 30, 0), "ESTACAO-01", new InfoVolume(@"C:\", "Sistema", "NTFS", 50L << 30, 200L << 30, 4096), interrompida, 10, duplicados);

    [Fact]
    public void Tem_marca_resumo_grafico_e_listas()
    {
        var html = RelatorioTecnico.Gerar(Dados(Raiz()));
        Assert.StartsWith("<!doctype html>", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("ESTACAO-01", html);
        Assert.Contains("01/10/2026 14:30", html);
        Assert.Contains("<svg", html);
        foreach (var titulo in new[] { "Resumo", "Gráfico", "Maiores pastas", "Maiores arquivos", "Por tipo", "Sem alteração há mais de 2 anos" })
        {
            Assert.Contains($"<h2>{titulo}</h2>", html);
        }

        Assert.Contains("50 GB livres de 200 GB", html);
    }

    [Fact]
    public void Pasta_sem_acesso_nunca_vira_zero()
    {
        var html = RelatorioTecnico.Gerar(Dados(Raiz()));
        Assert.Contains("System Volume Information", html);
        Assert.Contains("sem acesso", html);
        Assert.Contains("não entra nesta conta", html);
    }

    [Fact]
    public void Varredura_interrompida_avisa_no_topo()
    {
        var html = RelatorioTecnico.Gerar(Dados(Raiz(), interrompida: true));
        Assert.Contains("A varredura foi interrompida", html);
    }

    [Fact]
    public void Nao_carrega_nada_de_fora()
    {
        var html = RelatorioTecnico.Gerar(Dados(Raiz()));
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("<link", html);
        Assert.DoesNotContain("<script src", html);
    }

    [Fact]
    public void Nome_com_caractere_de_html_e_escapado()
    {
        var raiz = new NoPasta(@"C:\", null);
        raiz.Preencher([], [new NoPasta("<b>pasta</b>", raiz)]);
        var html = RelatorioTecnico.Gerar(Dados(raiz));
        Assert.DoesNotContain("<b>pasta</b>", html);
        Assert.Contains("&lt;b&gt;pasta&lt;/b&gt;", html);
    }

    [Fact]
    public void Duplicados_entram_so_quando_houve_busca()
    {
        Assert.DoesNotContain("<h2>Duplicados</h2>", RelatorioTecnico.Gerar(Dados(Raiz())));
        var vazio = new ResultadoDuplicados(Raiz(), [], 0, false);
        Assert.Contains("<h2>Duplicados</h2>", RelatorioTecnico.Gerar(Dados(Raiz(), duplicados: vazio)));
    }

    [Fact]
    public void Grava_em_utf8()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "relatorio-" + Guid.NewGuid().ToString("N") + ".html");
        RelatorioTecnico.Gravar(Dados(Raiz()), arquivo);
        Assert.Contains("Relatório de espaço em disco", File.ReadAllText(arquivo));
        File.Delete(arquivo);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RelatorioTecnicoTestes`
Esperado: não compila.

- [ ] **Passo 3: tirar os ajudantes comuns para `html.cs`**

```csharp
using System.Net;

namespace MapDisk.Nucleo;

/// <summary>O que os dois relatórios HTML usam: escape e o logo da MT embutido.</summary>
public static class Html
{
    public static string H(string texto) => WebUtility.HtmlEncode(texto);

    /// <summary>Texto dentro de aspas no JavaScript: barra, aspas e o fim de script escapados.</summary>
    public static string Js(string texto) => texto.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("<", "\\u003c");

    public static string Logo()
    {
        using var fluxo = typeof(Html).Assembly.GetManifestResourceStream("mt-logo.png")!;
        using var memoria = new MemoryStream();
        fluxo.CopyTo(memoria);
        return Convert.ToBase64String(memoria.ToArray());
    }
}
```

Em `pagina-avaliacao.cs`, apagar os três métodos privados `H`, `Js` e `Logo`, e trocar as chamadas por `Html.H`, `Html.Js` e `Html.Logo()`. Uma forma simples é pôr `using static MapDisk.Nucleo.Html;` no topo e trocar só `Logo()`, que continua igual. Os testes do relatório para o cliente têm que seguir verdes.

- [ ] **Passo 4: `Paleta.CoresDe`**

Mover o `Cores` privado do `PainelGrafico` para a `Paleta`, público, com o nome `CoresDe`:

```csharp
    /// <summary>As subpastas contam a posição entre elas, para a primeira ser sempre o verde da MT.</summary>
    public static string[] CoresDe(IReadOnlyList<ItemGrafico> itens)
    {
        var cores = new string[itens.Count];
        var pasta = 0;
        for (var i = 0; i < itens.Count; i++)
        {
            cores[i] = Cor(itens[i].Tipo == TipoItemGrafico.Pasta ? pasta++ : 0, itens[i].Tipo);
        }

        return cores;
    }
```

Em `painel-grafico.cs`, trocar `Cores(c.Itens)` por `Paleta.CoresDe(c.Itens)` e apagar o método privado.

- [ ] **Passo 5: criar `relatorio-tecnico.cs`**

```csharp
using System.Globalization;
using System.Text;
using static MapDisk.Nucleo.Html;

namespace MapDisk.Nucleo;

public sealed record DadosRelatorio(NoPasta Pasta, DateTime Gerado, string Computador, InfoVolume? Volume, bool Interrompida, int Quantos, ResultadoDuplicados? Duplicados);

/// <summary>
/// Relatório do técnico (R17): um arquivo HTML só, com logo, estilo e gráfico dentro, sem nada
/// da internet (regra 6). Pasta sem leitura aparece com o motivo, nunca com zero (regra 3).
/// </summary>
public static class RelatorioTecnico
{
    private const int LarguraGrafico = 960;
    private const int AlturaGrafico = 360;
    private const int MaximoNoGrafico = 20;

    private const string Estilo =
        "body{font-family:Segoe UI,Arial,sans-serif;color:#202020;background:#F4F4F4;margin:0}" +
        "header{display:flex;gap:16px;align-items:center;background:#006B2D;color:#fff;padding:12px 20px}" +
        "header img{height:56px;background:#fff;border-radius:8px;padding:4px}header h1{margin:0;font-size:22px}header p{margin:4px 0 0}" +
        "main{max-width:1100px;margin:0 auto;padding:16px 20px}h2{color:#006B2D;border-left:4px solid #43A92C;padding-left:8px}" +
        ".aviso{background:#fff4d6;border:1px solid #e0b100;padding:8px 12px}" +
        "table{width:100%;border-collapse:collapse;background:#fff}th,td{border:1px solid #ddd;padding:6px;font-size:13px;vertical-align:top}" +
        "th{background:#eef6ea;text-align:left}td.n{text-align:right;white-space:nowrap}svg{max-width:100%;height:auto;background:#fff}" +
        ".cor{display:inline-block;width:12px;height:12px;margin-right:6px;vertical-align:middle}" +
        "footer{margin-top:24px;font-size:12px;color:#666}" +
        "@media print{body{background:#fff}header{background:#fff;color:#006B2D;border-bottom:2px solid #006B2D}tr{page-break-inside:avoid}svg{page-break-inside:avoid}}";

    public static void Gravar(DadosRelatorio dados, string arquivo) =>
        File.WriteAllText(arquivo, Gerar(dados), new UTF8Encoding(false));

    public static string Gerar(DadosRelatorio d)
    {
        var p = d.Pasta;
        var caminho = p.CaminhoCompleto();
        var s = new StringBuilder();
        s.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        s.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.Append($"<title>Espaço em disco - {H(caminho)}</title><style>{Estilo}</style></head><body>");
        s.Append($"<header><img alt=\"MT - Manfred Tecnologia\" src=\"data:image/png;base64,{Logo()}\"><div><h1>Relatório de espaço em disco</h1>");
        s.Append($"<p>{H(d.Computador)} | {H(caminho)} | {d.Gerado.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}</p></div></header><main>");
        if (d.Interrompida)
        {
            s.Append("<p class=\"aviso\">A varredura foi interrompida antes do fim. Os números mostram só o que foi lido até ali.</p>");
        }

        Resumo(s, d);
        Grafico(s, p);
        MaioresPastas(s, p, d.Quantos);
        Arquivos(s, "Maiores arquivos", Analises.MaioresArquivos(p, d.Quantos));
        Tipos(s, p);
        var antigos = Analises.ArquivosAntigos(p, Idade.DoisAnos, d.Gerado, d.Quantos);
        s.Append($"<h2>Sem alteração há mais de 2 anos</h2><p>{H(Formatador.Plural(antigos.Quantidade, "arquivo", "arquivos"))}, {H(Formatador.Tamanho(antigos.Tamanho))}.</p>");
        Arquivos(s, null, antigos.Maiores);
        Usuarios(s, p);
        if (d.Duplicados is { } dup)
        {
            Duplicados(s, dup, d.Quantos);
        }

        s.Append($"<footer>Gerado pelo MapDisk - MT {H(ExecutorCli.Versao)}, da MT - Manfred Tecnologia (<a href=\"{Sobre.SiteMt}\">www.manfred.com.br</a>).</footer>");
        s.Append("</main></body></html>");
        return s.ToString();
    }

    private static void Resumo(StringBuilder s, DadosRelatorio d)
    {
        var p = d.Pasta;
        s.Append($"<h2>Resumo</h2><p>Total lido: <b>{H(Formatador.Tamanho(p.Tamanho))}</b> ({H(Formatador.Tamanho(p.Alocado))} alocados) ");
        s.Append($"em {H(Formatador.Plural(p.ArquivosTotal, "arquivo", "arquivos"))} e {H(Formatador.Plural(p.PastasTotal, "pasta", "pastas"))}.");
        if (d.Volume is { } v)
        {
            s.Append($" Unidade {H(v.Raiz)}: {H(Formatador.Tamanho(v.Livre))} livres de {H(Formatador.Tamanho(v.Total))} ({H(v.SistemaArquivos)}).");
        }

        s.Append("</p>");
        var semLeitura = p.PastasSemAcesso + p.PastasComErro;
        if (semLeitura > 0)
        {
            s.Append($"<p class=\"aviso\">{H(Formatador.Plural(semLeitura, "pasta não pôde ser lida e não entra nesta conta", "pastas não puderam ser lidas e não entram nesta conta"))}. ");
            s.Append("Varrer como administrador lê as pastas locais sem acesso.</p>");
        }
    }

    private static void Grafico(StringBuilder s, NoPasta p)
    {
        var c = ItensGrafico.DaPasta(p, ModoExibicao.Tamanho, MaximoNoGrafico);
        if (c.Itens.Count == 0)
        {
            return;
        }

        var blocos = Treemap.Dispor(c.Itens.Select(i => i.Valor).ToList(), LarguraGrafico, AlturaGrafico);
        var cores = Paleta.CoresDe(c.Itens);
        s.Append($"<h2>Gráfico</h2><svg viewBox=\"0 0 {LarguraGrafico} {AlturaGrafico}\" role=\"img\" aria-label=\"Gráfico em blocos\">");
        for (var i = 0; i < blocos.Count; i++)
        {
            var b = blocos[i];
            s.Append(FormattableString.Invariant($"<rect x=\"{b.X:0.#}\" y=\"{b.Y:0.#}\" width=\"{b.Largura:0.#}\" height=\"{b.Altura:0.#}\" fill=\"{cores[i]}\" stroke=\"#fff\"><title>{H(c.Itens[i].Nome)}</title></rect>"));
            if (b.Largura >= 90 && b.Altura >= 28)
            {
                s.Append(FormattableString.Invariant($"<text x=\"{b.X + 6:0.#}\" y=\"{b.Y + 18:0.#}\" fill=\"{Paleta.CorTexto(cores[i])}\" font-size=\"13\">{H(c.Itens[i].Nome)}</text>"));
            }
        }

        s.Append("</svg><table><thead><tr><th>Item</th><th>Tamanho</th><th>% da pasta</th></tr></thead><tbody>");
        for (var i = 0; i < c.Itens.Count; i++)
        {
            var item = c.Itens[i];
            s.Append($"<tr><td><span class=\"cor\" style=\"background:{cores[i]}\"></span>{H(item.Nome)}</td><td class=\"n\">{H(Formatador.Tamanho(item.Valor))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(c.Total > 0 ? (double)item.Valor / c.Total : 0))}</td></tr>");
        }

        s.Append("</tbody></table>");
        if (c.NaoLidas.Count > 0)
        {
            s.Append($"<p>Fora do gráfico, sem leitura: {H(string.Join(", ", c.NaoLidas))}.</p>");
        }
    }

    private static void MaioresPastas(StringBuilder s, NoPasta p, int quantos)
    {
        s.Append("<h2>Maiores pastas</h2><table><thead><tr><th>Pasta</th><th>Tamanho</th><th>% da pasta</th><th>Arquivos</th><th>Última alteração</th></tr></thead><tbody>");
        var lidas = p.Subpastas.Where(x => x.Estado == EstadoPasta.Lida).OrderByDescending(x => x.Tamanho).Take(quantos);
        var semLeitura = p.Subpastas.Where(x => x.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura or EstadoPasta.Pendente);
        foreach (var x in lidas)
        {
            s.Append($"<tr><td>{H(x.Nome)}</td><td class=\"n\">{H(Formatador.Tamanho(x.Tamanho))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(p.Tamanho > 0 ? (double)x.Tamanho / p.Tamanho : 0))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Numero(x.ArquivosTotal))}</td><td>{H(Formatador.Data(x.UltimaModificacao))}</td></tr>");
        }

        foreach (var x in semLeitura)
        {
            var estado = x.Estado switch
            {
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.ErroLeitura => "erro de leitura",
                _ => "não lida",
            };
            s.Append($"<tr><td>{H(x.Nome)}</td><td colspan=\"4\">{estado}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Arquivos(StringBuilder s, string? titulo, IReadOnlyList<ArquivoEncontrado> arquivos)
    {
        if (titulo is not null)
        {
            s.Append($"<h2>{H(titulo)}</h2>");
        }

        if (arquivos.Count == 0)
        {
            s.Append("<p>Nenhum.</p>");
            return;
        }

        s.Append("<table><thead><tr><th>Arquivo</th><th>Pasta</th><th>Tamanho</th><th>Última alteração</th></tr></thead><tbody>");
        foreach (var a in arquivos)
        {
            s.Append($"<tr><td>{H(a.Arquivo.Nome)}</td><td>{H(a.Pasta.CaminhoCompleto())}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Tamanho(a.Arquivo.Tamanho))}</td><td>{H(Formatador.Data(a.Arquivo.Modificacao))}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Tipos(StringBuilder s, NoPasta p)
    {
        s.Append("<h2>Por tipo</h2><table><thead><tr><th>Tipo</th><th>Arquivos</th><th>Tamanho</th><th>% da pasta</th></tr></thead><tbody>");
        foreach (var t in Analises.PorCategoria(p))
        {
            s.Append($"<tr><td>{H(t.Nome)}</td><td class=\"n\">{H(Formatador.Numero(t.Quantidade))}</td><td class=\"n\">{H(Formatador.Tamanho(t.Tamanho))}</td>");
            s.Append($"<td class=\"n\">{H(Formatador.Porcentagem(p.Tamanho > 0 ? (double)t.Tamanho / p.Tamanho : 0))}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Usuarios(StringBuilder s, NoPasta p)
    {
        var u = Analises.PorUsuario(p);
        if (u.PastaDePerfis is null)
        {
            return;
        }

        s.Append("<h2>Por usuário</h2><table><thead><tr><th>Perfil</th><th>Tamanho</th></tr></thead><tbody>");
        foreach (var perfil in u.Perfis)
        {
            var valor = perfil.Estado == EstadoPasta.Lida ? Formatador.Tamanho(perfil.Tamanho) : "sem acesso";
            s.Append($"<tr><td>{H(perfil.Nome)}</td><td class=\"n\">{H(valor)}</td></tr>");
        }

        s.Append("</tbody></table>");
    }

    private static void Duplicados(StringBuilder s, ResultadoDuplicados r, int quantos)
    {
        s.Append("<h2>Duplicados</h2>");
        if (r.Grupos.Count == 0)
        {
            s.Append("<p>Nenhum arquivo repetido acima do tamanho mínimo da busca.</p>");
            return;
        }

        var copias = r.Grupos.Sum(g => g.Repetido);
        s.Append($"<p>{H(Formatador.Plural(r.Grupos.Count, "grupo", "grupos"))} com {H(Formatador.Tamanho(copias))} em cópias.</p>");
        s.Append("<table><thead><tr><th>Grupo</th><th>Arquivo</th><th>Tamanho</th></tr></thead><tbody>");
        foreach (var g in r.Grupos.Take(quantos))
        {
            foreach (var a in g.Arquivos)
            {
                s.Append($"<tr><td>{g.Numero}</td><td>{H(a.Caminho)}</td><td class=\"n\">{H(Formatador.Tamanho(g.Tamanho))}</td></tr>");
            }
        }

        s.Append("</tbody></table>");
    }
}
```

- [ ] **Passo 6: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes, inclusive os do relatório para o cliente e os do gráfico.

- [ ] **Passo 7: commit**

```bash
sh .superpowers/rascunho/commit.sh "Cria o relatorio HTML do tecnico" "R17: um arquivo so, com o logo da MT e o grafico em blocos dentro, imprimivel em PDF pelo navegador. Pasta sem leitura aparece com o motivo, nunca com zero." src/mapdisk.nucleo/relatorios src/mapdisk.nucleo/grafico/paleta.cs src/mapdisk.nucleo/painel/painel-grafico.cs testes/mapdisk.testes/relatorio-tecnico-testes.cs
```

---

### Tarefa 3: item "Analisar com MapDisk" no Explorer

**Arquivos:**
- Criar: `src/mapdisk.nucleo/integracao/integracao-explorer.cs`, `src/mapdisk.nucleo/sistema/abertura.cs`
- Teste: `testes/mapdisk.testes/integracao-explorer-testes.cs`, `testes/mapdisk.testes/abertura-testes.cs`

**Interfaces:**
- Produz:
  - `IChavesUsuario` (`Gravar(chave, nome, valor)`, `Ler(chave, nome)`, `ApagarArvore(chave)`), com as implementações `ChavesUsuarioWindows` e `ChavesEmMemoria`;
  - `IntegracaoExplorer`: `Ligar(IChavesUsuario, string exe)`, `Desligar(IChavesUsuario)`, `Estado(IChavesUsuario, string exe)` e `ComandoDe(string exe, string marcador)`;
  - `EstadoIntegracao` (`Desligada`, `Ligada`, `OutroLocal`) e `IntegracaoExplorer.ExeRegistrado(IChavesUsuario)`;
  - `Abertura.Argumento` (`--abrir`), `Abertura.EhPedido(args, out alvo)` e `Abertura.Corrigir(string)`.

- [ ] **Passo 1: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class IntegracaoExplorerTestes
{
    private const string Exe = @"E:\Ferramentas\mapdisk.exe";

    [Fact]
    public void Ligar_grava_as_tres_chaves_no_usuario()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, Exe);
        foreach (var raiz in IntegracaoExplorer.Chaves)
        {
            Assert.StartsWith(@"Software\Classes\", raiz);
            Assert.Equal("Analisar com MapDisk", chaves.Ler(raiz, null));
            Assert.Equal(Exe + ",0", chaves.Ler(raiz, "Icon"));
            Assert.Contains("--abrir", chaves.Ler(raiz + @"\command", null));
        }

        Assert.Equal("\"" + Exe + "\" --abrir \"%V\"", chaves.Ler(@"Software\Classes\Directory\Background\shell\MapDisk\command", null));
        Assert.Equal("\"" + Exe + "\" --abrir \"%1\"", chaves.Ler(@"Software\Classes\Drive\shell\MapDisk\command", null));
    }

    [Fact]
    public void Desligar_tira_tudo()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, Exe);
        IntegracaoExplorer.Desligar(chaves);
        Assert.Empty(chaves.Todas);
        Assert.Equal(EstadoIntegracao.Desligada, IntegracaoExplorer.Estado(chaves, Exe));
    }

    [Fact]
    public void Estado_reconhece_outro_exe()
    {
        var chaves = new ChavesEmMemoria();
        IntegracaoExplorer.Ligar(chaves, @"F:\antigo\mapdisk.exe");
        Assert.Equal(EstadoIntegracao.OutroLocal, IntegracaoExplorer.Estado(chaves, Exe));
        Assert.Equal(@"F:\antigo\mapdisk.exe", IntegracaoExplorer.ExeRegistrado(chaves));
        IntegracaoExplorer.Ligar(chaves, Exe);
        Assert.Equal(EstadoIntegracao.Ligada, IntegracaoExplorer.Estado(chaves, Exe));
    }
}

public class AberturaTestes
{
    [Theory]
    [InlineData("C:\"", @"C:\")]
    [InlineData(@"D:\Dados", @"D:\Dados")]
    [InlineData(@"\\servidor\pasta", @"\\servidor\pasta")]
    public void Corrige_a_aspa_que_o_explorer_deixa(string recebido, string esperado) =>
        Assert.Equal(esperado, Abertura.Corrigir(recebido));

    [Fact]
    public void Reconhece_o_pedido()
    {
        Assert.True(Abertura.EhPedido(["--abrir", "C:\""], out var alvo));
        Assert.Equal(@"C:\", alvo);
        Assert.False(Abertura.EhPedido(["varrer", "C:"], out _));
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "IntegracaoExplorerTestes|AberturaTestes"`
Esperado: não compila.

- [ ] **Passo 3: criar `integracao-explorer.cs`**

```csharp
using Microsoft.Win32;

namespace MapDisk.Nucleo;

/// <summary>Chaves do registro do usuário atual. Os testes usam a versão em memória.</summary>
public interface IChavesUsuario
{
    void Gravar(string chave, string? nome, string valor);

    string? Ler(string chave, string? nome);

    void ApagarArvore(string chave);
}

/// <summary>HKCU de verdade. Não precisa de administrador (regra 4).</summary>
public sealed class ChavesUsuarioWindows : IChavesUsuario
{
    public void Gravar(string chave, string? nome, string valor)
    {
        using var k = Registry.CurrentUser.CreateSubKey(chave, writable: true);
        k.SetValue(nome ?? string.Empty, valor, RegistryValueKind.String);
    }

    public string? Ler(string chave, string? nome)
    {
        using var k = Registry.CurrentUser.OpenSubKey(chave);
        return k?.GetValue(nome ?? string.Empty) as string;
    }

    public void ApagarArvore(string chave) => Registry.CurrentUser.DeleteSubKeyTree(chave, throwOnMissingSubKey: false);
}

public sealed class ChavesEmMemoria : IChavesUsuario
{
    private readonly Dictionary<(string Chave, string Nome), string> _valores = [];

    public IReadOnlyCollection<string> Todas => _valores.Keys.Select(k => k.Chave).Distinct().ToList();

    public void Gravar(string chave, string? nome, string valor) => _valores[(chave.ToLowerInvariant(), nome ?? string.Empty)] = valor;

    public string? Ler(string chave, string? nome) =>
        _valores.TryGetValue((chave.ToLowerInvariant(), nome ?? string.Empty), out var v) ? v : null;

    public void ApagarArvore(string chave)
    {
        var prefixo = chave.ToLowerInvariant();
        foreach (var k in _valores.Keys.Where(k => k.Chave == prefixo || k.Chave.StartsWith(prefixo + "\\", StringComparison.Ordinal)).ToList())
        {
            _valores.Remove(k);
        }
    }
}

public enum EstadoIntegracao
{
    Desligada,
    Ligada,

    /// <summary>Ligada, mas apontando para outro .exe (o programa mudou de lugar).</summary>
    OutroLocal,
}

/// <summary>Item "Analisar com MapDisk" no menu clássico do Explorer (R19). Sai por completo ao desligar (regra 5).</summary>
public static class IntegracaoExplorer
{
    public const string Texto = "Analisar com MapDisk";

    public static readonly string[] Chaves =
    [
        @"Software\Classes\Directory\shell\MapDisk",
        @"Software\Classes\Drive\shell\MapDisk",
        @"Software\Classes\Directory\Background\shell\MapDisk",
    ];

    public static string ComandoDe(string exe, string marcador) => $"\"{exe}\" {Abertura.Argumento} \"{marcador}\"";

    public static void Ligar(IChavesUsuario chaves, string exe)
    {
        foreach (var chave in Chaves)
        {
            var marcador = chave.Contains(@"\Background\", StringComparison.Ordinal) ? "%V" : "%1";
            chaves.Gravar(chave, null, Texto);
            chaves.Gravar(chave, "Icon", exe + ",0");
            chaves.Gravar(chave + @"\command", null, ComandoDe(exe, marcador));
        }
    }

    public static void Desligar(IChavesUsuario chaves)
    {
        foreach (var chave in Chaves)
        {
            chaves.ApagarArvore(chave);
        }
    }

    /// <summary>O .exe que o item chama hoje, ou null quando desligado.</summary>
    public static string? ExeRegistrado(IChavesUsuario chaves)
    {
        var comando = chaves.Ler(Chaves[0] + @"\command", null);
        if (comando is null || !comando.StartsWith('"'))
        {
            return null;
        }

        var fim = comando.IndexOf('"', 1);
        return fim > 1 ? comando[1..fim] : null;
    }

    public static EstadoIntegracao Estado(IChavesUsuario chaves, string exe) => ExeRegistrado(chaves) switch
    {
        null => EstadoIntegracao.Desligada,
        var e when string.Equals(e, exe, StringComparison.OrdinalIgnoreCase) => EstadoIntegracao.Ligada,
        _ => EstadoIntegracao.OutroLocal,
    };
}
```

- [ ] **Passo 4: criar `abertura.cs`**

```csharp
namespace MapDisk.Nucleo;

/// <summary>Pedido do Explorer: abrir a janela já varrendo a pasta clicada.</summary>
public static class Abertura
{
    public const string Argumento = "--abrir";

    /// <summary>O Explorer manda a raiz da unidade como "C:\", e o Windows lê a barra antes da aspa como aspa escapada.</summary>
    public static string Corrigir(string alvo) => alvo.EndsWith('"') ? alvo[..^1] + "\\" : alvo;

    public static bool EhPedido(IReadOnlyList<string> args, out string? alvo)
    {
        alvo = args is [var a, var b] && string.Equals(a, Argumento, StringComparison.OrdinalIgnoreCase) ? Corrigir(b) : null;
        return alvo is not null;
    }
}
```

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes. Nenhum teste usa `ChavesUsuarioWindows`.

- [ ] **Passo 6: commit**

```bash
sh .superpowers/rascunho/commit.sh "Cria o item Analisar com MapDisk do Explorer" "R19: tres chaves em HKCU, sem administrador, que saem por completo ao desligar. O argumento --abrir corrige a aspa que o Explorer deixa na raiz da unidade." src/mapdisk.nucleo/integracao src/mapdisk.nucleo/sistema/abertura.cs testes/mapdisk.testes/integracao-explorer-testes.cs testes/mapdisk.testes/abertura-testes.cs
```

---

### Tarefa 4: linha de comando completa

**Arquivos:**
- Modificar: `src/mapdisk.nucleo/linha-de-comando/argumentos.cs`, `executor-cli.cs`, `src/mapdisk/modo-linha-de-comando.cs`
- Teste: `testes/mapdisk.testes/argumentos-testes.cs`, `executor-cli-testes.cs`

**Interfaces:**
- Consome: `RelatorioTecnico.Gravar`, `DadosRelatorio`, `IntegracaoExplorer`, `IChavesUsuario`.
- Produz:
  - `ComandoCli.Integrar` e `ComandoCli.RemoverIntegracao`;
  - `ArgumentosCli.Relatorio`;
  - um parâmetro novo em `ExecutorCli.Executar(..., IChavesUsuario? chaves = null, string? exe = null)`.

- [ ] **Passo 1: escrever os testes**

Em `argumentos-testes.cs`:

```csharp
    [Fact]
    public void Le_o_relatorio_e_exige_html()
    {
        Assert.Equal("d.html", ArgumentosCli.Interpretar(["varrer", "D:", "--relatorio", "d.html"]).Relatorio);
        Assert.Contains("O arquivo de --relatorio tem que terminar em .html.", ArgumentosCli.Interpretar(["varrer", "D:", "--relatorio", "d.txt"]).Erros);
        Assert.Contains("As opções --csv, --relatorio e --top pedem o comando varrer.", ArgumentosCli.Interpretar(["--relatorio", "d.html"]).Erros);
    }

    [Fact]
    public void Le_integrar_e_remover()
    {
        Assert.Equal(ComandoCli.Integrar, ArgumentosCli.Interpretar(["--integrar"]).Comando);
        Assert.Equal(ComandoCli.RemoverIntegracao, ArgumentosCli.Interpretar(["--remover-integracao"]).Comando);
        Assert.False(ArgumentosCli.Interpretar(["--integrar", "varrer", "C:"]).Valido);
    }

    [Fact]
    public void Ajuda_lista_todas_as_opcoes()
    {
        foreach (var opcao in new[] { "--relatorio", "--integrar", "--remover-integracao", "--csv", "--top" })
        {
            Assert.Contains(opcao, ArgumentosCli.TextoAjuda);
        }
    }
```

Em `executor-cli-testes.cs` (o `Rodar` passa a aceitar `chaves`):

```csharp
    [Fact]
    public void Grava_o_relatorio_pedido()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "cli-" + Guid.NewGuid().ToString("N") + ".html");
        var codigo = Rodar(Demonstracao.Motor(), out var saida, out _, "varrer", "C:", "--relatorio", arquivo);
        Assert.Equal(ExecutorCli.CodigoSucesso, codigo);
        Assert.Contains("Relatório gravado em", saida);
        Assert.Contains("Relatório de espaço em disco", File.ReadAllText(arquivo));
        File.Delete(arquivo);
    }

    [Fact]
    public void Integrar_e_remover_mexem_so_nas_chaves()
    {
        var chaves = new ChavesEmMemoria();
        var s = new StringWriter();
        Assert.Equal(ExecutorCli.CodigoSucesso, ExecutorCli.Executar(ArgumentosCli.Interpretar(["--integrar"]), Demonstracao.Motor(), s, new StringWriter(), CancellationToken.None, chaves, @"E:\mapdisk.exe"));
        Assert.Equal(EstadoIntegracao.Ligada, IntegracaoExplorer.Estado(chaves, @"E:\mapdisk.exe"));
        Assert.Contains("Mostrar mais opções", s.ToString());
        ExecutorCli.Executar(ArgumentosCli.Interpretar(["--remover-integracao"]), Demonstracao.Motor(), s, new StringWriter(), CancellationToken.None, chaves, @"E:\mapdisk.exe");
        Assert.Empty(chaves.Todas);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "ArgumentosTestes|ExecutorCliTestes"`
Esperado: não compila.

- [ ] **Passo 3: argumentos**

Em `argumentos.cs`:
- `ComandoCli` ganha `Integrar` e `RemoverIntegracao`.
- Propriedade `public string? Relatorio { get; private set; }`.
- No `switch`:

```csharp
                case "--integrar":
                    a.Comando = ComandoCli.Integrar;
                    comandos++;
                    break;
                case "--remover-integracao":
                    a.Comando = ComandoCli.RemoverIntegracao;
                    comandos++;
                    break;
                case "--relatorio":
                    a.Relatorio = Valor(args, ref i, arg, a.Erros);
                    break;
```

- No bloco `Varrer`, ao lado da conferência do `.csv`:

```csharp
            if (a.Relatorio is { } rel && !rel.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                a.Erros.Add("O arquivo de --relatorio tem que terminar em .html.");
            }
```

- No `else`, trocar a condição e a mensagem por `if (a.Csv != null || a.Relatorio != null || topDado)` e `"As opções --csv, --relatorio e --top pedem o comando varrer."`.
- Mensagem de comando repetido: `"Use só um comando por vez: varrer, --integrar, --remover-integracao, --ajuda ou --versao."`.
- `TextoAjuda`, as linhas novas. O texto final passa pela `humanizar-ptbr`:

```text
  mapdisk varrer <alvo> [opções]   varre e mostra os maiores itens
  mapdisk --integrar               põe "Analisar com MapDisk" no menu do Explorer
  mapdisk --remover-integracao     tira o item do menu do Explorer
  ...
Opções de varrer:
  --csv <arquivo.csv>         grava todas as pastas num arquivo CSV
  --relatorio <arquivo.html>  grava o relatório com a marca da MT e o gráfico
  --top <n>                   quantos itens mostrar e pôr em cada lista do
                              relatório, de 1 a 1000 (padrão: 10)

Exemplos:
  mapdisk varrer C:
  mapdisk varrer D:\ --relatorio d.html
  mapdisk varrer \\servidor\dados --csv dados.csv --top 30
```

- [ ] **Passo 4: executor**

Em `executor-cli.cs`, a assinatura fica `Executar(ArgumentosCli argumentos, IMotorVarredura motor, TextWriter saida, TextWriter erro, CancellationToken cancelar, IChavesUsuario? chaves = null, string? exe = null)`. No `switch` dos comandos:

```csharp
            case ComandoCli.Integrar:
                IntegracaoExplorer.Ligar(chaves ?? new ChavesUsuarioWindows(), exe ?? Environment.ProcessPath!);
                saida.WriteLine("Item \"Analisar com MapDisk\" ligado no menu das pastas e unidades do Explorer.");
                saida.WriteLine("No Windows 11, ele fica em \"Mostrar mais opções\". Se o mapdisk.exe mudar de lugar, rode --integrar de novo.");
                return CodigoSucesso;
            case ComandoCli.RemoverIntegracao:
                IntegracaoExplorer.Desligar(chaves ?? new ChavesUsuarioWindows());
                saida.WriteLine("Item \"Analisar com MapDisk\" tirado do menu do Explorer.");
                return CodigoSucesso;
```

Depois do bloco do CSV:

```csharp
        if (argumentos.Relatorio is { } relatorio)
        {
            try
            {
                RelatorioTecnico.Gravar(new DadosRelatorio(raiz, DateTime.Now, Environment.MachineName, r.Volume, r.Cancelada, argumentos.Top, null), relatorio);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                erro.WriteLine($"Não foi possível gravar o relatório: {e.Message}");
                return CodigoFalha;
            }

            saida.WriteLine();
            saida.WriteLine($"Relatório gravado em {Path.GetFullPath(relatorio)}");
        }
```

O `ModoLinhaDeComando` continua chamando sem os dois últimos argumentos, então os padrões do Windows valem.

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes. O teste que garante que a linha de comando não apaga nem move continua passando.

- [ ] **Passo 6: commit**

```bash
sh .superpowers/rascunho/commit.sh "Completa a linha de comando" "R18: --relatorio grava o HTML do tecnico, --integrar e --remover-integracao ligam e desligam o item do Explorer. A linha de comando segue sem apagar nem mover." src/mapdisk.nucleo/linha-de-comando testes/mapdisk.testes/argumentos-testes.cs testes/mapdisk.testes/executor-cli-testes.cs
```

---

### Tarefa 5: janela com Exportar, Opções e abertura pelo Explorer

**Arquivos:**
- Criar: `src/mapdisk/janela-opcoes.xaml` e `.cs`
- Modificar: `src/mapdisk/programa.cs`, `janela-principal.xaml` e `.cs`, `janela-relatorio.xaml.cs`
- Teste: `testes/mapdisk.testes/recursos-testes.cs`

**Interfaces:**
- Consome: tudo das tarefas 1 a 4.

- [ ] **Passo 1: escrever o teste**

```csharp
    [Fact]
    public void Janela_tem_exportar_e_opcoes()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Click=\"AoAbrirOpcoes\"", janela);
        Assert.Contains("Click=\"AoExportarHtml\"", janela);
        Assert.Contains("Click=\"AoExportarCsv\"", janela);
        var opcoes = File.ReadAllText(App("janela-opcoes.xaml"));
        foreach (var nome in new[] { "BotaoLigar", "BotaoDesligar", "CampoMaiores", "CampoMinimoDuplicados", "CampoPastas", "CampoArquivos", "CampoAnos", "CampoMinimo", "ListaAlvos", "TextoRegistro" })
        {
            Assert.Contains($"x:Name=\"{nome}\"", opcoes);
        }

        Assert.Contains("Abertura.EhPedido", File.ReadAllText(App("programa.cs")));
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter RecursosTestes`
Esperado: FALHA, porque não existe `AoAbrirOpcoes`.

- [ ] **Passo 3: `programa.cs`**

Ao lado do `Elevacao.EhPedido`:

```csharp
        var aberto = Abertura.EhPedido(args, out var alvoAberto);
        if (args.Length > 0 && !demonstracao && !elevado && !aberto)
```

Na montagem do painel:

```csharp
        if (elevado || aberto)
        {
            painel.TextoAlvo = (alvoElevado ?? alvoAberto)!;
        }

        return aplicativo.Run(new JanelaPrincipal(painel, administrador ? " (administrador)" : string.Empty, varrerAoAbrir: elevado || aberto));
```

- [ ] **Passo 4: janela principal**

- Na faixa do topo, antes do botão "Sobre":

```xml
<Button DockPanel.Dock="Right" Content="Opções" Style="{StaticResource BotaoFaixaContorno}" Click="AoAbrirOpcoes" Margin="0,0,8,0" />
```

- No cartão das pastas, entre "Relatório do cliente" e "Registro de ações". O "Registro de ações" sai do cartão e vai para as Opções, como diz a pendência da fatia 5:

```xml
<Button Content="Exportar" Style="{StaticResource BotaoContorno}" Click="AoAbrirMenuExportar" Margin="0,0,6,0"
        ToolTip="Grava o relatório da pasta das análises em HTML ou CSV">
    <Button.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Relatório HTML..." Click="AoExportarHtml" />
            <MenuItem Header="Planilha CSV..." Click="AoExportarCsv" />
        </ContextMenu>
    </Button.ContextMenu>
</Button>
```

- No `.cs`:
  - `AoAbrirMenuExportar` abre o `ContextMenu`, igual ao `AoAbrirMenuRelatorio`.
  - `AoExportarHtml` usa a pasta de `_painel.PastaDasAnalises(selecionada)`. Sem pasta, ou com varredura em andamento, avisa "Varra uma unidade ou pasta antes de exportar.". Abre um `SaveFileDialog` com o filtro `Página HTML (*.html)|*.html` e o nome `espaco-<nome da pasta sem caracteres inválidos>-<aaaammdd-hhmm>.html`. Grava com `RelatorioTecnico.Gravar(new DadosRelatorio(pasta, DateTime.Now, Environment.MachineName, _painel.Volume, _painel.Interrompida, _painel.Preferencias.MaioresArquivos, _painel.Analises.Duplicados.Resultado), arquivo)`. No fim, pergunta "Relatório gravado. Abrir agora?" e, com Sim, chama `Shell.AbrirNoNavegador(arquivo)`. Erro de gravação vira `MessageBox` com a mensagem.
  - `AoExportarCsv` faz o mesmo, com o filtro `CSV (*.csv)|*.csv`, `ExportadorCsv.Gravar(pasta, arquivo)` e "Mostrar no Explorer" no fim.
  - `AoAbrirOpcoes` abre `new JanelaOpcoes(_painel) { Owner = this }.ShowDialog()`. Na volta, força o recálculo das análises com `_raizVista = null`, para a nova quantidade de maiores arquivos valer na hora.
  - `AoAbrirRegistro` sai daqui e passa para a `JanelaOpcoes`, sem mudança no comportamento.
- Na criação do `PainelRelatorio`, `Criterios = _painel.Preferencias.Criterios`. A `JanelaRelatorio` recebe mais um parâmetro, `Action<CriteriosSugestao> aoSugerir`, e chama esse parâmetro no `AoSugerir` depois de validar. A janela principal passa `c => _painel.GravarPreferencias(_painel.Preferencias.ComCriterios(c))`.

- [ ] **Passo 5: `janela-opcoes.xaml`**

Janela modal, 560 de largura, altura pelo conteúdo, tema da MT (`Cartao`, `TituloCartao`, `BotaoContorno`, `BotaoFaixa`), com um `ScrollViewer` e cinco cartões em pilha:

1. **Menu do Explorer**:
   - `TextBlock x:Name="TextoExplorer"` com o estado: "Desligado.", "Ligado para este mapdisk.exe." ou "O item aponta para outro local: <caminho>. Clique em Ligar para apontar para este.";
   - botões `BotaoLigar` e `BotaoDesligar`;
   - texto fixo: "Põe "Analisar com MapDisk" no botão direito das pastas e unidades, só para o seu usuário. No Windows 11, fica em "Mostrar mais opções". Se o mapdisk.exe mudar de lugar, ligue de novo.".
2. **Análises**: `CampoMaiores` (Maiores arquivos, de 10 a 1000) e `CampoMinimoDuplicados` (Duplicados a partir de, em MB).
3. **Relatório para o cliente, critérios do Sugerir**: `CampoPastas`, `CampoArquivos`, `CampoAnos` e `CampoMinimo`, com os mesmos rótulos da janela do relatório.
4. **Últimos alvos**: `ListBox x:Name="ListaAlvos"` com `_painel.UltimosUsados`, e os botões "Esquecer" (só com um item selecionado) e "Limpar a lista".
5. **Registro de ações**: `TextBlock x:Name="TextoRegistro"` com `_painel.LocalDoRegistro`, e o botão "Abrir", com o mesmo código que saiu da janela principal.

Rodapé com "Salvar" (`IsDefault`) e "Cancelar" (`IsCancel`).

- [ ] **Passo 6: `janela-opcoes.xaml.cs`**

```csharp
using System.Globalization;
using System.IO;
using System.Windows;
using MapDisk.Nucleo;

namespace MapDisk;

public partial class JanelaOpcoes : Window
{
    private readonly PainelPrincipal _painel;
    private readonly IChavesUsuario _chaves = new ChavesUsuarioWindows();
    private readonly string _exe = Environment.ProcessPath!;

    public JanelaOpcoes(PainelPrincipal painel)
    {
        _painel = painel;
        InitializeComponent();
        var p = painel.Preferencias;
        CampoMaiores.Text = N(p.MaioresArquivos);
        CampoMinimoDuplicados.Text = N(p.DuplicadosMinimoMb);
        CampoPastas.Text = N(p.SugerirPastas);
        CampoArquivos.Text = N(p.SugerirArquivos);
        CampoAnos.Text = N(p.SugerirAnos);
        CampoMinimo.Text = N(p.SugerirMinimoMb);
        TextoRegistro.Text = painel.LocalDoRegistro;
        MostrarExplorer();
        MostrarAlvos();
    }

    private void MostrarExplorer()
    {
        var estado = IntegracaoExplorer.Estado(_chaves, _exe);
        TextoExplorer.Text = estado switch
        {
            EstadoIntegracao.Ligada => "Ligado para este mapdisk.exe.",
            EstadoIntegracao.OutroLocal => $"O item aponta para outro local: {IntegracaoExplorer.ExeRegistrado(_chaves)}. Clique em Ligar para apontar para este.",
            _ => "Desligado.",
        };
        BotaoDesligar.IsEnabled = estado != EstadoIntegracao.Desligada;
    }

    private void MostrarAlvos() => ListaAlvos.ItemsSource = _painel.UltimosUsados;

    private void AoLigar(object sender, RoutedEventArgs e)
    {
        IntegracaoExplorer.Ligar(_chaves, _exe);
        MostrarExplorer();
    }

    private void AoDesligar(object sender, RoutedEventArgs e)
    {
        IntegracaoExplorer.Desligar(_chaves);
        MostrarExplorer();
    }

    private void AoEsquecer(object sender, RoutedEventArgs e)
    {
        if (ListaAlvos.SelectedItem is string alvo)
        {
            _painel.EsquecerAlvo(alvo);
            MostrarAlvos();
        }
    }

    private void AoLimpar(object sender, RoutedEventArgs e)
    {
        _painel.LimparAlvos();
        MostrarAlvos();
    }

    private void AoAbrirRegistro(object sender, RoutedEventArgs e)
    {
        if (File.Exists(_painel.LocalDoRegistro))
        {
            Shell.MostrarNoExplorer(_painel.LocalDoRegistro, ehArquivo: true);
        }
        else
        {
            MessageBox.Show(this, $"Nenhuma ação registrada ainda. O registro fica em {_painel.LocalDoRegistro}.", "MapDisk - MT",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AoSalvar(object sender, RoutedEventArgs e)
    {
        var campos = new (string Nome, string Texto)[]
        {
            ("maiores-arquivos", CampoMaiores.Text),
            ("duplicados-minimo-mb", CampoMinimoDuplicados.Text),
            ("sugerir-pastas", CampoPastas.Text),
            ("sugerir-arquivos", CampoArquivos.Text),
            ("sugerir-anos", CampoAnos.Text),
            ("sugerir-minimo-mb", CampoMinimo.Text),
        };
        if (!campos.All(c => Preferencias.Valido(c.Nome, c.Texto)))
        {
            MessageBox.Show(this, "Algum número está fora da faixa. Maiores arquivos vai de 10 a 1000, duplicados a partir de 1 MB e anos sem alteração a partir de 1.",
                "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _painel.GravarPreferencias(Preferencias.DeTexto(campos.Select(c => $"{c.Nome}={c.Texto}")));
        DialogResult = true;
    }

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);
}
```

- [ ] **Passo 7: rodar build e testes**

Rodar: `dotnet build mapdisk.sln -c Release` e `dotnet test mapdisk.sln -c Release`
Esperado: sem aviso e todos verdes.

- [ ] **Passo 8: conferir na tela, em demonstração**

- Gerar o `.exe` com `ferramentas\publicar.cmd` e abrir com `--demonstracao`.
- Exportar > Relatório HTML: gravar em `.superpowers/rascunho/` e abrir. Conferir o logo, o gráfico, as listas e "sem acesso" na `System Volume Information`. Apagar o arquivo depois.
- Exportar > Planilha CSV: gravar em `.superpowers/rascunho/` e abrir como texto. Apagar depois.
- Opções: mudar "Maiores arquivos" para 10, salvar e ver a aba "Maiores arquivos" com 10 linhas. Voltar para 100.
- Opções > Ligar e Desligar o Explorer **não** é testado pelo agente, porque grava no registro do Windows do Manfred. Fica no "Como testar" do PR.
- A linha de comando com `--relatorio` é testada pelo agente numa pasta de teste dentro de `.superpowers/rascunho/`, com saída em `.superpowers/rascunho/`.

- [ ] **Passo 9: commit**

```bash
sh .superpowers/rascunho/commit.sh "Liga exportar, opcoes e a abertura pelo Explorer na janela" "O tecnico exporta HTML e CSV da pasta das analises, liga o item do Explorer e guarda as opcoes numa tela so. O --abrir abre a janela ja varrendo." src/mapdisk testes/mapdisk.testes/recursos-testes.cs
```

---

### Tarefa 6: página do programa em `public/`

**Arquivos:**
- Criar: `public/index.html`, `public/imagens/mt-logo.png` (cópia de `src/mapdisk/recursos/mt-logo.png`), `public/imagens/mapdisk.png` (ícone em 256 px, gerado do `.ico` pelo `ferramentas/gerar-icone.py`, ou tirado da arte em `docs/marca/`), `public/imagens/janela.png` e `public/imagens/relatorio.png` (telas do modo `--demonstracao`)
- Teste: `testes/mapdisk.testes/pagina-testes.cs`

- [ ] **Passo 1: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class PaginaTestes
{
    private static string Publico(string relativo) =>
        Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "public", relativo);

    [Fact]
    public void Pagina_nao_carrega_nada_de_fora()
    {
        var html = File.ReadAllText(Publico("index.html"));
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("src=\"http", html);
        Assert.DoesNotContain("<link rel=\"stylesheet\"", html);
    }

    [Fact]
    public void Pagina_tem_download_licenca_e_imagens()
    {
        var html = File.ReadAllText(Publico("index.html"));
        Assert.Contains("https://github.com/manfredjr/mapdisk/releases/latest", html);
        Assert.Contains("GPL-3.0", html);
        Assert.Contains("https://www.manfred.com.br", html);
        foreach (var imagem in new[] { "mt-logo.png", "mapdisk.png", "janela.png", "relatorio.png" })
        {
            Assert.Contains($"imagens/{imagem}", html);
            Assert.True(File.Exists(Publico(Path.Combine("imagens", imagem))), imagem);
        }
    }

    [Fact]
    public void Pagina_nao_usa_marca_de_terceiro()
    {
        Assert.DoesNotContain("treesize", File.ReadAllText(Publico("index.html")), StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter PaginaTestes`
Esperado: FALHA, porque `public/index.html` não existe.

- [ ] **Passo 3: escrever os textos**

Rascunho em `.superpowers/rascunho/pagina-textos.md`, com estas seções:
1. **Topo:** logo do MapDisk, nome, frase curta ("Descubra o que ocupa o disco e libere espaço com segurança") e botão "Baixar para Windows" (link da última versão).
2. **O que faz:** varredura, árvore, gráfico, análises, duplicados, ações seguras, relatório para o cliente, relatório do técnico, linha de comando.
3. **Segurança:**
   - nada sai da máquina;
   - não pede administrador;
   - não instala;
   - toda ação passa por confirmação e fica registrada.
4. **Requisitos:** Windows 10 e 11, Windows Server 2016 ou mais novo, 64 bits.
5. **Licença e garantia:** GPL-3.0, código no GitHub, sem garantia, nos termos da licença.
6. **Privacidade:** o programa não coleta nem envia dados, e a página não usa cookies nem rastreadores.
7. **Rodapé:** MT - Manfred Tecnologia, link para www.manfred.com.br.

Os textos passam pela `humanizar-ptbr`, e as seções 3, 5 e 6 pela `legal-br`. A `legal-br` usa como base a seção 5 de `docs/legal/verificacao-distribuicao-e-lgpd-2026-09-28.md` e os textos da janela Sobre (`src/mapdisk.nucleo/sobre/sobre.cs`). Nenhum número, nome ou data sem fonte. O que faltar vira `[PREENCHER]`, e a página não vai para o PR com `[PREENCHER]`.

- [ ] **Passo 4: montar `public/index.html`**

- HTML único, `lang="pt-BR"`, `meta viewport`, estilo no `<style>`, nas cores da MT (`#006B2D`, `#0F8F2F`, `#43A92C`, `#9AD52B`, `#202020`, `#F4F4F4`).
- Fonte do sistema (`Segoe UI, Arial, sans-serif`), para não carregar fonte de fora.
- Largura máxima de 1100 px. No celular, com até 600 px, as colunas viram uma.
- Imagens com `alt` e `width`/`height`.
- Sem script.

- [ ] **Passo 5: imagens**

- Copiar o logo da MT.
- Gerar o ícone de 256 px.
- Capturar a janela em `--demonstracao` com o roteiro de captura que já está em `.superpowers/rascunho/`, em 1280 por 800, na pasta `C:\`.
- Capturar o relatório HTML da demonstração aberto no navegador interno.
- Conferir em cada imagem que só aparecem nomes da demonstração.

- [ ] **Passo 6: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: todos verdes, inclusive caracteres proibidos (o teste já lê `.html`) e nome de arquivo em minúsculas.

- [ ] **Passo 7: conferir a página**

Abrir `public/index.html` no navegador interno, em largura de computador e de celular (375 px), e conferir que nada sai da tela.

- [ ] **Passo 8: commit**

```bash
sh .superpowers/rascunho/commit.sh "Cria a pagina do programa" "Pagina estatica para mapdisk.manfred.com.br, sem script e sem nada de fora, com telas do modo demonstracao. Textos revisados pela humanizacao e pela verificacao juridica. A publicacao e outro passo, com autorizacao." public testes/mapdisk.testes/pagina-testes.cs
```

---

### Tarefa 7: documentação e fechamento

**Arquivos:**
- Modificar: `README.md`, `docs/superpowers/pendencias.md`

- [ ] **Passo 1: README**

- Seção **Uso**:
  - Exportar (HTML e CSV);
  - Opções, com o que cada uma guarda e onde (`%LOCALAPPDATA%\MapDisk\opcoes.txt`);
  - item do Explorer, com a nota do Windows 11;
  - linha de comando com `--relatorio`, `--integrar` e `--remover-integracao`, num bloco `bat`.
- Linha da fatia 8 com "código em [#N] | Aguardando o teste do Manfred".

- [ ] **Passo 2: pendências**

- Fechar com "Fechado: entrou na fatia 8, PR #N":
  - maiores arquivos configurável (fatia 3);
  - imagem do gráfico no relatório (fatia 4);
  - registro de ações nas Opções (fatia 5);
  - critérios do "Sugerir" (fatia 6);
  - mínimo dos duplicados (fatia 7);
  - esquecer um alvo (fatia 2).
- Nova seção "Fatia 8":

| Item | Motivo | O que fecha |
|---|---|---|
| Pastas excluídas da varredura (spec, seção 6) | Fica para depois da versão 1, por decisão do Manfred em 01/10/2026. Mexe no motor e em todas as contas | Uma fatia própria depois da versão 1.0.0 |
| Item no menu compacto do Windows 11 | Exige empacotamento MSIX e assinatura (spec, seção 13) | Fora da versão 1 |
| Publicação da página | Publicar pede autorização. O roteiro do cPanel fica fora da pasta do projeto | Passo da versão 1.0.0, com autorização |
| Teste do item do Explorer | Grava no registro do Windows do Manfred | Teste do Manfred no PR da fatia 8 |

- [ ] **Passo 3: portões e commit**

Rodar os quatro portões do `AGENTS.md` e depois:

```bash
sh .superpowers/rascunho/commit.sh "Traz a documentacao da fatia 8" "README com exportar, opcoes, Explorer e a linha de comando completa. Pendencias das fatias 2 a 7 fechadas e as da fatia 8 abertas." README.md docs/superpowers/pendencias.md
```

- [ ] **Passo 4: Pull Request**

Abrir o PR "Fatia 8: relatório do técnico, linha de comando, Explorer, Opções e página", com "O que muda", "Como testar" e a linha de autores. Para o Manfred testar:
1. Opções > Ligar. No Explorer, botão direito numa pasta, numa unidade e no fundo de uma pasta: "Analisar com MapDisk" abre a janela já varrendo. Mover o `.exe` para outra pasta e abrir Opções: aparece "aponta para outro local". Desligar: o item some.
2. Exportar > Relatório HTML numa pasta real: abrir, imprimir em PDF pelo navegador.
3. No Prompt de Comando:

```bat
start /wait mapdisk varrer C:\Users --relatorio c:\temp\users.html
```

4. Opções: mudar os números, fechar e abrir o programa de novo, e conferir que ficaram.
5. Abrir `public\index.html` no navegador.

---

## Conferência do plano contra o desenho

| Requisito | Tarefa |
|---|---|
| R11, quantidade de maiores arquivos configurável | 1 e 5 |
| R17, HTML com a marca da MT e CSV | 2 e 5 (o CSV pela janela; pela linha de comando já existia) |
| R18, linha de comando completa | 4 |
| R19, item do Explorer sem administrador, ligado e desligado em Opções | 3, 4 e 5 |
| Seção 6, Opções | 1 e 5. As pastas excluídas da varredura ficam para depois da versão 1, por decisão do Manfred |
| Página `public/` | 6 |
| Pendências das fatias 2 a 7 que apontam para a 8 | 1, 2 e 5 |
