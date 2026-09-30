// Faux serveur d'API pour tester l'interface sans SQL Server : `node mock/server.mjs` (port 5080).
// Sert aussi dist/logs-platform si présent (http://localhost:5080).
import http from 'node:http';
import { randomBytes } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';

const PORT = Number(process.env.PORT ?? 5080);
const now = Date.now();
let seed = 42;
const rnd = () => ((seed = (seed * 1664525 + 1013904223) % 4294967296) / 4294967296);
const pick = a => a[Math.floor(rnd() * a.length)];

const TYPES = [
  ['Company.ExceptionHandling.Exceptions.NotFoundException', 404, 'Information', 'Produit introuvable.'],
  ['Company.ExceptionHandling.Exceptions.ForbiddenException', 403, 'Warning', "Accès refusé à cette ressource."],
  ['Company.ExceptionHandling.Exceptions.ValidationFailedException', 400, 'Warning', 'Le nom est obligatoire.'],
  ['Company.ExceptionHandling.Exceptions.ConflictException', 409, 'Warning', 'Ce produit existe déjà.'],
  ['Company.ExceptionHandling.Exceptions.BusinessRuleException', 422, 'Warning', 'Stock insuffisant.'],
  ['System.UnauthorizedAccessException', 401, 'Warning', 'Jeton invalide.'],
  ['Microsoft.Data.SqlClient.SqlException', 500, 'Error', 'Timeout expired.'],
  ['System.NullReferenceException', 500, 'Error', 'Object reference not set to an instance of an object.'],
  ['System.InvalidOperationException', 500, 'Error', 'Sequence contains no elements.'],
];
const WEIGHTS = [26, 12, 20, 6, 8, 8, 6, 8, 6];
const ENDPOINTS = [['GET', '/api/products'], ['GET', '/api/products/{id}'], ['POST', '/api/products'], ['PUT', '/api/products/{id}'], ['DELETE', '/api/categories/{id}'], ['GET', '/api/orders']];
const PROJECTS = ['CatalogueAPP', 'BillingAPP', 'IdentityAPP'];

const logs = [];
for (const proj of PROJECTS) {
  const n = proj === 'CatalogueAPP' ? 900 : 200;
  for (let i = 0; i < n; i++) {
    let r = rnd() * WEIGHTS.reduce((a, b) => a + b), k = 0;
    while (r > WEIGHTS[k]) { r -= WEIGHTS[k]; k++; }
    const [type, status, severity, message] = TYPES[k];
    const [m, p] = pick(ENDPOINTS);
    logs.push({
      id: logs.length + 1, traceId: randomBytes(16).toString('hex'), cprj: proj, applicationName: proj === 'CatalogueAPP' ? 'Catalog.Api' : proj,
      severity, statusCode: status, exceptionType: type, message, httpMethod: m, path: p.replace('{id}', String(1 + Math.floor(rnd() * 500))),
      userName: pick(['alice', 'bob', null, 'rami']), sourceContext: 'BPRI.ExceptionHandling', environmentName: 'Development', machineName: 'DEV-PC', fingerprint: randomBytes(8).toString('hex'),
      innerException: severity === 'Error' ? 'System.TimeoutException: The operation has timed out.' : null,
      exception: `${type}: ${message}\n   at Catalog.Application.Products.Handler.Handle(Request request) in Handler.cs:line ${10 + Math.floor(rnd() * 90)}\n   at Catalog.Api.Endpoints.ProductEndpoints.<>c.<Map>b__0_1()` + (severity === 'Error' ? '\n ---> System.TimeoutException: The operation has timed out.' : ''),
      occurredAtUtc: new Date(now - Math.pow(rnd(), 1.8) * 30 * 86400_000).toISOString(),
    });
  }
}
const APP_LOGS = ['Catalog.Application.Products.CreateProductHandler', 'Microsoft.EntityFrameworkCore.Database.Command', 'Catalog.Api.Auth'];
for (const l of logs.slice()) {
  if (rnd() < 0.5) {
    const ctx = pick(APP_LOGS);
    logs.push({ ...l, id: logs.length + 1, severity: rnd() < 0.15 ? 'Warning' : 'Information', statusCode: null, exceptionType: null, sourceContext: ctx,
      message: ctx.endsWith('Auth') ? 'Utilisateur connecté.' : ctx.includes('Database') ? 'Executed DbCommand (12ms)' : 'Produit créé.', innerException: null, exception: null, fingerprint: null });
  }
}
logs.forEach(l => { l.userId = l.userName ? `u-${l.userName}` : null; l.claims = l.userName ? JSON.stringify({ cprj: l.cprj, role: 'Admin', email: `${l.userName}@bpri.test` }) : null; });
const list = ({ exception, claims, ...x }) => x;

const send = (res, code, body) => { res.writeHead(code, { 'content-type': 'application/json' }); res.end(JSON.stringify(body)); };
const all = (u, k) => u.searchParams.getAll(k);

function scope(u, { ignoreType = false } = {}) {
  const from = new Date(u.searchParams.get('from') ?? 0).getTime();
  const to = new Date(u.searchParams.get('to') ?? now + 1e9).getTime();
  return logs.filter(l => l.cprj === u.searchParams.get('project') && +new Date(l.occurredAtUtc) >= from && +new Date(l.occurredAtUtc) <= to);
}
const countBy = (arr, f) => { const m = new Map(); arr.forEach(x => m.set(f(x), (m.get(f(x)) ?? 0) + 1)); return m; };

