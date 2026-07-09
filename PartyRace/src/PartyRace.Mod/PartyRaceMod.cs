using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Modding;
using PartyRace.Core.Core;

namespace PartyRace.Mod;

[ModInitializer("Initialize")]
public static class PartyRaceMod
{
    private const string HarmonyId = "party_race";
    private const string MainMenuTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu";
    private static readonly string[] NetServiceOwnerTypeNames =
    [
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.LoadRunLobby",
        "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.RunLobby"
    ];
    private static readonly string[] BeginRunListenerTypeNames =
    [
        "MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NMultiplayerLoadGameScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.CustomRun.NCustomRunLoadScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.CustomRun.NCustomRunScreen",
        "MegaCrit.Sts2.Core.Nodes.Screens.DailyRun.NDailyRunLoadScreen"
    ];
    private const string ButtonNodeName = "PartyRaceMainMenuButton";
    private static bool s_dependencyResolverInstalled;

    public static void Initialize()
    {
        InstallDependencyResolver();
        PartyRaceLog.Append($"{PartyRaceConstants.ModName} {PartyRaceConstants.ModVersion} initializer called.");

        try
        {
            InstallMainMenuPatch();
            InstallNetServiceCapturePatches();
            InstallBeginRunPatches();
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to install Harmony patches: {exception}");
        }
    }

    private static void InstallDependencyResolver()
    {
        if (s_dependencyResolverInstalled)
        {
            return;
        }

        s_dependencyResolverInstalled = true;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            string? modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string? assemblyName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(modDirectory) || string.IsNullOrWhiteSpace(assemblyName))
            {
                return null;
            }

            string candidatePath = Path.Combine(modDirectory, $"{assemblyName}.dll");
            if (!File.Exists(candidatePath))
            {
                return null;
            }

