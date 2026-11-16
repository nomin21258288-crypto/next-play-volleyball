// Map/room registry. A new map only needs to register its data; its guide is generated from it.
export const MAP_REQUIRED = ['id', 'nameKey', 'spawn', 'net', 'bounds', 'teammateSpawns', 'opponentSpawns'];
export class MapRegistry {
  constructor() { this.maps = new Map(); }
  register(map) {
    for (const k of MAP_REQUIRED) if (map[k] === undefined) throw new Error(`map ${map.id ?? '?'} missing ${k}`);
    if (this.maps.has(map.id)) throw new Error(`duplicate map ${map.id}`);
    this.maps.set(map.id, map); return this;
  }
  get(id) { return this.maps.get(id); }
  ids() { return [...this.maps.keys()]; }
}
// Guide steps reference world anchors (positions from map data) that the 3D scene renders as markers.
export function buildMapGuide(map) {
  return [
    { id: 'start', key: 'map.start', world: [{ kind: 'marker', at: map.spawn }], highlight: [] },
    { id: 'net', key: 'map.net', world: [{ kind: 'line', at: map.net }, { kind: 'bounds', at: map.bounds }], highlight: [] },
    { id: 'teams', key: 'map.teams', world: [...map.teammateSpawns.map((at) => ({ kind: 'teammate', at })), ...map.opponentSpawns.map((at) => ({ kind: 'opponent', at }))], highlight: [] },
    { id: 'controls', key: 'map.controls', world: [], highlight: ['joystick', 'btn_jump', 'btn_pass', 'btn_set', 'btn_spike', 'btn_block'] },
  ];
}
export const DEFAULT_COURT = { id: 'court_default', nameKey: 'map.court_default.name', spawn: { x: -4.5, z: -1.6 },
  net: { x: 0, halfWidth: 4.5, height: 2.43 }, bounds: { halfLength: 9, halfWidth: 4.5 },
  teammateSpawns: [{ x: -4.5, z: 1.6 }], opponentSpawns: [{ x: 4.5, z: -1.6 }, { x: 4.5, z: 1.6 }] };
