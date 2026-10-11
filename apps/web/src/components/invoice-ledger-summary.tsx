type InvoiceLedger = {
  totalAmount: number;
  adjustedAmount: number;
  paidAmount: number;
  balance: number;
  currency: string;
};

// Display the server's ledger projection; never infer payment from a status label.
export function InvoiceLedgerSummary({ invoice }: { invoice: InvoiceLedger }) {
  const money = (amount: number) => new Intl.NumberFormat("en-IN", {
    style: "currency", currency: invoice.currency,
  }).format(amount);
  return <section aria-label="Invoice balance summary">
    {([
      ["Invoice total", invoice.totalAmount],
      ["Approved adjustments", invoice.adjustedAmount],
      ["Collected payments", invoice.paidAmount],
      ["Remaining due", invoice.balance],
    ] as const).map(([label, value]) => <div className="invoice-paper-total" key={label}>
      <span>{label}</span><b>{money(value)}</b>
    </div>)}
  </section>;
}
