const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const clipboard = require("clipboardy");

const world = require("./parseWorld.js");
const buildFileList = require("./buildFileList.js");

// Read command-line parameters
const worldName = process.argv[2];
const debug = process.argv.includes("--debug");
// Validate parameters
if (!worldName) {
  console.error(`Usage: SaplingFS <world> [--debug]`);
  process.exit();
}
// Find Minecraft world file path
let worldPath;
if (fs.existsSync(worldName) && fs.lstatSync(worldName).isDirectory()) {
  worldPath = worldName;
} else {
  worldPath = `${os.homedir()}/.minecraft/saves/${worldName}`;
}

// Build list of files from root
const fileList = buildFileList("/home/p2r3/");
console.log(`Found ${fileList.length} files`);

let nodes = [[0, 32, 0]]; // Open nodes
const mapping = {}; // Closed nodes (linked to files)
let trees = [];

let terrainGroup = 0;
let lastParent = "";

const parentDepth = 3;

let min_x = 0, max_x = 0, min_z = 0, max_z = 0;

const debugPalette = [
  "white_wool",
  "light_gray_wool",
  "gray_wool",
  "black_wool",
  "brown_wool",
  "red_wool",
  "orange_wool",
  "yellow_wool",
  "lime_wool",
  "green_wool",
  "cyan_wool",
  "light_blue_wool",
  "blue_wool",
  "purple_wool",
  "magenta_wool",
  "pink_wool"
];

const suppressMaxIterations = 500;
let suppressFor = Math.floor(Math.random() * suppressMaxIterations);
let suppressDirection = Math.floor(Math.random() * 4);

while (fileList.length > 0 && nodes.length > 0) {

  let [x, y, z] = nodes.shift();
  const key = `${x},${y},${z}`;

  if (key in mapping) {
    if (nodes.length === 0) {
      let cx, cy, cz;
      do {
        cx = Math.floor(Math.random() * (max_x - min_x)) + min_x;
        cz = Math.floor(Math.random() * (max_z - min_z)) + min_z;
        cy = Math.floor(Math.random() * 64);
      } while (`${cx},${cy},${cz}` in mapping);
      nodes.push([cx, cy, cz]);
    }
    continue;
  }

  suppressFor --;
  if (suppressFor <= 0) {
    suppressFor = Math.floor(Math.random() * suppressMaxIterations);
    suppressDirection = Math.floor(Math.random() * 4);
  }

  const file = fileList.shift();

  const pathParts = file.path.split(path.sep).slice(0, -1);
  const shortParent = pathParts.slice(0, parentDepth + 1).join(path.sep);

  if (lastParent && lastParent !== shortParent) {
    console.log(shortParent);

    nodes = [];
    terrainGroup ++;

    for (const tree of trees) {
      const { x, z, files } = tree;
      const candidates = Object.values(mapping).filter(c => c.x === x && c.z === z);
      candidates.sort((a, b) => b.y - a.y);
      const y = candidates[0].y + 1;
      for (let i = 0; i < 5; i ++) {
        mapping[`${x},${y + i},${z}`] = { x, y: y + i, z, file: files.pop(), block: "oak_log", valid: true };
      }
      for (let i = 0; i < 2; i ++) {
        for (let j = -2; j <= 2; j ++) {
          for (let k = -2; k <= 2; k ++) {
            if (j === 0 && k === 0) continue;
            mapping[`${x + j},${y + i + 2},${z + k}`] = { x: x + j, y: y + i + 2, z: z + k, file: files.pop(), block: "oak_leaves", valid: true };
          }
        }
      }
      for (let i = 0; i < 2; i ++) {
        for (let j = -1; j <= 1; j ++) {
          for (let k = -1; k <= 1; k ++) {
            if (i === 0 && j === 0 && k === 0) continue;
            mapping[`${x + j},${y + i + 4},${z + k}`] = { x: x + j, y: y + i + 4, z: z + k, file: files.pop(), block: "oak_leaves", valid: true };
          }
        }
      }
    }
    trees = [];
  }
  lastParent = shortParent;

  let block;
  if (debug) {
    block = debugPalette[terrainGroup % debugPalette.length];
  } else {
    block = "grass_block";
  }
  mapping[key] = { x, y, z, file, block, valid: true };

  if (Math.random() < 0.05) nodes.push([x, y + 1, z]);
  if (y > -64 && Math.random() < 0.05) nodes.push([x, y - 1, z]);

  if (suppressDirection !== 0) nodes.push([x - 1, y, z]);
  if (suppressDirection !== 1) nodes.push([x + 1, y, z]);
  if (suppressDirection !== 2) nodes.push([x, y, z - 1]);
  if (suppressDirection !== 3) nodes.push([x, y, z + 1]);

  if (Math.floor(Math.random() * 5000) === 0) {
    if (fileList.length < 62) continue;
    if (trees.find(c => c.x === x && c.z === z)) continue;
    trees.push({
      x, z,
      files: fileList.slice(0, 62)
    });
    fileList.splice(0, 62);
  }

}

