const DEFAULTS = {
  name: "Royal Technology System",
  nameAr: "شركة رويال تكنولوجي سيستم",
  taxNumber: "", commercialReg: "",
  address: "", addressAr: "",
  phone: "", email: "", website: "https://rtegy.com",
  bankName: "", bankAccount: "", bankIban: "",
  vatRate: 14, currency: "EGP", fiscalYearStart: "01-01",
};

export type CompanySettings = typeof DEFAULTS;
export const COMPANY_SETTINGS_KEY = "rts-company-settings";

export function useCompanySettings(): CompanySettings {
  if (typeof window === "undefined") return DEFAULTS;
  try {
    const stored = localStorage.getItem(COMPANY_SETTINGS_KEY);
    return stored ? { ...DEFAULTS, ...JSON.parse(stored) } : DEFAULTS;
  } catch { return DEFAULTS; }
}
