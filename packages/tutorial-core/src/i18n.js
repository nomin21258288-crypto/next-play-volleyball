import { readFileSync, readdirSync } from 'node:fs';
export const RTL = new Set(['ur', 'ar']);
export function loadLocales(dir) {
  const L = {}; for (const f of readdirSync(dir)) if (f.endsWith('.json')) L[f.slice(0, -5)] = JSON.parse(readFileSync(`${dir}/${f}`, 'utf8'));
  return L;
}
// Adding a language = adding locales/<code>.json; missing keys fall back to English.
export const t = (L, lang, key) => (L[lang] && L[lang][key]) ?? L.en?.[key] ?? key;
