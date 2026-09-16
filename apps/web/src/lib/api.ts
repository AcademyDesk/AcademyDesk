export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export function apiHeaders(json = false): Record<string, string> {
  const token = typeof window === "undefined"
    ? null
    : window.localStorage.getItem("academydesk.accessToken");

  return {
    ...(json ? { "Content-Type": "application/json" } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

export function academyApi(path: string, init: RequestInit = {}) {
  return fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { ...apiHeaders(), ...init.headers },
  });
}
