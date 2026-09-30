# Plano da fatia 4: gráficos em blocos e em pizza

> **Para quem executa:** siga tarefa por tarefa, na ordem. Cada tarefa começa com o teste, roda o teste para ver falhar, implementa, roda para ver passar e termina com commit. Leia o `AGENTS.md` antes de começar.

**Objetivo:** uma aba "Gráfico", a primeira do painel de análises, mostra de relance o que pesa dentro da pasta selecionada. São dois desenhos: blocos (cada subpasta é um retângulo com área proporcional ao tamanho) e pizza (as 10 maiores, com legenda). O clique duplo num bloco ou numa fatia abre aquela pasta na árvore.

**Arquitetura:** o cálculo fica no núcleo, em `src/mapdisk.nucleo/grafico/`: a escolha dos itens, a disposição dos blocos (algoritmo "squarified", que evita retângulos finos) e as fatias da pizza, já com o caminho do desenho em texto. O `PainelGrafico` guarda o tipo de gráfico, o modo e o tamanho da área, e entrega listas prontas. A janela só desenha: um `Canvas` com retângulos e um com `Path`.

**Tecnologia:** C# com .NET 8, WPF, xUnit. Sem pacote novo.

Desenho: `docs/superpowers/specs/2026-09-28-mapdisk-design.md`, R10, seção 6 (aba "Gráfico" no painel) e a fatia 4 da seção 11.

## Restrições globais

- Regras do produto do `AGENTS.md`. Nesta fatia pesam a 3 (pasta sem leitura nunca vira bloco nem fatia de tamanho 0: fica fora do desenho e aparece pelo nome embaixo dele) e a 7 (o gráfico usa a árvore em memória, sem ler o disco de novo).
- Continua só lendo: nada é apagado nem movido.
- Testes só dentro da pasta de saída dos testes. Resultado de teste manual no computador do Manfred fica na conversa.
- Português, sem caracteres proibidos, nome de arquivo em minúsculas, build sem aviso, commit pela `.superpowers/rascunho/commit.sh`.

## Decisões desta fatia

| Decisão | Escolha | Motivo |
|---|---|---|
| Onde fica | Aba "Gráfico", a primeira do painel de análises, aberta por padrão | Spec, seção 6 |
| Sobre o que desenha | A pasta das análises (a selecionada, ou a raiz mostrada), um nível abaixo: cada subpasta lida e um item "Arquivos nesta pasta" com os arquivos soltos | Um nível se lê de relance. Para descer, clique duplo |
| Valor de cada item | O mesmo "Mostrar" da árvore: tamanho, espaço alocado ou contagem de arquivos. Em "Porcentagem", o tamanho | O técnico compara o gráfico com a árvore sem trocar de critério |
| Quantos itens | Blocos: até 200. Pizza: até 10. O que passar vira um item "N outros itens", em cinza | Mais que isso vira poeira no desenho e pesa na tela |
| Pasta sem leitura, pendente ou com erro | Fora do desenho. Embaixo dele: "Sem leitura, fora do gráfico: Windows, ..." | Regra 3: um bloco de tamanho 0 diria que a pasta está vazia |
| Junção e link simbólico | Fora do desenho, sem aviso | Não somam na árvore (regra 9) |
| Hard link repetido | Não soma, como na árvore | Regra 9 |
| Cores | Paleta de 8 verdes a partir das cores da MT, repetida em ciclo. "Arquivos nesta pasta" em cinza-escuro e "outros itens" em cinza-claro. Texto branco no verde escuro e grafite no verde claro | Marca da MT, e o cinza separa o que não é subpasta |
| Rótulo no bloco | Nome e valor quando o bloco tem pelo menos 70 x 34 px. Sempre na dica do mouse, com a porcentagem | Texto cortado num bloco pequeno não se lê |
| Clique | Clique duplo num bloco ou numa fatia de pasta faz "Abrir aqui" nela. Voltar retorna | Reusa a navegação da fatia 2 |
| Durante a varredura | Gráfico vazio e o aviso "Aguarde o fim da varredura", como nas outras abas | Número parcial engana |
| Exportar imagem do gráfico | Fica fora. O relatório HTML da fatia 7 decide se leva gráfico | Não pedido no R10 |

## Git desta fatia

1. O plano entra pelo ramo `fatia-4-graficos`, num Pull Request só do plano, junto com a linha da fatia 4 na "Situação do projeto" do README.
2. O código sai do `main` atualizado, no ramo `graficos`, e entra num segundo Pull Request.

## Mapa de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `src/mapdisk.nucleo/grafico/itens-grafico.cs` | Os itens de uma pasta para o gráfico, no modo escolhido, com o agrupamento em "outros" |
| `src/mapdisk.nucleo/grafico/treemap.cs` | Disposição dos blocos numa área, pelo algoritmo "squarified" |
| `src/mapdisk.nucleo/grafico/pizza.cs` | Ângulos e caminho do desenho de cada fatia |
| `src/mapdisk.nucleo/grafico/paleta.cs` | Cores dos itens e do texto sobre eles |
| `src/mapdisk.nucleo/painel/painel-grafico.cs` | Estado do gráfico e listas prontas para a tela |
| `src/mapdisk.nucleo/painel/painel-analises.cs` | Dono do `PainelGrafico`, que carrega junto com as outras abas |
| `src/mapdisk/janela-principal.xaml` e `.xaml.cs` | Aba "Gráfico" |
| `testes/mapdisk.testes/grafico-testes.cs` | Testes do núcleo desta fatia |

---

### Tarefa 1: itens do gráfico

**Arquivos:** novo `src/mapdisk.nucleo/grafico/itens-grafico.cs`. Teste: novo `testes/mapdisk.testes/grafico-testes.cs`.

