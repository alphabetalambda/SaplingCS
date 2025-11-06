const { $ } = require("bun");

/**
 * Returns a list of PIDs that own a handle to the given file.
 *
 * @param {string} path - Path to file
 * @returns {number[]} - Array of process IDs
 */
async function getHandleOwners (path) {
  try {
    if (process.platform === "win32") {
      // Use handle.exe on Windows
      const { stdout, stderr } = await $`handle.exe -p -u "${path}"`.quiet();
      // Parse handle.exe output to extract PIDs
      const pids = [];
      const lines = stdout.trim().split("\n");
      for (const line of lines) {
        if (line.includes(path)) {
          const pidMatch = line.match(/(\d+)/);
          if (pidMatch) {
            pids.push(parseInt(pidMatch[1], 10));
          }
        }
      }
      return pids;
    } else {
      // Use lsof on Unix-like systems
      const { stdout, stderr } = await $`lsof -F p "${path}"`.quiet();
      return stdout.trim().split("\n").map(c => parseInt(c.slice(1), 10));
    }
  } catch (e) {
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
  if (process.platform === "win32") {
    // Use taskkill on Windows
    await $`taskkill /PID ${pid} /F`.quiet();
  } else {
    // Use kill on Unix-like systems
    await $`kill ${"-" + signal} ${pid}`.quiet();
  }
}

module.exports = {
  getHandleOwners,
  killProcess
};
