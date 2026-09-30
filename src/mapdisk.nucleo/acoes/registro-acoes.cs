namespace MapDisk.Nucleo;

public sealed class RegistroAcoes
{
    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapDisk");
}
