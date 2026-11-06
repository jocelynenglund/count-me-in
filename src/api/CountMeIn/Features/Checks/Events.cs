namespace CountMeIn.Features.Checks;

// The events of one interest check, as plain records. Property names are the stored payload's
// (camelCase) fields, so streams written before the rules moved here still read back.
// Null fields are not written (an absent email or closing date stays absent).

public sealed record InterestCheckStarted(string CheckId, string Title, string Pitch, int Goal, string? ClosesOn, string OwnerKeyHash);

public sealed record InterestShown(string CheckId, string PersonId, string Person, string Name, string? Email, string On);

public sealed record InterestWithdrawn(string CheckId, string PersonId, string Person, string On);

public sealed record InterestCheckClosed(string CheckId, string On);

public sealed record ThemeChosen(string CheckId, string Theme);
