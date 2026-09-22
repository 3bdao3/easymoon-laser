import { HttpParams } from '@angular/common/http';

type ParamValue = string | number | boolean | null | undefined;

export function toHttpParams(source: object): HttpParams {
  const record = source as Record<string, ParamValue>;
  let params = new HttpParams();
  for (const [key, value] of Object.entries(record)) {
    if (value === undefined || value === null || value === '') {
      continue;
    }
    params = params.set(key, String(value));
  }
  return params;
}
