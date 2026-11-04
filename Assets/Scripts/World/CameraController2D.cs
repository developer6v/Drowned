using UnityEngine;
#if DROWNED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drowned.World
{
    /// <summary>
    /// Câmera 2D com pan e zoom limitados ao mapa (SCRUM-123).
    ///
    /// Controles:
    /// - Pan: WASD / setas, ou arrastar com o botão do meio ou direito do mouse, ou mouse na borda da tela (opcional).
    /// - Zoom: roda do mouse, centrado no cursor. Teclas + / - também.
    /// - Suavização por Lerp para não dar tranco.
    ///
    /// Na cena: coloque na Main Camera (ortográfica). Arraste o objeto Grid com
    /// <see cref="TilemapMapBehaviour"/> em "Map" para os limites virem do terreno; se vazio, usa "Manual Bounds".
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController2D : MonoBehaviour
    {
        [Header("Limites")]
        [SerializeField] private TilemapMapBehaviour _map;
        [Tooltip("Usado só quando Map está vazio. Em unidades do mundo: x=minX, y=minY, z=maxX, w=maxY.")]
        [SerializeField] private Vector4 _manualBounds = new(-20, -20, 20, 20);
        [Tooltip("Margem além do terreno que a câmera pode mostrar (para ver o fundo subaquático).")]
        [SerializeField, Min(0f)] private float _margin = 3f;

        [Header("Pan")]
        [SerializeField, Min(0f)] private float _keyboardPanSpeed = 12f;
        [SerializeField] private bool _edgePan = false;
        [SerializeField, Range(1f, 50f)] private float _edgePanPixels = 12f;
        [SerializeField, Range(0f, 1f)] private float _smoothing = 0.15f;

        [Header("Zoom")]
        [SerializeField, Min(0.5f)] private float _minOrthoSize = 3f;
        [SerializeField, Min(1f)] private float _maxOrthoSize = 20f;
        [SerializeField, Min(0.01f)] private float _zoomStep = 1.2f;
        [SerializeField] private float _startOrthoSize = 8f;

        private Camera _cam;
        private Vector3 _targetPos;
        private float _targetSize;
        private bool _dragging;
        private Vector3 _dragOriginWorld;

        private CameraBounds Bounds
        {
            get
            {
                if (_map != null)
                {
                    var b = _map.WorldBounds;
                    return new CameraBounds(b.min.x, b.min.y, b.max.x, b.max.y).Expand(_margin);
                }
                return new CameraBounds(_manualBounds.x, _manualBounds.y, _manualBounds.z, _manualBounds.w).Expand(_margin);
            }
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _targetSize = _startOrthoSize;
            _targetPos = transform.position;
        }

        private void Start()
        {
            // Centraliza no mapa na primeira vez
            var b = Bounds;
            _targetPos = new Vector3(b.CenterX, b.CenterY, transform.position.z);
            ApplyImmediate();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            HandleZoom();
            HandleDrag();
            HandleKeyboard(dt);
            HandleEdgePan(dt);
            ApplySmooth(dt);
        }

        // ------------------------------------------------------------------ entrada

        private void HandleZoom()
        {
            float scroll = ReadScroll();
            if (Mathf.Approximately(scroll, 0f) && !ReadZoomKey(out scroll)) return;

            Vector3 mouseWorldBefore = _cam.ScreenToWorldPoint(ReadMousePosition());
            float factor = scroll > 0 ? 1f / _zoomStep : _zoomStep;
            _targetSize = Bounds.ClampOrthoSize(_targetSize * factor, _minOrthoSize, _maxOrthoSize, _cam.aspect);

            // Zoom centrado no cursor: desloca o alvo para o ponto sob o mouse ficar parado
            float ratio = _targetSize / _cam.orthographicSize;
            Vector3 camPos = transform.position;
            Vector3 offset = mouseWorldBefore - camPos;
            _targetPos = mouseWorldBefore - offset * ratio;
            _targetPos.z = camPos.z;
        }

        private void HandleDrag()
        {
            bool held = ReadDragHeld();
            if (held && !_dragging)
            {
                _dragging = true;
                _dragOriginWorld = _cam.ScreenToWorldPoint(ReadMousePosition());
            }
            else if (!held)
            {
                _dragging = false;
                return;
            }

            Vector3 now = _cam.ScreenToWorldPoint(ReadMousePosition());
            Vector3 delta = _dragOriginWorld - now;
            delta.z = 0f;
            _targetPos += delta;
            transform.position += delta; // arrasto responde na hora; o smoothing só vale para teclado
        }

        private void HandleKeyboard(float dt)
        {
            Vector2 axis = ReadMoveAxis();
            if (axis.sqrMagnitude < 0.0001f) return;
            // Velocidade proporcional ao zoom: mais afastado, mais rápido
            float speed = _keyboardPanSpeed * (_cam.orthographicSize / _startOrthoSize);
            _targetPos += (Vector3)(axis.normalized * speed * dt);
        }

        private void HandleEdgePan(float dt)
        {
            if (!_edgePan || !Application.isFocused) return;
            Vector2 m = ReadMousePosition();
            Vector2 dir = Vector2.zero;
            if (m.x <= _edgePanPixels) dir.x -= 1;
            else if (m.x >= Screen.width - _edgePanPixels) dir.x += 1;
            if (m.y <= _edgePanPixels) dir.y -= 1;
            else if (m.y >= Screen.height - _edgePanPixels) dir.y += 1;
            if (dir == Vector2.zero) return;
            float speed = _keyboardPanSpeed * (_cam.orthographicSize / _startOrthoSize);
            _targetPos += (Vector3)(dir.normalized * speed * dt);
        }

        // ------------------------------------------------------------------ aplicação

        private void ApplySmooth(float dt)
        {
            var b = Bounds;
            _targetSize = b.ClampOrthoSize(_targetSize, _minOrthoSize, _maxOrthoSize, _cam.aspect);
            var (cx, cy) = b.ClampPosition(_targetPos.x, _targetPos.y, _targetSize, _cam.aspect);
            _targetPos = new Vector3(cx, cy, transform.position.z);

            float t = _smoothing <= 0f ? 1f : 1f - Mathf.Pow(_smoothing, dt * 60f);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, t);
            transform.position = Vector3.Lerp(transform.position, _targetPos, t);
        }

        private void ApplyImmediate()
        {
            var b = Bounds;
            _targetSize = b.ClampOrthoSize(_targetSize, _minOrthoSize, _maxOrthoSize, _cam.aspect);
            var (cx, cy) = b.ClampPosition(_targetPos.x, _targetPos.y, _targetSize, _cam.aspect);
            _targetPos = new Vector3(cx, cy, transform.position.z);
            _cam.orthographicSize = _targetSize;
            transform.position = _targetPos;
        }

        /// <summary>Move a câmera para um ponto do mundo (ex.: focar no Monumento).</summary>
        public void FocusOn(Vector3 world, bool instant = false)
        {
            _targetPos = new Vector3(world.x, world.y, transform.position.z);
            if (instant) ApplyImmediate();
        }

        // ------------------------------------------------------------------ leitura de input (novo ou antigo)

        private static Vector2 ReadMousePosition()
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private static float ReadScroll()
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0f;
#endif
        }

        private static bool ReadZoomKey(out float dir)
        {
            dir = 0f;
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) dir = 1f;
            else if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) dir = -1f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) dir = 1f;
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) dir = -1f;
#endif
            return dir != 0f;
        }

        private static bool ReadDragHeld()
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null && (m.middleButton.isPressed || m.rightButton.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(2) || Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private static Vector2 ReadMoveAxis()
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }
    }
}
