namespace EdSkill.Application.Features.Auth;

public static class SignupIntents
{
    public const string Learn = "learn";
    public const string Teach = "teach";

    public static string Normalize(string? signupIntent)
    {
        return signupIntent?.Trim().ToLowerInvariant() switch
        {
            Teach => Teach,
            _ => Learn
        };
    }

    public static bool IsValid(string? signupIntent)
    {
        return signupIntent?.Trim().ToLowerInvariant() is Learn or Teach;
    }

    public static IReadOnlyCollection<string> GetRoles(string? signupIntent)
    {
        return Normalize(signupIntent) == Teach
            ? ["learner", "companion"]
            : ["learner"];
    }
}
