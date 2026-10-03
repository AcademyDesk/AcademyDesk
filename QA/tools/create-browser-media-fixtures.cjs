// Generated synthetic binary fixtures only. No customer files or live storage.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../.build-check/browser-media-fixtures');
fs.mkdirSync(root, { recursive: true });
const image = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==', 'base64');
const audio = Buffer.alloc(44 + 16000); // One second of silent 8kHz mono PCM.
audio.write('RIFF', 0); audio.writeUInt32LE(audio.length - 8, 4); audio.write('WAVEfmt ', 8);
audio.writeUInt32LE(16, 16); audio.writeUInt16LE(1, 20); audio.writeUInt16LE(1, 22);
audio.writeUInt32LE(8000, 24); audio.writeUInt32LE(16000, 28); audio.writeUInt16LE(2, 32); audio.writeUInt16LE(16, 34);
audio.write('data', 36); audio.writeUInt32LE(16000, 40);
const resumable = Buffer.alloc(17 * 1024 * 1024, 0x51); // Three chunks; inert deterministic synthetic bytes.
for (const [name, bytes] of [['synthetic-picture.png', image], ['synthetic-audio.wav', audio], ['synthetic-unsupported.bin', Buffer.from('QA synthetic inert unknown format\n')], ['synthetic-resume.bin', resumable]]) {
  const file = path.join(root, name);
  if (fs.existsSync(file) && !fs.readFileSync(file).equals(bytes)) throw Error('Refused to overwrite a different fixture.');
  fs.writeFileSync(file, bytes);
  console.log(JSON.stringify({ file, length: bytes.length, sha256: crypto.createHash('sha256').update(bytes).digest('hex') }));
}
