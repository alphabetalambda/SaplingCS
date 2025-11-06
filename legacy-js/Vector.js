module.exports = class Vector {

  constructor (x = 0, y = 0, z = 0) {
    this.x = x;
    this.y = y;
    this.z = z;
  }

  toArray () {
    return [this.x, this.y, this.z];
  }
  toString () {
    return `${this.x},${this.y},${this.z}`;
  }

  clone () {
    return new Vector(this.x, this.y, this.z);
  }
  add (other, y = null, z = null) {
    if (y !== null && z !== null) {
      return new Vector(this.x + other, this.y + y, this.z + z);
    }
    return new Vector(this.x + other.x, this.y + other.y, this.z + other.z);
  }
  sub (other, y = null, z = null) {
    if (y !== null && z !== null) {
      return new Vector(this.x - other, this.y - y, this.z - z);
    }
    return new Vector(this.x - other.x, this.y - other.y, this.z - other.z);
  }
  length () {
    return Math.sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
  }
  normalize () {
    const length = this.length();
    if (length === 0) return new Vector();
    return new Vector(this.x / length, this.y / length, this.z / length);
  }

  static DIRECTIONS = [
    new Vector(1, 0, 0),
    new Vector(-1, 0, 0),
    new Vector(0, 0, 1),
    new Vector(0, 0, -1),
    new Vector(0, 1, 0),
    new Vector(0, -1, 0)
  ];

  // Shift by one unit in each of six directions
  shifted (dir) {
    return this.add(Vector.DIRECTIONS[dir]);
  }
  // Convert from chunk-relative coordinates to absolute coordinates
  absolute (_x, _z) {
    return this.add(_x * 16, -64, _z * 16);
  }
  // Convert from absolute coordinates to chunk-relative coordinates
  relative (_x, _z) {
    return this.sub(_x * 16, -64, _z * 16);
  }

  // Create a forward vector from Euler angles
  static fromAngles (yaw, pitch) {

    yaw *= Math.PI / 180;
    pitch *= Math.PI / 180;

    const cosPitch = Math.cos(pitch);
    const sinPitch = Math.sin(pitch);
    const cosYaw = Math.cos(yaw);
    const sinYaw = Math.sin(yaw);

    const dx = cosPitch * -sinYaw;
    const dy = -sinPitch;
    const dz = cosPitch * cosYaw;

    return new Vector(dx, dy, dz);

  }

}
