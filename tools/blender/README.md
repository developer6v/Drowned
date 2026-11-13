# Projeto 3D auxiliar (Blender → sprite)

Nada aqui roda dentro do jogo. Esta pasta transforma modelos 3D em spritesheets 2D para o Unity.

## Requisitos

Blender 5.2 LTS (ou 4.2+) no PATH ou chamado pelo caminho completo. No Windows:

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
```

## Teste do pipeline (SCRUM-117)

```bash
blender -b --python tools/blender/render_sprites.py -- --test-cube
```

Gera `tools/blender/out/TestCube/TestCube_sheet.png` com 4 direções. Copie para `Assets/Sprites/Placeholders/` e importe no Unity:

1. Selecione o PNG → Inspector → **Texture Type: Sprite (2D and UI)**, **Sprite Mode: Multiple**, **Pixels Per Unit: 128**, **Filter Mode: Bilinear**, **Compression: None**.
2. **Sprite Editor → Slice → Grid By Cell Size** com o `frameSize` do `.json` (256 × 256 no padrão).
3. Arraste um dos sprites para a cena. Se o cubo aparece em losango, a pipeline funciona.

## Renderizar um modelo de verdade

```bash
blender -b --python tools/blender/render_sprites.py -- --model modelos/moradia.blend --name Moradia_N1
blender -b --python tools/blender/render_sprites.py -- --model modelos/habitante.fbx --name Habitante --size 128 --frames 1-16
```

| Opção | Padrão | Para que serve |
|---|---|---|
| `--model` | | `.blend`, `.fbx`, `.obj`, `.glb`, `.gltf` |
| `--name` | nome do arquivo | prefixo dos PNGs |
| `--size` | 256 | lado do frame em px |
| `--directions` | 4 | 4 (S, W, N, E) ou 8 |
| `--frames` | 1 | `1` para estático, `1-16` para animação idle |
| `--ortho-scale` | automático | zoom da câmera; use o mesmo valor para todas as estruturas para manterem a escala |
| `--elevation` / `--azimuth` | 30 / 45 | ângulo da câmera isométrica (2:1) |
| `--out` | `tools/blender/out` | pasta de saída |

## Render em lote (SCRUM-154)

Um comando renderiza todos os modelos do jogo e deixa os sheets prontos em `Assets/Sprites`:

```bash
blender -b --python tools/blender/render_batch.py -- --manifest tools/blender/manifest.json
blender -b --python tools/blender/render_batch.py -- --only Housing_L1 --only Inhabitant
blender -b --python tools/blender/render_batch.py -- --skip-existing
```

- `manifest.json` lista cada item com `name`, `category` (pasta em `Assets/Sprites/`) e `model`
  (arquivo real) **ou** `generator` (`placeholder:<id>` de `placeholder_models.py`). Campos
  opcionais (`size`, `frames`, `orthoScale`, `groundAnchor`...) sobrescrevem `defaults`.
- Todas as estruturas usam o mesmo `orthoScale`, então uma Moradia 1×1 e um Laboratório 2×2
  saem na proporção certa um em relação ao outro. `groundAnchor: 0.25` põe o chão do modelo
  a 25 % da borda de baixo do frame em todos os sprites; o `.json` grava isso como `pivot`.
  O `pixelsPerUnit` do `.json` é calculado (`size / orthoScale × √2`) para que 1 célula do grid
  tenha exatamente 1 unidade de largura no Unity (Grid isométrico com cellSize 1 × 0,5).
- Saída: `Assets/Sprites/<category>/<name>/<name>_sheet.png` + `.json`. Frames individuais
  ficam em `tools/blender/out/` (ignorado pelo Git).
- Uma falha num item não derruba o lote; o resumo no fim lista ok/pulados/falhas e o processo
  sai com código 1 se algo falhou.

**No Unity não há passo manual.** `Assets/Scripts/Editor/SpriteSheetPostprocessor.cs` lê o
`.json` ao lado de cada `*_sheet.png` e configura o import: Sprite (2D and UI), Multiple,
PPU 128, sem compressão, fatiado em grade, pivô do `.json`, sprites nomeados
`<Nome>_<Dir>_<frame>` (ex.: `Housing_L1_S_000`). Para reimportar depois de rodar o lote de
novo, basta o Unity ganhar foco (ou Assets → Reimport na pasta).

### Modelos prontos (Kenney, CC0)

As estruturas, o habitante, o Monumento e a decoração usam modelos dos kits **Space Kit** e **Nature Kit**
da [Kenney](https://kenney.nl), licença **CC0** (domínio público; crédito não obrigatório, mas registrado aqui).
Os arquivos usados ficam em `tools/blender/modelos/kenney/{space,nature}/` com as licenças ao lado
(`LICENSE-space-kit.txt`, `LICENSE-nature-kit.txt`). Para trocar um modelo, mude o caminho no manifesto e rode o lote.

Opções do manifesto para montar um item a partir de modelos prontos:

| Campo | Onde | O que faz |
|---|---|---|
| `parts` | item | lista de peças; cada uma tem `model` (ou `generator`) e opcionais abaixo |
| `offset` | peça | desloca a peça `[x, y, z]` em unidades do modelo |
| `scale`, `rotate` | peça | escala e giro em Z (graus), em torno do centro da base da própria peça |
| `glassMaterials` | peça | materiais desta peça que viram vidro translúcido (ex.: `["dark"]`) |
| `fit` | item | `[largura, profundidade]` em células: escala o item para caber na pegada, centra e apoia em Z = 0 |
| `fitMargin`, `maxHeight` | item | folga do `fit` (padrão 0.9) e altura máxima em unidades |
| `fitHeight` | item | altura final em unidades (personagens) |
| `scale` + `anchor` | item | escala fixa e ponto `[x, y]` que vai para a origem; usado no Monumento para as 5 etapas terem a mesma base |
| `rotate` | item | gira o item inteiro em Z, para a frente apontar para -Y |

Decorações vão para `category: "Decor/Resources/Decor"`, porque o jogo as carrega com `Resources.LoadAll`.

### Modelos placeholder

Enquanto a arte final (SCRUM-126, 135–138, 151, 164, 165) não existe, `placeholder_models.py`
gera modelos low-poly só com primitivas: moradia N1/N2, fazenda de algas, gerador, planta de
oxigênio, laboratório, habitante e os 5 visuais do Monumento (ruína + 4 etapas). Quando o
modelo real ficar pronto, troque no manifesto:

```json
{ "name": "Housing_L1", "category": "Structures", "model": "modelos/moradia_n1.blend" }
```

e rode o lote de novo. Para abrir um placeholder no Blender e usar de referência de escala:

```bash
blender -b --python tools/blender/placeholder_models.py -- --id housing_1 --save tools/blender/out/housing_1.blend
```

## Convenções de modelagem (para quem está no Blender)

- Unidade: **1 unidade Blender = 1 célula do grid** no jogo. Uma estrutura 1×1 ocupa de -0.5 a 0.5 em X e Y.
- Origem do objeto no **centro da base** (Z = 0 é o chão).
- Frente do modelo apontando para **-Y** (direção "S"). O script gira em Z para as outras direções.
- Aplicar escala e rotação antes de exportar (Ctrl+A). Materiais simples, sem texturas procedurais pesadas.
- Para animação idle, salvar a ação no `.blend` e passar `--frames 1-N`.

## Saída

```
out/<Nome>/
  <Nome>_dir0_f000.png     frames individuais (dir0 = S, dir1 = W, dir2 = N, dir3 = E)
  <Nome>_sheet.png         spritesheet: uma linha por direção, uma coluna por frame
  <Nome>_sheet.json        tamanho do frame, direções, número de frames
```

A pasta `out/` está no `.gitignore`. O que vai para o Git são os sheets copiados para `Assets/Sprites/`.

## Quadros da introdução (SCRUM-198)

```bash
blender -b --python tools/blender/render_intro.py --
blender -b --python tools/blender/render_intro.py -- --only 3 --percent 30 --samples 16 --preview
```

Monta 5 cenas com os modelos Kenney (cidade sob a cúpula → cúpula rachando → ruínas → sobreviventes →
o Monumento ao longe) e grava `Assets/Sprites/Intro/Resources/Intro/intro_1..5.png` em 1920×1080.
`--preview` grava em `tools/blender/out/Intro/` sem mexer em Assets. A névoa da água é feita com cartões
transparentes na frente da câmera e os feixes de luz com cones emissivos: volume de verdade no EEVEE do
Blender 5.2 deixa o quadro preto. O `IntroPostprocessor` importa os PNGs como Sprite.
