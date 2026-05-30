# GX Limpeza - FiveM / GTA V

![Windows](https://img.shields.io/badge/Windows-10%2F11-white?style=for-the-badge&logo=windows&logoColor=black&labelColor=111111)
![CSharp](https://img.shields.io/badge/C%23-WinForms-white?style=for-the-badge&logo=csharp&logoColor=black&labelColor=111111)
![Status](https://img.shields.io/badge/status-em%20desenvolvimento-white?style=for-the-badge&labelColor=111111)

Ferramenta visual para detectar automaticamente o FiveM e o GTA V no Windows e limpar arquivos comuns de mods, plugins, ReShade, ENB e OpenIV.

O objetivo do projeto e ser simples para quem usa e claro para quem quer estudar ou compilar o codigo.

## Preview

Interface em tema preto e branco, com deteccao de diretorios, modo simulacao e registro visual da limpeza.

> O executavel nao acompanha o repositorio. Ele deve ser gerado localmente pelo script de build.

## Recursos

- Detecta automaticamente a pasta `FiveM.app`.
- Detecta o GTA V usando configuracoes do FiveM e caminhos comuns.
- Permite selecionar manualmente a pasta do FiveM.
- Possui modo `Simular`, para ver o que seria apagado antes de limpar.
- Limpa mods e plugins do FiveM.
- Remove arquivos comuns de mods do GTA V, como ENB, ReShade, OpenIV e ASI loaders.
- Mantem um registro visual da operacao dentro do painel.

## O Que Ele Limpa

### FiveM

- `mods`
- `plugins`
- `reshade-shaders`
- `ReShade`
- `ShaderCache`
- `CitizenFX\cache\priv`
- `citizen`

### GTA V

Arquivos e pastas comuns de modificacoes, incluindo:

- `mods`
- `plugins`
- `scripts`
- `reshade-shaders`
- `dinput8.dll`
- `ScriptHookV.dll`
- `OpenIV.asi`
- `ReShade.ini`
- `dxgi.dll`
- `d3d11.dll`
- `enbseries.ini`
- `enbseries`
- `enbfeeder.asi`
- `enbseries.h`
- `enbhelper.dll`
- `_weatherlist.ini`
- `enbadaptation.fx.ini`
- `enbbloom.fx.ini`
- `enbeffect.fx.ini`
- `enbeffectpostpass.fx.ini`
- `enbeffectprepass.fx.ini`
- `enblens.fx.ini`
- `enblightsprite.fx.ini`
- `intlightsprite.fx.ini`

## Estrutura

```text
.
├── assets/
│   └── GXCleaner.ico
├── dist/
│   └── .gitkeep
├── src/
│   └── GXCleanerGui.cs
├── build.ps1
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── SECURITY.md
└── LICENSE
```

## Como Compilar

Abra o PowerShell na pasta do projeto e execute:

```powershell
.\build.ps1
```

O executavel sera gerado em:

```text
dist\GX Limpeza.exe
```

## Requisitos

- Windows 10 ou Windows 11.
- .NET Framework com `csc.exe` disponivel no sistema.
- PowerShell.

## Observacao de Seguranca

Este projeto remove arquivos e pastas de mods. Use primeiro o modo `Simular` se quiser revisar o que sera apagado.

Builds locais ou executaveis baixados da internet podem ser marcados pelo Windows SmartScreen ou por antivirus, principalmente quando nao possuem assinatura digital. Para distribuicao publica, o ideal e assinar o `.exe` com um certificado de Code Signing.

## Aviso

Este projeto nao e afiliado a Rockstar Games, Cfx.re, FiveM, Steam, Epic Games ou qualquer distribuidora oficial. Use por sua conta e risco.
