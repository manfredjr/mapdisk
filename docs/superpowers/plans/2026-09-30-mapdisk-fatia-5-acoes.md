# Plano da fatia 5: ações seguras

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar. Esta é a fatia que mexe em arquivo do cliente: na dúvida, pare e pergunte.

**Objetivo:** o técnico seleciona pastas e arquivos na árvore ou nas listas de análise e, na própria janela, envia para a Lixeira, move para outro lugar ou, onde não há Lixeira, exclui definitivamente. Sempre com a lista, o total e o destino na confirmação, com proteção das pastas de sistema, com conferência antes de agir e com o registro de ações gravado antes de cada item.

**Arquitetura:** tudo que decide fica no núcleo, em `src/mapdisk.nucleo/acoes/`: proteção, itens, registro, operações no disco, executor e o preparador que transforma a seleção num pedido com os textos de confirmação. As operações no disco ficam atrás da interface `IOperacoesArquivo`: os testes usam as reais só dentro da pasta de saída dos testes, e o modo de demonstração usa uma versão que não toca no disco. A janela ganha os botões, os menus, a seleção múltipla, a janela de confirmação e a janela de andamento.

**Tecnologia:** C# com .NET 8, WPF, xUnit, `SHFileOperation` do Shell do Windows para a Lixeira. Sem pacote novo.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, R8, R9, R16, seções 7 e 8 e a fatia 5 da seção 11. Textos de confirmação: seção 5 de `docs/legal/verificacao-distribuicao-e-lgpd-2026-09-28.md`. Registro: seção 4.3 da mesma verificação.

## Restrições globais

- Regra 1 do `AGENTS.md`, inteira: mover, enviar para a Lixeira e excluir só pela janela, com a lista, o total e o destino na confirmação. Bloqueio de pasta de sistema e de raiz de unidade. No mover entre unidades, a origem só sai depois de a cópia ser conferida. Sem registro gravado, a ação não começa.
- Regra 2: a linha de comando continua sem mover nem apagar. Nenhuma mudança em `linha-de-comando/`.
- Regra 3: pasta sem leitura não entra numa ação, porque o tamanho dela é desconhecido.
- Regra 6: o registro fica só na máquina.
- Regra 7: arquivo só na nuvem nunca é baixado. O mover entre unidades recusa pasta que tenha arquivo só na nuvem.
- Testes: as operações reais rodam só dentro da pasta de saída dos testes, pelo `PastaTeste`. A Lixeira real não roda em teste automático, porque ela leva o arquivo para `C:\$Recycle.Bin`, fora da pasta do projeto.
- Nada de ação no computador do Manfred fora da pasta de teste sem pedido. O teste da Lixeira na janela é do Manfred.
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

As marcadas com **muda o desenho** ampliam ou ajustam a spec e precisam do aceite do Manfred neste Pull Request.

| Decisão | Escolha | Motivo |
|---|---|---|
| Onde agir | Na árvore (pastas e arquivos) e nas listas Maiores arquivos, Arquivos antigos e Por usuário. Seleção múltipla com Ctrl e Shift | Spec, seção 7, passo 1 |
| O que não é item | A linha "[N arquivos]" (abra o grupo e selecione os arquivos), a raiz da varredura quando é raiz de unidade e as linhas de Por tipo | A linha de grupo não é um arquivo nem uma pasta |
| Item dentro de item | Se uma pasta e algo dentro dela estão selecionados, só a pasta entra | Não agir duas vezes sobre o mesmo arquivo |
| Proteção | Raiz de unidade e de compartilhamento; `Windows`, `Program Files`, `Program Files (x86)` e `ProgramData` do sistema, e tudo dentro delas; `System Volume Information` e `$Recycle.Bin` em qualquer unidade; arquivos com o rótulo "arquivo do sistema" (R6); a pasta do registro de ações e as pastas acima dela | Spec, seção 7, passo 3. A última protege o próprio registro |
| Pasta sem leitura | Não entra: "sem leitura, atualize ou varra como administrador antes". Pasta lida com pastas sem leitura dentro entra, com aviso de que o total pode ser maior | Regra 3 |
| Link (junção) | Não entra: "é link, trate pelo Explorer" | O programa não segue link (regra 9) e não sabe o que há do outro lado |
| **Lixeira só em unidade fixa** (muda o desenho) | Enviar para a Lixeira só em unidade fixa local. Em caminho de rede, unidade mapeada, pendrive e disco removível, a ação vira "Excluir definitivamente", com `EXCLUIR` digitado | Nesses lugares o Windows costuma apagar direto, sem Lixeira. A spec cobria só a rede |
| Segunda rede de segurança da Lixeira | A chamada ao Windows leva o pedido de aviso quando o arquivo não couber na Lixeira: o próprio Windows pergunta antes de apagar direto | Arquivo maior que a Lixeira seria apagado sem aviso |
| Seleção que mistura lugares | Itens com e sem Lixeira na mesma seleção: bloqueado, "selecione um lugar de cada vez" | Um clique não pode virar exclusão definitiva de item local |
| Conferência na hora | Antes da confirmação: arquivo com tamanho e data iguais aos da varredura, pasta existente e com a mesma data. O que mudou aparece num aviso com a lista, e o técnico decide seguir ou não | Spec, seção 7, passo 5 |
| Destino do mover | O técnico escolhe a pasta. Bloqueado: destino dentro de um item, destino protegido, itens já nessa pasta, item com o mesmo nome no destino (nunca sobrescreve) e, entre unidades, "faltam X no destino" | Spec, seção 7, passos 2 e 5 |
| Mover entre unidades | Copia, confere tamanho de cada arquivo e a contagem, e só então apaga a origem. Falhou ou cancelou no meio: apaga só a cópia parcial, a origem fica. Pasta com link ou arquivo só na nuvem: recusado | Spec, seção 7, passo 6; regra 7 |
| Mover na mesma unidade | Renomeia o caminho, sem copiar | Spec |
| Arquivo somente leitura | Na exclusão definitiva e na limpeza da origem depois da cópia conferida, o atributo é tirado antes de apagar | O item já foi confirmado pelo técnico |
| Execução | Item por item, numa janela com barra, item atual e Cancelar. Falha de um item não para os outros. No fim, o resumo com o que deu certo e o que falhou, com o motivo | Spec, seção 7, passo 7 |
| Registro | `%LOCALAPPDATA%\MapDisk\acoes.log`, texto com tabulação, uma linha antes de cada item ("iniciado") e uma depois (resultado). Campos: data e hora, usuário do Windows, ação, origem, destino, bytes, resultado. Primeira linha com os nomes das colunas | Spec, seção 7, passo 9; verificação jurídica 4.3. A linha antes deixa rastro mesmo se o programa cair no meio |
| Sem registro, sem ação | Se a linha "iniciado" não grava, aquele item não começa e a execução para, com o aviso | Regra 1 |
| Onde ver o registro | Botão "Registro de ações" na barra de exibição, que abre a pasta do registro no Explorer. A tela de Opções da fatia 7 leva o botão para lá | Verificação jurídica 4.3 pede mostrar onde o registro fica |
| **"Nesta sessão" separado** (muda o desenho) | A barra de status mostra "Nesta sessão: X para a Lixeira, Y movidos, Z excluídos", só o que não for zero | O que vai para a Lixeira só libera espaço quando ela é esvaziada. "Liberado" seria número errado |
| Depois da ação | Os itens que deram certo saem da árvore e das somas, sem varrer de novo. No mover para pasta dentro da árvore, a pasta de destino é relida. As análises recalculam | Spec, seção 7, passo 8 |
| Pasta de perfil | Item que é pasta de perfil (dentro de `Users`) ganha na confirmação: "É a pasta de perfil de um usuário. Apagar a pasta não remove a conta do Windows." | Evita o técnico achar que removeu o usuário |
| **Demonstração sem disco** (muda o desenho) | Em `--demonstracao`, as ações seguem o fluxo inteiro na tela, mas não tocam no disco nem gravam registro. A confirmação avisa "Modo de demonstração: nada é apagado nem movido" | Os nomes fictícios podem existir de verdade na máquina |
| Durante a varredura | Botões de ação desligados | A árvore ainda muda |
| Teclado | Delete na árvore ou nas listas abre a confirmação de Lixeira ou de exclusão | Hábito do Explorer |

## Git desta fatia

1. O plano entra pelo ramo `fatia-5-acoes`, num Pull Request só do plano, junto com a linha da fatia 5 na "Situação do projeto" do README.
2. O código sai do `main` atualizado, no ramo `acoes`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/acoes/protecao.cs` | Locais protegidos e o motivo do bloqueio |
| `src/mapdisk.nucleo/acoes/item-acao.cs` | Item de uma ação, tipo de ação e onde há Lixeira |
| `src/mapdisk.nucleo/acoes/registro-acoes.cs` | Registro de ações em arquivo |
| `src/mapdisk.nucleo/acoes/operacoes-arquivo.cs` | Operações reais no disco: conferência, espaço livre, Lixeira, exclusão e mover |
| `src/mapdisk.nucleo/acoes/operacoes-demonstracao.cs` | Operações e registro que não tocam no disco |
| `src/mapdisk.nucleo/acoes/executor-acoes.cs` | Pedido, execução item por item, registro e resumo |
| `src/mapdisk.nucleo/acoes/preparador-acoes.cs` | Seleção em itens, bloqueios, tipo de remoção, destino e textos da confirmação |
| `src/mapdisk.nucleo/arvore/no-pasta.cs` | Tirar subpasta e arquivo da árvore, achar pasta pelo caminho |
| `src/mapdisk.nucleo/painel/arvore-visivel.cs` | Tirar da tela a pasta removida |
| `src/mapdisk.nucleo/painel/painel-principal.cs` e `dependencias-painel.cs` | Estado das ações, execução e o que muda depois |
| `src/mapdisk.nucleo/painel/demonstracao.cs` | Painel de demonstração com as operações que não tocam no disco |
| `src/mapdisk/janela-confirmacao.xaml` e `.cs` | Confirmação com lista, total, destino, avisos e `EXCLUIR` |
| `src/mapdisk/janela-andamento.xaml` e `.cs` | Barra, Cancelar e resumo |
| `src/mapdisk/janela-principal.xaml` e `.cs` | Botões, menus, seleção múltipla, Delete e barra de status |
| `testes/mapdisk.testes/acoes-testes.cs` | Testes do núcleo desta fatia |

---

### Tarefa 1: proteção

**Arquivos:** novo `src/mapdisk.nucleo/acoes/protecao.cs`. Teste: novo `testes/mapdisk.testes/acoes-testes.cs`.

**Interfaces:**
- Consome: `Volumes.RaizDe(string)`, `MarcaArquivo`.
- Produz: `sealed record LocaisProtegidos(IReadOnlyList<string> Pastas, string PastaRegistro)` com `static LocaisProtegidos DoSistema()`; `static class Protecao` com `string? Motivo(string caminho, bool ehPasta, MarcaArquivo marcas, LocaisProtegidos locais)` e `bool Dentro(string caminho, string pasta)` (igual ou abaixo, sem diferença de maiúsculas).

- [ ] **Passo 1: criar o ramo do código**

```bash
git checkout main
git pull
git checkout -b acoes
```

- [ ] **Passo 2: escrever os testes**

```csharp
namespace MapDisk.Testes;

public class AcoesTestes
{
    internal static readonly LocaisProtegidos Protegidos = new(
        [@"C:\Windows", @"C:\Program Files", @"C:\Program Files (x86)", @"C:\ProgramData"],
        @"C:\Users\tecnico\AppData\Local\MapDisk");

