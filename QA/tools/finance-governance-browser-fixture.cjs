// Synthetic, loopback-only UI fixture. No application database or credentials.
const http = require('node:http');

const academyId = '11111111-1111-4111-8111-111111111111';
const adjustmentId = '22222222-2222-4222-8222-222222222222';
const port = 49312;
let approvalAttempts = 0;
let adjustmentReads = 0;

const server = http.createServer((request, response) => {
  if (request.headers.host !== `127.0.0.1:${port}`) {
    response.writeHead(400).end();
    return;
  }
  response.setHeader('Access-Control-Allow-Origin', 'http://127.0.0.1:49311');
  response.setHeader('Access-Control-Allow-Methods', 'GET, PATCH, OPTIONS');
  response.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');
  response.setHeader('Content-Type', 'application/json');
  if (request.method === 'OPTIONS') return response.writeHead(204).end();
  const path = new URL(request.url, `http://127.0.0.1:${port}`).pathname;
  if (path === '/qa/state') return response.writeHead(200).end(JSON.stringify({ approvalAttempts, adjustmentReads }));
  if (request.method === 'GET' && path === '/api/academies') return response.writeHead(200).end(JSON.stringify([{ id: academyId }]));
  const prefix = `/api/academies/${academyId}`;
  if (request.method === 'GET' && path === `${prefix}/finance-adjustments`) {
    adjustmentReads++;
    return response.writeHead(200).end(JSON.stringify([{ id: adjustmentId, type: 'Discount', amount: 200, reason: 'Synthetic pending approval', status: 'PendingApproval' }]));
  }
  if (request.method === 'GET' && (path === `${prefix}/finance-governance/collections` || path === `${prefix}/finance-governance/collection-tasks`)) return response.writeHead(200).end('[]');
  if (request.method === 'GET' && path === `${prefix}/finance-governance/settings`) return response.writeHead(200).end(JSON.stringify({ taxRegistrationNumber: '', taxLabel: '', taxRatePercent: 0, defaultPaymentTermsDays: 0, taxInclusivePricing: false, invoiceTemplateKey: 'Classic', payslipTemplateKey: 'Standard' }));
  if (request.method === 'PATCH' && path === `${prefix}/finance-adjustments/${adjustmentId}/approval`) {
    approvalAttempts++;
    return response.writeHead(400).end(JSON.stringify({ message: 'Adjustment exceeds the remaining invoice balance.' }));
  }
  response.writeHead(404).end('{}');
});
server.listen(port, '127.0.0.1', () => console.log(`FINANCE-GOVERNANCE-FIXTURE ready http://127.0.0.1:${port}`));
