using System;
using Deltatime.Settings;
using UnityEngine.InputSystem;

namespace Deltatime.InputSystem
{
    public static class PlayerControlsFactory
    {
        public static PlayerControls Create()
        {
            PlayerControls controls = new PlayerControls();
            GameSettingsService.TryApplyBindingOverrides(controls.asset);
            return controls;
        }
    }

    /// <summary>
    /// Supplies short, current binding names to menu, HUD and tutorial copy.
    /// </summary>
    public static class InputBindingDisplay
    {
        private static PlayerControls displayControls;
        private static string loadedOverrides;

        public static string Get(string actionName, string bindingName = null)
        {
            EnsureCurrent();
            InputAction action = displayControls.asset.FindAction(
                actionName,
                true);
            int bindingIndex = FindBindingIndex(action, bindingName);
            return bindingIndex < 0
                ? "?"
                : ToCompactName(action.bindings[bindingIndex].effectivePath);
        }

        public static string GetMovement()
        {
            string up = Get("Move", "up");
            string left = Get("Move", "left");
            string down = Get("Move", "down");
            string right = Get("Move", "right");
            if (up == "W" && left == "A" && down == "S" && right == "D")
            {
                return "WASD";
            }

            return $"{up}/{left}/{down}/{right}";
        }

        public static void Invalidate()
        {
            displayControls?.Dispose();
            displayControls = null;
            loadedOverrides = null;
        }

        public static int FindBindingIndex(
            InputAction action,
            string bindingName = null)
        {
            if (action == null)
            {
                return -1;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(bindingName) ||
                    string.Equals(
                        binding.name,
                        bindingName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string ToCompactName(string effectivePath)
        {
            if (string.IsNullOrEmpty(effectivePath))
            {
                return "UNBOUND";
            }

            const string keyboard = "<Keyboard>/";
            if (effectivePath.StartsWith(keyboard, StringComparison.OrdinalIgnoreCase))
            {
                string key = effectivePath.Substring(keyboard.Length);
                switch (key.ToLowerInvariant())
                {
                    case "space": return "SPACE";
                    case "leftshift": return "L SHIFT";
                    case "rightshift": return "R SHIFT";
                    case "leftctrl": return "L CTRL";
                    case "rightctrl": return "R CTRL";
                    case "leftalt": return "L ALT";
                    case "rightalt": return "R ALT";
                    case "uparrow": return "UP";
                    case "downarrow": return "DOWN";
                    case "leftarrow": return "LEFT";
                    case "rightarrow": return "RIGHT";
                    default:
                        return key.StartsWith("digit", StringComparison.OrdinalIgnoreCase)
                            ? key.Substring(5)
                            : key.ToUpperInvariant();
                }
            }

            const string mouse = "<Mouse>/";
            if (effectivePath.StartsWith(mouse, StringComparison.OrdinalIgnoreCase))
            {
                string button = effectivePath.Substring(mouse.Length);
                switch (button.ToLowerInvariant())
                {
                    case "leftbutton": return "LMB";
                    case "rightbutton": return "RMB";
                    case "middlebutton": return "MMB";
                    case "forwardbutton": return "MOUSE 5";
                    case "backbutton": return "MOUSE 4";
                }
            }

            return InputControlPath.ToHumanReadableString(
                    effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice |
                    InputControlPath.HumanReadableStringOptions.UseShortNames)
                .ToUpperInvariant();
        }

        private static void EnsureCurrent()
        {
            string json = GameSettingsService.Current.BindingOverridesJson ?? string.Empty;
            if (displayControls != null && loadedOverrides == json)
            {
                return;
            }

            Invalidate();
            displayControls = new PlayerControls();
            GameSettingsService.TryApplyBindingOverrides(
                displayControls.asset,
                json);
            loadedOverrides = json;
        }
    }
}
