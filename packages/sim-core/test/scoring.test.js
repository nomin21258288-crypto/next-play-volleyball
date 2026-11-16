import test from 'node:test'; import assert from 'node:assert/strict';
import { newMatch, makeRules, onServe, createGame, tick, botInput, DT, SCORE_LIMITS, isValidTargetScore,
  createRoom, setTargetScore, startRoomGame, scoreOptions, FORMATS } from '../src/index.js';

// Awards exactly one point to `team` through the real rules code (serve-out-of-turn gives the point to the calling team).
const give = (m, team) => { m.server = 1 - team; onServe(m, team, 'T'); };
const ALL = Array.from({ length: 19 }, (_, i) => 7 + i);

test('every whole number from 7 to 25 is accepted, nothing else is', () => {
  assert.deepEqual(scoreOptions(), ALL); assert.equal(ALL.length, 19);
  for (const n of ALL) { assert.ok(isValidTargetScore(n)); assert.equal(makeRules({ mode: 'custom', targetScore: n }).setTargets[0], n); }
  for (const bad of [6, 26, 0, -1, 100, 7.5, 14.99, NaN, Infinity, '15', null, undefined, true, [], {}]) {
    assert.throws(() => makeRules({ mode: 'custom', targetScore: bad }), RangeError, 'accepted ' + String(bad));
  }
  assert.equal(SCORE_LIMITS.min, 7); assert.equal(SCORE_LIMITS.max, 25);
});

for (const n of ALL) {
  test(`custom target ${n}: match ends exactly at ${n} (nothing earlier), scoreboard and winner follow`, () => {
    const m = newMatch(makeRules({ mode: 'custom', targetScore: n }));
    for (let i = 1; i < n; i++) { give(m, 0); assert.equal(m.winner, null, `ended early at ${i}`); assert.deepEqual(m.score, [i, 0]); }
    give(m, 0);
    assert.equal(m.winner, 0); assert.deepEqual(m.sets, [1, 0]);
    assert.equal(m.log.filter((e) => e.setWinner !== undefined).at(-1).score[0], n);
  });
}

test('win by two applies to the chosen target (7 -> 8:7 continues, 9:7 ends)', () => {
  const m = newMatch(makeRules({ mode: 'custom', targetScore: 7 }));
  for (let i = 0; i < 6; i++) { give(m, 0); give(m, 1); }          // 6:6
  give(m, 0); assert.equal(m.winner, null);                       // 7:6 not enough
  give(m, 1); give(m, 0); assert.equal(m.winner, null);           // 8:7
  give(m, 0); assert.equal(m.winner, 0); assert.deepEqual(m.log.filter((e) => e.setWinner !== undefined).at(-1).score, [9, 7]);
});

test('either team can win a custom game; score never exceeds what was played', () => {
  const m = newMatch(makeRules({ mode: 'custom', targetScore: 11 }));
  for (let i = 0; i < 11; i++) give(m, 1);
  assert.equal(m.winner, 1); assert.deepEqual(m.sets, [0, 1]);
});

test('custom with setsToWin 2 uses the chosen target in every set', () => {
  const m = newMatch(makeRules({ mode: 'custom', targetScore: 9, setsToWin: 2 }));
  for (let i = 0; i < 9; i++) give(m, 0);
  assert.equal(m.winner, null); assert.deepEqual(m.sets, [1, 0]); assert.equal(m.setIndex, 1);
  for (let i = 0; i < 9; i++) give(m, 1); give(m, 0);            // set 2 -> 1; third set also to 9
  assert.deepEqual(m.sets, [1, 1]); assert.equal(m.setIndex, 2);
  for (let i = 0; i < 9; i++) give(m, 0); assert.equal(m.winner, 0);
});

test('RANKED keeps the fixed official rules and refuses any custom score', () => {
  const r = makeRules({ mode: 'ranked' }); assert.deepEqual([...r.setTargets], [25, 25, 15]); assert.equal(r.setsToWin, 2); assert.equal(r.winBy, 2);
  assert.deepEqual(makeRules(), r); assert.deepEqual(newMatch().rules, r);   // default = ranked, so all older code behaves as before
  for (const t of [7, 15, 21, 25]) assert.throws(() => makeRules({ mode: 'ranked', targetScore: t }), RangeError);
  assert.throws(() => makeRules({ mode: 'ranked', setsToWin: 1 }), RangeError);
  const m = newMatch(); for (let i = 0; i < 24; i++) give(m, 0); assert.equal(m.score[0], 24); give(m, 0);
  assert.deepEqual(m.sets, [1, 0]);                                // set 1 ended at 25
  for (let i = 0; i < 25; i++) give(m, 1); assert.deepEqual(m.sets, [1, 1]);
  for (let i = 0; i < 14; i++) give(m, 0); assert.equal(m.winner, null); give(m, 0); assert.equal(m.winner, 0);   // deciding set to 15
});

