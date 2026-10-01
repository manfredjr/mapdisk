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

    [Fact]
    public void Zero_e_muitos_ids_cabem_e_nao_repetem()
    {
        var ids = new ConjuntoIds();

        Assert.True(ids.Acrescentar(0));
        Assert.False(ids.Acrescentar(0));
        for (long i = 1; i <= 300_000; i++)
        {
            Assert.True(ids.Acrescentar(i * 7919));
        }

        for (long i = 1; i <= 300_000; i++)
        {
            Assert.False(ids.Acrescentar(i * 7919));
        }
    }
}
