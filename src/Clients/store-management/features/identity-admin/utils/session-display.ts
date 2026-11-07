export interface SessionDeviceLabel {
  browser: string;
  operatingSystem: string;
}

export function getSessionDeviceLabel(userAgent: string | null, unknownLabel: string): SessionDeviceLabel {
  if (!userAgent) return { browser: unknownLabel, operatingSystem: unknownLabel };

  const browser = userAgent.includes("Edg/")
    ? "Microsoft Edge"
    : userAgent.includes("OPR/") || userAgent.includes("Opera")
      ? "Opera"
      : userAgent.includes("Firefox/")
        ? "Mozilla Firefox"
        : userAgent.includes("Chrome/") && !userAgent.includes("Chromium/")
          ? "Google Chrome"
          : userAgent.includes("Safari/") && userAgent.includes("Version/")
            ? "Safari"
            : unknownLabel;

  const operatingSystem = /Windows NT/i.test(userAgent)
    ? "Windows"
    : /iPhone|iPad|iPod/i.test(userAgent)
      ? "iOS"
      : /Android/i.test(userAgent)
        ? "Android"
        : /Mac OS X|Macintosh/i.test(userAgent)
          ? "macOS"
          : /Linux/i.test(userAgent)
            ? "Linux"
            : unknownLabel;

  return { browser, operatingSystem };
}

export function formatSessionDate(value: string, locale: string, unknownLabel: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return unknownLabel;

  return new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
