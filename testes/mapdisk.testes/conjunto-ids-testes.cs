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
