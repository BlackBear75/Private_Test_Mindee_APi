namespace Test_MindeeApi.State;

public class SessionStorage
{
    private readonly Dictionary<long, UserSession> _sessions = new();

    public UserSession GetOrCreate(long userId)
    {
        if (!_sessions.ContainsKey(userId))
            _sessions[userId] = new UserSession();

        return _sessions[userId];
    }

    public void Clear(long userId)
    {
        if (_sessions.ContainsKey(userId))
            _sessions.Remove(userId);
    }
}
