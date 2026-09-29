# Plano da fatia 1: varredura, árvore e janela básica

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada passo tem caixa de seleção (`- [ ]`). Cada tarefa termina com teste verde e commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** o MapDisk varre uma unidade, pasta ou caminho de rede e mostra, numa janela com a identidade da MT, a árvore de pastas com tamanho, espaço alocado, arquivos, pastas, porcentagem e data, sem nunca mostrar 0 onde não houve leitura. A linha de comando varre e grava CSV.

**Arquitetura:** tudo que tem regra fica no núcleo (`src/mapdisk.nucleo`, sem WPF) e é testado: formatação, alvos, volumes, modelo da árvore, leitor de pasta pelo Win32, motor paralelo, árvore visível, painel da janela, linha de comando e CSV. O aplicativo (`src/mapdisk`) só liga a janela WPF ao painel e o console ao executor da linha de comando.

**Tecnologia:** C# com .NET 8 (`net8.0-windows`), WPF, xUnit, P/Invoke por `LibraryImport` para `CreateFileW`, `GetFileInformationByHandleEx`, `GetDiskFreeSpaceExW`, `GetDiskFreeSpaceW` e `GetVolumeInformationW`.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`. Requisitos desta fatia: R1 (sem a lista de últimos alvos), R2 (sem atualizar só uma ramificação), R3, R4, R6, R7 (sem o botão de administrador), R8 (sem "Liberado nesta sessão") e R18 parcial (varrer e CSV).

## Restrições globais

- Regras do produto 1 a 9 do `AGENTS.md`. Nesta fatia pesam a 2 (linha de comando só lê), a 3 (nunca 0 onde não houve leitura), a 4 (sem administrador), a 6 (sem internet), a 7 (teto de paralelismo, sem ler conteúdo, sem baixar arquivo da nuvem) e a 9 (hard link uma vez, junção não seguida, alocado do sistema de arquivos).
- Nenhum código apaga, move ou altera arquivo do usuário. Esta fatia só lê.
- Testes só dentro da pasta de saída dos testes (`testes/mapdisk.testes/bin/...`). Nenhum teste lê, move ou apaga fora dela.
- Tudo em português, sem os caracteres proibidos, nome de arquivo em minúsculas.
- `TreatWarningsAsErrors` ligado. Build sem aviso.
- Commit com verbo na 3ª pessoa, título sem acento, corpo com o porquê, fim `Autores: Manfred Heil Junior`. Mensagem em `.superpowers/rascunho/msg-commit.txt`, com `git commit -F`.
- O agente só lê e grava dentro de `C:\COWORK\CODE\MAPDISK-MT`. A cópia dos arquivos do MapNet listados na tarefa 1 e na tarefa 10 foi autorizada pelo Manfred em 29/09/2026.

## Git desta fatia

1. O plano entra pelo ramo `fatia-1-varredura`, num Pull Request só do plano. O código só começa depois que o Manfred aprovar e juntar esse PR.
2. O código sai do `main` atualizado, no ramo `varredura`, e entra num segundo Pull Request, com "O que muda", "Como testar" e a linha de autores.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `global.json`, `Directory.Build.props`, `mapdisk.sln` | SDK, metadados e solução |
| `src/mapdisk.nucleo/mapdisk.nucleo.csproj` | Biblioteca `net8.0-windows` |
| `src/mapdisk.nucleo/formatacao/formatador.cs` | Tamanho, porcentagem, número, data, duração e plural em pt-BR |
| `src/mapdisk.nucleo/alvos/alvo.cs` | Normalizar o alvo digitado, reconhecer rede, prefixo de caminho longo |
| `src/mapdisk.nucleo/unidades/volumes.cs` | Unidades, espaço livre e total, sistema de arquivos, cluster |
| `src/mapdisk.nucleo/arvore/no-pasta.cs` | Nó de pasta, arquivo, estados e soma dos totais para cima |
| `src/mapdisk.nucleo/varredura/leitor-pasta.cs` | Leitura de uma pasta pelo Win32 |
| `src/mapdisk.nucleo/varredura/motor-varredura.cs` | Varredura em paralelo, resultado e interface do motor |
| `src/mapdisk.nucleo/painel/linha-arvore.cs` | Uma linha da árvore na tela, com os textos |
| `src/mapdisk.nucleo/painel/arvore-visivel.cs` | Lista de linhas visíveis, abrir, fechar, ordenar, modo e unidade |
| `src/mapdisk.nucleo/painel/painel-principal.cs` | Estado da janela, comandos e barra de status |
| `src/mapdisk.nucleo/painel/demonstracao.cs` | Dados de exemplo para `--demonstracao` |
| `src/mapdisk.nucleo/linha-de-comando/argumentos.cs` | Leitura dos argumentos |
| `src/mapdisk.nucleo/linha-de-comando/executor-cli.cs` | Execução da linha de comando, testável |
| `src/mapdisk.nucleo/relatorios/exportador-csv.cs` | CSV das pastas |
| `src/mapdisk/*` | Aplicativo WPF: `programa.cs`, `modo-linha-de-comando.cs`, janela, tema, fontes, manifesto |
| `testes/mapdisk.testes/*` | Testes do núcleo e de convenção |
| `ferramentas/publicar.cmd`, `.github/workflows/testes.yml` | Gerar o `.exe` e CI |

---

### Tarefa 1: esqueleto do repositório e testes de convenção

**Arquivos:**
- Criar: `global.json`, `Directory.Build.props`, `mapdisk.sln`, `src/mapdisk.nucleo/mapdisk.nucleo.csproj`, `testes/mapdisk.testes/mapdisk.testes.csproj`, `testes/mapdisk.testes/caracteres-proibidos-testes.cs`, `README.md`

**Interfaces:**
- Produz: solução `mapdisk.sln` com o núcleo e os testes; `CaracteresProibidosTestes.RaizDoRepositorio()` (usado por outros testes de convenção).

- [ ] **Passo 1: criar o ramo do código**

```bash
git switch main
git pull
git switch -c varredura
```

- [ ] **Passo 2: `global.json`** (igual ao MapNet)

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Passo 3: `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Version>0.1.0</Version>
    <Authors>Manfred Heil Junior</Authors>
    <Company>MT - Manfred Tecnologia</Company>
    <Product>MapDisk - MT</Product>
    <Copyright>Copyright (c) 2026 MANFRED TECNOLOGIA LTDA</Copyright>
    <NeutralLanguage>pt-BR</NeutralLanguage>
    <SatelliteResourceLanguages>pt-BR</SatelliteResourceLanguages>
  </PropertyGroup>

  <!-- Na versao de entrega nao sai arquivo .pdb ao lado do .exe -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
  </PropertyGroup>
</Project>
```

- [ ] **Passo 4: `src/mapdisk.nucleo/mapdisk.nucleo.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <AssemblyName>mapdisk.nucleo</AssemblyName>
    <RootNamespace>MapDisk.Nucleo</RootNamespace>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Description>Nucleo do MapDisk - MT: varredura, arvore, formatacao e relatorios.</Description>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="mapdisk.testes" />
  </ItemGroup>

</Project>
```

- [ ] **Passo 5: `testes/mapdisk.testes/mapdisk.testes.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <AssemblyName>mapdisk.testes</AssemblyName>
    <RootNamespace>MapDisk.Testes</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="MapDisk.Nucleo" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\mapdisk.nucleo\mapdisk.nucleo.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Passo 6: solução**

```bash
dotnet new sln -n mapdisk
dotnet sln mapdisk.sln add src/mapdisk.nucleo/mapdisk.nucleo.csproj testes/mapdisk.testes/mapdisk.testes.csproj
```

- [ ] **Passo 7: teste de convenção, copiado do MapNet (cópia autorizada)**

```bash
sed -e 's/MapNet/MapDisk/g' -e 's/mapnet\.sln/mapdisk.sln/g' ../MAPNET-MT/testes/mapnet.testes/caracteres-proibidos-testes.cs > testes/mapdisk.testes/caracteres-proibidos-testes.cs
```

Conferir que o arquivo tem `namespace MapDisk.Testes;` e procura `mapdisk.sln` em `RaizDoRepositorio()`.

- [ ] **Passo 8: `README.md`**

```markdown
# MapDisk - MT

Analisador de espaço em disco para Windows, da MT - Manfred Tecnologia. Mostra quais pastas e arquivos ocupam mais espaço numa unidade, numa pasta ou num compartilhamento de rede. Nas próximas versões, também envia para a Lixeira e move, com confirmação e registro.

Software livre, sob licença [GPL-3.0](LICENSE).

As regras do projeto estão no [`AGENTS.md`](AGENTS.md) e o desenho em [`docs/superpowers/specs/2026-09-28-mapdisk-design.md`](docs/superpowers/specs/2026-09-28-mapdisk-design.md).

## Situação do projeto

| Fatia | Conteúdo | Ramo | Pull Request | Situação |
|---|---|---|---|---|
| 1 | Varredura, árvore com colunas, modos e unidades, barra de status, linha de comando com CSV | `varredura` | [PREENCHER] | Em andamento |
```

- [ ] **Passo 9: rodar os testes**

Rodar: `dotnet test mapdisk.sln -c Release`
Esperado: 2 testes verdes (`Codigo_e_documentacao_nao_tem_caractere_proibido`, `Nome_de_arquivo_e_minusculo`).

- [ ] **Passo 10: commit**

```bash
git add global.json Directory.Build.props mapdisk.sln src testes README.md
git commit -F .superpowers/rascunho/msg-commit.txt
```

Mensagem: `Cria a estrutura da solucao e os testes de convencao`.

---

### Tarefa 2: formatação em pt-BR

**Arquivos:**
- Criar: `src/mapdisk.nucleo/formatacao/formatador.cs`
- Teste: `testes/mapdisk.testes/formatador-testes.cs`

**Interfaces:**
- Produz: `enum UnidadeExibicao { Automatica, GB, MB, KB }`; `static class Formatador` com `CultureInfo PtBr`, `string Tamanho(long bytes, UnidadeExibicao unidade = UnidadeExibicao.Automatica)`, `string Porcentagem(double fracao)`, `string Numero(long n)`, `string Data(DateTime d)`, `string Duracao(TimeSpan t)`, `string Plural(long n, string um, string varios)`.

- [ ] **Passo 1: teste que falha**

```csharp
namespace MapDisk.Testes;

public class FormatadorTestes
{
    [Theory]
    [InlineData(0L, "0 Bytes")]
    [InlineData(1L, "1 Byte")]
    [InlineData(1023L, "1.023 Bytes")]
    [InlineData(1024L, "1,0 KB")]
    [InlineData(307_125_862L, "292,9 MB")]
    [InlineData(56_585_533_849L, "52,7 GB")]
    [InlineData(2_199_023_255_552L, "2,0 TB")]
    public void Tamanho_automatico_escolhe_a_unidade(long bytes, string esperado)
    {
        Assert.Equal(esperado, Formatador.Tamanho(bytes));
    }

    [Fact]
    public void Tamanho_em_unidade_fixa()
    {
        Assert.Equal("0,5 GB", Formatador.Tamanho(512L * 1024 * 1024, UnidadeExibicao.GB));
        Assert.Equal("1.536,0 MB", Formatador.Tamanho(1536L * 1024 * 1024, UnidadeExibicao.MB));
        Assert.Equal("4,0 KB", Formatador.Tamanho(4096, UnidadeExibicao.KB));
    }

