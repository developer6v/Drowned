# Drowned — documento de entrega

> **Rascunho** (SCRUM-196). Trechos marcados com **[preencher]** dependem do grupo. Números de teste e prints
> devem ser atualizados na última rodada antes da entrega.

**Disciplina:** Computação Gráfica — UNIFAL, 4º período
**Grupo:** [preencher nomes e RAs]
**Repositório:** https://github.com/developer6v/Drowned
**Quadro:** Jira, projeto SCRUM (drowned-cg.atlassian.net)

## 1. O jogo

Drowned é um city-builder subaquático em tempo real. O jogador administra uma pequena colônia no fundo do mar:
constrói e melhora estruturas (moradia, fazenda de algas, gerador, planta de oxigênio e laboratório), equilibra a
produção e o consumo de energia, oxigênio e alimento, faz a população crescer, desbloqueia especialistas, pesquisa
tecnologias e restaura o Monumento Central em quatro etapas.

- **Vitória:** concluir as quatro etapas do Monumento (Fundação, Estrutura, Sistemas, Acabamento).
- **Derrota:** energia ou oxigênio zerados por 3 dias seguidos de simulação.
- **Tempo:** 60 segundos reais por dia simulado a 1x, com pausa, 2x e 3x.

O desenho completo está no GDD (`docs/GDD-Drowned.pdf`).

## 2. Relação com Computação Gráfica

A proposta do GDD é rodar o jogo em 2D com sprites pré-renderizados de modelos 3D. Isso põe no centro do projeto
vários tópicos da disciplina.

### 2.1 Pipeline 3D → sprite 2D

Todo o pipeline é código Python rodando o Blender sem interface (`tools/blender`).

1. **Modelagem.** Modelos low-poly. Estruturas, habitante, Monumento e decoração vêm de kits CC0 da Kenney,
   combinados peça a peça por um manifesto (`tools/blender/manifest.json`). O chão e os placeholders são modelados
   por código a partir de primitivas.
2. **Câmera.** Ortográfica, com elevação de 30° e azimute de 45°. Isso dá a projeção isométrica 2:1 clássica:
   um quadrado do chão vira um losango duas vezes mais largo que alto.
3. **Iluminação.** Luz principal do tipo sol e uma luz de preenchimento azulada, para sugerir a água.
   Render no EEVEE com fundo transparente e sem curva de tom, para as cores dos materiais saírem fiéis.
4. **Direções.** O modelo gira 90° em torno de Z e é renderizado nas 4 direções (S, W, N, E).
5. **Spritesheet.** Os frames viram uma folha (linhas = direções, colunas = frames) e um `.json` com o layout.
6. **Escala e pivô.** O tamanho em pixels por unidade é calculado como `tamanho do frame / escala ortográfica × √2`,
   para que uma célula do grid tenha exatamente 1 unidade de largura no Unity. O pivô fica no ponto do chão
   abaixo do modelo, então todo sprite "pisa" no centro da sua célula.
7. **Importação.** Um `AssetPostprocessor` no Unity lê o `.json` e fatia a folha sozinho.

[preencher: print do Blender ou da folha de contato]

### 2.2 Renderização 2D no Unity

- **Grid isométrico.** Tilemap isométrico com células de 1 × 0,5. A conversão grid ↔ mundo é uma transformação
  linear: `x_tela = (x − y) / 2`, `y_tela = (x + y) / 4`.
- **Ordem de desenho.** Em isométrico, quem está mais à frente precisa ser desenhado por último (algoritmo do
  pintor). A ordem de cada sprite é `1000 − (x + y)`, acima do chão. Um bug real do projeto ilustra isso: com a
  ordem negativa, as estruturas eram desenhadas atrás do chão e o mapa parecia vazio. Ele foi achado por um
  teste que tira print da cena.
- **Parallax.** O fundo tem camadas que andam com a câmera em frações diferentes (0,6 e 0,85). Faixas repetidas
  em blocos dão a volta sem emenda (`ParallaxMath`).
- **Texturas procedurais.** Gradiente de profundidade, feixes de luz, silhuetas por soma de senos com
  frequências inteiras (para a textura repetir sem costura), neve marinha, bolhas, cantos arredondados 9-slice
  da interface e o domo de proteção, todos gerados pixel a pixel em código.
