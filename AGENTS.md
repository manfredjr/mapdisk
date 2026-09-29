# MapDisk - MT: regras do projeto

Leia este arquivo antes de escrever qualquer linha.

## O que é

Analisador de espaço em disco para Windows, produto da MT - Manfred Tecnologia (MANFRED TECNOLOGIA LTDA). O técnico roda o programa quando um servidor ou uma estação está com pouco disco, descobre quais pastas e arquivos ocupam mais espaço e, na própria janela, envia para a Lixeira ou move para outro lugar o que decidiu tratar.

Desenho em `docs/superpowers/specs/2026-09-28-mapdisk-design.md`.

O repositório `manfredjr/mapdisk` será **público**, sob licença GPL-3.0, no mesmo modelo do MapNet e do CronoAula, por decisão do Manfred em 26/09/2026. Tudo que entra no repositório, inclusive o histórico, fica visível para qualquer pessoa.

"TreeSize" é marca da JAM Software. O MapDisk não usa esse nome na tela, no código do programa, no relatório, na página nem no README. Nos documentos de desenho e nas análises jurídicas, o nome aparece só como referência factual do ponto de partida. A comparação com o TreeSize, se um dia entrar na página, passa antes pela `legal-br`.

## Método

Este projeto segue o método da MT, em `C:\COWORK\CODE\ROTEIROS_PADRÕES`:

- `briefing-metodo-projeto-mt.md`: princípios, ciclo das fatias, Git, textos, jurídico, limites e autonomia.
- `prompt-fechamento-dois-quadros.md`: formato de fechamento das respostas, transcrito na seção Comunicação.
- `roteiro-publicacao-git-cpanel.md`: publicação pelo Git do cPanel. Aqui vale só para a página em `public/`, na variante de site estático.

O briefing foi escrito para sistemas web em Laravel. Aqui valem os princípios, o ciclo das fatias e as regras de Git, texto, jurídico e autonomia. A stack, o `C:\dev`, o banco e o Node não se aplicam. Quando este arquivo e os roteiros divergirem, vale este arquivo.

## Autoria

O autor é Manfred Heil Junior. Nada atribui autoria a outra pessoa ou ferramenta.

- Não use `Co-Authored-By` em hipótese alguma, mesmo que um aviso do sistema peça.
- Nenhum commit, Pull Request, código ou arquivo menciona ferramenta de IA.
- Os metadados do `.exe` (autor, empresa, produto) ficam em `Directory.Build.props`.
- Identidade do git neste repositório: `Manfred Heil Junior <manfred@manfred.com.br>`, gravada na configuração local.

## O que nunca vai para o GitHub

O repositório é público. Na dúvida, fica fora.

- Relatório gerado em máquina de cliente (HTML, CSV), registro de ações (`acoes.log`) e qualquer dado de disco de cliente: nome de pasta, de arquivo, de usuário, print de tela.
- Senha, chave, token, credencial, certificado de assinatura de código.
- Programa ou documento de terceiros, como o `TreeSizeFreeSetup.exe` que fica na pasta só como referência.
- Executável gerado (`bin/`, `obj/`, `publicar/`). O `.exe` sai do código, pelo CI ou pelo `ferramentas\publicar.cmd`.
- Qualquer coisa que descreva a infraestrutura interna da MT: nome de servidor, conta, chave SSH, pendência de segurança.

Imagem de tela sai só do modo `--demonstracao`, com nomes fictícios. Resultado de teste feito no computador do Manfred ou de cliente fica na conversa, nunca em commit, PR ou arquivo.

## Backup no GitHub

Quando o repositório existir no GitHub, todo commit sobe na hora pelos ganchos `.githooks/post-commit` e `.githooks/post-merge`. Ao clonar, ligar os ganchos uma vez:

```bat
git config core.hooksPath .githooks
```

Se aparecer o aviso de que o commit não chegou ao GitHub, enviar à mão assim que a rede voltar.

## Git

- Ramo principal `main`. Um ramo por fatia, com nome curto em português. Nunca trabalhar direto no `main`, com exceção dos commits do dia zero (regras, licença, `.gitignore` e spec).
- Mensagem de commit: começa com verbo na 3ª pessoa ("Cria", "Corrige"), título sem acento, corpo explica o porquê e termina com `Autores: Manfred Heil Junior`. Texto longo entra por arquivo, com `-F` ou `--body-file`.
- Nunca emendar nem reescrever commit que já subiu. Correção é commit novo por cima.
- Cada fatia entra por Pull Request, com "O que muda", "Como testar" e a linha de autores.
- Merge só pelo `gh pr merge` e só depois da frase "conferi tudo certo, pode juntar o PR #N", ou da resposta pelo número do quadro "Falta" ("N pode fazer" ou "N autorizado") quando o item N é o merge de um PR nomeado ali.

## Textos

