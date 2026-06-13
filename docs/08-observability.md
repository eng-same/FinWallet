# 08 — Observability: OpenTelemetry & Jaeger

## 1. Overview

### What is OpenTelemetry?

[OpenTelemetry](https://opentelemetry.io/) (OTel) is a vendor-neutral, CNCF-graduated observability framework that provides a standard API and SDK for collecting **distributed traces**, **metrics**, and **logs** from applications. It removes vendor lock-in by separating signal collection from signal export — you instrument once and route to any compatible backend.

FinWallet uses OpenTelemetry to capture:

| Signal | What it records |
|---|---|
| HTTP spans | Every incoming API request and its duration |
| HTTP client spans | Any outgoing HTTP calls made by the application |
| Database spans | Every SQL query executed via Entity Framework Core / Npgsql |
| Messaging spans | RabbitMQ publish and consume events via MassTransit |
| Exception events | Unhandled exceptions attached to the active span |

### What is Jaeger?

[Jaeger](https://www.jaegertracing.io/) is an open-source, end-to-end distributed tracing platform originally developed by Uber. It stores and visualises traces — allowing you to follow a single request as it travels through multiple services, see timing breakdowns, and diagnose latency or errors.

FinWallet uses the **`jaegertracing/all-in-one`** Docker image, which bundles the collector, storage backend (in-memory), and UI into a single container — ideal for development and staging environments.

### Why does FinWallet use them?

FinWallet is an event-driven system. A single user-facing action (e.g., initiating a bank top-up) spawns an HTTP request to the API, a RabbitMQ message to the Worker, a processing step, a response message back, and a database write. Without distributed tracing it is impossible to:

- Correlate the full journey of a request across services.
- Identify which step introduced a latency spike.
- Pinpoint the exact span where an error occurred.

---

## 2. Local Development

### Starting the observability stack

Jaeger is included in the default `docker-compose.yml` and starts automatically:

```bash
# From the repository root
docker compose -f docker/docker-compose.yml up --build
```

No additional steps are required — Jaeger starts alongside PostgreSQL, RabbitMQ, the API, and the Worker.

### Accessing the Jaeger UI

Once all containers are running, open:

**http://localhost:16686**

> The port is configurable via `JAEGER_UI_PORT` in `docker/.env` (default: `16686`).

### Verifying traces are being collected

1. Open the Jaeger UI at `http://localhost:16686`.
2. In the **Service** dropdown, select `FinWallet.Api` or `FinWallet.MockBankCore.Worker`.
3. Click **Find Traces**.
4. Make an API request — for example, a login:
   ```bash
   curl -X POST http://localhost:8081/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@finwallet.local","password":"Admin@123456"}'
   ```
5. Refresh the Jaeger search — a new trace should appear within a few seconds.
6. Click the trace to inspect its spans:
   - An **HTTP POST /api/auth/login** root span.
   - One or more **database (SELECT/INSERT)** child spans.

### Configuration for local development

Observability is **enabled by default** in the Development environment. The relevant settings are:

| Variable | Value (local) | Source |
|---|---|---|
| `Observability__Enabled` | `true` | `docker-compose.yml` via `OBSERVABILITY_ENABLED` in `.env` |
| `Observability__ServiceName` | `FinWallet.Api` / `FinWallet.MockBankCore.Worker` | `docker-compose.yml` |
| `Observability__OtlpEndpoint` | `http://finwallet-jaeger:4317` | `docker-compose.yml` |

To **disable** tracing locally, set `OBSERVABILITY_ENABLED=false` in `docker/.env` and restart the stack.

---

## 3. Staging

### How staging observability works

The `docker-compose.staging.yml` file includes the same `finwallet-jaeger` service. In staging, tracing is **disabled by default** (`OBSERVABILITY_ENABLED=false`) to keep the environment conservative. Set it to `true` to activate tracing.

### Required environment variables

Add the following to your staging `.env` (or CI/CD secret store) to enable tracing:

```dotenv
# Enable distributed tracing
OBSERVABILITY_ENABLED=true

# Jaeger port configuration (optional — defaults match Jaeger standards)
JAEGER_UI_PORT=16686
JAEGER_OTLP_GRPC_PORT=4317
```

> The `Observability__ServiceName` and `Observability__OtlpEndpoint` are set directly in `docker-compose.staging.yml` and do not need to be overridden in `.env`.

### Deploying to staging

```bash
docker compose -f docker/docker-compose.staging.yml --env-file docker/.env up -d
```

### Verification steps (staging)

1. SSH into the staging host (or use your CI runner).
2. Check that all containers are running:
   ```bash
   docker compose -f docker/docker-compose.staging.yml ps
   ```
3. Confirm `finwallet-jaeger-staging` is `Up`.
4. Access the Jaeger UI at `http://<staging-host-ip>:16686`.
5. Make a test API request to the staging API and verify a trace appears.

> **Security note**: Consider placing Jaeger behind an authenticated reverse proxy (e.g., Nginx with Basic Auth) in staging environments accessible from the internet.

---

## 4. Configuration Reference

### `Observability` configuration section

Both `FinWallet.Api` and `FinWallet.MockBankCore.Worker` support this section in `appsettings.json`:

```json
{
  "Observability": {
    "Enabled": false,
    "ServiceName": "FinWallet.Api",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

| Key | Description | Default |
|---|---|---|
| `Enabled` | Whether to activate OTel tracing. | `false` |
| `ServiceName` | The service name shown in Jaeger. | `FinWallet.Api` or `FinWallet.MockBankCore.Worker` |
| `OtlpEndpoint` | gRPC endpoint for the OTLP exporter. | `http://localhost:4317` |

### Environment variable overrides

All keys follow the standard .NET double-underscore separator convention:

```bash
Observability__Enabled=true
Observability__ServiceName=FinWallet.Api
Observability__OtlpEndpoint=http://finwallet-jaeger:4317
```

---

## 5. What is Instrumented

| Instrumentation | Library | Spans Generated |
|---|---|---|
| Incoming HTTP requests | `OpenTelemetry.Instrumentation.AspNetCore` | One root span per request |
| Outgoing HTTP calls | `OpenTelemetry.Instrumentation.Http` | One child span per outbound call |
| EF Core / PostgreSQL | `OpenTelemetry.Instrumentation.EntityFrameworkCore` | One span per SQL command, including the SQL statement |
| MassTransit / RabbitMQ | `MassTransit.OpenTelemetry` | Publish and consume spans, with trace context propagated across the message broker |
| Exceptions | `System.Diagnostics.Activity` (BCL) | Exception recorded on the active span via `ExceptionHandlingMiddleware` |

---

## 6. Troubleshooting

### No traces appearing in Jaeger

**Check 1 — Is observability enabled?**

```bash
# For local dev
docker compose -f docker/docker-compose.yml exec finwallet-api \
  printenv | grep Observability
```
Expected output:
```
Observability__Enabled=true
Observability__OtlpEndpoint=http://finwallet-jaeger:4317
```
If `Enabled` is `false`, set `OBSERVABILITY_ENABLED=true` in `docker/.env` and restart.

**Check 2 — Is Jaeger running?**

```bash
docker compose -f docker/docker-compose.yml ps finwallet-jaeger
```
The container should show `Up`. If it exited, check logs:
```bash
docker compose -f docker/docker-compose.yml logs finwallet-jaeger
```

**Check 3 — Can the API reach Jaeger?**

```bash
docker compose -f docker/docker-compose.yml exec finwallet-api \
  wget -qO- http://finwallet-jaeger:16686
```
A valid HTML response confirms network connectivity to Jaeger.

---

### Connectivity problems between app and Jaeger

All services must be on the same Docker network (`finwallet-network`). Verify:

```bash
docker network inspect docker_finwallet-network
```

Ensure `finwallet-api`, `finwallet-mock-bank-core`, and `finwallet-jaeger` all appear as connected containers.

If a service was started outside of Docker Compose (e.g., running the API locally with `dotnet run`), it cannot reach `http://finwallet-jaeger:4317`. Use `http://localhost:4317` in `appsettings.Development.json` in that case.

---

### Misconfigured OTLP exporter

Symptoms: traces never reach Jaeger even though the API logs show requests being processed.

- Ensure `COLLECTOR_OTLP_ENABLED=true` is set on the `finwallet-jaeger` container (it is set in the compose file).
- Verify the endpoint uses `http://` (not `https://`) — the OTLP gRPC port `4317` does not use TLS in the default Jaeger all-in-one image.
- Check API logs for any OTel export errors:
  ```bash
  docker compose -f docker/docker-compose.yml logs finwallet-api | grep -i otel
  ```

---

### Traces appear but spans are missing

- **No database spans**: Ensure the `OpenTelemetry.Instrumentation.EntityFrameworkCore` package is installed and `AddEntityFrameworkCoreInstrumentation()` is called. Check `FinWallet.Infrastructure.csproj`.
- **No MassTransit spans**: Ensure `MassTransit.OpenTelemetry` is installed in both projects and `.AddSource("MassTransit")` is in the tracer builder.
- **No Worker traces**: Confirm `Observability__Enabled=true` is set for `finwallet-mock-bank-core` in Docker Compose.

---

### Port conflicts on the host

If port `16686` or `4317` is already in use, override in `docker/.env`:

```dotenv
JAEGER_UI_PORT=16687
JAEGER_OTLP_GRPC_PORT=4318
```

---

## 7. Verification Steps

The following steps confirm the full observability pipeline is working end-to-end.

### Step 1 — Start the stack

```bash
docker compose -f docker/docker-compose.yml up --build
```

Wait until all containers report healthy/started.

### Step 2 — Open Jaeger UI

Navigate to `http://localhost:16686`. The Jaeger search page should load.

### Step 3 — Generate an HTTP trace

```bash
curl -X POST http://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@finwallet.local","password":"Admin@123456"}'
```

In Jaeger UI -> Service: `FinWallet.Api` -> **Find Traces**. A trace named `POST /api/auth/login` should appear.

Click it and verify:
- Root span: `POST /api/auth/login`
- Child span(s): EF Core SQL queries (e.g., `SELECT` from the Users table)

### Step 4 — Verify trace propagation across services

1. Log in and obtain a JWT token from the previous step.
2. Initiate a bank top-up (or any operation that publishes a RabbitMQ message).
3. In Jaeger, search for traces in `FinWallet.Api`.
4. Open the trace and look for a `MassTransit: send finwallet.bank.topup.requests` span.
5. Separately search in `FinWallet.MockBankCore.Worker` — a correlated consume span should appear. Both traces will share the same `traceId`, demonstrating cross-service propagation.

### Step 5 — Verify exception recording

```bash
# Send a request that triggers a validation error
curl -X POST http://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"","password":""}'
```

In Jaeger, open the resulting trace. The span should show:
- Status: `Error`
- An exception event with the error message

### Step 6 — Verify database span content

In any trace involving a database call, click a DB span. The `db.statement` attribute should show the parameterised SQL query (e.g., `SELECT ... FROM "AspNetUsers" WHERE ...`).

---

## 8. Recommended Future Enhancements

| Enhancement | Tool | Benefit |
|---|---|---|
| Metrics collection | OpenTelemetry Metrics + Prometheus | CPU, memory, request rate, error rate dashboards |
| Log correlation | Serilog -> OpenTelemetry Logs | Correlate log entries with trace IDs |
| Persistent Jaeger storage | Jaeger + Elasticsearch or Cassandra | Traces survive container restarts |
| Centralised collection | OpenTelemetry Collector | Decouples app from backend; enables fan-out to multiple backends |
| Dashboards | Grafana | Pre-built dashboards for Jaeger, Prometheus, and logs |
| Log aggregation | Grafana Loki | Centralised log storage with trace ID correlation |
| Alerting | Grafana Alerting | Automated alerts on error rate or latency thresholds |
