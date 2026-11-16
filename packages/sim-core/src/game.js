// Authoritative local/server match simulation: players, actions, validation, rules glue.
import { COURT, BALL, DT, newBall, step } from './physics.js';
import { newMatch, makeRules, onServe, onTouch, onLanded } from './rules.js';
import { plan } from './plan.js';

// Exactly-rounded hypot (sqrt of sum of squares) so JS and the C# port agree bit-for-bit.
export const hyp = (a, b) => Math.sqrt(a * a + b * b);
export const ACTIONS = ['serve', 'pass', 'set', 'spike', 'block'];
export const CFG = { speed: 5.2, jumpV: 4.6, g: 9.81, reachH: 1.1, reachUp: 2.3, cooldown: 0.3, buffer: 0.3, deadTime: 1.5 };
export const rand = (g) => { g.seed = (Math.imul(g.seed, 1664525) + 1013904223) >>> 0; return g.seed / 4294967296; };
export const sideSign = (team) => (team === 0 ? -1 : 1);       // sign of x on a team's own half
export const BASE = [{ x: 4.5, z: -1.6 }, { x: 4.5, z: 1.6 }];  // |x| per player index (1v1 / 2v2)
// 6v6 formation (|x| from the net, z), front row then back row. 1v1 and 2v2 keep BASE exactly (golden parity).
export const SIX = [{ x: 3.2, z: -3.0 }, { x: 3.2, z: 0 }, { x: 3.2, z: 3.0 }, { x: 6.2, z: -3.0 }, { x: 6.2, z: 0 }, { x: 6.2, z: 3.0 }];
export const SUPPORTED_TEAM_SIZES = [1, 2, 6];
export const slot = (teamSize, team, idx) => { const b = teamSize === 6 ? SIX[idx] : BASE[idx]; return { x: sideSign(team) * b.x, z: b.z }; };
export const basePos = (g, p) => slot(teamSize(g), p.team, p.idx);

export function createGame({ humanIds = ['H0'], playersPerTeam = 2, coach = {}, rules } = {}) {
  if (!SUPPORTED_TEAM_SIZES.includes(playersPerTeam)) throw new RangeError('playersPerTeam must be 1, 2 or 6');
  const players = [];
  for (let team = 0; team < 2; team++) for (let i = 0; i < playersPerTeam; i++) {
    const id = (team === 0 ? 'H' : 'O') + i, human = humanIds.includes(id);
    players.push({ id, team, idx: i, human, pos: slot(playersPerTeam, team, i), y: 0, vy: 0, cd: 0, pending: null });
  }
  const g = { t: 0, players, match: newMatch(makeRules(rules)), ball: newBall({ x: 0, y: 1, z: 0 }, { x: 0, y: 0, z: 0 }), phase: 'serve',
    serveWait: 0, deadT: 0, events: [], logCursor: 0, lastAction: null, serveIdx: [0, 0], coach, prevServer: 0, seed: coach.seed ?? 12345, roll: { block: true, dig: 0, aim: 0 } };
  resetRally(g); return g;
}

export const server = (g) => g.players.find((p) => p.team === g.match.server && p.idx === g.serveIdx[g.match.server] % teamSize(g));
const teamSize = (g) => g.players.filter((p) => p.team === 0).length;
export const mates = (g, p) => g.players.filter((q) => q.team === p.team && q !== p);

function resetRally(g) {
  g.phase = 'serve'; g.serveWait = 0; g.lastAction = null;
  for (const p of g.players) { p.pos = basePos(g, p); p.y = 0; p.vy = 0; p.pending = null; }
  const s = server(g); s.pos = { x: sideSign(s.team) * 9.4, z: 0 };
  g.ball = newBall({ x: s.pos.x, y: 1.1, z: s.pos.z }, { x: 0, y: 0, z: 0 });
}

