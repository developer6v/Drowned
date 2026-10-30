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
