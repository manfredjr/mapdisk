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
}
