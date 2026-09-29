namespace MapDisk.Testes;

public class PrivilegiosTestes
{
    [Fact]
    public void Sem_administrador_o_privilegio_de_backup_nao_liga()
    {
        if (Privilegios.EhAdministrador())
        {
            return;
        }

        Assert.False(Privilegios.LigarBackup());
    }
}
