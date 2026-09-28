# MapDisk - MT: desenho da versão 1

Data: 28/09/2026. Autor: Manfred Heil Junior.

Este desenho registra o pedido do Manfred e as decisões tomadas com ele em 26/09/2026, antes de qualquer código. O ponto de partida foi o TreeSize Free, da JAM Software, que o Manfred usa hoje no atendimento.

## 1. O que é

Programa para Windows que mostra onde está o espaço ocupado de um disco, de uma pasta ou de um compartilhamento de rede. O técnico roda o programa quando um servidor ou uma estação está com pouco espaço, descobre quais pastas e arquivos ocupam mais e, no próprio programa, envia para a Lixeira ou move para outro lugar o que decidiu tratar.

O fluxo de trabalho que o programa atende:

1. Um servidor ou estação fica com pouco disco.
2. O técnico varre a unidade e acha onde está o volume: qual usuário, qual pasta, quais arquivos.
3. O técnico decide o que pode ser apagado e o que precisa ir para outro disco ou storage.
4. O técnico age e confere quanto espaço foi liberado.

## 2. Para quem

- **Quem usa:** a equipe técnica da MT e qualquer pessoa que baixar o programa, já que o código é aberto.
- **Onde roda:** estações Windows 10 e 11 e servidores Windows Server 2016 ou mais novo, todos de 64 bits. A licença do TreeSize Free proíbe o uso em servidor. O MapDisk não tem essa restrição, e o servidor é um dos casos principais.

## 3. Decisões

| Decisão | Escolha | Motivo |
|---|---|---|
| Nome | MapDisk - MT, executável `mapdisk.exe` | Decisão do Manfred em 26/09/2026. Faz par com o MapNet. "TreeSize" é marca da JAM Software e não pode ser usado |
| Código | Aberto, repositório público `manfredjr/mapdisk`, licença GPL-3.0 | Decisão do Manfred em 26/09/2026, no mesmo modelo do MapNet e do CronoAula. Sem chave de licença e sem edições pagas |
| Linguagem | C# com .NET 8 | Mesma stack do MapNet, acesso direto às APIs de arquivo do Windows |
| Entrega | `.exe` único, autocontido, `win-x64`, sem instalador | Rodar em servidor de cliente a partir de pendrive ou pasta de rede, sem instalar nada |
| Interface | Janela WPF e linha de comando no mesmo `.exe` | Mesmo padrão do MapNet, com o tema, a fonte e o logo da MT reaproveitados |
| Alvos | Unidades locais (fixas e removíveis), pastas e compartilhamentos de rede `\\servidor\pasta` | Decisão do Manfred em 26/09/2026. Nuvem (OneDrive, SharePoint, Google Drive por API) fica fora |
| Motor de varredura | Enumeração em paralelo pelas APIs do .NET (opção A) | Decisão do Manfred em 26/09/2026. Funciona em NTFS, exFAT, FAT e rede, com ou sem administrador. A leitura direta da MFT do NTFS fica para depois, atrás da mesma interface |
| Ações | Enviar para a Lixeira e mover, com confirmação e registro. Excluir definitivamente só em caminho de rede, onde não há Lixeira | Decisão do Manfred em 26/09/2026. O uso real termina em apagar ou mover |
| Linha de comando | Só varre e gera relatório. Nunca apaga nem move | Evitar que um roteiro apague arquivo sem ninguém olhar |
| Permissão | Roda como usuário comum. Um botão reabre o programa como administrador | Sem administrador, pastas protegidas ficam sem leitura. O programa mostra isso em vez de esconder |

## 4. Requisitos

