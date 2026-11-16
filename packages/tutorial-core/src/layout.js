// Single source of truth for on-screen controls. The game HUD AND the tutorial overlay both
// resolve rectangles from this data, so tutorial arrows can never point at a wrong position.
import { readFileSync } from 'node:fs';
export const loadLayout = (path) => JSON.parse(readFileSync(path, 'utf8'));

export function resolve(layout, screen) {            // screen: {w,h,insets:{l,r,t,b}} in dp
  const { w, h, insets = {} } = screen, I = { l: 0, r: 0, t: 0, b: 0, ...insets };
  const out = {};
  for (const c of layout.controls) {
    const [ox, oy] = c.offset, [cw, ch] = c.size, [v, hz] = c.anchor.split('-');
    const x = hz === 'left' ? I.l + ox : w - I.r - ox - cw;
    const y = v === 'top' ? I.t + oy : h - I.b - oy - ch;
    out[c.id] = { x, y, w: cw, h: ch, cx: x + cw / 2, cy: y + ch / 2, action: c.action, type: c.type };
  }
  return out;
}
export function validate(layout, screen, requiredActions) {
  const errs = [], r = resolve(layout, screen), ids = Object.keys(r);
  for (const id of ids) { const a = r[id];
    if (a.x < 0 || a.y < 0 || a.x + a.w > screen.w || a.y + a.h > screen.h) errs.push(`${id} off-screen`);
    if (a.w < 44 || a.h < 44) errs.push(`${id} smaller than 44dp touch target`); }
  for (let i = 0; i < ids.length; i++) for (let j = i + 1; j < ids.length; j++) {
    const a = r[ids[i]], b = r[ids[j]];
    if (a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h) errs.push(`${ids[i]} overlaps ${ids[j]}`); }
  const acts = layout.controls.map((c) => c.action);
  for (const a of requiredActions) if (acts.filter((x) => x === a).length !== 1) errs.push(`action ${a} must map to exactly one control`);
  return errs;
}
