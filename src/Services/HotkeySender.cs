using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.AliasFlow.Services;

public static class HotkeySender
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    // x64에서 INPUT 크기를 OS 기대치(40 bytes)로 맞추려면
    // UNION이 MOUSEINPUT(32 bytes)을 포함해야 합니다.
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    // ✅ Size를 강제로 지정해도 되고, MOUSEINPUT를 포함해 자동으로 max size가 되게 해도 됩니다.
    // 아래는 MOUSEINPUT 포함 + Size 지정으로 확실히 고정.
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static bool TrySendHotkey(string hotkey)
        => TrySendHotkey(hotkey, out _);

    public static bool TrySendHotkey(string hotkey, out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(hotkey))
        {
            error = "Hotkey is empty.";
            return false;
        }

        var parts = hotkey
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        if (parts.Count == 0)
        {
            error = $"Hotkey parse failed: '{hotkey}'";
            return false;
        }

        var modifiers = new List<Key>();
        Key? main = null;

        foreach (var p in parts)
        {
            var k = ParseKeyToken(p);
            if (k is null)
            {
                error = $"Unknown key token: '{p}' (hotkey: '{hotkey}')";
                return false;
            }

            if (IsModifier(k.Value))
                modifiers.Add(NormalizeModifier(k.Value));
            else
                main = k.Value;
        }

        if (main is null)
        {
            error = $"Main key not found (hotkey: '{hotkey}')";
            return false;
        }

        modifiers = modifiers
            .Distinct()
            .OrderBy(ModifierOrder)
            .ToList();

        var inputs = new List<INPUT>();

        foreach (var m in modifiers)
            inputs.Add(KeyDown(m));
        inputs.Add(KeyDown(main.Value));

        inputs.Add(KeyUp(main.Value));
        for (int i = modifiers.Count - 1; i >= 0; i--)
            inputs.Add(KeyUp(modifiers[i]));

        var arr = inputs.ToArray();

        // ✅ 여기서 size가 x64 기준 40이 되어야 정상
        var cbSize = Marshal.SizeOf<INPUT>();

        var sent = SendInput((uint)arr.Length, arr, cbSize);

        if (sent != arr.Length)
        {
            var last = Marshal.GetLastWin32Error();
            error = $"SendInput failed (sent {sent}/{arr.Length}), cbSize={cbSize}, Win32Error={last}, hotkey='{hotkey}'";
            return false;
        }

        return true;
    }

    private static int ModifierOrder(Key k) => k switch
    {
        Key.LeftCtrl or Key.RightCtrl => 1,
        Key.LeftShift or Key.RightShift => 2,
        Key.LeftAlt or Key.RightAlt => 3,
        Key.LWin or Key.RWin => 4,
        _ => 9
    };

    private static Key NormalizeModifier(Key k) => k switch
    {
        Key.RightCtrl => Key.LeftCtrl,
        Key.RightShift => Key.LeftShift,
        Key.RightAlt => Key.LeftAlt,
        Key.RWin => Key.LWin,
        _ => k
    };

    private static bool IsModifier(Key k) =>
        k is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;

    private static Key? ParseKeyToken(string token)
    {
        var t = token.Trim().ToLowerInvariant();

        return t switch
        {
            "ctrl" or "control" => Key.LeftCtrl,
            "shift" => Key.LeftShift,
            "alt" => Key.LeftAlt,
            "win" or "windows" => Key.LWin,

            "space" => Key.Space,
            "enter" or "return" => Key.Enter,
            "esc" or "escape" => Key.Escape,
            "tab" => Key.Tab,
            "backspace" => Key.Back,
            "delete" or "del" => Key.Delete,

            "up" => Key.Up,
            "down" => Key.Down,
            "left" => Key.Left,
            "right" => Key.Right,

            _ => TryParseEnumOrChar(token)
        };
    }

    private static Key? TryParseEnumOrChar(string token)
    {
        if (Enum.TryParse<Key>(token, ignoreCase: true, out var k))
            return k;

        if (token.Length == 1)
        {
            var c = token[0];

            if (char.IsLetter(c))
            {
                var name = char.ToUpperInvariant(c).ToString();
                if (Enum.TryParse<Key>(name, ignoreCase: true, out k))
                    return k;
            }

            if (char.IsDigit(c))
            {
                var name = "D" + c;
                if (Enum.TryParse<Key>(name, ignoreCase: true, out k))
                    return k;
            }
        }

        return null;
    }

    private static INPUT KeyDown(Key k)
    {
        var vk = KeyInterop.VirtualKeyFromKey(k);
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private static INPUT KeyUp(Key k)
    {
        var vk = KeyInterop.VirtualKeyFromKey(k);
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
