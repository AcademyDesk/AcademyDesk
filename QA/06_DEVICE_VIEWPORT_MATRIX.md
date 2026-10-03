# Device and viewport matrix

Actual targets are browser-based Platform Owner, Academy Admin, Teacher, and Student/Guardian portals. There is no native Flutter application. Breakpoints in `apps/web/src/app/globals.css` include 400, 480, 640, 760, 860 and 900 pixels, with additional route-specific rules. Test breakpoint edges as well as representative sizes.

| ID | CSS viewport | DPR | Representative target | Required coverage |
| --- | --- | --- | --- | --- |
| VP-SMALL | 320×568 | 2 | Small mobile portrait | Long names, scroll, all actions reachable; no page-wide horizontal overflow |
| VP-MOBILE | 390×844 | 3 | Normal mobile | iOS Safari physical + WebKit emulation; keyboard/safe areas |
| VP-LARGE | 430×932 | 3 | Large mobile | Android Chrome physical + Chromium emulation |
| VP-LANDSCAPE | 844×390 | 3 | Phone landscape | Popover collision, keyboard, short-height forms |
| VP-TABLET | 768×1024 | 2 | Tablet portrait | Sidebar/drawer boundary, two-column forms |
| VP-TABLET-WIDE | 1024×768 | 2 | Tablet landscape | Tables, calendar, modal height |
| VP-LAPTOP | 1366×768 | 1 | Laptop | Mouse/keyboard, nested scrolling |
| VP-DESKTOP | 1440×900 | 1 | Desktop | Full navigation, baseline candidate |
| VP-WIDE | 1920×1080 | 1 | Large desktop | Maximum content width, spacing |

Add widths 399/400/401, 479/480/481, 639/640/641, 759/760/761, 859/860/861 and 899/900/901 for changed responsive components; use pairwise coverage with state/theme, not the full Cartesian product on every page. Browser versions must be recorded at run time; versions are not yet pinned because no browser suite exists.

Run light/dark, 100%/200% browser zoom and enlarged OS text. Keyboard open/closed and rotation tests require physical devices for sign-off. Browser emulation cannot certify mobile keyboard, address-bar resizing, file capture or microphone access. All device execution currently NOT RUN.
