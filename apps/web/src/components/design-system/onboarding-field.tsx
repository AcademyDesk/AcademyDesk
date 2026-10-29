import type { ReactNode } from "react";
import styles from "./onboarding.module.css";

/** Native fields keep their labels after typing; request names/validation stay host-owned. */
export function OnboardingField({ label, children, wide = false }: { label: string; children: ReactNode; wide?: boolean }) {
  return <label className={`${styles.field} ${wide ? "onboarding-span-all" : ""}`}><span>{label}</span>{children}</label>;
}
