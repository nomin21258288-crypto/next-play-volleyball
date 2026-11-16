import test from 'node:test'; import assert from 'node:assert/strict';
import { newBall, step, applyHit, newMatch, onServe, onTouch, onLanded, DT } from '../src/index.js';

const run = (b, max = 20000) => { const ev = []; for (let i = 0; i < max && !b.landed; i++) ev.push(...step(b)); return ev; };

test('ball falls under gravity and lands (~sqrt(2h/g))', () => {
  const b = newBall({ x: 5, y: 3, z: 0 }, { x: 0, y: 0, z: 0 });
  let t = 0; while (!b.landed) { step(b); t += DT; }
  assert.ok(Math.abs(t - Math.sqrt(2 * 2.895 / 9.81)) < 0.03);
});
test('deterministic: same inputs give identical landing', () => {
  const mk = () => { const b = newBall({ x: -8, y: 2, z: 1 }, { x: 14, y: 6, z: -1 }); run(b); return b.landed; };
  assert.deepEqual(mk(), mk());
});
test('low ball hits net and does not cross', () => {
  const b = newBall({ x: -1, y: 1.2, z: 0 }, { x: 10, y: 1, z: 0 });
  const ev = run(b); assert.ok(ev.includes('net')); assert.ok(b.landed.x < 0);
});
test('high ball clears net and lands on far side', () => {
  const b = newBall({ x: -6, y: 2.5, z: 0 }, { x: 9, y: 6, z: 0 });
  run(b); assert.ok(b.landed.x > 0);
});
test('full point: serve crosses, lands in court -> server scores and serves', () => {
  const m = newMatch(), b = newBall({ x: -8, y: 2.5, z: 0 }, { x: 0, y: 0, z: 0 });
  onServe(m, 0, 'A1'); applyHit(b, { x: 9, y: 6, z: 0 }); run(b);
  onLanded(m, b.landed);
  assert.deepEqual(m.score, [1, 0]); assert.equal(m.server, 0);
});
test('out ball faults last toucher', () => {
  const m = newMatch(); onServe(m, 0, 'A1'); onLanded(m, { x: 12, z: 0 });
  assert.deepEqual(m.score, [0, 1]); assert.equal(m.server, 1);
});
test('four touches and double touch are faults', () => {
  let m = newMatch(); onServe(m, 0, 'A1'); onTouch(m, 0, 'A2'); onTouch(m, 0, 'A3'); onTouch(m, 0, 'A4');
  assert.deepEqual(m.score, [0, 1]);
  m = newMatch(); onServe(m, 0, 'A1'); onTouch(m, 0, 'A1');
  assert.deepEqual(m.score, [0, 1]);
});
test('set ends at 25 win-by-2; match at 2 sets', () => {
  const m = newMatch();
  // landing at x>0 => team 1's side => team 0 scores
  const win = (t) => { onServe(m, m.server, 'S'); onLanded(m, { x: t === 0 ? 5 : -5, z: 0 }); };
  for (let i = 0; i < 24; i++) { win(0); win(1); }
  win(0); assert.equal(m.sets[0], 0);
  win(0); assert.deepEqual(m.sets, [1, 0]);
  for (let i = 0; i < 26; i++) win(0);
  assert.equal(m.winner, 0);
});
