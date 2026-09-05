namespace AudioConverter.Core.Artwork;

public static class ArtworkPlanner
{
    private static readonly char[] InvalidFileNameCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public static ArtworkPlan Create(IReadOnlyList<ArtworkSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var skipped = new List<ArtworkSkippedSource>();
        var groups = new Dictionary<string, List<ArtworkPlannedAlbum>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            var images = source.Artwork.Where(item => item.IsAttachedPicture).ToArray();
            if (images.Length == 0)
            {
                skipped.Add(new ArtworkSkippedSource(source.Path, "Skipped because no embedded artwork was found."));
                continue;
            }
            var directory = Path.GetDirectoryName(Path.GetFullPath(source.Path));
            var albumFallback = directory is null ? Path.GetFileNameWithoutExtension(source.Path) : Path.GetFileName(directory);
            var artistFallback = directory is null ? albumFallback : Directory.GetParent(directory)?.Name ?? albumFallback;
            var artist = Tag(source.Tags, "album_artist", "albumartist", "artist") ?? artistFallback;
            var album = Tag(source.Tags, "album") ?? albumFallback;
            var key = Normalize(artist) + "\n" + Normalize(album);
            if (!groups.TryGetValue(key, out var candidates)) groups[key] = candidates = [];
            else skipped.Add(new ArtworkSkippedSource(source.Path, "Grouped with other tracks from this album."));
            foreach (var image in images)
                candidates.Add(new ArtworkPlannedAlbum(source.Path, artist, album,
                    SafeName(artist, 88) + " - " + SafeName(album, 88), image));
        }
        var albums = groups.Values.Select(candidates =>
        {
            var ranked = candidates.OrderByDescending(c => c.Artwork.IsFrontCover)
                .ThenByDescending(c => (long)(c.Artwork.Width ?? 0) * (c.Artwork.Height ?? 0)).ToArray();
            return ranked[0] with { Fallbacks = ranked.Skip(1).ToArray() };
        }).ToArray();
        return new ArtworkPlan(albums, skipped);
    }

    public static string OriginalExtension(string codecName) => codecName.ToLowerInvariant() switch
    {
        "mjpeg" or "jpeg" => "jpg",
        "png" => "png",
        "webp" => "webp",
        "bmp" => "bmp",
        "gif" => "gif",
        _ => throw new ArgumentException($"Unsupported embedded artwork codec: {codecName}"),
    };

    private static string? Tag(IReadOnlyDictionary<string, string> tags, params string[] names)
    {
        foreach (var name in names)
        foreach (var pair in tags)
            if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pair.Value)) return pair.Value.Trim();
        return null;
    }

    private static string Normalize(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string SafeName(string value, int maximumLength)
    {
        var characters = Normalize(value).Select(character => character < 32 || InvalidFileNameCharacters.Contains(character) ? '_' : character).ToArray();
        var safe = new string(characters).Trim().TrimEnd('.', ' ');
        if (safe.Length > maximumLength) safe = safe[..maximumLength].TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? "Unknown" : safe;
    }
}
