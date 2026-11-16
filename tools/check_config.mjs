// Stage 3 config checker (plain Node, no Unity): same rules as the C# BrandingConfig/CharacterRoster tests, plus file checks.
import fs from "node:fs"; import path from "node:path"; import { fileURLToPath } from "node:url";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const res = (p) => path.join(root, "client-unity/Assets/Resources", p);
const read = (p) => JSON.parse(fs.readFileSync(p, "utf8"));
const errors = []; const need = (c, m) => { if (!c) errors.push(m); };

for (const f of ["branding.json", "characters.json", "controls.layout.json"]) {
  const top = path.join(root, "config", f), copy = res("Config/" + f);
  need(fs.existsSync(top) && fs.existsSync(copy), f + ": missing top-level or Resources copy");
  if (fs.existsSync(top) && fs.existsSync(copy)) need(JSON.stringify(read(top)) === JSON.stringify(read(copy)), f + ": Resources copy differs from config/ (copy it over)");
}
const sl = path.join(root, "client-unity/Assets/StreamingAssets/controls.layout.json");
if (fs.existsSync(sl)) need(JSON.stringify(read(sl)) === JSON.stringify(read(path.join(root, "config/controls.layout.json"))), "StreamingAssets controls.layout.json differs");

const b = read(path.join(root, "config/branding.json"));
need(b.nameOptions.length >= 3, "all three supplied name options must be preserved");
for (const [k, list, key] of [["name", b.nameOptions, "id"], ["logo", b.logoOptions, "id"], ["tagline", b.taglineOptions, "id"], ["appIcon", b.appIconOptions, "id"]]) {
  const o = list.find((x) => x[key] === b.selected[k]); need(o, `selected.${k} '${b.selected[k]}' is not an option`);
  if (o && "available" in o) need(o.available !== false, `selected.${k} is unavailable`);
}
for (const i of b.appIconOptions) { const p = path.join(root, "client-unity", i.file); need(fs.existsSync(p), "icon file missing: " + i.file);
  if (fs.existsSync(p)) need(fs.readFileSync(p).subarray(1, 4).toString() === "PNG", "not a PNG: " + i.file); }
need(/^[a-z0-9]+(\.[a-z0-9_]+)+$/.test(b.android.packageId), "bad android.packageId");

const r = read(path.join(root, "config/characters.json"));
const expected = ["01_main_front_reference.png", "02_second_position_reference.png", "04A_two_person_photo_left_player_crop.jpg", "04B_two_person_photo_right_player_crop.jpg", "05_cream_kurta_reference.png", "06_circular_portrait_reference.png"];
need(r.characters.length === 6, "exactly six characters required");
need(new Set(r.characters.map((c) => c.faceTexture)).size === r.characters.length, "face textures must be unique");
need(r.jerseyNumbersApproved === false, "jersey numbers must not be enabled (none supplied)");
r.characters.forEach((c, i) => {
  need(c.order === i + 1, "order " + c.id); need(c.source === expected[i], `character ${i + 1} must use ${expected[i]}`);
  const tex = res(`Characters/${c.faceTexture}.png`); need(fs.existsSync(tex) && fs.readFileSync(tex).subarray(1, 4).toString() === "PNG", "face texture missing/invalid: " + c.faceTexture);
  need(/^#[0-9A-Fa-f]{6}$/.test(c.hairColor) && /^#[0-9A-Fa-f]{6}$/.test(c.skinColor), "colours " + c.id);
  const ref = [path.join(root, "design-references/Handoff_latest_starter/Character_References", c.source)]; need(fs.existsSync(ref[0]), "original photo missing: " + c.source);
});
if (errors.length) { console.error("CONFIG CHECK FAILED:\n - " + errors.join("\n - ")); process.exit(1); }
console.log(`config check OK: ${b.nameOptions.length} name options, ${b.taglineOptions.length} taglines, ${b.appIconOptions.length} icon options, ${r.characters.length} characters`);