    [Theory]
    [InlineData(@"C:\", true, "É a raiz da unidade.")]
    [InlineData(@"\\servidor\dados", true, "É a raiz da unidade.")]
    [InlineData(@"C:\Windows", true, @"É pasta do sistema (C:\Windows).")]
    [InlineData(@"C:\Windows\System32\drivers", true, @"É pasta do sistema (C:\Windows).")]
    [InlineData(@"C:\ProgramData\app\log.txt", false, @"É pasta do sistema (C:\ProgramData).")]
    [InlineData(@"D:\System Volume Information", true, "É pasta do sistema (System Volume Information).")]
    [InlineData(@"D:\$Recycle.Bin\S-1-5-21\x.txt", false, "É pasta do sistema ($Recycle.Bin).")]
    [InlineData(@"C:\Users\tecnico", true, "Contém o registro de ações do MapDisk.")]
    [InlineData(@"C:\Users", true, "Contém o registro de ações do MapDisk.")]
    public void Bloqueia_o_que_e_do_sistema(string caminho, bool ehPasta, string motivo)
    {
        Assert.Equal(motivo, Protecao.Motivo(caminho, ehPasta, MarcaArquivo.Nenhuma, Protegidos));
    }

    [Theory]
    [InlineData(@"C:\WindowsAntigo", true)]
    [InlineData(@"C:\Users\ana", true)]
    [InlineData(@"D:\Dados\video.mp4", false)]
    [InlineData(@"\\servidor\dados\backup", true)]
    public void Libera_o_resto(string caminho, bool ehPasta)
    {
        Assert.Null(Protecao.Motivo(caminho, ehPasta, MarcaArquivo.Nenhuma, Protegidos));
    }

    [Fact]
    public void Bloqueia_arquivo_do_sistema_pelo_rotulo()
    {
        Assert.Equal("É arquivo do sistema.", Protecao.Motivo(@"C:\pagefile.sys", false, MarcaArquivo.Sistema, Protegidos));
    }
}
```

- [ ] **Passo 3: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter AcoesTestes`
Expected: FAIL na compilação, `LocaisProtegidos` não existe.

- [ ] **Passo 4: implementar**

```csharp
namespace MapDisk.Nucleo;

/// <summary>Pastas do sistema e a pasta do registro de ações. Os testes passam os seus.</summary>
public sealed record LocaisProtegidos(IReadOnlyList<string> Pastas, string PastaRegistro)
{
    public static LocaisProtegidos DoSistema() => new(
        new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        }.Where(p => p.Length > 0).ToList(),
        RegistroAcoes.PastaPadrao);
}

/// <summary>O que o programa nunca move nem apaga (regra 1 e spec, seção 7, passo 3).</summary>
public static class Protecao
{
    private static readonly string[] NomesDoSistema = ["System Volume Information", "$Recycle.Bin"];

    /// <summary>O motivo do bloqueio, para o botão e a confirmação. Null quando pode agir.</summary>
    public static string? Motivo(string caminho, bool ehPasta, MarcaArquivo marcas, LocaisProtegidos locais)
    {
        var c = Limpo(caminho);
        if (ehPasta && string.Equals(c, Limpo(Volumes.RaizDe(caminho)), StringComparison.OrdinalIgnoreCase))
        {
            return "É a raiz da unidade.";
        }

        if ((marcas & MarcaArquivo.Sistema) != 0)
        {
            return "É arquivo do sistema.";
        }

        foreach (var nome in NomesDoSistema)
        {
            if (c.Split('\\').Any(parte => string.Equals(parte, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return $"É pasta do sistema ({nome}).";
            }
        }

        foreach (var pasta in locais.Pastas)
        {
            if (Dentro(c, pasta))
            {
                return $"É pasta do sistema ({Limpo(pasta)}).";
            }
        }

        return Dentro(locais.PastaRegistro, c) ? "Contém o registro de ações do MapDisk." : null;
    }

    /// <summary>O caminho é a própria pasta ou fica abaixo dela.</summary>
    public static bool Dentro(string caminho, string pasta)
    {
        var c = Limpo(caminho);
        var p = Limpo(pasta);
        return string.Equals(c, p, StringComparison.OrdinalIgnoreCase)
            || c.StartsWith(p + @"\", StringComparison.OrdinalIgnoreCase);
    }

    // Sem a barra do fim: "C:\" vira "C:" e "\\srv\dados\" vira "\\srv\dados".
    private static string Limpo(string caminho) => caminho.TrimEnd('\\');
}
```

`RegistroAcoes.PastaPadrao` vem na tarefa 3. Para compilar esta tarefa, criar já em `src/mapdisk.nucleo/acoes/registro-acoes.cs`:

```csharp
namespace MapDisk.Nucleo;

public sealed class RegistroAcoes
{
    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk");
}
```

- [ ] **Passo 5: rodar e ver passar**

Run: `dotnet test mapdisk.sln -c Release --filter AcoesTestes`
Expected: PASS, 14 casos.

- [ ] **Passo 6: commit** `Cria a protecao das pastas do sistema`, com `protecao.cs`, `registro-acoes.cs` e `acoes-testes.cs`.

---

### Tarefa 2: item de ação e onde há Lixeira

**Arquivos:** novo `src/mapdisk.nucleo/acoes/item-acao.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Consome: `NoPasta`, `ArquivoInfo`, `Alvo.EhRede`, `Volumes.RaizDe`.
- Produz: `enum TipoAcao { Lixeira, Mover, Excluir }`; `sealed record ItemAcao(string Caminho, bool EhPasta, long Tamanho, DateTime Modificacao, MarcaArquivo Marcas, NoPasta Pasta, ArquivoInfo? Arquivo)` com `string Nome`, `static ItemAcao DaPasta(NoPasta)` e `static ItemAcao DoArquivo(NoPasta pai, ArquivoInfo)`. Em item de pasta, `Pasta` é a própria pasta; em item de arquivo, a pasta onde ele está. `static class Lixeiras` com `bool Existe(string caminho, Func<string, DriveType> tipoDaUnidade)` e `DriveType TipoDaUnidade(string raiz)`.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Item_de_pasta_e_de_arquivo_tem_caminho_tamanho_e_data()
    {
        var raiz = AnalisesTestes.Exemplo();
        var bruno = raiz.Subpastas[0].Subpastas[1];

        var pasta = ItemAcao.DaPasta(bruno);
        var arquivo = ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]);

        Assert.Equal(@"C:\Users\bruno", pasta.Caminho);
        Assert.True(pasta.EhPasta);
        Assert.Equal(2100, pasta.Tamanho);
        Assert.Equal("bruno", pasta.Nome);
        Assert.Equal(@"C:\Users\bruno\caixa.pst", arquivo.Caminho);
        Assert.False(arquivo.EhPasta);
        Assert.Equal(2000, arquivo.Tamanho);
        Assert.Equal(new DateTime(2025, 6, 1), arquivo.Modificacao);
        Assert.Same(bruno, arquivo.Pasta);
    }

    [Theory]
    [InlineData(@"C:\Dados", DriveType.Fixed, true)]
    [InlineData(@"E:\fotos", DriveType.Removable, false)]
    [InlineData(@"Z:\compartilhado", DriveType.Network, false)]
    [InlineData(@"\\servidor\dados\x", DriveType.Fixed, false)]
    public void Lixeira_so_em_unidade_fixa(string caminho, DriveType tipo, bool temLixeira)
    {
        Assert.Equal(temLixeira, Lixeiras.Existe(caminho, _ => tipo));
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter AcoesTestes`
Expected: FAIL na compilação, `ItemAcao` não existe.

- [ ] **Passo 3: implementar**

```csharp
namespace MapDisk.Nucleo;

public enum TipoAcao
{
    Lixeira,
    Mover,
    Excluir,
}

/// <summary>
/// Um item de uma ação, com o tamanho e a data da varredura, para a conferência na hora.
/// Em item de pasta, Pasta é a própria pasta. Em item de arquivo, a pasta onde ele está.
/// </summary>
public sealed record ItemAcao(string Caminho, bool EhPasta, long Tamanho, DateTime Modificacao, MarcaArquivo Marcas, NoPasta Pasta, ArquivoInfo? Arquivo)
{
    public string Nome => EhPasta ? Pasta.Nome : Arquivo!.Value.Nome;

    public static ItemAcao DaPasta(NoPasta pasta) =>
        new(pasta.CaminhoCompleto(), true, pasta.Tamanho, pasta.ModificacaoPropria, MarcaArquivo.Nenhuma, pasta, null);

    public static ItemAcao DoArquivo(NoPasta pai, ArquivoInfo arquivo) =>
        new(Path.Combine(pai.CaminhoCompleto(), arquivo.Nome), false, arquivo.Tamanho, arquivo.Modificacao, arquivo.Marcas, pai, arquivo);
}

/// <summary>Onde o Windows garante a Lixeira: só em unidade fixa local.</summary>
public static class Lixeiras
{
    public static bool Existe(string caminho, Func<string, DriveType> tipoDaUnidade) =>
        !Alvo.EhRede(caminho) && tipoDaUnidade(Volumes.RaizDe(caminho)) == DriveType.Fixed;

    public static DriveType TipoDaUnidade(string raiz)
    {
        try
        {
            return new DriveInfo(raiz).DriveType;
        }
        catch (ArgumentException)
        {
            return DriveType.Unknown;
        }
    }
}
```

- [ ] **Passo 4: rodar e ver passar.** Run: `dotnet test mapdisk.sln -c Release --filter AcoesTestes`. Expected: PASS.

- [ ] **Passo 5: commit** `Cria o item de acao e a regra da Lixeira`.

---

### Tarefa 3: registro de ações

**Arquivos:** `src/mapdisk.nucleo/acoes/registro-acoes.cs`. O arquivo inteiro passa a ser o do passo 3, que mantém a `PastaPadrao` da tarefa 1. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Produz: `interface IRegistroAcoes { string Local { get; } void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado); }`; `sealed class RegistroAcoes : IRegistroAcoes` com `RegistroAcoes(string arquivo, Func<DateTime> agora, string usuario)`, `static RegistroAcoes Padrao()` e `const string Cabecalho`. `Gravar` lança `IOException` ou `UnauthorizedAccessException` quando não consegue gravar.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Registro_grava_cabecalho_e_uma_linha_por_chamada()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho(@"registro\acoes.log");
        var registro = new RegistroAcoes(arquivo, () => new DateTime(2026, 9, 30, 14, 5, 9), @"EMPRESA\tecnico");

        registro.Gravar(TipoAcao.Mover, @"D:\Dados\a.iso", @"E:\Arquivo", 4096, "iniciado");
        registro.Gravar(TipoAcao.Mover, @"D:\Dados\a.iso", @"E:\Arquivo", 4096, "ok");

        var linhas = File.ReadAllLines(arquivo);
        Assert.Equal(RegistroAcoes.Cabecalho, linhas[0]);
        Assert.Equal("2026-09-30 14:05:09\tEMPRESA\\tecnico\tmover\tD:\\Dados\\a.iso\tE:\\Arquivo\t4096\tiniciado", linhas[1]);
        Assert.EndsWith("\tok", linhas[2]);
        Assert.Equal(arquivo, registro.Local);
    }

    [Fact]
    public void Registro_tira_quebra_de_linha_e_tabulacao_do_motivo()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Caminho("acoes.log");
        var registro = new RegistroAcoes(arquivo, () => DateTime.Now, "tecnico");

        registro.Gravar(TipoAcao.Lixeira, @"C:\x", null, 1, "falhou: em uso\r\npor outro\tprograma");

