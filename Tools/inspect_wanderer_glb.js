"use strict";

const fs = require("fs");
const path = require("path");

function parseGlb(filePath) {
  const file = fs.readFileSync(filePath);
  let offset = 12;
  let gltf;
  let bin;
  while (offset < file.length) {
    const length = file.readUInt32LE(offset);
    const type = file.readUInt32LE(offset + 4);
    const data = file.subarray(offset + 8, offset + 8 + length);
    if (type === 0x4e4f534a) gltf = JSON.parse(data.toString("utf8").trimEnd());
    if (type === 0x004e4942) bin = data;
    offset += 8 + length;
  }
  if (!gltf || !bin) throw new Error("Invalid GLB: " + filePath);
  return { gltf, bin };
}

function triangleCount(gltf, primitive) {
  if (primitive.indices !== undefined) return gltf.accessors[primitive.indices].count / 3;
  return gltf.accessors[primitive.attributes.POSITION].count / 3;
}

function inventory(filePath) {
  const { gltf } = parseGlb(filePath);
  return {
    file: path.resolve(filePath),
    bytes: fs.statSync(filePath).size,
    scenes: gltf.scenes?.length || 0,
    nodes: (gltf.nodes || []).map((node, index) => ({
      index,
      name: node.name,
      mesh: node.mesh,
      skin: node.skin,
      children: node.children || [],
    })),
    meshes: (gltf.meshes || []).map((mesh, index) => ({
      index,
      name: mesh.name,
      triangles: mesh.primitives.reduce((sum, primitive) => sum + triangleCount(gltf, primitive), 0),
      primitives: mesh.primitives.map((primitive) => ({
        triangles: triangleCount(gltf, primitive),
        material: primitive.material === undefined ? null : gltf.materials?.[primitive.material]?.name,
        attributes: Object.keys(primitive.attributes),
      })),
    })),
    materials: (gltf.materials || []).map((material) => ({
      name: material.name,
      baseColorFactor: material.pbrMetallicRoughness?.baseColorFactor,
      doubleSided: material.doubleSided || false,
    })),
    skins: (gltf.skins || []).map((skin) => ({
      name: skin.name,
      joints: skin.joints.map((nodeIndex) => gltf.nodes[nodeIndex]?.name),
    })),
    animations: (gltf.animations || []).map((animation) => animation.name),
    extras: gltf.asset?.extras || null,
  };
}

const filePath = process.argv[2];
if (!filePath) throw new Error("Usage: node Tools/inspect_wanderer_glb.js <file.glb>");
process.stdout.write(JSON.stringify(inventory(filePath), null, 2) + "\n");
