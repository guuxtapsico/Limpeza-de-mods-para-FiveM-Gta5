# GX Limpeza - FiveM / GTA

Ferramenta Windows para detectar automaticamente a pasta `FiveM.app`, localizar o GTA V pelo `CitizenFX.ini` e limpar mods/arquivos comuns de FiveM, GTA V, ReShade e ENB.

## Recursos

- Detecta FiveM em caminhos comuns do Windows.
- Permite escolher manualmente a pasta `FiveM.app`.
- Detecta GTA V pelo `CitizenFX.ini`.
- Modo simulacao para ver o que seria removido.
- Limpeza selecionavel de FiveM e GTA V.
- Log visual da operacao.

## Build

Abra o PowerShell na pasta do projeto e rode:

```powershell
.\build.ps1
```

O executavel sera gerado em:

```text
dist\GX Limpeza.exe
```

## Observacao de Seguranca

Este projeto remove arquivos e pastas de mods. Antivirus podem marcar builds locais como suspeitas por heuristica, principalmente se o `.exe` nao estiver assinado digitalmente.

Para distribuicao publica, o ideal e assinar o executavel com um certificado de Code Signing.