**Interfaces:**
- Consome: `NoPasta` (`Subpastas`, `Arquivos`, `Estado`, `Tamanho`, `Alocado`, `ArquivosTotal`, `TamanhoProprio`, `AlocadoProprio`), `ModoExibicao`, `Formatador.Plural` e, nos testes, `AnalisesTestes.Exemplo()`.
- Produz: `enum TipoItemGrafico { Pasta, Arquivos, Outros }`; `sealed record ItemGrafico(string Nome, long Valor, TipoItemGrafico Tipo, NoPasta? Pasta)`; `sealed record ConteudoGrafico(IReadOnlyList<ItemGrafico> Itens, long Total, IReadOnlyList<string> NaoLidas)`; `static class ItensGrafico` com `const string NomeArquivos = "Arquivos nesta pasta"` e `ConteudoGrafico DaPasta(NoPasta pasta, ModoExibicao modo, int maximo)`.

- [ ] **Passo 1: criar o ramo do código**

```bash
git checkout main
git pull
git checkout -b graficos
```

- [ ] **Passo 2: escrever os testes**

A árvore `AnalisesTestes.Exemplo()` tem, na raiz `C:\`, o `video.mp4` (5.000 bytes, 8.192 alocados), a pasta `Users` (2.400 bytes em 3 arquivos que somam, 12.288 alocados, 4 arquivos contando o hard link repetido) e a pasta `Windows`, sem acesso.

```csharp
namespace MapDisk.Testes;

public class GraficoTestes
{
    private static NoPasta PastaCom(params long[] tamanhos)
    {
        var raiz = new NoPasta(@"D:\", null);
        var subs = tamanhos.Select((_, i) => new NoPasta($"p{i + 1}", raiz)).ToArray();
        raiz.Preencher([], subs);
        for (var i = 0; i < subs.Length; i++)
        {
            subs[i].Preencher([new ArquivoInfo("a.bin", tamanhos[i], tamanhos[i], new DateTime(2026, 1, 1), MarcaArquivo.Nenhuma)], []);
        }

        return raiz;
    }

    [Fact]
    public void Itens_sao_as_subpastas_e_os_arquivos_soltos_do_maior_para_o_menor()
    {
        var c = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Tamanho, 200);

        Assert.Equal([ItensGrafico.NomeArquivos, "Users"], c.Itens.Select(i => i.Nome));
        Assert.Equal([5000L, 2400L], c.Itens.Select(i => i.Valor));
        Assert.Equal(TipoItemGrafico.Arquivos, c.Itens[0].Tipo);
        Assert.Equal("Users", c.Itens[1].Pasta!.Nome);
        Assert.Equal(7400, c.Total);
    }

    [Fact]
    public void Pasta_sem_leitura_fica_fora_e_aparece_pelo_nome()
    {
        var c = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Tamanho, 200);

        Assert.DoesNotContain(c.Itens, i => i.Nome == "Windows");
        Assert.Equal(["Windows"], c.NaoLidas);
    }

    [Fact]
    public void Segue_o_modo_da_arvore()
    {
        var alocado = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Alocado, 200);
        var contagem = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Contagem, 200);
        var porcentagem = ItensGrafico.DaPasta(AnalisesTestes.Exemplo(), ModoExibicao.Porcentagem, 200);

        Assert.Equal([12288L, 8192L], alocado.Itens.Select(i => i.Valor));
        Assert.Equal("Users", alocado.Itens[0].Nome);
        Assert.Equal([4L, 1L], contagem.Itens.Select(i => i.Valor));
        Assert.Equal([5000L, 2400L], porcentagem.Itens.Select(i => i.Valor));
    }

    [Fact]
    public void Acima_do_maximo_o_resto_vira_outros_itens()
    {
        var c = ItensGrafico.DaPasta(PastaCom(50, 40, 30, 20, 10), ModoExibicao.Tamanho, 3);

        Assert.Equal(["p1", "p2", "3 outros itens"], c.Itens.Select(i => i.Nome));
        Assert.Equal(60, c.Itens[2].Valor);
        Assert.Equal(TipoItemGrafico.Outros, c.Itens[2].Tipo);
        Assert.Equal(150, c.Total);
    }

    [Fact]
    public void Pasta_vazia_e_link_nao_entram()
    {
        var raiz = new NoPasta(@"D:\", null);
        var vazia = new NoPasta("vazia", raiz);
        var link = new NoPasta("atalho", raiz);
        raiz.Preencher([], [vazia, link]);
        vazia.Preencher([], []);
        link.MarcarLink(@"D:\outro");

        var c = ItensGrafico.DaPasta(raiz, ModoExibicao.Tamanho, 200);

        Assert.Empty(c.Itens);
        Assert.Empty(c.NaoLidas);
        Assert.Equal(0, c.Total);
    }
}
```

- [ ] **Passo 3: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: FAIL na compilação, `ItensGrafico` não existe.

- [ ] **Passo 4: implementar**

```csharp
namespace MapDisk.Nucleo;

public enum TipoItemGrafico
{
    Pasta,
    Arquivos,
    Outros,
}

/// <summary>Um bloco ou uma fatia: uma subpasta, os arquivos soltos da pasta ou o resto agrupado.</summary>
public sealed record ItemGrafico(string Nome, long Valor, TipoItemGrafico Tipo, NoPasta? Pasta);

/// <summary>Itens do maior para o menor, o total deles e as subpastas sem leitura, que ficam fora do desenho.</summary>
public sealed record ConteudoGrafico(IReadOnlyList<ItemGrafico> Itens, long Total, IReadOnlyList<string> NaoLidas);

