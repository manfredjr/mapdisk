# MapDisk - MT

Analisador de espaço em disco para Windows, da MT - Manfred Tecnologia. Mostra quais pastas e arquivos ocupam mais espaço numa unidade, numa pasta ou num compartilhamento de rede. Na própria janela, envia para a Lixeira, move e, onde não há Lixeira, exclui definitivamente, sempre com confirmação e registro.

Software livre, sob licença [GPL-3.0](LICENSE).

As regras do projeto estão no [`AGENTS.md`](AGENTS.md) e o desenho em [`docs/superpowers/specs/2026-09-28-mapdisk-design.md`](docs/superpowers/specs/2026-09-28-mapdisk-design.md).

## Uso

Baixe o `mapdisk.exe` e abra. Não precisa instalar nem ser administrador. Escolha a unidade, digite uma pasta ou um caminho de rede (`\\servidor\pasta`) e clique em **Varrer**.

> Uso autorizado. O MapDisk - MT mostra o espaço ocupado em discos e pastas e permite enviar arquivos para a Lixeira, movê-los ou excluí-los definitivamente. Use o programa só em computadores e pastas que você tem autorização para administrar. Antes de apagar ou mover, confira a lista de itens e o destino. Em pastas de rede e em unidades removíveis ou mapeadas, o MapDisk não usa a Lixeira: a exclusão não pode ser desfeita pelo programa e os itens só voltam por uma cópia de segurança. O programa não envia nenhuma informação para fora do computador.

> Licença e garantias. O MapDisk - MT é distribuído gratuitamente sob a GPL-3.0. Os tamanhos mostrados dependem do que o sistema de arquivos informa e das permissões da conta que roda o programa, e podem ficar incompletos quando alguma pasta não pode ser lida. A licença não inclui promessa de funcionamento em todo computador nem serviço de suporte técnico. Quem apaga ou move arquivos pelo programa decide o que tratar e deve ter cópia de segurança do que for importante. As disposições da GPL-3.0 sobre garantias e responsabilidade aplicam-se nos limites permitidos pela legislação brasileira e não restringem direitos assegurados ao consumidor por lei.

Pastas que o Windows não deixa ler aparecem como **sem acesso**, e a barra de baixo diz quantas são. O total não inclui o que está nelas.

Para ler também essas pastas, clique em **Varrer como administrador**. O Windows pede confirmação e abre outra janela, que já começa varrendo e lê todas as pastas locais. Só leitura: nada é apagado nem movido.

Na árvore:

- **Abrir aqui** (botão direito) mostra uma pasta como raiz, sem varrer de novo. **Voltar**, **Avançar** e **Subir** navegam entre as raízes, e **Abrir até** abre a árvore até 5 níveis.
- **Atualizar esta pasta** (botão direito ou Shift+F5) lê de novo só a pasta escolhida.
- O botão direito também tem **Mostrar no Explorer**, **Copiar caminho** e **Propriedades**.
- A lista de alvos mostra as unidades e os últimos 10 alvos usados. A lista fica só neste computador.

À direita da árvore, o painel **Análises** responde "o que eu trato?" sobre a pasta selecionada:

- **Gráfico:** o que pesa dentro da pasta, em blocos (área proporcional ao tamanho) ou em pizza (as 10 maiores, com legenda). Segue o "Mostrar" da árvore. Clique duplo num bloco ou numa fatia abre a pasta, e Voltar retorna. Pasta sem leitura fica fora do desenho e aparece pelo nome embaixo dele.
- **Maiores arquivos:** os 100 maiores, ou a quantidade escolhida em **Opções**.
- **Arquivos antigos:** sem alteração há mais de 6 meses, 1, 2 ou 5 anos, com a quantidade e o total.
- **Por tipo:** vídeo, imagem, áudio, e-mail (.pst, .ost), imagem de disco, compactado e backup, instalador, documento e outros, mais o ranking por extensão.
- **Por usuário:** o ranking das pastas de perfil em `Users`.
- **Duplicados:** arquivos com o mesmo conteúdo, conferidos em três etapas (mesmo tamanho, primeiro 1 MB e arquivo inteiro). Só roda quando você clica em **Procurar duplicados**, porque é a única análise que lê o conteúdo dos arquivos. Arquivos só na nuvem nunca são baixados. **Selecionar as cópias** marca todas menos a mais antiga de cada grupo, e o programa não deixa apagar todas as cópias de um grupo.

