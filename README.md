# MapDisk - MT

Analisador de espaço em disco para Windows, da MT - Manfred Tecnologia. Mostra quais pastas e arquivos ocupam mais espaço numa unidade, numa pasta ou num compartilhamento de rede. Nas próximas versões, também envia para a Lixeira e move, com confirmação e registro.

Software livre, sob licença [GPL-3.0](LICENSE).

As regras do projeto estão no [`AGENTS.md`](AGENTS.md) e o desenho em [`docs/superpowers/specs/2026-09-28-mapdisk-design.md`](docs/superpowers/specs/2026-09-28-mapdisk-design.md).

## Uso

Baixe o `mapdisk.exe` e abra. Não precisa instalar nem ser administrador. Escolha a unidade, digite uma pasta ou um caminho de rede (`\\servidor\pasta`) e clique em **Varrer**.

Pastas que o Windows não deixa ler aparecem como **sem acesso**, e a barra de baixo diz quantas são. O total não inclui o que está nelas.

Para ler também essas pastas, clique em **Varrer como administrador**. O Windows pede confirmação e abre outra janela, que já começa varrendo e lê todas as pastas locais. Só leitura: nada é apagado nem movido.

Na árvore:

- **Abrir aqui** (botão direito) mostra uma pasta como raiz, sem varrer de novo. **Voltar**, **Avançar** e **Subir** navegam entre as raízes, e **Abrir até** abre a árvore até 5 níveis.
- **Atualizar esta pasta** (botão direito ou Shift+F5) lê de novo só a pasta escolhida.
- O botão direito também tem **Mostrar no Explorer**, **Copiar caminho** e **Propriedades**.
- A lista de alvos mostra as unidades e os últimos 10 alvos usados. A lista fica só neste computador.

À direita da árvore, o painel **Análises** responde "o que eu trato?" sobre a pasta selecionada:

- **Gráfico:** o que pesa dentro da pasta, em blocos (área proporcional ao tamanho) ou em pizza (as 10 maiores, com legenda). Segue o "Mostrar" da árvore. Clique duplo num bloco ou numa fatia abre a pasta, e Voltar retorna. Pasta sem leitura fica fora do desenho e aparece pelo nome embaixo dele.
- **Maiores arquivos:** os 100 maiores.
- **Arquivos antigos:** sem alteração há mais de 6 meses, 1, 2 ou 5 anos, com a quantidade e o total.
- **Por tipo:** vídeo, imagem, áudio, e-mail (.pst, .ost), imagem de disco, compactado e backup, instalador, documento e outros, mais o ranking por extensão.
- **Por usuário:** o ranking das pastas de perfil em `Users`.

O botão direito numa linha tem **Mostrar no Explorer**, **Copiar caminho** e **Abrir a pasta na árvore**. As análises usam o que já foi lido, sem varrer de novo, e avisam quando há pastas sem leitura fora da conta.

O botão **Sobre** mostra a versão, a autoria, a licença GPL-3.0 com o texto completo e onde baixar. O logo da MT abre o site www.manfred.com.br.

Linha de comando (só lê, nunca apaga nem move). O alvo é um caminho completo:

    mapdisk varrer C:
    mapdisk varrer \\servidor\dados --csv dados.csv --top 30

No Prompt de Comando, use `start /wait mapdisk varrer C:` para o prompt esperar o fim. No PowerShell, termine a linha com `| Out-Host`.

## Situação do projeto

| Fatia | Conteúdo | Ramo | Pull Request | Situação |
|---|---|---|---|---|
| 1 | Varredura, árvore com colunas, modos e unidades, barra de status, linha de comando com CSV | `varredura` | [#2](https://github.com/manfredjr/mapdisk/pull/2) | Concluída em 29/09/2026 |
| 2 | Navegação, últimos alvos, atualizar uma pasta, menu de contexto, varredura como administrador com o privilégio de backup, acertos da fatia 1 | `navegacao` | [#4](https://github.com/manfredjr/mapdisk/pull/4) | Concluída em 29/09/2026 |
| 3 | Painel de análises: maiores arquivos, arquivos antigos, por tipo, por usuário; memória; padrão visual do MapNet com o logo da MT; janela Sobre | `analises` | Plano em [#5](https://github.com/manfredjr/mapdisk/pull/5), código em [#6](https://github.com/manfredjr/mapdisk/pull/6) | Concluída em 30/09/2026 |
| 4 | Gráficos da pasta selecionada: blocos e pizza | `graficos` | Plano em [#7](https://github.com/manfredjr/mapdisk/pull/7), código em [#8](https://github.com/manfredjr/mapdisk/pull/8) | Aguardando o teste do Manfred |