/// <summary>
/// O que o gráfico desenha de uma pasta: um nível abaixo dela, no mesmo critério da árvore.
/// Pasta sem leitura não vira item de valor 0 (regra 3), e link não soma (regra 9).
/// </summary>
public static class ItensGrafico
{
    public const string NomeArquivos = "Arquivos nesta pasta";

    public static ConteudoGrafico DaPasta(NoPasta pasta, ModoExibicao modo, int maximo)
    {
        var itens = new List<ItemGrafico>();
        var naoLidas = new List<string>();
        foreach (var sub in pasta.Subpastas)
        {
            if (sub.Estado == EstadoPasta.Link)
            {
                continue;
            }

            if (sub.Estado != EstadoPasta.Lida)
            {
                naoLidas.Add(sub.Nome);
                continue;
            }

            var valor = Valor(sub, modo);
            if (valor > 0)
            {
                itens.Add(new ItemGrafico(sub.Nome, valor, TipoItemGrafico.Pasta, sub));
            }
        }

        var proprios = modo switch
        {
            ModoExibicao.Alocado => pasta.AlocadoProprio,
            ModoExibicao.Contagem => pasta.Arquivos.Count,
            _ => pasta.TamanhoProprio,
        };
        if (proprios > 0)
        {
            itens.Add(new ItemGrafico(NomeArquivos, proprios, TipoItemGrafico.Arquivos, null));
        }

        itens.Sort((a, b) => b.Valor.CompareTo(a.Valor));
        if (itens.Count > maximo)
        {
            var resto = itens.Skip(maximo - 1).ToList();
            itens = itens.Take(maximo - 1).ToList();
            itens.Add(new ItemGrafico(
                Formatador.Plural(resto.Count, "outro item", "outros itens"),
                resto.Sum(r => r.Valor),
                TipoItemGrafico.Outros,
                null));
        }

        return new ConteudoGrafico(itens, itens.Sum(i => i.Valor), naoLidas);
    }

    private static long Valor(NoPasta pasta, ModoExibicao modo) => modo switch
    {
        ModoExibicao.Alocado => pasta.Alocado,
        ModoExibicao.Contagem => pasta.ArquivosTotal,
        _ => pasta.Tamanho,
    };
}
```

- [ ] **Passo 5: rodar e ver passar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: PASS, 5 testes.

- [ ] **Passo 6: commit** `Cria os itens do grafico de uma pasta`, com `itens-grafico.cs` e `grafico-testes.cs`.

---

### Tarefa 2: disposição dos blocos

**Arquivos:** novo `src/mapdisk.nucleo/grafico/treemap.cs`. Teste: acrescentar em `grafico-testes.cs`.

**Interfaces:**
- Produz: `readonly record struct RetanguloGrafico(double X, double Y, double Largura, double Altura)` com `double Area`; `static class Treemap` com `IReadOnlyList<RetanguloGrafico> Dispor(IReadOnlyList<long> valores, double largura, double altura)`. Os valores chegam do maior para o menor e o retângulo `i` é o do valor `i`.

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Blocos_tem_area_proporcional_e_cobrem_a_area_toda()
    {
        long[] valores = [6, 6, 4, 3, 2, 2, 1];

        var r = Treemap.Dispor(valores, 600, 400);

        Assert.Equal(valores.Length, r.Count);
        Assert.Equal(240000, r.Sum(b => b.Area), 3);
        for (var i = 0; i < valores.Length; i++)
        {
            Assert.Equal(240000d * valores[i] / 24, r[i].Area, 3);
        }
    }

    [Fact]
    public void Blocos_ficam_dentro_da_area_sem_se_sobrepor()
    {
        var r = Treemap.Dispor([50, 30, 10, 5, 3, 1, 1], 300, 200);

        foreach (var b in r)
        {
            Assert.True(b.X >= -1e-6 && b.Y >= -1e-6 && b.X + b.Largura <= 300 + 1e-6 && b.Y + b.Altura <= 200 + 1e-6);
        }

        for (var i = 0; i < r.Count; i++)
        {
            for (var j = i + 1; j < r.Count; j++)
            {
                var largura = Math.Min(r[i].X + r[i].Largura, r[j].X + r[j].Largura) - Math.Max(r[i].X, r[j].X);
                var altura = Math.Min(r[i].Y + r[i].Altura, r[j].Y + r[j].Altura) - Math.Max(r[i].Y, r[j].Y);
                Assert.False(largura > 1e-6 && altura > 1e-6, $"blocos {i} e {j} se sobrepõem");
            }
        }
    }

    [Fact]
    public void Blocos_evitam_retangulos_finos()
    {
        var r = Treemap.Dispor([6, 6, 4, 3, 2, 2, 1], 600, 400);

        Assert.All(r, b => Assert.True(Math.Max(b.Largura / b.Altura, b.Altura / b.Largura) < 3));
    }

    [Fact]
    public void Sem_valores_ou_sem_area_nao_ha_blocos()
    {
        Assert.Empty(Treemap.Dispor([], 300, 200));
        Assert.Empty(Treemap.Dispor([10, 5], 0, 200));
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: FAIL na compilação, `Treemap` não existe.

- [ ] **Passo 3: implementar**

```csharp
namespace MapDisk.Nucleo;

public readonly record struct RetanguloGrafico(double X, double Y, double Largura, double Altura)
{
    public double Area => Largura * Altura;
}

