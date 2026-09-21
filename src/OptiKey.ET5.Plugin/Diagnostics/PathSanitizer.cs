using System;

namespace OptiKey.ET5.Plugin.Diagnostics
{
    /// <summary>
    /// Utility for sanitizing local filesystem paths in diagnostics and logs
    /// to prevent leaking local user accounts or profile directories (ADR-004, Mandate 9).
    /// </summary>
    public static class PathSanitizer
    {
        public static string Sanitize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path ?? string.Empty;
            }

            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile) && path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                {
                    return "%USERPROFILE%" + path.Substring(userProfile.Length);
                }
            }
            catch
            {
                // Fall back to original path if environment access fails
            }

            return path;
        }
    }
}
