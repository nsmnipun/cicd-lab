# Notes app (CI/CD learning project)

A small Notes app used to practice CI/CD. Keep the app simple; the pipeline is the point.

## Layout
- api/  .NET 10 Web API: api/src/Notes.Api, api/tests/Notes.Api.Tests, one solution file in api/
- ui/   React + Vite + TypeScript
- Dockerfiles, compose files and CI config are written by hand. Don't create or edit them unless asked.

## Rules every change must follow
- All configuration comes from environment variables. The database connection string is
  ConnectionStrings__Notes. Never put secrets in code or appsettings files.
- In containers the API listens on port 8080 (the .NET image default). Locally it runs on http://localhost:5080.
- All API routes live under /api (for example /api/notes).
- Health endpoints: GET /health/live (process is up, no dependencies checked) and
  GET /health/ready (database is reachable).
- Logging: structured JSON to stdout only. No log files.
- Telemetry: OpenTelemetry traces, metrics and logs exported over OTLP, configured only through the
  standard OTEL_* environment variables. If OTEL_EXPORTER_OTLP_ENDPOINT is not set, skip exporting.
- Database migrations: EF Core migrations. Never apply them on normal startup.
  Running the app with the single argument `migrate` applies pending migrations and exits with code 0.
- The UI calls the API with relative URLs under /api. In development, Vite proxies /api to http://localhost:5080.
- Every feature ships with tests. Keep tests fast and independent of each other.

## Commands
- API tests: dotnet test api
- UI tests (watch): cd ui && npm test
- UI tests (CI, writes JUnit XML to ui/test-results/junit.xml): cd ui && npm run test:ci
- UI build: cd ui && npm run build