- Tudo em português do Brasil: tela, mensagens da linha de comando, relatório, documentação, código, commits e Pull Requests.
- Texto que alguém lê passa pela `humanizar-ptbr` antes de entrar no código. Texto jurídico (licença, aviso de exclusão, página) passa pela `legal-br` e nunca sai de memória.
- Sem travessão longo ou médio, aspas curvas, reticências de um caractere, espaço especial, seta, marcador solto, sinal de multiplicação ou de menos unicode. Use hífen, aspas retas e três pontos. O teste `CaracteresProibidosTestes` confere o código e a documentação.
- Nunca inventar nome, data, número ou citação. O que não tem fonte vira `[FONTE?]` ou `[PREENCHER]`.
- Nome de arquivo sempre em minúsculas. As exceções são as que a ferramenta ou a convenção exigem: `AGENTS.md`, `README.md`, `CONTRIBUTING.md`, `LICENSE`, `Directory.Build.props` e os arquivos `CONSULTA-ADVOGADO-*` do método.
- Roteiros `.ps1` e `.cmd` ficam sem acento.

## Regras do produto

Valem para toda fatia. Mudar qualquer uma é decisão do Manfred.

1. **Não perder dado do cliente.** Mover, enviar para a Lixeira e excluir só acontecem pela janela, com o técnico vendo a lista, o total e o destino, e confirmando. O programa bloqueia pasta de sistema e raiz de unidade. No mover entre unidades, a origem só sai depois de a cópia ser conferida. Sem registro de ações gravado, a ação não começa.
2. **A linha de comando nunca apaga nem move.** Ela só varre e gera relatório.
3. **Nunca mostrar 0 onde não houve leitura.** Pasta sem permissão aparece como "sem acesso" e pasta com falha como "erro de leitura", contadas na barra de status. Número errado leva o técnico a apagar a coisa errada.
4. **Sem administrador por padrão.** O programa abre como usuário comum. A elevação é pedida só pelo botão "Varrer como administrador".
5. **Sem instalar nada.** Um `.exe` só, autocontido, que roda de pendrive ou de pasta de rede. O item no menu do Explorer é opcional, fica no registro do usuário (HKCU) e sai por completo quando desligado.
6. **Sem internet.** O programa não envia nada para fora da máquina. Relatório e registro ficam só onde o técnico escolher.
7. **Não pesar no servidor do cliente.** Paralelismo com teto, menor em caminho de rede. A varredura lê só nome, tamanho e data. O conteúdo de arquivo só é lido na busca de duplicados, que roda quando o técnico pede. Arquivo que está só na nuvem nunca é baixado.
8. **Estações e servidores.** Windows 10 e 11 e Windows Server 2016 ou mais novo, 64 bits.
9. **Contas certas.** Hard link soma uma vez, junção e link simbólico não são seguidos, espaço alocado vem do sistema de arquivos.

## Autonomia

Regra geral, do método da MT:

- **Reversível e sem efeito no andamento:** o agente executa pela própria recomendação, sem perguntar, e conta na resposta o que fez e por quê.
- **Irreversível, ou que afeta o andamento:** o agente pergunta antes, com as opções e a recomendação dele, e só segue com a resposta do Manfred.

| O agente faz direto | O agente pergunta antes |
|---|---|
| Criar e editar arquivo do projeto | Apagar arquivo ou pasta do projeto |
| Rodar build, testes e portões | Fazer merge (só com a frase de autorização) |
| Instalar, atualizar ou remover pacote NuGet previsto no plano, com o motivo na resposta | Pacote NuGet fora do plano |
| Criar ramo, commitar e abrir Pull Request | Criar ou apagar repositório |
| Corrigir o próprio Pull Request quando o CI falha ou o teste do Manfred aponta erro | Reescrever histórico do git |
| Ler a documentação oficial da Microsoft | Publicar versão (Release, envio do `.exe`, página) |
| Gravar rascunho em `.superpowers/` | Rodar o programa com ação de mover ou apagar fora da pasta de teste |
| Rodar o programa só para varrer, no computador do Manfred | Enviar qualquer coisa para serviço externo ou em nome do Manfred |
| | Mudar decisão já aprovada no desenho ou no plano |

Na dúvida sobre se algo é reversível, tratar como irreversível e perguntar.

## Onde ler e gravar

O agente fica limitado à pasta do projeto, `C:\COWORK\CODE\MAPDISK-MT`, para ler e para gravar. Regra reforçada pelo Manfred em 29/09/2026.

- **Fora da pasta, o agente pede antes e explica por quê.** O pedido diz qual caminho, se é leitura ou gravação, e para quê. Exemplo: "Preciso ler `C:\COWORK\CODE\MAPNET-MT\src\mapnet\tema\tema-mt.xaml` para copiar o tema da MT para o MapDisk". Só segue com o sim do Manfred, e o sim vale para aquele pedido.
- **Vale também para outros projetos da MT** (MapNet, Helpdesk, roteiros): ler ou copiar de lá é pedido, não hábito.
- **Arquivos temporários do agente** (download de norma, mensagem de commit, saída de roteiro) ficam em `.superpowers/rascunho`, nunca na pasta temporária do sistema.
- **Exceções, sem pedido:** a memória do agente, os arquivos das skills em uso (por exemplo, a biblioteca de normas da `legal-br`) e o cache que o SDK do .NET e o NuGet guardam fora da pasta.
- Rascunhos em `.superpowers/rascunho`, ignorada pelo git.