test('unknown mode is rejected', () => { assert.throws(() => makeRules({ mode: 'casual', targetScore: 15 }), RangeError); });

test('Create Room: all three formats accept every score 7..25', () => {
  for (const [format, n] of Object.entries(FORMATS)) for (const t of ALL) {
    const room = createRoom({ creatorId: 'u1', format, mode: 'custom', targetScore: t });
    assert.equal(room.playersPerTeam, n); assert.equal(room.rules.setTargets[0], t);
  }
  assert.deepEqual(Object.keys(FORMATS), ['1v1', '2v2', '6v6']);
  assert.throws(() => createRoom({ creatorId: 'u1', format: '3v3', targetScore: 10 }), RangeError);
  assert.throws(() => createRoom({ format: '1v1', targetScore: 10 }), TypeError);
  assert.throws(() => createRoom({ creatorId: 'u1', format: '2v2', mode: 'custom' }), RangeError);       // custom must choose a score
});

test('only the creator of a CUSTOM, not-yet-started room can change the score; ranked rooms cannot', () => {
  const room = createRoom({ creatorId: 'host', format: '2v2', mode: 'custom', targetScore: 15 });
  assert.equal(setTargetScore(room, 'host', 7).rules.setTargets[0], 7); assert.equal(setTargetScore(room, 'host', 25).rules.setTargets[0], 25);
  assert.equal(room.rules.setTargets[0], 15);                                                           // original untouched
  assert.throws(() => setTargetScore(room, 'guest', 9), /only the room creator/);
  assert.throws(() => setTargetScore(room, 'host', 26), RangeError); assert.throws(() => setTargetScore(room, 'host', 6), RangeError);
  assert.throws(() => setTargetScore({ ...room, started: true }, 'host', 9), /already started/);
  const ranked = createRoom({ creatorId: 'host', format: '2v2', mode: 'ranked' });
  assert.deepEqual([...ranked.rules.setTargets], [25, 25, 15]);
  assert.throws(() => setTargetScore(ranked, 'host', 9), /fixed official/);
  assert.throws(() => createRoom({ creatorId: 'host', format: '2v2', mode: 'ranked', targetScore: 9 }), RangeError);
});

// ---- the chosen score must drive the REAL simulation (bots play it out), for each format ----
const play = (g, maxSecs) => { for (let i = 0; i < maxSecs / DT && g.phase !== 'over'; i++) tick(g, {}, botInput); };
for (const [format, size] of Object.entries(FORMATS)) for (const t of [7, 12, 25]) {
  test(`${format} custom room to ${t}: bots finish a real match at exactly that score`, () => {
    const g = startRoomGame(createRoom({ creatorId: 'h', format, targetScore: t }), { humanIds: [], coach: { seed: 5 } });
    assert.equal(g.players.length, size * 2); assert.equal(g.match.rules.setTargets[0], t);
    play(g, 3600);
    assert.equal(g.phase, 'over', 'match did not finish'); assert.ok(g.match.winner !== null);
    const end = g.match.log.filter((e) => e.setWinner !== undefined);
    assert.equal(end.length, 1); const [a, b] = end[0].score; const hi = Math.max(a, b), lo = Math.min(a, b);
    assert.ok(hi >= t && hi - lo >= 2); assert.ok(hi === t || lo === hi - 2, `finished at ${a}:${b}`);   // exactly t, or deuce extension by 2
    for (const e of g.events.filter((e) => e.type === 'point')) assert.ok(e.score.every((s) => Number.isFinite(s)));
    assert.ok(g.events.some((e) => e.type === 'match_over'));
  });
}

test('2v2 and 1v1 ranked-default games are unchanged (still official rules, same default)', () => {
  for (const n of [1, 2]) { const g = createGame({ humanIds: [], playersPerTeam: n }); assert.equal(g.match.rules.mode, 'ranked'); assert.deepEqual([...g.match.rules.setTargets], [25, 25, 15]); }
  assert.throws(() => createGame({ playersPerTeam: 3 }), RangeError); assert.throws(() => createGame({ playersPerTeam: 2, rules: { mode: 'ranked', targetScore: 9 } }), RangeError);
});