/// <summary>
/// Blocos com área proporcional ao valor, pelo algoritmo "squarified" (Bruls, Huizing e van Wijk):
/// cada faixa recebe blocos enquanto isso deixa o pior deles mais perto de um quadrado.
/// </summary>
public static class Treemap
{
    /// <summary>Os valores chegam do maior para o menor, todos maiores que 0. O bloco i é o do valor i.</summary>
    public static IReadOnlyList<RetanguloGrafico> Dispor(IReadOnlyList<long> valores, double largura, double altura)
    {
        double total = valores.Sum(v => (double)v);
        if (valores.Count == 0 || total <= 0 || largura <= 0 || altura <= 0)
        {
            return [];
        }

        var escala = largura * altura / total;
        var areas = valores.Select(v => v * escala).ToArray();
        var saida = new RetanguloGrafico[areas.Length];
        double x = 0, y = 0, l = largura, a = altura;
        var inicio = 0;
        while (inicio < areas.Length)
        {
            var lado = Math.Min(l, a);
            var fim = inicio + 1;
            var pior = Pior(areas, inicio, fim, lado);
            while (fim < areas.Length)
            {
                var novo = Pior(areas, inicio, fim + 1, lado);
                if (novo > pior)
                {
                    break;
                }

                pior = novo;
                fim++;
            }

            var soma = Soma(areas, inicio, fim);
            if (l >= a)
            {
                // Faixa em pé, encostada à esquerda do que sobrou.
                var w = soma / a;
                var yy = y;
                for (var i = inicio; i < fim; i++)
                {
                    var h = areas[i] / w;
                    saida[i] = new RetanguloGrafico(x, yy, w, h);
                    yy += h;
                }

                x += w;
                l = Math.Max(0, l - w);
            }
            else
            {
                // Faixa deitada, encostada no alto do que sobrou.
                var h = soma / l;
                var xx = x;
                for (var i = inicio; i < fim; i++)
                {
                    var w = areas[i] / h;
                    saida[i] = new RetanguloGrafico(xx, y, w, h);
                    xx += w;
                }

                y += h;
                a = Math.Max(0, a - h);
            }

            inicio = fim;
        }

        return saida;
    }

    // Maior razão entre os lados, na faixa de inicio até fim (exclusivo), com os valores em ordem decrescente.
    private static double Pior(double[] areas, int inicio, int fim, double lado)
    {
        var soma = Soma(areas, inicio, fim);
        var s2 = soma * soma;
        var l2 = lado * lado;
        return Math.Max(l2 * areas[inicio] / s2, s2 / (l2 * areas[fim - 1]));
    }