| # | Requisito |
|---|---|
| R1 | Escolher o alvo: unidade local (com espaço livre e total), pasta digitada ou escolhida, ou caminho de rede `\\servidor\pasta`. Lista dos últimos alvos usados |
| R2 | Varredura em segundo plano, com resultado parcial na tela durante a varredura, botões Parar e Atualizar, e atualização de uma ramificação só (Shift+F5) |
| R3 | Árvore com as colunas Nome (com barra de proporção), Tamanho, Alocado, Arquivos, Pastas, % da pasta-pai e Última modificação, ordenável por qualquer coluna |
| R4 | Modos de exibição Tamanho, Alocado, Contagem e Porcentagem. Unidades Automática, GB, MB e KB, no formato brasileiro ("52,7 GB") |
| R5 | Expandir a árvore até N níveis. Voltar e avançar entre pastas visitadas |
| R6 | Os arquivos soltos de cada pasta ficam agrupados numa linha "[N arquivos]". `pagefile.sys`, `hiberfil.sys` e `swapfile.sys` recebem o rótulo "arquivo do sistema" |
| R7 | Pasta sem permissão de leitura aparece como "sem acesso", nunca como 0 Bytes. O total de pastas sem acesso aparece na barra de status, com o botão "Varrer como administrador" |
| R8 | Barra de status com espaço livre e total da unidade, número de arquivos, pastas sem acesso, tamanho do cluster, sistema de arquivos e "Liberado nesta sessão" |
| R9 | Menu de contexto na árvore e nas listas: Abrir no Explorer, Copiar caminho, Varrer a partir daqui, Atualizar ramificação, Propriedades, Mover e Enviar para a Lixeira |
| R10 | Gráficos da pasta selecionada: treemap e pizza |
| R11 | Maiores arquivos (100 por padrão, configurável) |
| R12 | Arquivos antigos: filtro "sem alteração há mais de 6 meses, 1, 2 ou 5 anos", com o total em GB |
| R13 | Resumo por tipo, com as extensões agrupadas em categorias: vídeo, imagem, áudio, e-mail (`.pst`, `.ost`), imagem de disco (`.iso`, `.vhd`, `.vhdx`), compactado e backup, instalador, documento e outros |
| R14 | Resumo por usuário: quando a varredura inclui `C:\Users` ou outra pasta de perfis, o ranking das pastas de perfil |
| R15 | Duplicados, sob demanda, confirmados em três etapas: mesmo tamanho, hash do primeiro 1 MB e hash completo |
| R16 | Ações: Enviar para a Lixeira e Mover, com o fluxo seguro da seção 7. Excluir definitivamente só em caminho de rede. Toda ação vai para o registro de ações |
| R17 | Exportação em HTML com a marca da MT (imprimível em PDF pelo navegador) e em CSV |
| R18 | Linha de comando para varrer e gerar relatório sem abrir a janela |
| R19 | Item "Analisar com MapDisk" no menu de contexto de pastas e unidades do Explorer, ligado e desligado em Opções, sem administrador |

## 5. Arquitetura

A divisão segue o MapNet:

| Pasta | Conteúdo |
|---|---|
| `src/mapdisk.nucleo` | Biblioteca `net8.0-windows` sem tela: toda a lógica. É o que os testes cobrem |
| `src/mapdisk` | Aplicativo WPF `net8.0-windows` e ponto de entrada da linha de comando. Gera o `mapdisk.exe` |
| `testes/mapdisk.testes` | Testes xUnit do núcleo |
| `ferramentas/` | Roteiro `publicar.cmd`, que roda os testes e gera o `.exe` |
| `public/` | Página do programa em `mapdisk.manfred.com.br` |
| `docs/superpowers/` | Specs, planos e pendências |

O núcleo usa `net8.0-windows`, e não `net8.0` como no MapNet, porque o tamanho alocado, o espaço livre, a Lixeira e o registro do Windows dependem de APIs do Windows. Por isso os testes rodam só no Windows.

### Módulos do núcleo

| Módulo | O que faz | Depende de |
|---|---|---|
| `varredura/` | Motor. Percorre as pastas em paralelo e monta a árvore com tamanho, alocado, arquivos, pastas, data e marcação de sem acesso ou erro. Informa progresso e aceita cancelamento. Fica atrás da interface `IMotorVarredura` | APIs de arquivo do .NET e do Win32 (`GetCompressedFileSize`, `GetFileInformationByHandle`) |
| `arvore/` | Modelo: nós de pasta, grupo de arquivos soltos, soma dos totais para cima e substituição de uma ramificação atualizada | Nada |
| `unidades/` | Lista de unidades com espaço livre e total, sistema de arquivos e tamanho do cluster. Reconhece caminho de rede | APIs do .NET e do Win32 (`GetDiskFreeSpace`) |
| `analises/` | Maiores arquivos, arquivos antigos, resumo por tipo, resumo por usuário e duplicados | `arvore/` |
| `acoes/` | Enviar para a Lixeira, mover e excluir definitivamente em rede. Regras de proteção, conferência antes da ação e registro de ações | `arvore/`, APIs do Shell do Windows |
| `formatacao/` | Tamanhos e porcentagens no formato brasileiro, nas unidades Automática, GB, MB e KB | Nada |
| `relatorios/` | Relatório HTML com a marca da MT e CSV | `arvore/`, `analises/` |
| `linha-de-comando/` | Leitura dos argumentos, no mesmo padrão do MapNet | Nada |
| `integracao/` | Liga e desliga o item do menu do Explorer em `HKCU\Software\Classes` | Registro do Windows |
| `painel/` | Estado da tela (modo, unidade, seleção, histórico de voltar e avançar), fora do WPF para ser testável | Os módulos acima |