console.log(`${fileList.length} files left unallocated`);

async function placeFileBlocks () {

  let _x, _z;

  for (const key in mapping) {
    const entry = mapping[key];
    if (!entry.valid) continue;
    _x = Math.floor(entry.x / 16);
    _z = Math.floor(entry.z / 16);
    break;
  }

  console.log(`Generating chunk (${_x} ${_z})`);

  const blocks = [];
  for (let x = 0; x < 16; x ++) {
    blocks[x] = [];
    for (let y = 0; y < 128 + 64; y ++) {
      blocks[x][y] = [];
      for (let z = 0; z < 16; z ++) {
        blocks[x][y][z] = "air";
      }
    }
  }

  let validEntries = Object.keys(mapping).length;

  for (const key in mapping) {
    const entry = mapping[key];
    if (!entry.valid) {
      validEntries --;
      continue;
    }
    if (_x !== Math.floor(entry.x / 16)) continue;
    if (_z !== Math.floor(entry.z / 16)) continue;

    blocks[entry.x - _x * 16][entry.y + 64][entry.z - _z * 16] = entry.block;

    entry.valid = false;
    validEntries --;
  }

  const countAdjacent = function (x, y, z) {
    let adjacent = 0;
    x += _x * 16;
    z += _z * 16;
    y -= 64;
    if (`${x + 1},${y},${z}` in mapping) adjacent ++;
    if (`${x - 1},${y},${z}` in mapping) adjacent ++;
    if (`${x},${y + 1},${z}` in mapping) adjacent ++;
    if (`${x},${y - 1},${z}` in mapping) adjacent ++;
    if (`${x},${y},${z + 1}` in mapping) adjacent ++;
    if (`${x},${y},${z - 1}` in mapping) adjacent ++;
    return adjacent;
  };

  for (let x = 0; x < 16; x ++) {
    for (let y = 0; y < 128 + 64; y ++) {
      for (let z = 0; z < 16; z ++) {

        if (blocks[x][y][z] === "air") continue;
        if (blocks[x][y][z] === "oak_log") continue;
        if (blocks[x][y][z] === "oak_leaves") continue;

        const adjacent = countAdjacent(x, y, z);
        if (adjacent > 2) continue;

        const block = blocks[x][y][z];
        const key = `${x + _x * 16},${y - 64},${z + _z * 16}`;
        blocks[x][y][z] = "air";

        const directions = [
          [1, 0],
          [-1, 0],
          [0, 1],
          [0, -1]
        ];

        let found = false;
        for (const [dx, dz] of directions) {
          const cx = x + dx, cz = z + dz;
          if (blocks[cx]?.[y]?.[cz] === "air" && countAdjacent(cx, y, cz) > adjacent) {
            blocks[cx][y][cz] = block;
            if (key in mapping) {
              mapping[`${cx + _x * 16},${y - 64},${cz + _z * 16}`] = mapping[key];
              delete mapping[key];
            }
            found = true;
            break;
          }
        }
        if (!found) blocks[x][y][z] = block;

      }
    }
  }

  if (!debug) {
    for (let x = 0; x < 16; x ++) {
      for (let y = 0; y < 128 + 64; y ++) {
        for (let z = 0; z < 16; z ++) {
          if (blocks[x][y][z] === "grass_block" && blocks[x][y + 1][z] !== "air") blocks[x][y][z] = "dirt";
        }
      }
    }
  }

  const bounds = [_x * 16, -64, _z * 16, _x * 16 + 16, 128, _z * 16 + 16];

  await world.forRegion (worldPath, async function (region, rx, rz) {
    await world.blocksToRegion(blocks, region, rx, rz, bounds);
  }, bounds);

  const regionWritePromises = [];
  for (const mcaFile in world.regionFileCache) {
    const region = world.regionFileCache[mcaFile];
    regionWritePromises.push(
      Bun.write(`${worldPath}/region/${mcaFile}`, region)
    );
  }
  await Promise.all(regionWritePromises);

  return validEntries;

}

let validEntries;
do {
  validEntries = await placeFileBlocks();
} while (validEntries > 0);

console.log("Listening for clipboard changes...");

let clipboardLast = "";
setInterval(async function () {

  const text = await clipboard.default.read();
  if (text === clipboardLast) return;
  clipboardLast = text;

  if (!text.startsWith("/execute in minecraft:overworld run tp @s")) return;

  const [x, y, z] = text.split("@s ")[1].split(" ").map(c => Math.floor(Number(c)));
  const key = `${x},${y - 1},${z}`;
  const entry = mapping[key];

  if (!entry) {
    console.log("No file associated with this block.");
  } else {
    console.log(`(${x} ${y - 1} ${z}) ${entry.file.path}`);
  }

}, 200);
