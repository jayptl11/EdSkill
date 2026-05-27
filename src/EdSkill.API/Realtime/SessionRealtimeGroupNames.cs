namespace EdSkill.API.Realtime;

public static class SessionRealtimeGroupNames
{
    public static string User(Guid userId) => $"user:{userId:D}";

    public static string Session(Guid sessionId) => $"session:{sessionId:D}";

    public static IReadOnlyCollection<string> ParticipantUsers(Guid companionId, Guid? learnerId)
    {
        var groups = new List<string> { User(companionId) };
        if (learnerId.HasValue)
        {
            groups.Add(User(learnerId.Value));
        }

        return groups;
    }
}
