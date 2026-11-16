import test from 'node:test'; import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url'; import { dirname, join } from 'node:path'; import { tmpdir } from 'node:os';
import { loadLayout, resolve, validate } from '../src/layout.js';
import { loadLocales, t, RTL } from '../src/i18n.js';
import { FIRST_LOGIN, FIRST_LOGIN_ID, PRACTICE_STEPS, PRACTICE_CONTROL, stepKeys } from '../src/steps.js';
import { TutorialProgress, MemoryAdapter, JsonFileAdapter } from '../src/progress.js';
import { TutorialSession, replay } from '../src/session.js';
import { MapRegistry, buildMapGuide, DEFAULT_COURT } from '../src/maps.js';
import { createPractice } from '../src/practice.js';
import { ACTIONS, botInput, DT } from '../../sim-core/src/index.js';

const here = dirname(fileURLToPath(import.meta.url));
const layout = loadLayout(join(here, '../../../config/controls.layout.json'));
const L = loadLocales(join(here, '../locales'));
const SCREENS = layout.referenceScreens.map(([w, h]) => ({ w, h, insets: { l: 0, r: 0, t: 0, b: 0 } }))
  .concat([{ w: 732, h: 412, insets: { l: 44, r: 44, t: 0, b: 21 } }]);   // notch + gesture bar