### Aplicativo

- `programa.cs`: sem argumentos abre a janela. Com argumentos roda a linha de comando. `--demonstracao` abre com dados de exemplo. `--elevado <alvo>` é usado pelo botão "Varrer como administrador".
- Tema `tema-mt.xaml`, fonte Montserrat e logo copiados do MapNet.
- A árvore usa virtualização: só desenha as linhas visíveis.

### Fluxo de dados

1. O técnico escolhe o alvo.
2. O motor varre em segundo plano e, a cada 250 ms, entrega um retrato parcial à tela.
3. A árvore e a barra de status se atualizam sem travar a janela.
4. As análises da seção 6 rodam quando o técnico abre a aba correspondente, sobre a árvore já em memória. Duplicados lê o conteúdo dos arquivos e só roda quando o técnico pede.

### Regras do motor

- Espaço alocado pelo `GetCompressedFileSize`, arredondado para o tamanho do cluster da unidade. Em arquivo comprimido ou esparso, o alocado é menor que o tamanho.
- Arquivo com vários hard links soma uma vez só, pelo identificador do arquivo no volume.
- Junções e links simbólicos não são seguidos. Aparecem com o rótulo "link para <destino>" e não somam tamanho.
- Arquivo do OneDrive que está só na nuvem aparece com o rótulo "na nuvem": entra o tamanho lógico, o alocado é 0 e o programa não força o download.
- Caminho com mais de 260 caracteres é lido com o prefixo `\\?\`.
- Paralelismo com teto: de 4 a 16 tarefas em disco local, menos em caminho de rede, para não pesar no servidor.
- O nó guarda só números e o nome. O caminho completo é montado quando precisa.

## 6. Telas

### Janela principal

- **Faixa de comandos**, em quatro grupos:
  - Varredura: Selecionar alvo, Parar, Atualizar, Varrer como administrador.
  - Exibição: modo, unidade e Expandir níveis.
  - Ações: Mover, Enviar para a Lixeira.
  - Exportar: HTML, CSV.
- **Árvore com colunas** à esquerda (R3). Pastas sem acesso em cinza, com cadeado.
- **Painel de análise** à direita, recolhível, com abas sobre a pasta selecionada: Gráfico (treemap e pizza), Maiores arquivos, Arquivos antigos, Por tipo, Por usuário e Duplicados.
- **Barra de status** (R8).

### Selecionar alvo

Lista das unidades com barra de uso, campo para pasta ou caminho de rede e lista dos últimos alvos.

### Opções

Integrar ao menu do Explorer, quantidade de maiores arquivos, pastas excluídas da varredura e botão para abrir o registro de ações.

### Linha de comando

Exemplos:

```bat
mapdisk varrer D:\ --relatorio d.html
mapdisk varrer \\srv\dados --csv dados.csv --top 50
mapdisk --integrar
mapdisk --remover-integracao
```

A linha de comando não tem ação de mover nem de apagar.

## 7. Ações seguras

1. O técnico seleciona um ou mais itens na árvore ou em qualquer lista.
2. A confirmação mostra os itens, o total em GB e o destino. Na Lixeira, o aviso diz para qual Lixeira vai. No mover, o técnico escolhe a pasta de destino.
3. **Proteção:** o programa bloqueia ação em `C:\Windows`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\ProgramData`, `System Volume Information`, `$Recycle.Bin`, na raiz de qualquer unidade e nos arquivos de sistema do R6. O botão fica desativado e diz o motivo.
4. **Rede:** caminho de rede não tem Lixeira. Lá a ação vira "Excluir definitivamente", e a confirmação exige digitar `EXCLUIR`.
5. **Conferência na hora:** antes de agir, o programa confere se o tamanho e a data do item mudaram desde a varredura. Se mudaram, avisa e pede nova confirmação. No mover, confere se o destino tem espaço e bloqueia com "faltam X GB no destino".
6. **Mover entre unidades:** copia, confere o tamanho da cópia e só então apaga a origem. Se a cópia falhar, a origem fica intacta. Na mesma unidade, só renomeia o caminho.
7. **Execução:** barra de progresso com Cancelar. Item em uso ou sem permissão falha sozinho, os outros seguem, e no fim aparece o resumo do que deu certo e do que falhou, com o motivo.
8. **Depois:** a ramificação é atualizada sozinha e a barra de status soma o "Liberado nesta sessão".
9. **Registro:** toda ação vai para `%LOCALAPPDATA%\MapDisk\acoes.log`, uma linha por item, com data e hora, usuário do Windows, ação, caminho de origem, destino, tamanho e resultado.

