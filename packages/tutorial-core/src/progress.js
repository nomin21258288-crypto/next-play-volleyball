// Per-account, per-tutorial progress. Lives in the player's ACCOUNT (server side), so it survives
// logout/login and reinstall. The client only caches it. Adapters: memory (tests), JSON file (dev).
import { readFileSync, writeFileSync, existsSync } from 'node:fs';

export class MemoryAdapter { constructor() { this.d = {}; } load(a) { return structuredClone(this.d[a] ?? {}); } save(a, rec) { this.d[a] = structuredClone(rec); } }
export class JsonFileAdapter {
  constructor(path) { this.path = path; }
  _all() { return existsSync(this.path) ? JSON.parse(readFileSync(this.path, 'utf8')) : {}; }
  load(a) { return this._all()[a] ?? {}; }
  save(a, rec) { const all = this._all(); all[a] = rec; writeFileSync(this.path, JSON.stringify(all, null, 1)); }
}

export class TutorialProgress {
  constructor(adapter, { now = () => Date.now(), featureReleasedAt = 0 } = {}) { this.ad = adapter; this.now = now; this.release = featureReleasedAt; }
  get(account, id) { return this.ad.load(account)[id] ?? null; }
  _put(account, id, patch) { const rec = this.ad.load(account); rec[id] = { ...(rec[id] ?? {}), ...patch, updatedAt: this.now() }; this.ad.save(account, rec); return rec[id]; }
  saveStep(account, id, step) { return this._put(account, id, { step }); }
  complete(account, id) { return this._put(account, id, { completed: true, skipped: false, completedAt: this.now() }); }
  skip(account, id) { return this._put(account, id, { skipped: true }); }
  reset(account, id) { return this._put(account, id, { completed: false, skipped: false, step: 0 }); } // replay / practice again
  isDone(account, id) { const r = this.get(account, id); return !!(r && (r.completed || r.skipped)); }

  // First-login tutorial: only for accounts created after the feature shipped, and only once.
  shouldAutoLaunchFirstLogin(account, id, { createdAt }) {
    if (createdAt < this.release) return false;       // existing/experienced accounts are never forced
    return !this.isDone(account, id);
  }
  // Map/room guide: once per map, never for experienced players or when hints are disabled.
  shouldShowMapGuide(account, mapId, { matchesPlayed = 0, hintsEnabled = true, experiencedAfter = 20 } = {}) {
    if (!hintsEnabled || matchesPlayed >= experiencedAfter) return false;
    return !this.isDone(account, `map:${mapId}`);
  }
}
