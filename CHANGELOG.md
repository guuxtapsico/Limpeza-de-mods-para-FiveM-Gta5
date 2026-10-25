# Changelog

Todas as mudancas relevantes deste projeto ficam registradas aqui.

## 0.2.0 - 2026-10-25

- Interface nova: tema escuro com o laranja do FiveM, logo GX e passos numerados.
- Cartões de FiveM e GTA V com a imagem de cada jogo e o que cada um apaga.
- "Simular" virou "Ver o que vai sair", com uma lista de caixinhas para desmarcar o que deve ficar.
- "Limpar agora" apaga só o que está marcado e mostra ✓ ou ⚠ em cada item.
- Busca das pastas e limpeza rodam em segundo plano, sem travar a janela.
- Opção de escolher a pasta do GTA V na mão e busca do GTA V também pelo registro da Rockstar.
- O GTA V só é limpo se a pasta tiver o `GTA5.exe`.
- A confirmação mostra quantos itens vão sair e avisa se o FiveM ou o GTA V estiver aberto.
- `build.ps1` embute as imagens, aceita acentos no código e para com erro se a compilação falhar.

## 0.1.0 - 2026-05-30

- Criado painel visual para limpeza de mods do FiveM e GTA V.
- Adicionada deteccao automatica da pasta `FiveM.app`.
- Adicionada deteccao do GTA V por configuracoes do FiveM e caminhos comuns.
- Adicionado modo de simulacao.
- Adicionada limpeza de itens comuns de ENB, ReShade, OpenIV e ASI.
- Atualizada limpeza do FiveM para remover `citizen` em vez de limpar `data\cache`.
- Adicionados `enbfeeder.asi` e `enbseries.h` na limpeza do GTA V.