O botão direito numa linha tem **Mostrar no Explorer**, **Copiar caminho** e **Abrir a pasta na árvore**. As análises usam o que já foi lido, sem varrer de novo, e avisam quando há pastas sem leitura fora da conta.

O botão **Sobre** mostra a versão, a autoria, a licença GPL-3.0 com o texto completo e onde baixar. O logo da MT abre o site www.manfred.com.br.

Para tratar o que ocupa espaço, selecione pastas ou arquivos na árvore ou nas listas de análise (Ctrl e Shift para vários) e use os botões no alto do cartão **Pastas**, o botão direito ou a tecla Delete:

- **Enviar para a Lixeira**, em unidade fixa do computador. Os itens voltam pela Lixeira enquanto ela não for esvaziada.
- **Excluir definitivamente**, em pasta de rede, unidade mapeada, pendrive e disco removível, onde o programa não usa a Lixeira. A confirmação pede que você digite EXCLUIR.
- **Mover**, para a pasta que você escolher. Entre unidades, a origem só é apagada depois de a cópia ser conferida.

A confirmação mostra a lista, o total e o destino. O programa não deixa agir na raiz da unidade, nas pastas do Windows, em Program Files, em ProgramData, em arquivos do sistema e em pastas sem leitura, e diz o motivo. Cada item fica no registro de ações, em `%LOCALAPPDATA%\MapDisk\acoes.log`, que **Opções** > **Registro de ações** mostra no Explorer. A barra de baixo soma o que a sessão enviou para a Lixeira, moveu e excluiu. No modo `--demonstracao`, as ações seguem o fluxo na tela, mas nada é apagado nem movido.

Quando os arquivos são do cliente, ele decide o que sai. O botão **Relatório do cliente**, no cartão Pastas, tem duas opções:

- **Gerar relatório:** monte a lista com **Sugerir** (maiores pastas, maiores arquivos, antigos e tipos que costumam sobrar, com os critérios que você ajusta), com **Acrescentar a seleção** e com **Tirar da lista**. Escreva o nome do cliente e clique em **Gerar**. Saem dois arquivos: uma página HTML, que o cliente abre no navegador, marca e salva a resposta (ou imprime em PDF), e uma planilha Excel com a coluna Decisão. Para cada item, ele escolhe Apagar, Mover, Manter ou Conversar. Itens bloqueados pelas ações, como pastas do sistema, não entram. Se você já procurou duplicados, o **Sugerir** também traz as cópias repetidas, deixando a mais antiga.
- **Ler resposta:** depois de varrer a pasta de novo, abra a planilha devolvida ou o arquivo salvo pela página. O MapDisk mostra o que o cliente decidiu e a situação de cada item. Item que mudou depois do relatório ou não existe mais fica de fora. Se a resposta veio por e-mail, use **Marcar pelos números** (por exemplo, `1, 3, 7-9`). Apagar e mover seguem a confirmação de sempre. Guarde a resposta com a ordem de serviço: ela é a autorização do cliente.

O botão **Exportar**, no cartão Pastas, grava a pasta das análises (a selecionada ou a raiz mostrada) em dois formatos:

- **Relatório HTML:** um arquivo só, com o logo da MT, o resumo, o gráfico em blocos, as maiores pastas e arquivos, os tipos, os arquivos antigos, os perfis de usuário e, se você já procurou, os duplicados. Pasta sem leitura aparece como "sem acesso", nunca como zero. Para ter em PDF, use o "Imprimir" do navegador.
- **Planilha CSV:** todas as pastas, para abrir no Excel.

O botão **Opções**, ao lado de Sobre, guarda o que vale entre sessões, em `%LOCALAPPDATA%\MapDisk\opcoes.txt`:

- **Menu do Explorer:** **Ligar** põe "Analisar com MapDisk" no botão direito das pastas, das unidades e do fundo de uma pasta, só para o seu usuário e sem administrador. Esse item abre o MapDisk já varrendo. No Windows 11, ele fica em "Mostrar mais opções". Se o `mapdisk.exe` mudar de lugar, as Opções avisam e **Ligar** aponta para o novo lugar. **Desligar** tira o item por completo.
- **Análises:** quantos maiores arquivos mostrar e o tamanho mínimo dos duplicados.
- **Relatório para o cliente:** os critérios do **Sugerir**. A janela do relatório também guarda os critérios usados.
- **Últimos alvos:** **Esquecer** um alvo ou **Limpar a lista**.
- **Registro de ações:** onde ele fica, com o botão para abrir.

