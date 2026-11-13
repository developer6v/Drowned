using UnityEngine;
using UnityEngine.EventSystems;
#if DROWNED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Drowned.Core
{
    /// <summary>
    /// Leitura de mouse e teclado que funciona com o Input System novo e com o Input Manager antigo.
    /// Mesmo padrão da câmera (SCRUM-123). Só o que o modo construção e os menus precisam.
    /// </summary>
    public static class GameInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool LeftClickDown
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        public static bool RightClickDown
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(1);
#else
                return false;
#endif
            }
        }

        public static bool EscapeDown
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Escape);
#else
                return false;
#endif
            }
        }

        /// <summary>Tecla de atalho F5 (salvar rápido).</summary>
        public static bool F5Down
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.F5);
#else
                return false;
#endif
            }
        }

        /// <summary>Barra de espaço (avançar a introdução, SCRUM-198).</summary>
        public static bool SpaceDown
        {
            get
            {
#if DROWNED_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Space);
#else
                return false;
#endif
            }
        }

        /// <summary>Verdadeiro se o mouse está sobre um elemento de UI (botão, painel).</summary>
        public static bool IsPointerOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
