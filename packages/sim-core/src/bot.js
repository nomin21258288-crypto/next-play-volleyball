// Volleyball AI: serve, receive/pass, set, spike, block, reposition. Pure function of game state.
import { simulate } from './plan.js';
import { sideSign, basePos, server, mates, hyp } from './game.js';

const SPEED_STEP = 0.06;
const toward = (p, t) => {
  const dx = t.x - p.pos.x, dz = t.z - p.pos.z, d = hyp(dx, dz);
  return d < SPEED_STEP ? { x: 0, z: 0 } : { x: dx / d * Math.min(1, d / 0.3), z: dz / d * Math.min(1, d / 0.3) };
};

export function botInput(g, p) {
  const s = sideSign(p.team), m = g.match, ball = g.ball, base = basePos(g, p);
  if (g.phase === 'serve') {
    if (p !== server(g)) return { move: toward(p, base) };
    const spot = { x: s * 9.4, z: 0 };
    const at = hyp(p.pos.x - spot.x, p.pos.z - spot.z) < 0.1;
    const aim = (((m.score[0] + m.score[1]) % 3) - 1) * 0.6;
    return at && g.serveWait > 1.0 ? { action: 'serve', aim: { z: aim } } : { move: toward(p, spot) };
  }
  if (g.phase !== 'rally') return { move: toward(p, base) };

  const lastIsMine = m.rally.lastTeam === p.team;
  const touchesMine = lastIsMine ? m.rally.touches : 0;
  const incoming = Math.sign(ball.vel.x) === s;
  const oppAttack = !lastIsMine && g.lastAction === 'spike';
  const team = g.players.filter((q) => q.team === p.team);

  // ---- block: pre-position while the set is in the air, jump when the spike is in flight ----
  const oppSet = !lastIsMine && m.rally.lastTeam !== null && m.rally.touches === 2 && g.lastAction === 'set';
  const noBlock = g.coach.noBlock && g.coach.noBlock[p.team];
  if ((oppAttack && incoming && Math.abs(ball.pos.x) > 0.3 || oppSet) && team.length > 1 && !noBlock) {
    const zAtNet = oppAttack ? ball.pos.z + ball.vel.z * (Math.abs(ball.pos.x) / Math.max(0.1, Math.abs(ball.vel.x)))
      : simulate(ball.pos, ball.vel, { stopY: 2.9 }).z;
    const forced = g.coach.blocker && g.coach.blocker[p.team] && team.find((q) => q.id === g.coach.blocker[p.team]);
    const blocker = forced || team.slice().sort((a, b) => Math.abs(a.pos.z - zAtNet) - Math.abs(b.pos.z - zAtNet) || a.idx - b.idx)[0];
    if (p === blocker) {
      const out = { move: toward(p, { x: s * 0.8, z: Math.max(-4, Math.min(4, zAtNet)) }) };
      if (oppAttack && g.roll.block) {
        const tNet = Math.abs(ball.pos.x) / Math.max(0.1, Math.abs(ball.vel.x));
        if (tNet < 0.3 && p.y === 0) out.jump = true;
        if (p.y > 0.25) out.action = 'block';
      }
      return out;
    }
  }
  // ---- who plays the ball ----
  const wantsY = touchesMine === 2 ? 2.9 : 1.0;
  const pred = simulate(ball.pos, ball.vel, { stopY: wantsY });
  const mySide = Math.sign(pred.x || s) === s || Math.abs(pred.x) < 0.5;
  const canPlay = mySide && (!pred.landed || Math.abs(pred.x) <= 11) && (lastIsMine ? m.rally.touches < 3 : true);
  if (canPlay) {
    const cands = team.filter((q) => !(lastIsMine && m.rally.touches > 0 && m.rally.lastPlayer === q.id));
    const who = cands.slice().sort((a, b) => hyp(a.pos.x - pred.x, a.pos.z - pred.z) - hyp(b.pos.x - pred.x, b.pos.z - pred.z) || a.idx - b.idx)[0];
    if (who === p) {
      const d = hyp(ball.pos.x - p.pos.x, ball.pos.z - p.pos.z);
      const miss = !lastIsMine && !g.coach.noMiss ? g.roll.dig : 0;
      const tgt = { x: Math.max(-11, Math.min(11, pred.x)), z: pred.z + miss };
      const out = { move: toward(p, tgt) };
      if (touchesMine === 2) {
        if (d < 2.2 && ball.pos.y < 3.8 && ball.vel.y < 0.5 && Math.abs(ball.pos.x) <= 3.2 && p.y === 0) out.jump = true;
        out.action = p.y > 0.25 && ball.vel.y <= 0.5 && ball.pos.y >= 2.3 && Math.abs(ball.pos.x) <= 3.0 ? 'spike' : (ball.pos.y < 1.3 && d < 1.0 ? 'pass' : undefined);
        out.aim = { z: g.roll.aim };
      } else if (d < 1.0) out.action = touchesMine === 1 ? 'set' : 'pass';
      return out;
    }
  }
  // supporting positions
  const sup = lastIsMine && touchesMine === 1 ? { x: s * 1.8, z: p.pos.z } : base;
  return { move: toward(p, sup) };
}