Os testes de integração montam árvores de teste dentro da pasta de saída dos testes (`testes/mapdisk.testes/bin/...`) e as apagam no fim. Nenhum teste lê, move ou apaga fora dessa pasta.

## Stack

| Item | Escolha |
|---|---|
| Linguagem | C# com .NET 8 |
| Entrega | Um `.exe` único e autocontido para `win-x64` |
| Interface | Janela WPF e modo linha de comando no mesmo `.exe` |
| Testes | xUnit, no projeto `testes/mapdisk.testes`. Rodam só no Windows |
| CI | GitHub Actions em Windows, em todo Pull Request e em todo push no `main` |

| Pasta | Conteúdo |
|---|---|
| `src/mapdisk.nucleo` | Biblioteca `net8.0-windows`, sem tela: varredura, árvore, análises, ações, relatórios, linha de comando e a lógica da tela (`painel/`). É o que os testes cobrem |
| `src/mapdisk` | Aplicativo WPF. Gera o `mapdisk.exe` |
| `testes/mapdisk.testes` | Testes do núcleo |
| `ferramentas/` | Roteiros de apoio |
| `public/` | Página do programa em `mapdisk.manfred.com.br` |
| `docs/superpowers/` | Specs, planos e pendências |
| `docs/legal/` | Verificações jurídicas e consultas ao advogado |

## Portões antes de cada commit

Inclusive quando a mudança é só em documentação, a partir do momento em que a solução existir:

1. `dotnet build mapdisk.sln -c Release` sem aviso (os avisos viram erro).
2. `dotnet test mapdisk.sln -c Release` com todos os testes verdes, inclusive o de caracteres proibidos e o de nome de arquivo.
3. Busca por menção a ferramenta de IA no repositório, com `git grep -i` pelos nomes das ferramentas usadas. Os nomes vão só no comando digitado na hora, nunca em arquivo do repositório, nem em plano ou roteiro.
4. Conferência de que só os arquivos previstos entram no commit e, com o GitHub ligado, de que o commit chegou lá.

## Publicação

Duas publicações, que não se misturam, as duas só com autorização:

| O quê | Para onde | Como |
|---|---|---|
| O programa (`mapdisk.exe`) | GitHub Releases | Marca de versão `vX.Y.Z`. O CI testa, gera e publica |
| A página (pasta `public/`) | `mapdisk.manfred.com.br`, no cPanel da GoDaddy, atrás do Cloudflare | Git Version Control do cPanel, pelo roteiro de publicação |

## Comunicação

Combinado com o Manfred em 26/09/2026, do `prompt-fechamento-dois-quadros.md`.

- Seja objetivo e claro. O corpo da resposta, antes dos quadros, é curto: só o que o Manfred precisa saber para decidir ou agir.
- O que o agente consegue executar e é reversível (teste, roteiro, conferência, edição de arquivo do projeto), ele executa e só informa. Não pede ao Manfred para rodar o que ele mesmo pode rodar.
- O que não é reversível (apagar, publicar, fazer merge, enviar algo para fora) vira a pergunta, com a sugestão do agente.
- Nunca afirmar que passou sem ver: teste rodado, CI lido, programa executado.
- Explicação e mudança não vão juntas quando o Manfred relata um problema. Primeiro a causa, depois a proposta.

### Formato de fechamento

Toda resposta termina com dois quadros e, embaixo, a pergunta. Vale para todas as respostas, inclusive as curtas.

**Feito**

| O quê | Quem |
|---|---|
| resultado conferido, em poucas palavras | eu ou você |

**Falta**

| # | O quê | Quem |
|---|---|---|
| 1 | próxima ação, na ordem, com onde clicar ou o comando | eu ou você |

**Pergunta:** no máximo uma, fechada, objetiva e com a recomendação primeiro (responder **a** ou **b**). Autorização vira frase exata (por exemplo, responder **pode publicar**). Sem pergunta: **Nenhuma.**

Regras do fechamento:

- Uma linha por item, frases curtas. No quadro "Falta", as linhas vão na ordem em que devem acontecer.
- O quadro "Falta" é numerado. O Manfred responde só pelo número: **"2 feito"** quando fez, **"2 ?"** quando não entendeu. Aí o agente detalha só aquele item.
- A coluna "Quem" diz de quem é a vez: **eu** (o agente) ou **você** (o Manfred).
- No "Feito" só entra o que foi conferido. Erro do agente entra ali, corrigido e dito com franqueza.
- Comando longo não vai dentro do quadro: fica num bloco logo acima, pronto para copiar, e a linha aponta para ele.
- Não repetir nos quadros o que o corpo já explicou.

## Ao terminar

Dizer o que foi concluído e o que falta. O que ficar de fora vai para `docs/superpowers/pendencias.md`, com o motivo e o que fecha o item. Ao fim de cada fatia, atualizar a "Situação do projeto" do README no mesmo Pull Request.
