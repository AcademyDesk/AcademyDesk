// Candidate design tokens only; no runtime/model/API version changes.
export const FOUNDATION_VERSION = "0.1.0-fw1.r1" as const;
export const TOKEN_VERSION = "1.0.0-fw1.r1" as const;
export type Theme = "orbit" | "aura" | "prism";
// Aura is the coded dark accent variant of the approved Aurora asset direction.
export const themeNames = { orbit: "Orbit dark", aura: "Aura / Aurora dark", prism: "Prism light" } as const;
export const colors = {
  orbit: { canvas:"#07111f", surface:"#0d1a2b", elevated:"#14263c", control:"#1a2d43", text:"#e7edf7", muted:"#aebed1", border:"#647d99", accent:"#48b7ef", onAccent:"#07111f", selected:"#10354f", disabled:"#95a6bc", success:"#57c99b", warning:"#f2b84c", danger:"#fb8e83", focus:"#7bd3ff" },
  aura: { canvas:"#10121c", surface:"#191d2e", elevated:"#252b42", control:"#30364d", text:"#f0eefc", muted:"#c0bdd7", border:"#86839f", accent:"#c5a3ff", onAccent:"#151020", selected:"#35284d", disabled:"#b4b0c4", success:"#71dbb4", warning:"#f7c874", danger:"#ffa4b4", focus:"#90dfff" },
  prism: { canvas:"#f4f7fb", surface:"#ffffff", elevated:"#edf2f8", control:"#e4ebf4", text:"#172033", muted:"#52637a", border:"#6c7c92", accent:"#0e68b5", onAccent:"#ffffff", selected:"#e5f1fb", disabled:"#59677b", success:"#116846", warning:"#865000", danger:"#b42318", focus:"#075a9e" }
} as const;
export const scale = {
  space: [4,8,12,16,20,24,32,48], body:16, meta:13, title:30, line:1.6,
  target:44, radiusControl:12, radiusCard:18, radiusPanel:22,
  overlay:50, motionMs:140, focusWidth:3
} as const;
