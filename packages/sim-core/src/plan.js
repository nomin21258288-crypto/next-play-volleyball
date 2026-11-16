// Ballistic aiming using the real physics step(), so planned shots obey net/drag/gravity.
import { BALL, COURT, DT, newBall, step } from './physics.js';
const hyp = (a, b) => Math.sqrt(a * a + b * b);

export function simulate(pos, vel, { maxT = 6, stopY = null } = {}) {
  const b = newBall(pos, vel); let t = 0, crossY = null, net = false;
  while (!b.landed && t < maxT) {
    const px = b.pos.x; const ev = step(b); t += DT;
    if (ev.includes('net')) { net = true; break; }
    if (px * b.pos.x < 0) crossY = b.pos.y;
    if (stopY !== null && b.vel.y < 0 && b.pos.y <= stopY) return { x: b.pos.x, z: b.pos.z, y: b.pos.y, t, crossY, net, landed: null };
  }
  return { x: b.pos.x, z: b.pos.z, y: b.pos.y, t, crossY, net, landed: b.landed };
}

// Find a launch velocity landing at target. opts.cross: must clear net; else must stay on own side.
export function plan(from, target, times, { cross = false, clearance = 0.2, tol = 0.5 } = {}) {
  for (const T of times) {
    let vx = (target.x - from.x) / T, vz = (target.z - from.z) / T;
    const vy = (BALL.radius - from.y + 0.5 * BALL.gravity * T * T) / T;
    let r;
    for (let i = 0; i < 6; i++) {
      r = simulate(from, { x: vx, y: vy, z: vz });
      if (r.net || !r.landed) break;
      vx += (target.x - r.landed.x) / r.t; vz += (target.z - r.landed.z) / r.t;
    }
    if (r.net || !r.landed) continue;
    const err = hyp(r.landed.x - target.x, r.landed.z - target.z);
    const crossed = r.crossY !== null;
    if (err > tol) continue;
    if (cross && !(crossed && r.crossY >= COURT.netHeight + BALL.radius + clearance)) continue;
    if (!cross && crossed) continue;
    return { v: { x: vx, y: vy, z: vz }, T };
  }
  return null;
}
