// Practice tutorial match: 1 human + 1 bot per team, driven by the REAL game. Steps complete from real
// game events (never from button taps alone). Coach settings only choose who serves/receives/hits.
import { createGame, tick, setupRally, inReach } from '../../sim-core/src/index.js';
import { PRACTICE_STEPS, PRACTICE_CONTROL } from './steps.js';

const H0 = 'H0', H1 = 'H1';
const SCENARIO = {
  serve:   { server: 0, idx: 0, coach: { noBlock: [true, true], noMiss: true } },
  receive: { server: 1, idx: 0, coach: { serveTarget: { 1: { x: -4.5, z: -1.6 } }, noBlock: [true, true], noMiss: true } },
  pass:    { server: 1, idx: 0, coach: { serveTarget: { 1: { x: -4.5, z: -1.6 } }, noBlock: [true, true], noMiss: true } },
  set:     { server: 1, idx: 0, coach: { serveTarget: { 1: { x: -4.5, z: 1.6 } }, hitter: { 0: H1 }, noBlock: [true, true], noMiss: true } },
  spike:   { server: 1, idx: 0, coach: { serveTarget: { 1: { x: -4.5, z: -1.6 } }, hitter: { 0: H0 }, noBlock: [true, true], noMiss: true } },
  block:   { server: 0, idx: 1, coach: { blocker: { 0: H0 }, noBlock: [false, false], noMiss: true, blockChance: 1 } },
  rally:   { server: 0, idx: 0, coach: { noMiss: false } },
};

export function createPractice({ seed = 1 } = {}) {
  const game = createGame({ humanIds: [H0], playersPerTeam: 2, coach: { seed } });
  const pr = { game, stepIndex: 0, completed: [], attempts: 0, cursor: 0, armed: false, rallyActions: 0, reArmAt: 0, armedAt: 0 };
  Object.defineProperty(pr, 'step', { get: () => PRACTICE_STEPS[pr.stepIndex] ?? null });
  pr.done = () => pr.stepIndex >= PRACTICE_STEPS.length;
  pr.arm = () => { const s = SCENARIO[pr.step]; if (!s) return; Object.assign(game.coach, { serveTarget: undefined, hitter: undefined, blocker: undefined, passTarget: undefined, noBlock: undefined, noMiss: false, blockChance: undefined, ...s.coach });
    setupRally(game, { server: s.server, serveIdx: s.idx }); pr.armed = true; pr.armedAt = game.t; pr.rallyActions = 0; pr.attempts++; };
  // Contextual instruction: show the right control at the right moment (not a wall of text).
  pr.hint = () => {
    if (pr.done()) return { textKey: 'practice.done', control: null, now: false };
    const ctl = PRACTICE_CONTROL[pr.step], h = game.players.find((p) => p.id === H0);
    const now = game.phase === 'rally' && inReach(game, h) ? true : (pr.step === 'serve' && game.phase === 'serve' && game.serveWait > 1);
    return { textKey: `practice.${pr.step}.hint`, control: ctl, now };
  };
  pr.update = () => {
    if (pr.done()) return;
    if (pr.armed && game.phase === 'rally' && game.t - pr.armedAt > 25) { pr.armed = false; pr.reArmAt = game.t; } // stuck/endless training rally -> retry
    if (!pr.armed && game.t >= pr.reArmAt) pr.arm();
    for (; pr.cursor < game.events.length; pr.cursor++) {
      const e = game.events[pr.cursor];
      if (e.type === 'action' && e.ok) { pr.rallyActions++;
        const s = pr.step;
        const hit = (s === 'serve' && e.player === H0 && e.action === 'serve') || (s === 'receive' && e.player === H0 && e.action === 'pass')
          || (s === 'pass' && e.player === H1 && e.action === 'set') || (s === 'set' && e.player === H0 && e.action === 'set')
          || (s === 'spike' && e.player === H0 && e.action === 'spike') || (s === 'block' && e.player === H0 && e.action === 'block');
        if (hit) { pr.completed.push(s); pr.stepIndex++; if (!pr.done() && s !== 'receive') { pr.armed = false; pr.reArmAt = game.t + 2.5; } }
      }
      if (e.type === 'point') {
        if (pr.step === 'rally' && pr.rallyActions >= 6) { pr.completed.push('rally'); pr.stepIndex++; }
        pr.armed = false; pr.reArmAt = game.t + 1.5; pr.rallyActions = 0;
      }
    }
  };
  pr.tick = (humanInput, botFn) => { pr.update(); tick(game, { [H0]: humanInput }, botFn); pr.update(); };
  return pr;
}
