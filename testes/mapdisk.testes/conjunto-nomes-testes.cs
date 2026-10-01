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

    [Fact]
    public void Muitos_nomes_diferentes_crescem_sem_perder_nenhum()
    {
        var nomes = new ConjuntoNomes();
        var primeiros = Enumerable.Range(0, 50_000).Select(i => nomes.Guardar($"arquivo-{i}.txt".AsSpan())).ToList();

        for (var i = 0; i < 50_000; i++)
        {
            Assert.Same(primeiros[i], nomes.Guardar($"arquivo-{i}.txt".AsSpan()));
        }

        Assert.Equal(string.Empty, nomes.Guardar(ReadOnlySpan<char>.Empty));
    }
}
