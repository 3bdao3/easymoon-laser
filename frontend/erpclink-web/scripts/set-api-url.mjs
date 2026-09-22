/**
 * Writes production environment.apiBaseUrl from API_BASE_URL (Render build env).
 * Usage: API_BASE_URL=https://xxx.onrender.com node scripts/set-api-url.mjs
 */
import { writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const target = join(__dirname, '..', 'src', 'environments', 'environment.ts');

const raw = (process.env.API_BASE_URL || '').trim().replace(/\/+$/, '');

if (!raw) {
  console.warn(
    '[set-api-url] API_BASE_URL is empty. Production build will use apiBaseUrl="". Set API_BASE_URL on Render before building.'
  );
}

const contents = `export const environment = {
  production: true,
  apiBaseUrl: ${JSON.stringify(raw)}
};
`;

writeFileSync(target, contents, 'utf8');
console.log(`[set-api-url] Wrote apiBaseUrl=${JSON.stringify(raw)} to src/environments/environment.ts`);
