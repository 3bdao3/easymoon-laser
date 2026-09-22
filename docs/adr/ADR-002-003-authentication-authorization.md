# ADR-002 / ADR-003 — Authentication & Authorization (Implemented)

## Status

Accepted — implemented in Administration module (Step 3).

## Context

Staff-only clinic ERP API needs secure login, short-lived access tokens, refresh rotation, and fine-grained permissions without microservices or an external IdP in V1.

## Decision

1. **Authentication:** ASP.NET Core **IdentityCore** + **JWT bearer** access tokens + **hashed rotating refresh tokens**.
2. **Authorization:** data-driven **Permission** entities linked to Identity **Roles** via **RolePermission**; enforce with `[HasPermission("...")]` dynamic policies.
3. **Current user:** `ICurrentUser` in BuildingBlocks; ASP.NET adapter in BuildingBlocks.AspNetCore.
4. **Ownership:** Administration module owns Identity/users/roles/permissions/refresh tokens.

## Alternatives considered

- External IdP (Auth0/Entra/Keycloak) — deferred; adds cost/complexity for single-org V1
- Role-only `[Authorize(Roles=...)]` — too coarse for PRD permission model
- Full `AddIdentity` cookie stack — conflicts with JWT-default API authentication

## Consequences

- Permissions must be seeded/managed in DB
- Access tokens cannot be revoked instantly; keep TTL short and revoke refresh tokens on logout
- External IdP can be added later without changing permission model
