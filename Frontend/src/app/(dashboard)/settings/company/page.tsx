"use client";
import { useState, useEffect } from "react";
import { useToast } from "@/components/ui/toast";
import { useRoles } from "@/hooks/useRoles";
import { Check, Building2 } from "lucide-react";
import { useCompanySettings, COMPANY_SETTINGS_KEY, type CompanySettings } from "@/hooks/useCompanySettings";

const DEFAULTS: CompanySettings = {
  name: "Royal Technology System", nameAr: "شركة رويال تكنولوجي سيستم",
  taxNumber: "", commercialReg: "", address: "", addressAr: "",
  phone: "", email: "", website: "https://rtegy.com",
  bankName: "", bankAccount: "", bankIban: "",
  vatRate: 14, currency: "EGP", fiscalYearStart: "01-01",
};

export default function CompanySettingsPage() {
  const { toast } = useToast();
  const { isAdmin } = useRoles();
  const [form, setForm] = useState<CompanySettings>(DEFAULTS);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    try {
      const stored = localStorage.getItem(COMPANY_SETTINGS_KEY);
      if (stored) setForm({ ...DEFAULTS, ...JSON.parse(stored) });
    } catch { /* ignore */ }
  }, []);

  const set = (k: keyof CompanySettings, v: string | number) =>
    setForm(f => ({ ...f, [k]: v }));

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    try {
      localStorage.setItem(COMPANY_SETTINGS_KEY, JSON.stringify(form));
      setSaved(true); setTimeout(() => setSaved(false), 2000);
      toast("Company settings saved.", "success");
    } catch { toast("Failed to save.", "error"); }
  };

  const F = ({ label, field, type = "text" }: { label: string; field: keyof CompanySettings; type?: string }) => (
    <div>
      <label className="text-xs text-muted-foreground block mb-1">{label}</label>
      <input type={type} value={String(form[field])}
        onChange={e => set(field, type === "number" ? Number(e.target.value) : e.target.value)}
        disabled={!isAdmin} className="input w-full disabled:opacity-60" />
    </div>
  );

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <div className="flex items-center gap-3">
        <Building2 className="h-6 w-6 text-primary" />
        <div>
          <h1 className="text-2xl font-semibold">Company Settings</h1>
          <p className="text-sm text-muted-foreground">Used in print headers, invoices, and reports.{!isAdmin && " Admin-only."}</p>
        </div>
      </div>

      <form onSubmit={handleSave} className="space-y-5">
        <div className="card p-5 space-y-4">
          <h2 className="font-semibold text-sm">Company Identity</h2>
          <div className="grid grid-cols-2 gap-3">
            <F label="Company Name (EN) *" field="name" />
            <F label="اسم الشركة (AR)" field="nameAr" />
            <F label="Tax Registration Number" field="taxNumber" />
            <F label="Commercial Registration #" field="commercialReg" />
          </div>
        </div>
        <div className="card p-5 space-y-4">
          <h2 className="font-semibold text-sm">Contact</h2>
          <div className="grid grid-cols-2 gap-3">
            <F label="Phone" field="phone" />
            <F label="Email" field="email" type="email" />
            <F label="Website" field="website" />
            <div />
            <div className="col-span-2"><F label="Address (EN)" field="address" /></div>
            <div className="col-span-2"><F label="العنوان (AR)" field="addressAr" /></div>
          </div>
        </div>
        <div className="card p-5 space-y-4">
          <h2 className="font-semibold text-sm">Banking</h2>
          <div className="grid grid-cols-2 gap-3">
            <F label="Bank Name" field="bankName" />
            <F label="Account Number" field="bankAccount" />
            <div className="col-span-2"><F label="IBAN" field="bankIban" /></div>
          </div>
        </div>
        <div className="card p-5 space-y-4">
          <h2 className="font-semibold text-sm">Finance Configuration</h2>
          <div className="grid grid-cols-3 gap-3">
            <F label="Default VAT Rate (%)" field="vatRate" type="number" />
            <div>
              <label className="text-xs text-muted-foreground block mb-1">Base Currency</label>
              <select value={form.currency} onChange={e => set("currency", e.target.value)}
                disabled={!isAdmin} className="input w-full disabled:opacity-60">
                <option value="EGP">EGP — Egyptian Pound</option>
                <option value="USD">USD — US Dollar</option>
                <option value="EUR">EUR — Euro</option>
                <option value="SAR">SAR — Saudi Riyal</option>
                <option value="AED">AED — UAE Dirham</option>
              </select>
            </div>
            <F label="Fiscal Year Start (MM-DD)" field="fiscalYearStart" />
          </div>
        </div>
        {isAdmin && (
          <button type="submit" className={`btn-primary px-6 py-2.5 rounded-lg text-sm flex items-center gap-2 ${saved ? "bg-emerald-600" : ""}`}>
            <Check className="h-4 w-4" />{saved ? "Saved!" : "Save Settings"}
          </button>
        )}
      </form>
    </div>
  );
}
