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
        public const string ModVer = "1.1.4";
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
        #region Vars
        private const string ModDesc = "Enter a font from the Font List or leave it empty to use the default font.\n\nMake sure to hit enter!";
        private static MelonLogger.Instance _logger;
        /// <summary>
        /// The logger.
        /// </summary>
        public static MelonLogger.Instance Logger
        {
            get
            {
                if (_logger != null) return _logger;

                Type buildinfo = Type.GetType("MelonLoader.Properties.BuildInfo, MelonLoader") ?? Type.GetType("MelonLoader.BuildInfo, MelonLoader");
                if (buildinfo == null)
                {
                    MelonLogger.Instance _logger = new("Fontifier");
                    _logger.Error("Could not find MelonLoader.BuildInfo or MelonLoader.Properties.BuildInfo.");
                    return _logger;
                }
                object version = buildinfo.GetProperty("VersionNumber", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (version == null)
                {
                    MelonLogger.Instance _logger = new("Fontifier");
                    _logger.Error("Could not get MelonLoader version.");
                    return _logger;
                }

                if (Semver.SemVersion.Equals(Semver.SemVersion.Parse("0.7.0"), (Semver.SemVersion)version))
                {
                    dynamic color = Type.GetType("System.Drawing.Color, System.Drawing.Common")?.GetMethod(
                        "FromArgb",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) },
                        null
                    )?.Invoke(null, new object[] { 255, 0, 160, 230 });
                    if (color != null)
                    {
                        _logger = new("Fontifier", color);
                        return _logger;
                    }
                    else
                    {
                        MelonLogger.Instance _logger = new("Fontifier");
                        _logger.Error("Detected MelonLoader 0.7.0 but couldn't use System.Drawing.Common");
                        return _logger;
                    }
                }
                else
                {
                    dynamic color = Type.GetType("MelonLoader.Logging.ColorARGB, MelonLoader").GetMethod(
                        "FromArgb",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new Type[] { typeof(byte), typeof(byte), typeof(byte), typeof(byte) },
                        null
                    )?.Invoke(null, new object[] { (byte)255, (byte)0, (byte)160, (byte)230 });
                    if (color != null)
                    {
                        _logger = new("Fontifier", color);
                        return _logger;
                    }
                    else
                    {
                        MelonLogger.Instance _logger = new("Fontifier");
                        _logger.Error("Detected MelonLoader 0.7.1+ but couldn't use MelonLoader.Logging.ColorARGB");
                        return _logger;
                    }
                }
            }
        }
        /// <summary>
        /// All font names.
        /// </summary>
        public readonly static List<TMP_FontAsset> fonts = new();
        private readonly static RumbleModUIPlus.Mod ModUI = new();
        private readonly static Tags tags = new();
        private readonly static FontNameValidator validator = new();
        private static TMP_FontAsset DefaultFont;
        private static readonly Dictionary<string, Dictionary<string, TMP_FontAsset>> modFontCache = new();
        #endregion

        #region Helper funcs
        /// <summary>
        /// Gets a font based on its name.
        /// </summary>
        /// <param name="fontName">The name of the font</param>
        [Obsolete("Can caused unintended side effects. Use FontFromNameCopy if possible.")]
        public static TMP_FontAsset FontFromName(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return DefaultFont;

            TMP_FontAsset font = fonts.FirstOrDefault(f => string.Equals(f.name, fontName, StringComparison.OrdinalIgnoreCase));
            if (font == null)
            {
                Logger.Warning($"Font with name {fontName} is not loaded");
                return DefaultFont;
            }
            return font;
        }

        /// <summary>
        /// Gets a font based on its name and copies it.
        /// </summary>
        /// <param name="modName">The name of your mod</param>
        /// <param name="fontName">The name of the font</param>
        /// <param name="cache">Should the cache be used?<br/>
        /// If true, it will attempt to get the font from the cache, and if there is nothing, it will create and add it.<br/>
        /// If false, it will just create it.</param>
        public static TMP_FontAsset FontFromNameCopy(string modName, string fontName, bool cache)
        {
            if (cache && modFontCache.TryGetValue(modName, out Dictionary<string, TMP_FontAsset> fonts) && fonts.TryGetValue(fontName, out TMP_FontAsset font))
                return font;

            #pragma warning disable CS0618
            TMP_FontAsset currentFont = FontFromName(fontName);
            #pragma warning restore CS0618

            TMP_FontAsset newFont;
            if (!string.IsNullOrWhiteSpace(currentFont.m_SourceFontFilePath) && File.Exists(currentFont.m_SourceFontFilePath))
                newFont = TMP_FontAsset.CreateFontAsset(
                    currentFont.m_SourceFontFilePath,
                    0,
                    90,
                    currentFont.atlasPadding,
                    currentFont.atlasRenderMode,
                    currentFont.atlasWidth,
                    currentFont.atlasHeight,
                    currentFont.atlasPopulationMode,
                    currentFont.isMultiAtlasTexturesEnabled
                );
            else if (currentFont.m_SourceFontFile != null)
                newFont = TMP_FontAsset.CreateFontAsset(
                    currentFont.m_SourceFontFile,
                    0,
                    90,
                    currentFont.atlasPadding,
                    currentFont.atlasRenderMode,
                    currentFont.atlasWidth,
                    currentFont.atlasHeight,
                    currentFont.atlasPopulationMode,
                    currentFont.isMultiAtlasTexturesEnabled
                );
            else
            {
                newFont = TMP_FontAsset.CreateFontAsset(
                    DefaultFont.m_SourceFontFile,
                    0,
                    90,
                    currentFont.atlasPadding,
                    currentFont.atlasRenderMode,
                    currentFont.atlasWidth,
                    currentFont.atlasHeight,
                    currentFont.atlasPopulationMode,
                    currentFont.isMultiAtlasTexturesEnabled
                );

                Logger.BigError($"Font named {currentFont.name} does not have a source.");
            }

            newFont.hideFlags = HideFlags.HideAndDontSave;
            newFont.name = $"[{modName}] {currentFont.name}";

            if (cache)
            {
                if (!modFontCache.ContainsKey(modName))
                    modFontCache[modName] = new Dictionary<string, TMP_FontAsset>();

                modFontCache[modName][fontName] = newFont;
            }

            return newFont;
        }

        /// <inheritdoc/>
        public override void OnInitializeMelon()
        {
            DefaultFont = TMP_FontAsset.CreateFontAsset(
                RumbleModdingAPI.Calls.LoadAssetFromStream<Font>(this, $"{FontifierModInfo.ModName}.gooddogfont", "GOODDP__"),
                0,
                90,
                5,
                GlyphRenderMode.SDFAA,
                1024,
                1024
            );
            DefaultFont.hideFlags = HideFlags.HideAndDontSave;
            DefaultFont.name = "Default - GOODDP__";
        }
        #endregion

        #region MODUI
        /// <inheritdoc/>
        public override void OnLateInitializeMelon() => UI.instance.UI_Initialized += OnUIInitialized;

        private void OnUIInitialized()
        {
            ModUI.ModName = "Fontifier";
            ModUI.ModVersion = FontifierModInfo.ModVer;
            ModUI.ModFormatVersion = FontifierModInfo.ModSchemaVer;
            ModUI.SetFolder("Fontifier");
            ModUI.AddDescriptionAtStart("Description", "", "Lets you change the font for other mods.", new Tags { IsSummary = true });

            #region Load Fonts
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
                        baseName = new SixLabors.Fonts.FontCollection().Add(fontPath).Name;
                        if (string.IsNullOrWhiteSpace(baseName))
                            throw new Exception("Font has no internal family name");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not read internal font name for {fontPath}, using filename instead.", ex);
                        baseName = Path.GetFileNameWithoutExtension(fontPath);
                    }

                    if (string.IsNullOrWhiteSpace(baseName))
                        throw new Exception("Font has an invalid name/filename");

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
            #endregion

            ModUI.AddDescriptionAtIndex("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true }, 1);

            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("RUMBLE Tournament Scoring", StringComparison.OrdinalIgnoreCase))
                {
                    FieldInfo scoreboardText = mod.GetType().GetField("scoreboardText", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (scoreboardText == null)
                    {
                        Logger.Warning("RUMBLE Tournament Scoring's scoreboardText FieldInfo is null");
                        continue;
                    }

                    TournamentScoreboardText = () => (TextMeshPro)scoreboardText.GetValue(mod);

                    TournamentScoringFont = RegisterModWithReferenceCopy("RUMBLE Tournament Scoring", TournamentScoringChanged);

                    HarmonyInstance.Patch(TournamentScoringPatch.TargetMethod(mod), postfix: TournamentScoringPatch.GetPostfix());
                }
                else if (mod.Info.Name.Equals("MatchInfo", StringComparison.OrdinalIgnoreCase))
                {
                    MatchInfoFont = RegisterModWithReferenceCopy("MatchInfo", MatchInfoChanged);
                    HarmonyInstance.Patch(MatchInfoPatch.TargetMethod(mod), postfix: MatchInfoPatch.GetPostfix());
                }
            }

            ModUI.GetFromFile();
            UI.instance.AddMod(ModUI);
        }
        #endregion

        #region RUMBLE Tournament Scoring
        private static Func<bool, TMP_FontAsset> TournamentScoringFont;
        private static Func<TextMeshPro> TournamentScoreboardText;

        static class TournamentScoringPatch
        {
            public static MethodBase TargetMethod(MelonMod mod) => mod.GetType().GetMethod("SpawnScoreboard", BindingFlags.Instance | BindingFlags.NonPublic);

            public static void Postfix()
            {
                TextMeshPro scoreboardText = TournamentScoreboardText();
                if (scoreboardText != null)
                    scoreboardText.font = TournamentScoringFont(true);
            }

            public static HarmonyMethod GetPostfix() => new(typeof(TournamentScoringPatch).GetMethod(nameof(Postfix)));
        }

        private static void TournamentScoringChanged(object sender, EventArgs args)
        {
            TextMeshPro scoreboardText = TournamentScoreboardText();
            if (scoreboardText != null)
                scoreboardText.font = FontFromNameCopy("RUMBLE Tournament Scoring", ((ValueChange<string>)args)?.Value, true);
        }
        #endregion

        #region MatchInfo
        private static Func<bool, TMP_FontAsset> MatchInfoFont;
        private static GameObject MatchInfoGameObject;
        private static GameObject MatchInfoGymGameObject;

        static class MatchInfoPatch
        {
            public static MethodBase TargetMethod(MelonMod mod) => mod.GetType().GetMethod("RunInit", BindingFlags.Instance | BindingFlags.NonPublic);

            public static void Postfix(MelonMod __instance)
            {
                Type modType = __instance.GetType();
                TMP_FontAsset font = MatchInfoFont(true);

                MatchInfoGameObject = (GameObject)modType.GetField("matchInfoGameObject", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (MatchInfoGameObject == null)
                    Logger.Warning("MatchInfo's matchInfoGameObject is null");
                else
                    foreach (TextMeshPro tmp in MatchInfoGameObject.GetComponentsInChildren<TextMeshPro>(true))
                        tmp.font = font;

                MatchInfoGymGameObject = (GameObject)modType.GetField("gymMatchInfoGameObject", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (MatchInfoGymGameObject == null)
                    Logger.Warning("MatchInfo's gymMatchInfoGameObject is null");
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
            TMP_FontAsset font = FontFromNameCopy("MatchInfo", ((ValueChange<string>)args)?.Value, true);

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
        private static ModSetting<string> RegisterModBase(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = ModUI.AddToList(modName, "", ModDesc, tags);
            ModUI.AddValidation(modName, validator);
            setting.CurrentValueChanged += valueChanged;
            return setting;
        }

        /// <summary>
        /// Register a mod to Fontifier.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>1: Invoke to get the current TMP_FontAsset<br/>2: Gets a font based on its name</returns>
        /// <remarks>If possible, use <see cref="RegisterModCopy"/> instead as it is more safe.</remarks>
        [Obsolete("Can caused unintended side effects. Use RegisterModCopy if possible.")]
        public static (Func<TMP_FontAsset>, Func<string, TMP_FontAsset>) RegisterMod(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = RegisterModBase(modName, valueChanged);
            return (() => FontFromName((string)setting.Value), FontFromName);
        }

        /// <summary>
        /// Register a mod to Fontifier and return the versions of the methods that copy.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>1: Invoke to get the current TMP_FontAsset<br/>2: Gets a font based on its name</returns>
        public static (Func<bool, TMP_FontAsset>, Func<string, bool, TMP_FontAsset>) RegisterModCopy(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = RegisterModBase(modName, valueChanged);
            return ((cache) => FontFromNameCopy(modName, (string)setting.Value, cache), (fontName, cache) => FontFromNameCopy(modName, fontName, cache));
        }

        private static ModSetting<string> RegisterModWithReferenceBase(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = ModUI.AddToList(modName, "", ModDesc, tags);
            ModUI.AddValidation(modName, validator);
            setting.CurrentValueChanged += valueChanged;
            return setting;
        }

        /// <summary>
        /// Register a mod to Fontifier if you have a reference to the dll.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>Invoke to get the current TMP_FontAsset</returns>
        /// <remarks>If possible, use <see cref="RegisterModWithReferenceCopy"/> instead as it is more safe.</remarks>
        [Obsolete("Can caused unintended side effects. Use RegisterModWithReferenceCopy if possible.")]
        public static Func<TMP_FontAsset> RegisterModWithReference(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = RegisterModWithReferenceBase(modName, valueChanged);
            return () => FontFromName((string)setting.Value);
        }

        /// <summary>
        /// Register a mod to Fontifier if you have a reference to the dll and return the version of the method that copies.
        /// </summary>
        /// <param name="modName">The name of your MelonMod (this.Info.Name)</param>
        /// <param name="valueChanged">The function that will be ran when the value changes</param>
        /// <returns>Invoke to get the current TMP_FontAsset.<br/>
        /// You must pass in a bool: cache.<br/>
        /// If true, it will attempt to get the font from the cache, and if there is nothing, it will create and add it.<br/>
        /// If false, it will just create it.</returns>
        public static Func<bool, TMP_FontAsset> RegisterModWithReferenceCopy(string modName, EventHandler<EventArgs> valueChanged)
        {
            ModSetting<string> setting = RegisterModWithReferenceBase(modName, valueChanged);
            return (cache) => FontFromNameCopy(modName, (string)setting.Value, cache);
        }
        #endregion
    }
}
