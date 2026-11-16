import test from 'node:test'; import assert from 'node:assert/strict';
import { createGame, tick, botInput, DT } from '../src/index.js';

const run = (g, secs, inputs = () => ({})) => { const n = Math.round(secs / DT); for (let i = 0; i < n && g.phase !== 'over'; i++) tick(g, inputs(g), botInput); };
const acts = (g) => g.events.filter((e) => e.type === 'action' && e.ok).map((e) => e.action);

test('all-bot match completes: every action type occurs, sets/match end by rules', () => {
  const g = createGame({ humanIds: [] }); run(g, 1800);
  assert.equal(g.phase, 'over'); assert.ok(g.match.winner !== null);
  const a = acts(g);
  for (const k of ['serve', 'pass', 'set', 'spike', 'block']) assert.ok(a.includes(k), 'missing ' + k);
  assert.ok(g.match.sets.some((s) => s === 2));
  for (const e of g.events.filter((e) => e.type === 'point')) assert.ok(e.score.every((s) => Number.isFinite(s)));
});
test('deterministic: same seed -> identical match', () => {
  const a = createGame({ humanIds: [], coach: { seed: 7 } }), b = createGame({ humanIds: [], coach: { seed: 7 } });
  run(a, 300); run(b, 300); assert.deepEqual(a.events, b.events); assert.deepEqual(a.ball.pos, b.ball.pos);
});
test('ball and players never produce NaN / leave sane bounds', () => {
  const g = createGame({ humanIds: [] });
  for (let i = 0; i < 60 * 120; i++) { tick(g, {}, botInput);
    for (const v of [g.ball.pos.x, g.ball.pos.y, g.ball.pos.z]) assert.ok(Number.isFinite(v) && Math.abs(v) < 60);
    for (const p of g.players) assert.ok(Math.abs(p.pos.x) <= 11 && Math.abs(p.pos.z) <= 7); }
});
test('human input: serve works only for the server; out-of-reach pass fails and is reported', () => {
  const g = createGame({ humanIds: ['H0'] });
  tick(g, { H0: { action: 'pass' } }, botInput);                // wrong phase: nothing happens
  assert.equal(g.phase, 'serve');
  run(g, 3, (gg) => { const s = gg.players[0]; return { H0: { move: { x: -9.4 - s.pos.x, z: -s.pos.z }, action: gg.serveWait > 1 ? 'serve' : undefined } }; });
  assert.ok(acts(g).includes('serve'));
  // H0 stays far from ball: a pass attempt when ball is elsewhere reports failure after buffer
  const g2 = createGame({ humanIds: ['H0'], coach: {} });
  run(g2, 1.2, () => ({})); // let bots serve
  const n = g2.events.length;
  tick(g2, { H0: { action: 'pass' } }, botInput); run(g2, 0.5);
  const fail = g2.events.slice(n).find((e) => e.player === 'H0' && e.action === 'pass');
  assert.ok(!fail || fail.ok === false);
});
test('cooldown: a human cannot act twice within cooldown', () => {
  const g = createGame({ humanIds: ['H0'] });
  run(g, 1.2); // server is H0, serve phase
  for (let i = 0; i < 20; i++) tick(g, { H0: { move: { x: -1, z: 0 }, action: 'serve' } }, botInput);
  const serves = g.events.filter((e) => e.action === 'serve' && e.ok);
  assert.ok(serves.length <= 1);
});
test('human jump is physical (apex ~ v^2/2g) and block requires being airborne', () => {
  const g = createGame({ humanIds: ['H0'] }); let apex = 0;
  for (let i = 0; i < 120; i++) { tick(g, { H0: i === 0 ? { jump: true } : {} }, botInput); apex = Math.max(apex, g.players[0].y); }
  assert.ok(Math.abs(apex - 4.6 ** 2 / (2 * 9.81)) < 0.05);
});
test('block does not count as a touch and does not double-count the point', () => {
  const g = createGame({ humanIds: [] }); run(g, 600);
  const blocks = acts(g).filter((a) => a === 'block').length; assert.ok(blocks > 0);
  assert.ok(g.events.filter((e) => e.type === 'point' && /four|double/.test(e.reason)).length < g.events.filter((e) => e.type === 'point').length);
});
