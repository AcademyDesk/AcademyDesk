export const meetingIntegrations = [
  {
    id: "GoogleWorkspace", name: "Google Meet", mark: "G", subtitle: "Google Workspace & Calendar",
    description: "Prepare Google Calendar events with a Google Meet class link.",
    requirements: ["An approved Google OAuth application and secure callback must be configured by Academy Desk.", "An authorized academy administrator signs in with Google and reviews the requested calendar permissions.", "The organizer, calendar and supported meeting settings must be verified before meeting creation is enabled."],
    documentation: "https://developers.google.com/identity/protocols/oauth2/web-server",
  },
  {
    id: "Microsoft365", name: "Microsoft Teams", mark: "M", subtitle: "Microsoft 365 & Entra",
    description: "Prepare Teams meetings through a verified Microsoft 365 connection.",
    requirements: ["An approved Microsoft Entra application and secure callback must be configured by Academy Desk.", "The academy grants the required Microsoft Graph consent; administrator approval may be required.", "The organizer account, Teams eligibility and supported meeting settings must be verified before use."],
    documentation: "https://learn.microsoft.com/en-us/graph/api/application-post-onlinemeetings?view=graph-rest-1.0",
  },
  {
    id: "Zoom", name: "Zoom", mark: "Z", subtitle: "Zoom account connection",
    description: "Prepare scheduled classes through an authorized Zoom account.",
    requirements: ["An approved Zoom OAuth application and secure callback must be configured by Academy Desk.", "An authorized academy administrator signs in with Zoom and reviews the requested permissions.", "The host account, meeting settings and account restrictions must be verified before use."],
    documentation: "https://developers.zoom.us/docs/integrations/oauth/",
  },
] as const;

// This release has no connection handlers. A marker or newer capability shape
// must not accidentally enable OAuth, meeting execution or destructive controls.
export function isUnavailableMeetingCapabilities(value: unknown): value is { asOfUtc: string } {
  if (!value || typeof value !== "object") return false;
  const body = value as Record<string, unknown>;
  if (body.contractVersion !== "meeting-host/0.1" || typeof body.asOfUtc !== "string" || !Number.isFinite(Date.parse(body.asOfUtc)) || !Array.isArray(body.providers) || body.providers.length !== 3) return false;
  const expected = new Set<string>(meetingIntegrations.map((provider) => provider.id));
  return body.providers.every((value: unknown) => {
    if (!value || typeof value !== "object") return false;
    const provider = value as Record<string, unknown>;
    if (typeof provider.provider !== "string" || !expected.delete(provider.provider) || provider.implemented !== false || provider.available !== false || provider.reasonCode !== "IntegrationUnavailable" || !Array.isArray(provider.supportedSettings) || provider.supportedSettings.length !== 0) return false;
    const operations = provider.operations;
    return !!operations && typeof operations === "object" && ["preview", "create", "update", "cancel", "history"].every((key) => (operations as Record<string, unknown>)[key] === false);
  }) && expected.size === 0;
}