        Assert.EndsWith("\t\t1\tfalhou: em uso  por outro programa", File.ReadAllLines(arquivo)[1]);
    }

    [Fact]
    public void Registro_sem_gravacao_possivel_lanca_erro()
    {
        using var pasta = new PastaTeste();
        var bloqueio = pasta.Arquivo("acoes.log", 0);
        using var aberto = new FileStream(bloqueio, FileMode.Open, FileAccess.Read, FileShare.None);
        var registro = new RegistroAcoes(bloqueio, () => DateTime.Now, "tecnico");

        Assert.Throws<IOException>(() => registro.Gravar(TipoAcao.Excluir, @"\\srv\d\x", null, 1, "iniciado"));
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação, construtor e `Gravar` não existem.

- [ ] **Passo 3: implementar**

```csharp
using System.Globalization;
using System.Text;

namespace MapDisk.Nucleo;

/// <summary>Registro das ações. Quem chama não age se a gravação falhar (regra 1).</summary>
public interface IRegistroAcoes
{
    string Local { get; }

    void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado);
}

/// <summary>
/// Uma linha por chamada, com tabulação entre os campos, para abrir no Excel. Guarda só o que a
/// verificação jurídica pede (seção 4.3): data e hora, usuário do Windows, ação, origem,
/// destino, bytes e resultado.
/// </summary>
public sealed class RegistroAcoes : IRegistroAcoes
{
    public const string Cabecalho = "data e hora\tusuário\tação\torigem\tdestino\tbytes\tresultado";

    private static readonly UTF8Encoding SemBom = new(false);
    private readonly Func<DateTime> _agora;
    private readonly string _usuario;

    public RegistroAcoes(string arquivo, Func<DateTime> agora, string usuario)
    {
        Local = arquivo;
        _agora = agora;
        _usuario = usuario;
    }

    public string Local { get; }

    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk");

    public static RegistroAcoes Padrao() => new(
        Path.Combine(PastaPadrao, "acoes.log"),
        () => DateTime.Now,
        $@"{Environment.UserDomainName}\{Environment.UserName}");

    public void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Local)!);
        var linha = string.Join('\t',
            _agora().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            _usuario,
            acao switch { TipoAcao.Lixeira => "lixeira", TipoAcao.Mover => "mover", _ => "excluir" },
            origem,
            destino ?? string.Empty,
            bytes.ToString(CultureInfo.InvariantCulture),
            Limpo(resultado));
        var texto = File.Exists(Local) ? linha + Environment.NewLine : Cabecalho + Environment.NewLine + linha + Environment.NewLine;
        File.AppendAllText(Local, texto, SemBom);
    }

    private static string Limpo(string texto) => texto.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
```

O teste do motivo espera dois espaços entre "em uso" e "por", porque `\r\n` vira dois espaços.

- [ ] **Passo 4: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 5: commit** `Cria o registro de acoes`.

---

### Tarefa 4: operações no disco

**Arquivos:** novos `src/mapdisk.nucleo/acoes/operacoes-arquivo.cs` e `src/mapdisk.nucleo/acoes/operacoes-demonstracao.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Consome: `ItemAcao`, `Volumes.RaizDe`, `Volumes.Ler`.
- Produz: `interface IOperacoesArquivo` com `string? Mudanca(ItemAcao item)`, `long? Livre(string pasta)`, `bool MesmoVolume(string origem, string pastaDestino)`, `void EnviarParaLixeira(string caminho)`, `void ExcluirDefinitivo(string caminho, bool ehPasta)` e `void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar)`; `sealed partial class OperacoesArquivo : IOperacoesArquivo` com `OperacoesArquivo(Func<string, string, bool>? mesmoVolume = null)`; `sealed class OperacoesDemonstracao : IOperacoesArquivo` e `sealed class RegistroEmMemoria : IRegistroAcoes` com `List<string> Linhas`.

- [ ] **Passo 1: escrever os testes**

```csharp
    private static ItemAcao ItemDoDisco(string caminho)
    {
        var raiz = new NoPasta(Path.GetDirectoryName(caminho)!, null);
        if (Directory.Exists(caminho))
        {
            var no = new NoPasta(Path.GetFileName(caminho), raiz, Directory.GetLastWriteTime(caminho));
            raiz.Preencher([], [no]);
            no.Preencher([], []);
            return ItemAcao.DaPasta(no);
        }

        var info = new FileInfo(caminho);
        var arquivo = new ArquivoInfo(info.Name, info.Length, info.Length, info.LastWriteTime, MarcaArquivo.Nenhuma);
        raiz.Preencher([arquivo], []);
        return ItemAcao.DoArquivo(raiz, arquivo);
    }

    [Fact]
    public void Conferencia_ve_arquivo_que_mudou_ou_sumiu()
    {
        using var pasta = new PastaTeste();
        var caminho = pasta.Arquivo("a.bin", 100, new DateTime(2025, 1, 1));
        var item = ItemDoDisco(caminho);
        var ops = new OperacoesArquivo();

        Assert.Null(ops.Mudanca(item));
        File.WriteAllBytes(caminho, new byte[150]);
        Assert.Equal("mudou desde a varredura", ops.Mudanca(item));
        File.Delete(caminho);
        Assert.Equal("não existe mais", ops.Mudanca(item));
    }

    [Fact]
    public void Mover_na_mesma_unidade_renomeia()
    {
        using var pasta = new PastaTeste();
        pasta.Arquivo(@"origem\docs\a.txt", 10);
        var destino = pasta.Pasta("destino");

        new OperacoesArquivo().Mover(pasta.Caminho(@"origem\docs"), destino, true, CancellationToken.None);

        Assert.False(Directory.Exists(pasta.Caminho(@"origem\docs")));
        Assert.Equal(10, new FileInfo(pasta.Caminho(@"destino\docs\a.txt")).Length);
    }

    [Fact]
    public void Mover_entre_unidades_copia_confere_e_so_depois_apaga_a_origem()
    {
        using var pasta = new PastaTeste();
        pasta.Arquivo(@"origem\docs\a.txt", 10);
        pasta.Arquivo(@"origem\docs\sub\b.txt", 2000);
        File.SetAttributes(pasta.Caminho(@"origem\docs\a.txt"), FileAttributes.ReadOnly);
        var destino = pasta.Pasta("destino");
        var ops = new OperacoesArquivo(mesmoVolume: (_, _) => false);

        ops.Mover(pasta.Caminho(@"origem\docs"), destino, true, CancellationToken.None);

        Assert.False(Directory.Exists(pasta.Caminho(@"origem\docs")));
        Assert.Equal(10, new FileInfo(pasta.Caminho(@"destino\docs\a.txt")).Length);
        Assert.Equal(2000, new FileInfo(pasta.Caminho(@"destino\docs\sub\b.txt")).Length);
        File.SetAttributes(pasta.Caminho(@"destino\docs\a.txt"), FileAttributes.Normal);
    }

    [Fact]
    public void Mover_nunca_sobrescreve_no_destino()
    {
        using var pasta = new PastaTeste();
        pasta.Arquivo(@"origem\a.txt", 10);
        pasta.Arquivo(@"destino\a.txt", 99);

        var erro = Assert.Throws<IOException>(() =>
            new OperacoesArquivo().Mover(pasta.Caminho(@"origem\a.txt"), pasta.Caminho("destino"), false, CancellationToken.None));

        Assert.Equal("Já existe um item com esse nome no destino.", erro.Message);
        Assert.True(File.Exists(pasta.Caminho(@"origem\a.txt")));
        Assert.Equal(99, new FileInfo(pasta.Caminho(@"destino\a.txt")).Length);
    }

    [Fact]
    public void Mover_cancelado_apaga_so_a_copia_parcial()
    {
        using var pasta = new PastaTeste();
        pasta.Arquivo(@"origem\docs\a.txt", 10);
        var destino = pasta.Pasta("destino");
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new OperacoesArquivo(mesmoVolume: (_, _) => false).Mover(pasta.Caminho(@"origem\docs"), destino, true, cancelar.Token));

        Assert.True(File.Exists(pasta.Caminho(@"origem\docs\a.txt")));
        Assert.False(Directory.Exists(pasta.Caminho(@"destino\docs")));
    }

    [Fact]
    public void Mover_entre_unidades_recusa_pasta_com_link()
    {
        using var pasta = new PastaTeste();
        pasta.Arquivo(@"alvo\x.txt", 5);
        pasta.Pasta(@"origem\docs");
        pasta.Juncao(@"origem\docs\atalho", "alvo");

        var erro = Assert.Throws<IOException>(() =>
            new OperacoesArquivo(mesmoVolume: (_, _) => false).Mover(pasta.Caminho(@"origem\docs"), pasta.Pasta("destino"), true, CancellationToken.None));

        Assert.StartsWith("Contém link ou arquivo só na nuvem", erro.Message);
        Assert.True(Directory.Exists(pasta.Caminho(@"origem\docs\atalho")));
        Assert.False(Directory.Exists(pasta.Caminho(@"destino\docs")));
    }

    [Fact]
    public void Excluir_definitivo_apaga_pasta_com_arquivo_somente_leitura()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Arquivo(@"velho\a.txt", 10);
        File.SetAttributes(arquivo, FileAttributes.ReadOnly);

        new OperacoesArquivo().ExcluirDefinitivo(pasta.Caminho("velho"), true);

        Assert.False(Directory.Exists(pasta.Caminho("velho")));
    }

    [Fact]
    public void Demonstracao_nao_toca_no_disco()
    {
        using var pasta = new PastaTeste();
        var arquivo = pasta.Arquivo("a.txt", 10);
        var ops = new OperacoesDemonstracao();

        ops.EnviarParaLixeira(arquivo);
        ops.ExcluirDefinitivo(arquivo, false);
        ops.Mover(arquivo, pasta.Pasta("destino"), false, CancellationToken.None);

        Assert.True(File.Exists(arquivo));
        Assert.Null(ops.Mudanca(ItemDoDisco(arquivo)));
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar as operações reais**

```csharp
using System.Runtime.InteropServices;

namespace MapDisk.Nucleo;

/// <summary>O que as ações fazem no disco. Os testes e a demonstração trocam por versões próprias.</summary>
public interface IOperacoesArquivo
{
    /// <summary>O que mudou desde a varredura, ou null quando está igual.</summary>
    string? Mudanca(ItemAcao item);

    /// <summary>Espaço livre no volume da pasta, ou null quando o volume não responde.</summary>
    long? Livre(string pasta);

    bool MesmoVolume(string origem, string pastaDestino);

    void EnviarParaLixeira(string caminho);

    void ExcluirDefinitivo(string caminho, bool ehPasta);

    void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar);
}

public sealed partial class OperacoesArquivo : IOperacoesArquivo
{
    // Atributos que dizem que o arquivo está só na nuvem: copiar forçaria o download (regra 7).
    private const FileAttributes SoNaNuvem = FileAttributes.Offline | (FileAttributes)0x00040000 | (FileAttributes)0x00400000;

    private const uint ApagarArquivo = 3;
    private const ushort Silencioso = 0x0004;
    private const ushort SemConfirmacao = 0x0010;
    private const ushort PermitirDesfazer = 0x0040;
    private const ushort SemTelaDeErro = 0x0400;
    private const ushort AvisarSeApagarDireto = 0x4000;

    private readonly Func<string, string, bool> _mesmoVolume;

    public OperacoesArquivo(Func<string, string, bool>? mesmoVolume = null)
    {
        _mesmoVolume = mesmoVolume ?? ((a, b) => string.Equals(Volumes.RaizDe(a), Volumes.RaizDe(b), StringComparison.OrdinalIgnoreCase));
    }

    public string? Mudanca(ItemAcao item)
    {
        if (item.EhPasta)
        {
            var pasta = new DirectoryInfo(item.Caminho);
            if (!pasta.Exists)
            {
                return "não existe mais";
            }

            // A raiz da varredura não tem data da varredura: confere só se existe.
            return item.Modificacao != default && pasta.LastWriteTime != item.Modificacao ? "mudou desde a varredura" : null;
        }

        var arquivo = new FileInfo(item.Caminho);
        if (!arquivo.Exists)
        {
            return "não existe mais";
        }

        return arquivo.Length != item.Tamanho || arquivo.LastWriteTime != item.Modificacao ? "mudou desde a varredura" : null;
    }

    public long? Livre(string pasta) => Volumes.Ler(pasta)?.Livre;

    public bool MesmoVolume(string origem, string pastaDestino) => _mesmoVolume(origem, pastaDestino);

    /// <summary>
    /// Lixeira pelo Shell do Windows. O pedido de aviso faz o próprio Windows perguntar antes de
    /// apagar direto, quando o item não cabe na Lixeira. Roda numa thread STA (ExecutorAcoes).
    /// </summary>
    public void EnviarParaLixeira(string caminho)
    {
        var operacao = new OperacaoShell
        {
            Funcao = ApagarArquivo,
            De = caminho + "\0\0",
            Opcoes = (ushort)(Silencioso | SemConfirmacao | PermitirDesfazer | SemTelaDeErro | AvisarSeApagarDireto),
        };
        var codigo = SHFileOperation(ref operacao);
        if (operacao.Abortada)
        {
            throw new OperationCanceledException("Cancelado no aviso do Windows.");
        }

        if (codigo != 0)
        {
            throw new IOException($"O Windows não enviou para a Lixeira (código {codigo}).");
        }
    }

    public void ExcluirDefinitivo(string caminho, bool ehPasta)
    {
        if (!ehPasta)
        {
            File.SetAttributes(caminho, FileAttributes.Normal);
            File.Delete(caminho);
            return;
        }

        RecusarLinkOuNuvem(caminho);
        foreach (var arquivo in Directory.EnumerateFiles(caminho, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(arquivo) & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(arquivo, FileAttributes.Normal);
            }
        }

        Directory.Delete(caminho, recursive: true);
    }

    public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar)
    {
        var destino = Path.Combine(pastaDestino, Path.GetFileName(origem.TrimEnd('\\')));
        if (File.Exists(destino) || Directory.Exists(destino))
        {
            throw new IOException("Já existe um item com esse nome no destino.");
        }

        if (MesmoVolume(origem, pastaDestino))
        {
            if (ehPasta)
            {
                Directory.Move(origem, destino);
            }
            else
            {
                File.Move(origem, destino);
            }

            return;
        }

        if (ehPasta)
        {
            RecusarLinkOuNuvem(origem);
        }

        try
        {
            if (ehPasta)
            {
                CopiarPasta(origem, destino, cancelar);
            }
            else
            {
                cancelar.ThrowIfCancellationRequested();
                File.Copy(origem, destino);
            }

            Conferir(origem, destino, ehPasta);
        }
        catch
        {
            ApagarCopia(destino, ehPasta);
            throw;
        }

        // A cópia foi conferida. Só agora a origem sai.
        try
        {
            ExcluirDefinitivo(origem, ehPasta);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"A cópia está em {destino}, mas a origem não saiu por inteiro: {e.Message}", e);
        }
    }

    private static void RecusarLinkOuNuvem(string pasta)
    {
        foreach (var entrada in new DirectoryInfo(pasta).EnumerateFileSystemInfos("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
        {
            if ((entrada.Attributes & (FileAttributes.ReparsePoint | SoNaNuvem)) != 0)
            {
                throw new IOException($"Contém link ou arquivo só na nuvem ({entrada.FullName}). Trate esta pasta pelo Explorer.");
            }
        }
    }

    private static void CopiarPasta(string origem, string destino, CancellationToken cancelar)
    {
        Directory.CreateDirectory(destino);
        foreach (var arquivo in Directory.EnumerateFiles(origem))
        {
            cancelar.ThrowIfCancellationRequested();
            File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)));
        }

        foreach (var sub in Directory.EnumerateDirectories(origem))
        {
            cancelar.ThrowIfCancellationRequested();
            CopiarPasta(sub, Path.Combine(destino, Path.GetFileName(sub)), cancelar);
        }
    }

    // Mesma quantidade de arquivos e mesmo tamanho em cada um.
    private static void Conferir(string origem, string destino, bool ehPasta)
    {
        if (!ehPasta)
        {
            if (new FileInfo(origem).Length != new FileInfo(destino).Length)
            {
                throw new IOException("A cópia não ficou do mesmo tamanho da origem.");
            }

            return;
        }

        var arquivos = Directory.GetFiles(origem, "*", SearchOption.AllDirectories);
        if (arquivos.Length != Directory.GetFiles(destino, "*", SearchOption.AllDirectories).Length)
        {
            throw new IOException("A cópia não ficou com a mesma quantidade de arquivos da origem.");
        }

        foreach (var arquivo in arquivos)
        {
            var copia = Path.Combine(destino, Path.GetRelativePath(origem, arquivo));
            if (!File.Exists(copia) || new FileInfo(copia).Length != new FileInfo(arquivo).Length)
            {
                throw new IOException($"A cópia de {arquivo} não confere.");
            }
        }
    }

    // A cópia parcial é do próprio programa: apagar não perde dado do cliente.
    private static void ApagarCopia(string destino, bool ehPasta)
    {
        try
        {
            if (ehPasta && Directory.Exists(destino))
            {
                Directory.Delete(destino, recursive: true);
            }
            else if (!ehPasta && File.Exists(destino))
            {
                File.Delete(destino);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Fica para o técnico: a origem está intacta.
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OperacaoShell
    {
        public nint Janela;
        public uint Funcao;
        public string De;
        public string? Para;
        public ushort Opcoes;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Abortada;
        public nint Nomes;
        public string? Titulo;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref OperacaoShell operacao);
}
```

Se o analisador pedir `LibraryImport` para `SHFileOperation` e o build falhar por aviso, suprimir só ali com `#pragma warning disable SYSLIB1054` e um comentário: a estrutura tem texto, que o `LibraryImport` não converte sozinho.

- [ ] **Passo 4: implementar a demonstração**

```csharp
namespace MapDisk.Nucleo;

/// <summary>Modo de demonstração: o fluxo inteiro na tela, sem tocar no disco.</summary>
public sealed class OperacoesDemonstracao : IOperacoesArquivo
{
    private const long Gb = 1024L * 1024 * 1024;

    public string? Mudanca(ItemAcao item) => null;

    public long? Livre(string pasta) => 500 * Gb;

    public bool MesmoVolume(string origem, string pastaDestino) =>
        string.Equals(Volumes.RaizDe(origem), Volumes.RaizDe(pastaDestino), StringComparison.OrdinalIgnoreCase);

    public void EnviarParaLixeira(string caminho)
    {
    }

    public void ExcluirDefinitivo(string caminho, bool ehPasta)
    {
    }

    public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar)
    {
    }
}

/// <summary>Registro da demonstração e dos testes: fica na memória.</summary>
public sealed class RegistroEmMemoria : IRegistroAcoes
{
    public List<string> Linhas { get; } = [];

    public string Local => "memória (modo de demonstração)";

    public void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado) =>
        Linhas.Add($"{acao}|{origem}|{destino}|{bytes}|{resultado}");
}
```

- [ ] **Passo 5: rodar e ver passar.** Run: `dotnet test mapdisk.sln -c Release --filter AcoesTestes`. Expected: PASS. A junção do teste de link sai no `Dispose` do `PastaTeste`.

- [ ] **Passo 6: commit** `Cria as operacoes de acao no disco`.

---

### Tarefa 5: executor

**Arquivos:** novo `src/mapdisk.nucleo/acoes/executor-acoes.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Consome: `IOperacoesArquivo`, `IRegistroAcoes`, `ItemAcao`, `TipoAcao`.
- Produz: `sealed record PedidoAcao(TipoAcao Acao, IReadOnlyList<ItemAcao> Itens, string? Destino)` com `long Total`; `sealed record ResultadoItem(ItemAcao Item, bool Ok, string Resultado)`; `sealed record ResumoAcao(IReadOnlyList<ResultadoItem> Resultados, bool Cancelada, bool RegistroFalhou)` com `long BytesOk`, `int Ok` e `int Falhas`; `readonly record struct ProgressoAcao(int Feitos, int Total, string Atual)`; `sealed class ExecutorAcoes` com `ExecutorAcoes(IOperacoesArquivo, IRegistroAcoes)`, `ResumoAcao Executar(PedidoAcao, IProgress<ProgressoAcao>?, CancellationToken)` e `Task<ResumoAcao> ExecutarAsync(...)`, que roda o `Executar` numa thread STA.

- [ ] **Passo 1: escrever os testes**

```csharp
    private sealed class OperacoesFalsas : IOperacoesArquivo
    {
        public List<string> Feitas { get; } = [];

        public HashSet<string> FalharEm { get; } = [];

        public string? Mudanca(ItemAcao item) => null;

        public long? Livre(string pasta) => long.MaxValue;

        public bool MesmoVolume(string origem, string pastaDestino) => true;

        public void EnviarParaLixeira(string caminho) => Fazer("lixeira", caminho);

        public void ExcluirDefinitivo(string caminho, bool ehPasta) => Fazer("excluir", caminho);

        public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar) => Fazer("mover", origem);

        private void Fazer(string acao, string caminho)
        {
            if (FalharEm.Contains(caminho))
            {
                throw new IOException("em uso");
            }

            Feitas.Add($"{acao} {caminho}");
        }
    }

    private sealed class RegistroQueFalha : IRegistroAcoes
    {
        public string Local => "nenhum";

        public void Gravar(TipoAcao acao, string origem, string? destino, long bytes, string resultado) =>
            throw new IOException("disco cheio");
    }

    private static IReadOnlyList<ItemAcao> ItensDoExemplo()
    {
        var bruno = AnalisesTestes.Exemplo().Subpastas[0].Subpastas[1];
        return [ItemAcao.DoArquivo(bruno, bruno.Arquivos[0]), ItemAcao.DoArquivo(bruno, bruno.Arquivos[1])];
    }

    [Fact]
    public void Executor_registra_antes_e_depois_e_segue_depois_de_uma_falha()
    {
        var ops = new OperacoesFalsas();
        ops.FalharEm.Add(@"C:\Users\bruno\caixa.pst");
        var registro = new RegistroEmMemoria();

        var r = new ExecutorAcoes(ops, registro).Executar(new PedidoAcao(TipoAcao.Lixeira, ItensDoExemplo(), null), null, CancellationToken.None);

        Assert.Equal([@"lixeira C:\Users\bruno\setup.exe"], ops.Feitas);
        Assert.Equal(1, r.Ok);
        Assert.Equal(1, r.Falhas);
        Assert.Equal("falhou: em uso", r.Resultados[0].Resultado);
        Assert.Equal(100, r.BytesOk);
        Assert.Equal(
            [
                @"Lixeira|C:\Users\bruno\caixa.pst||2000|iniciado",
                @"Lixeira|C:\Users\bruno\caixa.pst||2000|falhou: em uso",
                @"Lixeira|C:\Users\bruno\setup.exe||100|iniciado",
                @"Lixeira|C:\Users\bruno\setup.exe||100|ok",
            ],
            registro.Linhas);
    }

    [Fact]
    public void Sem_registro_nada_e_feito()
    {
        var ops = new OperacoesFalsas();

        var r = new ExecutorAcoes(ops, new RegistroQueFalha()).Executar(new PedidoAcao(TipoAcao.Excluir, ItensDoExemplo(), null), null, CancellationToken.None);

        Assert.Empty(ops.Feitas);
        Assert.True(r.RegistroFalhou);
        Assert.Empty(r.Resultados);
    }

    [Fact]
    public void Cancelar_para_antes_do_proximo_item()
    {
        var ops = new OperacoesFalsas();
        using var cancelar = new CancellationTokenSource();
        cancelar.Cancel();

        var r = new ExecutorAcoes(ops, new RegistroEmMemoria()).Executar(new PedidoAcao(TipoAcao.Mover, ItensDoExemplo(), @"D:\Arquivo"), null, cancelar.Token);

        Assert.True(r.Cancelada);
        Assert.Empty(ops.Feitas);
    }

    [Fact]
    public async Task Executar_async_roda_numa_thread_sta()
    {
        var ops = new OperacoesFalsas();

        var r = await new ExecutorAcoes(ops, new RegistroEmMemoria()).ExecutarAsync(new PedidoAcao(TipoAcao.Lixeira, ItensDoExemplo(), null), null, CancellationToken.None);

        Assert.Equal(2, r.Ok);
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar**

```csharp
namespace MapDisk.Nucleo;

public sealed record PedidoAcao(TipoAcao Acao, IReadOnlyList<ItemAcao> Itens, string? Destino)
{
    public long Total => Itens.Sum(i => i.Tamanho);
}

public sealed record ResultadoItem(ItemAcao Item, bool Ok, string Resultado);

public sealed record ResumoAcao(IReadOnlyList<ResultadoItem> Resultados, bool Cancelada, bool RegistroFalhou)
{
    public long BytesOk => Resultados.Where(r => r.Ok).Sum(r => r.Item.Tamanho);

    public int Ok => Resultados.Count(r => r.Ok);

    public int Falhas => Resultados.Count(r => !r.Ok);
}

public readonly record struct ProgressoAcao(int Feitos, int Total, string Atual);

/// <summary>
/// Faz a ação item por item. Antes de cada item grava "iniciado" no registro; sem essa linha,
/// o item não começa e a execução para (regra 1). Falha de um item não para os outros.
/// </summary>
public sealed class ExecutorAcoes
{
    private readonly IOperacoesArquivo _operacoes;
    private readonly IRegistroAcoes _registro;

    public ExecutorAcoes(IOperacoesArquivo operacoes, IRegistroAcoes registro)
    {
        _operacoes = operacoes;
        _registro = registro;
    }

    /// <summary>A Lixeira do Shell pede thread STA. A execução ganha uma só para ela.</summary>
    public Task<ResumoAcao> ExecutarAsync(PedidoAcao pedido, IProgress<ProgressoAcao>? progresso, CancellationToken cancelar)
    {
        var fim = new TaskCompletionSource<ResumoAcao>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                fim.SetResult(Executar(pedido, progresso, cancelar));
            }
            catch (Exception e)
            {
                fim.SetException(e);
            }
        })
        {
            IsBackground = true,
            Name = "MapDisk ações",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return fim.Task;
    }

    public ResumoAcao Executar(PedidoAcao pedido, IProgress<ProgressoAcao>? progresso, CancellationToken cancelar)
    {
        var resultados = new List<ResultadoItem>();
        for (var n = 0; n < pedido.Itens.Count; n++)
        {
            if (cancelar.IsCancellationRequested)
            {
                return new ResumoAcao(resultados, true, false);
            }

            var item = pedido.Itens[n];
            progresso?.Report(new ProgressoAcao(n, pedido.Itens.Count, item.Caminho));
            if (!Registrar(pedido, item, "iniciado"))
            {
                return new ResumoAcao(resultados, false, true);
            }

            string resultado;
            try
            {
                Agir(pedido, item, cancelar);
                resultado = "ok";
            }
            catch (OperationCanceledException)
            {
                resultado = "cancelado";
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                resultado = $"falhou: {e.Message}";
            }

            resultados.Add(new ResultadoItem(item, resultado == "ok", resultado));
            if (!Registrar(pedido, item, resultado))
            {
                return new ResumoAcao(resultados, false, true);
            }

            if (resultado == "cancelado")
            {
                return new ResumoAcao(resultados, true, false);
            }
        }

        progresso?.Report(new ProgressoAcao(pedido.Itens.Count, pedido.Itens.Count, string.Empty));
        return new ResumoAcao(resultados, false, false);
    }

    private void Agir(PedidoAcao pedido, ItemAcao item, CancellationToken cancelar)
    {
        switch (pedido.Acao)
        {
            case TipoAcao.Lixeira:
                _operacoes.EnviarParaLixeira(item.Caminho);
                break;
            case TipoAcao.Excluir:
                _operacoes.ExcluirDefinitivo(item.Caminho, item.EhPasta);
                break;
            default:
                _operacoes.Mover(item.Caminho, pedido.Destino!, item.EhPasta, cancelar);
                break;
        }
    }

    private bool Registrar(PedidoAcao pedido, ItemAcao item, string resultado)
    {
        try
        {
            _registro.Gravar(pedido.Acao, item.Caminho, pedido.Destino, item.Tamanho, resultado);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
```

- [ ] **Passo 4: rodar e ver passar.** Expected: PASS.

- [ ] **Passo 5: commit** `Cria o executor das acoes`.

---

### Tarefa 6: tirar da árvore o que saiu do disco

**Arquivos:** `src/mapdisk.nucleo/arvore/no-pasta.cs` e `src/mapdisk.nucleo/painel/arvore-visivel.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Produz: `NoPasta.RemoverSubpasta(NoPasta sub)` e `NoPasta.RemoverArquivo(ArquivoInfo arquivo)` (internas); `NoPasta? NoPasta.Encontrar(string caminho)` (pública, procura a partir desta pasta, sem diferença de maiúsculas); `ArvoreVisivel.Remover(ItemAcao item)`, que tira da árvore e da tela e, se a raiz mostrada estava dentro do que saiu, mostra a pasta de cima.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Remover_subpasta_desconta_das_somas_acima()
    {
        var raiz = AnalisesTestes.Exemplo();
        var users = raiz.Subpastas[0];
        var bruno = users.Subpastas[1];
        var pastasAntes = raiz.PastasTotal;

        users.RemoverSubpasta(bruno);

        Assert.Equal(["ana"], users.Subpastas.Select(s => s.Nome));
        Assert.Equal(300, users.Tamanho);
        Assert.Equal(5300, raiz.Tamanho);
        Assert.Equal(pastasAntes - 1, raiz.PastasTotal);
        Assert.Equal(3, raiz.ArquivosTotal);
    }

    [Fact]
    public void Remover_arquivo_desconta_das_somas_acima()
    {
        var raiz = AnalisesTestes.Exemplo();
        var bruno = raiz.Subpastas[0].Subpastas[1];

        bruno.RemoverArquivo(bruno.Arquivos[0]);

        Assert.Equal(["setup.exe"], bruno.Arquivos.Select(a => a.Nome));
        Assert.Equal(100, bruno.Tamanho);
        Assert.Equal(100, bruno.TamanhoProprio);
        Assert.Equal(5400, raiz.Tamanho);
    }

    [Fact]
    public void Encontrar_pasta_pelo_caminho()
    {
        var raiz = AnalisesTestes.Exemplo();

        Assert.Equal("bruno", raiz.Encontrar(@"c:\users\BRUNO")!.Nome);
        Assert.Same(raiz, raiz.Encontrar(@"C:\"));
        Assert.Null(raiz.Encontrar(@"C:\Users\carla"));
        Assert.Null(raiz.Encontrar(@"D:\Users"));
    }

    [Fact]
    public void Arvore_visivel_sobe_quando_a_raiz_mostrada_sai()
    {
        var raiz = AnalisesTestes.Exemplo();
        var arvore = new ArvoreVisivel();
        arvore.Carregar(raiz);
        var users = raiz.Subpastas[0];
        arvore.AbrirAqui(users.Subpastas[1]);

        arvore.Remover(ItemAcao.DaPasta(users.Subpastas[1]));

        Assert.Same(users, arvore.Raiz);
        Assert.DoesNotContain(arvore.Linhas, l => l.Nome == "bruno");
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: implementar em `no-pasta.cs`**

```csharp
    /// <summary>Tira a subpasta que foi apagada ou movida, e o que havia nela, das somas acima.</summary>
    internal void RemoverSubpasta(NoPasta sub)
    {
        sub.DescontarAcima();
        Somar(0, 0, 0, -1, 0, 0, 0);
        Volatile.Write(ref _subpastas, Volatile.Read(ref _subpastas).Where(s => s != sub).ToArray());
    }

    /// <summary>Tira o arquivo que foi apagado ou movido. Hard link repetido não somava e não desconta.</summary>
    internal void RemoverArquivo(ArquivoInfo arquivo)
    {
        var lista = Volatile.Read(ref _arquivos).ToList();
        if (!lista.Remove(arquivo))
        {
            return;
        }

        var tamanho = arquivo.Soma ? arquivo.Tamanho : 0;
        var alocado = arquivo.Soma ? arquivo.Alocado : 0;
        TamanhoProprio -= tamanho;
        AlocadoProprio -= alocado;
        Somar(-tamanho, -alocado, -1, 0, 0, 0, 0);
        Volatile.Write(ref _arquivos, lista.ToArray());
    }

    /// <summary>A pasta com esse caminho, procurando a partir desta. Null se não está na árvore.</summary>
    public NoPasta? Encontrar(string caminho)
    {
        var base_ = CaminhoCompleto().TrimEnd('\\');
        var alvo = caminho.TrimEnd('\\');
        if (string.Equals(alvo, base_, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        if (!alvo.StartsWith(base_ + @"\", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var no = this;
        foreach (var parte in alvo[(base_.Length + 1)..].Split('\\'))
        {
            no = no.Subpastas.FirstOrDefault(s => string.Equals(s.Nome, parte, StringComparison.OrdinalIgnoreCase));
            if (no is null)
            {
                return null;
            }
        }

        return no;
    }
```

- [ ] **Passo 4: implementar em `arvore-visivel.cs`**

```csharp
    /// <summary>Tira da árvore o item que saiu do disco. Se a raiz mostrada estava dentro dele, sobe.</summary>
    public void Remover(ItemAcao item)
    {
        if (item.EhPasta)
        {
            var pasta = item.Pasta;
            if (pasta.Pai is not { } pai)
            {
                return;
            }

            pai.RemoverSubpasta(pasta);
            Tirar(_voltar, pasta);
            Tirar(_avancar, pasta);
            if (Raiz is not null && Dentro(Raiz, pasta))
            {
                Raiz = pai;
                _pastasAbertas.Add(pai);
            }
        }
        else
        {
            item.Pasta.RemoverArquivo(item.Arquivo!.Value);
        }

        Atualizar();
    }

    private static bool Dentro(NoPasta no, NoPasta pasta)
    {
        for (var n = no; n is not null; n = n.Pai)
        {
            if (n == pasta)
            {
                return true;
            }
        }

        return false;
    }

    // Tira do histórico de Voltar e Avançar as pastas que saíram.
    private static void Tirar(Stack<NoPasta> pilha, NoPasta pasta)
    {
        var ficam = pilha.Reverse().Where(n => !Dentro(n, pasta)).ToList();
        pilha.Clear();
        foreach (var n in ficam)
        {
            pilha.Push(n);
        }
    }
```

- [ ] **Passo 5: rodar e ver passar.** Expected: PASS em todos os testes.

- [ ] **Passo 6: commit** `Tira da arvore o que a acao removeu`.

---

### Tarefa 7: preparador das ações

**Arquivos:** novo `src/mapdisk.nucleo/acoes/preparador-acoes.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Consome: `LinhaArvore`, `LinhaArquivo`, `LinhaResumo`, `ItemAcao`, `Protecao`, `Lixeiras`, `IOperacoesArquivo`, `Formatador`.
- Produz: `sealed record AvaliacaoSelecao(IReadOnlyList<ItemAcao> Itens, string? Bloqueio, TipoAcao Remocao)` com `bool PodeRemover`, `bool PodeMover` e `string TextoRemover` ("Enviar para a Lixeira" ou "Excluir definitivamente"); `sealed record Confirmacao(string Titulo, string Texto, IReadOnlyList<string> Avisos, bool PedeExcluir, string TextoBotao)`; `sealed class PreparadorAcoes` com `PreparadorAcoes(LocaisProtegidos, Func<string, DriveType>, IOperacoesArquivo, bool demonstracao)`, `AvaliacaoSelecao Avaliar(IEnumerable<object> selecionados)`, `string? BloqueioDestino(IReadOnlyList<ItemAcao>, string destino)`, `IReadOnlyList<string> Mudancas(IReadOnlyList<ItemAcao>)` e `Confirmacao Confirmar(PedidoAcao)`.

- [ ] **Passo 1: textos jurídicos.** A variante "Esta unidade não tem Lixeira." do texto de exclusão adapta o texto aprovado da seção 5 da verificação e passa pela `legal-br` antes de entrar no código. Registrar o resultado como seção 9 da verificação jurídica, com a data. Os outros textos desta tarefa passam pela `humanizar-ptbr`.

- [ ] **Passo 2: escrever os testes**

```csharp
    // Registro fora de C:\Users, para a pasta Users do exemplo poder ser selecionada.
    private static PreparadorAcoes Preparador(DriveType tipo = DriveType.Fixed, bool demonstracao = false) =>
        new(Protegidos with { PastaRegistro = @"E:\registro" }, _ => tipo, new OperacoesFalsas(), demonstracao);

    private static LinhaArvore LinhaDe(ArvoreVisivel arvore, string nome) => arvore.Linhas.First(l => l.Nome == nome);

    private static ArvoreVisivel ArvoreAberta()
    {
        var arvore = new ArvoreVisivel();
        arvore.Carregar(AnalisesTestes.Exemplo());
        arvore.AbrirNiveis(3);
        return arvore;
    }

    [Fact]
    public void Selecao_na_arvore_vira_itens_sem_repetir_o_que_esta_dentro()
    {
        var arvore = ArvoreAberta();

        var a = Preparador().Avaliar([LinhaDe(arvore, "Users"), LinhaDe(arvore, "bruno")]);

        Assert.Equal([@"C:\Users"], a.Itens.Select(i => i.Caminho));
        Assert.Null(a.Bloqueio);
        Assert.Equal(TipoAcao.Lixeira, a.Remocao);
        Assert.Equal("Enviar para a Lixeira", a.TextoRemover);
    }

    [Fact]
    public void Raiz_e_pasta_sem_leitura_bloqueiam_com_o_motivo()
    {
        var arvore = ArvoreAberta();

        Assert.Equal(@"C:\: É a raiz da unidade.", Preparador().Avaliar([arvore.Linhas[0]]).Bloqueio);
        Assert.Equal(@"C:\Windows: É pasta do sistema (C:\Windows).", Preparador().Avaliar([LinhaDe(arvore, "Windows")]).Bloqueio);

        var raiz = new NoPasta(@"D:\", null);
        var velha = new NoPasta("velha", raiz);
        raiz.Preencher([], [velha]);
        velha.MarcarSemAcesso("acesso negado");
        var outra = new ArvoreVisivel();
        outra.Carregar(raiz);
        Assert.Equal("velha: pasta sem leitura. Atualize ou varra como administrador antes.",
            Preparador().Avaliar([LinhaDe(outra, "velha")]).Bloqueio);
    }

    [Fact]
    public void Sem_lixeira_a_remocao_vira_exclusao()
    {
        var arvore = ArvoreAberta();

        var a = Preparador(DriveType.Removable).Avaliar([LinhaDe(arvore, "bruno")]);

        Assert.Equal(TipoAcao.Excluir, a.Remocao);
        Assert.Equal("Excluir definitivamente", a.TextoRemover);
    }

    [Fact]
    public void Nada_selecionado_explica()
    {
        Assert.Equal("Selecione pastas ou arquivos na árvore ou nas listas.", Preparador().Avaliar([]).Bloqueio);
    }

    [Fact]
    public void Confirmacao_da_lixeira_usa_o_texto_aprovado()
    {
        var arvore = ArvoreAberta();
        var itens = Preparador().Avaliar([LinhaDe(arvore, "bruno")]).Itens;

        var c = Preparador().Confirmar(new PedidoAcao(TipoAcao.Lixeira, itens, null));

        Assert.Equal(@"Enviar 1 item (2,1 KB) para a Lixeira de C:? Eles podem ser restaurados pela Lixeira enquanto ela não for esvaziada.", c.Texto);
        Assert.False(c.PedeExcluir);
        Assert.Contains("É a pasta de perfil de um usuário. Apagar a pasta não remove a conta do Windows.", c.Avisos);
    }

    [Fact]
    public void Confirmacao_da_exclusao_pede_excluir()
    {
        var arvore = ArvoreAberta();
        var itens = Preparador().Avaliar([LinhaDe(arvore, "bruno")]).Itens;

        var c = Preparador().Confirmar(new PedidoAcao(TipoAcao.Excluir, itens, null));

        Assert.True(c.PedeExcluir);
        Assert.Equal("Excluir definitivamente", c.TextoBotao);
        Assert.Equal(@"Excluir definitivamente 1 item (2,1 KB) de C:\Users? Esta unidade não tem Lixeira. Depois de excluídos, os itens só voltam por uma cópia de segurança. Para confirmar, digite EXCLUIR.", c.Texto);
    }

    [Fact]
    public void Confirmacao_na_demonstracao_avisa()
    {
        var arvore = ArvoreAberta();
        var itens = Preparador().Avaliar([LinhaDe(arvore, "bruno")]).Itens;

        var c = Preparador(demonstracao: true).Confirmar(new PedidoAcao(TipoAcao.Lixeira, itens, null));

        Assert.Contains("Modo de demonstração: nada é apagado nem movido.", c.Avisos);
    }

    [Fact]
    public void Destino_dentro_do_item_ou_na_mesma_pasta_e_bloqueado()
    {
        var arvore = ArvoreAberta();
        var itens = Preparador().Avaliar([LinhaDe(arvore, "bruno")]).Itens;

        Assert.Equal("O destino fica dentro de bruno.", Preparador().BloqueioDestino(itens, @"C:\Users\bruno\sub"));
        Assert.Equal("Os itens já estão nesta pasta.", Preparador().BloqueioDestino(itens, @"C:\Users"));
        Assert.Equal(@"Destino: É pasta do sistema (C:\Windows).", Preparador().BloqueioDestino(itens, @"C:\Windows\Temp"));
        Assert.Null(Preparador().BloqueioDestino(itens, @"D:\Arquivo"));
    }
```

O último teste usa `OperacoesFalsas`, com `Livre` sem limite e `MesmoVolume` sempre verdadeiro. O teste de espaço fica no próximo passo.

```csharp
    private sealed class OperacoesSemEspaco : IOperacoesArquivo
    {
        public string? Mudanca(ItemAcao item) => null;

        public long? Livre(string pasta) => 1000;

        public bool MesmoVolume(string origem, string pastaDestino) => false;

        public void EnviarParaLixeira(string caminho) => throw new InvalidOperationException();

        public void ExcluirDefinitivo(string caminho, bool ehPasta) => throw new InvalidOperationException();

        public void Mover(string origem, string pastaDestino, bool ehPasta, CancellationToken cancelar) => throw new InvalidOperationException();
    }

    [Fact]
    public void Destino_sem_espaco_diz_quanto_falta()
    {
        var arvore = ArvoreAberta();
        var preparador = new PreparadorAcoes(Protegidos, _ => DriveType.Fixed, new OperacoesSemEspaco(), false);
        var itens = preparador.Avaliar([LinhaDe(arvore, "bruno")]).Itens;

        Assert.Equal("Faltam 1,1 KB no destino.", preparador.BloqueioDestino(itens, @"D:\Arquivo"));
    }
```

- [ ] **Passo 3: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 4: implementar**

```csharp
namespace MapDisk.Nucleo;

public sealed record AvaliacaoSelecao(IReadOnlyList<ItemAcao> Itens, string? Bloqueio, TipoAcao Remocao)
{
    public bool PodeRemover => Bloqueio is null;

    public bool PodeMover => Bloqueio is null;

    public string TextoRemover => Remocao == TipoAcao.Lixeira ? "Enviar para a Lixeira" : "Excluir definitivamente";
}

public sealed record Confirmacao(string Titulo, string Texto, IReadOnlyList<string> Avisos, bool PedeExcluir, string TextoBotao);

/// <summary>
/// Da seleção da tela ao pedido: quais itens, o que bloqueia, se há Lixeira, o destino e os
/// textos da confirmação. Os textos de confirmação são os da seção 5 da verificação jurídica.
/// </summary>
public sealed class PreparadorAcoes
{
    private readonly LocaisProtegidos _locais;
    private readonly Func<string, DriveType> _tipoDaUnidade;
    private readonly IOperacoesArquivo _operacoes;
    private readonly bool _demonstracao;

    public PreparadorAcoes(LocaisProtegidos locais, Func<string, DriveType> tipoDaUnidade, IOperacoesArquivo operacoes, bool demonstracao)
    {
        _locais = locais;
        _tipoDaUnidade = tipoDaUnidade;
        _operacoes = operacoes;
        _demonstracao = demonstracao;
    }

    public AvaliacaoSelecao Avaliar(IEnumerable<object> selecionados)
    {
        var itens = new List<ItemAcao>();
        foreach (var s in selecionados)
        {
            switch (s)
            {
                case LinhaArvore { Tipo: TipoLinha.GrupoArquivos } grupo:
                    return Bloqueado($"{grupo.Nome}: abra o grupo e selecione os arquivos.");
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

        // Item dentro de outro item selecionado sai: a ação sobre a pasta já o leva.
        itens = itens
            .DistinctBy(i => i.Caminho, StringComparer.OrdinalIgnoreCase)
            .Where(i => !itens.Any(o => o.EhPasta && o != i && Protecao.Dentro(i.Caminho, o.Caminho) && !string.Equals(i.Caminho, o.Caminho, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (itens.Count == 0)
        {
            return Bloqueado("Selecione pastas ou arquivos na árvore ou nas listas.");
        }

        foreach (var item in itens)
        {
            if (Protecao.Motivo(item.Caminho, item.EhPasta, item.Marcas, _locais) is { } motivo)
            {
                return Bloqueado($"{item.Caminho}: {motivo}", itens);
            }

            if (item.EhPasta && item.Pasta.Estado == EstadoPasta.Link)
            {
                return Bloqueado($"{item.Nome}: é link. Trate pelo Explorer.", itens);
            }

            if (item.EhPasta && item.Pasta.Estado != EstadoPasta.Lida)
            {
                return Bloqueado($"{item.Nome}: pasta sem leitura. Atualize ou varra como administrador antes.", itens);
            }
        }

        var comLixeira = itens.Count(i => Lixeiras.Existe(i.Caminho, _tipoDaUnidade));
        if (comLixeira > 0 && comLixeira < itens.Count)
        {
            return Bloqueado("A seleção mistura lugares com e sem Lixeira. Selecione um lugar de cada vez.", itens);
        }

        return new AvaliacaoSelecao(itens, null, comLixeira > 0 ? TipoAcao.Lixeira : TipoAcao.Excluir);
    }

    public string? BloqueioDestino(IReadOnlyList<ItemAcao> itens, string destino)
    {
        foreach (var item in itens)
        {
            if (item.EhPasta && Protecao.Dentro(destino, item.Caminho))
            {
                return $"O destino fica dentro de {item.Nome}.";
            }
        }

        if (itens.All(i => string.Equals(Path.GetDirectoryName(i.Caminho.TrimEnd('\\'))?.TrimEnd('\\'), destino.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
        {
            return "Os itens já estão nesta pasta.";
        }

        if (Protecao.Motivo(Path.Combine(destino, "x"), false, MarcaArquivo.Nenhuma, _locais) is { } motivo)
        {
            return $"Destino: {motivo}";
        }

        var entreUnidades = itens.Where(i => !_operacoes.MesmoVolume(i.Caminho, destino)).Sum(i => i.Tamanho);
        if (entreUnidades > 0 && _operacoes.Livre(destino) is { } livre && livre < entreUnidades)
        {
            return $"Faltam {Formatador.Tamanho(entreUnidades - livre)} no destino.";
        }

        return null;
    }

    /// <summary>Os itens que mudaram desde a varredura, com o motivo, para o aviso antes da confirmação.</summary>
    public IReadOnlyList<string> Mudancas(IReadOnlyList<ItemAcao> itens) =>
        itens.Select(i => (i, m: _operacoes.Mudanca(i))).Where(x => x.m is not null).Select(x => $"{x.i.Caminho}: {x.m}").ToList();

    public Confirmacao Confirmar(PedidoAcao pedido)
    {
        var quantos = $"{Formatador.Plural(pedido.Itens.Count, "item", "itens")} ({Formatador.Tamanho(pedido.Total)})";
        var avisos = new List<string>();
        if (_demonstracao)
        {
            avisos.Add("Modo de demonstração: nada é apagado nem movido.");
        }

        if (pedido.Itens.Any(i => i.EhPasta && i.Pasta.Pai is { Nome: var pai } && string.Equals(pai, "Users", StringComparison.OrdinalIgnoreCase)))
        {
            avisos.Add("É a pasta de perfil de um usuário. Apagar a pasta não remove a conta do Windows.");
        }

        var semLeitura = pedido.Itens.Where(i => i.EhPasta).Sum(i => i.Pasta.PastasSemAcesso + i.Pasta.PastasComErro);
        if (semLeitura > 0)
        {
            avisos.Add($"{Formatador.Plural(semLeitura, "pasta sem leitura fica", "pastas sem leitura ficam")} dentro da seleção. O total pode ser maior que o mostrado.");
        }

        return pedido.Acao switch
        {
            TipoAcao.Lixeira => new Confirmacao(
                "Enviar para a Lixeira",
                $"Enviar {quantos} para a Lixeira de {Unidades(pedido)}? Eles podem ser restaurados pela Lixeira enquanto ela não for esvaziada.",
                avisos, false, "Enviar para a Lixeira"),
            TipoAcao.Excluir => new Confirmacao(
                "Excluir definitivamente",
                TextoExclusao(pedido, quantos),
                avisos, true, "Excluir definitivamente"),
            _ => new Confirmacao(
                "Mover",
                $"Mover {quantos} para {pedido.Destino}? A origem só é apagada depois de a cópia ser conferida.",
                avisos, false, "Mover"),
        };
    }

    // "C:" ou "C: e D:".
    private static string Unidades(PedidoAcao pedido) => string.Join(" e ", pedido.Itens
        .Select(i => Volumes.RaizDe(i.Caminho).TrimEnd('\\'))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    private static string TextoExclusao(PedidoAcao pedido, string quantos)
    {
        var local = Path.GetDirectoryName(pedido.Itens[0].Caminho.TrimEnd('\\')) ?? pedido.Itens[0].Caminho;
        var rede = pedido.Itens.All(i => Alvo.EhRede(i.Caminho));
        // O texto de rede é o aprovado (verificação jurídica, seção 5). O de unidade sem Lixeira
        // adapta a mesma frase e passa pela legal-br no passo 1 desta tarefa.
        var semLixeira = rede ? "Pastas de rede não têm Lixeira." : "Esta unidade não tem Lixeira.";
        return $"Excluir definitivamente {quantos} de {local}? {semLixeira} Depois de excluídos, os itens só voltam por uma cópia de segurança. Para confirmar, digite EXCLUIR.";
    }

    private static AvaliacaoSelecao Bloqueado(string motivo, IReadOnlyList<ItemAcao>? itens = null) =>
        new(itens ?? [], motivo, TipoAcao.Lixeira);
}
```

A frase "Esta unidade não tem Lixeira." troca só o lugar na frase aprovada "Pastas de rede não têm Lixeira.". Se a `legal-br` pedir outra redação no passo 1, ela entra aqui e no teste.

- [ ] **Passo 5: rodar e ver passar.** Expected: PASS em todos.

- [ ] **Passo 6: commit** `Cria o preparador das acoes com os textos de confirmacao`, com a seção 9 da verificação jurídica.

---

### Tarefa 8: ações no painel principal

**Arquivos:** `src/mapdisk.nucleo/painel/painel-principal.cs`, `dependencias-painel.cs` e `demonstracao.cs`. Teste: acrescentar em `acoes-testes.cs`.

**Interfaces:**
- Consome: tudo das tarefas 1 a 7.
- Produz: em `DependenciasPainel`, `IOperacoesArquivo Operacoes` (padrão `new OperacoesDemonstracao()`), `IRegistroAcoes Registro` (padrão `new RegistroEmMemoria()`), `LocaisProtegidos Locais` (padrão `LocaisProtegidos.DoSistema()`), `Func<string, DriveType> TipoDaUnidade` (padrão `Lixeiras.TipoDaUnidade`) e `bool Demonstracao`. Em `PainelPrincipal`: `PreparadorAcoes Acoes`, `ExecutorAcoes Executor`, `string LocalDoRegistro`, `AvaliacaoSelecao Selecao`, `void AvaliarSelecao(IEnumerable<object>)`, `bool PodeRemover`, `bool PodeMover`, `string TextoRemover`, `string MotivoBloqueio`, `void Concluir(PedidoAcao, ResumoAcao)`, `string TextoSessao` e `int AcoesConcluidas`.

- [ ] **Passo 1: escrever os testes**

```csharp
    private static PainelPrincipal PainelComExemplo(RegistroEmMemoria registro)
    {
        var painel = new PainelPrincipal(new DependenciasPainel
        {
            Motor = Demonstracao.Motor(),
            ListarUnidades = () => [],
            Operacoes = new OperacoesFalsas(),
            Registro = registro,
            Locais = Protegidos,
            TipoDaUnidade = _ => DriveType.Fixed,
        });
        painel.Arvore.Carregar(AnalisesTestes.Exemplo());
        painel.Arvore.AbrirNiveis(3);
        return painel;
    }

    [Fact]
    public void Painel_avalia_a_selecao_para_os_botoes()
    {
        var painel = PainelComExemplo(new RegistroEmMemoria());

        painel.AvaliarSelecao([painel.Arvore.Linhas[0]]);
        Assert.False(painel.PodeRemover);
        Assert.Equal(@"C:\: É a raiz da unidade.", painel.MotivoBloqueio);

        painel.AvaliarSelecao([painel.Arvore.Linhas.First(l => l.Nome == "bruno")]);
        Assert.True(painel.PodeRemover);
        Assert.Equal("Enviar para a Lixeira", painel.TextoRemover);
    }

    [Fact]
    public async Task Depois_da_acao_o_item_sai_da_arvore_e_a_sessao_soma()
    {
        var painel = PainelComExemplo(new RegistroEmMemoria());
        painel.AvaliarSelecao([painel.Arvore.Linhas.First(l => l.Nome == "bruno")]);
        var pedido = new PedidoAcao(TipoAcao.Lixeira, painel.Selecao.Itens, null);

        var resumo = await painel.Executor.ExecutarAsync(pedido, null, CancellationToken.None);
        painel.Concluir(pedido, resumo);

        Assert.DoesNotContain(painel.Arvore.Linhas, l => l.Nome == "bruno");
        Assert.Equal(5300, painel.Arvore.Raiz!.Tamanho);
        Assert.Equal("Nesta sessão: 2,1 KB para a Lixeira", painel.TextoSessao);
        Assert.Equal(1, painel.AcoesConcluidas);
    }

    [Fact]
    public void Durante_a_varredura_nao_ha_acao()
    {
        var painel = new PainelPrincipal(new DependenciasPainel { Motor = Demonstracao.Motor(), ListarUnidades = () => [] });
        painel.TextoAlvo = @"C:\";
        painel.Varrer();

        painel.AvaliarSelecao([]);

        Assert.False(painel.PodeRemover);
        Assert.Equal("Aguarde o fim da varredura.", painel.MotivoBloqueio);
    }
```

- [ ] **Passo 2: rodar e ver falhar.** Expected: FAIL na compilação.

- [ ] **Passo 3: dependências.** Em `dependencias-painel.cs`, acrescentar:

```csharp
    /// <summary>Operações no disco. O padrão não toca no disco: só o Padrao() do painel liga as reais.</summary>
    public IOperacoesArquivo Operacoes { get; init; } = new OperacoesDemonstracao();

    public IRegistroAcoes Registro { get; init; } = new RegistroEmMemoria();

    public LocaisProtegidos Locais { get; init; } = LocaisProtegidos.DoSistema();

    public Func<string, DriveType> TipoDaUnidade { get; init; } = Lixeiras.TipoDaUnidade;

    /// <summary>Modo --demonstracao: a confirmação avisa que nada é feito.</summary>
    public bool Demonstracao { get; init; }
```

O padrão das operações é o que não toca no disco, para um painel montado sem querer nunca apagar nada.

- [ ] **Passo 4: painel.** Em `painel-principal.cs`:
  - No construtor, com o parâmetro `dependencias`: `Acoes = new PreparadorAcoes(dependencias.Locais, dependencias.TipoDaUnidade, dependencias.Operacoes, dependencias.Demonstracao); Executor = new ExecutorAcoes(dependencias.Operacoes, dependencias.Registro); LocalDoRegistro = dependencias.Registro.Local;`.
  - Em `Padrao()`: `Operacoes = new OperacoesArquivo(), Registro = RegistroAcoes.Padrao()`.
  - Estado e métodos:

```csharp
    private long _sessaoLixeira;
    private long _sessaoMovido;
    private long _sessaoExcluido;

    public PreparadorAcoes Acoes { get; }

    public ExecutorAcoes Executor { get; }

    public string LocalDoRegistro { get; }

    public AvaliacaoSelecao Selecao { get; private set; } = new([], "Selecione pastas ou arquivos na árvore ou nas listas.", TipoAcao.Lixeira);

    public bool PodeRemover => Estado == EstadoPainel.Parado && Selecao.PodeRemover;

    public bool PodeMover => Estado == EstadoPainel.Parado && Selecao.PodeMover;

    public string TextoRemover => Selecao.TextoRemover;

    public string MotivoBloqueio => Estado != EstadoPainel.Parado ? "Aguarde o fim da varredura." : Selecao.Bloqueio ?? string.Empty;

    /// <summary>Quantas ações terminaram. A janela compara para recalcular as análises.</summary>
    public int AcoesConcluidas { get; private set; }

    public string TextoSessao { get; private set; } = string.Empty;

    public void AvaliarSelecao(IEnumerable<object> selecionados)
    {
        Selecao = Acoes.Avaliar(selecionados);
        Avisar();
    }

    /// <summary>Tira da árvore o que deu certo, soma a sessão e relê o destino do mover, se estiver na árvore.</summary>
    public void Concluir(PedidoAcao pedido, ResumoAcao resumo)
    {
        foreach (var r in resumo.Resultados.Where(r => r.Ok))
        {
            Arvore.Remover(r.Item);
        }

        switch (pedido.Acao)
        {
            case TipoAcao.Lixeira:
                _sessaoLixeira += resumo.BytesOk;
                break;
            case TipoAcao.Mover:
                _sessaoMovido += resumo.BytesOk;
                break;
            default:
                _sessaoExcluido += resumo.BytesOk;
                break;
        }

        var partes = new List<string>();
        if (_sessaoLixeira > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoLixeira)} para a Lixeira");
        }

        if (_sessaoMovido > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoMovido)} movidos");
        }

        if (_sessaoExcluido > 0)
        {
            partes.Add($"{Formatador.Tamanho(_sessaoExcluido)} excluídos");
        }

        TextoSessao = partes.Count == 0 ? string.Empty : $"Nesta sessão: {string.Join(", ", partes)}";
        AcoesConcluidas++;
        Selecao = Acoes.Avaliar([]);
        AtualizarTextos();

        var raizDaVarredura = Arvore.Raiz;
        while (raizDaVarredura?.Pai is { } pai)
        {
            raizDaVarredura = pai;
        }

        if (pedido.Acao == TipoAcao.Mover && resumo.Ok > 0 && raizDaVarredura?.Encontrar(pedido.Destino!) is { } destino)
        {
            AtualizarPasta(destino);
        }
    }
```

  - `AtualizarTextos()` já chama `Avisar()`, que atualiza os botões.

- [ ] **Passo 5: demonstração.** Em `demonstracao.cs`, `Painel()` passa a montar o painel com `Operacoes = new OperacoesDemonstracao()`, `Registro = new RegistroEmMemoria()` e `Demonstracao = true`.

- [ ] **Passo 6: rodar e ver passar.** Run: `dotnet test mapdisk.sln -c Release`. Expected: PASS em todos.

- [ ] **Passo 7: commit** `Liga as acoes ao painel principal`.

---

### Tarefa 9: janelas de confirmação e de andamento, e a janela principal

**Arquivos:** novos `src/mapdisk/janela-confirmacao.xaml` e `.xaml.cs`, `src/mapdisk/janela-andamento.xaml` e `.xaml.cs`; mudanças em `src/mapdisk/janela-principal.xaml` e `.xaml.cs`. Teste: acrescentar em `recursos-testes.cs`.

**Interfaces:**
- Consome: `PainelPrincipal` (tarefa 8), `Confirmacao`, `PedidoAcao`, `ResumoAcao`, `ProgressoAcao`, `Shell.MostrarNoExplorer`.

- [ ] **Passo 1: teste de recurso**

```csharp
    [Fact]
    public void Janela_tem_as_acoes_com_confirmacao()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("Click=\"AoRemover\"", janela);
        Assert.Contains("Click=\"AoMover\"", janela);
        Assert.Contains("Click=\"AoAbrirRegistro\"", janela);
        Assert.Contains("SelectionMode=\"Extended\"", janela);
        Assert.Contains("ToolTipService.ShowOnDisabled=\"True\"", janela);
        Assert.Contains("{Binding TextoSessao}", janela);
        var confirmacao = File.ReadAllText(App("janela-confirmacao.xaml"));
        Assert.Contains("x:Name=\"CampoExcluir\"", confirmacao);
        Assert.Contains("IsCancel=\"True\"", confirmacao);
    }
```

- [ ] **Passo 2: janela de confirmação.** `JanelaConfirmacao(Confirmacao c, PedidoAcao p)`, no estilo `JanelaMT`, largura 640, altura pelo conteúdo, `WindowStartupLocation="CenterOwner"`:
  - faixa verde com `c.Titulo`;
  - `c.Texto` em destaque, com quebra de linha;
  - os avisos, um por linha, em `PincelVerdeEscuro` e seminegrito;
  - uma `ListView` com altura máxima de 240, com Nome, Tamanho e Caminho de cada item de `p.Itens`, e embaixo "Total: X" e, no mover, "Destino: Y";
  - quando `c.PedeExcluir`, um `TextBox x:Name="CampoExcluir"` com o rótulo "Digite EXCLUIR para confirmar". O botão de ação só liga quando o texto é exatamente `EXCLUIR`;
  - botões: `Cancelar` com `IsCancel="True"` e `IsDefault="True"`, e o botão de ação com `c.TextoBotao`. O foco começa no Cancelar;
  - `DialogResult = true` só pelo botão de ação.

- [ ] **Passo 3: janela de andamento.** `JanelaAndamento`, no mesmo estilo:
  - enquanto roda: `ProgressBar` de 0 a `Total` com `Feitos`, o item atual e o botão Cancelar, que chama `Cancel()` no `CancellationTokenSource`. Fechar a janela pelo X durante a execução também cancela;
  - no fim: "N itens deram certo" e, se houver, a lista dos que falharam com o motivo. Quando `RegistroFalhou`: "O registro de ações não pôde ser gravado em {local}. A ação parou antes do próximo item.";
  - botão Fechar.

- [ ] **Passo 4: janela principal.**
  - `SelectionMode="Extended"` na árvore e em `ListaMaiores`, `ListaAntigos` e `ListaUsuarios`.
  - Na barra de exibição, depois de "Análises": botões `Content="{Binding TextoRemover}"` (`Click="AoRemover"`, `IsEnabled="{Binding PodeRemover}"`), "Mover..." (`Click="AoMover"`, `IsEnabled="{Binding PodeMover}"`), os dois com `ToolTip="{Binding MotivoBloqueio}"` e `ToolTipService.ShowOnDisabled="True"`, e "Registro de ações" (`Click="AoAbrirRegistro"`).
  - Menus de contexto da árvore, dos arquivos e dos perfis: "Enviar para a Lixeira ou excluir..." (`AoRemover`) e "Mover..." (`AoMover`).
  - Barra de status: `TextBlock` com `{Binding TextoSessao}`, depois do texto de pastas sem leitura.
  - Código:

```csharp
    private IList? _ultimaSelecao;

    // A última lista em que o técnico selecionou algo é a que vale para os botões.
    private void AoSelecionarParaAcao(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView lista)
        {
            _ultimaSelecao = lista.SelectedItems;
            _painel.AvaliarSelecao(lista.SelectedItems.Cast<object>());
        }
    }

    private async void AoRemover(object sender, RoutedEventArgs e)
    {
        _painel.AvaliarSelecao(_ultimaSelecao?.Cast<object>() ?? []);
        if (_painel.PodeRemover)
        {
            await Agir(new PedidoAcao(_painel.Selecao.Remocao, _painel.Selecao.Itens, null));
        }
    }

    private async void AoMover(object sender, RoutedEventArgs e)
    {
        _painel.AvaliarSelecao(_ultimaSelecao?.Cast<object>() ?? []);
        if (!_painel.PodeMover)
        {
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

    private async Task Agir(PedidoAcao pedido)
    {
        var mudancas = _painel.Acoes.Mudancas(pedido.Itens);
        if (mudancas.Count > 0 && MessageBox.Show(this,
                $"Estes itens mudaram desde a varredura:\n\n{string.Join("\n", mudancas.Take(10))}\n\nSeguir mesmo assim?",
                "MapDisk - MT", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            return;
        }

        if (new JanelaConfirmacao(_painel.Acoes.Confirmar(pedido), pedido) { Owner = this }.ShowDialog() != true)
        {
            return;
        }

        var andamento = new JanelaAndamento(_painel.Executor, pedido, _painel.LocalDoRegistro) { Owner = this };
        andamento.ShowDialog();
        if (andamento.Resumo is { } resumo)
        {
            _painel.Concluir(pedido, resumo);
            _pastaAnalisada = null;
            await AtualizarAnalises();
        }
    }

    private void AoAbrirRegistro(object sender, RoutedEventArgs e)
    {
        if (File.Exists(_painel.LocalDoRegistro))
        {
            Shell.MostrarNoExplorer(_painel.LocalDoRegistro, ehArquivo: true);
        }
        else
        {
            MessageBox.Show(this, $"Nenhuma ação registrada ainda. O registro fica em {_painel.LocalDoRegistro}.", "MapDisk - MT");
        }
    }
```

  - `SelectionChanged="AoSelecionarParaAcao"` na árvore (além do atual) e nas três listas de análise.
  - Na árvore e nas listas, `Delete` chama `AoRemover`.
  - `JanelaAndamento` expõe `ResumoAcao? Resumo` e roda `ExecutarAsync` no `Loaded`, com `Progress<ProgressoAcao>` que atualiza a barra.

- [ ] **Passo 5: build e conferência pelo agente.**
  - Em `--demonstracao`: seleção múltipla; botões desligados com o motivo na raiz e em "System Volume Information"; confirmação de Lixeira com lista, total e aviso de demonstração; andamento e resumo; item saindo da árvore; "Nesta sessão" na barra; o mover com o bloqueio "O destino fica dentro de ...".
  - Na pasta de teste do projeto (`testes/mapdisk.testes/bin/...`), com uma árvore criada para isso: o mover de verdade e a conferência "mudou desde a varredura", mexendo num arquivo depois da varredura.
  - A Lixeira real e a exclusão em rede ficam para o teste do Manfred (restrições globais).

- [ ] **Passo 6: commit** `Liga as acoes na janela`.

---

### Tarefa 10: documentação e Pull Request

- [ ] **Passo 1:** README: "Uso" com as ações (Lixeira, mover, excluir em rede e unidade removível, proteção, registro, "Nesta sessão") e o texto "Uso autorizado" da seção 5 da verificação jurídica, perto do download. Linha da fatia 5 na "Situação do projeto".
- [ ] **Passo 2:** verificação jurídica, seção 6: marcar os itens feitos nesta fatia.
- [ ] **Passo 3:** pendências da fatia 5: registro sem rotação de tamanho; hard link removido que deixa a outra cópia sem somar; o botão "Registro de ações" que vai para Opções na fatia 7; permissões (ACL) que não são copiadas no mover entre unidades.
- [ ] **Passo 4:** portões, commit `Traz a documentacao da fatia 5` e Pull Request do ramo `acoes`, com "O que muda", "Como testar" (com o roteiro de teste da Lixeira e do mover numa pasta de teste do Manfred) e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec

| Requisito | Onde |
|---|---|
| R16 Lixeira e mover com o fluxo seguro | Tarefas 4, 5, 7, 8 e 9 |
| R16 excluir definitivamente só onde não há Lixeira | Tarefas 2 e 7 (ampliado para unidade removível, decisão marcada) |
| R16 toda ação no registro | Tarefas 3 e 5 |
| R9 menu de contexto com Mover e Lixeira | Tarefa 9 |
| R8 "Liberado nesta sessão" | Tarefa 8 (separado por tipo, decisão marcada) |
| Seção 7, passo 3, proteção | Tarefa 1 |
| Seção 7, passo 5, conferência e espaço no destino | Tarefas 4, 7 e 9 |
| Seção 7, passo 6, mover entre unidades | Tarefa 4 |
| Seção 7, passo 7, execução com Cancelar e resumo | Tarefas 5 e 9 |
| Seção 7, passo 8, árvore atualizada | Tarefas 6 e 8 |
| Seção 8, arquivo em uso, destino sem espaço, item mudou, registro sem gravação | Tarefas 4, 5, 7 e 9 |
| Verificação jurídica, seção 5, textos | Tarefa 7 |
| Verificação jurídica, seção 4.3, registro | Tarefas 3 e 9 |
| Regra 2, linha de comando sem ação | Nenhuma mudança em `linha-de-comando/` |
