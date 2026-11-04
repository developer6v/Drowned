using System.Collections.Generic;
using Drowned.Monument;
using Drowned.Population;
using Drowned.Resources;
using Drowned.UI;
using Drowned.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Drowned.Core
{
    /// <summary>
    /// Monta a cena de jogo inteira por código (SCRUM-188, demo jogável): câmera, relógio, grid isométrico com
    /// tiles gerados, GameManager com as regras (do asset arrastado ou do <see cref="DemoContent"/>), os Behaviours
    /// de cada sistema, o Monumento, o modo construção, o áudio e a HUD.
    ///
    /// Uso: cena vazia, um objeto "Bootstrap" com este componente, Play. Para usar arte e números reais, arraste
    /// o asset <see cref="GameRules"/> no campo; quando a cena definitiva existir, este componente sai.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DemoBootstrap : MonoBehaviour
    {
        [Tooltip("Regras da partida. Vazio = conteúdo de demonstração com os números propostos no Jira.")]
        [SerializeField] private GameRules _rules;

        [Header("Mapa gerado")]
        [SerializeField, Min(8)] private int _mapWidth = 24;
        [SerializeField, Min(8)] private int _mapHeight = 24;
        [Tooltip("Borda não construível ao redor do mapa, em células.")]
        [SerializeField, Min(0)] private int _border = 2;
        [Tooltip("Lado do quadrado reservado ao Monumento no centro, em células.")]
        [SerializeField, Min(2)] private int _monumentSize = 4;

        [Header("Sons placeholder (opcional)")]
        [SerializeField] private AudioManager _audioPrefab;

        private readonly List<Object> _created = new List<Object>();

        private void Awake()
        {
            if (GameManagerBehaviour.Instance != null)
            {
                Debug.LogWarning("DemoBootstrap: a cena já tem um GameManager; nada a montar.", this);
                return;
            }

            // Regras: campo do Inspector > Assets/Data/Resources/GameRules.asset (gerado da planilha, SCRUM-118)
            // > conteúdo em memória com os números propostos (DemoContent).
            GameRules rules = _rules != null ? _rules : Resources.Load<GameRules>("GameRules");
            bool demoArt = rules == null;
            if (rules == null)
            {
                Debug.Log("DemoBootstrap: GameRules.asset não encontrado; usando DemoContent. Rode Drowned → Gerar dados da planilha.", this);
                rules = DemoContent.CreateRules(out var created);
                _created.AddRange(created);
            }

            // 1. Câmera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.02f, 0.07f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // 2. Relógio
            var clockGo = new GameObject("GameClock");
            clockGo.SetActive(false); // Configure() precisa rodar antes do Awake
            clockGo.AddComponent<GameClockBehaviour>().Configure(rules.SecondsPerDay, rules.TicksPerDay);
            clockGo.AddComponent<GameSpeedInput>();
            clockGo.SetActive(true);

            // 3. Grid e tilemaps
            var map = BuildMap();

            // 4. Controle de câmera
            var camCtrl = cam.gameObject.AddComponent<CameraController2D>();
            camCtrl.Configure(map, 7f);

            // 5. GameManager
            var gmGo = new GameObject("GameManager");
            gmGo.SetActive(false);
            var gm = gmGo.AddComponent<GameManagerBehaviour>();
            gm.Configure(rules, map);
            gmGo.SetActive(true);
            if (gm.Game == null)
            {
                Debug.LogError("DemoBootstrap: o GameManager não inicializou (veja o erro acima).", this);
                return;
            }

            // 6. Sistemas
            new GameObject("ResourceManager").AddComponent<ResourceManagerBehaviour>();
            new GameObject("PopulationManager").AddComponent<PopulationManagerBehaviour>();
            var structuresGo = new GameObject("Structures");
            structuresGo.AddComponent<StructureManagerBehaviour>();

            // 7. Monumento no centro do mapa
            if (gm.Game.Monument.StageCount > 0)
            {
                var monGo = new GameObject("Monument");
                monGo.SetActive(false);
                var sr = monGo.AddComponent<SpriteRenderer>();
                int cx = _mapWidth / 2 - _monumentSize / 2, cy = _mapHeight / 2 - _monumentSize / 2;
                Vector3 a = map.GridToWorldCenter(cx, cy);
                Vector3 b = map.GridToWorldCenter(cx + _monumentSize - 1, cy + _monumentSize - 1);
                monGo.transform.position = (a + b) * 0.5f;
                sr.sortingOrder = -(cx + cy);
                if (demoArt) sr.sprite = DemoContent.RuinSprite();
                monGo.AddComponent<MonumentBehaviour>();
                monGo.SetActive(true);
                if (sr.sprite == null && rules.Monument != null) sr.sprite = rules.Monument.RuinSprite;
            }

            // 8. Interação, áudio e HUD
            var inputGo = new GameObject("BuildMode");
            inputGo.AddComponent<BuildModeController>();
            inputGo.AddComponent<StructureSelector>();

            if (AudioManager.Instance == null)
            {
                var audio = _audioPrefab != null ? Instantiate(_audioPrefab) : new GameObject("AudioManager").AddComponent<AudioManager>();
                audio.LoadPlaceholders();
            }

            var hudGo = new GameObject("HUD");
            hudGo.SetActive(false);
            var hud = hudGo.AddComponent<HudBuilder>();
            hud.Configure(DemoContent.ResourceIcons()); // ícones finais: SCRUM-184
            hudGo.SetActive(true);
        }

        private TilemapMapBehaviour BuildMap()
        {
            var gridGo = new GameObject("Grid");
            gridGo.SetActive(false);
            var grid = gridGo.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var ground = NewTilemap(gridGo.transform, "Ground", 0);
            var buildable = NewTilemap(gridGo.transform, "Buildable", 1);

            var groundTile = DemoArt.Tile(DemoArt.Ground, DemoArt.GroundOutline);
            var buildTile = DemoArt.Tile(DemoArt.Buildable, DemoArt.Buildable);
            _created.Add(groundTile);
            _created.Add(buildTile);

            int mx0 = _mapWidth / 2 - _monumentSize / 2, my0 = _mapHeight / 2 - _monumentSize / 2;
            for (int x = 0; x < _mapWidth; x++)
                for (int y = 0; y < _mapHeight; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    ground.SetTile(cell, groundTile);
                    bool inBorder = x < _border || y < _border || x >= _mapWidth - _border || y >= _mapHeight - _border;
                    bool inMonument = x >= mx0 && x < mx0 + _monumentSize && y >= my0 && y < my0 + _monumentSize;
                    if (!inBorder && !inMonument) buildable.SetTile(cell, buildTile);
                }

            var map = gridGo.AddComponent<TilemapMapBehaviour>();
            map.Configure(ground, buildable);
            gridGo.SetActive(true); // Awake monta o GridMap
            return map;
        }

        private static Tilemap NewTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = sortingOrder;
            r.mode = TilemapRenderer.Mode.Individual;
            return tm;
        }

        private void OnDestroy()
        {
            foreach (var o in _created) if (o != null) Destroy(o);
            _created.Clear();
        }
    }
}
