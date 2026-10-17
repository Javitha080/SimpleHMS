namespace SimpleHMS
{
    // Identity of the user that authenticated, set once by LoginForm and read
    // anywhere without another DB hit (MainForm header, future role checks).
    public sealed record UserInfo(int UserId, string Username, string UserType)
    {
        public bool IsAdmin => string.Equals(UserType, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    // Static session state for the current application run.
    public static class AppSession
    {
        public static UserInfo? Current { get; private set; }

        public static void Start(UserInfo user) => Current = user;

        public static void Clear() => Current = null;
    }
}
