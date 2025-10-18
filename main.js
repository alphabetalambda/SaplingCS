const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const clipboard = require("clipboardy");

const world = require("./parseWorld.js");
const buildFileList = require("./buildFileList.js");
const Vector = require("./Vector.js");

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

let nodes = [new Vector(0, 32, 0)]; // Open nodes
const mapping = {}; // Closed nodes (linked to files)
let trees = [], ponds = [];

let terrainGroup = 0;
let lastParent = "";

const parentDepth = 3;

let mins = new Vector(0, 0, 0);
let maxs = new Vector(0, 0, 0);
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

// Whether the block is part of natural ground terrain
function isGroundBlock (block) {
  return (
    block === "dirt" ||
    block === "grass_block"
  );
}

// Whether grass converts to dirt under this block
function isHeavyBlock (block) {
  return isGroundBlock(block) || block === "water";
}

function isAir (block) {
  return !block || block === "air";
}

const suppressMaxIterations = 100;
let suppressFor = Math.floor(Math.random() * suppressMaxIterations);
let suppressDirection = Math.floor(Math.random() * 4);

while (fileList.length > 0 && nodes.length > 0) {

  const pos = nodes.shift();
  const key = pos.toString();

  if (key in mapping) {
    if (nodes.length === 0) {
      const rand = new Vector();
      do {
        rand.x = Math.floor(Math.random() * (maxs.x - mins.x)) + mins.x;
        rand.z = Math.floor(Math.random() * (maxs.z - maxs.z)) + mins.z;
        rand.y = Math.floor(Math.random() * 64);
      } while (rand.toString() in mapping);
      nodes.push(rand);
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

    for (const pond of ponds) {

      const candidates = Object.values(mapping).filter(c => (
        c.pos.x === pond.x &&
        c.pos.z === pond.z
      ));
      candidates.sort((a, b) => b.pos.y - a.pos.y);
      pond.y = candidates[0].pos.y;

      const fillNodes = [pond];

      do {

        const curr = fillNodes.shift();
        const key = curr.toString();
        if (!(key in mapping)) continue;

        if (trees.find(c => (
          Math.abs(c.pos.x - curr.x) < 3 &&
          Math.abs(c.pos.z - curr.z) < 3
        ))) continue;

        const blockAbove = mapping[curr.add(0, 1, 0).toString()]?.block;
        if (isGroundBlock(blockAbove)) continue;

        let neighbors = 0;
        let skip = false;
        for (let i = 0; i < 6; i ++) {
          const shiftKey = curr.shifted(i).toString();
          const block = mapping[shiftKey]?.block;
          if (i < 4 && isAir(block)) {
            skip = true;
            break;
          }
          if (block === "water") neighbors ++;
        }
        if (skip) continue;

        mapping[key].block = "water";

        for (let i = 0; i < 4; i ++) {
          if (neighbors < 3 && Math.random() < 0.1) continue;
          fillNodes.push(curr.shifted(i));
        }
        if (curr.y < 127 && Math.random() < 0.05) fillNodes.push(curr.add(0, 1, 0));
        if (curr.y > -64 && Math.random() < 0.05) fillNodes.push(curr.add(0, -1, 0));

      } while (Math.floor(Math.random() * 2000) !== 0 && fillNodes.length > 0);

    }
    ponds = [];

    for (const tree of trees) {

      const candidates = Object.values(mapping).filter(c => (
        c.pos.x === tree.pos.x &&
        c.pos.z === tree.pos.z
      ));
      candidates.sort((a, b) => b.pos.y - a.pos.y);
      tree.pos.y = candidates[0].pos.y + 1;

      // Tree stump
      for (let i = 0; i < 5; i ++) {
        const target = tree.pos.add(0, i, 0);
        const key = target.toString();
        if (key in mapping) {
          fileList.push(tree.files.pop());
          continue;
        }
        mapping[target.toString()] = { pos: target, file: tree.files.pop(), block: "oak_log" }
      }
      // Bottom leaf layer
      for (let i = 0; i < 2; i ++) {
        for (let j = -2; j <= 2; j ++) {
          for (let k = -2; k <= 2; k ++) {
            if (j === 0 && k === 0) continue;
            if (i === 1 && Math.abs(j) === 2 && Math.abs(k) === 2) continue;
            const target = tree.pos.add(j, i + 2, k);
            const key = target.toString();
            if (key in mapping) {
              fileList.push(tree.files.pop());
              continue;
            }
            mapping[target.toString()] = { pos: target, file: tree.files.pop(), block: "oak_leaves" };
          }
        }
      }
      // Top leaf layer
      for (let i = 0; i < 2; i ++) {
        for (let j = -1; j <= 1; j ++) {
          for (let k = -1; k <= 1; k ++) {
            if (i === 0 && j === 0 && k === 0) continue;
            if (i === 1 && j !== 0 && k !== 0) continue;
            const target = tree.pos.add(j, i + 4, k);
            const key = target.toString();
            if (key in mapping) {
              fileList.push(tree.files.pop());
              continue;
            }
            mapping[target.toString()] = { pos: target, file: tree.files.pop(), block: "oak_leaves" };
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
  mapping[key] = { pos, file, block };

  if (pos.x < mins.x) mins.x = pos.x;
  if (pos.y < mins.y) mins.y = pos.y;
  if (pos.z < mins.z) mins.z = pos.z;
  if (pos.x > maxs.x) maxs.x = pos.x;
  if (pos.y > maxs.y) maxs.y = pos.y;
  if (pos.z > maxs.z) maxs.z = pos.z;

  for (let i = 0; i < 4; i ++) {
    if (suppressDirection === i) continue;
    nodes.push(pos.shifted(i));
  }
  if (pos.y < 127 && Math.random() < 0.05) nodes.push(pos.add(0, 1, 0));
  if (pos.y > -64 && Math.random() < 0.05) nodes.push(pos.add(0, -1, 0));

  if (Math.floor(Math.random() * 10000) === 0) {
    ponds.push(pos.clone());
  }

  if (Math.floor(Math.random() * 5000) === 0) {
    if (fileList.length < 62) continue;
    if (trees.find(c => (
      Math.abs(c.pos.x - pos.x) < 5 &&
      Math.abs(c.pos.z - pos.z) < 5
    ))) continue;
    trees.push({
      pos: pos.clone(),
      files: fileList.slice(0, 62)
    });
    fileList.splice(0, 62);
  }

}

console.log(`${fileList.length} files left unallocated`);

/**
 * Iterates over chunks with blocks present in `mapping`, running a
 * callback function on each chunk.
 *
 * The callback is provided a block string array filled with air,
 * an array of relevant `mapping` entries, X/Z chunk coordinates,
 * and an array of min/max bounds for the chunk.
 *
 * @param callback - The function to call (and await) on each chunk
 */
async function forMappedChunks (callback) {
  for (const key in mapping) {
    mapping[key].valid = true;
  }
  let validEntries;
  do {
    validEntries = await iterateMappedChunks(callback);
  } while (validEntries > 0);
}
// Recursive helper function for `forMappedChunks`
async function iterateMappedChunks (callback) {

  let _x, _z;
  for (const key in mapping) {
    const entry = mapping[key];
    if (!("valid" in entry)) continue;
    _x = Math.floor(entry.pos.x / 16);
    _z = Math.floor(entry.pos.z / 16);
    break;
  }

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
  const entries = [];

  for (const key in mapping) {
    const entry = mapping[key];
    if (!("valid" in entry)) {
      validEntries --;
      continue;
    }

    if (_x !== Math.floor(entry.pos.x / 16)) continue;
    if (_z !== Math.floor(entry.pos.z / 16)) continue;

    entries.push(entry);
    delete entry.valid;
    validEntries --;
  }

  const bounds = [
    new Vector(_x * 16, -64, _z * 16),
    new Vector(_x * 16 + 16, 128, _z * 16 + 16),
  ];

  await callback(blocks, entries, _x, _z, bounds);

  return validEntries;

}

// Returns number of allocated blocks adjacent to the given position
function countAdjacent (pos) {
  let adjacent = 0;
  for (let i = 0; i < 6; i ++) {
    if (pos.shifted(i).toString() in mapping) adjacent ++;
  }
  return adjacent;
};

// Generate region data for mapped blocks
await forMappedChunks(async function (blocks, entries, _x, _z, bounds) {

  console.log(`Generating chunk (${_x} ${_z})`);

  // Smooth out terrain by clumping together lonely blocks
  let swaps;
  do {
    swaps = 0;
    for (const entry of entries) {

      if (!isGroundBlock(entry.block)) continue;

      const pos = entry.pos;
      const adjacent = countAdjacent(pos);

      // Temporarily remove the current block's mapping entry
      // It can still be restored by assigning `entry`
      const key = pos.toString();
      delete mapping[key];

      let bestAdjacent = adjacent;
      let bestPosition;

      // Check all 6 directions for a better adjacent block score
      for (let i = 0; i < 6; i ++) {
        const abs = pos.shifted(i);
        const rel = abs.relative(_x, _z);
        // Make sure we're not stepping out of this chunk's boundaries
        if (
          rel.x < 0 || rel.x >= 16 ||
          rel.z < 0 || rel.z >= 16 ||
          rel.y < 0 || rel.y >= 128 + 64
        ) continue;
        // Abort if we're next to water
        const key = abs.toString();
        if (mapping[abs]?.block === "water") {
          bestAdjacent = adjacent;
          break;
        }
        // Make sure we're not shifting into an existing block
        if (key in mapping) continue;
        // Compare this cluster to the previous best
        const newAdjacent = countAdjacent(abs);
        if (newAdjacent <= bestAdjacent) continue;
        bestAdjacent = newAdjacent;
        bestPosition = { rel, abs };
      }

      // If no progress was made, restore previous mapping entry
      if (bestAdjacent === adjacent) {
        // Convert 1-block stubs to short grass
        if (adjacent === 1) {
          const blockBelow = mapping[pos.add(0, -1, 0).toString()]?.block;
          if (blockBelow === "grass_block") entry.block = "short_grass";
        }
        mapping[key] = entry;
        continue;
      }

      // Assign new block position
      entry.pos = bestPosition.abs;
      mapping[bestPosition.abs.toString()] = entry;

      swaps ++;

    }
  } while (swaps > 0);

  // Fill block array with relevant blocks
  for (const entry of entries) {
    // Convert covered grass blocks to dirt
    const blockAbove = mapping[entry.pos.add(0, 1, 0).toString()];
    if (entry.block === "grass_block" && isHeavyBlock(blockAbove?.block)) {
      entry.block = "dirt";
    }
    // Convert lonely blocks in ponds to water
    let waterAdjacent = 0;
    for (let i = 0; i < 6; i ++) {
      if (mapping[entry.pos.shifted(i).toString()]?.block === "water") {
        waterAdjacent ++;
      } else if (i < 4) {
        waterAdjacent = 0;
        break;
      }
    }
    if (waterAdjacent >= 5) entry.block = "water";
    // Assign block to chunk array
    const [x, y, z] = entry.pos.relative(_x, _z).toArray();
    blocks[x][y][z] = entry.block;
  }

  // Save changes to region data
  await world.forRegion(worldPath, async function (region, rx, rz) {
    await world.blocksToRegion(blocks, region, rx, rz, bounds);
  }, bounds);

});

// Write new region data to disk
const regionWritePromises = [];
for (const mcaFile in world.regionFileCache) {
  const region = world.regionFileCache[mcaFile];
  regionWritePromises.push(
    Bun.write(`${worldPath}/region/${mcaFile}`, region)
  );
}
await Promise.all(regionWritePromises);

console.log("\nChunk generation finished");

/**
 * Uses an implementation of 3D DDA to cast a ray that hits
 * the first mapped block.
 *
 * @param {Vector} pos - Starting position of the ray
 * @param {Vector} fvec - Ray direction (must be normalized)
 * @param {number} [range=50] – Maximum distance to search
 *
 * @returns {Object|null} – `mapping` entry or null if nothing was hit
 */
function raycast (pos, fvec, range = 50) {

  // Set the starting voxel
  let voxelX = Math.floor(pos.x);
  let voxelY = Math.floor(pos.y);
  let voxelZ = Math.floor(pos.z);

  // Calculate step direction for each axis
  const stepX = fvec.x > 0 ?  1 : fvec.x < 0 ? -1 : 0;
  const stepY = fvec.y > 0 ?  1 : fvec.y < 0 ? -1 : 0;
  const stepZ = fvec.z > 0 ?  1 : fvec.z < 0 ? -1 : 0;

  // Distance along the ray to cross one voxel in each axis
  const tDeltaX = fvec.x !== 0 ? Math.abs(1 / fvec.x) : Infinity;
  const tDeltaY = fvec.y !== 0 ? Math.abs(1 / fvec.y) : Infinity;
  const tDeltaZ = fvec.z !== 0 ? Math.abs(1 / fvec.z) : Infinity;

  // Distance from ray start to first voxel boundary on each axis
  const nextBoundaryX = stepX > 0 ? voxelX + 1 : voxelX;
  const nextBoundaryY = stepY > 0 ? voxelY + 1 : voxelY;
  const nextBoundaryZ = stepZ > 0 ? voxelZ + 1 : voxelZ;
  let tMaxX = fvec.x !== 0 ? (nextBoundaryX - pos.x) / fvec.x : Infinity;
  let tMaxY = fvec.y !== 0 ? (nextBoundaryY - pos.y) / fvec.y : Infinity;
  let tMaxZ = fvec.z !== 0 ? (nextBoundaryZ - pos.z) / fvec.z : Infinity;

  // Iterate until we hit a block or exceed range
  let distance = 0;
  while (distance <= range) {

    // Check for intersection with a mapped block
    const key = `${voxelX},${voxelY},${voxelZ}`;
    if (key in mapping) return mapping[key];

    // Choose the smallest tMax to step to the next voxel
    if (tMaxX < tMaxY) {
      if (tMaxX < tMaxZ) {
        voxelX += stepX;
        distance = tMaxX;
        tMaxX += tDeltaX;
      } else {
        voxelZ += stepZ;
        distance = tMaxZ;
        tMaxZ += tDeltaZ;
      }
    } else {
      if (tMaxY < tMaxZ) {
        voxelY += stepY;
        distance = tMaxY;
        tMaxY += tDeltaY;
      } else {
        voxelZ += stepZ;
        distance = tMaxZ;
        tMaxZ += tDeltaZ;
      }
    }

  }

  return null;
}

// Returns a human-readable representation of a block-file mapping
function formatMappingString (entry) {

  // Get block position
  const positionString = entry.pos.toArray().join(" ");
  // Build collapsed path
  const pathParts = entry.file.path.split(path.sep);
  const pathFile = pathParts.pop();
  const pathStart = pathParts.slice(0, parentDepth + 1).join(path.sep);
  const pathEllipses = pathParts.length > (parentDepth + 2) ? "/..." : "";

  return `"${entry.block}" at (${positionString}): "${pathStart}${pathEllipses}/${pathFile}"`

}

console.log("Listening for clipboard changes...");

let clipboardLast = "";
setInterval(async function () {

  const text = await clipboard.default.read();
  if (text === clipboardLast) return;
  clipboardLast = text;

  if (!text.startsWith("/execute in minecraft:overworld run tp @s")) return;

  const [x, y, z, yaw, pitch] = text.split("@s ")[1].split(" ").map(c => Number(c));

  const pos = new Vector(x, y + 1.62, z); // Eye position
  const fvec = Vector.fromAngles(yaw, pitch); // Eye forward vector

  const entry = raycast(pos, fvec);

  if (!entry) {
    console.log("No file associated with this block.");
  } else {
    console.log(formatMappingString(entry));
  }

}, 200);

console.log("Listening for block changes...");

async function checkBlockChanges () {

  // Update region file cache from disk
  await world.fillRegionFileCache(worldPath);

  // Check for changes from expected block states
  await forMappedChunks(async function (blocks, entries, _x, _z, bounds) {
    // Load region data into block array
    await world.forRegion(worldPath, async function (region, rx, rz) {
      await world.regionToBlocks(region, blocks, rx, rz, bounds);
    }, bounds);

    // Look for blocks that don't match the expected mapping
    for (const entry of entries) {

      const [x, y, z] = entry.pos.relative(_x, _z).toArray();
      const block = blocks[x][y][z];

      if (block === entry.block) continue;

      console.log(`Removed ${formatMappingString(entry)}`);
      console.log(` ^ Replaced by "${block}"`);

      const key = entry.pos.toString();
      delete mapping[key];

    }
  });

  // Repeat this check after a delay
  setTimeout(checkBlockChanges, 2000);
}
checkBlockChanges();
