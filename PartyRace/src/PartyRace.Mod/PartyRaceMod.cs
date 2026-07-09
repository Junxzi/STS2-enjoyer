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
            PartyRaceSts2Context.CaptureNetService(netService, __instance.GetType().FullName ?? __instance.GetType().Name);
        }
        catch (Exception exception)
        {
            PartyRaceLog.Append($"Failed to capture STS2 net service: {exception}");
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
}
