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
    private const string UnlockStateTypeName = "MegaCrit.Sts2.Core.Unlocks.UnlockState";
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
    private static readonly string[] CustomButtonOwnerTypeNames =
    [
        "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NSingleplayerSubmenu",
        "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMultiplayerHostSubmenu"
    ];
    private const string ButtonNodeName = "PartyRaceMainMenuButton";
    private const string ForceCustomButtonNodeName = "PartyRaceForceCustomButton";
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
            InstallUnlockBypassPatches();
            InstallCustomButtonUnlockPatches();
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

    private static void InstallUnlockBypassPatches()
    {
        Type? unlockStateType = AccessTools.TypeByName(UnlockStateTypeName);
        if (unlockStateType is null)
        {
            PartyRaceLog.Append($"Could not find STS2 unlock state type: {UnlockStateTypeName}");
            return;
        }

        Harmony harmony = new(HarmonyId);
        MethodInfo? instancePostfix = AccessTools.Method(typeof(PartyRaceMod), nameof(ForceUnlockStateInstance));
        MethodInfo? resultPostfix = AccessTools.Method(typeof(PartyRaceMod), nameof(ForceUnlockStateResult));
        MethodInfo? intPostfix = AccessTools.Method(typeof(PartyRaceMod), nameof(ForceUnlockedCount));
        if (instancePostfix is null || resultPostfix is null || intPostfix is null)
        {
            PartyRaceLog.Append("Could not find unlock bypass postfix methods.");
            return;
        }

        foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(unlockStateType))
        {
            TryPatch(harmony, constructor, instancePostfix, $"{unlockStateType.FullName}..ctor");
        }

        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(unlockStateType))
        {
            if ((method.Name == "get_NumberOfRuns" || method.Name == "EpochUnlockCount") &&
                     method.ReturnType == typeof(int))
            {
                TryPatch(harmony, method, intPostfix, $"{unlockStateType.FullName}.{method.Name}");
            }
            else if (method.ReturnType == unlockStateType && !method.ContainsGenericParameters)
            {
                TryPatch(harmony, method, resultPostfix, $"{unlockStateType.FullName}.{method.Name}");
            }
        }

        foreach (Type type in unlockStateType.Assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType == unlockStateType && !method.ContainsGenericParameters)
                {
                    TryPatch(harmony, method, resultPostfix, $"{type.FullName}.{method.Name}");
                }
            }
        }
    }

    private static void TryPatch(Harmony harmony, MethodBase method, MethodInfo postfix, string description)
    {
        try
        {
            harmony.Patch(method, postfix: new HarmonyMethod(postfix));
            PartyRaceLog.Append($"Installed unlock bypass patch: {description}.");
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Skipped unlock bypass patch {description}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void InstallCustomButtonUnlockPatches()
    {
        Harmony harmony = new(HarmonyId);
        MethodInfo? postfixMethod = AccessTools.Method(typeof(PartyRaceMod), nameof(OnCustomButtonOwnerReady));
        if (postfixMethod is null)
        {
            PartyRaceLog.Append("Could not find custom button unlock postfix.");
            return;
        }

        foreach (string typeName in CustomButtonOwnerTypeNames)
        {
            Type? ownerType = AccessTools.TypeByName(typeName);
            MethodInfo? readyMethod = ownerType is null ? null : AccessTools.Method(ownerType, "_Ready");
            if (ownerType is null || readyMethod is null)
            {
                PartyRaceLog.Append($"Could not find STS2 custom button owner ready method: {typeName}._Ready");
                continue;
            }

            harmony.Patch(readyMethod, postfix: new HarmonyMethod(postfixMethod));
            PartyRaceLog.Append($"Installed custom button unlock patch: {ownerType.FullName}._Ready.");
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

    private static void ForceUnlockStateInstance(object __instance)
    {
        ForceUnlockState(__instance);
    }

    private static void ForceUnlockStateResult(object __result)
    {
        ForceUnlockState(__result);
    }

    private static void ForceUnlockedCount(ref int __result)
    {
        __result = Math.Max(__result, 999);
    }

    private static void ForceUnlockState(object? unlockState)
    {
        if (unlockState is null)
        {
            return;
        }

        FieldInfo? field = unlockState.GetType().GetField("_unlockedEpochIds", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(unlockState) is not HashSet<string> unlockedEpochIds)
        {
            PartyRaceLog.Append($"Skipped force unlock state for {unlockState.GetType().FullName}: _unlockedEpochIds was not a HashSet<string>.");
            return;
        }

        int before = unlockedEpochIds.Count;
        foreach (string epochId in GetAllEpochIds(unlockState.GetType().Assembly))
        {
            unlockedEpochIds.Add(epochId);
        }

        FieldInfo? numberOfRunsField = unlockState.GetType().GetField("<NumberOfRuns>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (numberOfRunsField?.FieldType == typeof(int))
        {
            numberOfRunsField.SetValue(unlockState, Math.Max((int)(numberOfRunsField.GetValue(unlockState) ?? 0), 999));
        }

        PartyRaceLog.Append($"Forced unlock state epochs {before}->{unlockedEpochIds.Count} for {unlockState.GetType().FullName}.");
    }

    private static IEnumerable<string> GetAllEpochIds(Assembly sts2Assembly)
    {
        foreach (Type type in sts2Assembly.GetTypes())
        {
            if (type.Namespace != "MegaCrit.Sts2.Core.Timeline.Epochs" ||
                !type.Name.EndsWith("Epoch", StringComparison.Ordinal))
            {
                continue;
            }

            yield return type.Name;
        }
    }

    private static void OnCustomButtonOwnerReady(object __instance)
    {
        try
        {
            FieldInfo? field = __instance.GetType().GetField("_customButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object? customButton = field?.GetValue(__instance);
            if (customButton is null)
            {
                PartyRaceLog.Append($"Skipped custom button unlock for {__instance.GetType().FullName}: _customButton was not found.");
                return;
            }

            if (customButton is Control control)
            {
                control.Visible = true;
                control.MouseFilter = Control.MouseFilterEnum.Stop;
            }

            TrySetProperty(customButton, "Disabled", false);
            TrySetProperty(customButton, "IsLocked", false);
            TryInvoke(customButton, "SetDisabled", false);
            TryInvoke(customButton, "SetVisuallyLocked", false);
            TryInvoke(customButton, "SetLocked", false);
            TryInvoke(customButton, "SetVisible", true);
            AddForceCustomButton(__instance, customButton);
            PartyRaceLog.Append($"Forced custom button unlocked for {__instance.GetType().FullName}.");
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to force custom button unlocked for {__instance.GetType().FullName}: {exception}");
        }
    }

    private static void AddForceCustomButton(object owner, object customButton)
    {
        if (owner is not Control ownerControl)
        {
            PartyRaceLog.Append($"Skipped force custom overlay for {owner.GetType().FullName}: owner was not a Godot Control.");
            return;
        }

        if (HasDirectChild(ownerControl, ForceCustomButtonNodeName))
        {
            return;
        }

        Vector2 position = new(600, 120);
        Vector2 size = new(240, 360);
        if (customButton is Control customControl)
        {
            position = customControl.Position;
            size = customControl.Size;
            if (size.X <= 0 || size.Y <= 0)
            {
                size = customControl.CustomMinimumSize;
            }
        }

        if (size.X <= 0 || size.Y <= 0)
        {
            size = new Vector2(240, 360);
        }

        Button forceButton = new()
        {
            Name = ForceCustomButtonNodeName,
            Text = "CUSTOM",
            Position = position,
            Size = size,
            CustomMinimumSize = size,
            ZIndex = 5000,
            MouseFilter = Control.MouseFilterEnum.Stop,
            Modulate = new Color(1f, 1f, 1f, 0.85f)
        };
        forceButton.Pressed += () => InvokeCustomRunOwner(owner);

        ownerControl.AddChild(forceButton);
        PartyRaceLog.Append($"Added force custom overlay for {owner.GetType().FullName} at {position} size={size}.");
    }

    private static void InvokeCustomRunOwner(object owner)
    {
        try
        {
            MethodInfo? method = owner.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method =>
                    (method.Name == "OpenCustomScreen" || method.Name == "OnCustomPressed") &&
                    method.GetParameters().Length == 1);

            if (method is not null)
            {
                method.Invoke(owner, [null]);
                PartyRaceLog.Append($"Invoked custom run owner method {owner.GetType().FullName}.{method.Name} from Party Race overlay.");
                return;
            }

            MethodInfo? startHostMethod = owner.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "StartHost" && method.GetParameters().Length == 1);
            ParameterInfo? parameter = startHostMethod?.GetParameters().FirstOrDefault();
            if (startHostMethod is not null && parameter?.ParameterType.IsEnum == true)
            {
                object customMode = Enum.Parse(parameter.ParameterType, "Custom");
                startHostMethod.Invoke(owner, [customMode]);
                PartyRaceLog.Append($"Invoked custom host method {owner.GetType().FullName}.{startHostMethod.Name} from Party Race overlay.");
                return;
            }

            PartyRaceLog.Append($"Could not find a custom run method on {owner.GetType().FullName}.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            PartyRaceLog.Append($"Failed to invoke custom run owner for {owner.GetType().FullName}: {exception.InnerException}");
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to invoke custom run owner for {owner.GetType().FullName}: {exception}");
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

    private static void TrySetProperty(object target, string propertyName, object value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.CanWrite == true && property.PropertyType.IsInstanceOfType(value))
        {
            property.SetValue(target, value);
        }
    }

    private static void TryInvoke(object target, string methodName, object argument)
    {
        MethodInfo? method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
            {
                if (method.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(argument);
            });

        method?.Invoke(target, [argument]);
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
