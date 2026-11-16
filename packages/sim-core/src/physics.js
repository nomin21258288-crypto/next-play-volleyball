// Deterministic fixed-step ball physics. No engine, no wall-clock, no Math.random.
// Axes: x = court length (net at x=0), y = up, z = court width. Units: metres, seconds.
export const COURT = { halfLength: 9, halfWidth: 4.5, netHeight: 2.43 };
export const BALL = { radius: 0.105, gravity: 9.81, drag: 0.02, netDamping: 0.3 };
export const DT = 1 / 120;

export const newBall = (pos, vel) => ({ pos: { ...pos }, vel: { ...vel }, landed: null });

// Advance one fixed step. Returns events: 'net' | 'landed'.
export function step(ball) {
  if (ball.landed) return [];
  const ev = [], p = ball.pos, v = ball.vel, prevX = p.x;
  v.y -= BALL.gravity * DT;
  const k = 1 - BALL.drag * DT;
  v.x *= k; v.z *= k;
  p.x += v.x * DT; p.y += v.y * DT; p.z += v.z * DT;
  // Net: ball centre crosses x=0 plane below the net tape (within court width)
  if (prevX * p.x < 0 && p.y - BALL.radius < COURT.netHeight && Math.abs(p.z) <= COURT.halfWidth + 0.5) {
    p.x = prevX; v.x = -v.x * BALL.netDamping; ev.push('net');
  }
  if (p.y <= BALL.radius) {
    p.y = BALL.radius;
    ball.landed = { x: p.x, z: p.z };
    ev.push('landed');
  }
  return ev;
}

// A player contact sets the ball velocity (hit is validated by caller/server).
export function applyHit(ball, vel) { ball.vel = { ...vel }; }
