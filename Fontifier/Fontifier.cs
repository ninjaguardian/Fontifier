using Fontifier;
using HarmonyLib;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players;
using Il2CppTMPro;
using MelonLoader;
using RumbleModUI;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// TODO: Make ModUI show what the fonts look like.

[assembly: MelonInfo(typeof(Fontifier.Fontifier), "Fontifier", FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]

[assembly: MelonOptionalDependencies("HealthDisplayWithFont", "tournamentScoring")]

namespace Fontifier
{
    /// <summary>
    /// Contains mod version.
    /// </summary>
    static class FontifierModInfo
    {
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVer = "1.0.2";
        /// <summary>
        /// Mod schema version.
        /// </summary>
        public const string ModSchemaVer = "1.0.0";
    }

    /// <summary>
    /// Makes sure it's a valid font name.
    /// </summary>
    class FontNameValidator : ValidationParameters
    {
        /// <inheritdoc/>
        public override bool DoValidation(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                return true; // default font
            }
            foreach (TMP_FontAsset font in Fontifier.fonts)
            {
                if (font.name.Equals(Input, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Lets you change the font for other mods.
    /// </summary>
    class Fontifier : MelonMod
    {
        private const string ModDesc = "Enter a font from the Font List or leave it empty to use the default font.\n\nMake sure to hit enter!";
        /// <summary>
        /// The logger.
        /// </summary>
        public static readonly MelonLogger.Instance Logger = new("Fontifier", System.Drawing.Color.FromArgb(255, 0, 160, 230));
        private readonly static Mod ModUI = new();
        /// <summary>
        /// All font names.
        /// </summary>
        public readonly static List<TMP_FontAsset> fonts = new();
        private static TMP_FontAsset _defaultFont;

        private static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    GameObject go = RumbleModdingAPI.Calls.Create.NewText();
                    _defaultFont = go.GetComponent<TextMeshPro>().font;
                    UnityEngine.Object.Destroy(go);
                }
                return _defaultFont;
            }
        }

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            UI.instance.UI_Initialized += OnUIInitialized;
        }

        #region MODUI
        private void OnUIInitialized()
        {
            ModUI.ModName = "Fontifier";
            ModUI.ModVersion = FontifierModInfo.ModVer;
            ModUI.SetFolder("Fontifier");
            ModUI.AddDescription("Description", "", "Lets you change the font for other mods.", new Tags { IsSummary = true });

            HashSet<string> existingNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (string fontPath in
                Directory.EnumerateFiles(@"UserData\Fontifier\fonts", "*.*", SearchOption.TopDirectoryOnly)
                .Where(
                    f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                try
                {
                    TMP_FontAsset loadedFont = TMP_FontAsset.CreateFontAsset(
                        fontPath,
                        0,
                        90,
                        5,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024
                    );
                    loadedFont.hideFlags = HideFlags.HideAndDontSave;

                    string baseName;
                    try
                    {
                        using PrivateFontCollection fontCollection = new();
                        fontCollection.AddFontFile(fontPath);
                        if (fontCollection.Families.Length == 0 || string.IsNullOrWhiteSpace(fontCollection.Families[0].Name))
                            throw new Exception("Font has no internal family name.");
                        baseName = fontCollection.Families[0].Name;
                    }
                    catch (Exception ex)
                    {
                        baseName = Path.GetFileNameWithoutExtension(fontPath);
                        Logger.Warning($"Could not read internal font name for {fontPath}, using filename instead.", ex);
                    }

                    if (string.IsNullOrWhiteSpace(baseName))
                    {
                        throw new Exception("Font has an invalid name/filename.");
                    }

                    string candidate = baseName;
                    int suffix = 0;
                    while (existingNames.Contains(candidate))
                    {
                        suffix++;
                        candidate = $"{baseName} ({suffix})";
                    }

                    loadedFont.name = candidate;
                    existingNames.Add(loadedFont.name);
                    fonts.Add(loadedFont);
                    Logger.Msg($"Loaded font: {loadedFont.name} from {fontPath}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            ModUI.AddDescription("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true });

            FontNameValidator validator = new();
            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("HealthDisplayWithFont", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Msg("HealthDisplayWithFont was found.");
                    ModUI.AddToList("HealthDisplayWithFont", "", ModDesc, new Tags()).CurrentValueChanged += HealthDisplayWithFontChanged;
                    ModUI.AddValidation("HealthDisplayWithFont", validator);
                }
                else if (mod.Info.Name.Equals("RUMBLE Tournament Scoring", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Msg("RUMBLE Tournament Scoring was found.");
                    TournamentScoringFont = ModUI.AddToList("RUMBLE Tournament Scoring", "", ModDesc, new Tags());
                    TournamentScoringFont.CurrentValueChanged += TournamentScoringChanged;
                    ModUI.AddValidation("RUMBLE Tournament Scoring", validator);

                    HarmonyInstance.Patch(TournamentScoringPatch.TargetMethod(), null, TournamentScoringPatch.GetPostfix());
                }
            }

            ModUI.GetFromFile();
            UI.instance.AddMod(ModUI);
            Logger.Msg("Fontifier added to ModUI.");
        }

        [HarmonyPatch(typeof(Mod), nameof(ModUI.GetFromFile))]
        public static class GetFromFilePatch
        {
            [HarmonyPrefix]
            static bool GetFromFile(Mod __instance)
            {
                if (!object.ReferenceEquals(__instance, ModUI)) return true;
                Type type = __instance.GetType();

                FieldInfo debugField = type.GetField("debug", BindingFlags.NonPublic | BindingFlags.Instance);
                bool debug = false;
                if (debugField != null)
                    debug = (bool)debugField.GetValue(__instance);
                else
                    Logger.Warning("Could not get debug field from ModUI.");

                PropertyInfo prop = type.GetProperty("IsFileLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite)
                {
                    Logger.Error("Could not get IsFileLoaded from ModUI. Using default 'GetFromFile'.");
                    return true;
                }

                MethodInfo valueValidationMethod = type.GetMethod("ValueValidation", BindingFlags.NonPublic | BindingFlags.Instance);
                if (valueValidationMethod == null)
                {
                    Logger.Error("Could not get ValueValidation from ModUI.");
                }

                FieldInfo foldersField = type.GetField("Folders", BindingFlags.NonPublic | BindingFlags.Instance);
                if (foldersField == null)
                {
                    Logger.Error("Could not get Folders from ModUI. Using default 'GetFromFile'.");
                    return true;
                }
                Baum_API.Folders Folders = (Baum_API.Folders)foldersField.GetValue(__instance);

                string Path;
                string[] Lines;
                bool ValidFile = false;

                if (Folders.GetSubFolder(0) != null) Path = Folders.GetFolderString(Folders.GetSubFolder(0)) + @"\" + __instance.SettingsFile;
                else Path = Folders.GetFolderString() + @"\" + __instance.SettingsFile;

                if (File.Exists(Path))
                {
                    Lines = File.ReadAllLines(Path);

                    if (Lines[0].Contains(__instance.ModName) && Lines[0].Contains(FontifierModInfo.ModSchemaVer))
                    {
                        ValidFile = true;
                        Lines[0] = "";
                        Lines[1] = "";
                    }

                    if (ValidFile)
                    {
                        foreach (string line in Lines)
                        {
                            foreach (ModSetting setting in __instance.Settings)
                            {
                                if (setting.Name.Length + 2 < line.Length)
                                {
                                    if (line[..setting.Name.Length] == setting.Name)
                                    {
                                        bool Valid;
                                        string value = line[(setting.Name.Length + 2)..];
                                        if (valueValidationMethod == null)
                                        {
                                            ModSetting<string> stringset = (ModSetting<string>)setting;

                                            if (!stringset.ValidationParameters.DoValidation(value))
                                                Valid = false;
                                            else
                                            {
                                                stringset.Value = value;
                                                Valid = true;
                                            }
                                        }
                                        else
                                        {
                                            Valid = (bool)valueValidationMethod.Invoke(__instance, new object[] { value, setting });
                                        }
                                        if (Valid)
                                        {
                                            setting.SavedValue = setting.Value;
                                            if (debug)
                                            {
                                                new MelonLogger.Instance(BuildInfo.ModName, System.Drawing.Color.FromArgb(200, 0, 200, 0)).Msg(__instance.ModName + " - " + setting.Name + " " + setting.Value.ToString());
                                            }
                                        }
                                        else
                                        {
                                            new MelonLogger.Instance(BuildInfo.ModName, System.Drawing.Color.FromArgb(200, 0, 200, 0)).Msg(__instance.ModName + " - " + setting.Name + " File Read Error.");
                                        }
                                    }
                                }
                            }
                        }
                        prop.SetValue(__instance, true);
                    }
                    else
                    {
                        prop.SetValue(__instance, false);
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Mod), nameof(ModUI.SaveModData))]
        public static class SaveModDataPatch
        {
            [HarmonyPrefix]
            static bool SaveModData(Mod __instance, string UI_String)
            {
                if (!object.ReferenceEquals(__instance, ModUI)) return true;
                Type type = __instance.GetType();

                PropertyInfo prop = type.GetProperty("IsSaved", BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite)
                {
                    Logger.Error("Could not get IsSaved from ModUI. Using default 'SaveModData'.");
                    return true;
                }

                FieldInfo foldersField = type.GetField("Folders", BindingFlags.NonPublic | BindingFlags.Instance);
                if (foldersField == null)
                {
                    Logger.Error("Could not get Folders from ModUI. Using default 'SaveModData'.");
                    return true;
                }
                Baum_API.Folders Folders = (Baum_API.Folders)foldersField.GetValue(__instance);

                FieldInfo eventField = type.GetField("ModSaved", BindingFlags.Instance | BindingFlags.NonPublic);
                if (eventField == null)
                {
                    Logger.Error("Could not get ModSaved from ModUI.");
                }

                string Path;
                string Output;

                if (Folders.GetSubFolder(0) != null) Path = Folders.GetFolderString(Folders.GetSubFolder(0)) + @"\" + __instance.SettingsFile;
                else Path = Folders.GetFolderString() + @"\" + __instance.SettingsFile;

                Folders.CheckAllFoldersExist();

                Output = __instance.ModName + " " + FontifierModInfo.ModSchemaVer + Il2CppSystem.Environment.NewLine + UI_String + Il2CppSystem.Environment.NewLine;

                foreach (ModSetting Setting in __instance.Settings)
                {
                    if (!Setting.Tags.DoNotSave)
                    {
                        Output += Setting.Name + ": " + Setting.GetValueAsString() + Il2CppSystem.Environment.NewLine;
                    }
                }

                File.WriteAllText(Path, Output);

                for (int i = 0; i < __instance.Settings.Count; i++)
                {
                    __instance.Settings[i].SavedValue = __instance.Settings[i].Value;
                }
                if (eventField != null)
                    ((Action)eventField.GetValue(__instance))?.Invoke();
                prop.SetValue(__instance, true);

                return false;
            }
        }
        #endregion

        #region HealthDisplayWithFont
        private static void HealthDisplayWithFontSetAll(Type healthDisplayType, TMP_FontAsset font)
        {
            MethodInfo method = healthDisplayType.GetMethod(
                "GetHealthbar",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Transform), typeof(ControllerType?) },
                null
            );

            if (method == null)
            {
                Logger.Warning("GetHealthbar method not found in HealthDisplayWithFont.");
                return;
            }

            foreach (Player player in PlayerManager.instance.AllPlayers)
            {
                PlayerController controller = player.Controller;
                if (controller == null) continue;

                Transform uiTransform = method.Invoke(null, new object[] { controller.transform.Find("UI"), controller.controllerType }) as Transform;
                TextMeshPro tmp = uiTransform?.Find("HealthText")?.GetComponent<TextMeshPro>();
                if (tmp != null) tmp.font = font;
            }
        }

        private static void HealthDisplayWithFontChanged(object sender, EventArgs args)
        {
            MelonMod healthMod = RegisteredMelons.FirstOrDefault(m => m.Info.Name == "HealthDisplayWithFont");
            if (healthMod == null)
            {
                Logger.Warning("HealthDisplayWithFont mod not found.");
                return;
            }

            Type modType = healthMod.GetType();
            FieldInfo fontField = modType.GetField("fontAsset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fontField == null)
            {
                Logger.Warning("Field 'fontAsset' not found in HealthDisplayWithFont.");
                return;
            }

            string fontName = (args as ValueChange<string>)?.Value;
            if (string.IsNullOrWhiteSpace(fontName))
            {
                fontField.SetValue(null, DefaultFont);
                HealthDisplayWithFontSetAll(modType, DefaultFont);
                return;
            }

            TMP_FontAsset font = fonts.FirstOrDefault(f => string.Equals(f.name, fontName, StringComparison.OrdinalIgnoreCase));
            if (font == null)
            {
                Logger.Warning($"Font with name {fontName} is not loaded.");
                return;
            }

            fontField.SetValue(null, font);
            HealthDisplayWithFontSetAll(modType, font);
        }
        #endregion

        #region RUMBLE Tournament Scoring
        private static ModSetting<string> TournamentScoringFont;

        class TournamentScoringPatch
        {
            public static MethodBase TargetMethod()
            {
                return RegisteredMelons.FirstOrDefault(m => m.Info.Name == "RUMBLE Tournament Scoring").GetType().GetMethod("SpawnScoreboard", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public static void Postfix(MelonMod __instance)
            {
                TournamentScoringChanged(__instance, TournamentScoringFont.Value as string);
            }

            public static HarmonyMethod GetPostfix()
            {
                return new HarmonyMethod(typeof(TournamentScoringPatch).GetMethod(nameof(Postfix)));
            }
        }

        private static void TournamentScoringChanged(object sender, EventArgs args)
        {
            MelonMod tournamentScoringMod = RegisteredMelons.FirstOrDefault(m => m.Info.Name == "RUMBLE Tournament Scoring");
            if (tournamentScoringMod == null)
            {
                Logger.Warning("RUMBLE Tournament Scoring mod not found.");
                return;
            }

            TournamentScoringChanged(tournamentScoringMod, (args as ValueChange<string>)?.Value);
        }

        private static void TournamentScoringChanged(MelonMod tournamentScoringMod, string fontName)
        {
            if (tournamentScoringMod.GetType()
                .GetField("scoreboardText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(tournamentScoringMod) is TextMeshPro scoreboardText
                && scoreboardText != null)
            {
                if (string.IsNullOrWhiteSpace(fontName))
                {
                    scoreboardText.font = DefaultFont;
                    return;
                }

                TMP_FontAsset font = fonts.FirstOrDefault(f => string.Equals(f.name, fontName, StringComparison.OrdinalIgnoreCase));
                if (font != null)
                    scoreboardText.font = font;
                else
                    Logger.Warning($"Font with name {fontName} is not loaded.");
            }
        }
        #endregion
    }
}
