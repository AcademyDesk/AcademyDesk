'use strict';
// Exclusive cross-wrapper coordination. A crash leaves the lock for explicit
// inspection; never guess ownership, delete a stale lease or kill its process.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
function acquire(directory,scope){
 a.equal(typeof scope,'string');a.match(scope,/^[a-z-]{1,50}$/);const base=path.resolve(directory);a.equal(fs.realpathSync(base).toLowerCase(),base.toLowerCase());a.equal(fs.lstatSync(base).isSymbolicLink(),false);const file=path.join(base,'qa-validation-window.lock');
 const owner={version:1,pid:process.pid,scope,nonce:crypto.randomUUID(),acquiredAtUtc:new Date().toISOString()},bytes=Buffer.from(JSON.stringify(owner));let fd;
 try{fd=fs.openSync(file,'wx',0o600);}catch{throw Error('Another local validation window or retained lease exists; no services changed. Inspect ownership, never auto-delete.');}
 try{fs.writeFileSync(fd,bytes);fs.fsyncSync(fd);}finally{fs.closeSync(fd);}let released=false;
 return {owner,release(){a.equal(released,false,'Lease was already released.');a.equal(fs.lstatSync(file).isSymbolicLink(),false);a.equal(fs.realpathSync(file).toLowerCase(),file.toLowerCase());a.deepEqual(fs.readFileSync(file),bytes,'Foreign or changed lease cannot be removed.');fs.unlinkSync(file);released=true;}};
}
async function withLease(directory,scope,work){const lease=acquire(directory,scope);try{return await work();}finally{lease.release();}}
module.exports={acquire,withLease};
