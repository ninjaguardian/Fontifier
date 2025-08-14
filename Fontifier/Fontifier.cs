using Fontifier;
using HarmonyLib;
using Il2CppTMPro;
using MelonLoader;
using RumbleModUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// TODO: Make ModUI show what the fonts look like.
// TODO: Font size changer (especialy for MatchInfo in gym + CRUMBLE)

#region Assemblies
[assembly: MelonInfo(typeof(Fontifier.Fontifier), FontifierModInfo.ModName, FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(FontifierModInfo.MLVersion, true)]
#endregion

namespace Fontifier
{
    /// <summary>
    /// Contains mod info.
    /// </summary>
    public static class FontifierModInfo
    {
        /// <summary>
        /// Mod name.
        /// </summary>
        public const string ModName = "Fontifier";
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVer = "1.1.1";
        /// <summary>
        /// Mod schema version.
        /// </summary>
        public const string ModSchemaVer = "1.0.0";
        /// <summary>
        /// Melonloader Version.
        /// </summary>
        public const string MLVersion = "0.7.0";
    }

    /// <summary>
    /// Makes sure it's a valid font name.
    /// </summary>
    public class FontNameValidator : ValidationParameters
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
    public class Fontifier : MelonMod
    {
        #region vars
        private const string ModDesc = "Enter a font from the Font List or leave it empty to use the default font.\n\nMake sure to hit enter!";
        /// <summary>
        /// The logger.
        /// </summary>
        public readonly static MelonLogger.Instance Logger = new("Fontifier", System.Drawing.Color.FromArgb(255, 0, 160, 230));
        /// <summary>
        /// All font names.
        /// </summary>
        public readonly static List<TMP_FontAsset> fonts = new();
        private readonly static RumbleModUIPlus.Mod ModUI = new();
        private readonly static Tags tags = new();
        private readonly static FontNameValidator validator = new();
        /// <summary>
        /// Use DefaultFont instead.
        /// </summary>
        private static TMP_FontAsset _defaultFont;
        /// <summary>
        /// Don't set this.
        /// </summary>
        private static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                        .FirstOrDefault(font => font.name.Equals("GOODDP__ SDF Global Text Material", StringComparison.OrdinalIgnoreCase));
                }
                return _defaultFont;
            }
        }
        #endregion

        #region Helper funcs
        /// <summary>
        /// Gets a font based on its name.
        /// </summary>
        public static TMP_FontAsset FontFromName(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return DefaultFont;

            TMP_FontAsset font = fonts.FirstOrDefault(f => string.Equals(f.name, fontName, StringComparison.OrdinalIgnoreCase));
            if (font == null)
            {
                Logger.Warning($"Font with name {fontName} is not loaded.");
                return DefaultFont;
            }
            return font;
        }

        /// <summary>
        /// Gets a MelonMod based on its name.
        /// </summary>
        public static MelonMod MelonFromName(string melonName) => RegisteredMelons.FirstOrDefault(m => m.Info.Name == melonName);
        #endregion

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
            ModUI.ModFormatVersion = FontifierModInfo.ModSchemaVer;
            ModUI.SetFolder("Fontifier");
            ModUI.AddDescriptionAtStart("Description", "", "Lets you change the font for other mods.", new Tags { IsSummary = true });

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
                        string familyName = new SixLabors.Fonts.FontCollection().Add(fontPath).Name;
                        if (string.IsNullOrWhiteSpace(familyName))
                            throw new Exception("Font has no internal family name.");
                        baseName = familyName;
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
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            ModUI.AddDescriptionAtIndex("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true }, 1);

            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("RUMBLE Tournament Scoring", StringComparison.OrdinalIgnoreCase))
                {
                    FieldInfo scoreboardText = mod.GetType().GetField("scoreboardText", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (scoreboardText == null)
                    {
                        Logger.Warning("RUMBLE Tournament Scoring's scoreboardText FieldInfo is null.");
                        continue;
                    }

                    TournamentScoreboardText = () => (TextMeshPro)scoreboardText.GetValue(mod);

                    TournamentScoringFont = RegisterModWithReference("RUMBLE Tournament Scoring", TournamentScoringChanged);

                    HarmonyInstance.Patch(TournamentScoringPatch.TargetMethod(mod), postfix: TournamentScoringPatch.GetPostfix());
                }
                else if (mod.Info.Name.Equals("MatchInfo", StringComparison.OrdinalIgnoreCase))
                {
                    MatchInfoFont = RegisterModWithReference("MatchInfo", MatchInfoChanged);
                    HarmonyInstance.Patch(MatchInfoPatch.TargetMethod(mod), postfix: MatchInfoPatch.GetPostfix());
                }
            }

            ModUI.GetFromFile();
            UI.instance.AddMod(ModUI);
        }
        #endregion

        #region RUMBLE Tournament Scoring
        private static Func<TMP_FontAsset> TournamentScoringFont;
        private static Func<TextMeshPro> TournamentScoreboardText;

        static class TournamentScoringPatch
        {
            public static MethodBase TargetMethod(MelonMod mod) => mod.GetType().GetMethod("SpawnScoreboard", BindingFlags.Instance | BindingFlags.NonPublic);

            public static void Postfix()
            {
                TextMeshPro scoreboardText = TournamentScoreboardText();
                if (scoreboardText != null)
                    scoreboardText.font = TournamentScoringFont();
            }

            public static HarmonyMethod GetPostfix() => new(typeof(TournamentScoringPatch).GetMethod(nameof(Postfix)));
        }

        private static void TournamentScoringChanged(object sender, EventArgs args)
        {
            TextMeshPro scoreboardText = TournamentScoreboardText();
            if (scoreboardText != null)
                scoreboardText.font = FontFromName(((ValueChange<string>)args)?.Value);
        }
        #endregion

        #region MatchInfo
        private static Func<TMP_FontAsset> MatchInfoFont;
        private static GameObject MatchInfoGameObject;
        private static GameObject MatchInfoGymGameObject;

        static class MatchInfoPatch
        {
            public static MethodBase TargetMethod(MelonMod mod) => mod.GetType().GetMethod("RunInit", BindingFlags.Instance | BindingFlags.NonPublic);

            public static void Postfix(MelonMod __instance)
            {
                Type modType = __instance.GetType();
                TMP_FontAsset font = MatchInfoFont();

                MatchInfoGameObject = (GameObject)modType.GetField("matchInfoGameObject", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (MatchInfoGameObject == null)
                    Logger.Warning("MatchInfo's matchInfoGameObject is null.");
                else
                    foreach (TextMeshPro tmp in MatchInfoGameObject.GetComponentsInChildren<TextMeshPro>(true))
                        tmp.font = font;

                MatchInfoGymGameObject = (GameObject)modType.GetField("gymMatchInfoGameObject", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (MatchInfoGymGameObject == null)
                    Logger.Warning("MatchInfo's gymMatchInfoGameObject is null.");
                else
                {
                    MatchInfoGymGameObject.GetComponent<TextMeshPro>().font = font;
                    foreach (TextMeshPro tmp in MatchInfoGymGameObject.GetComponentsInChildren<TextMeshPro>(true))
                        tmp.font = font;
                }
            }

            public static HarmonyMethod GetPostfix() => new(typeof(MatchInfoPatch).GetMethod(nameof(Postfix)));
        }

        private static void MatchInfoChanged(object sender, EventArgs args)
        {
            TMP_FontAsset font = FontFromName(((ValueChange<string>)args)?.Value);

            if (MatchInfoGameObject != null)
                foreach (TextMeshPro tmp in MatchInfoGameObject.GetComponentsInChildren<TextMeshPro>(true))
                    tmp.font = font;

            if (MatchInfoGymGameObject != null)
            {
                MatchInfoGymGameObject.GetComponent<TextMeshPro>().font = font;
                foreach (TextMeshPro tmp in MatchInfoGymGameObject.GetComponentsInChildren<TextMeshPro>(true))
                    tmp.font = font;
            }
        }
        #endregion

        #region Dev Stuff
        /// <summary>
        /// Register a mod to Fontifier.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>1: Invoke to get the current TMP_FontAsset, 2: Gets a font based on its name</returns>
        public static (Func<TMP_FontAsset>, Func<string, TMP_FontAsset>) RegisterMod(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = ModUI.AddToList(modName, "", ModDesc, tags);
            ModUI.AddValidation(modName, validator);
            setting.CurrentValueChanged += valueChanged;
            return (() => FontFromName((string)setting.Value), FontFromName);
        }

        /// <summary>
        /// Register a mod to Fontifier if you have a reference to the dll.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>Invoke to get the current TMP_FontAsset</returns>
        public static Func<TMP_FontAsset> RegisterModWithReference(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = ModUI.AddToList(modName, "", ModDesc, tags);
            ModUI.AddValidation(modName, validator);
            setting.CurrentValueChanged += valueChanged;
            return () => FontFromName((string)setting.Value);
        }
        #endregion
    }
}
