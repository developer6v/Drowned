# Planilha de balanceamento (SCRUM-118)

Esta pasta **é** a planilha de balanceamento do Drowned. É a fonte única dos números do jogo:
custos, produção, consumo, thresholds, escala de tempo. Nenhum número fica hardcoded no C#.

Cada aba é um arquivo CSV. O Unity lê os CSVs e gera os ScriptableObjects em `Assets/Data/`.

## Fluxo para quem balanceia

1. Abra o CSV no Excel, LibreOffice ou Google Sheets (ou num editor de texto).
2. Mude os números. Mantenha as colunas e os ids.
3. Salve como **CSV separado por `;`**, codificação **UTF-8** (com ou sem BOM, tanto faz).
4. Commit + PR.
5. Quem estiver no Unity roda **Drowned → Gerar dados da planilha**. Os assets em `Assets/Data`
   são atualizados no lugar (mesmo GUID, as referências na cena não quebram). Erros aparecem no
   Console com arquivo e linha, e nesse caso nada é gravado.

Decimal com **ponto** (`1.5`), não vírgula. Linhas vazias são ignoradas. Textos com `;` dentro
vão entre aspas duplas.

## Abas de parâmetros

`Tempo.csv`, `Inicial.csv`, `Populacao.csv` e `NivelCidade.csv` têm sempre as colunas:

| Coluna | O que é |
|---|---|
| `Chave` | identificador fixo que o código procura. **Não renomear.** |
| `Parametro` | nome legível, só para pessoas |
| `Valor` | o número |
| `Unidade` | só documentação |
| `Justificativa` | por que esse valor; livre |

### Tempo.csv (SCRUM-119, SCRUM-121)

| Chave | Tipo | Significado |
|---|---|---|
| `secondsPerDay` | decimal ≥ 1 | segundos reais por dia simulado na velocidade 1x |
| `ticksPerDay` | inteiro ≥ 1 | ticks de simulação por dia; produção/consumo por dia são divididos por isso |
| `daysToCollapse` | inteiro ≥ 1 | dias consecutivos com energia **ou** oxigênio zerados até a derrota |

### Inicial.csv (SCRUM-120)

| Chave | Tipo | Significado |
|---|---|---|
| `initialPopulation` | inteiro ≥ 0 | habitantes no dia 1 |
| `initialEnergy` / `initialOxygen` / `initialFood` | decimal ≥ 0 | estoque inicial de cada recurso |

As estruturas já construídas no início ficam em `EstruturasIniciais.csv` (abaixo), porque
precisam de posição no grid.

### Populacao.csv (SCRUM-132, SCRUM-133)

| Chave | Tipo | Significado |
|---|---|---|
| `growthPerDay` | inteiro ≥ 0 | quantos habitantes nascem por dia quando há moradia e os 3 recursos > 0 |
| `starvationLossPerDay` | inteiro ≥ 0 | quantos morrem por dia com alimento zerado |
| `minimumPopulation` | inteiro ≥ 0 | a população nunca cai abaixo disso |
| `consumptionEnergy` / `consumptionOxygen` / `consumptionFood` | decimal ≥ 0 | consumo **por habitante por dia** |

### NivelCidade.csv (SCRUM-162)

| Chave | Tipo | Significado |
|---|---|---|
| `populationWeight` | decimal | pontos por habitante |
| `structureLevelWeight` | decimal | pontos por nível de cada estrutura construída (N2 vale o dobro) |
| `technologyWeight` | decimal | pontos por tecnologia pesquisada |
| `pointsPerLevel` | decimal ≥ 1 | pontos necessários para cada nível de cidade |
| `maxLevel` | inteiro ≥ 1 | nível máximo |

## Abas de tabela

Uma linha por item. A primeira linha é o cabeçalho e os nomes das colunas importam.

### EstruturasIniciais.csv (SCRUM-120)

| Coluna | Tipo | Significado |
|---|---|---|
| `id` | texto | id de uma estrutura de `Estruturas.csv` |
| `x`, `y` | inteiro | célula do canto inferior-esquerdo da pegada, no grid do mapa |
| `nivel` | inteiro ≥ 1 | nível com que ela começa |
| `Justificativa` | livre | |

O mapa da demo tem 24×24 células com borda de 2 não construível e o Monumento ocupando 4×4 no
centro (células 10 a 13). Posições fora da área construível ou em cima de outra estrutura dão
erro ao iniciar a partida.

### Estruturas.csv (SCRUM-128, SCRUM-143)

**Uma linha por nível.** As colunas de identidade (`id`, `nome`, `categoria`, `largura`,
`altura`, `requer_tecnologia`, `nivel_max_base`) são lidas da linha de nível 1 e precisam ser
iguais nas outras linhas do mesmo id.

