// Authoritative rally-point rules. Pure state machine: input events -> new state.
// Prototype rules (NOT yet approved as final): rally scoring, sets to 25 win-by-2,
// deciding set to 15, best of 3, max 3 touches/side, no same-player consecutive touch.
import { COURT } from './physics.js';

// ---- Match rules. RANKED = fixed official rules (best of 3, sets to 25, deciding set to 15, win by 2). Never configurable.
// CUSTOM (custom-room creator only) = any whole-number target 7..25 inclusive, win by 2, single game by default.
export const SCORE_LIMITS = Object.freeze({ min: 7, max: 25 });
export const isValidTargetScore = (n) => Number.isInteger(n) && n >= SCORE_LIMITS.min && n <= SCORE_LIMITS.max;

export function makeRules({ mode = 'ranked', targetScore, setsToWin } = {}) {
  if (mode === 'ranked') {
    if (targetScore !== undefined && targetScore !== null) throw new RangeError('ranked matches use fixed official scoring: targetScore is not allowed');
    if (setsToWin !== undefined && setsToWin !== null) throw new RangeError('ranked matches use fixed official scoring: setsToWin is not allowed');
    return Object.freeze({ mode: 'ranked', setsToWin: 2, setTargets: Object.freeze([25, 25, 15]), winBy: 2 });
  }
  if (mode === 'custom') {
    if (!isValidTargetScore(targetScore)) throw new RangeError(`custom targetScore must be a whole number from ${SCORE_LIMITS.min} to ${SCORE_LIMITS.max}, got ${String(targetScore)}`);
    const sets = setsToWin === undefined || setsToWin === null ? 1 : setsToWin;
    if (sets !== 1 && sets !== 2) throw new RangeError('custom setsToWin must be 1 or 2');
    return Object.freeze({ mode: 'custom', setsToWin: sets, setTargets: Object.freeze([targetScore]), winBy: 2 });
  }
  throw new RangeError('unknown match mode ' + String(mode));
}
export const targetFor = (rules, setIndex) => rules.setTargets[Math.min(setIndex, rules.setTargets.length - 1)];

export const newMatch = (rules = makeRules()) => ({
  rules, sets: [0, 0], score: [0, 0], setIndex: 0, server: 0,
  rally: { touches: 0, side: null, lastPlayer: null, lastTeam: null, served: false },
  winner: null, log: [],
});

const target = (m) => targetFor(m.rules, m.setIndex);
const other = (t) => 1 - t;
const sideOfX = (x) => (x < 0 ? 0 : 1);

function award(m, team, reason) {
  m.score[team]++;
  m.log.push({ set: m.setIndex, team, reason, score: [...m.score] });
  const [a, b] = m.score, t = target(m);
  if ((a >= t || b >= t) && Math.abs(a - b) >= m.rules.winBy) {
    m.sets[a > b ? 0 : 1]++;
    m.log.push({ set: m.setIndex, setWinner: a > b ? 0 : 1, score: [a, b] });
    if (m.sets[0] === m.rules.setsToWin || m.sets[1] === m.rules.setsToWin) m.winner = m.sets[0] === m.rules.setsToWin ? 0 : 1;
    else { m.setIndex++; m.score = [0, 0]; }
  }
  m.server = team; // rally point: winner of rally serves
  m.rally = { touches: 0, side: null, lastPlayer: null, lastTeam: null, served: false };
}

// team 0 defends x<0, team 1 defends x>0.
export function onServe(m, team, playerId) {
  if (m.winner !== null || m.rally.served) return;
  if (team !== m.server) return award(m, other(team), 'serve-out-of-turn');
  m.rally = { touches: 1, side: team, lastPlayer: playerId, lastTeam: team, served: true };
}

export function onTouch(m, team, playerId, { block = false } = {}) {
  if (m.winner !== null || !m.rally.served) return;
  const r = m.rally;
  if (block) { // a block does not count as one of the team's three touches
    if (r.lastTeam !== team) { r.touches = 0; r.side = team; }
    r.lastTeam = team; r.lastPlayer = null; return;
  }
  if (r.lastTeam !== team) { r.touches = 0; r.side = team; r.lastPlayer = null; } // ball crossed
  if (r.lastPlayer === playerId && r.touches > 0 && r.lastTeam === team) return award(m, other(team), 'double-touch');
  r.touches++; r.lastPlayer = playerId; r.lastTeam = team;
  if (r.touches > 3) award(m, other(team), 'four-touches');
}

export function onLanded(m, { x, z }) {
  if (m.winner !== null || !m.rally.served) return;
  const out = Math.abs(x) > COURT.halfLength || Math.abs(z) > COURT.halfWidth;
  const landSide = sideOfX(x), last = m.rally.lastTeam;
  if (out) return award(m, other(last), 'out');           // last toucher's team faulted
  award(m, other(landSide), 'ball-in-court');              // landing side loses the rally
  }
