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

Linha de comando (só lê, nunca apaga nem move). O alvo é um caminho completo:

    mapdisk varrer C:
    mapdisk varrer \\servidor\dados --csv dados.csv --top 30

No Prompt de Comando, use `start /wait mapdisk varrer C:` para o prompt esperar o fim. No PowerShell, termine a linha com `| Out-Host`.

## Situação do projeto

| Fatia | Conteúdo | Ramo | Pull Request | Situação |
|---|---|---|---|---|
| 1 | Varredura, árvore com colunas, modos e unidades, barra de status, linha de comando com CSV | `varredura` | [#2](https://github.com/manfredjr/mapdisk/pull/2) | Concluída em 29/09/2026 |
| 2 | Navegação, últimos alvos, atualizar uma pasta, menu de contexto, varredura como administrador com o privilégio de backup, acertos da fatia 1 | `navegacao` | [#4](https://github.com/manfredjr/mapdisk/pull/4) | Aguardando o teste do Manfred |
