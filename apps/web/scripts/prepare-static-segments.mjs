import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// Next's exported segment payloads must also be directly addressable on plain
// static hosts (including the out/ upload), without an Nginx-specific rewrite.
export function prepareStaticSegments(directory) {
  const root = fs.realpathSync(directory);
  if (!fs.statSync(root).isDirectory()) throw new Error("Static export must be a directory.");
  const pending = [];
  const destinations = new Set();

  function walk(current) {
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const file = path.join(current, entry.name);
      if (entry.isSymbolicLink()) throw new Error("Static export must not contain symbolic links.");
      if (entry.isDirectory()) { walk(file); continue; }
      if (!entry.isFile() || entry.name !== "__PAGE__.txt") continue;
      const parts = path.relative(root, file).split(path.sep);
      const marker = parts.findIndex((part) => part.startsWith("__next."));
      if (marker < 0) continue;
      const segments = parts.slice(marker, -1);
      if (!/^__next\.[A-Za-z0-9_-]+$/.test(segments[0]) ||
          segments.slice(1).some((part) => !/^[A-Za-z0-9_-]+$/.test(part))) {
        throw new Error("Unsupported exported static segment layout; review before publishing.");
      }
      const destination = path.join(root, ...parts.slice(0, marker), segments.join(".") + ".__PAGE__.txt");
      const relative = path.relative(root, destination);
      if (relative.startsWith("..") || path.isAbsolute(relative)) throw new Error("Static segment escaped export.");
      if (destinations.has(destination)) throw new Error("Conflicting exported static segment aliases.");
      destinations.add(destination);
      const bytes = fs.readFileSync(file);
      if (fs.existsSync(destination)) {
        if (!fs.lstatSync(destination).isFile() || !bytes.equals(fs.readFileSync(destination))) {
          throw new Error("Existing static segment alias differs; refusing to overwrite.");
        }
      } else {
        pending.push({ destination, bytes });
      }
    }
  }

  // Validate the entire tree and all existing destinations before writing.
  walk(root);
  if (destinations.size === 0) throw new Error("No exported static segments found; review the export layout.");
  for (const { destination, bytes } of pending) fs.writeFileSync(destination, bytes, { flag: "wx" });
  return { segments: destinations.size, created: pending.length, unchanged: destinations.size - pending.length };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const root = fileURLToPath(new URL("../out/", import.meta.url));
  console.log("Static segment aliases: " + JSON.stringify(prepareStaticSegments(root)));
}