    private static double Soma(double[] areas, int inicio, int fim)
    {
        double soma = 0;
        for (var i = inicio; i < fim; i++)
        {
            soma += areas[i];
        }

        return soma;
    }
}
```

- [ ] **Passo 4: rodar e ver passar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: PASS, 9 testes.

- [ ] **Passo 5: commit** `Cria a disposicao dos blocos do grafico`, com `treemap.cs` e `grafico-testes.cs`.

---

### Tarefa 3: fatias da pizza

**Arquivos:** novo `src/mapdisk.nucleo/grafico/pizza.cs`. Teste: acrescentar em `grafico-testes.cs`.

**Interfaces:**
- Produz: `readonly record struct FatiaPizza(double Inicio, double Fim, string Caminho)`; `static class Pizza` com `IReadOnlyList<FatiaPizza> Fatias(IReadOnlyList<long> valores, double raio)`. Ângulos em graus, a partir do alto, no sentido do relógio. O `Caminho` é o texto do desenho, no formato que o `Path.Data` do WPF aceita, com ponto decimal, e o centro fica em (raio, raio).

- [ ] **Passo 1: escrever os testes**

```csharp
    [Fact]
    public void Fatias_somam_uma_volta_proporcional_aos_valores()
    {
        var f = Pizza.Fatias([30, 10], 100);

        Assert.Equal(0, f[0].Inicio, 6);
        Assert.Equal(270, f[0].Fim, 6);
        Assert.Equal(270, f[1].Inicio, 6);
        Assert.Equal(360, f[1].Fim, 6);
    }

    [Fact]
    public void Caminho_da_fatia_no_formato_do_wpf()
    {
        var f = Pizza.Fatias([1, 1], 50);

        Assert.Equal("M 50,50 L 50,0 A 50,50 0 0 1 50,100 Z", f[0].Caminho);
        Assert.Equal("M 50,50 L 50,100 A 50,50 0 0 1 50,0 Z", f[1].Caminho);
    }

    [Fact]
    public void Fatia_maior_que_meia_volta_usa_o_arco_grande()
    {
        var f = Pizza.Fatias([3, 1], 50);

        Assert.Equal("M 50,50 L 50,0 A 50,50 0 1 1 0,50 Z", f[0].Caminho);
    }

    [Fact]
    public void Um_item_so_e_o_circulo_inteiro()
    {
        var f = Pizza.Fatias([7], 50);

        Assert.Equal("M 50,0 A 50,50 0 1 1 50,100 A 50,50 0 1 1 50,0 Z", Assert.Single(f).Caminho);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: FAIL na compilação, `Pizza` não existe.

- [ ] **Passo 3: implementar**

```csharp
using System.Globalization;

namespace MapDisk.Nucleo;

/// <summary>Uma fatia: ângulos em graus a partir do alto, no sentido do relógio, e o desenho para o Path do WPF.</summary>
public readonly record struct FatiaPizza(double Inicio, double Fim, string Caminho);

public static class Pizza
{
    public static IReadOnlyList<FatiaPizza> Fatias(IReadOnlyList<long> valores, double raio)
    {
        double total = valores.Sum(v => (double)v);
        if (valores.Count == 0 || total <= 0)
        {
            return [];
        }

        if (valores.Count == 1)
        {
            // Um arco só não fecha a volta: o círculo sai de dois meios arcos.
            var circulo = $"M {N(raio)},0 A {N(raio)},{N(raio)} 0 1 1 {N(raio)},{N(2 * raio)} A {N(raio)},{N(raio)} 0 1 1 {N(raio)},0 Z";
            return [new FatiaPizza(0, 360, circulo)];
        }

        var saida = new List<FatiaPizza>(valores.Count);
        double inicio = 0;
        foreach (var v in valores)
        {
            var fim = inicio + (v / total * 360);
            var (x1, y1) = Ponto(inicio, raio);
            var (x2, y2) = Ponto(fim, raio);
            var grande = fim - inicio > 180 ? 1 : 0;
            var caminho = $"M {N(raio)},{N(raio)} L {N(x1)},{N(y1)} A {N(raio)},{N(raio)} 0 {grande} 1 {N(x2)},{N(y2)} Z";
            saida.Add(new FatiaPizza(inicio, fim, caminho));
            inicio = fim;
        }

        return saida;
    }

    private static (double X, double Y) Ponto(double graus, double raio)
    {
        var rad = graus * Math.PI / 180;
        return (raio + (raio * Math.Sin(rad)), raio - (raio * Math.Cos(rad)));
    }

    // Duas casas bastam para a tela. Ponto decimal, que é o que o Path.Data entende.
    private static string N(double v) => Math.Round(v, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
```

- [ ] **Passo 4: rodar e ver passar**

Run: `dotnet test mapdisk.sln -c Release --filter GraficoTestes`
Expected: PASS, 13 testes. Se `-0` aparecer num caminho (seno de 360 graus arredondado), trocar `N` para somar `0.0` ao valor arredondado (`Math.Round(v, 2) + 0.0`) e rodar de novo.

- [ ] **Passo 5: commit** `Cria as fatias do grafico em pizza`, com `pizza.cs` e `grafico-testes.cs`.

---

### Tarefa 4: paleta e painel do gráfico

**Arquivos:** novos `src/mapdisk.nucleo/grafico/paleta.cs` e `src/mapdisk.nucleo/painel/painel-grafico.cs`; mudança em `src/mapdisk.nucleo/painel/painel-analises.cs`. Testes: acrescentar em `grafico-testes.cs` e em `painel-analises-testes.cs`.

**Interfaces:**
- Consome: `ItensGrafico.DaPasta`, `Treemap.Dispor`, `Pizza.Fatias` (tarefas 1 a 3), `Formatador.Tamanho`, `Formatador.Plural`, `Formatador.Porcentagem`.
- Produz: `static class Paleta` com `string[] Cores`, `CorArquivos`, `CorOutros`, `string Cor(int posicaoDaPasta, TipoItemGrafico tipo)` e `string CorTexto(string cor)`; `enum TipoGrafico { Blocos, Pizza }`; `sealed record BlocoGrafico(double X, double Y, double Largura, double Altura, string Cor, string CorTexto, string Nome, string TextoValor, bool MostrarRotulo, string Dica, NoPasta? Pasta)`; `sealed record FatiaGrafico(string Caminho, string Cor, string Nome, string TextoValor, string TextoPorcentagem, string Dica, NoPasta? Pasta)`; `sealed class PainelGrafico : INotifyPropertyChanged` com `TipoGrafico Tipo`, `ModoExibicao Modo`, `IReadOnlyList<BlocoGrafico> Blocos`, `IReadOnlyList<FatiaGrafico> Fatias`, `string TextoNaoLidas`, `bool MostrarBlocos`, `bool MostrarPizza`, `double DiametroPizza`, `Carregar(NoPasta?)`, `Limpar()`, `DefinirTipo(TipoGrafico)`, `DefinirModo(ModoExibicao)`, `Redimensionar(double, double)`; `PainelAnalises.Grafico`; `AbaAnalise.Grafico` como primeiro valor do enum.

- [ ] **Passo 1: escrever os testes**

Em `grafico-testes.cs`:

```csharp
    [Fact]
    public void Paleta_da_verde_as_pastas_e_cinza_ao_resto()
    {
        Assert.Equal("#006B2D", Paleta.Cor(0, TipoItemGrafico.Pasta));
        Assert.Equal(Paleta.Cores[0], Paleta.Cor(Paleta.Cores.Length, TipoItemGrafico.Pasta));
        Assert.Equal(Paleta.CorArquivos, Paleta.Cor(3, TipoItemGrafico.Arquivos));
        Assert.Equal(Paleta.CorOutros, Paleta.Cor(3, TipoItemGrafico.Outros));
        Assert.Equal("#FFFFFF", Paleta.CorTexto("#006B2D"));
        Assert.Equal("#202020", Paleta.CorTexto("#9AD52B"));
    }

    [Fact]
    public void Painel_monta_os_blocos_da_pasta_na_area()
    {
        var g = new PainelGrafico();
        g.Redimensionar(400, 300);

        g.Carregar(AnalisesTestes.Exemplo());

        Assert.Equal([ItensGrafico.NomeArquivos, "Users"], g.Blocos.Select(b => b.Nome));
        Assert.Equal(120000, g.Blocos.Sum(b => b.Largura * b.Altura), 3);
        Assert.Equal(Paleta.CorArquivos, g.Blocos[0].Cor);
        Assert.Equal("#006B2D", g.Blocos[1].Cor);
        Assert.Equal("4,9 KB", g.Blocos[0].TextoValor);
        Assert.Contains("67,6 %", g.Blocos[0].Dica);
        Assert.True(g.Blocos[0].MostrarRotulo);
        Assert.Equal("Sem leitura, fora do gráfico: Windows", g.TextoNaoLidas);
        Assert.True(g.MostrarBlocos);
        Assert.Empty(g.Fatias);
    }

    [Fact]
    public void Painel_troca_para_pizza_com_legenda()
    {
        var g = new PainelGrafico();
        g.Carregar(AnalisesTestes.Exemplo());

        g.DefinirTipo(TipoGrafico.Pizza);

        Assert.True(g.MostrarPizza);
        Assert.Empty(g.Blocos);
        Assert.Equal(["4,9 KB", "2,3 KB"], g.Fatias.Select(f => f.TextoValor));
        Assert.Equal("67,6 %", g.Fatias[0].TextoPorcentagem);
        Assert.Equal("Users", g.Fatias[1].Pasta!.Nome);
        Assert.StartsWith($"M {PainelGrafico.RaioPizza},{PainelGrafico.RaioPizza} ", g.Fatias[0].Caminho);
    }

    [Fact]
    public void Painel_segue_o_modo_e_conta_arquivos_na_contagem()
    {
        var g = new PainelGrafico();
        g.Redimensionar(400, 300);
        g.Carregar(AnalisesTestes.Exemplo());

        g.DefinirModo(ModoExibicao.Contagem);

        Assert.Equal("Users", g.Blocos[0].Nome);
        Assert.Equal("4 arquivos", g.Blocos[0].TextoValor);
    }

    [Fact]
    public void Bloco_pequeno_nao_mostra_rotulo_e_limpar_esvazia()
    {
        var g = new PainelGrafico();
        g.Redimensionar(60, 30);
        g.Carregar(AnalisesTestes.Exemplo());

        Assert.All(g.Blocos, b => Assert.False(b.MostrarRotulo));

        g.Limpar();

        Assert.Empty(g.Blocos);
        Assert.Equal(string.Empty, g.TextoNaoLidas);
    }
```

Em `painel-analises-testes.cs`:

```csharp
    [Fact]
    public async Task Calcular_carrega_o_grafico_e_aguardar_limpa()
    {
        var p = new PainelAnalises();
        p.Grafico.Redimensionar(400, 300);

        await p.CalcularAsync(AnalisesTestes.Exemplo(), AnalisesTestes.Hoje);

        Assert.Equal(2, p.Grafico.Blocos.Count);
        Assert.Equal(AbaAnalise.Grafico, p.Aba);

        p.Aguardar();

        Assert.Empty(p.Grafico.Blocos);
    }
```

- [ ] **Passo 2: rodar e ver falhar**

Run: `dotnet test mapdisk.sln -c Release --filter "GraficoTestes|PainelAnalisesTestes"`
Expected: FAIL na compilação, `Paleta` e `PainelGrafico` não existem.

- [ ] **Passo 3: implementar a paleta**

```csharp
namespace MapDisk.Nucleo;

/// <summary>
/// Cores do gráfico, a partir das cores da MT. As subpastas recebem os verdes em ciclo; os
/// arquivos soltos e o resto agrupado ficam em cinza, para não parecerem subpasta.
/// </summary>
public static class Paleta
{
    public static readonly string[] Cores =
    [
        "#006B2D", "#43A92C", "#0F8F2F", "#9AD52B", "#3B6E4A", "#7FBF5F", "#2E8B57", "#B5D98A",
    ];

    public const string CorArquivos = "#8C8C8C";

    public const string CorOutros = "#BDBDBD";

    private static readonly HashSet<string> Claras = ["#9AD52B", "#7FBF5F", "#B5D98A", CorOutros];

    public static string Cor(int posicaoDaPasta, TipoItemGrafico tipo) => tipo switch
    {
        TipoItemGrafico.Arquivos => CorArquivos,
        TipoItemGrafico.Outros => CorOutros,
        _ => Cores[posicaoDaPasta % Cores.Length],
    };

    /// <summary>Grafite sobre as cores claras, branco sobre as escuras.</summary>
    public static string CorTexto(string cor) => Claras.Contains(cor) ? "#202020" : "#FFFFFF";
}
```

- [ ] **Passo 4: implementar o painel do gráfico**

```csharp
using System.ComponentModel;

namespace MapDisk.Nucleo;

public enum TipoGrafico
{
    Blocos,
    Pizza,
}

public sealed record BlocoGrafico(
    double X, double Y, double Largura, double Altura, string Cor, string CorTexto,
    string Nome, string TextoValor, bool MostrarRotulo, string Dica, NoPasta? Pasta);

public sealed record FatiaGrafico(
    string Caminho, string Cor, string Nome, string TextoValor, string TextoPorcentagem, string Dica, NoPasta? Pasta);

/// <summary>Estado da aba Gráfico, sem tipos do WPF. A janela informa o tamanho da área e desenha as listas.</summary>
public sealed class PainelGrafico : INotifyPropertyChanged
{
    public const int MaximoBlocos = 200;

    public const int MaximoFatias = 10;

    public const double RaioPizza = 110;

    private const double LarguraMinimaRotulo = 70;

    private const double AlturaMinimaRotulo = 34;

    private NoPasta? _pasta;
    private double _largura;
    private double _altura;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TipoGrafico Tipo { get; private set; }

    public ModoExibicao Modo { get; private set; }

    public IReadOnlyList<BlocoGrafico> Blocos { get; private set; } = [];

    public IReadOnlyList<FatiaGrafico> Fatias { get; private set; } = [];

    public string TextoNaoLidas { get; private set; } = string.Empty;

    public bool MostrarBlocos => Tipo == TipoGrafico.Blocos;

    public bool MostrarPizza => Tipo == TipoGrafico.Pizza;

    public double DiametroPizza => RaioPizza * 2;

    public void Carregar(NoPasta? pasta)
    {
        _pasta = pasta;
        Montar();
    }

    public void Limpar() => Carregar(null);

    public void DefinirTipo(TipoGrafico tipo)
    {
        Tipo = tipo;
        Montar();
    }

    public void DefinirModo(ModoExibicao modo)
    {
        Modo = modo;
        Montar();
    }

    public void Redimensionar(double largura, double altura)
    {
        _largura = largura;
        _altura = altura;
        if (Tipo == TipoGrafico.Blocos)
        {
            Montar();
        }
    }

    private void Montar()
    {
        Blocos = [];
        Fatias = [];
        TextoNaoLidas = string.Empty;
        if (_pasta is { } pasta)
        {
            var c = ItensGrafico.DaPasta(pasta, Modo, Tipo == TipoGrafico.Blocos ? MaximoBlocos : MaximoFatias);
            TextoNaoLidas = c.NaoLidas.Count == 0 ? string.Empty : $"Sem leitura, fora do gráfico: {string.Join(", ", c.NaoLidas)}";
            var cores = Cores(c.Itens);
            if (Tipo == TipoGrafico.Blocos)
            {
                var r = Treemap.Dispor(c.Itens.Select(i => i.Valor).ToList(), _largura, _altura);
                Blocos = r.Select((b, n) =>
                {
                    var item = c.Itens[n];
                    var rotulo = b.Largura >= LarguraMinimaRotulo && b.Altura >= AlturaMinimaRotulo;
                    return new BlocoGrafico(b.X, b.Y, b.Largura, b.Altura, cores[n], Paleta.CorTexto(cores[n]),
                        item.Nome, TextoValor(item.Valor), rotulo, Dica(item, c.Total), item.Pasta);
                }).ToList();
            }
            else
            {
                var f = Pizza.Fatias(c.Itens.Select(i => i.Valor).ToList(), RaioPizza);
                Fatias = f.Select((fatia, n) =>
                {
                    var item = c.Itens[n];
                    return new FatiaGrafico(fatia.Caminho, cores[n], item.Nome, TextoValor(item.Valor),
                        Formatador.Porcentagem((double)item.Valor / c.Total), Dica(item, c.Total), item.Pasta);
                }).ToList();
            }
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    // As subpastas contam a posição entre elas, para a primeira ser sempre o verde da MT.
    private static string[] Cores(IReadOnlyList<ItemGrafico> itens)
    {
        var cores = new string[itens.Count];
        var pasta = 0;
        for (var i = 0; i < itens.Count; i++)
        {
            cores[i] = Paleta.Cor(itens[i].Tipo == TipoItemGrafico.Pasta ? pasta++ : 0, itens[i].Tipo);
        }

        return cores;
    }

    private string TextoValor(long valor) => Modo == ModoExibicao.Contagem
        ? Formatador.Plural(valor, "arquivo", "arquivos")
        : Formatador.Tamanho(valor);

    private string Dica(ItemGrafico item, long total) =>
        $"{item.Nome}\n{TextoValor(item.Valor)} ({Formatador.Porcentagem((double)item.Valor / total)})";
}
```

- [ ] **Passo 5: ligar ao painel de análises**

Em `painel-analises.cs`:
- `AbaAnalise` ganha `Grafico` como primeiro valor, antes de `MaioresArquivos`.
- Nova propriedade `public PainelGrafico Grafico { get; } = new();`.
- Em `Aguardar()`, antes de `Avisar()`: `Grafico.Limpar();`.
- Em `CalcularAsync`, logo depois do `if (_pasta != pasta) return;`: `Grafico.Carregar(pasta);`.

- [ ] **Passo 6: rodar e ver passar**

Run: `dotnet test mapdisk.sln -c Release`
Expected: PASS em todos, inclusive os 6 testes novos desta tarefa.

- [ ] **Passo 7: commit** `Cria o painel do grafico com blocos e pizza`, com os arquivos da tarefa.

---

### Tarefa 5: aba Gráfico na janela

**Arquivos:** `src/mapdisk/janela-principal.xaml` e `.xaml.cs`.

**Interfaces:**
- Consome: `PainelAnalises.Grafico` e todo o `PainelGrafico` da tarefa 4; `PainelPrincipal.AbrirAqui(NoPasta)`.

- [ ] **Passo 1: aba.** No `TabControl` do painel de análises, antes de "Maiores arquivos":

```xml
<TabItem Header="Gráfico">
    <DockPanel DataContext="{Binding Grafico}" Margin="0,6,0,0">
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,6">
            <RadioButton GroupName="TipoGrafico" Content="Blocos" Tag="Blocos" IsChecked="True" Checked="AoMudarGrafico" Margin="0,0,12,0" />
            <RadioButton GroupName="TipoGrafico" Content="Pizza" Tag="Pizza" Checked="AoMudarGrafico" />
        </StackPanel>
        <TextBlock DockPanel.Dock="Bottom" Text="{Binding TextoNaoLidas}" Foreground="{StaticResource PincelTextoSuave}"
                   TextWrapping="Wrap" Margin="0,6,0,0" />
        <Grid>
            <!-- Blocos: area proporcional ao valor. Clique duplo abre a pasta na arvore -->
            <ItemsControl ItemsSource="{Binding Blocos}" SizeChanged="AoRedimensionarGrafico" ClipToBounds="True"
                          Visibility="{Binding MostrarBlocos, Converter={StaticResource VisivelSe}}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <Canvas />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemContainerStyle>
                    <Style TargetType="{x:Type ContentPresenter}">
                        <Setter Property="Canvas.Left" Value="{Binding X}" />
                        <Setter Property="Canvas.Top" Value="{Binding Y}" />
                    </Style>
                </ItemsControl.ItemContainerStyle>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Width="{Binding Largura}" Height="{Binding Altura}" Background="{Binding Cor}"
                                BorderBrush="White" BorderThickness="1" ToolTip="{Binding Dica}" MouseLeftButtonDown="AoClicarNoGrafico">
                            <StackPanel Margin="6,4" Visibility="{Binding MostrarRotulo, Converter={StaticResource VisivelSe}}">
                                <TextBlock Text="{Binding Nome}" Foreground="{Binding CorTexto}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis" />
                                <TextBlock Text="{Binding TextoValor}" Foreground="{Binding CorTexto}" TextTrimming="CharacterEllipsis" />
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

            <!-- Pizza com legenda -->
            <DockPanel Visibility="{Binding MostrarPizza, Converter={StaticResource VisivelSe}}">
                <ItemsControl DockPanel.Dock="Left" ItemsSource="{Binding Fatias}" VerticalAlignment="Top"
                              Width="{Binding DiametroPizza}" Height="{Binding DiametroPizza}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <Canvas />
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Path Data="{Binding Caminho}" Fill="{Binding Cor}" Stroke="White" StrokeThickness="1"
                                  ToolTip="{Binding Dica}" MouseLeftButtonDown="AoClicarNoGrafico" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <ItemsControl ItemsSource="{Binding Fatias}" Margin="14,0,0,0">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <DockPanel Margin="0,0,0,4" ToolTip="{Binding Dica}">
                                <Border DockPanel.Dock="Left" Width="12" Height="12" CornerRadius="2" Background="{Binding Cor}" Margin="0,0,6,0" />
                                <TextBlock DockPanel.Dock="Right" Text="{Binding TextoPorcentagem}" Style="{StaticResource Numero}" Width="52" />
                                <TextBlock DockPanel.Dock="Right" Text="{Binding TextoValor}" Style="{StaticResource Numero}" Width="80" />
                                <TextBlock Text="{Binding Nome}" TextTrimming="CharacterEllipsis" />
                            </DockPanel>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </DockPanel>
        </Grid>
    </DockPanel>
</TabItem>
```

- [ ] **Passo 2: código.** Em `janela-principal.xaml.cs`:

```csharp
    private void AoMudarGrafico(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag } && Enum.TryParse<TipoGrafico>(tag, out var tipo))
        {
            _painel.Analises.Grafico.DefinirTipo(tipo);
        }
    }

    private void AoRedimensionarGrafico(object sender, SizeChangedEventArgs e) =>
        _painel.Analises.Grafico.Redimensionar(e.NewSize.Width, e.NewSize.Height);

    // Clique duplo num bloco ou numa fatia de pasta abre a pasta na árvore. Voltar retorna.
    private void AoClicarNoGrafico(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not FrameworkElement { DataContext: var item })
        {
            return;
        }

        var pasta = item switch
        {
            BlocoGrafico b => b.Pasta,
            FatiaGrafico f => f.Pasta,
            _ => null,
        };
        if (pasta is not null)
        {
            _painel.AbrirAqui(pasta);
            e.Handled = true;
        }
    }
```

Em `AoMudarModo`, depois de `_painel.Arvore.DefinirModo(modo);`, acrescentar `_painel.Analises.Grafico.DefinirModo(modo);`.

- [ ] **Passo 3: teste de recurso.** Em `recursos-testes.cs`:

```csharp
    [Fact]
    public void Aba_grafico_e_a_primeira_do_painel()
    {
        var janela = File.ReadAllText(App("janela-principal.xaml"));
        Assert.Contains("SizeChanged=\"AoRedimensionarGrafico\"", janela);
        Assert.Contains("MouseLeftButtonDown=\"AoClicarNoGrafico\"", janela);
        Assert.True(janela.IndexOf("Header=\"Gráfico\"", StringComparison.Ordinal)
            < janela.IndexOf("Header=\"Maiores arquivos\"", StringComparison.Ordinal));
    }
```

- [ ] **Passo 4: build e conferência na janela, pelo agente.**
  - Em `--demonstracao`, varrer e ver os blocos da raiz com rótulo, a troca para pizza com legenda, a troca de "Mostrar" na árvore mudando o gráfico, e o clique duplo num bloco abrindo a pasta (com Voltar retornando).
  - A pasta "System Volume Information" da demonstração, sem acesso, aparece embaixo do gráfico e não como bloco.
  - Janela estreita e larga: os blocos se refazem ao arrastar a divisória.
  - Números do computador do Manfred só na conversa.

- [ ] **Passo 5: commit** `Liga a aba de grafico na janela`.

---

### Tarefa 6: documentação e Pull Request

- [ ] **Passo 1:** README com a aba "Gráfico" na seção "Uso" e a linha da fatia 4 na "Situação do projeto".
- [ ] **Passo 2:** pendências da fatia 4, com o que ficou de fora (imagem do gráfico no relatório, que a fatia 7 decide).
- [ ] **Passo 3:** portões, commit `Traz a documentacao da fatia 4` e Pull Request do ramo `graficos`, com "O que muda", "Como testar" e a linha de autores. Ligar o PR à sessão e ler o CI.

---

## Conferência do plano contra a spec

| Requisito | Onde |
|---|---|
| R10 gráfico em blocos (treemap) da pasta selecionada | Tarefas 1, 2, 4 e 5 |
| R10 gráfico em pizza da pasta selecionada | Tarefas 1, 3, 4 e 5 |
| Seção 6: aba "Gráfico" no painel, primeira da lista | Tarefas 4 e 5 |
| Regra 3 no gráfico | Pasta sem leitura fora do desenho e listada pelo nome (tarefas 1 e 4) |
| Regra 7 no gráfico | Só a árvore em memória (tarefa 1) |
| Regra 9 no gráfico | Link fora e hard link repetido sem somar (tarefa 1) |
