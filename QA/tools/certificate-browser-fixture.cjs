// Synthetic in-memory transport for real DOM testing. NOT Identity/SQL proof.
const http = require('node:http');
const port = Number(process.env.QA_CERTIFICATE_API_PORT || 49132);
const webOrigin = process.env.QA_CERTIFICATE_WEB_ORIGIN || 'http://127.0.0.1:49131';
if (!Number.isInteger(port) || port < 1024 || port > 65535 || !/^http:\/\/127\.0\.0\.1:\d+$/.test(webOrigin)) throw Error('Loopback nonprivileged ports required');
const base = { id: 'saved-c1', studentId: 's1', batchId: 'b1', certificateNumber: 'CERT-SYNTHETIC-001', verificationCode: 'VERIFY-SYNTHETIC-001', title: 'Saved achievement', templateKey: 'school-merit', issuedDate: '2020-02-29', status: 'Issued', notes: 'Saved recognition' };
const students = [{ id: 's1', firstName: 'Saved', lastName: 'Learner' }, { id: 's2', firstName: 'Other', lastName: 'Learner' }];
const batches = [{ id: 'b1', name: 'Saved programme' }, { id: 'b2', name: 'Other programme' }];
const branding = { academyName: 'Synthetic QA Academy', accentColor: '#0F6CBD', signatoryName: 'Saved Signatory', logoUrl: null };
let certificates = [{ ...base }, { ...base, id: 'revoked-c2', certificateNumber: 'CERT-SYNTHETIC-REVOKED', status: 'Revoked' }], readsFail = false, issueFail = false, sequence = 0;
const requests = [], prints = [];
const json = (res, value, status = 200) => { res.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' }); res.end(JSON.stringify(value)); };
async function body(req) { let data = ''; for await (const chunk of req) { data += chunk; if (data.length > 200000) throw Error('Too large'); } return JSON.parse(data || '{}'); }
const server = http.createServer(async (req, res) => {
  try {
    if (req.headers.origin && req.headers.origin !== webOrigin) return json(res, { message: 'Wrong QA origin' }, 403);
    res.setHeader('Access-Control-Allow-Origin', webOrigin); res.setHeader('Access-Control-Allow-Headers', 'Content-Type'); res.setHeader('Access-Control-Allow-Methods', 'GET,POST,OPTIONS');
    if (req.method === 'OPTIONS') return json(res, null, 204);
    if (req.headers.authorization) return json(res, { message: 'Refused account credentials on synthetic QA transport' }, 403);
    if (req.method === 'GET' && req.url === '/qa/state') return json(res, { certificates, requests, prints, readsFail, issueFail });
    if (req.method === 'POST' && req.url === '/qa/control') {
      const { action } = await body(req);
      if (action === 'reset') { certificates = [{ ...base }, { ...base, id: 'revoked-c2', certificateNumber: 'CERT-SYNTHETIC-REVOKED', status: 'Revoked' }]; readsFail = issueFail = false; }
      else if (action === 'revoke') certificates = certificates.map(x => x.id === base.id ? { ...x, status: 'Revoked' } : x);
      else if (action === 'replace') certificates = certificates.map(x => x.id === base.id ? { ...x, status: 'Replaced' } : x);
      else if (action === 'read-fail') readsFail = true;
      else if (action === 'recover') readsFail = issueFail = false;
      else if (action === 'issue-fail') issueFail = true;
      else if (action === 'independent') certificates = [{ ...base, batchId: null, notes: null }];
      else if (action === 'long-note') certificates = [{ ...base, notes: 'Synthetic long recognition note. '.repeat(160) }];
      else return json(res, { message: 'Unknown synthetic action' }, 400);
      console.log('QA synthetic scenario ' + action); return json(res, { message: 'Synthetic scenario: ' + action });
    }
    if (req.method === 'POST' && req.url === '/qa/print') { const capture = await body(req); prints.push(capture); console.log('QA DOM print snapshot ' + JSON.stringify(capture)); return json(res, { captured: prints.length }); }
    requests.push({ method: req.method, path: req.url });
    console.log('QA transport ' + req.method + ' ' + req.url);
    if (req.method === 'GET' && req.url === '/api/portal/announcements') return json(res, []);
    if (req.method === 'GET' && req.url === '/api/academies') return json(res, [{ id: 'synthetic-owned', name: 'Synthetic QA Academy' }]);
    const suffix = req.url.replace('/api/academies/synthetic-owned/', '');
    if (req.method === 'GET') {
      if (readsFail) return json(res, { message: 'Synthetic read failure' }, 500);
      if (suffix === 'students') return json(res, students);
      if (suffix === 'batches') return json(res, batches);
      if (suffix === 'enrollments') return json(res, [{ studentId: 's1', batchId: 'b1', status: 'Active' }, { studentId: 's2', batchId: 'b2', status: 'Completed' }]);
      if (suffix === 'certificates') return json(res, certificates);
      if (suffix === 'certificates/branding') return json(res, branding);
    }
    if (req.method === 'POST' && suffix === 'certificates') {
      const value = await body(req); if (issueFail) return json(res, { message: 'Synthetic issuance rejected' }, 400);
      if (!students.some(x => x.id === value.studentId) || !value.title) return json(res, { message: 'Synthetic invalid payload' }, 400);
      const saved = { ...base, ...value, batchId: value.batchId || null, notes: value.notes || null, issuedDate: value.issuedDate || '2026-10-02', status: 'Issued', id: 'new-c' + ++sequence, certificateNumber: 'CERT-SYNTHETIC-NEW-' + sequence, verificationCode: 'VERIFY-SYNTHETIC-NEW-' + sequence };
      certificates = [certificates[0], saved, ...certificates.slice(1)].filter(Boolean); return json(res, saved, 201);
    }
    return json(res, { message: 'Not implemented in isolated certificate fixture' }, 404);
  } catch { json(res, { message: 'Synthetic fixture body failure' }, 400); }
});
server.listen(port, '127.0.0.1', () => console.log('QA certificate fixture ready http://127.0.0.1:' + port + '; synthetic only; no auth/SQL/customer/provider/Azure'));
