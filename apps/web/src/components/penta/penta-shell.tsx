"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { AREAS, NAV_GROUPS } from "./penta-data";

export function PentaShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [navOpen, setNavOpen] = useState(false);
  const [panelOpen, setPanelOpen] = useState(true);
  const askRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        askRef.current?.focus();
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  return (
    <div className="penta-shell">
      <a href="#penta-main" className="sr-only">Skip to main content</a>

      <header className="penta-shell-topbar">
        <button
          type="button"
          className="penta-shell-nav-toggle"
          aria-label={navOpen ? "Close modules menu" : "Open modules menu"}
          aria-expanded={navOpen}
          onClick={() => setNavOpen((open) => !open)}
        >
          <span aria-hidden="true">☰</span>
        </button>

        <Link href="/penta" className="penta-shell-logo">
          <span className="penta-shell-logo-mark" aria-hidden="true">✦</span>
          <span>PENTA</span>
        </Link>

        <form
          className="penta-shell-search"
          role="search"
          onSubmit={(event) => {
            event.preventDefault();
            window.location.href = "/penta/ask";
          }}
        >
          <span aria-hidden="true" className="penta-shell-search-icon">✦</span>
          <input
            ref={askRef}
            type="text"
            placeholder="Ask PENTA or search anything…"
            aria-label="Ask PENTA or search anything"
          />
          <kbd className="penta-shell-kbd">⌘K</kbd>
        </form>

        <nav className="penta-shell-areas" aria-label="PENTA areas">
          {AREAS.map((area) => (
            <Link key={area.key} href={area.href} className="penta-shell-area-tab" title={area.question}>
              {area.label}
              {area.count > 0 && <span className="penta-shell-area-count">{area.count}</span>}
            </Link>
          ))}
        </nav>

        <div className="penta-shell-identity">
          <button type="button" className="penta-shell-branch">Main Branch</button>
          <span className="penta-shell-avatar" aria-hidden="true">AO</span>
        </div>
      </header>

      <div className="penta-shell-body">
        <aside className={navOpen ? "penta-shell-nav is-open" : "penta-shell-nav"} aria-label="Manual modules">
          <p className="penta-shell-nav-eyebrow">Manual modules</p>
          {NAV_GROUPS.map((group) => (
            <div key={group.label} className="penta-shell-nav-group">
              <p className="penta-shell-nav-group-label">{group.label}</p>
              <ul>
                {group.items.map((item) => {
                  const active = pathname === item.href;
                  return (
                    <li key={item.href}>
                      <Link href={item.href} className={active ? "is-active" : undefined}>
                        {item.label}
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </aside>

        <main id="penta-main" className="penta-shell-main">
          {children}
        </main>

        <aside className={panelOpen ? "penta-shell-panel is-open" : "penta-shell-panel"} aria-label="PENTA context panel">
          <button
            type="button"
            className="penta-shell-panel-toggle"
            onClick={() => setPanelOpen((open) => !open)}
            aria-expanded={panelOpen}
          >
            <span aria-hidden="true">✦</span>
            {panelOpen ? "Hide PENTA" : "Show PENTA"}
          </button>
          {panelOpen && (
            <div className="penta-shell-panel-body">
              <p className="penta-shell-panel-eyebrow">PENTA notices</p>
              <div className="penta-shell-panel-card">
                <strong>18 students are overdue on fees</strong>
                <p>Oldest overdue is 41 days. Want a draft reminder?</p>
                <Link href="/penta/inbox" className="penta-secondary-action">Review in Inbox</Link>
              </div>
              <div className="penta-shell-panel-card">
                <strong>Tomorrow has a schedule conflict</strong>
                <p>3 classes share one teacher at an overlapping time.</p>
                <Link href="/penta/pulse" className="penta-secondary-action">See the conflict</Link>
              </div>
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}
