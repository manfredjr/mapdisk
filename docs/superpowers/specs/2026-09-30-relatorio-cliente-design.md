# MapDisk - MT: relatório para o cliente avaliar

Data: 30/09/2026. Autor: Manfred Heil Junior.

Complementa o desenho da versão 1 (`2026-09-28-mapdisk-design.md`) com o requisito R20. Decisões tomadas com o Manfred em 30/09/2026.

## 1. O pedido

Quando uma pasta, quase sempre de rede, está cheia, o técnico não sabe sozinho o que pode sair: os documentos são do cliente. Hoje ele manda uma lista por e-mail e procura à mão o que o cliente respondeu. O MapDisk passa a gerar um relatório para o cliente decidir item por item e a ler a resposta de volta, selecionando os itens para a ação que o técnico confirma na janela.

| # | Requisito |
|---|---|
| R20 | Relatório para o cliente avaliar o que pode ser apagado ou movido: lista montada pelo técnico, gerada em página HTML (imprimível em PDF) e em planilha Excel, com as opções Apagar, Mover, Manter e Conversar por item, e leitura da resposta que seleciona os itens para as ações da seção 7 do desenho da versão 1 |

## 2. Decisões

| Decisão | Escolha | Motivo |
|---|---|---|
| Como o cliente responde | Nos três formatos: planilha Excel com a coluna Decisão, página HTML com caixas de marcar e arquivo de resposta, e PDF impresso da página, respondido por e-mail com os números | Cada cliente responde do jeito que sabe. Decisão do Manfred |
| Quem monta a lista | Três jeitos num fluxo só: "Sugerir" automático pelos critérios, "Acrescentar a seleção" da árvore e das listas, e "Tirar da lista" para ajustar | Decisão do Manfred |
| Critérios do "Sugerir" | Editáveis na janela, com os padrões: 20 maiores pastas um nível abaixo da pasta mostrada; 50 maiores arquivos; arquivos sem alteração há mais de 2 anos e acima de 100 MB; imagem de disco, compactado e backup, e instalador acima de 100 MB | Aprovados pelo Manfred |
| Opções por item | Apagar, Mover (o cliente escreve o destino), Manter e Conversar. Nada vem marcado; item sem marca conta como manter | Aprovadas pelo Manfred. Sem pré-marcação, nada sai por descuido |
| Ordem | Nova fatia 6. Duplicados passam para a 7 e relatório do técnico, integração ao Explorer e página passam para a 8 | Aprovada pelo Manfred |
| Número do item | #1, #2... igual na página, na planilha e no PDF | O cliente pode responder por e-mail citando o número |
| Item repetido | Arquivo dentro de uma pasta que já está na lista não entra de novo | A decisão sobre a pasta já o leva |
| Pasta sem leitura | Nunca entra na lista | Regra 3: tamanho desconhecido |
| Onde salvar | Pasta escolhida pelo técnico na hora de gerar | Regra 6 |
| Arquivos gerados | `avaliacao-<pasta>-<aaaa-mm-dd>.html` e `.xlsx`, com nome em minúsculas e sem acento | Convenção do projeto |

## 3. Fluxo

1. O técnico varre a pasta.
2. O botão **Relatório para o cliente**, no cartão Pastas, abre a janela do relatório, sobre a raiz mostrada na árvore.
3. A janela tem a lista de itens, com número, nome, caminho, tamanho, quantidade de arquivos (em pasta), última alteração e motivo ("entre as 20 maiores pastas", "sem alteração há mais de 2 anos", "backup", "escolhido pelo técnico"). Os botões **Sugerir**, **Acrescentar a seleção** e **Tirar da lista** montam a lista. Campos: nome do cliente (obrigatório), técnico (padrão: usuário do Windows) e mensagem para o cliente (opcional).
4. **Gerar** pede a pasta de saída e grava a página e a planilha.
5. O cliente responde por um dos três caminhos.
6. O técnico varre a pasta de novo e clica em **Ler resposta do cliente**, que aceita a planilha devolvida ou o arquivo de resposta da página. Resposta do PDF, por e-mail, o técnico marca pelos números na mesma janela ("Marcar pelos números", por exemplo `1, 3, 7-9`).
7. A janela da resposta mostra quatro grupos: Apagar, Mover (com o destino escrito pelo cliente), Conversar e Manter, com quantidade e total. **Selecionar para apagar** e **Selecionar para mover** levam os itens para as ações da fatia 5, com a confirmação de sempre.

## 4. A página HTML

