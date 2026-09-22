# Angular frontend (foundation only)

Auth foundation TypeScript files live under `src/app/core`.

A full Angular workspace (`ng new`) is intentionally not generated in Step 3.
When scaffolding the Angular app in a later step, copy/wire:

- `core/auth/*`
- `core/interceptors/auth.interceptor.ts`
- `core/guards/auth.guard.ts`

Register `authInterceptor` with `provideHttpClient(withInterceptors([authInterceptor]))`.
