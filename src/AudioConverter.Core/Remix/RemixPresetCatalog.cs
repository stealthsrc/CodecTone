namespace AudioConverter.Core.Remix;

public static class RemixPresetCatalog
{
    public static IReadOnlyList<RemixPresetDefinition> All { get; } =
    [
        Item(RemixPreset.SlowedReverb, "Slowed + Reverb", RemixPresetCategory.SpeedPitch),
        Item(RemixPreset.DeepSlowed, "Deep Slowed", RemixPresetCategory.SpeedPitch),
        Item(RemixPreset.SpedUpReverb, "Sped Up + Reverb", RemixPresetCategory.SpeedPitch),
        Item(RemixPreset.Nightcore, "Nightcore", RemixPresetCategory.SpeedPitch),
        Item(RemixPreset.HalfTime, "Half-Time", RemixPresetCategory.SpeedPitch),
        Item(RemixPreset.WarmBass, "Warm Bass", RemixPresetCategory.BassPunch),
        Item(RemixPreset.BassBoost, "Bass Boost", RemixPresetCategory.BassPunch),
        Item(RemixPreset.DeepBass, "Deep Bass", RemixPresetCategory.BassPunch),
        Item(RemixPreset.SubFocus, "Sub Focus", RemixPresetCategory.BassPunch),
        Item(RemixPreset.ClubPunch, "Club Punch", RemixPresetCategory.BassPunch),
        Item(RemixPreset.LightRoom, "Light Room", RemixPresetCategory.Atmosphere),
        Item(RemixPreset.DreamyReverb, "Dreamy Reverb", RemixPresetCategory.Atmosphere),
        Item(RemixPreset.WideHall, "Wide Hall", RemixPresetCategory.Atmosphere),
        Item(RemixPreset.AmbientWash, "Ambient Wash", RemixPresetCategory.Atmosphere),
        Item(RemixPreset.EchoSpace, "Echo Space", RemixPresetCategory.Atmosphere),
        Item(RemixPreset.VocalBoost, "Vocal Boost", RemixPresetCategory.VocalClarity),
        Item(RemixPreset.VocalPresence, "Vocal Presence", RemixPresetCategory.VocalClarity),
        Item(RemixPreset.SoftVocal, "Soft Vocal", RemixPresetCategory.VocalClarity),
        Item(RemixPreset.DeMud, "De-Mud", RemixPresetCategory.VocalClarity),
        Item(RemixPreset.ClearMix, "Clear Mix", RemixPresetCategory.VocalClarity),
        Item(RemixPreset.LoFi, "Lo-Fi", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.Telephone, "Telephone", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.Radio, "Radio", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.VintageWarm, "Vintage Warm", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.DarkTone, "Dark Tone", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.BrightTone, "Bright Tone", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.Earrape, "EARRAPE", RemixPresetCategory.ColorTexture),
        Item(RemixPreset.CleanMaster, "Clean Master", RemixPresetCategory.Mastering),
        Item(RemixPreset.LoudMaster, "Loud Master", RemixPresetCategory.Mastering),
        Item(RemixPreset.DynamicMaster, "Dynamic Master", RemixPresetCategory.Mastering),
        Item(RemixPreset.WideMaster, "Wide Master", RemixPresetCategory.Mastering),
        Item(RemixPreset.StreamingMaster, "Streaming -14 LUFS", RemixPresetCategory.Mastering),
        Item(RemixPreset.ClubMaster, "Club Master -10 LUFS", RemixPresetCategory.Mastering),
    ];

    public static IReadOnlyList<RemixPresetDefinition> For(RemixPresetCategory category) =>
        All.Where(item => item.Category == category).ToArray();

    private static RemixPresetDefinition Item(RemixPreset preset, string name, RemixPresetCategory category) =>
        new(preset, name, category);
}
