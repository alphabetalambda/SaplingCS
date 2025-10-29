const { $ } = require("bun");

/**
 * Returns a list of PIDs that own a handle to the given file.
 *
 * @param {string} path - Path to file
 * @returns {number[]} - Array of process IDs
 */
async function getHandleOwners (path) {
  try {
    const { stdout, stderr } = await $`lsof -F p "${path}"`.quiet();
    return stdout.trim().split("\n").map(c => parseInt(c.slice(1), 10));
  } catch (e) {
    console.error(e);
    return [];
  }
}

/**
 * Kills the given process by PID
 *
 * @param {number} pid - Process ID
 * @param {number} [signal=9] - Signal number
 */
async function killProcess (pid, signal = 9) {
  await $`kill ${"-" + signal} ${pid}`.quiet();
}

module.exports = {
  getHandleOwners,
  killProcess
};
