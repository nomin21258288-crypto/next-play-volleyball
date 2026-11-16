// Tutorial content as data. `highlight`/`arrows[].to` are control ids from config/controls.layout.json.
// `tryIt` = the step also waits for the player to use the REAL control; the HUD reports that action.
export const FIRST_LOGIN_ID = 'first_login';
export const FIRST_LOGIN = [
  { id: 'welcome', key: 'tut.welcome', highlight: [], arrows: [] },
  { id: 'move',  key: 'tut.move',  highlight: ['joystick'], arrows: [{ to: 'joystick', anim: 'drag' }], tryIt: 'move' },
  { id: 'jump',  key: 'tut.jump',  highlight: ['btn_jump'], arrows: [{ to: 'btn_jump', anim: 'tap' }], tryIt: 'jump' },
  { id: 'serve', key: 'tut.serve', highlight: ['btn_serve'], arrows: [{ to: 'btn_serve', anim: 'tap' }], demo: 'serve' },
  { id: 'pass',  key: 'tut.pass',  highlight: ['btn_pass'], arrows: [{ to: 'btn_pass', anim: 'tap' }], demo: 'pass' },
  { id: 'set',   key: 'tut.set',   highlight: ['btn_set'], arrows: [{ to: 'btn_set', anim: 'tap' }], demo: 'set' },
  { id: 'spike', key: 'tut.spike', highlight: ['btn_jump', 'btn_spike'], arrows: [{ to: 'btn_jump', anim: 'tap', order: 1 }, { to: 'btn_spike', anim: 'tap', order: 2 }], demo: 'spike' },
  { id: 'block', key: 'tut.block', highlight: ['btn_jump', 'btn_block'], arrows: [{ to: 'btn_jump', anim: 'tap', order: 1 }, { to: 'btn_block', anim: 'tap', order: 2 }], demo: 'block' },
  { id: 'rules', key: 'tut.rules', highlight: [], arrows: [] },
];
export const stepKeys = (s) => [`${s.key}.title`, `${s.key}.text`];

// Practice match steps (played with the real game; completion comes from real game events).
export const PRACTICE_STEPS = ['serve', 'receive', 'pass', 'set', 'spike', 'block', 'rally'];
export const PRACTICE_CONTROL = { serve: 'btn_serve', receive: 'btn_pass', pass: 'btn_pass', set: 'btn_set', spike: 'btn_spike', block: 'btn_block', rally: null };
