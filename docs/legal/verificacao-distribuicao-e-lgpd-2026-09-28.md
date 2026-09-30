# Verificação jurídica: distribuição do MapDisk - MT, ações sobre arquivos e LGPD

Data: 28/09/2026. Objeto: MapDisk - MT, software livre (GPL-3.0) desenvolvido e distribuído pela MANFRED TECNOLOGIA LTDA (MT - Manfred Tecnologia), CNPJ 21.075.901/0001-12, microempresa. Situação: desenho da versão 1 aprovado (`docs/superpowers/specs/2026-09-28-mapdisk-design.md`), sem código.

Esta análise parte da feita para o MapNet - MT em 26/09/2026 (`C:\COWORK\CODE\MAPNET-MT\docs\legal\`), cujo parecer tratou da distribuição gratuita sob GPL-3.0 com finalidade de divulgação. Os fatos da distribuição são os mesmos. O que muda no MapDisk são dois pontos: o programa **apaga e move arquivos**, e a **equipe técnica da MT vai usá-lo no atendimento a clientes**.

## 1. Contexto e fatos

- **O que o programa faz:** varre unidades locais e compartilhamentos de rede e mostra quanto espaço ocupam pastas e arquivos. Lê nome, tamanho e data. O conteúdo de arquivo só é lido na busca de duplicados, para calcular o hash, e não é guardado.
- **Ações:** pela janela, com confirmação, o usuário envia itens para a Lixeira, move para outra pasta ou disco e, em caminho de rede (onde não há Lixeira), exclui definitivamente. Pastas de sistema e raiz de unidade ficam bloqueadas. A linha de comando não tem ação.
- **Registro de ações:** arquivo local `acoes.log`, no perfil do usuário do Windows que rodou o programa, com data e hora, usuário do Windows, ação, caminhos, tamanho e resultado.
- **O que sai da máquina:** nada. Relatório e registro ficam onde o usuário escolher.
- **Distribuição:** de graça, pela página `mapdisk.manfred.com.br` e pelo GitHub, para qualquer pessoa. Finalidade: divulgar a MT.
- **Uso pela MT:** a equipe técnica da MT roda o programa em servidores e estações de clientes, quando falta espaço, e apaga ou move arquivos como parte do atendimento (informação do Manfred em 26/09/2026). Isso é diferente do MapNet, que a MT só distribui.
- **Dados que aparecem:** nomes de pastas e arquivos podem conter nomes de pessoas (a pasta de perfil `C:\Users\<nome>`, documentos com nome de cliente ou funcionário).

## 2. Fontes e verificação

| Norma | Onde foi verificada | Data |
|---|---|---|
| LGPD (Lei nº 13.709/2018), arts. 4º, I, 5º, I, VI, VII, X e XIV, 37, 39 e 46 | Biblioteca local da `legal-br`, `fontes-md/leis/03-lgpd-lei-13709-2018-compilado.md`, marcada EM DIA no relatório de 23/09/2026 | 28/09/2026 |
| CDC (Lei nº 8.078/1990), arts. 2º, 3º, § 1º e § 2º, 6º, III, 25 e 51, I | planalto.gov.br, texto compilado (a cópia local está marcada MUDOU no relatório de 23/09/2026 e não foi usada) | 28/09/2026 |
| Código Penal, art. 154-A, caput e § 1º | planalto.gov.br, texto compilado do Decreto-Lei nº 2.848/1940 | 28/09/2026 |
| Código Civil, arts. 186 e 927 | Biblioteca local, `fontes-md/leis/01-codigo-civil-lei-10406-2002-compilada.md`, EM DIA no relatório de 23/09/2026 | 28/09/2026 |
| Parecer sobre a distribuição do MapNet - MT | `C:\COWORK\CODE\MAPNET-MT\docs\legal\parecer-distribuicao-2026-09-26.md` | Recebido em 26/09/2026 |

## 3. Premissas de incidência

| Premissa | O que aciona | Fundamento |
|---|---|---|
| Quem roda o programa trata dados pessoais | LGPD, para esse usuário | Nomes de pastas e arquivos podem identificar pessoas (art. 5º, I). Listar, mover e excluir esses arquivos são operações de tratamento (art. 5º, X, que inclui "acesso", "arquivamento", "armazenamento" e "eliminação") |
| No atendimento a clientes, a MT age em nome do cliente | LGPD, com a MT como operadora e o cliente como controlador | É o cliente que decide o que pode ser apagado ou movido. A MT executa (art. 5º, VI e VII) |
| Na simples distribuição, a MT não trata os dados de quem baixa | A MT fica fora dos deveres de agente de tratamento quanto a esses usos | O programa não envia nada à MT. Mesma premissa da análise do MapNet |
| Quem baixa de graça pode ser consumidor | CDC, se a distribuição com finalidade de divulgação for considerada remunerada de forma indireta | Ponto tratado no parecer do MapNet: há fundamento relevante para considerar a incidência, sem afirmação definitiva. Esta análise adota a premissa por cautela |
| O atendimento da MT a clientes é serviço remunerado | CDC com cliente pessoa física destinatário final; regime civil com empresa que contrata para a própria atividade | Art. 2º e art. 3º, § 2º, do CDC. O enquadramento depende de cada cliente e do contrato de serviço, fora do escopo do programa |

## 4. Análise

### 4.1 LGPD: o programa e quem o usa

**FATO LEGAL.** LGPD, art. 5º: "X - tratamento: toda operação realizada com dados pessoais, como as que se referem a coleta, produção, recepção, classificação, utilização, acesso, reprodução, transmissão, distribuição, processamento, arquivamento, armazenamento, eliminação, avaliação ou controle da informação, modificação, comunicação, transferência, difusão ou extração". Verificado na biblioteca local em 28/09/2026. Incidência: quem varre e age sobre arquivos que identificam pessoas realiza acesso, arquivamento e eliminação.

**FATO LEGAL.** LGPD, art. 4º: "Esta Lei não se aplica ao tratamento de dados pessoais: I - realizado por pessoa natural para fins exclusivamente particulares e não econômicos". Incidência: alcança quem usa o programa no próprio computador de casa. Não alcança empresa nem técnico que atende cliente.

**INTERPRETAÇÃO.** Na distribuição, a MT não é controladora nem operadora dos dados que os usuários veem ou apagam, pelo mesmo raciocínio da análise do MapNet: ela não decide sobre esse tratamento nem o realiza.

### 4.2 LGPD: a MT no atendimento a clientes

**FATO LEGAL.** LGPD, art. 5º, "VI - controlador: pessoa natural ou jurídica, de direito público ou privado, a quem competem as decisões referentes ao tratamento de dados pessoais; VII - operador: pessoa natural ou jurídica, de direito público ou privado, que realiza o tratamento de dados pessoais em nome do controlador". Art. 39: "O operador deverá realizar o tratamento segundo as instruções fornecidas pelo controlador, que verificará a observância das próprias instruções e das normas sobre a matéria." Art. 37: "O controlador e o operador devem manter registro das operações de tratamento de dados pessoais que realizarem, especialmente quando baseado no legítimo interesse." Verificados na biblioteca local em 28/09/2026. Incidência: a MT, ao apagar ou mover arquivos do cliente por decisão dele, realiza tratamento em nome do cliente.

**INTERPRETAÇÃO.** No atendimento, a MT é operadora. O que apagar ou mover é instrução do cliente, e a MT deve poder mostrar o que fez. O `acoes.log` do programa serve de registro das operações do art. 37 para essa parte do atendimento.

**FATO LEGAL.** LGPD, art. 46: "Os agentes de tratamento devem adotar medidas de segurança, técnicas e administrativas aptas a proteger os dados pessoais de acessos não autorizados e de situações acidentais ou ilícitas de destruição, perda, alteração, comunicação ou qualquer forma de tratamento inadequado ou ilícito." Verificado na biblioteca local em 28/09/2026. Incidência: vale para a MT como operadora no atendimento.

**INTERPRETAÇÃO.** As travas de produto já aprovadas (confirmação com lista e total, bloqueio de pastas de sistema, Lixeira em vez de exclusão direta no disco local, cópia conferida antes de apagar a origem no mover, registro gravado antes da ação) são medidas técnicas contra destruição acidental no sentido do art. 46. A medida administrativa que falta é fora do programa: a instrução do cliente registrada antes da exclusão.

### 4.3 O registro de ações

**INTERPRETAÇÃO.** O `acoes.log` guarda o nome de usuário do Windows de quem agiu e caminhos que podem conter nomes de pessoas. São dados pessoais (art. 5º, I) do técnico e de terceiros, guardados no computador onde o programa rodou. A finalidade (provar o que foi feito) é legítima e compatível com o art. 37. O registro não sai da máquina.

**RECOMENDAÇÃO.** Guardar só o necessário (data e hora, usuário do Windows, ação, origem, destino, tamanho, resultado), sem conteúdo de arquivo. Mostrar em Opções onde o registro fica. No atendimento da MT, anexar uma cópia do registro à ordem de serviço, para a MT ter o próprio registro do art. 37 sem depender do computador do cliente.

### 4.4 Apagar arquivo alheio e o art. 154-A do Código Penal

**FATO LEGAL.** Código Penal, art. 154-A, redação da Lei nº 14.155/2021: "Invadir dispositivo informático de uso alheio, conectado ou não à rede de computadores, com o fim de obter, adulterar ou destruir dados ou informações sem autorização expressa ou tácita do usuário do dispositivo ou de instalar vulnerabilidades para obter vantagem ilícita". § 1º, incluído pela Lei nº 12.737/2012: "Na mesma pena incorre quem produz, oferece, distribui, vende ou difunde dispositivo ou programa de computador com o intuito de permitir a prática da conduta definida no caput." Verificados no Planalto em 28/09/2026.

**INTERPRETAÇÃO.** O tipo exige invadir o dispositivo. O MapDisk não contorna permissão: lê e age só com as permissões que o Windows já dá à conta que o roda, e a elevação passa pela confirmação do próprio Windows. Não há, na distribuição, o intuito de permitir invasão exigido pelo § 1º. O parecer do MapNet já concluiu no mesmo sentido para um programa com risco maior, que abre conexões em equipamentos de terceiros.

**INTERPRETAÇÃO.** No atendimento, o técnico da MT entra no computador do cliente com acesso dado por ele. O que protege a MT não é o programa, e sim a autorização do cliente para aquela exclusão, pela mesma razão do item 4.2.

### 4.5 Dano por exclusão e informação sobre riscos

**FATO LEGAL.** Código Civil, art. 186: "Aquele que, por ação ou omissão voluntária, negligência ou imprudência, violar direito e causar dano a outrem, ainda que exclusivamente moral, comete ato ilícito." Art. 927, caput: "Aquele que, por ato ilícito (arts. 186 e 187), causar dano a outrem, fica obrigado a repará-lo." Verificados na biblioteca local em 28/09/2026.

**FATO LEGAL.** CDC, art. 6º, III, direito básico do consumidor à "informação adequada e clara sobre os diferentes produtos e serviços, com especificação correta de quantidade, características, composição, qualidade, tributos incidentes e preço, bem como sobre os riscos que apresentem". Verificado no Planalto em 28/09/2026. Incidência: sob a premissa de relação de consumo na distribuição (seção 3).

**INTERPRETAÇÃO.** O risco próprio do MapDisk é a perda de arquivo. A informação sobre esse risco precisa aparecer onde a decisão acontece, na confirmação da ação, e não só na licença. A diferença entre Lixeira (recuperável) e exclusão em rede (sem volta) precisa estar dita com clareza.

### 4.6 CDC e a ausência de garantia da GPL-3.0

**FATO LEGAL.** CDC, art. 3º, § 2º: "Serviço é qualquer atividade fornecida no mercado de consumo, mediante remuneração". Art. 25: "É vedada a estipulação contratual de cláusula que impossibilite, exonere ou atenue a obrigação de indenizar prevista nesta e nas seções anteriores." Art. 51, I: são nulas as cláusulas que "impossibilitem, exonerem ou atenuem a responsabilidade do fornecedor por vícios de qualquer natureza dos produtos e serviços ou impliquem renúncia ou disposição de direitos. Nas relações de consumo entre o fornecedor e o consumidor pessoa jurídica, a indenização poderá ser limitada, em situações justificáveis". Verificados no Planalto em 28/09/2026.

**INTERPRETAÇÃO.** O parecer do MapNet respondeu a esta questão para a mesma forma de distribuição: manter a GPL-3.0 íntegra e pôr, separado e em português, um aviso que descreva os limites do programa e ressalve os direitos garantidos por lei. A conclusão vale para o MapDisk, com o risco de perda de arquivo acrescentado ao aviso.

### 4.7 A marca TreeSize

**RECOMENDAÇÃO.** O `AGENTS.md` já proíbe o nome "TreeSize" em tela, código, página e arquivo publicado. Com essa regra, não há uso de marca alheia a analisar. Se um dia a página quiser fazer comparação com o TreeSize, a comparação passa antes por nova análise da `legal-br`, que não foi feita aqui.

## 5. Textos para o produto

Os textos seguem o modelo do parecer do MapNet, adaptados ao risco do MapDisk. Passam pela `humanizar-ptbr` antes de entrar no código, sem mudar o sentido.

**Página e README, perto do download:**

> Uso autorizado. O MapDisk - MT mostra o espaço ocupado em discos e pastas e permite enviar arquivos para a Lixeira, movê-los ou, em pastas de rede, excluí-los definitivamente. Use o programa só em computadores e pastas que você tem autorização para administrar. Antes de apagar ou mover, confira a lista de itens e o destino. Em pastas de rede não existe Lixeira: a exclusão não pode ser desfeita pelo programa. O programa não envia nenhuma informação para fora do computador.

> Licença e garantias. O MapDisk - MT é distribuído gratuitamente sob a GPL-3.0. Os tamanhos mostrados dependem do que o sistema de arquivos informa e das permissões da conta que roda o programa, e podem ficar incompletos quando alguma pasta não pode ser lida. A licença não inclui promessa de funcionamento em todo computador nem serviço de suporte técnico. Quem apaga ou move arquivos pelo programa decide o que tratar e deve ter cópia de segurança do que for importante. As disposições da GPL-3.0 sobre garantias e responsabilidade aplicam-se nos limites permitidos pela legislação brasileira e não restringem direitos assegurados ao consumidor por lei.

**Confirmação de envio para a Lixeira:**

> Enviar N itens (X GB) para a Lixeira de C:? Eles podem ser restaurados pela Lixeira enquanto ela não for esvaziada.

**Confirmação de exclusão em rede:**

> Excluir definitivamente N itens (X GB) de \\servidor\pasta? Pastas de rede não têm Lixeira. Depois de excluídos, os itens só voltam por uma cópia de segurança. Para confirmar, digite EXCLUIR.

**Confirmação de mover:**

> Mover N itens (X GB) para D:\Arquivo? A origem só é apagada depois de a cópia ser conferida.

## 6. Requisitos para implementação

- [ ] Textos "Uso autorizado" e "Licença e garantias" na página e no README, perto do download (fatia 7).
- [x] Confirmação de cada ação com a lista, o total e o destino, e o aviso de risco da seção 5 (fatia 5).
- [x] Exclusão em rede com a palavra EXCLUIR digitada e o aviso de que não há Lixeira (fatia 5). Vale também para unidade removível e mapeada, com o texto da seção 9.
- [x] Registro de ações só com data e hora, usuário do Windows, ação, origem, destino, tamanho e resultado. Sem conteúdo de arquivo (fatia 5).
- [x] Opções mostra onde fica o registro e abre a pasta dele (fatia 5). Por enquanto, pelo botão "Registro de ações" no cartão das pastas; vai para a tela de Opções na fatia 7.
- [ ] Nada sai da máquina: nenhum envio de rede no programa, conferido por teste (todas as fatias).
- [ ] Fora do programa, no atendimento da MT: autorização do cliente para as exclusões registrada na ordem de serviço, com cópia do `acoes.log` anexada. Fica em `docs/superpowers/pendencias.md` como decisão de processo do Manfred.

## 7. Pendências de validação

Nenhuma. Os pontos que dependiam de parecer (relação de consumo na distribuição gratuita e alcance das seções 15 e 16 da GPL-3.0) foram respondidos pelo parecer do MapNet, com fatos de distribuição iguais.

## 8. Janela Sobre (acréscimo de 30/09/2026)

**FATO LEGAL.** GPL-3.0, seção 0: "An interactive user interface displays "Appropriate Legal Notices" to the extent that it includes a convenient and prominently visible feature that (1) displays an appropriate copyright notice, and (2) tells the user that there is no warranty for the work (except to the extent that warranties are provided), that licensees may convey the work under this License, and how to view a copy of this License." Seção 5, d: "If the work has interactive user interfaces, each must display Appropriate Legal Notices". Verificado no arquivo `LICENSE` do repositório, que é o texto oficial da GPL-3.0, em 30/09/2026.

**Incidência.** A seção 5 regula quem distribui versão modificada. A MANFRED TECNOLOGIA LTDA, titular do código, não está obrigada pela própria licença. O aviso é **RECOMENDAÇÃO**: o próprio texto da GPL, em "How to Apply These Terms", sugere uma caixa "Sobre" para programa com janela. Com o aviso na janela, quem distribuir versão modificada já recebe a tela pronta.

**RECOMENDAÇÃO aplicada.** A janela Sobre mostra o aviso de copyright, o aviso de software livre (tradução do aviso sugerido pela GPL, só com "versão 3", sem a cláusula "ou posterior", que não foi decidida) e o texto "Licença e garantias" da seção 5 sem mudança. O texto integral da licença vai embutido no programa e abre pelo botão "Ver a licença", sem internet. O texto "Licença e garantias" já cita apagar e mover, que chegam na fatia 5. Fica sem mudança porque nenhuma versão é publicada antes da fatia 7, e a versão publicada terá essas ações.

## 9. Exclusão em unidade sem Lixeira garantida (acréscimo de 30/09/2026)

**Contexto.** A fatia 5 limita a Lixeira à unidade fixa local, decisão aprovada pelo Manfred no PR #9. Em unidade removível ou mapeada, a ação vira exclusão definitiva, como na rede, e a confirmação precisa de uma variante do texto aprovado da seção 5.

**INTERPRETAÇÃO.** A variante cumpre a mesma função da seção 4.5: diz o risco na hora da decisão e mantém a frase de que os itens só voltam por cópia de segurança. A redação "Esta unidade não tem Lixeira" seria inexata: algumas unidades removíveis formatadas em NTFS têm Lixeira, e quem decide não usá-la é o programa. Informação sobre risco precisa ser correta (CDC, art. 6º, III, já verificado na seção 4.5).

**RECOMENDAÇÃO aplicada.** Texto da confirmação em unidade removível ou mapeada:

> Excluir definitivamente N itens (X GB) de E:\fotos? Nesta unidade, o MapDisk não usa a Lixeira. Depois de excluídos, os itens só voltam por uma cópia de segurança. Para confirmar, digite EXCLUIR.

O texto de rede da seção 5 fica como está. Não há pendência de validação.