- **Um arquivo só:** logo da MT, estilo e código dentro do arquivo. Nada é carregado da internet (regra 6). O único endereço externo é o link para `www.manfred.com.br`, que só abre se o cliente clicar.
- **Conteúdo:** cabeçalho com a marca da MT, cliente, pasta, data da varredura e número do relatório; apresentação curta; mensagem do técnico; resumo para contexto (total da pasta, espaço livre do volume quando conhecido, maiores pastas, por tipo e antigos); a lista numerada com as quatro opções, um campo de destino (só no Mover) e um de observação por item; observação geral; nome de quem decide.
- **Salvar resposta:** gera `avaliacao-<numero>-resposta.json` com o número do relatório, quem decidiu, a data e hora, e para cada item o número, o caminho, o tamanho e a data da varredura, a decisão, o destino e a observação. O navegador baixa o arquivo; o cliente devolve por e-mail.
- **Imprimir ou salvar em PDF:** folha de impressão própria, sem botões, com as quatro opções como caixas vazias para marcar à mão.

## 5. A planilha Excel

- Formato `.xlsx` gravado pelo próprio .NET (`System.IO.Compression` e XML), sem pacote NuGet.
- Aba **Avaliação**: cabeçalho com cliente, pasta, data e número do relatório; células "Decidido por" e "Data da decisão" para o cliente preencher; a tabela com número, nome, caminho, tamanho, arquivos, última alteração, motivo, **Decisão** (lista de opções Apagar, Mover, Manter, Conversar), Destino e Observação.
- Aba **Controle**, oculta: número do relatório e, por item, o caminho, o tamanho em bytes e a data da varredura, para casar a resposta.
- A leitura aceita a planilha salva pelo Excel e pelo LibreOffice (textos compartilhados e textos na própria célula).

## 6. A leitura da resposta

- O item casa pelo caminho com a varredura atual. Item que não existe mais aparece como "não encontrado". Item com tamanho ou data diferentes dos do relatório aparece como "mudou depois do relatório" e não é selecionado sozinho.
- Resposta de outro relatório, arquivo corrompido ou planilha sem a aba Controle: aviso claro, nada é selecionado.
- A leitura nunca age: ela só seleciona. As ações seguem as regras da fatia 5 (proteção, confirmação, registro).
- O arquivo devolvido, com quem decidiu e a data, é a autorização do cliente que a MT anexa à ordem de serviço (pendência jurídica do dia zero).

## 7. Dados e textos

- O relatório tem nomes de pastas e de arquivos do cliente. Fica só na pasta que o técnico escolher e nunca entra no repositório. Os testes usam só a árvore de demonstração e árvores fictícias.
- A apresentação e o texto de autorização da página e da planilha passam pela `legal-br`, a partir da seção 4.2 da verificação jurídica (a MT como operadora, por instrução do cliente), e pela `humanizar-ptbr`.

## 8. Módulos

| Onde | O que faz |
|---|---|
| `relatorios/avaliacao.cs` | Modelo do relatório: cabeçalho, itens numerados com motivo, resumo |
| `relatorios/sugestao.cs` | Critérios e o "Sugerir", sem repetir item dentro de pasta da lista |
| `relatorios/pagina-avaliacao.cs` | Gera a página HTML |
| `relatorios/planilha-avaliacao.cs` | Grava e lê a planilha `.xlsx` |
| `relatorios/resposta-avaliacao.cs` | Lê o arquivo de resposta da página, marca pelos números e casa a resposta com a árvore |
| `painel/painel-relatorio.cs` | Estado da janela do relatório e da janela da resposta, sem WPF |
| `src/mapdisk/janela-relatorio.xaml` e `janela-resposta.xaml` | As duas janelas |

## 9. Testes

- Sugestão: cada critério, os padrões, item dentro de pasta da lista fora, pasta sem leitura fora.
- Página: contém todos os itens e números; não tem `src=` nem `url(` para fora do arquivo; o texto passa no teste de caracteres proibidos.
- Planilha: gravar, preencher a coluna Decisão como o Excel faria e ler de volta; ler com textos compartilhados e com textos na célula.
- Resposta: ler o JSON da página; "Marcar pelos números" com `1, 3, 7-9`; casar com a árvore, com item não encontrado e item que mudou.
- Teste manual do Manfred: gerar o relatório de uma pasta de teste, abrir a página e a planilha, responder pelos dois, ler de volta e apagar pela confirmação.

## 10. Fora desta fatia

- Duplicados como critério do "Sugerir" (fatia 7, quando os duplicados existirem).
- Envio do relatório por e-mail pelo programa: fica com o técnico (regra 6).
- Dono do arquivo (proprietário no Windows): a varredura lê só nome, tamanho e data (regra 7).
