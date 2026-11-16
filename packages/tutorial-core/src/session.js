// Tutorial navigation: Next / Previous / Skip / Practice Again / replay. Persists after every action.
export class TutorialSession {
  constructor({ progress, account, id, steps }) {
    this.p = progress; this.a = account; this.id = id; this.steps = steps;
    const rec = progress.get(account, id);
    this.i = rec && !rec.completed && !rec.skipped ? Math.min(rec.step ?? 0, steps.length - 1) : 0; // resume
    this.finished = false;
  }
  get step() { return this.steps[this.i]; }
  get isFirst() { return this.i === 0; } get isLast() { return this.i === this.steps.length - 1; }
  next() { if (this.isLast) { this.p.complete(this.a, this.id); this.finished = true; return null; } this.i++; this.p.saveStep(this.a, this.id, this.i); return this.step; }
  prev() { if (!this.isFirst) { this.i--; this.p.saveStep(this.a, this.id, this.i); } return this.step; }
  skip() { this.p.skip(this.a, this.id); this.finished = true; }           // skipped != completed; replay stays available
  practiceAgain() { this.p.reset(this.a, this.id); this.i = 0; this.finished = false; return this.step; }
}
// Help / Tutorial from Settings: always allowed, never gated by completion.
export const replay = (progress, account, id, steps) => { progress.reset(account, id); return new TutorialSession({ progress, account, id, steps }); };
