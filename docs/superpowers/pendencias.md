# Pendências

O que ficou de fora, com o motivo e o que fecha o item.

## Dia zero

| Item | Motivo | O que fecha |
|---|---|---|
| Autorização do cliente para exclusões no atendimento da MT | No atendimento, a MT é operadora dos dados do cliente e apaga ou move arquivos por instrução dele (LGPD, arts. 37 e 39). Verificação de 28/09/2026 | Decidido pelo Manfred em 29/09/2026: campo fixo na ordem de serviço da MT, com o registro de ações anexado. Mensagem escrita do cliente (e-mail ou WhatsApp) só em emergência, guardada junto com o registro. Texto do campo abaixo. Falta incluir o campo no modelo de OS da MT, fora deste repositório |
| Ícone e logo do MapDisk | Não havia arte do MapDisk | Fechado em 29/09/2026: arte recebida, ícone em DIB no `.exe` e na janela, símbolo na faixa do topo. Detalhes em `docs/marca/leia-me.md` |
| Pasta antiga `C:\COWORK\CODE\TREEZISE-MT` | Ficou vazia depois da troca de nome. O Windows negou a exclusão pela sessão | O Manfred apagar pelo Explorer |
| Repositório público no GitHub | Criar repositório pede autorização | Fechado em 29/09/2026: `manfredjr/mapdisk`, público, com os ganchos de backup ligados |

### Campo da ordem de serviço

Rascunho feito a partir da seção 4.2 da verificação jurídica de 28/09/2026. Antes de entrar no modelo de OS, passa pela `legal-br` junto com o restante da OS.

> **Autorização para excluir ou mover arquivos.** Autorizo a MT - Manfred Tecnologia a enviar para a Lixeira, mover ou excluir os arquivos e pastas descritos abaixo, nos equipamentos indicados nesta ordem de serviço. Declaro que posso decidir sobre esses arquivos em nome da empresa. Sei que a exclusão em pastas de rede não pode ser desfeita pelo programa e que, nesse caso, a recuperação depende de cópia de segurança. O registro das ações executadas segue anexo a esta ordem de serviço.
>
> Itens autorizados: [descrição ou caminho]
> Destino, quando for mover: [pasta ou disco]
> Responsável: [nome] - Cargo: [cargo] - Data: [data] - Assinatura ou aceite: [assinatura]

## Fatia 1: varredura, árvore e janela básica

| Item | Motivo | O que fecha |
|---|---|---|
| Lista dos últimos alvos (R1) | Fica para a fatia 2, junto com a navegação | Fechado em 29/09/2026 na fatia 2 |
| Atualizar só uma ramificação (R2) | Fica para a fatia 2, com o menu de contexto | Fechado em 29/09/2026 na fatia 2 (Shift+F5 e menu de contexto) |
| Botão "Varrer como administrador" (R7) | Fica para a fatia 2. Sem administrador, um disco de sistema costuma ter centenas de pastas sem acesso | Fechado em 29/09/2026 na fatia 2, com o privilégio de backup |
| Hard link em caminho de rede | O identificador vem do servidor e pode repetir entre discos dele. Em rede, o hard link soma mais de uma vez | Aceito. Reavaliar se aparecer caso real |
| Unidade de rede mapeada e desligada pode atrasar a lista de unidades | O `DriveInfo.IsReady` espera a rede responder | Aceito na fatia 1. Reavaliar se atrapalhar |
| Rótulo "sem acesso" repetido na linha da pasta | A coluna do valor e o rótulo dizem a mesma coisa | Fechado em 29/09/2026 na fatia 2: a pasta sem acesso e a não lida ficam sem rótulo |
| Memória acima da meta | A spec pede menos de 300 MB para 1 milhão de arquivos. No teste manual de 29/09/2026, num disco com pouco mais de 1 milhão de arquivos, o pico passou de 300 MB. O tempo ficou dentro da meta de 60 s. Suspeitos: o dicionário de identificadores de hard link, com uma entrada por arquivo, e os nomes guardados em cada arquivo | Reduzido na fatia 2 (buffer e listas reaproveitados, conjunto de identificadores em faixas, coletor em modo de economia): o pico caiu cerca de um terço. Na fatia 3, os nomes de arquivo repetidos passaram a ser guardados uma vez só, com mais uns 10% de ganho na medida. Segue um pouco acima de 300 MB num disco com pouco mais de 1 milhão de arquivos. Próximo passo, se fizer falta: guardar a data do arquivo em 4 bytes em vez de 8 e medir de novo com `ferramentas/medir-memoria.ps1` |
| Uma varredura do C: com menos arquivos | No teste manual de 29/09/2026, uma de seis varreduras do mesmo disco veio com cerca de 4% a menos de arquivos e de pastas. As outras cinco, pela janela e pela linha de comando, bateram entre si. Não se repetiu. O teste `Muitas_varreduras_seguidas_dao_sempre_o_mesmo_total` varre 30 vezes uma árvore de mais de mil pastas com 16 tarefas, e passou em 90 varreduras. Pode ter sido o disco mudando naquele minuto ou uma corrida rara no motor | Fechado em 29/09/2026 na fatia 2: ao terminar, a varredura conta as pastas sem leitura e avisa se houver alguma. Um teste de nome de arquivo que falhava às vezes foi achado e corrigido no caminho |

## Fatia 2: navegação, elevação e acertos da fatia 1

| Item | Motivo | O que fecha |
|---|---|---|
| Esquecer um alvo da lista de últimos alvos | Fora do escopo da fatia 2. A lista guarda até 10 caminhos só nesta máquina, em `%LOCALAPPDATA%\MapDisk\alvos.txt` | Opções da fatia 7, junto com "limpar a lista" |
| Hard link depois de "Atualizar esta pasta" | A releitura confere hard link só dentro da pasta relida. Um arquivo com outro nome fora dela passa a somar duas vezes | Aceito. Reavaliar se aparecer caso real |
| Leitura como administrador em caminho de rede | O privilégio de backup vale só para disco local. Em `\\servidor\pasta`, quem manda são as permissões do servidor | Aceito, é como o Windows funciona |
| Conferência da janela elevada | O aviso de elevação do Windows é confirmado pelo Manfred, não pelo agente | Teste do Manfred no PR da fatia 2 |
