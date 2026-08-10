namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Outcome of ensuring a player directory is at <see cref="SaveVersion.Current"/>.</summary>
    public readonly struct SaveEnsureResult
    {
        public bool Success { get; }
        public string Message { get; }
        public int ResolvedVersion { get; }

        private SaveEnsureResult(bool success, string message, int resolvedVersion)
        {
            Success = success;
            Message = message ?? string.Empty;
            ResolvedVersion = resolvedVersion;
        }

        public static SaveEnsureResult Ok(int version) =>
            new SaveEnsureResult(true, string.Empty, version);

        public static SaveEnsureResult Fail(string message) =>
            new SaveEnsureResult(false, message, -1);
    }
}
