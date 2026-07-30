import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: ["class"],
  content: ["./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Design tokens land here once the Figma/shadcn theme is finalized —
        // see UI.md §"Color palette" for the intended slate + indigo accent system.
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
};

export default config;
