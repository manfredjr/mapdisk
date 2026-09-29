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

        public NoPasta? Relida { get; private set; }

        public Varredura Reler(NoPasta pasta, CancellationToken cancelar)
        {
            Relida = pasta;
            Token = cancelar;
            return new Varredura(pasta).Comecar(_ => Fim.Task);
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

    [Fact]
    public void Fim_com_pastas_nao_lidas_avisa()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        motor.Fim.SetResult(new ResultadoVarredura
        {
            Raiz = motor.Raiz!,
            Volume = null,
            Duracao = TimeSpan.FromSeconds(1),
            Cancelada = false,
            PastasNaoLidas = 3,
        });

        p.Tique();

        Assert.Equal(EstadoPainel.Parado, p.Estado);
        Assert.Contains("3 pastas não foram lidas", p.Erro);
        Assert.Contains("Atualizar", p.Erro);
    }

    [Fact]
    public void Atualizar_pasta_rele_so_ela()
    {
        var motor = new MotorFalso();
        var p = Painel(motor);
        p.Varrer();
        var a = new NoPasta("a", motor.Raiz);
        motor.Raiz!.Preencher([], [a]);
        a.Preencher([], []);
        motor.Fim.SetResult(Resultado(motor.Raiz));
        p.Tique();

        Assert.True(p.AtualizarPasta(a));

        Assert.Same(a, motor.Relida);
        Assert.Equal(EstadoPainel.Varrendo, p.Estado);
    }

    [Fact]
    public void Abrir_aqui_mostra_os_totais_da_pasta_aberta()
    {
        var p = Demonstracao.Painel();
        p.Varrer();
        p.Tique();
        var total = p.TextoTotais;
        var users = p.Arvore.Raiz!.Subpastas.Single(s => s.Nome == "Users");

        p.AbrirAqui(users);

        Assert.NotEqual(total, p.TextoTotais);
        Assert.True(p.PodeVoltar);
        p.Voltar();
        Assert.Equal(total, p.TextoTotais);
        Assert.True(p.PodeAvancar);
    }
}
