// Create Room: validated room settings. Only the creator of a CUSTOM room can pick the target score (7..25, any whole number).
// Ranked rooms always use the fixed official rules and reject any target score.
import { makeRules, SCORE_LIMITS, isValidTargetScore } from './rules.js';
import { createGame } from './game.js';

export const FORMATS = Object.freeze({ '1v1': 1, '2v2': 2, '6v6': 6 });
export const scoreOptions = () => Array.from({ length: SCORE_LIMITS.max - SCORE_LIMITS.min + 1 }, (_, i) => SCORE_LIMITS.min + i);

export function createRoom({ creatorId, format, mode = 'custom', targetScore, setsToWin } = {}) {
  if (!creatorId) throw new TypeError('creatorId is required');
  if (!Object.prototype.hasOwnProperty.call(FORMATS, format)) throw new RangeError('format must be 1v1, 2v2 or 6v6');
  const rules = makeRules({ mode, targetScore, setsToWin });
  return Object.freeze({ creatorId, format, playersPerTeam: FORMATS[format], mode, rules, started: false });
}

// Returns a NEW room. Only the creator, only in a custom room, only before the match has started.
export function setTargetScore(room, actorId, targetScore) {
  if (room.started) throw new Error('the match has already started');
  if (room.mode !== 'custom') throw new Error('ranked rooms use fixed official scoring');
  if (actorId !== room.creatorId) throw new Error('only the room creator can change the target score');
  if (!isValidTargetScore(targetScore)) throw new RangeError(`target score must be a whole number from ${SCORE_LIMITS.min} to ${SCORE_LIMITS.max}`);
  return Object.freeze({ ...room, rules: makeRules({ mode: 'custom', targetScore, setsToWin: room.rules.setsToWin }) });
}

// The room's rules actually drive the simulation (rules and scoreboard come from game.match.rules).
export function startRoomGame(room, { humanIds = ['H0'], coach = {} } = {}) {
  const rules = room.mode === 'ranked' ? { mode: 'ranked' } : { mode: 'custom', targetScore: room.rules.setTargets[0], setsToWin: room.rules.setsToWin };
  return createGame({ humanIds, playersPerTeam: room.playersPerTeam, coach, rules });
}
