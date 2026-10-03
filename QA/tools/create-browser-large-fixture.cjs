// Valid silent PCM WAV just above the inline-preview limit; synthetic only.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../.build-check/browser-media-fixtures');
fs.mkdirSync(root, { recursive: true });
const file = path.join(root, 'synthetic-above-preview.wav');
const bytes = Buffer.alloc(33 * 1024 * 1024);
bytes.write('RIFF', 0); bytes.writeUInt32LE(bytes.length - 8, 4); bytes.write('WAVEfmt ', 8);
bytes.writeUInt32LE(16, 16); bytes.writeUInt16LE(1, 20); bytes.writeUInt16LE(1, 22);
bytes.writeUInt32LE(16000, 24); bytes.writeUInt32LE(32000, 28); bytes.writeUInt16LE(2, 32); bytes.writeUInt16LE(16, 34);
bytes.write('data', 36); bytes.writeUInt32LE(bytes.length - 44, 40);
if (fs.existsSync(file)) {
  if (!fs.readFileSync(file).equals(bytes)) throw Error('Refused to overwrite different fixture bytes.');
} else fs.writeFileSync(file, bytes, { flag: 'wx' });
console.log(JSON.stringify({ file, length: bytes.length, sha256: crypto.createHash('sha256').update(bytes).digest('hex'), chunks: Math.ceil(bytes.length / 8388608) }));
