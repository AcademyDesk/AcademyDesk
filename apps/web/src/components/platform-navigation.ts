/** Presentation destinations only; this registry grants no role or tenant access. */
export const platformLinks = [
  { label: "Overview", icon: "▦", href: "/platform" },
  { label: "Tenant Management", icon: "◫", href: "/platform/control?tab=Tenants", tab: "Tenants" },
  { label: "Tenant Onboarding", icon: "✦", href: "/platform/control?tab=Tenant%20onboarding", tab: "Tenant onboarding" },
  { label: "Tenant Information", icon: "ⓘ", href: "/platform/control?tab=Tenant%20information", tab: "Tenant information" },
  { label: "Academy Admins", icon: "♙", href: "/platform/control?tab=Admins", tab: "Admins" },
  { label: "Billing", icon: "₹", href: "/platform/control?tab=Billing", tab: "Billing" },
  { label: "Support", icon: "?", href: "/platform/control?tab=Support", tab: "Support" },
  { label: "Announcements", icon: "!", href: "/platform/control?tab=Announcements", tab: "Announcements" },
  { label: "Activity logs", icon: "☷", href: "/platform/control?tab=Activity%20logs", tab: "Activity logs" },
  { label: "Settings", icon: "⚙", href: "/platform/control?tab=Settings", tab: "Settings" },
  { label: "Audit & health", icon: "✓", href: "/platform/control?tab=Audit%20%26%20health", tab: "Audit & health" },
] as const;

export const platformControlTabs: readonly string[] = platformLinks.flatMap(
  (item) => "tab" in item ? [item.tab] : [],
);
