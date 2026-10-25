# GX Limpeza - FiveM / GTA V

![Windows](https://img.shields.io/badge/Windows-10%2F11-F57F00?style=for-the-badge&logo=windows&logoColor=white&labelColor=111111)
![CSharp](https://img.shields.io/badge/C%23-WinForms-F57F00?style=for-the-badge&logo=csharp&logoColor=white&labelColor=111111)
![Status](https://img.shields.io/badge/status-em%20desenvolvimento-F57F00?style=for-the-badge&labelColor=111111)

Tira mods, ReShade, ENB e scripts do FiveM e do GTA V num lugar só. Você vê a lista do que vai sair, desmarca o que quer manter e limpa.

![GX Limpeza](docs/preview.png)

## Como usar

1. Abra o `GX Limpeza.exe`. Ele acha o FiveM e o GTA V sozinho. Se não achar, use **Trocar pasta**.
2. Escolha o que limpar: **FiveM**, **GTA V** ou os dois.
3. Clique em **Ver o que vai sair**. Isso só monta a lista, não apaga nada.
4. Desmarque na lista o que quiser manter. A caixinha do grupo marca ou desmarca tudo do jogo.
5. Clique em **Limpar agora** e confirme. Cada item mostra ✓ se saiu ou ⚠ se deu erro.

## O que ele limpa

### FiveM (pasta `FiveM.app`)

- Conteúdo das pastas `mods` e `plugins`
- `reshade-shaders`, `ReShade` e `ShaderCache`
- `CitizenFX\cache\priv`
- `citizen` (o FiveM baixa de novo quando abrir)

### GTA V (pasta do jogo)

Pastas `mods`, `scripts`, `plugins`, `reshade-shaders`, `enbseries` e arquivos de ReShade, ENB, ScriptHook, OpenIV e menus `.asi`.

<details>
<summary>Ver a lista completa do GTA V</summary>

- Pastas: `mods`, `scripts`, `plugins`, `reshade-shaders`, `enbseries`, `shaderinput`, `menyooStuff`, `QuantV`, `NVE`, `NaturalVision Evolved`
- DLL e ASI: `d3d11.dll`, `d3d12.dll`, `dxgi.dll`, `dinput8.dll`, `ScriptHookV.dll`, `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`, `ScriptHookVDotNet3.dll`, `NativeTrainer.asi`, `Menyoo.asi`, `OpenIV.asi`, `openCameraV.asi`, `HeapAdjuster.asi`, `PackfileLimitAdjuster.asi`, `enbfeeder.asi`
- Configurações: `Gameconfig.xml`, `ReShade.ini`, `ReShadePreset.ini`, `enblocal.ini`, `enbseries.ini`, `enbfeeder.ini`, `enbseries.h`
- Efeitos ENB: `enbadaptation.fx`, `enbbloom.fx`, `enbeffect.fx`, `enbeffectpostpass.fx`, `enbeffectprepass.fx`, `enblens.fx`, `enblightsprite.fx`
- Logs: `ReShade.log`, `OpenIV.log`, `openCameraV.log`

</details>

## Segurança

- Nada é apagado sem você confirmar. **Ver o que vai sair** só mostra a lista.
- Só o que estiver marcado na lista é apagado.
- O GTA V só é limpo se a pasta tiver o `GTA5.exe`.
- Avisa se o FiveM ou o GTA V estiver aberto antes de limpar.
- Seus saves e sua conta não são tocados.

## Como compilar

Abra o PowerShell na pasta do projeto e rode:

```powershell
.\build.ps1
```

O executável fica em `dist\GX Limpeza.exe`. Ele não vem no repositório; cada push na `main` também gera o `.exe` nos artefatos do GitHub Actions.

Requisitos: Windows 10 ou 11, .NET Framework 4 (já vem no Windows, com o `csc.exe`) e PowerShell.

## Estrutura

```text
.
├── assets/
│   ├── GXCleaner.ico      ícone do programa
│   ├── gx-logo.png        logo GX do topo
│   ├── fivem.png          imagem do cartão do FiveM
│   └── gtav.jpg           imagem do cartão do GTA V
├── docs/
│   └── preview.png        print usado neste README
├── dist/
│   └── .gitkeep
├── src/
│   └── GXCleanerGui.cs    o programa inteiro
├── build.ps1
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── SECURITY.md
└── LICENSE
```

## Observação sobre o Windows

Executáveis sem assinatura digital podem ser marcados pelo Windows SmartScreen ou por antivírus. Para distribuir publicamente, o ideal é assinar o `.exe` com um certificado de Code Signing.

## Aviso

Este projeto não é afiliado à Rockstar Games, Cfx.re, FiveM, Steam, Epic Games ou qualquer distribuidora oficial. FiveM e Grand Theft Auto V são marcas dos seus donos; as imagens dos jogos são usadas só para identificá-los na interface. Use por sua conta e risco.
