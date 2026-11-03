import { readFile } from 'node:fs/promises';
import { join } from 'node:path';

export async function unityEval(root, code, timeout = 120000) {
  let descriptor;
  try {
    descriptor = JSON.parse(await readFile(join(root, 'Library/Pipeline/.unity-pipeline-port'), 'utf8'));
  } catch {
    throw new Error('Unity에서 이 프로젝트를 열고 Window > Pipeline > Start Server를 실행하세요.');
  }
  const response = await fetch(`http://127.0.0.1:${descriptor.port}/api/exec`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${descriptor.evalToken}` },
    body: JSON.stringify({ command: 'eval', parameters: { code, timeout }, timeout }),
    signal: AbortSignal.timeout(timeout + 15000),
  });
  const result = await response.json();
  if (!response.ok || result.success === false || result.result?.success === false) {
    const detail = result.result ?? result;
    throw new Error(JSON.stringify({ error: detail.error ?? result.error, details: detail.errorDetails, diagnostics: detail.diagnostics }));
  }
  return result.result?.result ?? result.result ?? result;
}
