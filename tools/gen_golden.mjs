// Generates golden fixtures from the JS reference sim. The C# port must reproduce them.
// Run: node tools/gen_golden.mjs   (writes client-unity/Assets/Tests/EditMode/Golden/golden.json)
import { writeFileSync } from 'node:fs';
import { newBall, step, simulate, plan, createGame, tick, botInput, DT } from '../packages/sim-core/src/index.js';

const physics = [];
for (const [pos, vel] of [
  [{ x: 5, y: 3, z: 0 }, { x: 0, y: 0, z: 0 }], [{ x: -8, y: 2, z: 1 }, { x: 14, y: 6, z: -1 }],
  [{ x: -1, y: 1.2, z: 0 }, { x: 10, y: 1, z: 0 }], [{ x: -6, y: 2.5, z: 0 }, { x: 9, y: 6, z: 0 }],
  [{ x: 3, y: 2.8, z: -2 }, { x: -6.2, y: 3.1, z: 1.4 }], [{ x: 0.4, y: 3, z: 1 }, { x: -5, y: -2, z: 0.3 }]]) {
  const b = newBall(pos, vel), ev = []; let n = 0;
  while (!b.landed && n++ < 20000) { for (const e of step(b)) ev.push(e); }
  physics.push({ pos, vel, events: ev, landed: b.landed, steps: n, end: b.pos, endVel: b.vel });
}
const sims = [];
for (const [pos, vel, stopY] of [[{ x: -8, y: 2.5, z: 0 }, { x: 9, y: 6, z: 0 }, null], [{ x: 2, y: 3, z: 0.5 }, { x: -4, y: 2, z: 0 }, 1.0], [{ x: 2, y: 3, z: 0.5 }, { x: -4, y: 2, z: 0 }, 2.9]]) {
  const r = simulate(pos, vel, { stopY }); sims.push({ pos, vel, stopY, result: r });
}
const plans = [];
for (const [from, target, times, cross] of [
  [{ x: -9.4, y: 2.3, z: 0 }, { x: 5.5, z: 0.6 }, [0.9, 1.0, 1.1, 1.3, 1.5], true],
  [{ x: -3, y: 1.0, z: 1 }, { x: -2.6, z: -1.6 }, [1.0, 1.2, 1.4, 1.7], false],
  [{ x: -2.5, y: 2.8, z: 0 }, { x: 5.0, z: 2 }, [0.5, 0.55, 0.6, 0.7, 0.8, 1.0, 1.3, 1.6], true],
  [{ x: -4, y: 1.0, z: 0 }, { x: -1.2, z: 1 }, [1.3, 1.4, 1.5, 1.7], false]]) {
  plans.push({ from, target, times, cross, result: plan(from, target, times, { cross }) });
}
const ser = (g) => g.events.map((e) => { const o = { t: e.t, type: e.type }; for (const k of ['player', 'team', 'action', 'ok', 'reason', 'score', 'sets']) if (e[k] !== undefined) o[k] = e[k]; return o; });
const final = (g) => ({ t: g.t, phase: g.phase, ball: g.ball.pos, ballVel: g.ball.vel, score: g.match.score, sets: g.match.sets, server: g.match.server, winner: g.match.winner,
  players: g.players.map((p) => ({ id: p.id, x: p.pos.x, z: p.pos.z, y: p.y })) });
const traces = [];
for (const [seed, secs] of [[7, 400], [12345, 200], [3, 120]]) {
  const g = createGame({ humanIds: [], coach: { seed } });
  for (let i = 0; i < Math.round(secs / DT) && g.phase !== 'over'; i++) tick(g, {}, botInput);
  traces.push({ seed, ticks: Math.round(secs / DT), events: ser(g), final: final(g) });
}
// scripted human (integer-pattern inputs, no trig) in H0 slot, bots elsewhere
const ACT = ['serve', 'pass', 'set', 'spike', 'block'];
const scripted = (i) => { const inp = { move: { x: (i % 200) < 100 ? 0.8 : -0.8, z: (i % 90) < 45 ? 0.5 : -0.5 } };
  if (i % 300 === 0) inp.jump = true; if (i % 40 === 0) { inp.action = ACT[Math.floor(i / 40) % 5]; inp.aim = { z: (i % 3 - 1) * 0.5 }; } return inp; };
{ const g = createGame({ humanIds: ['H0'], coach: { seed: 5 } }); const N = 120 * 90;
  for (let i = 0; i < N && g.phase !== 'over'; i++) tick(g, { H0: scripted(i) }, botInput);
  traces.push({ seed: 5, scriptedHuman: true, ticks: N, events: ser(g), final: final(g) }); }
const out = { generator: 'node tools/gen_golden.mjs', dt: DT, physics, sims, plans, traces };
writeFileSync(process.argv[2] ?? 'client-unity/Assets/Tests/EditMode/Golden/golden.json', JSON.stringify(out));
console.log('physics', physics.length, 'plans', plans.map((p) => !!p.result), 'trace events', traces.map((t) => t.events.length), 'scores', traces.map((t) => t.final.score + '/' + t.final.sets));
