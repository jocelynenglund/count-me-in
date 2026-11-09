using Shouldly;
using System.Text.RegularExpressions;

namespace CountMeIn.Tests;

/// <summary>
/// The shape of the API: a feature owns its command or view, its fold and its route. The only shared code is
/// Domain (events, ids) and Store (the event store adapter). These tests scan the source, so a slice that
/// reaches into another slice, or back to a shared fold or router, fails here.
/// </summary>
public class StructureTests
{
    static readonly string Api = FindApi();

    static string FindApi()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "api", "CountMeIn", "Features")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "api", "CountMeIn");
    }

    /// <summary>Every source file under Features/, with the chapter folder it lives in.</summary>
    static List<(string Chapter, string File, string Text)> Slices() =>
        Directory.GetFiles(Path.Combine(Api, "Features"), "*.cs", SearchOption.AllDirectories)
            .Select(f => (Chapter: Path.GetFileName(Path.GetDirectoryName(f))!, File: Path.GetRelativePath(Api, f), Text: File.ReadAllText(f)))
            .ToList();

    [Fact]
    public void A_feature_never_uses_another_feature_namespace()
    {
        var bad = new List<string>();
        foreach (var (chapter, file, text) in Slices())
        {
            foreach (Match m in Regex.Matches(text, @"CountMeIn\.Features\.(\w+)"))
                if (m.Groups[1].Value != chapter) bad.Add($"{file} refers to {m.Value}");
            var ns = Regex.Match(text, @"^namespace (\S+);", RegexOptions.Multiline).Groups[1].Value;
            if (ns != $"CountMeIn.Features.{chapter}") bad.Add($"{file} is in namespace {ns}");
        }
        bad.ShouldBeEmpty();
    }

    [Fact]
    public void A_feature_never_names_a_public_type_of_another_feature()
    {
        var slices = Slices();
        var bad = new List<string>();
        foreach (var owner in slices)
        {
            var declared = Regex.Matches(owner.Text, @"public (?:static |sealed )*(?:class|record) (\w+)").Select(m => m.Groups[1].Value);
            foreach (var type in declared)
                foreach (var other in slices.Where(s => s.Chapter != owner.Chapter))
                    if (Regex.IsMatch(other.Text, $@"\b{type}\b")) bad.Add($"{other.File} uses {type} from {owner.File}");
        }
        bad.ShouldBeEmpty();
    }

    [Fact]
    public void Folds_are_private_to_their_slice()
    {
        // A public type named like a fold, or a public From(history)/Fold(history), is a fold others could pick up.
        var bad = Slices()
            .Where(s => Regex.IsMatch(s.Text, @"public (?:static |sealed )*(?:class|record) (?:CheckState|\w*Facts|\w*Fold\w*)\b")
                     || Regex.IsMatch(s.Text, @"public static \w+ (?:From|Fold)\(IReadOnlyList<object>"))
            .Select(s => s.File);
        bad.ShouldBeEmpty();
    }

    [Fact]
    public void No_shared_fold_or_routing_helper_is_left()
    {
        var shared = new[] { "CheckState", "CheckFacts", "ViewRoutes", "Features.Checks" };
        var bad = Directory.GetFiles(Api, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => shared.Any(File.ReadAllText(f).Contains))
            .Select(f => Path.GetRelativePath(Api, f));
        bad.ShouldBeEmpty();
        Directory.Exists(Path.Combine(Api, "Features", "Checks")).ShouldBeFalse();
    }

    [Fact]
    public void Domain_and_Store_hold_no_feature_and_no_rules()
    {
        var bad = new List<string>();
        foreach (var folder in new[] { "Domain", "Store" })
            foreach (var f in Directory.GetFiles(Path.Combine(Api, folder), "*.cs"))
                if (File.ReadAllText(f).Contains("CountMeIn.Features")) bad.Add(Path.GetRelativePath(Api, f));
        bad.ShouldBeEmpty();
    }
}
