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
