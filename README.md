# Fontifier
![Photo](https://raw.githubusercontent.com/ninjaguardian/Fontifier/master/Fontifier.png)

[![Github](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/available/github_vector.svg)](https://github.com/ninjaguardian/Fontifier)
[![Changelog](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/documentation/changelog_vector.svg)](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier/changelog)
[![Thunderstore](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/documentation/website_vector.svg)](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier)

## What is this?
This mod lets you change the font of other mods' text. It also allows people to use this in their own mods (see 'For Devs').

## Instructions
1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader)
2. Run RUMBLE without mods
3. Drop Mods from .zip into RUMBLE's installation folder
4. Drop UserData from .zip into RUMBLE's installation folder
5. Drop UserLibs from .zip into RUMBLE's installation folder (Not required for Windows 6.1+)
6. Install dependencies
    - [RumbleModdingAPI](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI)
    - [RumbleModUI](https://thunderstore.io/c/rumble/p/Baumritter/RumbleModUI)
    - [RumbleModUIPlus](https://thunderstore.io/c/rumble/p/ninjaguardian/RumbleModUIPlus)
7. Play RUMBLE!

## Supported mods
| [HealthDisplayWithFont](https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont) | [TournamentScoringMod](https://thunderstore.io/c/rumble/p/davisgreenwell/TournamentScoringMod) | [MatchInfo](https://thunderstore.io/c/rumble/p/UlvakSkillz/MatchInfo) |
|--|--|--|
| [![HealthDisplayWithFont's Icon](https://raw.githubusercontent.com/ninjaguardian/HealthDisplayWithFont/master/icon.png)](https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont) | [![TournamentScoringMod's Icon](https://gcdn.thunderstore.io/live/repository/icons/davisgreenwell-TournamentScoringMod-1.0.1.png)](https://thunderstore.io/c/rumble/p/davisgreenwell/TournamentScoringMod) | [![MatchInfo's Icon](https://gcdn.thunderstore.io/live/repository/icons/UlvakSkillz-MatchInfo-2.4.0.png)](https://thunderstore.io/c/rumble/p/UlvakSkillz/MatchInfo) |

## Choose a font
1. Move any .ttf or .otf fonts into UserData\Fontifier\fonts
2. Press F10 to open Mod UI
3. Go to Fontifier in the dropdown
4. In the second dropdown, select 'Fonts List'
5. Find the name of the font you want
6. In the second dropdown, select any mod
7. Type in the name of the font you want (case-insensitive) and hit enter
8. If it looks good, hit save

## "I can't type in ModUI"
If you have UnityExplorer, hit F7 first. If not, ask the discord.

## "It's not saving"
Make sure to press enter and then hit save. If you are, ask the discord.

## For Devs
<details>
<summary>For Devs</summary>

If you create a TextMeshPro (or similar) in your mod and want to use Fontifier with it, here's how to do it.

First, choose if you want Fontifier to be a required dependency or optional dependency.

<details>
<summary>Required</summary>

- <details><summary>You will need the following usings:</summary>

    ```c#
    using Il2CppTMPro;
    using MelonLoader;
    using static Fontifier.Fontifier;
    // The following is needed if ImplicitUsings are disabled
    using System;
    ```
  </details>

- <details><summary>And these dll refrences:</summary>

    - net6
        - MelonLoader.dll
    - Il2CppAssemblies
        - Unity.TextMeshPro.dll
    - Mods
        - Fontifier.dll

  </details>

- <details><summary>And this code if your code will modify the returned font: (safest)</summary>
    Whenever you call a method, it will create a new instance of the font unless you specify cache.

    Caching will make it so that when you call those methods and get a font, if you call it again, it won't make a new one unless your mod has not called this method for that specific font yet. Each mod has its own cache.

    If you want this, wherever it says `[CACHE]`, replace it with true. Otherwise, replace it with false. Caching is recommended.

    (Place this in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<bool, TMP_FontAsset> GetFont;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        GetFont = RegisterModWithReferenceCopy(this.Info.Name, new EventHandler<EventArgs>(FontChanged));
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromNameCopy(this.Info.Name, ((dynamic)args).Value, [CACHE]);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont([CACHE]);`
  </details>

- <details><summary>And this code if your code will only use the font to set the font for text and will not modify it:</summary>
    The returned font, if modified, will modify EVERY MOD'S FONTS. Only use this if needed. The font could be modified in unexpected ways. In most cases, the above is best option because of its safety. This is mostly here for legacy support.

    (Place this in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<TMP_FontAsset> GetFont;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        GetFont = RegisterModWithReference(this.Info.Name, new EventHandler<EventArgs>(FontChanged));
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromName(((dynamic)args).Value);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont();`
  </details>

</details>

<details>
<summary>Optional</summary>

- <details><summary>You will need the following usings:</summary>

    ```c#
    using Il2CppTMPro;
    using MelonLoader;
    using System.Reflection;
    // The following is needed if ImplicitUsings is disabled
    using System;
    ```
  </details>

- <details><summary>And these dll refrences:</summary>

    - net6
        - MelonLoader.dll
    - Il2CppAssemblies
        - Unity.TextMeshPro.dll

  </details>

- <details><summary>And this code if your code will modify the returned font: (safest)</summary>
    Whenever you call a method, it will create a new instance of the font unless you specify cache.

    Caching will make it so that when you call those methods and get a font, if you call it again, it won't make a new one unless your mod has not called this method for that specific font yet. Each mod has its own cache.

    If you want this, wherever it says `[CACHE]`, replace it with true. Otherwise, replace it with false. Caching is recommended.

    (Place this in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<bool, TMP_FontAsset> GetFont;
    private static Func<string, bool, TMP_FontAsset> FontFromName;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        if (FindMelon("Fontifier", "ninjaguardian")?.GetType() is Type fontifierType && fontifierType != null) (GetFont, FontFromName) = ((Func<bool, TMP_FontAsset>, Func<string, bool, TMP_FontAsset>))fontifierType.GetMethod("RegisterModCopy", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { this.Info.Name, new EventHandler<EventArgs>(FontChanged) });
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromName(((dynamic)args).Value, [CACHE]);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont([CACHE]);`
  </details>

- <details><summary>And this code if your code will only use the font to set the font for text and will not modify it:</summary>
    The returned font, if modified, will modify EVERY MOD'S FONTS. Only use this if needed. The font could be modified in unexpected ways. In most cases, the above is best option because of its safety. This is mostly here for legacy support.

    (Place this in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<TMP_FontAsset> GetFont;
    private static Func<string, TMP_FontAsset> FontFromName;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        if (FindMelon("Fontifier", "ninjaguardian")?.GetType() is Type fontifierType && fontifierType != null) (GetFont, FontFromName) = ((Func<TMP_FontAsset>, Func<string, TMP_FontAsset>))fontifierType.GetMethod("RegisterMod", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { this.Info.Name, new EventHandler<EventArgs>(FontChanged) });
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromName(((dynamic)args).Value);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont();`
  </details>
</details>
</details>

## Help And Other Resources
Get help and find other resources in the [Modding Discord](https://discord.gg/fsbcnZgzfa)

## Included font credit
- [SDRAWKCABMIAY's video](https://youtu.be/wp4VaVm_XpI)
- [File link](https://drive.google.com/drive/folders/1-Wr4TW4FVQ8j8EyKAMHPa-D2Srg05Fyk)

## License

**My code:** [CC0 1.0 Universal](https://github.com/ninjaguardian/Fontifier?tab=CC0-1.0-1-ov-file) (public domain)

[![CC0-1.0 License](https://img.shields.io/badge/License-CC0_1.0_Universal-green.svg)](https://github.com/ninjaguardian/Fontifier?tab=CC0-1.0-1-ov-file)

**Includes SixLabors.Fonts.dll** (© Six Labors, [Apache License 2.0](https://github.com/ninjaguardian/Fontifier?tab=Apache-2.0-3-ov-file))

[![Apache License 2.0](https://img.shields.io/badge/License-Apache_License_2.0-green.svg)](https://github.com/ninjaguardian/Fontifier?tab=Apache-2.0-3-ov-file)

**Includes GoodDog Plain font** (© 1997 Fonthead Design, [Freeware](https://github.com/ninjaguardian/Fontifier?tab=License-2-ov-file))

[![Freeware](https://img.shields.io/badge/License-Freeware-green.svg)](https://github.com/ninjaguardian/Fontifier?tab=License-2-ov-file)