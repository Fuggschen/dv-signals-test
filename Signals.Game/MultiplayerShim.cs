using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace Signals.Game
{
    /// <summary>
    /// Loads the Signals.Multiplayer integration DLL via reflection to avoid a hard dependency on MultiplayerAPI.
    /// Follows the "Shim With Integration" pattern from the Multiplayer API docs.
    /// </summary>
    internal static class MultiplayerShim
    {
        private const string MULTIPLAYER_MOD_ID = "Multiplayer";
        private const string MPAPI_ASSEMBLY_NAME = "MultiplayerAPI";
        private const string MP_INTEGRATION_DLL = "Signals.Multiplayer.dll";
        private const string MP_INTEGRATION_BOOTSTRAP = "Signals.Multiplayer.Bootstrap";

        private static MethodInfo? _teardownMethod;

        internal static bool IsInitialized { get; private set; }

        internal static void Initialize()
        {
            var multiplayer = UnityModManager.FindMod(MULTIPLAYER_MOD_ID);

            Assembly? mpapiAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.GetName().Name == MPAPI_ASSEMBLY_NAME)
                    {
                        mpapiAssembly = asm;
                        break;
                    }
                }
                catch
                {
                    // Skip assemblies that throw on GetName() (e.g. dynamic assemblies).
                }
            }

            try
            {
                if (multiplayer == null || !multiplayer.Enabled)
                {
                    SignalsMod.LogVerbose("Multiplayer mod not found or not enabled, signal sync disabled.");
                    return;
                }

                if (mpapiAssembly == null)
                {
                    SignalsMod.Warning("Multiplayer mod is enabled but MultiplayerAPI assembly not loaded. Check that LoadAfter is set in info.json.");
                    return;
                }

                var path = Path.Combine(SignalsMod.Instance.Path, MP_INTEGRATION_DLL);

                if (!File.Exists(path))
                {
                    SignalsMod.Warning($"{MP_INTEGRATION_DLL} was not found, multiplayer sync disabled.");
                    return;
                }

                var mpAssembly = Assembly.LoadFile(path);
                var bootstrap = mpAssembly.GetType(MP_INTEGRATION_BOOTSTRAP);

                if (bootstrap == null)
                {
                    SignalsMod.Warning($"Failed to find {MP_INTEGRATION_BOOTSTRAP}, multiplayer sync disabled.");
                    return;
                }

                var init = bootstrap.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                _teardownMethod = bootstrap.GetMethod("Teardown", BindingFlags.Public | BindingFlags.Static);

                init?.Invoke(null, new object[]
                {
                    SignalsMod.Guid,
                    (Action<string>)SignalsMod.Log,
                    (Action<string>)SignalsMod.LogVerbose
                });

                IsInitialized = true;
                SignalsMod.Log("Multiplayer signal sync loaded.");
            }
            catch (Exception ex)
            {
                SignalsMod.Warning($"Failed to load multiplayer sync.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }

        internal static void Teardown()
        {
            if (!IsInitialized) return;

            try
            {
                _teardownMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                SignalsMod.Warning($"Failed to teardown multiplayer sync.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }

            _teardownMethod = null;
            IsInitialized = false;
        }
    }
}
