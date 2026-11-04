# Drowned

City-sim subaquático em **Unity 2D**. O jogador administra uma pequena comunidade no fundo do mar, equilibra energia, oxigênio e alimento, faz a população crescer e restaura o **Monumento Central** em 4 etapas. Perde se energia ou oxigênio ficarem zerados por dias consecutivos.

Projeto da disciplina de Computação Gráfica (UNIFAL, 4º período). O GDD está em [`docs/GDD-Drowned.pdf`](docs/GDD-Drowned.pdf) e o quadro de tarefas em <https://drowned-cg.atlassian.net>.

## Versões fixadas (não mude sem combinar com o grupo)

| Ferramenta | Versão |
|---|---|
| Unity | **6000.6.7f1** (Unity 6) com template **Universal 2D** |
| Blender | **5.2 LTS** (qualquer 4.2+ roda os scripts) |
| Git LFS | 3.x |

O Unity grava a versão em `ProjectSettings/ProjectVersion.txt`. Quem abrir o projeto com outra versão converte os arquivos e quebra para os demais. Instale exatamente a versão que estiver nesse arquivo.

## Primeiro clone

```bash
git lfs install
git clone https://github.com/developer6v/Drowned.git
cd Drowned
git lfs pull
```

Abra a pasta `Drowned` no Unity Hub (a raiz do repositório é a raiz do projeto Unity: `Assets/`, `Packages/`, `ProjectSettings/`).

### Configuração obrigatória do editor (quem cria o projeto faz uma vez)

Em **Edit → Project Settings → Editor**:

- **Asset Serialization → Mode: Force Text**
- **Version Control → Mode: Visible Meta Files**

Sem isso cenas e prefabs viram binário e o Git não consegue fazer diff nem merge.

## Estrutura

```
Assets/
  Scripts/        C# por sistema: Core, World, Resources, Population, Research, Monument, UI, Save
  Tests/EditMode/ Testes do Unity Test Framework (lógica pura, sem cena)
  Tests/PlayMode/ Smoke test da demo (entra em Play, simula dias, constrói)
  Sprites/        Spritesheets renderizados do Blender (+ Placeholders/)
  Prefabs/  Scenes/  Audio/  UI/  Data/ (ScriptableObjects)
docs/             GDD e documentação de entrega
tools/blender/    Projeto 3D auxiliar: scripts Python que renderizam modelos em sprites isométricos
```

## Pipeline 3D → sprite

Os modelos são feitos no Blender e renderizados em 4 direções com câmera ortográfica isométrica. Nada de 3D roda no jogo. Veja [`tools/blender/README.md`](tools/blender/README.md). Teste rápido:

```bash
blender -b --python tools/blender/render_sprites.py -- --test-cube
```

## Rodando o jogo

Duas cenas em `Assets/Scenes`, cada uma com um único objeto que monta o resto por código ao dar Play:

- `MainMenu.unity`: menu principal (Jogar, Continuar, Opções, Sair).
- `Game.unity`: a partida. O objeto `DemoBootstrap` cria grid, relógio, GameManager com o `GameRules.asset` da planilha, estruturas iniciais, Monumento, modo construção, áudio e HUD.

Abra qualquer uma e dê Play. No editor, "Jogar" carrega `Game.unity` pelo caminho mesmo sem Build Settings. Para a build Windows, adicione as duas nas Build Settings (MainMenu primeiro). Se precisar recriar as cenas: **Drowned → Criar cenas (MainMenu + Game)**.

Controles: WASD/setas ou arrastar com o botão direito movem a câmera, roda do mouse dá zoom, Espaço pausa, 1/2/3 mudam a velocidade, Esc abre o menu de pausa, F5 salva. Clique em uma estrutura da barra inferior e depois no mapa para construir; clique em uma estrutura construída para melhorar ou remover.

Se `Assets/Data/Resources/GameRules.asset` não existir, o `DemoBootstrap` cai nos números em memória (`DemoContent`) e avisa no Console; rode **Drowned → Gerar dados da planilha** para gerá-lo a partir de `docs/balanceamento`.

## Fluxo de trabalho

1. Toda tarefa vem do Jira (chave `SCRUM-nnn`). Leia a descrição de aceite antes de começar.
2. Crie uma branch a partir de `main` com o nome da chave: `SCRUM-124-gameclock`.
3. Abra um Pull Request. **Ninguém commita direto em `main`.**
4. Outra pessoa revisa, testa no editor e faz o merge. Depois comenta na issue onde está o resultado (branch, prefab, aba da planilha) e move para Concluído.

### Regras para evitar conflito de cena

- Cada pessoa trabalha em cenas de teste separadas (`Assets/Scenes/Sandbox_<nome>.unity`). A cena principal `Main.unity` só recebe mudanças combinadas.
- Tudo que vai para a cena principal vira **prefab** primeiro.
- A IA (Claude Code) nunca edita `.unity` nem `.prefab`. Ela escreve C#, testes, scripts de Blender, ícones, sons e docs; ligar os scripts nos objetos é trabalho de quem está no editor.

## Rodando os testes sem abrir o editor

```bash
"<caminho do Unity>/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -logFile -
"<caminho do Unity>/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults PlayMode.xml -logFile -
```

## Equipe

Diogo Salgado, Luis Filipi, Otávio de Oliveira. IA: Claude Code, com as convenções em [`CLAUDE.md`](CLAUDE.md).