            PartyRaceLog.Append($"Resolved dependency from mod directory: {assemblyName}.dll");
            return Assembly.LoadFrom(candidatePath);
        };
    }

    private static void InstallMainMenuPatch()
    {
        Type? mainMenuType = AccessTools.TypeByName(MainMenuTypeName);
        if (mainMenuType is null)
        {
            PartyRaceLog.Append($"Could not find STS2 type: {MainMenuTypeName}");
            return;
        }

        MethodInfo? readyMethod = AccessTools.Method(mainMenuType, "_Ready");
        MethodInfo? postfixMethod = AccessTools.Method(typeof(PartyRaceMod), nameof(OnMainMenuReady));
        if (readyMethod is null || postfixMethod is null)
        {
            PartyRaceLog.Append("Could not find main menu patch methods.");
            return;
        }

        Harmony harmony = new(HarmonyId);
        harmony.Patch(readyMethod, postfix: new HarmonyMethod(postfixMethod));
        PartyRaceLog.Append("Installed main menu patch.");
    }

    private static void InstallNetServiceCapturePatches()
    {
        Harmony harmony = new(HarmonyId);
        MethodInfo? postfixMethod = AccessTools.Method(typeof(PartyRaceMod), nameof(OnNetServiceOwnerCreated));
        if (postfixMethod is null)
        {
            PartyRaceLog.Append("Could not find net service capture postfix.");
            return;
        }

        foreach (string typeName in NetServiceOwnerTypeNames)
        {
            Type? ownerType = AccessTools.TypeByName(typeName);
            if (ownerType is null)
            {
                PartyRaceLog.Append($"Could not find STS2 net service owner type: {typeName}");
                continue;
            }

            foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(ownerType))
            {
                if (constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(INetGameService)))
                {
                    harmony.Patch(constructor, postfix: new HarmonyMethod(postfixMethod));
                    PartyRaceLog.Append($"Installed net service capture patch: {ownerType.FullName}.");
                }
            }
        }
    }

    private static void InstallBeginRunPatches()
    {
        Harmony harmony = new(HarmonyId);
        MethodInfo? prefixMethod = AccessTools.Method(typeof(PartyRaceMod), nameof(OnSts2BeginRun));
        if (prefixMethod is null)
        {
            PartyRaceLog.Append("Could not find STS2 BeginRun prefix.");
            return;
        }

        foreach (string typeName in BeginRunListenerTypeNames)
        {
            Type? listenerType = AccessTools.TypeByName(typeName);
            if (listenerType is null)
            {
                PartyRaceLog.Append($"Could not find STS2 BeginRun listener type: {typeName}");
                continue;
            }

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(listenerType)
                         .Where(method => method.Name == "BeginRun" && FirstParameterIsString(method)))
            {
                harmony.Patch(method, prefix: new HarmonyMethod(prefixMethod));
                PartyRaceLog.Append($"Installed STS2 BeginRun seed capture prefix patch: {listenerType.FullName}.{method.Name}.");
            }
        }
    }

    private static void OnMainMenuReady(object __instance)
    {
        try
        {
            if (__instance is not Control mainMenu)
            {
                PartyRaceLog.Append($"Main menu instance was not a Godot Control: {__instance.GetType().FullName}");
                return;
            }

            PartyRaceMenuView menuView = PartyRaceMenuView.EnsureAttached(mainMenu);
            if (HasDirectChild(mainMenu, ButtonNodeName))
            {
                return;
            }

            Button button = new()
            {
                Name = ButtonNodeName,
                Text = "Party Race",
                Position = new Vector2(32, 32),
                Size = new Vector2(220, 56),
                CustomMinimumSize = new Vector2(220, 56),
                ZIndex = 1000,
                MouseFilter = Control.MouseFilterEnum.Stop
            };
            button.Pressed += () =>
            {
                PartyRaceLog.Append("Party Race main menu button pressed.");
                menuView.Open();
            };

            mainMenu.AddChild(button);
            PartyRaceLog.Append("Added Party Race main menu button.");
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to add main menu button: {exception}");
        }
    }

    private static void OnNetServiceOwnerCreated(object __instance, INetGameService netService)
    {
        try
        {
            PartyRaceSts2Context.CaptureNetService(netService, __instance, __instance.GetType().FullName ?? __instance.GetType().Name);
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to capture STS2 net service: {exception}");
        }
    }

    private static bool OnSts2BeginRun(object __instance, object[] __args)
    {
        try
        {
            if (__args.Length == 0 || __args[0] is not string seed || string.IsNullOrWhiteSpace(seed))
            {
                PartyRaceLog.Append($"Skipped STS2 BeginRun seed capture from {__instance.GetType().FullName}: seed argument was missing.");
                return true;
            }

            bool wasPartyRaceRunArmed = PartyRaceSts2Context.IsPartyRaceRunArmed;
            PartyRaceSts2Context.ObserveSts2RunBegin(seed, __instance.GetType().FullName ?? __instance.GetType().Name);
            if (!wasPartyRaceRunArmed)
            {
                PartyRaceLog.Append($"Allowed STS2 BeginRun seed={seed}: Party Race was not armed before BeginRun.");
                return true;
            }

            if (!TryStartPartyRaceLocalRun(__instance, __args, seed))
            {
                PartyRaceLog.Append($"Allowed STS2 BeginRun seed={seed}: Party Race local run launch did not start.");
                return true;
            }

            PartyRaceLog.Append($"Skipped original STS2 BeginRun seed={seed}: Party Race local singleplayer run was launched.");
            return false;
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to observe STS2 BeginRun: {exception}");
            return true;
        }
    }

    private static bool TryStartPartyRaceLocalRun(object listener, object[] args, string seed)
    {
        if (!PartyRaceSts2Context.IsPartyRaceRunArmed)
        {
            PartyRaceLog.Append($"Skipped Party Race local run launch seed={seed}: Party Race is not armed.");
            return false;
        }

        if (args.Length < 2)
        {
            PartyRaceLog.Append($"Skipped Party Race local run launch seed={seed}: BeginRun run arguments were missing.");
            return false;
        }

        MethodInfo? method = listener.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
            {
                if (method.Name != "StartNewSingleplayerRun")
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length || parameters[0].ParameterType != typeof(string))
                {
                    return false;
                }

                for (int index = 1; index < parameters.Length; index++)
                {
                    object? argument = args[index];
                    Type parameterType = parameters[index].ParameterType;
                    if (argument is null)
                    {
                        if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                        {
                            return false;
                        }

                        continue;
                    }

                    if (!parameterType.IsInstanceOfType(argument))
                    {
                        return false;
                    }
                }

                return true;
            });

        if (method is null)
        {
            PartyRaceLog.Append($"Skipped Party Race local run launch seed={seed}: {listener.GetType().FullName} does not expose a matching StartNewSingleplayerRun overload.");
            return false;
        }

        try
        {
            object? result = method.Invoke(listener, args);
            PartyRaceLog.Append($"Invoked Party Race local singleplayer run seed={seed} via {listener.GetType().FullName}.{method.Name} args={args.Length}.");
            if (result is Task task)
            {
                _ = LogLocalRunLaunchTask(task, seed);
            }

            return true;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            PartyRaceLog.Append($"Failed to launch Party Race local singleplayer run seed={seed}: {exception.InnerException}");
            return false;
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to launch Party Race local singleplayer run seed={seed}: {exception}");
            return false;
        }
    }

    private static async Task LogLocalRunLaunchTask(Task task, string seed)
    {
        try
        {
            await task;
            PartyRaceLog.Append($"Party Race local singleplayer run launch task completed seed={seed}.");
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Party Race local singleplayer run launch task failed seed={seed}: {exception}");
        }
    }

    private static bool HasDirectChild(Node parent, string childName)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name.ToString() == childName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool FirstParameterIsString(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length > 0 && parameters[0].ParameterType == typeof(string);
    }
}
