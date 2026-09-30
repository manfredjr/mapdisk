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

    // Duas casas bastam para a tela. Ponto decimal, que é o que o Path.Data entende. Somar 0.0 tira o "-0".
    private static string N(double v) => (Math.Round(v, 2) + 0.0).ToString("0.##", CultureInfo.InvariantCulture);
}
