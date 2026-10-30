using UnityEngine;
using UnityEngine.Events;

namespace Drowned.Core
{
    /// <summary>
    /// Ponte entre a cena e o <see cref="GameClock"/> (SCRUM-124 / SCRUM-125).
    /// Coloque um único objeto "GameClock" na cena com este componente.
    ///
    /// Outros sistemas pegam a referência por <see cref="Instance"/> e assinam
    /// <c>Clock.OnTick</c> / <c>Clock.OnNewDay</c>. Os UnityEvents abaixo existem para ligar
    /// a HUD pelo inspetor sem escrever código.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameClockBehaviour : MonoBehaviour
    {
        public static GameClockBehaviour Instance { get; private set; }

        [Header("Escala de tempo (SCRUM-119) — valor final vem da planilha")]
        [Tooltip("Segundos reais por dia simulado na velocidade 1x.")]
        [SerializeField, Min(1f)] private float _secondsPerDay = 60f;

        [Tooltip("Ticks de simulação por dia. Recursos e população atualizam a cada tick.")]
        [SerializeField, Min(1)] private int _ticksPerDay = 24;

        [SerializeField] private GameSpeed _initialSpeed = GameSpeed.Normal;

        [Header("Eventos para a HUD")]
        public UnityEvent<int> onNewDay;
        public UnityEvent<GameSpeed> onSpeedChanged;

        public GameClock Clock { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Já existe um GameClockBehaviour na cena. Destruindo o duplicado.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Clock = new GameClock(_secondsPerDay, _ticksPerDay);
            Clock.OnNewDay += day => onNewDay?.Invoke(day);
            Clock.OnSpeedChanged += speed => onSpeedChanged?.Invoke(speed);
            Clock.SetSpeed(_initialSpeed);
        }

        private void Update()
        {
            // unscaledDeltaTime: a velocidade é tratada pelo GameClock, não pelo Time.timeScale,
            // para que animações de UI e menus continuem normais quando o jogo está pausado.
            Clock.Advance(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Atalhos para botões da HUD (OnClick no inspetor)
        public void SetPaused() => Clock.SetSpeed(GameSpeed.Paused);
        public void SetNormal() => Clock.SetSpeed(GameSpeed.Normal);
        public void SetFast() => Clock.SetSpeed(GameSpeed.Fast);
        public void SetFastest() => Clock.SetSpeed(GameSpeed.Fastest);
        public void TogglePause() => Clock.TogglePause();
    }
}
