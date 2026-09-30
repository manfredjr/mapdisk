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