Linha de comando (só lê, nunca apaga nem move arquivo). O alvo é um caminho completo:

    mapdisk varrer C:
    mapdisk varrer D:\ --relatorio d.html
    mapdisk varrer \\servidor\dados --csv dados.csv --top 30
    mapdisk --integrar
    mapdisk --remover-integracao

`--relatorio` grava o relatório HTML, com `--top` itens em cada lista (padrão: 10). `--integrar` e `--remover-integracao` ligam e desligam o item do menu do Explorer.

No Prompt de Comando, use `start /wait mapdisk varrer C:` para o prompt esperar o fim. No PowerShell, termine a linha com `| Out-Host`.

## Situação do projeto

| Fatia | Conteúdo | Ramo | Pull Request | Situação |
|---|---|---|---|---|
| 1 | Varredura, árvore com colunas, modos e unidades, barra de status, linha de comando com CSV | `varredura` | [#2](https://github.com/manfredjr/mapdisk/pull/2) | Concluída em 29/09/2026 |
| 2 | Navegação, últimos alvos, atualizar uma pasta, menu de contexto, varredura como administrador com o privilégio de backup, acertos da fatia 1 | `navegacao` | [#4](https://github.com/manfredjr/mapdisk/pull/4) | Concluída em 29/09/2026 |
| 3 | Painel de análises: maiores arquivos, arquivos antigos, por tipo, por usuário; memória; padrão visual do MapNet com o logo da MT; janela Sobre | `analises` | Plano em [#5](https://github.com/manfredjr/mapdisk/pull/5), código em [#6](https://github.com/manfredjr/mapdisk/pull/6) | Concluída em 30/09/2026 |
| 4 | Gráficos da pasta selecionada: blocos e pizza | `graficos` | Plano em [#7](https://github.com/manfredjr/mapdisk/pull/7), código em [#8](https://github.com/manfredjr/mapdisk/pull/8) | Concluída em 30/09/2026 |
| 5 | Ações seguras: Lixeira, mover, exclusão onde não há Lixeira, proteção, registro de ações, "Nesta sessão" | `acoes` | Plano em [#9](https://github.com/manfredjr/mapdisk/pull/9), código em [#10](https://github.com/manfredjr/mapdisk/pull/10) | Concluída em 30/09/2026 |
| 6 | Relatório para o cliente avaliar: página, planilha e leitura da resposta | `relatorio-cliente` | Spec em [#11](https://github.com/manfredjr/mapdisk/pull/11), plano em [#12](https://github.com/manfredjr/mapdisk/pull/12), código em [#13](https://github.com/manfredjr/mapdisk/pull/13) | Concluída em 30/09/2026 |
| 7 | Duplicados: busca em três etapas, aba no painel e cópias no relatório para o cliente | `duplicados` | Plano em [#14](https://github.com/manfredjr/mapdisk/pull/14), código em [#15](https://github.com/manfredjr/mapdisk/pull/15) | Concluída em 01/10/2026 |
| 8 | Relatório do técnico em HTML com o gráfico, exportar pela janela, linha de comando completa, item "Analisar com MapDisk" no Explorer, tela de Opções e página do programa | `relatorio-e-integracao` | Plano em [#16](https://github.com/manfredjr/mapdisk/pull/16), código em [#17](https://github.com/manfredjr/mapdisk/pull/17) | Concluída em 01/10/2026 |
| - | Versão 1.0.0: troca a versão de 0.1.0 para 1.0.0, para a primeira Release | `versao-1-0-0` | [#19](https://github.com/manfredjr/mapdisk/pull/19) | Publicada em 01/10/2026: [Release v1.0.0](https://github.com/manfredjr/mapdisk/releases/tag/v1.0.0) |
| - | Página do programa em mapdisk.manfred.com.br, pelo Git do cPanel | `publicar-pagina` | [#20](https://github.com/manfredjr/mapdisk/pull/20) | Aguardando autorização para publicar |