function handle(u, res) {
  const path = u.pathname;
  if (path === '/api/projects') {
    return send(res, 200, PROJECTS.map(code => {
      const l = logs.filter(x => x.cprj === code);
      return { code, totalLogs: l.length, lastOccurredAtUtc: l.map(x => x.occurredAtUtc).sort().pop() };
    }));
  }
  if (path === '/api/stats') {
    const s = scope(u);
    const from = new Date(u.searchParams.get('from')).getTime(), to = new Date(u.searchParams.get('to')).getTime();
    const hourly = to - from <= 2 * 86400_000 + 1000;
    const step = hourly ? 3600_000 : 86400_000;
    const start = Math.floor(from / step) * step;
    const buckets = new Map();
    for (let t = start; t <= to; t += step) buckets.set(t, { timestampUtc: new Date(t).toISOString(), error: 0, warning: 0, information: 0 });
    s.forEach(l => { const b = buckets.get(Math.floor(+new Date(l.occurredAtUtc) / step) * step); if (b) b[l.severity === 'Error' ? 'error' : l.severity === 'Warning' ? 'warning' : 'information']++; });
    const sev = countBy(s, l => l.severity);
    return send(res, 200, {
      total: s.length, errors: sev.get('Error') ?? 0, warnings: sev.get('Warning') ?? 0, informations: sev.get('Information') ?? 0,
      bySeverity: [...sev].map(([severity, count]) => ({ severity, count })),
      byStatusCode: [...countBy(s.filter(l => l.statusCode), l => l.statusCode)].map(([statusCode, count]) => ({ statusCode, count })),
      byExceptionType: [...countBy(s.filter(l => l.exceptionType), l => l.exceptionType)].map(([exceptionType, count]) => ({ exceptionType, count })).sort((a, b) => b.count - a.count),
      topEndpoints: [...countBy(s.filter(l => l.path), l => `${l.httpMethod} ${l.path.replace(/\d+$/, '{id}')}`)].map(([k, count]) => ({ method: k.split(' ')[0], path: k.split(' ')[1], count })).sort((a, b) => b.count - a.count).slice(0, 8),
      timeline: [...buckets.values()], timelineBucket: hourly ? 'hour' : 'day',
    });
  }
  if (path === '/api/filters') {
    const s = scope(u);
    return send(res, 200, { exceptionTypes: [...new Set(s.map(l => l.exceptionType).filter(Boolean))].sort(), statusCodes: [...new Set(s.map(l => l.statusCode).filter(Boolean))].sort((a, b) => a - b) });
  }
  if (path === '/api/logs') {
    let s = scope(u);
    const sev = all(u, 'severity'), st = all(u, 'statusCode').map(Number);
    if (sev.length) s = s.filter(l => sev.includes(l.severity));
    if (st.length) s = s.filter(l => st.includes(l.statusCode));
    if (u.searchParams.get('exceptionType')) s = s.filter(l => l.exceptionType === u.searchParams.get('exceptionType'));
    if (u.searchParams.get('traceId')) s = s.filter(l => l.traceId.startsWith(u.searchParams.get('traceId').toLowerCase()));
    const q = u.searchParams.get('search')?.toLowerCase();
    if (q) s = s.filter(l => (l.message + (l.path ?? '')).toLowerCase().includes(q));
    const f = u.searchParams.get('sortField') ?? 'occurredAtUtc', dir = u.searchParams.get('sortDir') === 'asc' ? 1 : -1;
    s.sort((a, b) => (a[f] > b[f] ? 1 : a[f] < b[f] ? -1 : 0) * dir);
    const page = Number(u.searchParams.get('page') ?? 1), size = Number(u.searchParams.get('pageSize') ?? 25);
    return send(res, 200, { items: s.slice((page - 1) * size, page * size).map(list), total: s.length, page, pageSize: size });
  }
  const m = path.match(/^\/api\/logs\/(\d+)$/);
  if (m) { const l = logs.find(x => x.id === Number(m[1])); return l ? send(res, 200, l) : send(res, 404, { title: 'Not found' }); }
  return false;
}

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.ico': 'image/x-icon', '.json': 'application/json' };
const root = join(process.cwd(), 'dist', 'logs-platform', 'browser');
http.createServer(async (req, res) => {
  const u = new URL(req.url, 'http://x');
  if (u.pathname.startsWith('/api/')) { if (handle(u, res) === false) send(res, 404, {}); return; }
  const file = normalize(join(root, u.pathname === '/' ? 'index.html' : u.pathname));
  if (!file.startsWith(root)) { res.writeHead(403); return res.end(); }
  try { const b = await readFile(file); res.writeHead(200, { 'content-type': MIME[extname(file)] ?? 'application/octet-stream' }); res.end(b); }
  catch { try { const b = await readFile(join(root, 'index.html')); res.writeHead(200, { 'content-type': 'text/html' }); res.end(b); } catch { res.writeHead(404); res.end('build manquant'); } }
}).listen(PORT, () => console.log(`Mock API sur http://localhost:${PORT}`));