function syncLog(g) {
  while (g.logCursor < g.match.log.length) {
    const e = g.match.log[g.logCursor++];
    if (e.team !== undefined) g.events.push({ t: g.t, type: 'point', team: e.team, reason: e.reason, score: [...e.score], sets: [...g.match.sets] });
    if (e.setWinner !== undefined) g.events.push({ t: g.t, type: 'set_end', team: e.setWinner, score: e.score });
  }
}

function endPoint(g) {
  if (g.match.winner !== null) { g.phase = 'over'; g.events.push({ t: g.t, type: 'match_over', team: g.match.winner }); }
  else { g.phase = 'dead'; g.deadT = CFG.deadTime; }
}

export const inReach = (g, p) => {
  const b = g.ball.pos;
  return hyp(b.x - p.pos.x, b.z - p.pos.z) <= CFG.reachH && b.y >= 0.1 && b.y <= p.y + CFG.reachUp;
};
const clampSide = (v, team) => (team === 0 ? Math.min(v, -0.3) : Math.max(v, 0.3));

function launch(g, from, target, times, cross) {
  const r = plan(from, target, times, { cross }) || plan(from, target, [...times, 1.8, 2.2, 2.6], { cross, tol: 1.2 });
  return r ? r.v : { x: -Math.sign(from.x) * (cross ? 6 : 0), y: 6, z: 0 };
}

function doAction(g, p, a, aim) {
  const s = sideSign(p.team), opp = -s, m = g.match, ball = g.ball;
  if (g.phase === 'serve') {
    if (a !== 'serve' || p !== server(g)) return 'wrong_phase';
    ball.pos = { x: p.pos.x, y: 2.3, z: p.pos.z };
    const tgt = g.coach.serveTarget && g.coach.serveTarget[p.team] ? g.coach.serveTarget[p.team] : { x: opp * 5.5, z: (aim.z || 0) * 3.2 };
    ball.vel = launch(g, ball.pos, tgt, [0.9, 1.0, 1.1, 1.3, 1.5], true);
    onServe(m, p.team, p.id); g.phase = 'rally'; g.lastAction = 'serve'; g.roll = { block: false, dig: rand(g) < 0.75 ? 0 : (rand(g) < 0.5 ? -2.2 : 2.2), aim: 0 }; return null;
  }
  if (g.phase !== 'rally' || a === 'serve') return 'wrong_phase';
  if (!inReach(g, p)) return 'out_of_reach';
  const bp = ball.pos, touchesBefore = m.rally.lastTeam === p.team ? m.rally.touches : 0;
  if (a === 'spike' && !(p.y > 0.25 && bp.y >= 2.0)) return p.y > 0.25 ? 'ball_too_low' : 'not_airborne';
  if (a === 'block') {
    const incoming = Math.sign(ball.vel.x) === s && m.rally.lastTeam !== p.team;
    if (!(p.y > 0.25 && Math.abs(p.pos.x) <= 1.6 && Math.abs(bp.x) <= 1.3 && bp.y >= 1.8 && incoming)) return 'bad_block';
    onTouch(m, p.team, p.id, { block: true });
    ball.vel = { x: opp * 4.5, y: -2.5, z: ball.vel.z * 0.3 }; g.lastAction = 'block'; return null;
  }
  const logLen = m.log.length;
  onTouch(m, p.team, p.id);
  if (m.log.length > logLen) return 'fault';
  const touches = m.rally.touches, mate = mates(g, p)[0];
  const from = { ...bp }; ball.pos = from;
  if (a === 'spike') {
    const tz = Math.max(-3.8, Math.min(3.8, (aim.z || 0) * 3.8));
    ball.vel = launch(g, from, { x: opp * 5.0, z: tz }, [0.5, 0.55, 0.6, 0.7, 0.8, 1.0, 1.3, 1.6], true);
  } else if (touches >= 3) {                                   // third touch that is not a spike: free ball over
    ball.vel = launch(g, from, { x: opp * 5.5, z: 0 }, [1.2, 1.5, 1.8], true);
  } else if (a === 'pass') {
    const t = g.coach.passTarget && g.coach.passTarget[p.team] ? g.coach.passTarget[p.team]
      : mate ? { x: s * 2.6, z: Math.max(-3, Math.min(3, mate.pos.z)) } : { x: s * 3, z: 0 };
    ball.vel = launch(g, from, t, [1.0, 1.2, 1.4, 1.7], false);
  } else {                                                      // set
    const hit = g.coach.hitter && g.coach.hitter[p.team] ? g.players.find((q) => q.id === g.coach.hitter[p.team]) : mate;
    const t = hit ? { x: s * 1.2, z: Math.max(-3, Math.min(3, hit.pos.z)) } : { x: s * 1.2, z: p.pos.z };
    ball.vel = launch(g, from, t, [1.3, 1.4, 1.5, 1.7], false);
  }
  if (a !== 'block') g.roll = { block: rand(g) < (g.coach.blockChance ?? 0.55), dig: rand(g) < (a === 'spike' ? 0.5 : 0.7) ? 0 : (rand(g) < 0.5 ? -2.2 : 2.2), aim: rand(g) * 2 - 1 };
  g.lastAction = a; return null;
}

