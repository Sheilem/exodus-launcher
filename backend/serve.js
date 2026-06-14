// serve.js — zero-dependency static host for the Exodus Launcher demo.
//   node serve.js [port] [rootDir]
// Serves manifest.json, status.json, news.json and the /files mirror so the
// launcher can run a full update cycle against your machine. CORS-open, no deps.

const http = require("http");
const fs = require("fs");
const path = require("path");

const port = parseInt(process.argv[2] || "8777", 10);
const root = path.resolve(process.argv[3] || path.join(__dirname, "dist"));
const stub = path.join(__dirname, "stub");

const types = {
  ".json": "application/json",
  ".img": "application/octet-stream",
  ".wz": "application/octet-stream",
  ".dll": "application/octet-stream",
  ".exe": "application/octet-stream",
};

function send(res, code, body, type) {
  res.writeHead(code, { "Content-Type": type || "text/plain", "Access-Control-Allow-Origin": "*" });
  res.end(body);
}

const server = http.createServer((req, res) => {
  const urlPath = decodeURIComponent(req.url.split("?")[0]);
  // status.json / news.json fall back to the stub folder if not in dist
  const candidates = [path.join(root, urlPath), path.join(stub, urlPath)];
  let file = candidates.find((p) => fs.existsSync(p) && fs.statSync(p).isFile());

  if (!file) return send(res, 404, "Not found: " + urlPath);
  // prevent path traversal
  if (!candidates.some((c) => path.resolve(file) === path.resolve(c))) return send(res, 403, "Forbidden");

  const ext = path.extname(file).toLowerCase();
  fs.readFile(file, (err, data) => {
    if (err) return send(res, 500, String(err));
    send(res, 200, data, types[ext] || "application/octet-stream");
  });
});

server.listen(port, () => {
  console.log(`Exodus demo host  ->  http://localhost:${port}`);
  console.log(`  manifest:  http://localhost:${port}/manifest.json   (from ${root})`);
  console.log(`  status:    http://localhost:${port}/status.json     (stub fallback)`);
  console.log(`  news:      http://localhost:${port}/news.json        (stub fallback)`);
  console.log(`  files:     http://localhost:${port}/files/...`);
});
