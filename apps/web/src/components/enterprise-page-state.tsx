type EnterprisePageStateProps = {
  tone?: "loading" | "success" | "warning" | "error";
  children: React.ReactNode;
  className?: string;
};

/** A compact, accessible status treatment shared by Admin operational pages. */
export function EnterprisePageState({ tone = "loading", children, className = "" }: EnterprisePageStateProps) {
  return <p className={`enterprise-page-state enterprise-page-state-${tone} ${className}`.trim()} role={tone === "error" ? "alert" : "status"} aria-live="polite">{children}</p>;
}
