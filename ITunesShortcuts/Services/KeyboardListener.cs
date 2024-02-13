using ITunesShortcuts.Enums;
using ITunesShortcuts.EventArgs;
using ITunesShortcuts.Helpers;
using ITunesShortcuts.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ITunesShortcuts.Services;

public class KeyboardListener
{
    readonly ILogger<KeyboardListener> logger;
    readonly WindowHelper windowHelper;

    readonly Dictionary<Key, HashSet<Modifier>?> keysToListen = new();
    readonly Dictionary<Key, Action<KeyPressedEventArgs>> keySubscriptions = new();
    readonly HashSet<Key> pressedKeys = new();

    public KeyboardListener(
        ILogger<KeyboardListener> logger,
        WindowHelper windowHelper)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;

        logger.LogInformation("[KeyboardListener-.ctor] KeyboardListener has been initialized.");
    }


    Win32.HookProc hookProc = default!;
    IntPtr hookId = IntPtr.Zero;

    private IntPtr SetHook(
        Win32.HookProc hookProc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        if (curModule is null || curModule.ModuleName is null)
        {
            logger.LogError("[KeyboardListener-SetHook] Failed to set hook: Could not retrieve main module of the current process.");
            return IntPtr.Zero;
        }

        IntPtr result = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, hookProc, Win32.GetModuleHandle(curModule.ModuleName), 0);

        logger.LogInformation("[KeyboardListener-SetHook] Hook was set: {result}", result);
        return result;
    }

    private IntPtr HookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        IntPtr PassNextHookEx() => Win32.CallNextHookEx(hookId, nCode, wParam, lParam);
        IntPtr CallNextHookEx() => BlockKeys ? (IntPtr)1 : PassNextHookEx();

        // Read key from parameter
        int vkCode = Marshal.ReadInt32(lParam);
        if (!Enum.IsDefined(typeof(Key), vkCode))
        {
            logger.LogError("[KeyboardListener-HookCallback] Hook callback: Invalid vkCode was returned.");
            return PassNextHookEx();
        }
        Key key = (Key)vkCode;
        HashSet<Modifier>? modifiers = null;

        // Early return if (should not listen for key & key is not a modifier) or not all modifiers are pressed
        bool shouldListenForKey = ListenForAllKeys ||keysToListen.TryGetValue(key, out modifiers);
        bool areAllModifiersPressed = modifiers?.All(modifier => modifier.ToKey() is Key modifierKey && pressedKeys.Contains(modifierKey)) ?? true;
        if (!shouldListenForKey && !key.IsModifier() || !areAllModifiersPressed)
            return PassNextHookEx();

        // Handle key up event
        if (wParam == (IntPtr)Win32.WM_KEYUP || wParam == (IntPtr)Win32.WM_SYSKEYUP)
        {
            pressedKeys.Remove(key);
            return CallNextHookEx();
        }
        // Early return if event is not key down
        if (wParam != (IntPtr)Win32.WM_KEYDOWN && wParam != (IntPtr)Win32.WM_SYSKEYDOWN)
            return PassNextHookEx();

        // Early return if key down already handled
        if (pressedKeys.Contains(key))
            return CallNextHookEx();

        // Handle key down event
        pressedKeys.Add(key);
        if (shouldListenForKey)
            OnKeyPressed(new(key, modifiers));

        return CallNextHookEx();
    }


    public bool IsListening { get; private set; } = false;

    public bool BlockKeys { get; set; } = false;
    public bool ListenForAllKeys { get; set; } = false;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;


    public bool Start()
    {
        hookProc = HookCallback;
        hookId = SetHook(hookProc);

        if (hookId == IntPtr.Zero)
        {
            logger.LogError("[KeyboardListener-Start] Failed to start keyboard hook.");
            return false;
        }

        logger.LogInformation("[KeyboardListener-Start] Keyboard hook successfully started.");
        IsListening = true;
        return true;
    }

    public bool Stop()
    {
        if (hookId == IntPtr.Zero)
            return false;

        if (!Win32.UnhookWindowsHookEx(hookId))
        {
            logger.LogError("[KeyboardListener-Stop] Failed to stop keyboard hook.");
            return false;
        }

        pressedKeys.Clear();
        hookProc = default!;
        hookId = IntPtr.Zero;

        logger.LogInformation("[KeyboardListener-Stop] Keyboard hook successfully stopped.");
        IsListening = false;
        return true;
    }


    public KeyValuePair<Key, Action<KeyPressedEventArgs>>[] GetSubscriptions()
    {
        KeyValuePair<Key, Action<KeyPressedEventArgs>>[] result = keySubscriptions.ToArray();

        logger.LogInformation("[KeyboardListener-GetSubscriptions] Got all key pressed subscriptions.");
        return result;
    }

    public bool Subscribe(
        Key key,
        Action<KeyPressedEventArgs> action)
    {
        bool result = keySubscriptions.TryAdd(key, action);

        logger.Log(result ? LogLevel.Information : LogLevel.Warning, "[KeyboardListener-Subscribe] Action subscribed to [{key}] press: {result}.", key.ToString(), result);
        return result;
    }
    
    public bool Unsubscribe(
        Key key)
    {
        bool result = keySubscriptions.Remove(key);

        logger.Log(result ? LogLevel.Information : LogLevel.Warning, "[KeyboardListener-Unsubscribe] Action unsubscribed from [{key}] press: {result}.", key.ToString(), result);
        return result;
    }

    void OnKeyPressed(
        KeyPressedEventArgs args)
    {
        KeyPressed?.Invoke(this, args);

        if (keySubscriptions.TryGetValue(args.Key, out Action<KeyPressedEventArgs>? action))
            windowHelper.EnqueDispatcher(() => action.Invoke(args));

    }


    public KeyValuePair<Key, HashSet<Modifier>?>[] GetKeys()
    {
        KeyValuePair<Key, HashSet<Modifier>?>[] result = keysToListen.ToArray();

        logger.LogInformation("[KeyboardListener-GetHotkeys] Got all keys to listen for.");
        return result;
    }

    public bool AddKey(
        Key key,
        HashSet<Modifier>? modifiers)
    {
        bool result = keysToListen.TryAdd(key, modifiers);

        logger.Log(result ? LogLevel.Information : LogLevel.Warning, "[KeyboardListener-AddKey] key added to listener [{key}]: {result}.", key.ToString(), result);
        return result;
    }
    public bool AddKey(
        Key key,
        Modifier modifier) =>
        keysToListen.TryAdd(key, new() { modifier });
    public bool AddKey(
        Key key) =>
        keysToListen.TryAdd(key, null);

    public bool RemoveKey(
        Key key)
    {
        bool result = keysToListen.Remove(key);

        logger.Log(result ? LogLevel.Information : LogLevel.Warning, "[KeyboardListener-RemoveKey] key removed from listener [{key}]: {result}.", key.ToString(), result);
        return result;
    }

}