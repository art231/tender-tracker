#!/usr/bin/env node

const fs = require('fs-extra');
const path = require('path');

const libsDir = path.join(__dirname, 'src', 'libs');

// Ensure libs directory exists
fs.ensureDirSync(libsDir);

// Copy libraries from node_modules to src/libs
const libraries = [
  {
    from: 'bootstrap/dist',
    to: 'bootstrap/dist'
  },
  {
    from: 'jquery/dist',
    to: 'jquery/dist'
  },
  {
    from: 'angular',
    to: 'angular'
  },
  {
    from: 'rxjs/bundles',
    to: 'rxjs/bundles'
  },
  {
    from: 'bootstrap-icons/font',
    to: 'bootstrap-icons/font'
  }
];

console.log('Copying libraries to src/libs...');

libraries.forEach(lib => {
  const src = path.join(__dirname, 'node_modules', lib.from);
  const dest = path.join(libsDir, lib.to);
  
  if (fs.existsSync(src)) {
    fs.copySync(src, dest, { overwrite: true });
    console.log(`✓ Copied ${lib.from} to ${lib.to}`);
  } else {
    console.error(`✗ Source not found: ${src}`);
  }
});

console.log('Library preparation complete!');
