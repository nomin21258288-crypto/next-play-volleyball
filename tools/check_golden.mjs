// Fails if the committed golden fixtures differ from what the JS reference sim produces now.
import { execFileSync } from 'node:child_process'; import { readFileSync } from 'node:fs'; import { tmpdir } from 'node:os'; import { join } from 'node:path';
const tmp = join(tmpdir(), 'golden_check_' + process.pid + '.json');
execFileSync('node', ['tools/gen_golden.mjs', tmp], { stdio: 'ignore' });
const same = readFileSync(tmp, 'utf8') === readFileSync('client-unity/Assets/Tests/EditMode/Golden/golden.json', 'utf8');
console.log(same ? 'golden fixtures: up to date' : 'golden fixtures: STALE (run node tools/gen_golden.mjs)'); process.exit(same ? 0 : 1);
