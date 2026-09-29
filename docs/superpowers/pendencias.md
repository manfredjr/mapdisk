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
| Lista dos últimos alvos (R1) | Fica para a fatia 2, junto com a navegação | Fatia 2 |
| Atualizar só uma ramificação (R2) | Fica para a fatia 2, com o menu de contexto | Fatia 2 |
| Botão "Varrer como administrador" (R7) | Fica para a fatia 2. Na conferência de 29/09/2026, o C: do Manfred sem administrador teve 446 pastas sem acesso | Fatia 2 |
| Hard link em caminho de rede | O identificador vem do servidor e pode repetir entre discos dele. Em rede, o hard link soma mais de uma vez | Aceito. Reavaliar se aparecer caso real |
| Unidade de rede mapeada e desligada pode atrasar a lista de unidades | O `DriveInfo.IsReady` espera a rede responder | Aceito na fatia 1. Reavaliar se atrapalhar |
| Rótulo "sem acesso" repetido na linha da pasta | A coluna do valor e o rótulo dizem a mesma coisa | Revisar o texto da linha junto com o menu de contexto da fatia 2 |
| Memória acima da meta | A spec pede menos de 300 MB para 1 milhão de arquivos. Medido em 29/09/2026 no C: do Manfred, sem administrador: pico de 490 MB para 1.144.842 arquivos. O tempo cumpre a meta de 60 s: 35,1 s na primeira varredura e 17,3 s com o cache do disco aquecido. Suspeitos: o dicionário de identificadores de hard link, com uma entrada por arquivo, e os nomes guardados em cada arquivo | Fatia 2: medir cada parte e reduzir, por exemplo guardando só os identificadores de arquivos com mais de um link |
| Uma varredura do C: com menos arquivos | No teste de 29/09/2026, uma varredura pela janela deu 1.096.976 arquivos e 225.253 pastas, em 6,5 s. Outras 5 varreduras, antes e depois, pela janela e pela linha de comando, deram entre 1.143.748 e 1.143.794 arquivos e cerca de 229.000 pastas. Não se repetiu. O teste `Muitas_varreduras_seguidas_dao_sempre_o_mesmo_total` varre 30 vezes uma árvore de mais de mil pastas com 16 tarefas, e passou em 90 varreduras. Pode ter sido o disco mudando naquele minuto ou uma corrida rara no motor | Fatia 2: registrar, ao fim de cada varredura, quantas pastas foram lidas e quantas entraram na fila, e mostrar aviso se não baterem |
