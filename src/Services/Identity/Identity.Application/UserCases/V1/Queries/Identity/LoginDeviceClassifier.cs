namespace Identity.Application.UserCases.V1.Queries.Identity
{
    internal static class LoginDeviceClassifier
    {
        public static string GetCategory(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Computer";

            var isTablet = userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("PlayBook", StringComparison.OrdinalIgnoreCase)
                || (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)
                    && userAgent.Contains("Mobile/", StringComparison.OrdinalIgnoreCase))
                || (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
                    && !userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase));
            var isPhone = userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("Windows Phone", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("IEMobile", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("BlackBerry", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("Opera Mini", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase);

            return isPhone || isTablet ? "Phone" : "Computer";
        }
    }
}
