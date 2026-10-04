import type { ReactNode } from "react";
import { PentaShell } from "@/components/penta/penta-shell";

export default function PentaLayout({ children }: { children: ReactNode }) {
  return <PentaShell>{children}</PentaShell>;
}
