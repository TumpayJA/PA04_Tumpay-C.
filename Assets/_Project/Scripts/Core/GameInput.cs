using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EscapeGame
{
    /// <summary>
    /// ÚNICO punto del proyecto que lee teclado y ratón.
    /// Compila con el Input System nuevo (por defecto en Unity 6) y también con el Input Manager antiguo,
    /// así que funciona sin importar qué valor tenga "Active Input Handling" en Project Settings > Player.
    /// Ningún otro script llama a Input.* ni a Keyboard.current: si quieres cambiar una tecla, se cambia aquí.
    /// </summary>
    public static class GameInput
    {
        // Equivale a la sensibilidad 0.1 del Input Manager antiguo, para que ambos caminos se sientan igual.
        const float MouseScale = 0.1f;

        /// <summary>Movimiento: x = izquierda/derecha, y = atrás/adelante. Largo máximo 1.</summary>
        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                if (kb == null) return Vector2.zero;
                float x = Axis(kb.dKey.isPressed || kb.rightArrowKey.isPressed, kb.aKey.isPressed || kb.leftArrowKey.isPressed);
                float y = Axis(kb.wKey.isPressed || kb.upArrowKey.isPressed, kb.sKey.isPressed || kb.downArrowKey.isPressed);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>Cuánto se movió el ratón en este fotograma (ya escalado).</summary>
        public static Vector2 LookDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Mouse mouse = Mouse.current;
                return mouse == null ? Vector2.zero : mouse.delta.ReadValue() * MouseScale;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>Shift mantenido.</summary>
        public static bool Sprint
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
                return false;
#endif
            }
        }

        /// <summary>Espacio (solo el fotograma en que se pulsa).</summary>
        public static bool JumpPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                return kb != null && kb.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Space);
#else
                return false;
#endif
            }
        }

        /// <summary>Tecla E: interactuar (abrir la salida).</summary>
        public static bool InteractPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                return kb != null && kb.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.E);
#else
                return false;
#endif
            }
        }

        /// <summary>Tecla F: linterna.</summary>
        public static bool FlashlightPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                return kb != null && kb.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.F);
#else
                return false;
#endif
            }
        }

        /// <summary>Esc o P: pausa.</summary>
        public static bool PausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard kb = Keyboard.current;
                return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#else
                return false;
#endif
            }
        }

        /// <summary>F3: mostrar/ocultar el panel de rendimiento.</summary>
        public static bool PerformanceTogglePressed
        {
            get { return FunctionKeyDown(3); }
        }

        /// <summary>Teclas F1 a F8 (las usan DebugTools y PerformanceOverlay).</summary>
        public static bool FunctionKeyDown(int number)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            switch (number)
            {
                case 1: return kb.f1Key.wasPressedThisFrame;
                case 2: return kb.f2Key.wasPressedThisFrame;
                case 3: return kb.f3Key.wasPressedThisFrame;
                case 4: return kb.f4Key.wasPressedThisFrame;
                case 5: return kb.f5Key.wasPressedThisFrame;
                case 6: return kb.f6Key.wasPressedThisFrame;
                case 7: return kb.f7Key.wasPressedThisFrame;
                case 8: return kb.f8Key.wasPressedThisFrame;
                default: return false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (number)
            {
                case 1: return Input.GetKeyDown(KeyCode.F1);
                case 2: return Input.GetKeyDown(KeyCode.F2);
                case 3: return Input.GetKeyDown(KeyCode.F3);
                case 4: return Input.GetKeyDown(KeyCode.F4);
                case 5: return Input.GetKeyDown(KeyCode.F5);
                case 6: return Input.GetKeyDown(KeyCode.F6);
                case 7: return Input.GetKeyDown(KeyCode.F7);
                case 8: return Input.GetKeyDown(KeyCode.F8);
                default: return false;
            }
#else
            return false;
#endif
        }

        static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