| Coluna | Tipo | Significado |
|---|---|---|
| `id` | texto, `snake_case`, único | id estável usado no save, nas tecnologias e nas iniciais |
| `nome` | texto | nome em português na HUD |
| `categoria` | `Housing`, `Food`, `Energy`, `Oxygen`, `Research`, `Monument` | aba da barra de construção |
| `largura`, `altura` | inteiro ≥ 1 | pegada no grid em células |
| `requer_tecnologia` | `0` ou `1` | `1` = só aparece na barra depois que uma tecnologia com `UnlockStructure:<id>` for pesquisada |
| `nivel_max_base` | inteiro ≥ 1 | até que nível pode subir **sem** tecnologia; níveis acima precisam de `UnlockLevel:<id>:<nivel>` |
| `nivel` | inteiro ≥ 1, sequencial a partir de 1 | o nível que esta linha descreve |
| `custo_energia`, `custo_oxigenio`, `custo_alimento` | decimal ≥ 0 | custo para construir (nível 1) ou para subir até este nível |
| `prod_energia`, `prod_oxigenio`, `prod_alimento` | decimal ≥ 0 | produção **por dia** neste nível |
| `cons_energia`, `cons_oxigenio`, `cons_alimento` | decimal ≥ 0 | consumo **por dia** neste nível |
| `moradia` | inteiro ≥ 0 | capacidade de habitantes que este nível dá |
| `pesquisa_dia` | decimal ≥ 0 | pontos de pesquisa por dia (laboratório) |
| `sprite` | texto | sprite em `Assets/Sprites`, ver abaixo; vazio = sem sprite |

### Especialistas.csv (SCRUM-144, SCRUM-150)

| Coluna | Tipo | Significado |
|---|---|---|
| `id` | texto único | usado em `Monumento.csv` |
| `nome` | texto | |
| `populacao_minima` | inteiro ≥ 1 | população em que ele é desbloqueado automaticamente |
| `bonus` | `EnergyProduction`, `FoodProduction`, `ResearchSpeed`, `BuildCostDiscount` | tipo do bônus |
| `valor` | decimal | `0.20` = +20 % (ou −20 % de custo, no desconto) |
| `descricao` | texto | frase da HUD |

### Tecnologias.csv (SCRUM-146, SCRUM-149)

| Coluna | Tipo | Significado |
|---|---|---|
| `id` | texto único | |
| `nome` | texto | |
| `custo` | decimal ≥ 1 | pontos de pesquisa |
| `prerequisitos` | ids separados por `\|` | tecnologias que precisam estar pesquisadas; vazio = disponível desde o início |
| `efeitos` | efeitos separados por `\|` | ver sintaxe abaixo; vazio = só serve de pré-requisito |
| `descricao` | texto | |

Sintaxe de cada efeito:

| Efeito | Exemplo | Significado |
|---|---|---|
| `UnlockStructure:<id>` | `UnlockStructure:lab` | libera uma estrutura com `requer_tecnologia = 1` |
| `UnlockLevel:<id>:<nivel>` | `UnlockLevel:housing:2` | libera o upgrade até esse nível |
| `ProductionBonus:<recurso>:<valor>` | `ProductionBonus:Food:0.2` | +20 % na produção do recurso (`Energy`, `Oxygen`, `Food`) |
| `ResearchBonus:<valor>` | `ResearchBonus:0.3` | +30 % de pesquisa |
| `HousingBonus:<valor>` | `HousingBonus:0.25` | +25 % de capacidade de moradia |

A árvore não pode ter ciclo, e um pré-requisito tem que existir no mesmo arquivo.

### Monumento.csv (SCRUM-160, SCRUM-163)

| Coluna | Tipo | Significado |
|---|---|---|
| `etapa` | `0`, `1`, `2`… sequencial | `0` é a ruína (só o `sprite` é usado); `1` em diante são as etapas de restauração |
| `nome` | texto | |
| `req_energia`, `req_oxigenio`, `req_alimento` | decimal ≥ 0 | total de recursos que a etapa consome até ficar pronta |
| `contrib_energia`, `contrib_oxigenio`, `contrib_alimento` | decimal ≥ 0 | quanto a obra tira do estoque **por dia** enquanto ligada |
| `populacao_minima` | inteiro ≥ 0 | população necessária para a etapa avançar |
| `especialistas` | ids separados por `\|` | especialistas que precisam estar desbloqueados |
| `tecnologias` | ids separados por `\|` | tecnologias que precisam estar pesquisadas |
| `sprite` | texto | visual do Monumento quando esta etapa está concluída |

## Coluna `sprite`

Aponta para um spritesheet gerado por `tools/blender/render_batch.py`:

```
<Categoria>/<Nome>        → Assets/Sprites/<Categoria>/<Nome>/<Nome>_sheet.png, direção S, frame 0
<Categoria>/<Nome>:N      → mesma coisa, direção N
```

Os nomes disponíveis são os de `tools/blender/manifest.json`. Se o sheet não existir, o gerador
avisa no Console e deixa o sprite vazio (a demo cai no desenho procedural).

## O que o gerador produz

```
Assets/Data/
  Resources/GameRules.asset      regras + referências a tudo abaixo (carregado pela demo)
  Structures/<id>.asset          um por estrutura, com todos os níveis
  Specialists/<id>.asset
  Technologies/<id>.asset
  Monument.asset
```

Assets de ids que sumiram do CSV **não** são apagados automaticamente; apague à mão.

## Estado dos números

Os valores atuais são as propostas registradas no Jira (SCRUM-119, 120, 121, 143, 149, 150, 163)
mais o `Inicial` enviado pelo grupo em 04/11/2026. Estão marcados como propostas: o ajuste real
vem depois do playtest (SCRUM-190).
