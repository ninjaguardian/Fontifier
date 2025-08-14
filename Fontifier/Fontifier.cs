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
// TODO: Add stuff so other people can integrate this themselvs in their mod.

[assembly: MelonInfo(typeof(Fontifier.Fontifier), FontifierModInfo.ModName, FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(FontifierModInfo.MLVersion, true)]

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
        public const string ModVer = "1.1.0";
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
            ModUI.AddDescription("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true });

            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("RUMBLE Tournament Scoring", StringComparison.OrdinalIgnoreCase))
                {
                    TournamentScoringFont = ModUI.AddToList("RUMBLE Tournament Scoring", "", ModDesc, new Tags());
                    TournamentScoringFont.CurrentValueChanged += TournamentScoringChanged;
                    ModUI.AddValidation("RUMBLE Tournament Scoring", validator);

                    HarmonyInstance.Patch(TournamentScoringPatch.TargetMethod(), null, TournamentScoringPatch.GetPostfix());
                }
            }

            ModUI.GetFromFile();
            UI.instance.AddMod(ModUI);
        }
        #endregion

        #region RUMBLE Tournament Scoring
        private static ModSetting<string> TournamentScoringFont;

        class TournamentScoringPatch
        {
            public static MethodBase TargetMethod()
            {
                return MelonFromName("RUMBLE Tournament Scoring").GetType().GetMethod("SpawnScoreboard", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            public static void Postfix(MelonMod __instance)
            {
                TournamentScoringChanged(__instance, (string)TournamentScoringFont.Value);
            }

            public static HarmonyMethod GetPostfix()
            {
                return new HarmonyMethod(typeof(TournamentScoringPatch).GetMethod(nameof(Postfix)));
            }
        }

        private static void TournamentScoringChanged(object sender, EventArgs args)
        {
            MelonMod tournamentScoringMod = MelonFromName("RUMBLE Tournament Scoring");
            if (tournamentScoringMod == null)
            {
                Logger.Warning("RUMBLE Tournament Scoring mod not found.");
                return;
            }

            TournamentScoringChanged(tournamentScoringMod, ((ValueChange<string>)args)?.Value);
        }

        private static void TournamentScoringChanged(MelonMod tournamentScoringMod, string fontName)
        {
            if (tournamentScoringMod.GetType()
                .GetField("scoreboardText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(tournamentScoringMod) is TextMeshPro scoreboardText
                && scoreboardText != null)

                scoreboardText.font = FontFromName(fontName);
        }
        #endregion

        /// <summary>
        /// Register a mod to Fontifier.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>Invoke to get the current TMP_FontAsset</returns>
        public static Func<TMP_FontAsset> RegisterMod(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = ModUI.AddToList(modName, "", ModDesc, new Tags());
            ModUI.AddValidation(modName, validator);
            setting.CurrentValueChanged += valueChanged;
            return () => FontFromName((string)setting.Value);
        }
    }
}
