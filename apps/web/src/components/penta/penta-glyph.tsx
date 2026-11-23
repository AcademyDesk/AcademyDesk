// Host presentation glyphs adapted from PENTA chat-visual-preview/src/visuals.tsx.
// Reuses the owner's existing October 2026 concept vocabulary; no new icon package.
export type PentaGlyphName = "pulse" | "chat" | "grid" | "compass" | "twin" | "play" | "panel" | "arrow" | "spark";
const paths: Record<PentaGlyphName, string> = {
  pulse: "M2 12h4l3-8 5 16 3-8h5",
  chat: "M21 11.5a8.5 8.5 0 0 1-8.5 8.5H4l-2 2V11.5a9.5 9.5 0 1 1 19 0Z",
  grid: "M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z",
  compass: "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0ZM16 8l-3 5-5 3 3-5z",
  twin: "M16 21v-3a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v3M9 10a4 4 0 1 0 0-8 4 4 0 0 0 0 8M16 3a4 4 0 0 1 0 7M22 21v-3a4 4 0 0 0-3-4",
  play: "M6 3l15 9-15 9z",
  panel: "M3 4h18v16H3zM15 4v16",
  arrow: "M12 20V4m-6 6 6-6 6 6",
  spark: "M12 2l2.8 7.2L22 12l-7.2 2.8L12 22l-2.8-7.2L2 12l7.2-2.8z",
};
export function PentaGlyph({ name }: { name: PentaGlyphName }) {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}