export function tick(g, inputs = {}, botFn = null) {
  if (g.phase === 'over') return;
  g.t += DT;
  for (const p of g.players) {
    const inp = (p.human ? inputs[p.id] : botFn && botFn(g, p)) || {};
    p.cd = Math.max(0, p.cd - DT);
    if (inp.move) {
      const L = hyp(inp.move.x, inp.move.z) || 1, k = Math.min(1, L) / L;
      p.pos.x = clampSide(p.pos.x + inp.move.x * k * CFG.speed * DT, p.team);
      p.pos.z += inp.move.z * k * CFG.speed * DT;
      p.pos.x = Math.max(-11, Math.min(11, p.pos.x)); p.pos.z = Math.max(-7, Math.min(7, p.pos.z));
    }
    if (inp.jump && p.y === 0) p.vy = CFG.jumpV;
    if (p.vy !== 0 || p.y > 0) { p.vy -= CFG.g * DT; p.y += p.vy * DT; if (p.y <= 0) { p.y = 0; p.vy = 0; } }
    if (inp.action) p.pending = { a: inp.action, aim: inp.aim || {}, t: CFG.buffer };
    if (p.pending) {
      if (p.cd > 0) { p.pending.t -= DT; }
      else {
        const before = g.match.log.length, why = doAction(g, p, p.pending.a, p.pending.aim);
        if (!why) { g.events.push({ t: g.t, type: 'action', player: p.id, team: p.team, action: p.pending.a, ok: true }); p.cd = CFG.cooldown; p.pending = null; }
        else { p.pending.t -= DT; if (why === 'fault') { if (p.human) g.events.push({ t: g.t, type: 'action', player: p.id, team: p.team, action: p.pending.a, ok: false, reason: 'fault' }); p.pending = null; } }
        if (g.match.log.length > before) { syncLog(g); endPoint(g); }
      }
      if (p.pending && p.pending.t <= 0) { if (p.human) g.events.push({ t: g.t, type: 'action', player: p.id, team: p.team, action: p.pending.a, ok: false }); p.pending = null; }
    }
  }
  if (g.phase === 'serve') { g.serveWait += DT; const s = server(g); g.ball.pos = { x: s.pos.x, y: 1.1, z: s.pos.z }; }
  if (g.phase === 'rally') {
    const ev = step(g.ball);
    if (ev.includes('net')) g.events.push({ t: g.t, type: 'net' });
    if (g.ball.landed) { onLanded(g.match, g.ball.landed); syncLog(g); endPoint(g); }
  }
  if (g.phase === 'dead') { g.deadT -= DT; if (g.deadT <= 0) { if (g.match.server !== g.prevServer) { g.serveIdx[g.match.server]++; g.prevServer = g.match.server; } resetRally(g); } }
}

// Reposition for a scripted/training rally (used by the practice tutorial).
export function setupRally(g, { server: team, serveIdx = 0 }) {
  g.match.server = team; g.serveIdx[team] = serveIdx; g.prevServer = team; resetRally(g);
}