    [Fact]
    public void Porcentagem_numero_data_duracao_e_plural()
    {
        Assert.Equal("84,1 %", Formatador.Porcentagem(0.841));
        Assert.Equal("100,0 %", Formatador.Porcentagem(1));
        Assert.Equal("1.158.005", Formatador.Numero(1_158_005));
        Assert.Equal("25/09/2026", Formatador.Data(new DateTime(2026, 9, 25, 14, 30, 0)));
        Assert.Equal("", Formatador.Data(DateTime.MinValue));
        Assert.Equal("12,4 s", Formatador.Duracao(TimeSpan.FromSeconds(12.4)));
        Assert.Equal("2 min 5 s", Formatador.Duracao(TimeSpan.FromSeconds(125)));
        Assert.Equal("1 pasta", Formatador.Plural(1, "pasta", "pastas"));
        Assert.Equal("1.200 pastas", Formatador.Plural(1200, "pasta", "pastas"));
        Assert.Equal("0 pastas", Formatador.Plural(0, "pasta", "pastas"));
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter FormatadorTestes`
Esperado: erro de compilação, `Formatador` não existe.

- [ ] **Passo 3: implementação**

```csharp
using System.Globalization;

namespace MapDisk.Nucleo;

/// <summary>Unidade escolhida na tela para os tamanhos.</summary>
public enum UnidadeExibicao
{
    Automatica,
    GB,
    MB,
    KB,
}

/// <summary>Números, tamanhos e datas no formato brasileiro ("52,7 GB", "1.158.005").</summary>
public static class Formatador
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private const double Kb = 1024d;
    private const double Mb = Kb * 1024;
    private const double Gb = Mb * 1024;
    private const double Tb = Gb * 1024;

    public static string Tamanho(long bytes, UnidadeExibicao unidade = UnidadeExibicao.Automatica) => unidade switch
    {
        UnidadeExibicao.GB => Com(bytes / Gb, "GB"),
        UnidadeExibicao.MB => Com(bytes / Mb, "MB"),
        UnidadeExibicao.KB => Com(bytes / Kb, "KB"),
        _ => bytes switch
        {
            1 => "1 Byte",
            < 1024 => $"{Numero(bytes)} Bytes",
            < 1024L * 1024 => Com(bytes / Kb, "KB"),
            < 1024L * 1024 * 1024 => Com(bytes / Mb, "MB"),
            < 1024L * 1024 * 1024 * 1024 => Com(bytes / Gb, "GB"),
            _ => Com(bytes / Tb, "TB"),
        },
    };

    public static string Porcentagem(double fracao) => Com(fracao * 100, "%");

    public static string Numero(long n) => n.ToString("N0", PtBr);

    public static string Data(DateTime d) => d == DateTime.MinValue ? string.Empty : d.ToString("dd/MM/yyyy", PtBr);

    public static string Duracao(TimeSpan t) => t.TotalSeconds < 60
        ? $"{t.TotalSeconds.ToString("0.0", PtBr)} s"
        : $"{(int)t.TotalMinutes} min {t.Seconds} s";

    public static string Plural(long n, string um, string varios) => n == 1 ? $"1 {um}" : $"{Numero(n)} {varios}";

    private static string Com(double valor, string sufixo) => $"{valor.ToString("#,##0.0", PtBr)} {sufixo}";
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter FormatadorTestes`
Esperado: 9 testes verdes.

- [ ] **Passo 5: commit** com a mensagem `Cria a formatacao de tamanhos e numeros em pt-BR`.

---

### Tarefa 3: alvos e volumes

**Arquivos:**
- Criar: `src/mapdisk.nucleo/alvos/alvo.cs`, `src/mapdisk.nucleo/unidades/volumes.cs`
- Teste: `testes/mapdisk.testes/alvo-testes.cs`, `testes/mapdisk.testes/volumes-testes.cs`

**Interfaces:**
- Consome: `Formatador.Tamanho`.
- Produz: `static class Alvo` com `string? Normalizar(string? texto, out string? erro)`, `bool EhRede(string caminho)`, `string Longo(string caminho)`, `string Juntar(string pasta, string nome)`. `sealed record InfoVolume(string Raiz, string Rotulo, string SistemaArquivos, long Livre, long Total, long Cluster)` com `string Descricao`. `static partial class Volumes` com `IReadOnlyList<InfoVolume> ListarUnidades()`, `InfoVolume? Ler(string caminho)`, `string RaizDe(string caminho)`.

- [ ] **Passo 1: testes que falham**

`alvo-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class AlvoTestes
{
    [Theory]
    [InlineData("c:", @"C:\")]
    [InlineData(@" ""D:\Dados\"" ", @"D:\Dados")]
    [InlineData("D:/Dados/Fotos/", @"D:\Dados\Fotos")]
    [InlineData(@"\\srv\dados", @"\\srv\dados\")]
    [InlineData(@"\\srv\dados\pasta\", @"\\srv\dados\pasta")]
    [InlineData(@"\\?\C:\Dados", @"C:\Dados")]
    [InlineData(@"\\?\UNC\srv\dados\x", @"\\srv\dados\x")]
    public void Normaliza_o_alvo(string texto, string esperado)
    {
        Assert.Equal(esperado, Alvo.Normalizar(texto, out var erro));
        Assert.Null(erro);
    }

    [Theory]
    [InlineData("", "Informe")]
    [InlineData("   ", "Informe")]
    [InlineData(@"\\srv", "incompleto")]
    [InlineData("Dados", "caminho completo")]
    [InlineData("C:Dados", "caminho completo")]
    public void Recusa_alvo_invalido_explicando(string texto, string trecho)
    {
        Assert.Null(Alvo.Normalizar(texto, out var erro));
        Assert.Contains(trecho, erro);
    }

    [Fact]
    public void Prefixo_longo_e_juncao_de_caminhos()
    {
        Assert.Equal(@"\\?\C:\a", Alvo.Longo(@"C:\a"));
        Assert.Equal(@"\\?\UNC\srv\d\", Alvo.Longo(@"\\srv\d\"));
        Assert.Equal(@"\\?\C:\a", Alvo.Longo(@"\\?\C:\a"));
        Assert.Equal(@"C:\Users", Alvo.Juntar(@"C:\", "Users"));
        Assert.Equal(@"C:\Users\Ana", Alvo.Juntar(@"C:\Users", "Ana"));
        Assert.True(Alvo.EhRede(@"\\srv\d\"));
        Assert.False(Alvo.EhRede(@"C:\"));
    }
}
```

`volumes-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class VolumesTestes
{
    [Fact]
    public void Le_o_volume_da_pasta_dos_testes()
    {
        var volume = Volumes.Ler(AppContext.BaseDirectory);

        Assert.NotNull(volume);
        Assert.True(volume.Total > 0);
        Assert.InRange(volume.Livre, 0, volume.Total);
        Assert.True(volume.Cluster > 0 && (volume.Cluster & (volume.Cluster - 1)) == 0, "cluster deve ser potência de 2");
        Assert.False(string.IsNullOrEmpty(volume.SistemaArquivos));
    }

    [Fact]
    public void Lista_a_unidade_dos_testes()
    {
        var raiz = Volumes.RaizDe(AppContext.BaseDirectory);
        Assert.Contains(Volumes.ListarUnidades(), v => string.Equals(v.Raiz, raiz, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Raiz_de_caminho_local_e_de_rede()
    {
        Assert.Equal(@"C:\", Volumes.RaizDe(@"C:\Users\Ana"));
        Assert.Equal(@"\\srv\dados\", Volumes.RaizDe(@"\\srv\dados\a\b"));
    }

    [Fact]
    public void Descricao_da_unidade()
    {
        var volume = new InfoVolume(@"C:\", "Sistema", "NTFS", 373L << 30, 952L << 30, 4096);
        Assert.Equal(@"C:\  Sistema  373,0 GB livres de 952,0 GB", volume.Descricao);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "AlvoTestes|VolumesTestes"`
Esperado: erro de compilação.

- [ ] **Passo 3: `alvo.cs`**

```csharp
namespace MapDisk.Nucleo;

/// <summary>O que o técnico digita ou escolhe para varrer: unidade, pasta ou caminho de rede.</summary>
public static class Alvo
{
    /// <summary>
    /// Deixa o alvo num formato só: tira espaços e aspas, troca "/" por "\", aceita "C:" como
    /// "C:\" e caminho com prefixo \\?\. Raiz de unidade e de compartilhamento terminam em "\";
    /// pasta, não. Devolve null com o motivo em erro quando o alvo não serve.
    /// </summary>
    public static string? Normalizar(string? texto, out string? erro)
    {
        erro = null;
        var t = texto?.Trim().Trim('"').Trim().Replace('/', '\\');
        if (string.IsNullOrEmpty(t))
        {
            erro = "Informe uma unidade, pasta ou caminho de rede.";
            return null;
        }

        if (t.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            t = @"\\" + t[8..];
        }
        else if (t.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            t = t[4..];
        }

        if (t.Length == 2 && char.IsAsciiLetter(t[0]) && t[1] == ':')
        {
            t += "\\";
        }

        if (EhRede(t))
        {
            var partes = t[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2)
            {
                erro = @"Caminho de rede incompleto. Use \\servidor\compartilhamento.";
                return null;
            }

            return @"\\" + string.Join('\\', partes) + (partes.Length == 2 ? "\\" : string.Empty);
        }

        if (!Path.IsPathFullyQualified(t))
        {
            erro = $@"Use um caminho completo, como C:\ ou D:\Dados. Recebido: {t}";
            return null;
        }

        var cheio = Path.GetFullPath(t);
        var raiz = Path.GetPathRoot(cheio)!;
        return cheio.Length > raiz.Length ? cheio.TrimEnd('\\') : raiz.ToUpperInvariant();
    }

    public static bool EhRede(string caminho) => caminho.StartsWith(@"\\", StringComparison.Ordinal)
        && !caminho.StartsWith(@"\\?\", StringComparison.Ordinal);

    /// <summary>Caminho com o prefixo \\?\, para as APIs do Win32 aceitarem mais de 260 caracteres.</summary>
    public static string Longo(string caminho) =>
        caminho.StartsWith(@"\\?\", StringComparison.Ordinal) ? caminho
        : EhRede(caminho) ? @"\\?\UNC\" + caminho[2..]
        : @"\\?\" + caminho;

    public static string Juntar(string pasta, string nome) => pasta.EndsWith('\\') ? pasta + nome : pasta + "\\" + nome;
}
```

- [ ] **Passo 4: `volumes.cs`**

```csharp
using System.Runtime.InteropServices;

namespace MapDisk.Nucleo;

/// <summary>Uma unidade ou compartilhamento, com o espaço lido na hora.</summary>
public sealed record InfoVolume(string Raiz, string Rotulo, string SistemaArquivos, long Livre, long Total, long Cluster)
{
    /// <summary>Texto da lista de unidades da janela.</summary>
    public string Descricao => $"{Raiz}  {Rotulo}  {Formatador.Tamanho(Livre)} livres de {Formatador.Tamanho(Total)}";
}

public static partial class Volumes
{
    /// <summary>Unidades prontas (fixas, removíveis e de rede mapeadas), na ordem da letra.</summary>
    public static IReadOnlyList<InfoVolume> ListarUnidades() => DriveInfo.GetDrives()
        .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Network && d.IsReady)
        .Select(d => Ler(d.RootDirectory.FullName))
        .OfType<InfoVolume>()
        .ToList();

    /// <summary>
    /// Espaço, sistema de arquivos e cluster do volume onde está o caminho, que pode ser de
    /// unidade ou de compartilhamento. Null quando o volume não responde.
    /// </summary>
    public static unsafe InfoVolume? Ler(string caminho)
    {
        var raiz = RaizDe(caminho);
        if (!GetDiskFreeSpaceEx(raiz, out var livre, out var total, out _))
        {
            return null;
        }

        var rotulo = stackalloc char[261];
        var sistema = stackalloc char[261];
        var temInfo = GetVolumeInformation(raiz, rotulo, 261, out _, out _, out _, sistema, 261);
        var cluster = GetDiskFreeSpace(raiz, out var setoresPorCluster, out var bytesPorSetor, out _, out _)
            ? (long)setoresPorCluster * bytesPorSetor
            : 0;
        return new InfoVolume(
            raiz,
            temInfo ? new string(rotulo) : string.Empty,
            temInfo ? new string(sistema) : string.Empty,
            (long)livre,
            (long)total,
            cluster);
    }

    /// <summary>"C:\" para caminho local, "\\servidor\compartilhamento\" para caminho de rede.</summary>
    public static string RaizDe(string caminho)
    {
        if (Alvo.EhRede(caminho))
        {
            var partes = caminho[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);
            return $@"\\{partes[0]}\{partes[1]}\";
        }

        return Path.GetPathRoot(caminho)!.ToUpperInvariant();
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetDiskFreeSpaceEx(string pasta, out ulong livreParaUsuario, out ulong total, out ulong livreTotal);

    [LibraryImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetDiskFreeSpace(string raiz, out uint setoresPorCluster, out uint bytesPorSetor, out uint clustersLivres, out uint clustersTotais);

    [LibraryImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetVolumeInformation(string raiz, char* rotulo, int tamanhoRotulo, out uint serie, out uint maiorNome, out uint opcoes, char* sistema, int tamanhoSistema);
}
```

Nota: `new string(char*)` lê até o primeiro caractere nulo, que o Windows grava no fim do texto.

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "AlvoTestes|VolumesTestes"`
Esperado: 17 testes verdes.

- [ ] **Passo 6: commit** com a mensagem `Cria a leitura do alvo e das unidades`.

---

### Tarefa 4: modelo da árvore

**Arquivos:**
- Criar: `src/mapdisk.nucleo/arvore/no-pasta.cs`
- Teste: `testes/mapdisk.testes/no-pasta-testes.cs`

**Interfaces:**
- Consome: `Alvo.Juntar`.
- Produz: `enum EstadoPasta { Pendente, Lida, SemAcesso, ErroLeitura, Link }`; `[Flags] enum MarcaArquivo { Nenhuma = 0, Sistema = 1, NaNuvem = 2, Link = 4, LinkRepetido = 8 }`; `readonly record struct ArquivoInfo(string Nome, long Tamanho, long Alocado, DateTime Modificacao, MarcaArquivo Marcas)` com `bool Soma`; `sealed class NoPasta` com construtor `(string nome, NoPasta? pai, DateTime modificacao = default)`, propriedades `Nome`, `Pai`, `ModificacaoPropria`, `Estado`, `Motivo`, `DestinoLink`, `Subpastas`, `Arquivos`, `TamanhoProprio`, `AlocadoProprio`, `Tamanho`, `Alocado`, `ArquivosTotal`, `PastasTotal`, `PastasSemAcesso`, `PastasComErro`, `UltimaModificacao`, `Nivel`, e métodos `CaminhoCompleto()`, `Preencher(ArquivoInfo[] arquivos, NoPasta[] subpastas)`, `MarcarSemAcesso(string motivo)`, `MarcarErro(string motivo)`, `MarcarLink(string? destino)`.

- [ ] **Passo 1: testes que falham**

```csharp
namespace MapDisk.Testes;

public class NoPastaTestes
{
    private static readonly DateTime _d = new(2026, 9, 1);

    private static ArquivoInfo Arq(string nome, long tamanho, long alocado, MarcaArquivo marcas = MarcaArquivo.Nenhuma, DateTime? data = null) =>
        new(nome, tamanho, alocado, data ?? _d, marcas);

    [Fact]
    public void Preencher_soma_na_pasta_e_em_todas_acima()
    {
        var raiz = new NoPasta(@"C:\", null);
        var a = new NoPasta("a", raiz);
        var b = new NoPasta("b", a);
        raiz.Preencher([], [a]);
        a.Preencher([Arq("1.bin", 100, 4096)], [b]);
        b.Preencher([Arq("2.bin", 10, 4096), Arq("3.bin", 20, 4096)], []);

        Assert.Equal(130, raiz.Tamanho);
        Assert.Equal(12288, raiz.Alocado);
        Assert.Equal(3, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.PastasTotal);
        Assert.Equal(1, a.PastasTotal);
        Assert.Equal(30, b.TamanhoProprio);
        Assert.Equal(100, a.TamanhoProprio);
        Assert.Equal(EstadoPasta.Lida, b.Estado);
    }

    [Fact]
    public void Pasta_sem_acesso_fica_marcada_e_contada()
    {
        var raiz = new NoPasta(@"C:\", null);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([], [windows]);
        windows.MarcarSemAcesso("acesso negado");

        Assert.Equal(EstadoPasta.SemAcesso, windows.Estado);
        Assert.Equal("acesso negado", windows.Motivo);
        Assert.Equal(1, raiz.PastasSemAcesso);
        Assert.Equal(0, raiz.PastasComErro);
    }

    [Fact]
    public void Pasta_com_erro_e_contada_separada()
    {
        var raiz = new NoPasta(@"\\srv\d\", null);
        var x = new NoPasta("x", raiz);
        raiz.Preencher([], [x]);
        x.MarcarErro("rede caiu");

        Assert.Equal(EstadoPasta.ErroLeitura, x.Estado);
        Assert.Equal(1, raiz.PastasComErro);
    }

    [Fact]
    public void Hard_link_repetido_aparece_mas_nao_soma()
    {
        var raiz = new NoPasta(@"C:\", null);
        raiz.Preencher([Arq("x.bin", 4096, 4096), Arq("y.bin", 4096, 4096, MarcaArquivo.LinkRepetido)], []);

        Assert.Equal(4096, raiz.Tamanho);
        Assert.Equal(2, raiz.ArquivosTotal);
        Assert.Equal(2, raiz.Arquivos.Count);
    }

    [Fact]
    public void Link_nao_soma_e_guarda_o_destino()
    {
        var raiz = new NoPasta(@"C:\", null);
        var link = new NoPasta("Documents and Settings", raiz);
        link.MarcarLink(@"C:\Users");
        raiz.Preencher([], [link]);

        Assert.Equal(EstadoPasta.Link, link.Estado);
        Assert.Equal(@"C:\Users", link.DestinoLink);
        Assert.Equal(0, raiz.Tamanho);
    }

    [Fact]
    public void Ultima_modificacao_e_a_mais_nova_da_subarvore()
    {
        var raiz = new NoPasta(@"C:\", null, new DateTime(2026, 1, 1));
        var a = new NoPasta("a", raiz, new DateTime(2026, 2, 1));
        raiz.Preencher([Arq("velho.txt", 1, 1, data: new DateTime(2025, 1, 1))], [a]);
        a.Preencher([Arq("novo.txt", 1, 1, data: new DateTime(2026, 9, 20))], []);

        Assert.Equal(new DateTime(2026, 9, 20), raiz.UltimaModificacao);
    }

    [Fact]
    public void Caminho_completo_e_nivel()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var ana = new NoPasta("Ana", users);
        var rede = new NoPasta(@"\\srv\d\", null);
        var x = new NoPasta("x", rede);

        Assert.Equal(@"C:\Users\Ana", ana.CaminhoCompleto());
        Assert.Equal(@"C:\", raiz.CaminhoCompleto());
        Assert.Equal(@"\\srv\d\x", x.CaminhoCompleto());
        Assert.Equal(2, ana.Nivel);
    }

    [Fact]
    public void Soma_certa_com_muitas_tarefas_ao_mesmo_tempo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var filhas = Enumerable.Range(0, 2000).Select(i => new NoPasta($"p{i}", raiz)).ToArray();
        raiz.Preencher([], filhas);

        Parallel.ForEach(filhas, f => f.Preencher([Arq("a", 3, 4096), Arq("b", 7, 4096)], []));

        Assert.Equal(20_000, raiz.Tamanho);
        Assert.Equal(4000, raiz.ArquivosTotal);
        Assert.Equal(2000, raiz.PastasTotal);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter NoPastaTestes`
Esperado: erro de compilação.

- [ ] **Passo 3: implementação**

```csharp
namespace MapDisk.Nucleo;

public enum EstadoPasta
{
    /// <summary>Ainda não lida. Durante a varredura, e depois dela se foi interrompida.</summary>
    Pendente,
    Lida,
    SemAcesso,
    ErroLeitura,

    /// <summary>Junção ou link simbólico. Não é seguido e não soma.</summary>
    Link,
}

[Flags]
public enum MarcaArquivo
{
    Nenhuma = 0,
    Sistema = 1,
    NaNuvem = 2,
    Link = 4,

    /// <summary>Hard link cujo conteúdo já foi contado em outro lugar do volume.</summary>
    LinkRepetido = 8,
}

/// <summary>Um arquivo lido na varredura. Guarda só o que a tela e as análises usam.</summary>
public readonly record struct ArquivoInfo(string Nome, long Tamanho, long Alocado, DateTime Modificacao, MarcaArquivo Marcas)
{
    /// <summary>Entra nas somas. O hard link repetido aparece na lista, mas não soma de novo.</summary>
    public bool Soma => (Marcas & MarcaArquivo.LinkRepetido) == 0;
}

/// <summary>
/// Uma pasta da varredura. Os totais incluem tudo que está abaixo dela e são atualizados
/// enquanto a varredura anda: quando uma pasta é lida, o que ela tem é somado nela e em todas
/// as pastas acima. Assim a tela mostra números parciais sem esperar o fim.
/// </summary>
public sealed class NoPasta
{
    private static readonly NoPasta[] _semSubpastas = [];
    private static readonly ArquivoInfo[] _semArquivos = [];

    private NoPasta[] _subpastas = _semSubpastas;
    private ArquivoInfo[] _arquivos = _semArquivos;
    private volatile EstadoPasta _estado;
    private long _tamanho;
    private long _alocado;
    private long _arquivosTotal;
    private long _pastasTotal;
    private long _semAcesso;
    private long _comErro;
    private long _modificacao;

    public NoPasta(string nome, NoPasta? pai, DateTime modificacao = default)
    {
        Nome = nome;
        Pai = pai;
        ModificacaoPropria = modificacao;
    }

    /// <summary>Nome da pasta. Na raiz, o caminho completo do alvo.</summary>
    public string Nome { get; }

    public NoPasta? Pai { get; }

    public DateTime ModificacaoPropria { get; }

    public EstadoPasta Estado => _estado;

    public string? Motivo { get; private set; }

    public string? DestinoLink { get; private set; }

    public IReadOnlyList<NoPasta> Subpastas => Volatile.Read(ref _subpastas);

    public IReadOnlyList<ArquivoInfo> Arquivos => Volatile.Read(ref _arquivos);

    /// <summary>Soma dos arquivos que estão direto nesta pasta.</summary>
    public long TamanhoProprio { get; private set; }

    public long AlocadoProprio { get; private set; }

    public long Tamanho => Interlocked.Read(ref _tamanho);

    public long Alocado => Interlocked.Read(ref _alocado);

    public long ArquivosTotal => Interlocked.Read(ref _arquivosTotal);

    /// <summary>Pastas abaixo desta, em todos os níveis.</summary>
    public long PastasTotal => Interlocked.Read(ref _pastasTotal);

    /// <summary>Pastas sem permissão de leitura nesta subárvore, contando esta.</summary>
    public long PastasSemAcesso => Interlocked.Read(ref _semAcesso);

    public long PastasComErro => Interlocked.Read(ref _comErro);

    public DateTime UltimaModificacao => new(Interlocked.Read(ref _modificacao));

    public int Nivel
    {
        get
        {
            var nivel = 0;
            for (var p = Pai; p != null; p = p.Pai)
            {
                nivel++;
            }

            return nivel;
        }
    }

    public string CaminhoCompleto()
    {
        var partes = new Stack<string>();
        var no = this;
        while (no.Pai != null)
        {
            partes.Push(no.Nome);
            no = no.Pai;
        }

        var caminho = no.Nome;
        foreach (var parte in partes)
        {
            caminho = Alvo.Juntar(caminho, parte);
        }

        return caminho;
    }

    /// <summary>Grava o que foi lido e soma aqui e acima. Chamado uma vez, pela tarefa que leu a pasta.</summary>
    public void Preencher(ArquivoInfo[] arquivos, NoPasta[] subpastas)
    {
        long tamanho = 0;
        long alocado = 0;
        var recente = ModificacaoPropria.Ticks;
        foreach (var a in arquivos)
        {
            if (a.Soma)
            {
                tamanho += a.Tamanho;
                alocado += a.Alocado;
            }

            recente = Math.Max(recente, a.Modificacao.Ticks);
        }

        TamanhoProprio = tamanho;
        AlocadoProprio = alocado;
        Volatile.Write(ref _arquivos, arquivos);
        Volatile.Write(ref _subpastas, subpastas);
        _estado = EstadoPasta.Lida;
        Somar(tamanho, alocado, arquivos.Length, subpastas.Length, 0, 0, recente);
    }

    public void MarcarSemAcesso(string motivo)
    {
        Motivo = motivo;
        _estado = EstadoPasta.SemAcesso;
        Somar(0, 0, 0, 0, 1, 0, ModificacaoPropria.Ticks);
    }

    public void MarcarErro(string motivo)
    {
        Motivo = motivo;
        _estado = EstadoPasta.ErroLeitura;
        Somar(0, 0, 0, 0, 0, 1, ModificacaoPropria.Ticks);
    }

    public void MarcarLink(string? destino)
    {
        DestinoLink = destino;
        _estado = EstadoPasta.Link;
        Somar(0, 0, 0, 0, 0, 0, ModificacaoPropria.Ticks);
    }

    private void Somar(long tamanho, long alocado, long arquivos, long pastas, long semAcesso, long comErro, long modificacao)
    {
        for (var no = this; no != null; no = no.Pai)
        {
            Interlocked.Add(ref no._tamanho, tamanho);
            Interlocked.Add(ref no._alocado, alocado);
            Interlocked.Add(ref no._arquivosTotal, arquivos);
            Interlocked.Add(ref no._pastasTotal, pastas);
            Interlocked.Add(ref no._semAcesso, semAcesso);
            Interlocked.Add(ref no._comErro, comErro);

            long atual;
            while (modificacao > (atual = Interlocked.Read(ref no._modificacao))
                && Interlocked.CompareExchange(ref no._modificacao, modificacao, atual) != atual)
            {
            }
        }
    }
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter NoPastaTestes`
Esperado: 8 testes verdes.

- [ ] **Passo 5: commit** com a mensagem `Cria o modelo da arvore com totais ao vivo`.

---

### Tarefa 5: leitor de pasta pelo Win32

**Arquivos:**
- Criar: `src/mapdisk.nucleo/varredura/leitor-pasta.cs`, `testes/mapdisk.testes/apoio/pasta-teste.cs`
- Teste: `testes/mapdisk.testes/leitor-pasta-testes.cs`

**Interfaces:**
- Consome: `Alvo.Longo`, `ArquivoInfo`, `MarcaArquivo`.
- Produz: `enum ResultadoLeitura { Lida, SemAcesso, Erro }`; `readonly record struct EntradaPasta(string Nome, DateTime Modificacao, bool EhLink)`; `internal static partial class LeitorPasta` com `ResultadoLeitura Ler(string caminho, bool raizDoVolume, Func<long, bool> primeiraVez, List<ArquivoInfo> arquivos, List<EntradaPasta> subpastas, out string? motivo)`. Nos testes, `internal sealed class PastaTeste : IDisposable` com `Raiz`, `Caminho`, `Pasta`, `Arquivo`, `Juncao`, `HardLink`, `NegarLeitura`.

**Por que Win32 direto:** `GetFileInformationByHandleEx` com `FileIdBothDirectoryInfo` devolve, numa chamada por bloco de 64 KB, nome, tamanho, espaço alocado, atributos, etiqueta de reparse, data e identificador de cada entrada, sem abrir os arquivos (regra 7). O espaço alocado vem do próprio sistema de arquivos (regra 9) e o identificador permite contar o hard link uma vez. Onde a classe com identificador não existe (FAT, exFAT, alguns compartilhamentos), o leitor usa `FileFullDirectoryInfo`, sem identificador.

- [ ] **Passo 1: apoio dos testes, `apoio/pasta-teste.cs`**

```csharp
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MapDisk.Testes;

/// <summary>
/// Árvore de teste dentro da pasta de saída dos testes. Nunca toca fora dela. No fim, tira as
/// regras de acesso negado que o teste criou e apaga tudo.
/// </summary>
internal sealed class PastaTeste : IDisposable
{
    private readonly List<string> _negadas = [];

    public PastaTeste()
    {
        Raiz = Path.Combine(AppContext.BaseDirectory, "arvores-teste", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Raiz);
    }

    public string Raiz { get; }

    public string Caminho(string relativo) => Path.Combine(Raiz, relativo);

    public string Pasta(string relativo)
    {
        var caminho = Caminho(relativo);
        Directory.CreateDirectory(caminho);
        return caminho;
    }

    public string Arquivo(string relativo, int bytes, DateTime? modificacao = null)
    {
        var caminho = Caminho(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllBytes(caminho, new byte[bytes]);
        if (modificacao is { } data)
        {
            File.SetLastWriteTime(caminho, data);
        }

        return caminho;
    }

    public string Juncao(string relativo, string destinoRelativo)
    {
        var caminho = Caminho(relativo);
        Rodar($"mklink /J \"{caminho}\" \"{Caminho(destinoRelativo)}\"");
        return caminho;
    }

    public string HardLink(string relativo, string existenteRelativo)
    {
        var caminho = Caminho(relativo);
        Rodar($"mklink /H \"{caminho}\" \"{Caminho(existenteRelativo)}\"");
        return caminho;
    }

    public string NegarLeitura(string relativo)
    {
        var caminho = Pasta(relativo);
        var info = new DirectoryInfo(caminho);
        var regras = info.GetAccessControl();
        regras.AddAccessRule(Negacao());
        info.SetAccessControl(regras);
        _negadas.Add(caminho);
        return caminho;
    }

    public void Dispose()
    {
        foreach (var caminho in _negadas)
        {
            var info = new DirectoryInfo(caminho);
            var regras = info.GetAccessControl();
            regras.RemoveAccessRule(Negacao());
            info.SetAccessControl(regras);
        }

        try
        {
            Directory.Delete(@"\\?\" + Raiz, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static FileSystemAccessRule Negacao() =>
        new(WindowsIdentity.GetCurrent().User!, FileSystemRights.ListDirectory, AccessControlType.Deny);

    private static void Rodar(string comando)
    {
        using var processo = Process.Start(new ProcessStartInfo("cmd.exe", "/c " + comando)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        processo.WaitForExit();
        if (processo.ExitCode != 0)
        {
            throw new InvalidOperationException($"Falhou: {comando}{Environment.NewLine}{processo.StandardError.ReadToEnd()}");
        }
    }
}
```

O `Directory.Delete` recursivo do .NET apaga a junção sem entrar nela, então o destino da junção nunca é apagado por esse caminho. O `mklink /J` e o `mklink /H` rodam sem administrador. Se o build acusar que `GetAccessControl` não existe, acrescentar ao projeto de testes `<PackageReference Include="System.IO.FileSystem.AccessControl" Version="5.0.0" />` (pacote previsto neste plano).

- [ ] **Passo 2: testes que falham, `leitor-pasta-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class LeitorPastaTestes
{
    private static ResultadoLeitura Ler(string caminho, out List<ArquivoInfo> arquivos, out List<EntradaPasta> subpastas, out string? motivo, Func<long, bool>? primeiraVez = null, bool raizDoVolume = false)
    {
        arquivos = [];
        subpastas = [];
        return LeitorPasta.Ler(caminho, raizDoVolume, primeiraVez ?? (_ => true), arquivos, subpastas, out motivo);
    }

    [Fact]
    public void Le_arquivos_com_tamanho_e_subpastas()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 1000, new DateTime(2026, 3, 4, 5, 6, 0));
        t.Pasta("sub");

        Assert.Equal(ResultadoLeitura.Lida, Ler(t.Raiz, out var arquivos, out var subpastas, out _));

        var a = Assert.Single(arquivos);
        Assert.Equal("a.bin", a.Nome);
        Assert.Equal(1000, a.Tamanho);
        Assert.Equal(new DateTime(2026, 3, 4, 5, 6, 0), a.Modificacao);
        var sub = Assert.Single(subpastas);
        Assert.Equal("sub", sub.Nome);
        Assert.False(sub.EhLink);
    }

    [Fact]
    public void Alocado_vem_do_sistema_de_arquivos_em_clusters()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 5000);
        var cluster = Volumes.Ler(t.Raiz)!.Cluster;

        Ler(t.Raiz, out var arquivos, out _, out _);

        var alocado = Assert.Single(arquivos).Alocado;
        Assert.True(alocado >= 5000, $"alocado {alocado}");
        Assert.Equal(0, alocado % cluster);
    }

    [Fact]
    public void Juncao_vem_como_link()
    {
        using var t = new PastaTeste();
        t.Pasta("dados");
        t.Juncao("atalho", "dados");

        Ler(t.Raiz, out _, out var subpastas, out _);

        Assert.True(subpastas.Single(s => s.Nome == "atalho").EhLink);
        Assert.False(subpastas.Single(s => s.Nome == "dados").EhLink);
    }

    [Fact]
    public void Hard_link_repetido_e_marcado()
    {
        using var t = new PastaTeste();
        t.Arquivo("x.bin", 4096);
        t.HardLink("y.bin", "x.bin");
        var vistos = new HashSet<long>();

        Ler(t.Raiz, out var arquivos, out _, out _, vistos.Add);

        Assert.Equal(2, arquivos.Count);
        Assert.Single(arquivos, a => a.Marcas.HasFlag(MarcaArquivo.LinkRepetido));
    }

    [Fact]
    public void Pasta_negada_vem_sem_acesso()
    {
        using var t = new PastaTeste();
        var negada = t.NegarLeitura("fechada");

        Assert.Equal(ResultadoLeitura.SemAcesso, Ler(negada, out _, out _, out var motivo));
        Assert.Equal("acesso negado", motivo);
    }

    [Fact]
    public void Pasta_que_nao_existe_vem_com_erro()
    {
        using var t = new PastaTeste();

        Assert.Equal(ResultadoLeitura.Erro, Ler(t.Caminho("nao-existe"), out _, out _, out var motivo));
        Assert.Equal("pasta não encontrada", motivo);
    }

    [Fact]
    public void Caminho_longo_e_lido()
    {
        using var t = new PastaTeste();
        var relativo = Path.Combine(Enumerable.Repeat(new string('p', 60), 5).ToArray());
        t.Arquivo(Path.Combine(relativo, "fundo.bin"), 10);
        var caminho = t.Caminho(relativo);
        Assert.True(caminho.Length > 260);

        Assert.Equal(ResultadoLeitura.Lida, Ler(caminho, out var arquivos, out _, out _));
        Assert.Equal(10, Assert.Single(arquivos).Tamanho);
    }

    [Fact]
    public void Arquivo_de_sistema_so_na_raiz_do_volume()
    {
        using var t = new PastaTeste();
        t.Arquivo("pagefile.sys", 10);

        Ler(t.Raiz, out var naPasta, out _, out _);
        Ler(t.Raiz, out var naRaiz, out _, out _, raizDoVolume: true);

        Assert.False(Assert.Single(naPasta).Marcas.HasFlag(MarcaArquivo.Sistema));
        Assert.True(Assert.Single(naRaiz).Marcas.HasFlag(MarcaArquivo.Sistema));
    }
}
```

- [ ] **Passo 3: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter LeitorPastaTestes`
Esperado: erro de compilação, `LeitorPasta` não existe.

- [ ] **Passo 4: implementação, `leitor-pasta.cs`**

```csharp
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MapDisk.Nucleo;

public enum ResultadoLeitura
{
    Lida,
    SemAcesso,
    Erro,
}

/// <summary>Subpasta encontrada na leitura de uma pasta.</summary>
public readonly record struct EntradaPasta(string Nome, DateTime Modificacao, bool EhLink);

/// <summary>
/// Lê uma pasta pelo GetFileInformationByHandleEx. Cada chamada devolve um bloco de entradas
/// com nome, tamanho, espaço alocado, atributos, etiqueta de reparse, data e identificador,
/// sem abrir os arquivos. Só lê: nada aqui altera a pasta.
/// </summary>
internal static partial class LeitorPasta
{
    private const uint ListarPasta = 0x0001;
    private const uint CompartilharTudo = 0x0007;
    private const uint AbrirExistente = 3;
    private const uint SemanticaDeBackup = 0x02000000;

    private const int ClasseComIdRecomecar = 11;
    private const int ClasseComId = 10;
    private const int ClasseCompletaRecomecar = 15;
    private const int ClasseCompleta = 14;

    private const int ErroFuncaoInvalida = 1;
    private const int ErroArquivoNaoEncontrado = 2;
    private const int ErroCaminhoNaoEncontrado = 3;
    private const int ErroAcessoNegado = 5;
    private const int ErroSemMaisArquivos = 18;
    private const int ErroNaoSuportado = 50;
    private const int ErroParametroInvalido = 87;

    private const uint AtributoPasta = 0x10;
    private const uint AtributoReparse = 0x400;
    private const uint AtributoOffline = 0x1000;
    private const uint AtributoRecuperarAoAbrir = 0x40000;
    private const uint AtributoRecuperarAoLer = 0x400000;

    private const uint EtiquetaJuncao = 0xA0000003;
    private const uint EtiquetaLinkSimbolico = 0xA000000C;

    // Posições dentro de FILE_ID_BOTH_DIR_INFO e FILE_FULL_DIR_INFO, iguais até o campo EaSize.
    private const int PosProxima = 0;
    private const int PosEscrita = 24;
    private const int PosTamanho = 40;
    private const int PosAlocado = 48;
    private const int PosAtributos = 56;
    private const int PosTamanhoNome = 60;
    private const int PosEtiqueta = 64;
    private const int PosIdComId = 96;
    private const int PosNomeComId = 104;
    private const int PosNomeCompleta = 68;

    private static readonly string[] _arquivosDoSistema = ["pagefile.sys", "hiberfil.sys", "swapfile.sys"];

    public static unsafe ResultadoLeitura Ler(
        string caminho,
        bool raizDoVolume,
        Func<long, bool> primeiraVez,
        List<ArquivoInfo> arquivos,
        List<EntradaPasta> subpastas,
        out string? motivo)
    {
        motivo = null;
        using var pasta = CreateFile(Alvo.Longo(caminho), ListarPasta, CompartilharTudo, 0, AbrirExistente, SemanticaDeBackup, 0);
        if (pasta.IsInvalid)
        {
            return Falha(Marshal.GetLastPInvokeError(), out motivo);
        }

        var buffer = new byte[64 * 1024];
        var comId = true;
        var primeira = true;
        fixed (byte* p = buffer)
        {
            while (true)
            {
                var classe = comId
                    ? (primeira ? ClasseComIdRecomecar : ClasseComId)
                    : (primeira ? ClasseCompletaRecomecar : ClasseCompleta);
                if (!GetFileInformationByHandleEx(pasta, classe, p, (uint)buffer.Length))
                {
                    var erro = Marshal.GetLastPInvokeError();
                    if (erro == ErroSemMaisArquivos)
                    {
                        return ResultadoLeitura.Lida;
                    }

                    if (comId && primeira && erro is ErroParametroInvalido or ErroNaoSuportado or ErroFuncaoInvalida)
                    {
                        comId = false;
                        continue;
                    }

                    arquivos.Clear();
                    subpastas.Clear();
                    return Falha(erro, out motivo);
                }

                primeira = false;
                Interpretar(buffer, comId, raizDoVolume, primeiraVez, arquivos, subpastas);
            }
        }
    }

    private static void Interpretar(
        ReadOnlySpan<byte> buffer,
        bool comId,
        bool raizDoVolume,
        Func<long, bool> primeiraVez,
        List<ArquivoInfo> arquivos,
        List<EntradaPasta> subpastas)
    {
        var posNome = comId ? PosNomeComId : PosNomeCompleta;
        var inicio = 0;
        while (true)
        {
            var entrada = buffer[inicio..];
            var proxima = MemoryMarshal.Read<int>(entrada[PosProxima..]);
            var tamanhoNome = MemoryMarshal.Read<int>(entrada[PosTamanhoNome..]);
            var nome = new string(MemoryMarshal.Cast<byte, char>(entrada.Slice(posNome, tamanhoNome)));
            if (nome is not ("." or ".."))
            {
                var escrita = Data(MemoryMarshal.Read<long>(entrada[PosEscrita..]));
                var atributos = MemoryMarshal.Read<uint>(entrada[PosAtributos..]);
                var etiqueta = (atributos & AtributoReparse) != 0 ? MemoryMarshal.Read<uint>(entrada[PosEtiqueta..]) : 0;
                var ehLink = etiqueta is EtiquetaJuncao or EtiquetaLinkSimbolico;
                if ((atributos & AtributoPasta) != 0)
                {
                    subpastas.Add(new EntradaPasta(nome, escrita, ehLink));
                }
                else
                {
                    var marcas = MarcaArquivo.Nenhuma;
                    if (ehLink)
                    {
                        marcas |= MarcaArquivo.Link;
                    }

                    if ((atributos & (AtributoOffline | AtributoRecuperarAoAbrir | AtributoRecuperarAoLer)) != 0)
                    {
                        marcas |= MarcaArquivo.NaNuvem;
                    }

                    if (raizDoVolume && _arquivosDoSistema.Contains(nome, StringComparer.OrdinalIgnoreCase))
                    {
                        marcas |= MarcaArquivo.Sistema;
                    }

                    var id = comId ? MemoryMarshal.Read<long>(entrada[PosIdComId..]) : 0;
                    if (id != 0 && !primeiraVez(id))
                    {
                        marcas |= MarcaArquivo.LinkRepetido;
                    }

                    arquivos.Add(new ArquivoInfo(
                        nome,
                        MemoryMarshal.Read<long>(entrada[PosTamanho..]),
                        MemoryMarshal.Read<long>(entrada[PosAlocado..]),
                        escrita,
                        marcas));
                }
            }

            if (proxima == 0)
            {
                return;
            }

            inicio += proxima;
        }
    }

    private static DateTime Data(long tempoDeArquivo) => tempoDeArquivo is > 0 and < 2_650_000_000_000_000_000
        ? DateTime.FromFileTime(tempoDeArquivo)
        : DateTime.MinValue;

    private static ResultadoLeitura Falha(int erro, out string? motivo)
    {
        if (erro == ErroAcessoNegado)
        {
            motivo = "acesso negado";
            return ResultadoLeitura.SemAcesso;
        }

        motivo = erro is ErroArquivoNaoEncontrado or ErroCaminhoNaoEncontrado
            ? "pasta não encontrada"
            : new Win32Exception(erro).Message;
        return ResultadoLeitura.Erro;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFile(string nome, uint acesso, uint compartilhamento, nint seguranca, uint criacao, uint atributos, nint modelo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetFileInformationByHandleEx(SafeFileHandle arquivo, int classe, byte* buffer, uint tamanho);
}
```

O `SemanticaDeBackup` é exigido pelo Windows para abrir pasta. Sem o privilégio de backup ligado (fatia 2), ele não dá acesso a mais nada.

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter LeitorPastaTestes`
Esperado: 8 testes verdes. Conferir que `testes/mapdisk.testes/bin/Release/net8.0-windows/arvores-teste/` ficou vazia.

- [ ] **Passo 6: commit** com a mensagem `Cria a leitura de pasta pelo Win32`.

---

### Tarefa 6: motor de varredura em paralelo

**Arquivos:**
- Criar: `src/mapdisk.nucleo/varredura/motor-varredura.cs`
- Teste: `testes/mapdisk.testes/motor-varredura-testes.cs`

**Interfaces:**
- Consome: `LeitorPasta.Ler`, `NoPasta`, `Volumes.Ler`, `Volumes.RaizDe`, `Alvo`.
- Produz: `sealed class ResultadoVarredura { required NoPasta Raiz; required InfoVolume? Volume; required TimeSpan Duracao; required bool Cancelada }`; `sealed class Varredura` com construtor `(NoPasta raiz)`, `Raiz`, `PastaAtual`, `Task<ResultadoVarredura> Conclusao`, `Varredura Comecar(Func<Varredura, Task<ResultadoVarredura>> executar)`; `interface IMotorVarredura { Varredura Iniciar(string alvo, CancellationToken cancelar); }`; `sealed class MotorVarredura(int? tarefas = null) : IMotorVarredura`.

- [ ] **Passo 1: testes que falham**

```csharp
namespace MapDisk.Testes;

public class MotorVarreduraTestes
{
    private static ResultadoVarredura Varrer(string caminho, CancellationToken cancelar = default) =>
        new MotorVarredura().Iniciar(Alvo.Normalizar(caminho, out _)!, cancelar).Conclusao.GetAwaiter().GetResult();

    [Fact]
    public void Soma_a_arvore_inteira()
    {
        using var t = new PastaTeste();
        t.Arquivo("4.bin", 10);
        t.Arquivo(@"a\1.bin", 1000);
        t.Arquivo(@"a\b\2.bin", 2000);
        t.Arquivo(@"c\3.bin", 3000);

        var r = Varrer(t.Raiz);

        Assert.False(r.Cancelada);
        Assert.Equal(6010, r.Raiz.Tamanho);
        Assert.Equal(4, r.Raiz.ArquivosTotal);
        Assert.Equal(3, r.Raiz.PastasTotal);
        Assert.Equal(EstadoPasta.Lida, r.Raiz.Estado);
        Assert.NotNull(r.Volume);
    }

    [Fact]
    public void Hard_link_conta_uma_vez()
    {
        using var t = new PastaTeste();
        t.Arquivo("x.bin", 4096);
        t.HardLink(@"outra\y.bin", "x.bin");

        var r = Varrer(t.Raiz);

        Assert.Equal(4096, r.Raiz.Tamanho);
        Assert.Equal(2, r.Raiz.ArquivosTotal);
    }

    [Fact]
    public void Juncao_nao_e_seguida()
    {
        using var t = new PastaTeste();
        t.Arquivo(@"dados\grande.bin", 10_000);
        t.Juncao("atalho", "dados");

        var r = Varrer(t.Raiz);

        Assert.Equal(10_000, r.Raiz.Tamanho);
        var atalho = r.Raiz.Subpastas.Single(s => s.Nome == "atalho");
        Assert.Equal(EstadoPasta.Link, atalho.Estado);
        Assert.EndsWith("dados", atalho.DestinoLink);
    }

    [Fact]
    public void Pasta_sem_acesso_e_contada_e_nao_vira_zero()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 100);
        t.NegarLeitura("fechada");

        var r = Varrer(t.Raiz);

        Assert.Equal(1, r.Raiz.PastasSemAcesso);
        Assert.Equal(EstadoPasta.SemAcesso, r.Raiz.Subpastas.Single(s => s.Nome == "fechada").Estado);
        Assert.Equal(100, r.Raiz.Tamanho);
    }

    [Fact]
    public void Caminho_longo_entra_na_soma()
    {
        using var t = new PastaTeste();
        t.Arquivo(Path.Combine(Path.Combine(Enumerable.Repeat(new string('q', 60), 5).ToArray()), "fundo.bin"), 77);

        Assert.Equal(77, Varrer(t.Raiz).Raiz.Tamanho);
    }

    [Fact]
    public void Cancelada_antes_de_comecar_termina_como_cancelada()
    {
        using var t = new PastaTeste();
        t.Arquivo("a.bin", 1);
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        var r = Varrer(t.Raiz, cancelar.Token);

        Assert.True(r.Cancelada);
        Assert.Equal(EstadoPasta.Pendente, r.Raiz.Estado);
    }

    [Fact]
    public void Alvo_que_nao_existe_marca_erro_na_raiz()
    {
        using var t = new PastaTeste();

        var r = Varrer(t.Caminho("nao-existe"));

        Assert.Equal(EstadoPasta.ErroLeitura, r.Raiz.Estado);
        Assert.Equal("pasta não encontrada", r.Raiz.Motivo);
    }

    [Fact]
    public void Raiz_existe_desde_o_inicio_para_a_tela()
    {
        using var t = new PastaTeste();
        var alvo = Alvo.Normalizar(t.Raiz, out _)!;

        var varredura = new MotorVarredura().Iniciar(alvo, CancellationToken.None);

        Assert.Equal(alvo, varredura.Raiz.Nome);
        Assert.Same(varredura.Raiz, varredura.Conclusao.GetAwaiter().GetResult().Raiz);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter MotorVarreduraTestes`
Esperado: erro de compilação.

- [ ] **Passo 3: implementação**

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapDisk.Nucleo;

public sealed class ResultadoVarredura
{
    public required NoPasta Raiz { get; init; }

    public required InfoVolume? Volume { get; init; }

    public required TimeSpan Duracao { get; init; }

    /// <summary>Interrompida antes do fim. Os números mostram só o que foi lido até ali.</summary>
    public required bool Cancelada { get; init; }
}

/// <summary>Uma varredura em andamento. A raiz existe desde o início e vai sendo preenchida.</summary>
public sealed class Varredura
{
    private string _pastaAtual;

    public Varredura(NoPasta raiz)
    {
        Raiz = raiz;
        _pastaAtual = raiz.Nome;
    }

    public NoPasta Raiz { get; }

    public string PastaAtual
    {
        get => Volatile.Read(ref _pastaAtual);
        internal set => Volatile.Write(ref _pastaAtual, value);
    }

    public Task<ResultadoVarredura> Conclusao { get; private set; } = Task.FromResult<ResultadoVarredura>(null!);

    public Varredura Comecar(Func<Varredura, Task<ResultadoVarredura>> executar)
    {
        Conclusao = executar(this);
        return this;
    }
}

public interface IMotorVarredura
{
    /// <summary>Começa a varrer o alvo, já normalizado, e devolve na hora, com a raiz para a tela.</summary>
    Varredura Iniciar(string alvo, CancellationToken cancelar);
}

/// <summary>
/// Varredura com várias tarefas lendo pastas ao mesmo tempo, com teto: até 16 em disco local e
/// 4 em caminho de rede, para não pesar no servidor. Só lê.
/// </summary>
public sealed class MotorVarredura(int? tarefas = null) : IMotorVarredura
{
    public Varredura Iniciar(string alvo, CancellationToken cancelar)
    {
        var quantas = tarefas ?? (Alvo.EhRede(alvo) ? 4 : Math.Clamp(Environment.ProcessorCount, 4, 16));
        return new Varredura(new NoPasta(alvo, null))
            .Comecar(v => Task.Run(() => Executar(v, quantas, cancelar), CancellationToken.None));
    }

    private static ResultadoVarredura Executar(Varredura varredura, int tarefas, CancellationToken cancelar)
    {
        var relogio = Stopwatch.StartNew();
        var raiz = varredura.Raiz;
        var volume = Volumes.Ler(raiz.Nome);

        // Hard link: o mesmo identificador de arquivo no volume soma uma vez só. Em caminho de
        // rede o identificador vem do servidor e pode repetir entre discos dele, então não conta.
        var vistos = new ConcurrentDictionary<long, byte>();
        Func<long, bool> primeiraVez = Alvo.EhRede(raiz.Nome) ? _ => true : id => vistos.TryAdd(id, 0);
        var alvoERaizDoVolume = string.Equals(raiz.Nome, Volumes.RaizDe(raiz.Nome), StringComparison.OrdinalIgnoreCase);

        using var fila = new BlockingCollection<(NoPasta No, string Caminho)>(new ConcurrentStack<(NoPasta, string)>());
        var pendentes = 1;
        fila.Add((raiz, raiz.Nome));

        var trabalhadores = new Task[tarefas];
        for (var i = 0; i < tarefas; i++)
        {
            trabalhadores[i] = Task.Run(
                () =>
                {
                    try
                    {
                        foreach (var (no, caminho) in fila.GetConsumingEnumerable(cancelar))
                        {
                            LerPasta(varredura, no, caminho, alvoERaizDoVolume && no == raiz, primeiraVez);
                            foreach (var sub in no.Subpastas)
                            {
                                if (sub.Estado == EstadoPasta.Pendente)
                                {
                                    Interlocked.Increment(ref pendentes);
                                    fila.Add((sub, Alvo.Juntar(caminho, sub.Nome)));
                                }
                            }

                            if (Interlocked.Decrement(ref pendentes) == 0)
                            {
                                fila.CompleteAdding();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                },
                CancellationToken.None);
        }

        Task.WaitAll(trabalhadores);
        return new ResultadoVarredura
        {
            Raiz = raiz,
            Volume = volume,
            Duracao = relogio.Elapsed,
            Cancelada = Volatile.Read(ref pendentes) > 0,
        };
    }

    private static void LerPasta(Varredura varredura, NoPasta no, string caminho, bool raizDoVolume, Func<long, bool> primeiraVez)
    {
        varredura.PastaAtual = caminho;
        var arquivos = new List<ArquivoInfo>();
        var entradas = new List<EntradaPasta>();
        try
        {
            switch (LeitorPasta.Ler(caminho, raizDoVolume, primeiraVez, arquivos, entradas, out var motivo))
            {
                case ResultadoLeitura.SemAcesso:
                    no.MarcarSemAcesso(motivo!);
                    return;
                case ResultadoLeitura.Erro:
                    no.MarcarErro(motivo!);
                    return;
            }
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            no.MarcarErro(e.Message);
            return;
        }

        var subpastas = new NoPasta[entradas.Count];
        for (var i = 0; i < entradas.Count; i++)
        {
            var entrada = entradas[i];
            var sub = new NoPasta(entrada.Nome, no, entrada.Modificacao);
            if (entrada.EhLink)
            {
                sub.MarcarLink(DestinoDoLink(Alvo.Juntar(caminho, entrada.Nome)));
            }

            subpastas[i] = sub;
        }

        no.Preencher(arquivos.ToArray(), subpastas);
    }

    private static string? DestinoDoLink(string caminho)
    {
        try
        {
            return new DirectoryInfo(Alvo.Longo(caminho)).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
```

- [ ] **Passo 4: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter MotorVarreduraTestes`
Esperado: 8 testes verdes.

- [ ] **Passo 5: commit** com a mensagem `Cria o motor de varredura em paralelo`.

---

### Tarefa 7: árvore visível

**Arquivos:**
- Criar: `src/mapdisk.nucleo/painel/linha-arvore.cs`, `src/mapdisk.nucleo/painel/arvore-visivel.cs`
- Teste: `testes/mapdisk.testes/arvore-visivel-testes.cs`

**Interfaces:**
- Consome: `NoPasta`, `ArquivoInfo`, `Formatador`, `UnidadeExibicao`.
- Produz: `enum ModoExibicao { Tamanho, Alocado, Contagem, Porcentagem }`; `enum ColunaOrdem { Valor, Nome, Tamanho, Alocado, Arquivos, Pastas, Modificacao }`; `enum TipoLinha { Pasta, GrupoArquivos, Arquivo }`; `sealed class LinhaArvore : INotifyPropertyChanged` com `Tipo`, `Pasta`, `Arquivo`, `Nivel`, `Expandida`, `PodeExpandir`, `Sinal`, `Recuo`, `Nome`, `Rotulo`, `SemValor`, `Tamanho`, `Alocado`, `Arquivos`, `Pastas`, `Modificacao`, `long Valor(ModoExibicao)`, `Fracao`, `LarguraBarra`, `TextoValor`, `TextoTamanho`, `TextoAlocado`, `TextoArquivos`, `TextoPastas`, `TextoPorcentagem`, `TextoModificacao`; `sealed class ArvoreVisivel` com `Linhas`, `Raiz`, `Modo`, `Unidade`, `Ordem`, `Decrescente`, `Carregar(NoPasta)`, `Limpar()`, `Expandir(LinhaArvore)`, `Recolher(LinhaArvore)`, `Alternar(LinhaArvore)`, `DefinirModo(ModoExibicao)`, `DefinirUnidade(UnidadeExibicao)`, `Ordenar(ColunaOrdem)`, `Atualizar()`.

- [ ] **Passo 1: testes que falham**

```csharp
namespace MapDisk.Testes;

public class ArvoreVisivelTestes
{
    private static readonly DateTime _d = new(2026, 9, 1);

    /// <summary>
    /// C:\ com dois arquivos soltos (510 bytes), Users (Ana 300, Bruno 100) e Windows sem acesso.
    /// Total 910.
    /// </summary>
    private static NoPasta Exemplo()
    {
        var raiz = new NoPasta(@"C:\", null);
        var users = new NoPasta("Users", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher(
            [new("pagefile.sys", 500, 500, _d, MarcaArquivo.Sistema), new("setup.log", 10, 4096, _d, MarcaArquivo.Nenhuma)],
            [users, windows]);
        var ana = new NoPasta("Ana", users);
        var bruno = new NoPasta("Bruno", users);
        users.Preencher([], [ana, bruno]);
        ana.Preencher([new("video.mp4", 300, 300, _d, MarcaArquivo.Nenhuma)], []);
        bruno.Preencher([new("foto.jpg", 100, 100, _d, MarcaArquivo.Nenhuma)], []);
        windows.MarcarSemAcesso("acesso negado");
        return raiz;
    }

    private static ArvoreVisivel Carregada(out NoPasta raiz)
    {
        raiz = Exemplo();
        var arvore = new ArvoreVisivel();
        arvore.Carregar(raiz);
        return arvore;
    }

    private static LinhaArvore Linha(ArvoreVisivel a, string nome) => a.Linhas.Single(l => l.Nome == nome);

    [Fact]
    public void Carregar_mostra_a_raiz_aberta_com_filhos_do_maior_para_o_menor()
    {
        var a = Carregada(out _);

        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Windows"], a.Linhas.Select(l => l.Nome));
        Assert.Equal([0, 1, 1, 1], a.Linhas.Select(l => l.Nivel));
        Assert.True(a.Linhas[0].Expandida);
    }

    [Fact]
    public void Pasta_sem_acesso_mostra_sem_acesso_e_nunca_zero()
    {
        var a = Carregada(out _);
        var windows = Linha(a, "Windows");

        Assert.True(windows.SemValor);
        Assert.Equal("sem acesso", windows.TextoTamanho);
        Assert.Equal("sem acesso", windows.TextoValor);
        Assert.Equal("sem acesso", windows.Rotulo);
        Assert.Equal("", windows.TextoPorcentagem);
        Assert.Equal("1 pasta sem leitura dentro", Linha(a, @"C:\").Rotulo);
    }

    [Fact]
    public void Expandir_e_recolher()
    {
        var a = Carregada(out _);

        a.Expandir(Linha(a, "Users"));
        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Ana", "Bruno", "Windows"], a.Linhas.Select(l => l.Nome));
        Assert.Equal(2, Linha(a, "Ana").Nivel);

        a.Recolher(Linha(a, "Users"));
        Assert.Equal([@"C:\", "[2 arquivos]", "Users", "Windows"], a.Linhas.Select(l => l.Nome));
    }

    [Fact]
    public void Grupo_de_arquivos_abre_com_o_rotulo_do_sistema()
    {
        var a = Carregada(out _);

        a.Alternar(Linha(a, "[2 arquivos]"));

        var pagefile = Linha(a, "pagefile.sys");
        Assert.Equal(2, pagefile.Nivel);
        Assert.Equal("arquivo do sistema", pagefile.Rotulo);
        Assert.Equal(510, Linha(a, "[2 arquivos]").Tamanho);
    }

    [Fact]
    public void Porcentagem_da_pasta_pai()
    {
        var a = Carregada(out _);
        a.Expandir(Linha(a, "Users"));

        Assert.Equal("100,0 %", Linha(a, @"C:\").TextoPorcentagem);
        Assert.Equal("44,0 %", Linha(a, "Users").TextoPorcentagem);
        Assert.Equal("75,0 %", Linha(a, "Ana").TextoPorcentagem);
        Assert.Equal(45, Linha(a, "Ana").LarguraBarra);
    }

    [Fact]
    public void Ordenar_por_nome_e_clicar_de_novo_inverte()
    {
        var a = Carregada(out _);

        a.Ordenar(ColunaOrdem.Nome);
        var crescente = a.Linhas.Select(l => l.Nome).ToList();
        Assert.True(crescente.IndexOf("Users") < crescente.IndexOf("Windows"));

        a.Ordenar(ColunaOrdem.Nome);
        var decrescente = a.Linhas.Select(l => l.Nome).ToList();
        Assert.True(decrescente.IndexOf("Windows") < decrescente.IndexOf("Users"));
    }

    [Fact]
    public void Modo_alocado_muda_o_valor_e_a_ordem()
    {
        var a = Carregada(out _);

        a.DefinirModo(ModoExibicao.Alocado);

        Assert.Equal("4,5 KB", Linha(a, "[2 arquivos]").TextoValor);
        Assert.Equal("400 Bytes", Linha(a, "Users").TextoValor);
        Assert.Equal("[2 arquivos]", a.Linhas[1].Nome);
    }

    [Fact]
    public void Modo_contagem_e_modo_porcentagem()
    {
        var a = Carregada(out _);

        a.DefinirModo(ModoExibicao.Contagem);
        Assert.Equal("2", Linha(a, "Users").TextoValor);

        a.DefinirModo(ModoExibicao.Porcentagem);
        Assert.Equal("44,0 %", Linha(a, "Users").TextoValor);
    }

    [Fact]
    public void Unidade_fixa()
    {
        var a = Carregada(out _);

        a.DefinirUnidade(UnidadeExibicao.KB);

        Assert.Equal("0,4 KB", Linha(a, "Users").TextoTamanho);
    }

    [Fact]
    public void Atualizar_mostra_o_que_a_varredura_leu_depois()
    {
        var raiz = new NoPasta(@"D:\", null);
        var a = new ArvoreVisivel();
        a.Carregar(raiz);
        Assert.Single(a.Linhas);

        var x = new NoPasta("x", raiz);
        raiz.Preencher([], [x]);
        a.Atualizar();

        Assert.Equal([@"D:\", "x"], a.Linhas.Select(l => l.Nome));
    }

    [Fact]
    public void A_mesma_linha_continua_depois_de_atualizar()
    {
        var a = Carregada(out _);
        var users = Linha(a, "Users");

        a.Atualizar();
        a.DefinirModo(ModoExibicao.Alocado);

        Assert.Same(users, Linha(a, "Users"));
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter ArvoreVisivelTestes`
Esperado: erro de compilação.

- [ ] **Passo 3: `linha-arvore.cs`**

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum ModoExibicao
{
    Tamanho,
    Alocado,
    Contagem,
    Porcentagem,
}

public enum ColunaOrdem
{
    /// <summary>O valor do modo de exibição. É a ordem padrão, do maior para o menor.</summary>
    Valor,
    Nome,
    Tamanho,
    Alocado,
    Arquivos,
    Pastas,
    Modificacao,
}

public enum TipoLinha
{
    Pasta,

    /// <summary>A linha "[N arquivos]", que junta os arquivos soltos de uma pasta.</summary>
    GrupoArquivos,
    Arquivo,
}

/// <summary>Uma linha da árvore na tela. Os textos são calculados pela ArvoreVisivel.</summary>
public sealed class LinhaArvore : INotifyPropertyChanged
{
    private const double LarguraMaximaBarra = 60;

    internal LinhaArvore(TipoLinha tipo, NoPasta pasta, int nivel, ArquivoInfo arquivo = default)
    {
        Tipo = tipo;
        Pasta = pasta;
        Nivel = nivel;
        Arquivo = arquivo;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TipoLinha Tipo { get; }

    /// <summary>A pasta da linha. No grupo e no arquivo, a pasta onde eles estão.</summary>
    public NoPasta Pasta { get; }

    public ArquivoInfo Arquivo { get; }

    public int Nivel { get; }

    public bool Expandida { get; internal set; }

    public bool PodeExpandir => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Subpastas.Count > 0 || Pasta.Arquivos.Count > 0,
        TipoLinha.GrupoArquivos => true,
        _ => false,
    };

    /// <summary>Sinal do botão de abrir e fechar: "+", "-" ou nada.</summary>
    public string Sinal => !PodeExpandir ? string.Empty : Expandida ? "-" : "+";

    public double Recuo => Nivel * 16;

    public string Nome => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Nome,
        TipoLinha.GrupoArquivos => $"[{Formatador.Plural(Pasta.Arquivos.Count, "arquivo", "arquivos")}]",
        _ => Arquivo.Nome,
    };

    /// <summary>Pasta sem número de verdade: sem acesso, com erro ou link. Nunca mostra 0.</summary>
    public bool SemValor => Tipo == TipoLinha.Pasta
        && Pasta.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura or EstadoPasta.Link;

    public string Rotulo => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Estado switch
        {
            EstadoPasta.SemAcesso => "sem acesso",
            EstadoPasta.ErroLeitura => $"erro de leitura: {Pasta.Motivo}",
            EstadoPasta.Link => Pasta.DestinoLink is { } destino ? $"link para {destino}" : "link",
            _ => Pasta.PastasSemAcesso + Pasta.PastasComErro is var n and > 0
                ? $"{Formatador.Plural(n, "pasta", "pastas")} sem leitura dentro"
                : string.Empty,
        },
        TipoLinha.Arquivo => RotuloDoArquivo(Arquivo.Marcas),
        _ => string.Empty,
    };

    public long Tamanho => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Tamanho,
        TipoLinha.GrupoArquivos => Pasta.TamanhoProprio,
        _ => Arquivo.Tamanho,
    };

    public long Alocado => Tipo switch
    {
        TipoLinha.Pasta => Pasta.Alocado,
        TipoLinha.GrupoArquivos => Pasta.AlocadoProprio,
        _ => Arquivo.Alocado,
    };

    public long Arquivos => Tipo switch
    {
        TipoLinha.Pasta => Pasta.ArquivosTotal,
        TipoLinha.GrupoArquivos => Pasta.Arquivos.Count,
        _ => 1,
    };

    public long Pastas => Tipo == TipoLinha.Pasta ? Pasta.PastasTotal : 0;

    public DateTime Modificacao => Tipo switch
    {
        TipoLinha.Pasta => Pasta.UltimaModificacao,
        TipoLinha.GrupoArquivos => Pasta.Arquivos.Count == 0 ? DateTime.MinValue : Pasta.Arquivos.Max(a => a.Modificacao),
        _ => Arquivo.Modificacao,
    };

    /// <summary>Fração do valor da pasta-pai, de 0 a 1. Na raiz, 1.</summary>
    public double Fracao { get; private set; }

    public double LarguraBarra => Math.Round(Fracao * LarguraMaximaBarra, 1);

    public string TextoValor { get; private set; } = string.Empty;

    public string TextoTamanho { get; private set; } = string.Empty;

    public string TextoAlocado { get; private set; } = string.Empty;

    public string TextoArquivos { get; private set; } = string.Empty;

    public string TextoPastas { get; private set; } = string.Empty;

    public string TextoPorcentagem { get; private set; } = string.Empty;

    public string TextoModificacao { get; private set; } = string.Empty;

    public long Valor(ModoExibicao modo) => modo switch
    {
        ModoExibicao.Alocado => Alocado,
        ModoExibicao.Contagem => Arquivos,
        _ => Tamanho,
    };

    /// <summary>Recalcula os textos com os números de agora e avisa a tela.</summary>
    internal void Atualizar(ModoExibicao modo, UnidadeExibicao unidade, long valorDoPai)
    {
        if (SemValor)
        {
            var texto = Pasta.Estado switch
            {
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.Link => "link",
                _ => "erro",
            };
            Fracao = 0;
            TextoValor = TextoTamanho = TextoAlocado = texto;
            TextoArquivos = TextoPastas = TextoPorcentagem = string.Empty;
        }
        else
        {
            var valor = Valor(modo);
            Fracao = valorDoPai > 0 ? Math.Clamp((double)valor / valorDoPai, 0, 1) : (Nivel == 0 ? 1 : 0);
            TextoTamanho = Formatador.Tamanho(Tamanho, unidade);
            TextoAlocado = Formatador.Tamanho(Alocado, unidade);
            TextoArquivos = Tipo == TipoLinha.Arquivo ? string.Empty : Formatador.Numero(Arquivos);
            TextoPastas = Tipo == TipoLinha.Pasta ? Formatador.Numero(Pastas) : string.Empty;
            TextoPorcentagem = Formatador.Porcentagem(Fracao);
            TextoValor = modo switch
            {
                ModoExibicao.Alocado => TextoAlocado,
                ModoExibicao.Contagem => Formatador.Numero(Arquivos),
                ModoExibicao.Porcentagem => TextoPorcentagem,
                _ => TextoTamanho,
            };
        }

        TextoModificacao = Formatador.Data(Modificacao);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private static string RotuloDoArquivo(MarcaArquivo marcas)
    {
        var partes = new List<string>();
        if (marcas.HasFlag(MarcaArquivo.Sistema))
        {
            partes.Add("arquivo do sistema");
        }

        if (marcas.HasFlag(MarcaArquivo.NaNuvem))
        {
            partes.Add("na nuvem");
        }

        if (marcas.HasFlag(MarcaArquivo.LinkRepetido))
        {
            partes.Add("hard link, contado uma vez");
        }

        if (marcas.HasFlag(MarcaArquivo.Link))
        {
            partes.Add("link");
        }

        return string.Join(", ", partes);
    }
}
```

- [ ] **Passo 4: `arvore-visivel.cs`**

```csharp
using System.Collections.ObjectModel;

namespace MapDisk.Nucleo;

/// <summary>
/// A árvore como lista de linhas visíveis, para uma tabela com rolagem virtual. Guarda quais
/// pastas e grupos estão abertos e remonta a lista a cada mudança, reaproveitando as mesmas
/// linhas, para a seleção da tela não se perder.
/// </summary>
public sealed class ArvoreVisivel
{
    private readonly HashSet<NoPasta> _pastasAbertas = [];
    private readonly HashSet<NoPasta> _gruposAbertos = [];
    private readonly Dictionary<NoPasta, LinhaArvore> _linhasPasta = [];
    private readonly Dictionary<NoPasta, LinhaArvore> _linhasGrupo = [];
    private readonly Dictionary<(NoPasta, int), LinhaArvore> _linhasArquivo = [];

    public ObservableCollection<LinhaArvore> Linhas { get; } = [];

    public NoPasta? Raiz { get; private set; }

    public ModoExibicao Modo { get; private set; }

    public UnidadeExibicao Unidade { get; private set; }

    public ColunaOrdem Ordem { get; private set; } = ColunaOrdem.Valor;

    public bool Decrescente { get; private set; } = true;

    public void Carregar(NoPasta raiz)
    {
        Limpar();
        Raiz = raiz;
        _pastasAbertas.Add(raiz);
        Atualizar();
    }

    public void Limpar()
    {
        Raiz = null;
        _pastasAbertas.Clear();
        _gruposAbertos.Clear();
        _linhasPasta.Clear();
        _linhasGrupo.Clear();
        _linhasArquivo.Clear();
        Linhas.Clear();
    }

    public void Expandir(LinhaArvore linha)
    {
        if (!linha.PodeExpandir)
        {
            return;
        }

        (linha.Tipo == TipoLinha.Pasta ? _pastasAbertas : _gruposAbertos).Add(linha.Pasta);
        Atualizar();
    }

    public void Recolher(LinhaArvore linha)
    {
        if (linha.Tipo == TipoLinha.Arquivo)
        {
            return;
        }

        (linha.Tipo == TipoLinha.Pasta ? _pastasAbertas : _gruposAbertos).Remove(linha.Pasta);
        Atualizar();
    }

    public void Alternar(LinhaArvore linha)
    {
        if (linha.Expandida)
        {
            Recolher(linha);
        }
        else
        {
            Expandir(linha);
        }
    }

    public void DefinirModo(ModoExibicao modo)
    {
        Modo = modo;
        Atualizar();
    }

    public void DefinirUnidade(UnidadeExibicao unidade)
    {
        Unidade = unidade;
        Atualizar();
    }

    /// <summary>Clicar na mesma coluna inverte a ordem. Coluna nova: nome em ordem crescente, números do maior para o menor.</summary>
    public void Ordenar(ColunaOrdem coluna)
    {
        if (coluna == Ordem)
        {
            Decrescente = !Decrescente;
        }
        else
        {
            Ordem = coluna;
            Decrescente = coluna != ColunaOrdem.Nome;
        }

        Atualizar();
    }

    /// <summary>Remonta a lista com os números de agora. A tela chama a cada 250 ms durante a varredura.</summary>
    public void Atualizar()
    {
        if (Raiz is null)
        {
            return;
        }

        var alvo = new List<LinhaArvore>();
        var raiz = LinhaDaPasta(Raiz, 0);
        Acrescentar(raiz, raiz.Valor(Modo), alvo);
        Sincronizar(alvo);
    }

    private void Acrescentar(LinhaArvore linha, long valorDoPai, List<LinhaArvore> alvo)
    {
        linha.Expandida = linha.PodeExpandir && linha.Tipo switch
        {
            TipoLinha.Pasta => _pastasAbertas.Contains(linha.Pasta),
            TipoLinha.GrupoArquivos => _gruposAbertos.Contains(linha.Pasta),
            _ => false,
        };
        linha.Atualizar(Modo, Unidade, valorDoPai);
        alvo.Add(linha);
        if (!linha.Expandida)
        {
            return;
        }

        // Os arquivos de um grupo são comparados com a pasta onde estão, não com o grupo.
        var valorDosFilhos = linha.Tipo == TipoLinha.GrupoArquivos ? valorDoPai : linha.Valor(Modo);
        foreach (var filho in Ordenados(Filhos(linha)))
        {
            Acrescentar(filho, valorDosFilhos, alvo);
        }
    }

    private IEnumerable<LinhaArvore> Filhos(LinhaArvore linha)
    {
        var nivel = linha.Nivel + 1;
        if (linha.Tipo == TipoLinha.GrupoArquivos)
        {
            var arquivos = linha.Pasta.Arquivos;
            for (var i = 0; i < arquivos.Count; i++)
            {
                yield return LinhaDoArquivo(linha.Pasta, i, nivel);
            }

            yield break;
        }

        foreach (var sub in linha.Pasta.Subpastas)
        {
            yield return LinhaDaPasta(sub, nivel);
        }

        if (linha.Pasta.Arquivos.Count > 0)
        {
            yield return LinhaDoGrupo(linha.Pasta, nivel);
        }
    }

    // As chaves são lidas uma vez antes de ordenar: durante a varredura os números mudam a todo instante.
    private List<LinhaArvore> Ordenados(IEnumerable<LinhaArvore> linhas)
    {
        var comChave = linhas.Select(l => (Linha: l, Numero: Chave(l), l.Nome)).ToList();
        var nome = StringComparer.CurrentCultureIgnoreCase;
        var ordem = Ordem == ColunaOrdem.Nome
            ? (Decrescente ? comChave.OrderByDescending(c => c.Nome, nome) : comChave.OrderBy(c => c.Nome, nome))
            : (Decrescente ? comChave.OrderByDescending(c => c.Numero) : comChave.OrderBy(c => c.Numero)).ThenBy(c => c.Nome, nome);
        return ordem.Select(c => c.Linha).ToList();
    }

    private long Chave(LinhaArvore linha) => Ordem switch
    {
        ColunaOrdem.Tamanho => linha.Tamanho,
        ColunaOrdem.Alocado => linha.Alocado,
        ColunaOrdem.Arquivos => linha.Arquivos,
        ColunaOrdem.Pastas => linha.Pastas,
        ColunaOrdem.Modificacao => linha.Modificacao.Ticks,
        ColunaOrdem.Nome => 0,
        _ => linha.SemValor ? -1 : linha.Valor(Modo),
    };

    private LinhaArvore LinhaDaPasta(NoPasta no, int nivel) =>
        _linhasPasta.TryGetValue(no, out var linha) ? linha : _linhasPasta[no] = new LinhaArvore(TipoLinha.Pasta, no, nivel);

    private LinhaArvore LinhaDoGrupo(NoPasta no, int nivel) =>
        _linhasGrupo.TryGetValue(no, out var linha) ? linha : _linhasGrupo[no] = new LinhaArvore(TipoLinha.GrupoArquivos, no, nivel);

    private LinhaArvore LinhaDoArquivo(NoPasta no, int indice, int nivel) =>
        _linhasArquivo.TryGetValue((no, indice), out var linha)
            ? linha
            : _linhasArquivo[(no, indice)] = new LinhaArvore(TipoLinha.Arquivo, no, nivel, no.Arquivos[indice]);

    // Leva a lista da tela à lista nova com o mínimo de mudanças, para a rolagem não pular.
    private void Sincronizar(List<LinhaArvore> alvo)
    {
        for (var i = 0; i < alvo.Count; i++)
        {
            if (i < Linhas.Count && ReferenceEquals(Linhas[i], alvo[i]))
            {
                continue;
            }

            var atual = Linhas.IndexOf(alvo[i]);
            if (atual >= 0)
            {
                Linhas.Move(atual, i);
            }
            else
            {
                Linhas.Insert(i, alvo[i]);
            }
        }

        while (Linhas.Count > alvo.Count)
        {
            Linhas.RemoveAt(Linhas.Count - 1);
        }
    }
}
```

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter ArvoreVisivelTestes`
Esperado: 11 testes verdes.

- [ ] **Passo 6: commit** com a mensagem `Cria a arvore visivel com modos, unidades e ordem`.

---

### Tarefa 8: painel da janela e modo de demonstração

**Arquivos:**
- Criar: `src/mapdisk.nucleo/painel/painel-principal.cs`, `src/mapdisk.nucleo/painel/demonstracao.cs`
- Teste: `testes/mapdisk.testes/painel-principal-testes.cs`

**Interfaces:**
- Consome: `IMotorVarredura`, `Varredura`, `ResultadoVarredura`, `ArvoreVisivel`, `Volumes`, `Alvo`, `Formatador`.
- Produz: `enum EstadoPainel { Parado, Varrendo, Cancelando }`; `sealed class PainelPrincipal : INotifyPropertyChanged` com construtor `(IMotorVarredura motor, Func<IReadOnlyList<InfoVolume>> listarUnidades)`, `static PainelPrincipal Padrao()`, `Arvore`, `Unidades`, `TextoAlvo`, `Estado`, `PodeVarrer`, `PodeParar`, `PodeAtualizar`, `Varrendo`, `TextoEstado`, `TextoVolume`, `TextoTotais`, `TextoSemLeitura`, `Erro`, `AtualizarUnidades()`, `bool Varrer()`, `Atualizar()`, `Parar()`, `Tique()`, `LimparErro()`; `static class Demonstracao` com `const string Argumento = "--demonstracao"`, `PainelPrincipal Painel()`, `IMotorVarredura Motor()`.

- [ ] **Passo 1: testes que falham**

```csharp
namespace MapDisk.Testes;

public class PainelPrincipalTestes
{
    private sealed class MotorFalso : IMotorVarredura
    {
        public TaskCompletionSource<ResultadoVarredura> Fim { get; } = new();

        public CancellationToken Token { get; private set; }

        public NoPasta? Raiz { get; private set; }

        public string? AlvoRecebido { get; private set; }

        public Varredura Iniciar(string alvo, CancellationToken cancelar)
        {
            AlvoRecebido = alvo;
            Token = cancelar;
            Raiz = new NoPasta(alvo, null);
            return new Varredura(Raiz).Comecar(_ => Fim.Task);
        }
    }

    private static readonly InfoVolume _c = new(@"C:\", "Sistema", "NTFS", 1L << 30, 2L << 30, 4096);
    private static readonly InfoVolume _d = new(@"D:\", "Dados", "NTFS", 1L << 30, 2L << 30, 4096);

    private static PainelPrincipal Painel(MotorFalso motor) => new(motor, () => [_c, _d]);

    private static ResultadoVarredura Resultado(NoPasta raiz, bool cancelada = false) => new()
    {
        Raiz = raiz,
        Volume = new InfoVolume(@"D:\", "Dados", "NTFS", 1024 * 1024, 2 * 1024 * 1024, 4096),
        Duracao = TimeSpan.FromSeconds(1.5),
        Cancelada = cancelada,
    };

    [Fact]
    public void Comeca_parado_com_a_primeira_unidade_no_alvo()
    {
        var p = Painel(new MotorFalso());

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Equal(@"C:\", p.TextoAlvo);
        Assert.True(p.PodeVarrer);
        Assert.False(p.PodeAtualizar);
        Assert.Equal(2, p.Unidades.Count);
    }

    [Fact]
    public void Alvo_invalido_nao_varre_e_explica()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.TextoAlvo = "pasta";

        Assert.False(p.Varrer());
        Assert.Contains("caminho completo", p.Erro);
        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Null(motor.AlvoRecebido);
    }

    [Fact]
    public void Varrer_normaliza_o_alvo_e_passa_a_varrendo()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.TextoAlvo = "d:";

        Assert.True(p.Varrer());

        Assert.Equal(@"D:\", motor.AlvoRecebido);
        Assert.Equal(EstadoPainel.Varrendo, p.Estado);
        Assert.True(p.PodeParar);
        Assert.False(p.PodeVarrer);
        Assert.StartsWith("Varrendo", p.TextoEstado);
    }

    [Fact]
    public void Parar_cancela_e_mostra_parando()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();

        p.Parar();

        Assert.Equal(EstadoPainel.Cancelando, p.Estado);
        Assert.True(motor.Token.IsCancellationRequested);
        Assert.Equal("Parando...", p.TextoEstado);
        Assert.False(p.PodeParar);
    }

    [Fact]
    public void Fim_da_varredura_volta_a_parado_com_totais()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.TextoAlvo = "D:";
        p.Varrer();
        motor.Raiz!.Preencher([new("a.bin", 100, 4096, DateTime.MinValue, MarcaArquivo.Nenhuma)], []);
        motor.Fim.SetResult(Resultado(motor.Raiz));

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Equal("Varredura concluída em 1,5 s.", p.TextoEstado);
        Assert.Equal("1 arquivo em 0 pastas", p.TextoTotais);
        Assert.Equal("Livre: 1,0 MB de 2,0 MB | Cluster 4,0 KB (NTFS)", p.TextoVolume);
        Assert.Equal("", p.TextoSemLeitura);
        Assert.True(p.PodeAtualizar);
        Assert.Null(p.Erro);
    }

    [Fact]
    public void Raiz_sem_acesso_vira_erro_depois_de_voltar_a_parado()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.TextoAlvo = "D:";
        p.Varrer();
        motor.Raiz!.MarcarSemAcesso("acesso negado");
        motor.Fim.SetResult(Resultado(motor.Raiz));

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Equal(@"Não foi possível ler D:\: acesso negado.", p.Erro);
        Assert.Equal("1 pasta sem acesso", p.TextoSemLeitura);
    }

    [Fact]
    public void Varredura_interrompida_avisa_que_os_numeros_sao_parciais()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        p.Parar();
        motor.Fim.SetResult(Resultado(motor.Raiz!, cancelada: true));

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Contains("interrompida", p.TextoEstado);
        Assert.Contains("só o que foi lido", p.TextoEstado);
    }

    [Fact]
    public void Falha_do_motor_vira_erro()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        motor.Fim.SetException(new IOException("disco removido"));

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Contains("disco removido", p.Erro);
    }

    [Fact]
    public void Contagem_separada_de_sem_acesso_e_erro()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        var a = new NoPasta("a", motor.Raiz);
        var b = new NoPasta("b", motor.Raiz);
        var c = new NoPasta("c", motor.Raiz);
        motor.Raiz!.Preencher([], [a, b, c]);
        a.MarcarSemAcesso("acesso negado");
        b.MarcarSemAcesso("acesso negado");
        c.MarcarErro("rede caiu");

        p.Tique();

        Assert.Equal("2 pastas sem acesso | 1 pasta com erro de leitura", p.TextoSemLeitura);
    }

    [Fact]
    public void Demonstracao_mostra_arvore_com_pasta_sem_acesso()
    {
        var p = Demonstracao.Painel();

        Assert.True(p.Varrer());
        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Equal("1 pasta sem acesso", p.TextoSemLeitura);
        Assert.Equal(@"C:\", p.Arvore.Linhas[0].Nome);
        Assert.True(p.Arvore.Linhas.Count > 3);
        Assert.Contains("NTFS", p.TextoVolume);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter PainelPrincipalTestes`
Esperado: erro de compilação.

- [ ] **Passo 3: `painel-principal.cs`**

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum EstadoPainel
{
    Parado,
    Varrendo,
    Cancelando,
}

/// <summary>
/// Estado e comandos da janela principal, sem nenhum tipo do WPF. A janela chama Tique a cada
/// 250 ms. É no Tique que os números se atualizam e que a varredura é fechada quando termina.
/// </summary>
public sealed class PainelPrincipal : INotifyPropertyChanged
{
    private readonly IMotorVarredura _motor;
    private readonly Func<IReadOnlyList<InfoVolume>> _listarUnidades;
    private CancellationTokenSource? _cancelar;
    private Varredura? _varredura;
    private InfoVolume? _volume;
    private string? _ultimoAlvo;

    public PainelPrincipal(IMotorVarredura motor, Func<IReadOnlyList<InfoVolume>> listarUnidades)
    {
        _motor = motor;
        _listarUnidades = listarUnidades;
        Unidades = listarUnidades();
        TextoAlvo = Unidades.FirstOrDefault()?.Raiz ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static PainelPrincipal Padrao() => new(new MotorVarredura(), Volumes.ListarUnidades);

    public ArvoreVisivel Arvore { get; } = new();

    public IReadOnlyList<InfoVolume> Unidades { get; private set; }

    public string TextoAlvo { get; set; }

    public EstadoPainel Estado { get; private set; }

    public bool PodeVarrer => Estado == EstadoPainel.Parado;

    public bool PodeParar => Estado == EstadoPainel.Varrendo;

    public bool PodeAtualizar => Estado == EstadoPainel.Parado && _ultimoAlvo != null;

    public bool Varrendo => Estado != EstadoPainel.Parado;

    public string TextoEstado { get; private set; } = "Escolha uma unidade, pasta ou caminho de rede e clique em Varrer.";

    public string TextoVolume { get; private set; } = string.Empty;

    public string TextoTotais { get; private set; } = string.Empty;

    public string TextoSemLeitura { get; private set; } = string.Empty;

    /// <summary>Mensagem para a janela mostrar numa caixa. A janela chama LimparErro depois.</summary>
    public string? Erro { get; private set; }

    public void AtualizarUnidades()
    {
        Unidades = _listarUnidades();
        Avisar();
    }

    public bool Varrer()
    {
        if (!PodeVarrer)
        {
            return false;
        }

        var alvo = Alvo.Normalizar(TextoAlvo, out var erro);
        if (alvo is null)
        {
            Erro = erro;
            Avisar();
            return false;
        }

        Iniciar(alvo);
        return true;
    }

    public void Atualizar()
    {
        if (PodeAtualizar)
        {
            Iniciar(_ultimoAlvo!);
        }
    }

    public void Parar()
    {
        if (Estado != EstadoPainel.Varrendo)
        {
            return;
        }

        Estado = EstadoPainel.Cancelando;
        TextoEstado = "Parando...";
        _cancelar!.Cancel();
        Avisar();
    }

    public void LimparErro() => Erro = null;

    public void Tique()
    {
        if (_varredura is null)
        {
            return;
        }

        if (_varredura.Conclusao.IsCompleted)
        {
            Concluir();
            return;
        }

        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void Iniciar(string alvo)
    {
        _ultimoAlvo = alvo;
        TextoAlvo = alvo;
        Erro = null;
        _volume = null;
        _cancelar = new CancellationTokenSource();
        _varredura = _motor.Iniciar(alvo, _cancelar.Token);
        Arvore.Carregar(_varredura.Raiz);
        Estado = EstadoPainel.Varrendo;
        AtualizarTextos();
    }

    private void Concluir()
    {
        var varredura = _varredura!;
        _varredura = null;

        // Primeiro o estado, depois qualquer aviso: os botões voltam ao normal antes da caixa de erro.
        Estado = EstadoPainel.Parado;
        if (varredura.Conclusao.IsFaulted)
        {
            TextoEstado = "A varredura parou por um erro.";
            Erro = $"A varredura parou por um erro: {varredura.Conclusao.Exception!.GetBaseException().Message}";
        }
        else
        {
            var r = varredura.Conclusao.Result;
            _volume = r.Volume;
            TextoEstado = r.Cancelada
                ? $"Varredura interrompida em {Formatador.Duracao(r.Duracao)}. Os números mostram só o que foi lido até ali."
                : $"Varredura concluída em {Formatador.Duracao(r.Duracao)}.";
            if (r.Raiz.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura)
            {
                Erro = $"Não foi possível ler {r.Raiz.Nome}: {r.Raiz.Motivo}.";
            }
        }

        _cancelar?.Dispose();
        _cancelar = null;
        Arvore.Atualizar();
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        if (Arvore.Raiz is { } raiz)
        {
            if (_varredura is not null && Estado == EstadoPainel.Varrendo)
            {
                TextoEstado = $"Varrendo {_varredura.PastaAtual}";
            }

            TextoTotais = $"{Formatador.Plural(raiz.ArquivosTotal, "arquivo", "arquivos")} em {Formatador.Plural(raiz.PastasTotal, "pasta", "pastas")}";
            var partes = new List<string>();
            if (raiz.PastasSemAcesso > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasSemAcesso, "pasta sem acesso", "pastas sem acesso"));
            }

            if (raiz.PastasComErro > 0)
            {
                partes.Add(Formatador.Plural(raiz.PastasComErro, "pasta com erro de leitura", "pastas com erro de leitura"));
            }

            TextoSemLeitura = string.Join(" | ", partes);
        }

        TextoVolume = _volume is { } v
            ? $"Livre: {Formatador.Tamanho(v.Livre)} de {Formatador.Tamanho(v.Total)} | Cluster {Formatador.Tamanho(v.Cluster)} ({v.SistemaArquivos})"
            : string.Empty;
        Avisar();
    }

    private void Avisar() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
```

- [ ] **Passo 4: `demonstracao.cs`**

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Dados de exemplo para a janela com --demonstracao, para prints da página e do README. Nomes
/// fictícios, nada lido do disco.
/// </summary>
public static class Demonstracao
{
    public const string Argumento = "--demonstracao";

    private const long Gb = 1024L * 1024 * 1024;

    private static readonly InfoVolume _volume = new(@"C:\", "Sistema", "NTFS", 373 * Gb, 952 * Gb, 4096);

    public static PainelPrincipal Painel() => new(Motor(), () => [_volume]);

    public static IMotorVarredura Motor() => new MotorDemonstracao();

    internal static NoPasta Montar(string alvo)
    {
        var d = new DateTime(2026, 9, 25, 10, 0, 0);
        var raiz = new NoPasta(alvo, null, d);
        var users = new NoPasta("Users", raiz, d);
        var windows = new NoPasta("Windows", raiz, d);
        var programas = new NoPasta("Program Files", raiz, d);
        var dados = new NoPasta("Dados", raiz, d);
        var sistema = new NoPasta("System Volume Information", raiz, d);
        raiz.Preencher(
            [
                new ArquivoInfo("pagefile.sys", 16 * Gb, 16 * Gb, d, MarcaArquivo.Sistema),
                new ArquivoInfo("hiberfil.sys", 12 * Gb, 12 * Gb, d, MarcaArquivo.Sistema),
            ],
            [users, windows, programas, dados, sistema]);
        sistema.MarcarSemAcesso("acesso negado");

        var ana = new NoPasta("ana.souza", users, d);
        var bruno = new NoPasta("bruno.lima", users, d);
        var publico = new NoPasta("Public", users, d);
        users.Preencher([], [ana, bruno, publico]);
        Encher(ana, d, ("Videos", 48 * Gb, 212), ("Downloads", 19 * Gb, 1840), ("Documents", 6 * Gb, 5230));
        Encher(bruno, d, ("Desktop", 9 * Gb, 830), ("AppData", 14 * Gb, 22000));
        Encher(publico, d, ("Documents", 1 * Gb, 40));
        Encher(windows, d, ("System32", 9 * Gb, 19000), ("WinSxS", 11 * Gb, 60000), ("Temp", 3 * Gb, 2400));
        Encher(programas, d, ("Office", 4 * Gb, 5600), ("Sistemas", 7 * Gb, 9800));
        Encher(dados, d, ("Backup", 120 * Gb, 48), ("Fotos", 31 * Gb, 15000));
        return raiz;
    }

    private static void Encher(NoPasta pasta, DateTime data, params (string Nome, long Bytes, int Arquivos)[] filhas)
    {
        var subpastas = filhas.Select(f => new NoPasta(f.Nome, pasta, data)).ToArray();
        pasta.Preencher([], subpastas);
        for (var i = 0; i < filhas.Length; i++)
        {
            var (_, bytes, quantos) = filhas[i];
            var cada = bytes / quantos;
            var alocado = (cada + 4095) / 4096 * 4096;
            subpastas[i].Preencher(
                Enumerable.Range(1, quantos)
                    .Select(n => new ArquivoInfo($"arquivo-{n:D5}.dat", cada, alocado, data.AddDays(-n), MarcaArquivo.Nenhuma))
                    .ToArray(),
                []);
        }
    }

    private sealed class MotorDemonstracao : IMotorVarredura
    {
        public Varredura Iniciar(string alvo, CancellationToken cancelar)
        {
            var raiz = Montar(alvo);
            return new Varredura(raiz).Comecar(_ => Task.FromResult(new ResultadoVarredura
            {
                Raiz = raiz,
                Volume = _volume,
                Duracao = TimeSpan.FromSeconds(12.4),
                Cancelada = false,
            }));
        }
    }
}
```

- [ ] **Passo 5: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter PainelPrincipalTestes`
Esperado: 10 testes verdes.

- [ ] **Passo 6: commit** com a mensagem `Cria o painel da janela e o modo de demonstracao`.

---

### Tarefa 9: linha de comando e CSV

**Arquivos:**
- Criar: `src/mapdisk.nucleo/linha-de-comando/argumentos.cs`, `src/mapdisk.nucleo/linha-de-comando/executor-cli.cs`, `src/mapdisk.nucleo/relatorios/exportador-csv.cs`
- Teste: `testes/mapdisk.testes/argumentos-testes.cs`, `testes/mapdisk.testes/exportador-csv-testes.cs`, `testes/mapdisk.testes/executor-cli-testes.cs`

**Interfaces:**
- Consome: `Alvo`, `IMotorVarredura`, `NoPasta`, `Formatador`, `Demonstracao.Motor()` (nos testes).
- Produz: `enum ComandoCli { Janela, Ajuda, Versao, Varrer }`; `sealed class ArgumentosCli` com `const string TextoAjuda`, `Comando`, `Caminho`, `Csv`, `Top`, `Erros`, `Valido`, `static ArgumentosCli Interpretar(IReadOnlyList<string> args)`; `static class ExportadorCsv` com `const string Cabecalho`, `Gravar(NoPasta, string arquivo)`, `Gravar(NoPasta, TextWriter)`; `static class ExecutorCli` com os códigos `CodigoSucesso = 0`, `CodigoArgumentos = 1`, `CodigoSemLeitura = 2`, `CodigoFalha = 3`, `CodigoCancelado = 4` e `int Executar(ArgumentosCli, IMotorVarredura, TextWriter saida, TextWriter erro, CancellationToken)`.

- [ ] **Passo 1: testes que falham**

`argumentos-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class ArgumentosTestes
{
    private static ArgumentosCli A(params string[] args) => ArgumentosCli.Interpretar(args);

    [Fact]
    public void Sem_argumentos_abre_a_janela()
    {
        var a = A();
        Assert.Equal(ComandoCli.Janela, a.Comando);
        Assert.True(a.Valido);
    }

    [Theory]
    [InlineData("--ajuda", ComandoCli.Ajuda)]
    [InlineData("/?", ComandoCli.Ajuda)]
    [InlineData("--versao", ComandoCli.Versao)]
    public void Ajuda_e_versao(string arg, ComandoCli esperado)
    {
        Assert.Equal(esperado, A(arg).Comando);
    }

    [Fact]
    public void Varrer_com_alvo_csv_e_top()
    {
        var a = A("varrer", "c:", "--csv", "saida.csv", "--top", "30");

        Assert.True(a.Valido, string.Join("; ", a.Erros));
        Assert.Equal(ComandoCli.Varrer, a.Comando);
        Assert.Equal(@"C:\", a.Caminho);
        Assert.Equal("saida.csv", a.Csv);
        Assert.Equal(30, a.Top);
    }

    [Fact]
    public void Varrer_caminho_de_rede()
    {
        Assert.Equal(@"\\srv\dados\", A("varrer", @"\\srv\dados").Caminho);
    }

    [Theory]
    [InlineData(new[] { "varrer" }, "Falta o alvo")]
    [InlineData(new[] { "varrer", "c:", "d:" }, "um alvo por vez")]
    [InlineData(new[] { "varrer", "Dados" }, "caminho completo")]
    [InlineData(new[] { "varrer", "c:", "--top", "0" }, "de 1 a 1000")]
    [InlineData(new[] { "varrer", "c:", "--top", "abc" }, "de 1 a 1000")]
    [InlineData(new[] { "varrer", "c:", "--csv" }, "Falta o valor de --csv")]
    [InlineData(new[] { "varrer", "c:", "--csv", "saida.txt" }, "terminar em .csv")]
    [InlineData(new[] { "--csv", "x.csv" }, "pedem o comando varrer")]
    [InlineData(new[] { "varrer", "c:", "--apagar" }, "Opção desconhecida: --apagar")]
    [InlineData(new[] { "apagar", "c:" }, "Comando desconhecido: apagar")]
    [InlineData(new[] { "varrer", "c:", "--ajuda" }, "só um comando")]
    public void Recusa_o_que_nao_existe_explicando(string[] args, string trecho)
    {
        var a = A(args);
        Assert.False(a.Valido);
        Assert.Contains(a.Erros, e => e.Contains(trecho, StringComparison.Ordinal));
    }

    [Fact]
    public void A_ajuda_diz_que_a_linha_de_comando_so_le()
    {
        Assert.Contains("nunca apaga nem move", ArgumentosCli.TextoAjuda);
    }
}
```

`exportador-csv-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class ExportadorCsvTestes
{
    private static readonly DateTime _d = new(2026, 9, 25, 14, 30, 0);

    private static string[] Linhas()
    {
        var raiz = new NoPasta(@"C:\", null);
        var dados = new NoPasta("Dados", raiz);
        var windows = new NoPasta("Windows", raiz);
        raiz.Preencher([new("a.bin", 100, 4096, _d, MarcaArquivo.Nenhuma)], [dados, windows]);
        var estranha = new NoPasta("x;y", dados);
        dados.Preencher([new("b.bin", 300, 4096, _d, MarcaArquivo.Nenhuma)], [estranha]);
        estranha.Preencher([new("c.bin", 50, 4096, _d, MarcaArquivo.Nenhuma)], []);
        windows.MarcarSemAcesso("acesso negado");

        var texto = new StringWriter();
        ExportadorCsv.Gravar(raiz, texto);
        return texto.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void Uma_linha_por_pasta_em_pre_ordem()
    {
        var linhas = Linhas();

        Assert.Equal(ExportadorCsv.Cabecalho, linhas[0]);
        Assert.Equal(@"C:\;lida;450;12288;3;3;100,0;25/09/2026 14:30", linhas[1]);
        Assert.Equal(@"C:\Dados;lida;350;8192;2;1;77,8;25/09/2026 14:30", linhas[2]);
        Assert.Equal(@"""C:\Dados\x;y"";lida;50;4096;1;0;14,3;25/09/2026 14:30", linhas[3]);
        Assert.Equal(5, linhas.Length);
    }

    [Fact]
    public void Pasta_sem_acesso_sai_sem_numeros_e_nunca_com_zero()
    {
        Assert.Equal(@"C:\Windows;sem acesso;;;;;;", Linhas()[4]);
    }

    [Fact]
    public void Arquivo_gravado_em_utf8_com_bom_para_o_excel()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"teste-{Guid.NewGuid():N}.csv");
        try
        {
            ExportadorCsv.Gravar(new NoPasta(@"C:\", null), arquivo);
            var bytes = File.ReadAllBytes(arquivo);
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        }
        finally
        {
            File.Delete(arquivo);
        }
    }
}
```

Conta da porcentagem: `C:\Dados` tem 350 de 450 (77,8 %); `x;y` tem 50 de 350 (14,3 %).

`executor-cli-testes.cs`:

```csharp
namespace MapDisk.Testes;

public class ExecutorCliTestes
{
    private static int Rodar(IMotorVarredura motor, out string saida, out string erro, params string[] args)
    {
        var s = new StringWriter();
        var e = new StringWriter();
        var codigo = ExecutorCli.Executar(ArgumentosCli.Interpretar(args), motor, s, e, CancellationToken.None);
        saida = s.ToString();
        erro = e.ToString();
        return codigo;
    }

    [Fact]
    public void Varrer_mostra_total_avisos_e_maiores_itens()
    {
        var codigo = Rodar(Demonstracao.Motor(), out var saida, out _, "varrer", "C:");

        Assert.Equal(ExecutorCli.CodigoSucesso, codigo);
        Assert.Contains("Varredura concluída", saida);
        Assert.Contains("Atenção: 1 pasta sem acesso", saida);
        Assert.Contains(@"Maiores itens em C:\", saida);
        var linhas = saida.Split(Environment.NewLine);
        Assert.Contains(linhas, l => l.Contains("Dados") && l.Contains("GB"));
        Assert.Contains(linhas, l => l.Contains("System Volume Information") && l.Contains("sem acesso"));
    }

    [Fact]
    public void Grava_o_csv_pedido()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, $"teste-{Guid.NewGuid():N}.csv");
        try
        {
            var codigo = Rodar(Demonstracao.Motor(), out var saida, out _, "varrer", "C:", "--csv", arquivo);

            Assert.Equal(ExecutorCli.CodigoSucesso, codigo);
            Assert.Contains("CSV gravado em", saida);
            Assert.Equal(ExportadorCsv.Cabecalho, File.ReadLines(arquivo).First());
        }
        finally
        {
            File.Delete(arquivo);
        }
    }

    [Fact]
    public void Argumento_invalido_devolve_1_com_o_motivo()
    {
        var codigo = Rodar(Demonstracao.Motor(), out _, out var erro, "varrer");

        Assert.Equal(ExecutorCli.CodigoArgumentos, codigo);
        Assert.Contains("Falta o alvo", erro);
        Assert.Contains("--ajuda", erro);
    }

    [Fact]
    public void Alvo_que_nao_pode_ser_lido_devolve_2()
    {
        var alvo = Path.Combine(AppContext.BaseDirectory, "nao-existe");

        var codigo = Rodar(new MotorVarredura(), out _, out var erro, "varrer", alvo);

        Assert.Equal(ExecutorCli.CodigoSemLeitura, codigo);
        Assert.Contains("pasta não encontrada", erro);
    }

    [Fact]
    public void Ajuda_e_versao_devolvem_0()
    {
        Assert.Equal(0, Rodar(Demonstracao.Motor(), out var ajuda, out _, "--ajuda"));
        Assert.Contains("mapdisk varrer", ajuda);
        Assert.Equal(0, Rodar(Demonstracao.Motor(), out var versao, out _, "--versao"));
        Assert.StartsWith("MapDisk - MT 0.1.0", versao);
    }
}
```

- [ ] **Passo 2: rodar e ver falhar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "ArgumentosTestes|ExportadorCsvTestes|ExecutorCliTestes"`
Esperado: erro de compilação.

- [ ] **Passo 3: `argumentos.cs`**

```csharp
using System.Globalization;

namespace MapDisk.Nucleo;

public enum ComandoCli
{
    Janela,
    Ajuda,
    Versao,
    Varrer,
}

/// <summary>Argumentos da linha de comando já interpretados. A linha de comando só lê.</summary>
public sealed class ArgumentosCli
{
    public const string TextoAjuda = """
        MapDisk - MT: onde está o espaço ocupado no disco
        https://mapdisk.manfred.com.br

        Uso:
          mapdisk                          abre a janela
          mapdisk varrer <alvo> [opções]   varre e mostra os maiores itens
          mapdisk --ajuda                  mostra esta ajuda
          mapdisk --versao                 mostra a versão

        <alvo> é uma unidade (C:), uma pasta (D:\Dados) ou um caminho de rede
        (\\servidor\pasta).

        Opções de varrer:
          --csv <arquivo.csv>   grava todas as pastas num arquivo CSV
          --top <n>             quantos itens mostrar, de 1 a 1000 (padrão: 10)

        Exemplos:
          mapdisk varrer C:
          mapdisk varrer \\servidor\dados --csv dados.csv --top 30

        A linha de comando só lê. Ela nunca apaga nem move arquivos.
        """;

    public ComandoCli Comando { get; private set; } = ComandoCli.Janela;

    /// <summary>O alvo de varrer, já normalizado.</summary>
    public string? Caminho { get; private set; }

    public string? Csv { get; private set; }

    public int Top { get; private set; } = 10;

    public List<string> Erros { get; } = [];

    public bool Valido => Erros.Count == 0;

    public static ArgumentosCli Interpretar(IReadOnlyList<string> args)
    {
        var a = new ArgumentosCli();
        var comandos = 0;
        var topDado = false;
        var alvos = new List<string>();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i].Trim();
            switch (arg.ToLowerInvariant())
            {
                case "--ajuda" or "-h" or "--help" or "-?" or "/?":
                    a.Comando = ComandoCli.Ajuda;
                    comandos++;
                    break;
                case "--versao" or "--version":
                    a.Comando = ComandoCli.Versao;
                    comandos++;
                    break;
                case "varrer":
                    a.Comando = ComandoCli.Varrer;
                    comandos++;
                    break;
                case "--csv":
                    a.Csv = Valor(args, ref i, arg, a.Erros);
                    break;
                case "--top":
                    topDado = true;
                    if (Numero(Valor(args, ref i, arg, a.Erros), 1, 1000, arg, a.Erros) is int top)
                    {
                        a.Top = top;
                    }

                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        a.Erros.Add($"Opção desconhecida: {arg}");
                    }
                    else
                    {
                        alvos.Add(arg);
                    }

                    break;
            }
        }

        if (comandos > 1)
        {
            a.Erros.Add("Use só um comando por vez: varrer, --ajuda ou --versao.");
        }

        if (a.Comando == ComandoCli.Varrer)
        {
            if (alvos.Count == 0)
            {
                a.Erros.Add("Falta o alvo. Exemplo: mapdisk varrer C:");
            }
            else if (alvos.Count > 1)
            {
                a.Erros.Add("Informe um alvo por vez.");
            }
            else
            {
                a.Caminho = Alvo.Normalizar(alvos[0], out var erro);
                if (a.Caminho is null)
                {
                    a.Erros.Add(erro!);
                }
            }

            if (a.Csv is { } csv && !csv.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                a.Erros.Add("O arquivo de --csv tem que terminar em .csv.");
            }
        }
        else
        {
            if (alvos.Count > 0)
            {
                a.Erros.Add($"Comando desconhecido: {alvos[0]}. Use varrer ou --ajuda.");
            }

            if (a.Csv != null || topDado)
            {
                a.Erros.Add("As opções --csv e --top pedem o comando varrer.");
            }
        }

        return a;
    }

    private static string? Valor(IReadOnlyList<string> args, ref int i, string opcao, List<string> erros)
    {
        if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            i++;
            return args[i];
        }

        erros.Add($"Falta o valor de {opcao}.");
        return null;
    }

    private static int? Numero(string? texto, int minimo, int maximo, string opcao, List<string> erros)
    {
        if (texto is null)
        {
            return null;
        }

        if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= minimo && n <= maximo)
        {
            return n;
        }

        erros.Add($"Valor inválido em {opcao}: {texto}. Use um número de {minimo} a {maximo}.");
        return null;
    }
}
```

O caso `"--csv"` sem valor cai em "Falta o valor de --csv.", que o teste confere.

- [ ] **Passo 4: `exportador-csv.cs`**

```csharp
using System.Globalization;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>
/// CSV com uma linha por pasta, separado por ponto e vírgula e gravado em UTF-8 com BOM, para
/// o Excel em português abrir direto. Pasta que não foi lida sai sem números, nunca com zero.
/// </summary>
public static class ExportadorCsv
{
    public const string Cabecalho = "Caminho;Estado;Tamanho (bytes);Alocado (bytes);Arquivos;Pastas;% da pasta-pai;Última modificação";

    public static void Gravar(NoPasta raiz, string arquivo)
    {
        using var saida = new StreamWriter(arquivo, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Gravar(raiz, saida);
    }

    public static void Gravar(NoPasta raiz, TextWriter saida)
    {
        saida.WriteLine(Cabecalho);
        var pilha = new Stack<(NoPasta No, string Caminho)>();
        pilha.Push((raiz, raiz.Nome));
        while (pilha.Count > 0)
        {
            var (no, caminho) = pilha.Pop();
            saida.WriteLine(Linha(no, caminho));
            var subpastas = no.Subpastas;
            for (var i = subpastas.Count - 1; i >= 0; i--)
            {
                pilha.Push((subpastas[i], Alvo.Juntar(caminho, subpastas[i].Nome)));
            }
        }
    }

    private static string Linha(NoPasta no, string caminho)
    {
        string[] campos = no.Estado == EstadoPasta.Lida
            ? [caminho, Estado(no), N(no.Tamanho), N(no.Alocado), N(no.ArquivosTotal), N(no.PastasTotal), Porcentagem(no), Data(no.UltimaModificacao)]
            : [caminho, Estado(no), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
        return string.Join(';', campos.Select(Campo));
    }

    private static string Estado(NoPasta no) => no.Estado switch
    {
        EstadoPasta.Lida => "lida",
        EstadoPasta.SemAcesso => "sem acesso",
        EstadoPasta.ErroLeitura => "erro de leitura",
        EstadoPasta.Link => "link",
        _ => "não lida",
    };

    private static string N(long n) => n.ToString(CultureInfo.InvariantCulture);

    private static string Porcentagem(NoPasta no)
    {
        var fracao = no.Pai is null ? 1d : no.Pai.Tamanho > 0 ? (double)no.Tamanho / no.Pai.Tamanho : 0d;
        return (fracao * 100).ToString("0.0", Formatador.PtBr);
    }

    private static string Data(DateTime d) => d == DateTime.MinValue ? string.Empty : d.ToString("dd/MM/yyyy HH:mm", Formatador.PtBr);

    private static string Campo(string texto) => texto.IndexOfAny([';', '"', '\n', '\r']) >= 0
        ? "\"" + texto.Replace("\"", "\"\"") + "\""
        : texto;
}
```

- [ ] **Passo 5: `executor-cli.cs`**

```csharp
namespace MapDisk.Nucleo;

/// <summary>A linha de comando sem console: recebe onde escrever, para ser testada. Só lê.</summary>
public static class ExecutorCli
{
    public const int CodigoSucesso = 0;
    public const int CodigoArgumentos = 1;
    public const int CodigoSemLeitura = 2;
    public const int CodigoFalha = 3;
    public const int CodigoCancelado = 4;

    public static string Versao => typeof(ExecutorCli).Assembly.GetName().Version!.ToString(3);

    public static int Executar(ArgumentosCli argumentos, IMotorVarredura motor, TextWriter saida, TextWriter erro, CancellationToken cancelar)
    {
        if (!argumentos.Valido)
        {
            foreach (var e in argumentos.Erros)
            {
                erro.WriteLine(e);
            }

            erro.WriteLine("Use --ajuda para ver as opções.");
            return CodigoArgumentos;
        }

        switch (argumentos.Comando)
        {
            case ComandoCli.Versao:
                saida.WriteLine($"MapDisk - MT {Versao}");
                return CodigoSucesso;
            case ComandoCli.Ajuda or ComandoCli.Janela:
                saida.WriteLine(ArgumentosCli.TextoAjuda);
                return CodigoSucesso;
        }

        saida.WriteLine($"Varrendo {argumentos.Caminho}...");
        var r = motor.Iniciar(argumentos.Caminho!, cancelar).Conclusao.GetAwaiter().GetResult();
        var raiz = r.Raiz;
        if (raiz.Estado is EstadoPasta.SemAcesso or EstadoPasta.ErroLeitura)
        {
            erro.WriteLine($"Não foi possível ler {raiz.Nome}: {raiz.Motivo}.");
            return CodigoSemLeitura;
        }

        saida.WriteLine(r.Cancelada
            ? "Varredura interrompida. Os números mostram só o que foi lido até ali."
            : $"Varredura concluída em {Formatador.Duracao(r.Duracao)}.");
        saida.WriteLine($"Total: {Formatador.Tamanho(raiz.Tamanho)} ({Formatador.Tamanho(raiz.Alocado)} alocados) em {Formatador.Plural(raiz.ArquivosTotal, "arquivo", "arquivos")} e {Formatador.Plural(raiz.PastasTotal, "pasta", "pastas")}.");
        if (r.Volume is { } v)
        {
            saida.WriteLine($"Unidade: {Formatador.Tamanho(v.Livre)} livres de {Formatador.Tamanho(v.Total)} ({v.SistemaArquivos}).");
        }

        if (raiz.PastasSemAcesso > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(raiz.PastasSemAcesso, "pasta sem acesso", "pastas sem acesso")}. O total não inclui o que está nelas. Rode como administrador para ler tudo.");
        }

        if (raiz.PastasComErro > 0)
        {
            saida.WriteLine($"Atenção: {Formatador.Plural(raiz.PastasComErro, "pasta com erro de leitura", "pastas com erro de leitura")}. O total não inclui o que está nelas.");
        }

        saida.WriteLine();
        saida.WriteLine($"Maiores itens em {raiz.Nome}:");
        foreach (var linha in MaioresItens(raiz, argumentos.Top))
        {
            saida.WriteLine(linha);
        }

        if (argumentos.Csv is { } csv)
        {
            try
            {
                ExportadorCsv.Gravar(raiz, csv);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                erro.WriteLine($"Não foi possível gravar o CSV: {e.Message}");
                return CodigoFalha;
            }

            saida.WriteLine();
            saida.WriteLine($"CSV gravado em {Path.GetFullPath(csv)}");
        }

        return r.Cancelada ? CodigoCancelado : CodigoSucesso;
    }

    private static IEnumerable<string> MaioresItens(NoPasta raiz, int quantos)
    {
        var itens = raiz.Subpastas
            .Select(p => (p.Nome, p.Tamanho, Texto: p.Estado switch
            {
                EstadoPasta.SemAcesso => "sem acesso",
                EstadoPasta.ErroLeitura => "erro",
                EstadoPasta.Link => "link",
                _ => Formatador.Tamanho(p.Tamanho),
            }, Lida: p.Estado == EstadoPasta.Lida))
            .ToList();
        if (raiz.Arquivos.Count > 0)
        {
            itens.Add(($"[{Formatador.Plural(raiz.Arquivos.Count, "arquivo", "arquivos")}]", raiz.TamanhoProprio, Formatador.Tamanho(raiz.TamanhoProprio), true));
        }

        foreach (var (nome, tamanho, texto, lida) in itens.OrderByDescending(i => i.Tamanho).ThenBy(i => i.Nome, StringComparer.CurrentCultureIgnoreCase).Take(quantos))
        {
            var porcentagem = lida && raiz.Tamanho > 0 ? Formatador.Porcentagem((double)tamanho / raiz.Tamanho) : string.Empty;
            yield return $"  {texto,12}  {porcentagem,8}  {nome}";
        }
    }
}
```

- [ ] **Passo 6: rodar e ver passar**

Rodar: `dotnet test mapdisk.sln -c Release --filter "ArgumentosTestes|ExportadorCsvTestes|ExecutorCliTestes"`
Esperado: 26 testes verdes (18 de argumentos, 3 do CSV e 5 do executor).

- [ ] **Passo 7: commit** com a mensagem `Cria a linha de comando e o CSV`.

---

### Tarefa 10: aplicativo WPF

**Arquivos:**
- Criar: `src/mapdisk/mapdisk.csproj`, `src/mapdisk/app.manifest`, `src/mapdisk/programa.cs`, `src/mapdisk/modo-linha-de-comando.cs`, `src/mapdisk/janela-principal.xaml`, `src/mapdisk/janela-principal.xaml.cs`
- Copiar do MapNet (autorizado): `src/mapdisk/tema/tema-mt.xaml`, `src/mapdisk/recursos/fontes/*.ttf`, `src/mapdisk/recursos/fontes/ofl.txt`, `src/mapdisk/recursos/mt-logo.png`
- Teste: `testes/mapdisk.testes/recursos-testes.cs`

**Interfaces:**
- Consome: `PainelPrincipal`, `ArvoreVisivel`, `LinhaArvore`, `ModoExibicao`, `UnidadeExibicao`, `ColunaOrdem`, `Demonstracao`, `ArgumentosCli`, `ExecutorCli`, `MotorVarredura`.

- [ ] **Passo 1: copiar tema, fontes e logo do MapNet**

```bash
mkdir -p src/mapdisk/tema src/mapdisk/recursos/fontes
sed -e 's#/mapnet;component/#/mapdisk;component/#g' -e 's/tela do MapNet/tela do MapDisk/' ../MAPNET-MT/src/mapnet/tema/tema-mt.xaml > src/mapdisk/tema/tema-mt.xaml
cp ../MAPNET-MT/src/mapnet/recursos/fontes/*.ttf ../MAPNET-MT/src/mapnet/recursos/fontes/ofl.txt src/mapdisk/recursos/fontes/
cp ../MAPNET-MT/src/mapnet/recursos/mt-logo.png src/mapdisk/recursos/
grep -n "mapnet" src/mapdisk/tema/tema-mt.xaml || echo "tema sem referencia ao mapnet"
```

Esperado: `tema sem referencia ao mapnet`.

- [ ] **Passo 2: teste de recursos que falha, `recursos-testes.cs`**

```csharp
namespace MapDisk.Testes;

public class RecursosTestes
{
    private static string App(string relativo) =>
        Path.Combine(CaracteresProibidosTestes.RaizDoRepositorio(), "src", "mapdisk", relativo);

    [Fact]
    public void Fonte_vai_com_a_licenca()
    {
        foreach (var fonte in new[] { "montserrat-regular.ttf", "montserrat-semibold.ttf", "montserrat-extrabold.ttf", "ofl.txt" })
        {
            Assert.True(File.Exists(App(Path.Combine("recursos", "fontes", fonte))), fonte);
        }
    }

    [Fact]
    public void Tema_tem_as_cores_oficiais_da_mt()
    {
        var tema = File.ReadAllText(App(Path.Combine("tema", "tema-mt.xaml")));
        foreach (var cor in new[] { "#006B2D", "#0F8F2F", "#43A92C", "#9AD52B", "#202020", "#F4F4F4" })
        {
            Assert.Contains(cor, tema);
        }

        Assert.DoesNotContain("mapnet", tema, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Manifesto_roda_sem_administrador()
    {
        var manifesto = File.ReadAllText(App("app.manifest"));
        Assert.Contains("level=\"asInvoker\"", manifesto);
    }
}
```

Rodar: `dotnet test mapdisk.sln -c Release --filter RecursosTestes`
Esperado: `Manifesto_roda_sem_administrador` falha (o manifesto ainda não existe). Os outros dois passam.

- [ ] **Passo 3: `app.manifest`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="MT.MapDisk" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <!-- Abre sem administrador (regra 4). A elevacao e pedida so pelo botao da fatia 2 -->
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <!-- Nitidez em monitores com escala diferente de 100%, cada um com a sua -->
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
      <!-- Caminhos com mais de 260 caracteres nas APIs do .NET -->
      <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
    </windowsSettings>
  </application>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <!-- Windows 10 e 11, Windows Server 2016 ou mais novo -->
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
```

- [ ] **Passo 4: `mapdisk.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <AssemblyName>mapdisk</AssemblyName>
    <RootNamespace>MapDisk</RootNamespace>
    <Description>MapDisk - MT: onde esta o espaco ocupado no disco.</Description>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <ApplicationManifest>app.manifest</ApplicationManifest>

    <!-- Entrega: um .exe unico, autocontido, para Windows 64 bits -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>

  <!-- Fonte e logo da MT embutidos no .exe e lidos pela tela com pack:// -->
  <ItemGroup>
    <Resource Include="recursos\mt-logo.png" />
    <Resource Include="recursos\fontes\*.ttf" />
    <Resource Include="recursos\fontes\ofl.txt" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\mapdisk.nucleo\mapdisk.nucleo.csproj" />
  </ItemGroup>

</Project>
```

O ícone do MapDisk fica de fora até a arte chegar (pendência "Ícone e logo do MapDisk").

- [ ] **Passo 5: `programa.cs`**

```csharp
using System.Windows;
using System.Windows.Threading;
using MapDisk.Nucleo;

namespace MapDisk;

internal static class Programa
{
    private static bool _erroMostrado;

    /// <summary>
    /// Sem argumentos abre a janela. Com argumentos roda a linha de comando, no mesmo .exe.
    /// O argumento --demonstracao abre a janela com dados de exemplo, sem ler o disco.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        var demonstracao = args is [Demonstracao.Argumento];
        if (args.Length > 0 && !demonstracao)
        {
            return ModoLinhaDeComando.Executar(ArgumentosCli.Interpretar(args));
        }

        var aplicativo = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        aplicativo.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/mapdisk;component/tema/tema-mt.xaml", UriKind.Absolute),
        });
        aplicativo.DispatcherUnhandledException += AoErroNaoTratado;
        return aplicativo.Run(demonstracao
            ? new JanelaPrincipal(Demonstracao.Painel(), " (demonstração)")
            : new JanelaPrincipal(PainelPrincipal.Padrao(), string.Empty));
    }

    /// <summary>
    /// Erro que escapou da tela: mostra a mensagem uma vez e fecha o programa. Um erro de
    /// desenho da tela se repete a cada tentativa de redesenhar, e manter o programa aberto
    /// empilharia uma janela de erro atrás da outra.
    /// </summary>
    private static void AoErroNaoTratado(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (_erroMostrado)
        {
            return;
        }

        _erroMostrado = true;
        MessageBox.Show(
            $"Aconteceu um erro inesperado: {e.Exception.Message}\n\nO MapDisk vai fechar. Abra de novo e, se o erro voltar, avise a MT.",
            "MapDisk - MT",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Application.Current.Shutdown(1);
    }
}
```

- [ ] **Passo 6: `modo-linha-de-comando.cs`**

```csharp
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MapDisk.Nucleo;

namespace MapDisk;

/// <summary>
/// Liga o .exe de janela ao console de quem o chamou e passa a vez ao ExecutorCli. No Prompt de
/// Comando, use "start /wait" para o prompt esperar o fim. No PowerShell, termine com "| Out-Host".
/// </summary>
internal static partial class ModoLinhaDeComando
{
    private const int ConsoleDoPai = -1;
    private const int SaidaPadrao = -11;
    private const uint TipoDisco = 1;
    private const uint TipoPipe = 3;

    public static int Executar(ArgumentosCli argumentos)
    {
        LigarConsole();
        using var cancelar = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancelar.Cancel();
            Console.WriteLine();
            Console.WriteLine("Interrompendo...");
        };

        try
        {
            Console.WriteLine();
            return ExecutorCli.Executar(argumentos, new MotorVarredura(), Console.Out, Console.Error, cancelar.Token);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Erro: {e.Message}");
            return ExecutorCli.CodigoFalha;
        }
        finally
        {
            Console.Out.Flush();
        }
    }

    private static void LigarConsole()
    {
        if (GetFileType(GetStdHandle(SaidaPadrao)) is TipoDisco or TipoPipe)
        {
            // Arquivo ou pipe: grava em UTF-8, para os acentos chegarem inteiros.
            var utf8 = new UTF8Encoding(false);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true });
            return;
        }

        if (!AttachConsole(ConsoleDoPai))
        {
            AllocConsole();
        }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Console sem suporte à troca de página de código: segue com a padrão.
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int qual);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetFileType(nint arquivo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int processo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();
}
```

- [ ] **Passo 7: `janela-principal.xaml`**

```xml
<Window x:Class="MapDisk.JanelaPrincipal"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Style="{StaticResource JanelaMT}"
        Title="MapDisk - MT" Width="1180" Height="760" MinWidth="860" MinHeight="480"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <!-- Botao de abrir e fechar a linha: so o sinal, sem fundo -->
        <Style x:Key="BotaoAbrir" TargetType="{x:Type Button}">
            <Setter Property="Width" Value="18" />
            <Setter Property="Height" Value="18" />
            <Setter Property="Focusable" Value="False" />
            <Setter Property="Cursor" Value="Hand" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type Button}">
                        <Border Background="Transparent">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Setter Property="FontWeight" Value="ExtraBold" />
            <Setter Property="Foreground" Value="{StaticResource PincelVerdeEscuro}" />
        </Style>

        <Style x:Key="Numero" TargetType="{x:Type TextBlock}">
            <Setter Property="TextAlignment" Value="Right" />
        </Style>
    </Window.Resources>

    <DockPanel>
        <!-- Faixa do topo: alvo e comandos -->
        <Border DockPanel.Dock="Top" Background="{StaticResource GradienteFaixa}" Padding="14,10">
            <DockPanel>
                <TextBlock DockPanel.Dock="Left" Text="MapDisk" Foreground="White" FontSize="20"
                           FontWeight="ExtraBold" VerticalAlignment="Center" Margin="0,0,18,0" />
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                    <ComboBox x:Name="CampoAlvo" Width="340" IsEditable="True"
                              ItemsSource="{Binding Unidades}" TextSearch.TextPath="Raiz"
                              Text="{Binding TextoAlvo, UpdateSourceTrigger=PropertyChanged}"
                              ToolTip="Unidade, pasta ou caminho de rede, como C:, D:\Dados ou \\servidor\pasta"
                              KeyDown="AoTeclarNoAlvo" DropDownOpened="AoAbrirUnidades">
                        <ComboBox.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding Descricao}" />
                            </DataTemplate>
                        </ComboBox.ItemTemplate>
                    </ComboBox>
                    <Button Content="Procurar..." Style="{StaticResource BotaoFaixaContorno}" Click="AoProcurar"
                            IsEnabled="{Binding PodeVarrer}" />
                    <Button Content="Varrer" Style="{StaticResource BotaoFaixa}" Click="AoVarrer"
                            IsEnabled="{Binding PodeVarrer}" />
                    <Button Content="Parar" Style="{StaticResource BotaoFaixaContorno}" Click="AoParar"
                            IsEnabled="{Binding PodeParar}" />
                    <Button Content="Atualizar" Style="{StaticResource BotaoFaixaContorno}" Click="AoAtualizar"
                            IsEnabled="{Binding PodeAtualizar}" />
                </StackPanel>
            </DockPanel>
        </Border>

        <!-- Exibicao: modo e unidade -->
        <Border DockPanel.Dock="Top" Background="White" BorderBrush="{StaticResource PincelBorda}"
                BorderThickness="0,0,0,1" Padding="14,6">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Mostrar:" Style="{StaticResource TituloGrupo}" VerticalAlignment="Center" Margin="0,0,8,0" />
                <RadioButton GroupName="Modo" Content="Tamanho" Tag="Tamanho" IsChecked="True" Checked="AoMudarModo" Margin="0,0,12,0" />
                <RadioButton GroupName="Modo" Content="Espaço alocado" Tag="Alocado" Checked="AoMudarModo" Margin="0,0,12,0" />
                <RadioButton GroupName="Modo" Content="Contagem de arquivos" Tag="Contagem" Checked="AoMudarModo" Margin="0,0,12,0" />
                <RadioButton GroupName="Modo" Content="Porcentagem" Tag="Porcentagem" Checked="AoMudarModo" Margin="0,0,28,0" />
                <TextBlock Text="Unidade:" Style="{StaticResource TituloGrupo}" VerticalAlignment="Center" Margin="0,0,8,0" />
                <RadioButton GroupName="Unidade" Content="Automática" Tag="Automatica" IsChecked="True" Checked="AoMudarUnidade" Margin="0,0,12,0" />
                <RadioButton GroupName="Unidade" Content="GB" Tag="GB" Checked="AoMudarUnidade" Margin="0,0,12,0" />
                <RadioButton GroupName="Unidade" Content="MB" Tag="MB" Checked="AoMudarUnidade" Margin="0,0,12,0" />
                <RadioButton GroupName="Unidade" Content="KB" Tag="KB" Checked="AoMudarUnidade" />
            </StackPanel>
        </Border>

        <!-- Barra de status -->
        <Border DockPanel.Dock="Bottom" Background="{StaticResource PincelGrafite}" Padding="12,6">
            <DockPanel>
                <ProgressBar DockPanel.Dock="Left" Style="{StaticResource ProgressoMT}" Width="120" Margin="0,0,12,0"
                             IsIndeterminate="True" VerticalAlignment="Center"
                             Visibility="{Binding Varrendo, Converter={StaticResource VisivelSe}}" />
                <TextBlock DockPanel.Dock="Right" Text="{Binding TextoVolume}" Style="{StaticResource TextoEstado}" />
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="{Binding TextoEstado}" Style="{StaticResource TextoEstado}"
                               MaxWidth="520" TextTrimming="CharacterEllipsis" />
                    <TextBlock Text="{Binding TextoTotais}" Style="{StaticResource TextoEstado}" Margin="18,0,0,0" />
                    <TextBlock Text="{Binding TextoSemLeitura}" Style="{StaticResource TextoEstado}" Margin="18,0,0,0"
                               Foreground="{StaticResource PincelVerdeClaro}" FontWeight="SemiBold" />
                </StackPanel>
            </DockPanel>
        </Border>

        <!-- Arvore com colunas. So as linhas visiveis sao desenhadas -->
        <ListView x:Name="Tabela" ItemsSource="{Binding Arvore.Linhas}" BorderThickness="0"
                  VirtualizingPanel.IsVirtualizing="True" VirtualizingPanel.VirtualizationMode="Recycling"
                  ScrollViewer.CanContentScroll="True" SelectionMode="Single"
                  MouseDoubleClick="AoClicarDuasVezes" KeyDown="AoTeclarNaTabela">
            <ListView.ItemContainerStyle>
                <Style TargetType="{x:Type ListViewItem}">
                    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding SemValor}" Value="True">
                            <Setter Property="Foreground" Value="{StaticResource PincelTextoSuave}" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ListView.ItemContainerStyle>
            <ListView.View>
                <GridView>
                    <GridViewColumn Width="460">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Nome" Tag="Nome" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <StackPanel Orientation="Horizontal">
                                    <Border Width="{Binding Recuo}" />
                                    <Button Style="{StaticResource BotaoAbrir}" Content="{Binding Sinal}" Click="AoAlternar" />
                                    <Grid Width="60" Height="10" Margin="4,0,6,0" VerticalAlignment="Center">
                                        <Border Background="{StaticResource PincelCinzaClaro}" CornerRadius="2" />
                                        <Border Background="{StaticResource PincelVerdeMedio}" CornerRadius="2"
                                                HorizontalAlignment="Left" Width="{Binding LarguraBarra}" />
                                    </Grid>
                                    <TextBlock Text="{Binding TextoValor}" Width="78" Style="{StaticResource Numero}" Margin="0,0,10,0" />
                                    <TextBlock Text="{Binding Nome}" FontWeight="SemiBold" />
                                    <TextBlock Text="{Binding Rotulo}" Foreground="{StaticResource PincelTextoSuave}" Margin="10,0,0,0" />
                                </StackPanel>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="95">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Tamanho" Tag="Tamanho" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoTamanho}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="95">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Alocado" Tag="Alocado" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoAlocado}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="85">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Arquivos" Tag="Arquivos" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoArquivos}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="75">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Pastas" Tag="Pastas" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoPastas}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="80">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="% da pasta" Tag="Valor" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoPorcentagem}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                    <GridViewColumn Width="120">
                        <GridViewColumn.Header>
                            <GridViewColumnHeader Content="Última modificação" Tag="Modificacao" Click="AoClicarCabecalho" />
                        </GridViewColumn.Header>
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TextoModificacao}" Style="{StaticResource Numero}" />
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                </GridView>
            </ListView.View>
        </ListView>
    </DockPanel>
</Window>
```

- [ ] **Passo 8: `janela-principal.xaml.cs`**

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MapDisk.Nucleo;
using Microsoft.Win32;

namespace MapDisk;

/// <summary>Só liga os controles ao PainelPrincipal. Toda regra fica no núcleo.</summary>
public partial class JanelaPrincipal : Window
{
    private readonly PainelPrincipal _painel;
    private readonly DispatcherTimer _relogio = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public JanelaPrincipal(PainelPrincipal painel, string sufixoTitulo)
    {
        _painel = painel;
        DataContext = painel;
        InitializeComponent();
        Title = $"MapDisk - MT {ExecutorCli.Versao}{sufixoTitulo}";
        _relogio.Tick += (_, _) =>
        {
            _painel.Tique();
            MostrarErro();
        };
        _relogio.Start();
    }

    private void AoVarrer(object sender, RoutedEventArgs e)
    {
        _painel.Varrer();
        MostrarErro();
    }

    private void AoParar(object sender, RoutedEventArgs e) => _painel.Parar();

    private void AoAtualizar(object sender, RoutedEventArgs e) => _painel.Atualizar();

    private void AoAbrirUnidades(object? sender, EventArgs e) => _painel.AtualizarUnidades();

    private void AoProcurar(object sender, RoutedEventArgs e)
    {
        var dialogo = new OpenFolderDialog { Title = "Escolha a pasta para varrer" };
        if (dialogo.ShowDialog(this) == true)
        {
            CampoAlvo.Text = dialogo.FolderName;
        }
    }

    private void AoTeclarNoAlvo(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AoVarrer(sender, e);
            e.Handled = true;
        }
    }

    private void AoMudarModo(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<ModoExibicao>(tag, out var modo))
        {
            _painel.Arvore.DefinirModo(modo);
        }
    }

    private void AoMudarUnidade(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<UnidadeExibicao>(tag, out var unidade))
        {
            _painel.Arvore.DefinirUnidade(unidade);
        }
    }

    private void AoClicarCabecalho(object sender, RoutedEventArgs e)
    {
        if (sender is GridViewColumnHeader { Tag: string tag } && Enum.TryParse<ColunaOrdem>(tag, out var coluna))
        {
            _painel.Arvore.Ordenar(coluna);
        }
    }

    private void AoAlternar(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LinhaArvore linha })
        {
            _painel.Arvore.Alternar(linha);
        }
    }

    private void AoClicarDuasVezes(object sender, MouseButtonEventArgs e)
    {
        if (Tabela.SelectedItem is LinhaArvore linha)
        {
            _painel.Arvore.Alternar(linha);
        }
    }

    private void AoTeclarNaTabela(object sender, KeyEventArgs e)
    {
        if (Tabela.SelectedItem is not LinhaArvore linha)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Right:
                _painel.Arvore.Expandir(linha);
                e.Handled = true;
                break;
            case Key.Left:
                _painel.Arvore.Recolher(linha);
                e.Handled = true;
                break;
            case Key.Enter:
                _painel.Arvore.Alternar(linha);
                e.Handled = true;
                break;
        }
    }

    private void MostrarErro()
    {
        if (_painel.Erro is not { } erro)
        {
            return;
        }

        _painel.LimparErro();
        MessageBox.Show(this, erro, "MapDisk - MT", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

- [ ] **Passo 9: acrescentar o aplicativo à solução e compilar**

```bash
dotnet sln mapdisk.sln add src/mapdisk/mapdisk.csproj
dotnet build mapdisk.sln -c Release
dotnet test mapdisk.sln -c Release
```

Esperado: build sem aviso e todos os testes verdes, inclusive os 3 de `RecursosTestes`.

- [ ] **Passo 10: conferência na janela, pelo agente**

1. `dotnet run --project src/mapdisk -c Release -- --demonstracao`: a janela abre com a árvore de exemplo. Conferir:
   - `System Volume Information` aparece em cinza como "sem acesso";
   - a barra diz "1 pasta sem acesso";
   - os quatro modos e as quatro unidades trocam os números;
   - clicar nos cabeçalhos muda a ordem;
   - "+", duplo clique, Enter e as setas abrem e fecham as linhas;
   - o grupo "[2 arquivos]" da raiz mostra `pagefile.sys` como "arquivo do sistema".
2. `dotnet run --project src/mapdisk -c Release`, digitar a pasta `testes\mapdisk.testes\bin` do projeto e clicar em Varrer: os números aparecem durante a varredura, e Parar interrompe.
3. Linha de comando: `dotnet run --project src/mapdisk -c Release -- varrer . --csv .superpowers/rascunho/teste.csv | Out-Host` (no PowerShell). Conferir o resumo e o CSV.

Resultado da conferência vai para a conversa, nunca para arquivo do repositório.

- [ ] **Passo 11: commit** com a mensagem `Cria a janela WPF e o modo linha de comando`.

---

### Tarefa 11: publicação, CI, documentação e Pull Request

**Arquivos:**
- Criar: `ferramentas/publicar.cmd`, `.github/workflows/testes.yml`
- Modificar: `README.md`, `docs/superpowers/pendencias.md`

- [ ] **Passo 1: copiar `publicar.cmd` e o CI do MapNet (autorizado), trocando o nome**

```bash
mkdir -p ferramentas .github/workflows
sed -e 's/mapnet/mapdisk/g' ../MAPNET-MT/ferramentas/publicar.cmd > ferramentas/publicar.cmd
sed -e 's/mapnet/mapdisk/g' -e 's/MapNet/MapDisk/g' ../MAPNET-MT/.github/workflows/testes.yml > .github/workflows/testes.yml
grep -n -i "mapnet" ferramentas/publicar.cmd .github/workflows/testes.yml || echo "sem referencia ao mapnet"
```

Esperado: `sem referencia ao mapnet`. Conferir que o CI fala em `mapdisk.sln`, `src/mapdisk/mapdisk.csproj` e `publicar/mapdisk.exe`, e que a Release só sai com marca `vX.Y.Z`.

- [ ] **Passo 2: gerar o `.exe` pelo roteiro**

Rodar: `ferramentas\publicar.cmd`
Esperado: testes verdes e `publicar\mapdisk.exe` gerado. Anotar o tamanho na conversa. O `publicar/` fica fora do git.

- [ ] **Passo 3: README com o uso e a situação**

Acrescentar ao `README.md`, antes de "Situação do projeto":

```markdown
## Uso

Baixe o `mapdisk.exe` e abra. Não precisa instalar nem ser administrador. Escolha a unidade, digite uma pasta ou um caminho de rede (`\\servidor\pasta`) e clique em **Varrer**.

Pastas que o Windows não deixa ler aparecem como **sem acesso**, e a barra de baixo diz quantas são. O total não inclui o que está nelas.

Linha de comando (só lê, nunca apaga nem move):

    mapdisk varrer C:
    mapdisk varrer \\servidor\dados --csv dados.csv --top 30

No Prompt de Comando, use `start /wait mapdisk varrer C:` para o prompt esperar o fim. No PowerShell, termine a linha com `| Out-Host`.
```

Na tabela "Situação do projeto", trocar `[PREENCHER]` pelo link do Pull Request do código, criado no passo 6.

- [ ] **Passo 4: pendências da fatia 1**

Acrescentar a `docs/superpowers/pendencias.md`:

```markdown
## Fatia 1: varredura, árvore e janela básica

| Item | Motivo | O que fecha |
|---|---|---|
| Lista dos últimos alvos (R1) | Fica para a fatia 2, junto com a navegação | Fatia 2 |
| Atualizar só uma ramificação (R2) | Fica para a fatia 2, com o menu de contexto | Fatia 2 |
| Botão "Varrer como administrador" (R7) | Fica para a fatia 2 | Fatia 2 |
| Ícone do `.exe` | Sem arte do MapDisk | Pendência "Ícone e logo do MapDisk" |
| Hard link em caminho de rede | O identificador vem do servidor e pode repetir entre discos dele. Em rede, o hard link soma mais de uma vez | Aceito. Reavaliar se aparecer caso real |
| Unidade de rede mapeada e desligada pode atrasar a lista de unidades | O `DriveInfo.IsReady` espera a rede responder | Aceito na fatia 1. Reavaliar se atrapalhar |
| Meta de desempenho (1 milhão de arquivos em menos de 60 s) | Precisa de um disco real grande, fora dos testes | Teste manual do Manfred no C: dele |
```

- [ ] **Passo 5: portões e commit**

```bash
dotnet build mapdisk.sln -c Release
dotnet test mapdisk.sln -c Release
git status --short
```

Fazer também a busca por menção a ferramenta de IA do portão 3 do `AGENTS.md`. Os nomes buscados ficam no comando digitado na hora, nunca escritos em arquivo do repositório.

Commit com a mensagem `Traz a publicacao, o CI e a documentacao da fatia 1`.

- [ ] **Passo 6: Pull Request do código**

Corpo em `.superpowers/rascunho/pr-fatia-1.md`:

```markdown
## O que muda

Primeira versão do MapDisk - MT (0.1.0): varre unidade, pasta ou caminho de rede e mostra a árvore de pastas com tamanho, alocado, arquivos, pastas, porcentagem e data, em quatro modos e quatro unidades. Pasta sem permissão aparece como "sem acesso", nunca como 0. Linha de comando com resumo e CSV. Esta versão só lê: nada é apagado nem movido.

## Como testar

1. Baixar o `mapdisk.exe` do CI deste PR (Artifacts) ou rodar `ferramentas\publicar.cmd`.
2. Abrir sem administrador e varrer o C:. Conferir que `Windows`, `ProgramData` e outras pastas protegidas aparecem como "sem acesso", com a contagem na barra de baixo.
3. Trocar os modos e as unidades, ordenar pelas colunas, abrir e fechar pastas.
4. Varrer uma pasta de rede (`\\servidor\pasta`).
5. No PowerShell: `.\mapdisk.exe varrer C: --csv c.csv | Out-Host` e abrir o CSV no Excel.

Autores: Manfred Heil Junior
```

```bash
gh pr create --base main --head varredura --title "Fatia 1: varredura, arvore e janela basica" --body-file .superpowers/rascunho/pr-fatia-1.md
gh pr checks
```

Ler o resultado do CI. Merge só depois do teste do Manfred e da frase "conferi tudo certo, pode juntar o PR #N".

---

## Conferência do plano contra a spec

| Requisito | Onde |
|---|---|
| R1 alvo local, pasta e rede, lista de unidades com livre e total | Tarefas 3, 8 e 10. Últimos alvos: pendência para a fatia 2 |
| R2 segundo plano, parcial na tela, Parar, Atualizar | Tarefas 6, 8 e 10. Ramificação: fatia 2 |
| R3 colunas e ordenação | Tarefas 7 e 10 |
| R4 modos e unidades em pt-BR | Tarefas 2, 7 e 10 |
| R6 "[N arquivos]" e arquivo do sistema | Tarefas 5 e 7 |
| R7 sem acesso nunca vira 0, contagem na barra | Tarefas 4, 5, 6, 7, 8 e 9. Botão de administrador: fatia 2 |
| R8 barra de status (livre, total, arquivos, sem acesso, cluster, sistema de arquivos) | Tarefa 8 |
| R18 linha de comando que varre e grava CSV, sem ações | Tarefa 9 |
| Regra 7: não ler conteúdo, não baixar da nuvem, teto de paralelismo | Tarefas 5 e 6 |
| Regra 9: hard link uma vez, junção não seguida, alocado do sistema | Tarefas 5 e 6 |
