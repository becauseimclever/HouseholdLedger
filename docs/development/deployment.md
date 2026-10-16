# Self-Hosted Deployment

This is the B01-B04 hosting implementation, not a claim that the first
deployable baseline has completed acceptance. See the
[delivery status](delivery-plan.md#implementation-and-evidence).

## Topology and Secrets

The [Dockerfile](../../Dockerfile) publishes the API and Blazor WebAssembly
assets together, using .NET SDK 10.0.402 and ASP.NET Core 10.0.13. The runtime
uses the non-root `app` user. PostgreSQL 18 and the application have separate
containers in [Compose](../../compose.yaml). The database has no published
port. The application binds host loopback port 8080; put an HTTPS reverse proxy
on that same host. Authentik is an independently managed service, not included
in this Compose stack.

Both Linux AMD64 and ARM64 images can be built. ARM64 emulation is not evidence
that the application, database, and identity provider fit on a Pi 4 with 2 GB.
Physical hardware, realistic data volumes, and the final service placement still
require qualification.

Copy [.env.example](../../.env.example) to an ignored `.env`, replacing only
non-secret configuration and absolute secret-file paths. Keep the files outside
the checkout and restrict access to the operator and container runtime:

- PostgreSQL password: one nonempty password.
- Application connection: an Npgsql connection string using `Host=database`,
  database/user `householdledger`, and the same password.
- OIDC client secret: the confidential provider application's secret.

Do not print these files, pass real secrets on command lines, publish resolved
Compose configuration, or put credentials in frontend settings. Compose mounts
the application secrets read-only under `/run/secrets`; the app reads the
explicit `ConnectionStrings:HouseholdLedgerFile` and
`Authentication:ClientSecretFile` settings. The non-root application user must
be able to read these mounted files. Compose file-backed secrets are not an
encrypted secret store; protect their host storage and backups accordingly.

## Authentik and HTTPS

1. Create a confidential OAuth2/OpenID Connect provider and associated
   application in Authentik. Enable authorization-code flow; the client uses
   PKCE and requests only `openid`.
2. Register the exact redirect URI
   `https://ledger.home.example/signin-oidc`, replacing the example hostname.
   Do not use a wildcard redirect URI. Set the client ID, secret file, and
   HTTPS issuer URL in the deployment configuration. Use the issuer published
   in that application's discovery document, including its trailing slash.
3. Obtain the intended owner's exact OIDC `sub` through a trusted provider
   administrative interface and configure `HL_OWNER_SUBJECT`. This is not an
   email/name match. The application validates issuer and subject. Another
   authenticated identity cannot access the ledger.
4. Set `HL_HOSTNAME` to the public hostname. Configure the TLS reverse proxy
   to preserve the original Host and forward the original scheme/client
   address. Set `HL_PROXY_IP` to the proxy source IP actually seen by the
   container; do not trust arbitrary forwarding sources. A containerized
   reverse proxy requires an intentionally designed network/port arrangement
   rather than this host-loopback recipe.
5. The API container must be able to resolve and securely reach the issuer.
   Install a private CA in the image through a reviewed image customization
   if needed; never disable certificate or issuer verification.

Non-Development startup fails without the authentication settings. Development
alone may omit authentication; never deploy with `Development` to avoid login.
The owner receives an HttpOnly, Secure, same-site session cookie with a fixed
eight-hour lifetime. Tokens and client secrets are not delivered to WASM.
Static assets, OpenAPI, and health checks are public but contain no ledger data.
Anonymous ledger API calls return 401; another identity returns 403.

The browser obtains a no-cache antiforgery request token from `/auth/session`
before each mutation and sends `X-CSRF-TOKEN`. Failed session verification stops
the mutation; it does not retry financial writes. The session/sign-out page
submits a verified POST. Logout ends the local ledger session, not the
Authentik session; signing in again may reuse the provider's existing session.

If an already-loaded browser receives 401 or 403, it shows a sign-in notice
instead of automatically navigating or replaying requests. Pending edits in an
existing workspace remain available to copy before signing in; clicking Sign in
performs a full-document navigation. An unauthorized initial settings load shows
recovery without mounting the protected workspace. These responses indicate lost
access, not a server-side 500; other HTTP failures retain their existing handling.

This slice authenticates the hosted browser client. It does not introduce
machine bearer credentials or cross-origin cookie sessions. Future mobile/MCP
adapters need an explicit authentication design while reusing backend use cases;
business rules remain independent of the browser and OIDC transport.

## Fresh Deployment and Migrations

From the repository root, with Docker Compose v2 and the reviewed configuration:

```powershell
docker compose build ledger
docker compose up -d database
docker compose run --rm ledger --migrate-database
docker compose up -d ledger
docker compose ps
```

Check every exit code before proceeding. The migration command applies checked
EF Core migrations and exits; it adds no sample records. Ordinary startup never
migrates or seeds. Do not use `--initialize-development-database` in a real
deployment.

Liveness is `/api/v1/health`; readiness is `/health/ready`. Readiness is 503
without a usable database or while migrations are pending, and 200 after the
schema is current. Container health uses the latter. Neither health endpoint
proves that the identity provider or a browser login works.

The named `postgres_data` volume retains PostgreSQL data across container
replacement. `protection_keys` retains ASP.NET Core Data Protection keys at
`/var/lib/householdledger/keys`, preserving cookie validation across app
replacement. Those keys are not encrypted at rest by this recipe. Protect
their host volume and access as sensitive material; losing them invalidates
sessions, while disclosure compromises session protection.

## Backup, Restore, and Upgrades

Before real data or an upgrade, take an operator-owned backup outside the
containers. Do not rely on retaining the database volume as your only backup.
The following uses PostgreSQL tools inside the database container and avoids
binary redirection through the shell:

```powershell
docker compose exec -T database pg_dump -U householdledger -d householdledger --format=custom --file=/tmp/ledger-backup.dump
docker compose cp database:/tmp/ledger-backup.dump .\private-backups\ledger-backup.dump
docker compose exec -T database rm /tmp/ledger-backup.dump
```

Create the restricted backup directory outside the checkout first and substitute
its absolute path. Use unique filenames; do not overwrite your only known-good
backup. Check command success before removing the container-local copy. Encrypt
and protect database backups, secret files, deployment settings, and any key
volume backup; keep a separate copy appropriate to the household's recovery needs.

Test restoration into a **new, disposable database**, never on top of a live
ledger:

```powershell
docker compose exec -T database createdb -U householdledger householdledger_restore
docker compose cp .\private-backups\ledger-backup.dump database:/tmp/ledger-restore.dump
docker compose exec -T database pg_restore -U householdledger -d householdledger_restore --exit-on-error /tmp/ledger-restore.dump
docker compose exec -T database rm /tmp/ledger-restore.dump
```

Point a separately isolated application instance at that restored database,
using the same authentication safeguards. Confirm readiness, sign in, and compare
known records and totals. Document the result and recovery time. The automated
container smoke validates this dump/restore mechanism against disposable fixture
data, not the operator's actual backup files or storage.

For upgrades, back up first, review migration compatibility, stop ledger writes,
build or obtain the reviewed replacement image, run its explicit migration
command, then recreate the ledger container and verify readiness plus login.
Do not remove named volumes, use `down --volumes`, or assume an older image can
read a newer schema. Container image rollback is not database rollback. Prefer
forward-compatible repair; restoring a backup is a separate, explicitly
authorized recovery action with possible loss of subsequent writes.

Identity-provider recovery is an operator configuration task: restore the
provider or deliberately rebind the trusted issuer/owner and restart the host.
There is no unauthenticated web bypass. Changing the issuer invalidates the
old owner's authorization; protect configuration changes and force sign-in.

## Executable Evidence and Remaining Acceptance

```powershell
pwsh scripts\ci\Test-PullRequest.ps1
pwsh tests\HouseholdLedger.Infrastructure.IntegrationTests\Run-PostgreSqlTests.ps1
pwsh scripts\deployment\Test-ContainerBaseline.ps1
```

The container smoke owns isolated containers, volumes, network, and generated
test credentials, then removes them. It checks pre-migration readiness failure,
explicit migration, anonymous API rejection, seven fixture records surviving
app/database recreation, dump/restore, and restored-host readiness. It does not
authenticate against the placeholder issuer.

Before marking B04 verified, also record:

- Required PR/check rules and remote CI evidence are recorded in the
  [delivery plan](delivery-plan.md#implementation-and-evidence), including the
  completed failed-check merge-block probe. No remote rules are changed by
  deployment scripts; future PRs must still pass their own required checks.
- Real Authentik owner login, rejection of a different account, keyboard logout
  and sign-in, and creation/correction/refresh of persistent calendar activity
  through the assembled production deployment.
- The Pi 4 2 GB service topology, resource observations, and dated supported
  browser/version/platform qualification.
- A successful operator backup restoration before trusting real household data.

The signed-token OIDC middleware fixture and Development browser regression are
valuable separate evidence, but neither replaces the real production login
journey.

The repaired notices journey and the other three system/browser cases pass
using separate disposable database runs, as recorded in the
[local verification results](delivery-plan.md#implementation-and-evidence).
The fresh-setup journey must run separately from cases that leave ledger data.
These Development browser checks do not replace production acceptance.