test('layout valid on all reference screens (in bounds, no overlap, >=44dp, one control per game action)', () => {
  for (const s of SCREENS) assert.deepEqual(validate(layout, s, ACTIONS.concat(['jump', 'move'])), [], JSON.stringify(s));
});
test('every real game action has exactly one on-screen control', () => {
  const acts = layout.controls.map((c) => c.action);
  for (const a of ACTIONS) assert.equal(acts.filter((x) => x === a).length, 1);
});
test('every tutorial highlight/arrow targets a control that exists, and matches the action it teaches', () => {
  const ids = new Set(layout.controls.map((c) => c.id));
  for (const s of FIRST_LOGIN) { for (const id of [...s.highlight, ...s.arrows.map((a) => a.to)]) assert.ok(ids.has(id), `${s.id}->${id}`);
    if (['serve', 'pass', 'set', 'spike', 'block', 'jump'].includes(s.id)) assert.ok(s.arrows.some((a) => layout.controls.find((c) => c.id === a.to).action === s.id), s.id); }
  for (const c of Object.values(PRACTICE_CONTROL)) if (c) assert.ok(ids.has(c));
});
test('arrow target rectangles are derived from the layout at runtime (change layout -> arrow moves)', () => {
  const s = SCREENS[1], a = resolve(layout, s).btn_spike, moved = structuredClone(layout);
  moved.controls.find((c) => c.id === 'btn_spike').offset = [300, 104];
  assert.notEqual(resolve(moved, s).btn_spike.x, a.x);
});
test('Urdu and English complete; RTL flagged; unknown language falls back to English; new language is just a file', () => {
  const keys = new Set([...Object.keys(L.en)]);
  for (const s of FIRST_LOGIN) for (const k of stepKeys(s)) assert.ok(L.en[k], k);
  for (const c of layout.controls) assert.ok(L.en[c.labelKey], c.labelKey);
  for (const s of PRACTICE_STEPS) assert.ok(L.en[`practice.${s}.hint`]);
  for (const k of keys) assert.ok(L.ur[k] && L.ur[k].length, 'ur missing ' + k);
  assert.ok(RTL.has('ur')); assert.equal(t(L, 'fr', 'ui.next'), 'Next'); assert.equal(t(L, 'ur', 'ui.next'), 'اگلا');
});
test('first login: new account auto-launches; completed/skipped never re-forced; legacy accounts never forced', () => {
  const p = new TutorialProgress(new MemoryAdapter(), { featureReleasedAt: 1000 });
  assert.equal(p.shouldAutoLaunchFirstLogin('u1', FIRST_LOGIN_ID, { createdAt: 2000 }), true);
  assert.equal(p.shouldAutoLaunchFirstLogin('old', FIRST_LOGIN_ID, { createdAt: 500 }), false);
  p.skip('u1', FIRST_LOGIN_ID); assert.equal(p.shouldAutoLaunchFirstLogin('u1', FIRST_LOGIN_ID, { createdAt: 2000 }), false);
});
test('session: next/prev/skip/practice-again/replay; progress resumes and survives logout/login (file persistence)', () => {
  const f = join(tmpdir(), 'tut_' + process.pid + '.json');
  let p = new TutorialProgress(new JsonFileAdapter(f)), s = new TutorialSession({ progress: p, account: 'u1', id: FIRST_LOGIN_ID, steps: FIRST_LOGIN });
  assert.equal(s.step.id, 'welcome'); s.next(); s.next(); s.prev(); assert.equal(s.step.id, 'move');
  p = new TutorialProgress(new JsonFileAdapter(f));                       // "logout / login": new objects, same account file
  s = new TutorialSession({ progress: p, account: 'u1', id: FIRST_LOGIN_ID, steps: FIRST_LOGIN }); assert.equal(s.step.id, 'move');
  while (!s.finished) s.next(); assert.equal(p.get('u1', FIRST_LOGIN_ID).completed, true);
  const r = replay(p, 'u1', FIRST_LOGIN_ID, FIRST_LOGIN); assert.equal(r.step.id, 'welcome');
  r.skip(); assert.equal(p.get('u1', FIRST_LOGIN_ID).skipped, true); assert.equal(r.practiceAgain().id, 'welcome');
  assert.equal(new TutorialProgress(new JsonFileAdapter(f)).get('u2', FIRST_LOGIN_ID), null);   // other accounts unaffected
});
test('maps: any registered map gets a guide; shown once per map; suppressed for experienced players / hints off', () => {
  const reg = new MapRegistry().register(DEFAULT_COURT).register({ ...DEFAULT_COURT, id: 'beach_future', nameKey: 'map.beach.name', spawn: { x: -3, z: 0 } });
  assert.throws(() => reg.register({ id: 'bad' })); assert.throws(() => reg.register(DEFAULT_COURT));
  for (const id of reg.ids()) { const g = buildMapGuide(reg.get(id)); assert.deepEqual(g.map((x) => x.id), ['start', 'net', 'teams', 'controls']);
    for (const s of g) assert.ok(L.en[`${s.key}.title`]); }
  const p = new TutorialProgress(new MemoryAdapter());
  assert.equal(p.shouldShowMapGuide('u', 'court_default'), true); p.complete('u', 'map:court_default');
  assert.equal(p.shouldShowMapGuide('u', 'court_default'), false);
  assert.equal(p.shouldShowMapGuide('u', 'beach_future'), true);                       // separate completion per map
  assert.equal(p.shouldShowMapGuide('u', 'beach_future', { matchesPlayed: 30 }), false);
  assert.equal(p.shouldShowMapGuide('u', 'beach_future', { hintsEnabled: false }), false);
});
test('practice match: all 7 steps complete from REAL game events (autopilot stands in for the human)', () => {
  const pr = createPractice({ seed: 3 }); let n = 0;
  while (!pr.done() && n++ < 120 * 60 * 15) pr.tick(botInput(pr.game, pr.game.players[0]), botInput);
  assert.deepEqual(pr.completed, ['serve', 'receive', 'pass', 'set', 'spike', 'block', 'rally'], JSON.stringify({ c: pr.completed, step: pr.step, t: pr.game.t }));
  assert.equal(pr.hint().textKey, 'practice.done');
  assert.equal(pr.game.players.filter((p) => p.team === 0 && p.human).length, 1);   // one human + one bot per team
});
test('practice: tapping buttons alone does not complete steps (needs a successful game action)', () => {
  const pr = createPractice({ seed: 3 });
  for (let i = 0; i < 120 * 5; i++) pr.tick({ jump: i % 60 === 0 }, () => ({}));   // bots idle, human only jumps
  assert.equal(pr.completed.length, 0);
});
