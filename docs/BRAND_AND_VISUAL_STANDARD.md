# AcademyDesk Brand and Visual Standard

This document is the implementation reference for AcademyDesk branding and visual refinement. Product work must use these rules instead of introducing local colours, fonts, or control styles.

## Typography

- **Approved interface font:** Geist Sans, loaded through Next.js and exposed as `--font-geist-sans`.
- Use it for all product UI: navigation, headings, tables, forms, buttons, empty states, portal pages, and Admin workspaces.
- The logo wordmark uses the same family at a heavier weight with tighter letter spacing; it is not a separate font.
- Do not introduce Inter, system-only fonts, serif fonts, or display fonts in product screens.
- Use the mono font only for genuine technical values such as code snippets or identifiers where a monospace treatment is useful.

## Colour system

### Light mode

- Canvas: `#f4f7fb`
- Surface: `#ffffff`
- Input/control surface: `#edf2f8`
- Primary action: `#55c8ef`
- Primary action text: `#07111f`
- Main text: `#172033`

### Dark mode

- Canvas: `#07111f`
- Surface: `#0d1a2b`
- Input/control surface: `#1a2d43`
- Primary action: `#0783bb`
- Primary action text: `#ffffff`
- Main text: `#e7edf7`

The theme toggle must swap the complete token set. No page should hard-code a blue button, background, or text colour that bypasses these tokens.

## Form controls

- Form controls use the shared input surface token so they remain distinct from their containing panel in both themes.
- Inputs, selects, and textareas use an 8px radius, a 42px minimum height, and 10px × 12px inner padding.
- Form text is compact: 0.875rem controls and 0.8rem labels.
- Form sections use a short heading, restrained supporting text, and a clear logical grouping. Do not put unrelated fields in one large panel.
- Primary actions use the shared action token; never use dark default blue for a main action.

## Layout and density

- Use a calm, application-like canvas, not a marketing-page layout.
- Desktop forms use a two-column grid where fields are related and readable; mobile collapses to one column.
- Keep page titles prominent, but keep field labels, supporting text, tables, and controls compact.
- Empty, loading, success, and error states must use the same surface, border, typography, and spacing system.

## Implementation source of truth

The live tokens and shared Admin control rules are in:

- `apps/web/src/app/globals.css`

Before adding a local visual rule, first check whether a shared token or component rule can satisfy the requirement. This preserves a consistent enterprise interface as new modules are built.