- **Domo de proteção.** Definido por uma função de distância normalizada: elipse achatada na base e meia elipse
  em cima. A opacidade cresce perto da borda, imitando vidro visto de lado (`DomeShape`).
- **Transparência.** Mistura alfa no vidro, nas bolhas, nos feixes de luz e no "fantasma" do modo construção.
- **Animação por transformação.** A montagem das estruturas usa uma curva ease-out-back na escala; o idle usa
  respiração senoidal com fase por instância; o gerador pulsa de brilho pela cor do sprite.

### 2.3 Interface

Interface flat em azul com destaques âmbar, montada inteira por código (`UiKit`, `UiTheme`): cantos arredondados
gerados por código, borda fina, sombra no texto e ícones gerados em Python com Pillow.

## 3. Arquitetura

- **Lógica pura separada do MonoBehaviour.** Cada sistema é uma classe C# comum e testável (`GameClock`,
  `ResourceManager`, `PopulationManager`, `StructureManager`, `SpecialistManager`, `ResearchManager`,
  `MonumentManager`, `CollapseMonitor`, `SaveSystem`) e um componente fino que só liga com a cena.
- **Eventos** entre sistemas por `event Action<...>`.
- **Dados.** Todo número de balanceamento sai da planilha (`docs/balanceamento/*.csv`). Um gerador no editor
  transforma os CSVs em ScriptableObjects em `Assets/Data`.
- **Cenas.** `MainMenu` e `Game` têm um objeto cada; o resto é montado por código (`DemoBootstrap`).

[preencher: diagrama de sistemas, se o professor pedir]

## 4. Ferramentas

| Ferramenta | Versão | Uso |
|---|---|---|
| Unity | 6000.6.7f1, Universal 2D | jogo |
| Blender | 5.2 LTS | render dos sprites, sem interface |
| Python | 3.12 + Pillow, numpy | ícones, sons, prints |
| Git + Git LFS / GitHub | | código e binários |
| Jira | | quadro, sprints, critérios de aceite |

## 5. Organização do trabalho

Seis sprints semanais, de 30/10 a 05/12/2026, com objetivos registrados no Jira. Cada tarefa tem descrição de
aceite antes de começar, uma branch com o nome da issue e um pull request revisado.

| Sprint | Objetivo |
|---|---|
| 1 | Base técnica: projeto, repositório, pipeline do Blender, planilha |
| 2 | Cidade mínima jogável |
| 3 | Especialistas, pesquisa e sprites finais |
| 4 | Monumento Central, alertas, cenário e som ambiente |
| 5 | Vitória, derrota, save, menus, ícones e sons |
| 6 | Playtests, correções, build e entrega |

[preencher: print do quadro do Jira (SCRUM-195) e divisão de tarefas por integrante]

## 6. Testes

- **EditMode (NUnit):** um arquivo por classe pura. [atualizar: total na última rodada]
- **PlayMode:** smoke test que monta a cena, simula 5 dias e constrói; teste que abre `MainMenu` e entra no jogo.
- **Prints automáticos:** teste PlayMode explícito que renderiza a HUD em 1920×1080 (`HudScreenshotTests`).
- **Playtests** (SCRUM-188/189): [preencher: duração da partida, problemas encontrados]

## 7. Prints

- HUD antes e depois do acabamento da UI: `docs/screenshots/SCRUM-192/`
- [preencher: print final com modelos da Kenney, chão, domo, decoração e habitantes]

## 8. Créditos e licenças

- Modelos 3D: Space Kit e Nature Kit, Kenney (kenney.nl), CC0. Licenças em `tools/blender/modelos/kenney/`.
- Efeitos sonoros: Interface Sounds, Impact Sounds e Music Jingles, Kenney, CC0. Licenças em `tools/audio/kenney/`.
- Ícones, fundo, chão, domo, ambiente sonoro, alarme e interface: gerados por código neste repositório.
- Parte do código e da documentação foi escrita com auxílio de IA (Claude), revisada e integrada pelo grupo.
  [confirmar com o grupo como declarar isso]

## 9. Limitações conhecidas

- Acessórios dos especialistas (SCRUM-152) não foram feitos; os especialistas aparecem só na interface.
- Números de balanceamento ainda são as propostas iniciais; o ajuste depende dos playtests (SCRUM-190).
- [preencher após os playtests]
