#!/usr/bin/env node
import { spawnSync } from 'node:child_process';
import { mkdir, copyFile, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { unityEval } from './unity-bridge.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '../..');
const vendor = join(here, 'vendor');
const args = process.argv.slice(2);
function run(file, parameters) {
  const result = spawnSync(process.execPath, [join(vendor, 'tools', file), ...parameters], {
    cwd: vendor, stdio: 'inherit',
  });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${file} failed (${result.status})`);
}

try {
  if (!args.length || args.includes('--help')) {
    console.log('사용법: node Tools/PixelFx/export.mjs <category/name> [--import] [--ppu 100] [--fps 30] [--opts JSON]\n검색: node Tools/PixelFx/export.mjs --list frost\nUnity 실행 중 --import를 사용하면 스프라이트, 애니메이션, 프리팹을 생성합니다.');
    process.exit(0);
  }
  if (args[0] === '--list') {
    run('list.mjs', args.slice(1));
    process.exit(0);
  }
  const id = args.shift();
  if (!/^[a-z0-9-]+\/[a-z0-9-]+$/.test(id)) throw new Error('이펙트 ID는 category/name 형식이어야 합니다.');
  let ppu = 100, shouldImport = false;
  const effectOptions = {};
  const renderArgs = [id, '--sheet', '--no-actor', '--out', join(here, 'records')];
  while (args.length) {
    const flag = args.shift();
    if (flag === '--import') { shouldImport = true; continue; }
    if (!['--ppu', '--fps', '--opts', '--seed'].includes(flag)) throw new Error(`알 수 없는 옵션: ${flag}`);
    const value = args.shift();
    if (value === undefined) throw new Error(`${flag} 값이 필요합니다.`);
    if (flag === '--ppu') {
      ppu = Number(value);
      if (!Number.isFinite(ppu) || ppu <= 0) throw new Error('PPU는 양수여야 합니다.');
    } else {
      if (flag === '--opts') {
        const parsed = JSON.parse(value);
        if (parsed === null || Array.isArray(parsed) || typeof parsed !== 'object') throw new Error('--opts는 JSON 객체여야 합니다.');
      }
      if (flag === '--seed' && !Number.isFinite(Number(value))) throw new Error('seed는 숫자여야 합니다.');
      if (flag === '--fps' && (!Number.isFinite(Number(value)) || Number(value) <= 0 || Number(value) > 120))
        throw new Error('FPS는 0보다 크고 120 이하여야 합니다.');
      effectOptions[flag.slice(2)] = flag === "--opts" ? JSON.parse(value) : Number(value);
      renderArgs.push(flag, value);
    }
  }
  run('render.mjs', renderArgs);
  const name = id.split('/')[1];
  const source = join(here, 'records', id);
  const assetDirectory = `Assets/PixelFx/Sheets/${id}`;
  await mkdir(join(root, assetDirectory), { recursive: true });
  for (const extension of ['png', 'json'])
    await copyFile(join(source, `${name}.${extension}`), join(root, assetDirectory, `${name}.${extension}`));
  await writeFile(join(root, assetDirectory, 'pixelfx-source.txt'),
    `${JSON.stringify({ ...JSON.parse(await readFile(join(here, 'upstream.json'), 'utf8')), id, ppu, renderOptions: effectOptions }, null, 2)}\n`);
  console.log(`Exported: ${assetDirectory}/${name}.png + .json (PPU ${ppu})`);
  console.log(`Preview: ${join(source, 'contact.png')}`);
  if (shouldImport) {
    const template = await readFile(join(here, 'ImportSheet.cs'), 'utf8');
    const job = JSON.stringify({ assetPath: `${assetDirectory}/${name}.png`, ppu });
    const code = template.replace('__PIXELFX_JOB__', () => JSON.stringify(job));
    console.log(await unityEval(root, code));
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