## 8. Erros

| Situação | Comportamento |
|---|---|
| Pasta sem permissão | Marcada "sem acesso", contada na barra de status. A varredura segue |
| Rede cai durante a varredura | Ramificação marcada "erro de leitura", com o motivo. A varredura segue |
| Unidade removida durante a varredura | A varredura para e mostra o que já leu, com aviso |
| Arquivo em uso ou sem permissão na ação | Falha só aquele item. Aparece no resumo e no registro |
| Destino sem espaço | Bloqueado antes de começar |
| Item mudou desde a varredura | Aviso e nova confirmação |
| Registro de ações sem gravação possível | A ação não começa. Sem registro, sem ação |

## 9. Desempenho

- Meta: unidade com 1 milhão de arquivos em SSD varrida em menos de 60 segundos, sem administrador.
- A janela responde em menos de 100 ms durante a varredura.
- Cerca de 1 milhão de arquivos em menos de 300 MB de memória.

## 10. Privacidade e segredos

- **O que o programa lê:** nomes, tamanhos e datas de arquivos. Só o Duplicados lê o conteúdo, para calcular o hash, e não guarda nada dele.
- **Dado pessoal:** nome de pasta e de arquivo pode trazer nome de pessoa, como a pasta de perfil em `C:\Users`. O relatório e o registro de ações ficam só na máquina onde o programa rodou, na pasta que o técnico escolher. O programa não envia nada para fora.
- **Repositório:** relatório gerado em máquina de cliente, registro de ações e print de tela real nunca entram no git. Imagem de tela sai só do modo `--demonstracao`.
- **Segredos:** o programa não usa senha, chave nem token.
- **Enquadramento jurídico:** `docs/legal/verificacao-distribuicao-e-lgpd-2026-09-28.md`. Na distribuição, a MT não trata os dados de quem usa o programa. No atendimento a clientes, a MT é operadora e age por instrução do cliente. Os textos de confirmação, de uso autorizado e de licença saem da seção 5 da verificação.

## 11. Ordem das fatias

| Fatia | Conteúdo |
|---|---|
| 1 | Motor de varredura, árvore, formatação, unidades, janela com árvore e colunas, modos e unidades de exibição, barra de status, seleção de alvo local e de rede, linha de comando com CSV (R1 a R4, R6 a R8, R18 parcial) |
| 2 | Navegação e elevação: expandir níveis, voltar e avançar, atualizar ramificação, menu de contexto sem ações, "Varrer como administrador" (R2, R5, R7, R9 parcial) |
| 3 | Painel de análise: maiores arquivos, arquivos antigos, por tipo, por usuário (R11 a R14) |
| 4 | Gráficos: treemap e pizza (R10) |
| 5 | Ações seguras: Lixeira, mover, exclusão em rede, proteção, registro, "Liberado nesta sessão" (R16, R9 completo) |
| 6 | Duplicados (R15) |
| 7 | Relatório HTML com a marca da MT, integração ao Explorer e página `public/` (R17, R19, R18 completo) |

A ordem das fatias 3 a 7 pode mudar por decisão do Manfred.

## 12. Prioridade dos testes

1. **Ações não perdem dado:** proteção das pastas de sistema, mover que só apaga a origem depois de conferir a cópia, falha no meio sem perda, registro gravado antes da ação.
2. **Contas certas:** soma para cima, alocado por cluster, hard link contado uma vez, junção não seguida, sem acesso nunca vira 0.
3. **Formatação:** tamanhos e porcentagens em pt-BR.
4. **Análises:** filtro de antiguidade, categorias por extensão, as três etapas dos duplicados.
5. **Linha de comando:** leitura dos argumentos e ausência de qualquer ação destrutiva.
6. **Convenções herdadas do MapNet:** caracteres proibidos, nome de arquivo em minúsculas, fonte e licença embutidas, nenhum dado real no repositório.

Os testes de integração criam dentro da pasta de saída dos testes (`testes/mapdisk.testes/bin/...`) uma árvore com tamanhos conhecidos, junção, hard link, caminho longo e pasta com permissão negada, e comparam a varredura com o esperado.

Teste manual antes de cada versão: C: com e sem administrador, compartilhamento de rede, mover e Lixeira em pasta de teste, e um Windows Server.

## 13. Fora da versão 1

- Leitura direta da MFT do NTFS.
- Nuvem por API (OneDrive, SharePoint, Google Drive).
- Compressão NTFS pelo programa.
- Varredura agendada, envio por e-mail e comparação entre varreduras.
- Item no menu compacto do Windows 11, que exige empacotamento MSIX e assinatura.
- Mover ou apagar pela linha de comando.
