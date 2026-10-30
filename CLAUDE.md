# CLAUDE.md — convenções do projeto Drowned

Jogo city-sim subaquático em Unity 2D. GDD em `docs/GDD-Drowned.pdf`. Quadro: Jira `SCRUM`, site drowned-cg.atlassian.net, board 1.

## Papel da IA neste repositório

A IA escreve e prepara; a pessoa abre no editor e decide.

- **Escreve:** C# em `Assets/Scripts`, testes EditMode em `Assets/Tests/EditMode`, scripts Python de Blender em `tools/blender`, ícones, sons, planilha de balanceamento, documentação, descrições de aceite no Jira.
- **Nunca edita:** `*.unity`, `*.prefab`, `*.asset` de cena, `ProjectSettings/`, `Packages/manifest.json` (a não ser que a tarefa seja exatamente essa e a pessoa tenha pedido).
- **Nunca commita em `main`.** Branch por tarefa com o nome da chave Jira (`SCRUM-124-gameclock`), PR com descrição curta apontando a issue.
- Antes de começar uma tarefa, a issue precisa ter **descrição de aceite** no Jira. Se não tiver, a IA escreve primeiro e só depois codifica.
- Ao abrir o PR, comenta na issue do Jira com o link.

## Versões

Unity **6000.0 LTS** (2D URP). Blender **5.2 LTS**. Conferir `ProjectSettings/ProjectVersion.txt` antes de sugerir API.

## Organização do código

```
Assets/Scripts/
  Core/        GameClock, GameSpeed, GameManager, eventos globais
  World/       Grid/Tilemap, colocação de estruturas, câmera 2D
  Resources/   ResourceManager, StructureData (ScriptableObject), custos
  Population/  PopulationManager, SpecialistManager
  Research/    ResearchManager, TechnologyData
  Monument/    MonumentManager, etapas
  UI/          HUD, painéis, menus, alertas
  Save/        SaveSystem (JSON)
```

Regras:

- **Lógica pura separada do MonoBehaviour.** Cada sistema tem uma classe C# simples testável (ex.: `GameClock`) e um componente fino (ex.: `GameClockBehaviour`) que só faz a ponte com `Update()` e a cena. Testes cobrem a classe pura.
- Assembly definitions: `Drowned.Runtime` (em `Assets/Scripts`) e `Drowned.Tests.EditMode` (em `Assets/Tests/EditMode`). Não criar outros sem combinar.
- Namespace raiz `Drowned`, um sub-namespace por pasta (`Drowned.Core`, `Drowned.World`...).
- Nomes: classes e métodos em PascalCase, campos privados `_camelCase`, campos serializados `[SerializeField] private tipo nome`. Constantes `PascalCase`.
- Comentários e strings de UI em **português**. Identificadores em inglês.
- Números de balanceamento (custos, produção, consumo, thresholds, escala de tempo) **nunca ficam hardcoded**: vão para ScriptableObjects em `Assets/Data` ou para a classe de configuração do sistema, alimentada pela planilha.
- Eventos entre sistemas via `event Action<...>` em C#; nada de `FindObjectOfType` em `Update`.
- Sem pacotes de terceiros sem combinar (nada de DOTween, Odin etc.).

## Testes

Unity Test Framework, EditMode, NUnit. Um arquivo de teste por classe pura: `GameClockTests.cs`. Rodar em batch:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -logFile -
```

Se o Unity não estiver instalado na máquina, dizer isso no PR em vez de afirmar que os testes passaram.

## Blender

Tudo em `tools/blender`, rodando sem interface: `blender -b --python tools/blender/render_sprites.py -- <args>`. Câmera ortográfica isométrica, **4 direções** (N, E, S, W), fundo transparente, PNG por direção + spritesheet horizontal. Sprites vão para `Assets/Sprites/<Categoria>/<Nome>/`.

## Decisões já tomadas (atualizar quando mudar)

- 2026-10-30: 4 direções para estruturas e habitante (SCRUM-115, a confirmar com o grupo).
- 2026-10-30: raiz do repositório é a raiz do projeto Unity.
- 2026-10-30: planilha de balanceamento é a fonte única dos números; ScriptableObjects são gerados a partir dela.
