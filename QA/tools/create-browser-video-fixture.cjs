// Tiny synthetic H.264 video for bounded browser playback QA; no customer media.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const { spawnSync } = require('node:child_process');
const encoder = process.argv[2];
if (!encoder || !path.isAbsolute(encoder) || !fs.existsSync(encoder)) throw Error('Provide an existing absolute ffmpeg executable path.');
const root = path.resolve(__dirname, '../../.build-check/browser-media-fixtures');
fs.mkdirSync(root, { recursive: true });
const file = path.join(root, 'synthetic-video.mp4');
if (fs.existsSync(file)) throw Error('Refused to overwrite existing video fixture; reuse after verifying its hash.');
const args = ['-hide_banner', '-n', '-f', 'lavfi', '-i', 'testsrc=size=320x180:rate=24', '-t', '4', '-an', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', file];
const run = spawnSync(encoder, args, { encoding: 'utf8' });
if (run.error || run.status !== 0) throw Error(run.error?.message || run.stderr);
const bytes = fs.readFileSync(file);
console.log(JSON.stringify({ file, length: bytes.length, sha256: crypto.createHash('sha256').update(bytes).digest('hex'), recipe: args, encoderSha256: crypto.createHash('sha256').update(fs.readFileSync(encoder)).digest('hex') }, null, 2));
