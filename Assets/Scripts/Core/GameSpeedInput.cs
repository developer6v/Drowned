using UnityEngine;
#if DROWNED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drowned.Core
{
    /// <summary>
    /// Atalhos de teclado para os controles de velocidade (SCRUM-125):
    /// Espaço = pausa/retoma, 1 = 1x, 2 = 2x, 3 = 3x.
    /// Funciona tanto com o Input System novo quanto com o Input Manager antigo.
    /// Coloque no mesmo objeto do <see cref="GameClockBehaviour"/>.
    /// </summary>
    [RequireComponent(typeof(GameClockBehaviour))]
    public sealed class GameSpeedInput : MonoBehaviour
    {
        private GameClockBehaviour _clock;

        private void Awake()
        {
            _clock = GetComponent<GameClockBehaviour>();
        }

        private void Update()
        {
            if (_clock.Clock == null) return;

            if (PressedToggle()) _clock.Clock.TogglePause();
            else if (PressedDigit(1)) _clock.Clock.SetSpeed(GameSpeed.Normal);
            else if (PressedDigit(2)) _clock.Clock.SetSpeed(GameSpeed.Fast);
            else if (PressedDigit(3)) _clock.Clock.SetSpeed(GameSpeed.Fastest);
        }

        private static bool PressedToggle()
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private static bool PressedDigit(int digit)
        {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return digit switch
            {
                1 => kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame,
                2 => kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame,
                3 => kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return digit switch
            {
                1 => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1),
                2 => Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2),
                3 => Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3),
                _ => false
            };
#else
            return false;
#endif
        }
    }
}
