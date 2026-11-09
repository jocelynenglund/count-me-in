namespace CountMeIn.Domain;

/// <summary>The looks there are: data, not a fold. Choose Theme checks against it, Look Choices offers it. Keep in step with src/web/lib/themes.mjs.</summary>
public static class Themes
{
    public static readonly IReadOnlySet<string> All = new HashSet<string> { "classic", "christmas" };

    /// <summary>The looks with their labels, in the order they are offered.</summary>
    public static readonly IReadOnlyList<(string Theme, string Label)> Labels = [("classic", "Classic"), ("christmas", "Christmas")];
}
