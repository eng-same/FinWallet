# FinWallet — Digital Wallet Simulation System

FinWallet is an academic digital wallet simulation platform built for a software development university course. It demonstrates architectural and design patterns including:
1. **Modular Monolithic API** (.NET 10 Web API + Entity Framework Core).
2. **Clean Architecture / Domain-Driven Design (DDD)** structure.
3. **Event-Driven Integration** using MassTransit and RabbitMQ.
4. **Relational Schema Separation** in PostgreSQL.
5. **Modern Single Page Application** (Vue 3, Pinia, TypeScript, Quasar Framework).
6. **Double-Entry Ledger Accounting** for financial transactions.
7. **Compliance Auditing** with full system audit logs.

---

## 1. Quickstart (Run with Docker Compose)

The entire system (PostgreSQL database, RabbitMQ message broker, backend API, mock bank worker service, and Vue 3 frontend) is fully containerized.

To start the system:
1. Clone or open the repository.
2. In the root directory, run:
   ```bash
   docker compose up --build
   ```
3. Once all containers show as healthy, open the client application at:
   - **Web UI Client**: [http://localhost:9000](http://localhost:9000)

---

## 2. Default Access Credentials & Endpoints

### Default Administrator Account
Log in as the administrator to monitor system stats, audit ledger activity, view user transactions, and freeze/unfreeze wallets:
- **Email**: `admin@finwallet.local`
- **Password**: `Admin@123456`

### Access Ports & Services
When running Docker Compose, the following ports are mapped on your host machine:

| Port | Service Component | Description | Local URL |
|---|---|---|---|
| **9000** | `finwallet-frontend` | Vue 3 client served via Nginx. | [http://localhost:9000](http://localhost:9000) |
| **8081** | `finwallet-api` | ASP.NET Core REST API. | [http://localhost:8081](http://localhost:8081) |
| **16686**| `finwallet-jaeger` | Jaeger distributed tracing UI. | [http://localhost:16686](http://localhost:16686) |
| **15672**| `finwallet-rabbitmq` | RabbitMQ Management Console. | [http://localhost:15672](http://localhost:15672)<br>User: `finwallet_mq_user`<br>Pass: `finwallet_mq_password` |
| **5432** | `finwallet-postgres` | PostgreSQL Database Server. | Host: `localhost`, Port: `5432`<br>DB: `finwallet_db`<br>User: `finwallet_user`<br>Pass: `finwallet_password` |

---

## 3. Project Directory Structure
```text
university project/
├── src/
│   ├── backend/
│   │   ├── FinWallet.slnx                    # .NET 10 solution manifest
│   │   ├── FinWallet.Domain/                 # Pure domain business rules & entities
│   │   ├── FinWallet.Contracts/              # RabbitMQ shared integration events
│   │   ├── FinWallet.Application/            # MediatR commands, queries & validators
│   │   ├── FinWallet.Infrastructure/          # DbContext, DB schema mapping & seed
│   │   ├── FinWallet.Api/                    # Controllers & Auth Middleware
│   │   ├── FinWallet.MockBankCore.Worker/    # Asynchronous external bank worker
│   │   └── FinWallet.Tests/                  # Automated xUnit integration tests
│   │
│   └── frontend/
│       └── finwallet-web/
│           ├── src/
│           │   ├── views/                    # Views (Client Dashboard, Admin Management)
│           │   ├── stores/                   # State management (Pinia)
│           │   └── router/                   # Client-side router & navigation guards
│           ├── Dockerfile                    # Multi-stage production build config
│           └── nginx.conf                    # Nginx config supporting routing fallback
│
├── docs/                                     # System documentation files
│   ├── 01-project-scope.md
│   ├── 02-architecture.md
│   ├── 03-frontend-pages-and-api-contracts.md
│   ├── 04-backend-design-and-database-schema.md
│   ├── 05-uml-text-specification.md
│   ├── 06-testing-plan.md
│   ├── 07-demo-script.md
│   └── 08-observability.md               # OpenTelemetry & Jaeger observability guide
│
├── docker-compose.yml                        # Docker services orchestrator
├── docker-compose.override.yml               # Dev overrides configuration
├── README.md                                 # This quickstart document
└── .gitignore
```

---

## 4. Running Backend Tests Locally
To execute the automated xUnit business boundary tests without running docker containers:
1. Navigate to the backend directory:
   ```bash
   cd src/backend
   ```
2. Run tests:
   ```bash
   dotnet test FinWallet.slnx
   ```

---

## 5. Troubleshooting & FAQ

### Port Conflict Issues
- **Error**: `port is already allocated` or `bind: address already in use`.
- **Solution**: Check if you have local PostgreSQL (5432) or RabbitMQ (5672/15672) running. Terminate them before executing `docker compose up`.

### Rebuilding Containers
If you make code edits and wish to force-rebuild the containers, run:
```bash
docker compose down -v
docker compose up --build
```
The `-v` flag removes the named database volumes to force EF Core's database seeder to run again on clean databases.

---

## 6. Distributed Tracing & Observability

FinWallet includes built-in distributed tracing via OpenTelemetry and Jaeger. Traces are collected automatically when running with Docker Compose.

- **Jaeger UI**: [http://localhost:16686](http://localhost:16686)
- **Full guide**: [`docs/08-observability.md`](docs/08-observability.md)
