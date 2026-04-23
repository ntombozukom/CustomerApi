# CustomerApi

A production-oriented RESTful Customer Management API built with **.NET 9 Clean Architecture**, demonstrating real-world backend engineering principles: security, caching, PII encryption, containerisation, and cloud deployment readiness.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Environment Variables](#environment-variables)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Running Tests](#running-tests)
- [Architecture](#architecture)
- [Security](#security)
- [Cloud Deployment](#cloud-deployment-aws-reference)

---

## Quick Start

### Prerequisites

| Tool | Minimum Version |
|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x |
| Git | any |

> No .NET SDK required — the build runs entirely inside Docker.

### 1. Clone the repository

```bash
git clone <repository-url>
cd CustomerApi
```

### 2. Configure environment (optional)

Default values are provided for local development. To override, copy `.env.example` and edit:

```bash
cp .env.example .env
```

Edit `.env` with your preferred values — at minimum change `AUTH_PASSWORD` and `ENCRYPTION_KEY` for any shared environment.

### 3. Start all services

```bash
docker compose up --build
```

This starts three containers:
- **api** — the .NET 9 API (port `8080`)
- **db** — PostgreSQL 16 (port `5432`)
- **redis** — Redis 7 (port `6379`)

Database migrations run automatically on first start.

### 4. Access Swagger UI

| Run method | Swagger URL |
|---|---|
| `docker compose up` | `http://localhost:8080/swagger` |
| IDE / `dotnet run` | `http://localhost:5104/swagger` |

Click **Authorize** (padlock icon), enter the credentials below, and use "Try it out" on any endpoint.

| Field | Default Value |
|---|---|
| Username | `<username>` |
| Password | `<password>` |

### 5. Stop

```bash
docker compose down
```

To also remove the database volume:

```bash
docker compose down -v
```

---

## Environment Variables

All variables can be set in `.env` (for local Docker) or as container environment variables (for cloud).

| Variable | Default | Description |
|---|---|---|
| `AUTH_USERNAME` | `admin` | Basic Auth username |
| `AUTH_PASSWORD` | *(see `.env.example`)* | Basic Auth password — **never commit real value** |
| `ENCRYPTION_KEY` | *(see `.env.example`)* | AES-256 key for PII encryption — **never commit real value** |
| `DB_PASSWORD` | *(see `.env.example`)* | PostgreSQL password |
| `Swagger__Title` | `Customer API` | Swagger UI page title |
| `Swagger__ContactName` | — | Contact name shown in Swagger docs |
| `Database__AutoMigrate` | `true` (Development) | Set to `false` to disable auto-migration |

> In production, supply secrets via your platform's secrets manager (e.g., AWS Secrets Manager, Azure Key Vault) — never commit a `.env` file with real credentials.

---

## API Endpoints

Base URL: `http://localhost:8080/api/v1`

| Method | Endpoint | Description | Success Code |
|---|---|---|---|
| `POST` | `/customers` | Create a new customer | `201 Created` |
| `GET` | `/customers/{id}` | Get customer by ID | `200 OK` |
| `GET` | `/customers` | Get all customers (paged, optional filter) | `200 OK` |
| `PUT` | `/customers/{id}` | Update a customer (full replace) | `200 OK` |
| `DELETE` | `/customers/{id}` | Delete a customer | `204 No Content` |

### Query parameters for `GET /customers`

| Parameter | Type | Default | Description |
|---|---|---|---|
| `firstName` | string | — | Filter by first name (exact HMAC match) |
| `page` | int | `1` | Page number (min 1) |
| `pageSize` | int | `10` | Results per page (1–100) |

### Error responses

All errors follow [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807):

| HTTP Code | Scenario |
|---|---|
| `400` | Malformed request body |
| `401` | Missing or invalid Basic Auth credentials |
| `404` | Customer not found |
| `409` | Email already exists |
| `422` | Validation failed (field-level errors returned) |
| `500` | Unexpected server error |

---

## Authentication

All endpoints require **HTTP Basic Authentication**.

### Via Swagger UI

1. Open `http://localhost:8080/swagger` (Docker) or `http://localhost:5104/swagger` (IDE)
2. Click the **Authorize** padlock (top right)
3. Enter `Username` and `Password`
4. Click **Authorize** → **Close**
5. All "Try it out" requests will include the credentials automatically

### Via curl

```bash
curl -u <username>:<password> http://localhost:8080/api/v1/customers
```

### Via .http file (Visual Studio / Rider)

Open `src/CustomerApi.API/CustomerApi.API.http` — all requests are pre-configured with Basic Auth.

> **Production note:** Basic Auth transmits credentials as Base64 over the wire and requires HTTPS to be secure. For public APIs, replace with OAuth2 / JWT Bearer (Azure AD, Auth0, Keycloak).

---

## Running Tests

```bash
dotnet test
```

### Coverage

22 test cases across the service layer:

| Area | Scenarios covered |
|---|---|
| `GetByIdAsync` | Cache hit, cache miss → DB, not found |
| `CreateAsync` | Valid input, duplicate email, 5 invalid input cases |
| `UpdateAsync` | Valid update, not found, email taken, 5 invalid input cases |
| `DeleteAsync` | Exists → deleted + cache evicted, not found |
| `GetAllAsync` | FirstName filter, pagination (page/size/totals) |

---

## Architecture

Clean Architecture — dependencies point inward only:

```
┌─────────────────────────────────────────────────────┐
│  API Layer          Controllers · Middleware · DI   │
├─────────────────────────────────────────────────────┤
│  Application Layer  Services · Validators · DTOs    │
├─────────────────────────────────────────────────────┤
│  Domain Layer       Entities · Interfaces · Exceptions│
├─────────────────────────────────────────────────────┤
│  Infrastructure     EF Core · Redis · Encryption    │
└─────────────────────────────────────────────────────┘
```

### Key design decisions

| Decision | Rationale |
|---|---|
| Cache-aside in service layer | `CustomerCache` is a mechanism; `CustomerService` owns the strategy — correct SRP |
| HMAC-SHA256 hash columns | Filter/search without decrypting every row |
| `IExceptionHandler` (ASP.NET Core 8+) | Preferred over custom middleware — built-in, chainable, recognisable |
| `IOptions<T>` for all config | Strongly typed, validated at startup — no raw string key access in classes |
| `Database:AutoMigrate` config flag | Auto-migrate in dev; CI/CD controls production schema |

---

## Security

### PII Encryption

Sensitive fields encrypted at rest using **AES-256-CBC** via EF Core Value Converters:

| Field | Stored as |
|---|---|
| `FirstName` | AES-256 ciphertext (Base64) |
| `LastName` | AES-256 ciphertext (Base64) |
| `Email` | AES-256 ciphertext (Base64) |

Searchable without decryption using companion **HMAC-SHA256 hash columns** (`FirstNameHash`, `LastNameHash`, `EmailHash`) indexed at the database level.

### Authentication

- HTTP Basic Auth via custom ASP.NET Core middleware
- Constant-time credential comparison (`CryptographicOperations.FixedTimeEquals`) — prevents timing attacks
- Credentials loaded from `IOptions<BasicAuthOptions>` — not hard-coded
- Swagger available in Development environment only

---

## Cloud Deployment (AWS Reference)

Designed for deployment on **AWS ECS Fargate** (serverless containers).

| Component | AWS Service |
|---|---|
| Compute | ECS Fargate |
| Container Registry | Amazon ECR |
| Database | Amazon RDS PostgreSQL |
| Cache | Amazon ElastiCache Redis |
| Load Balancer | Application Load Balancer (ALB) |
| Secrets | AWS Secrets Manager |
| Logging | CloudWatch |

### High-level deployment steps

1. **Push image to ECR**
   ```bash
   aws ecr get-login-password | docker login --username AWS --password-stdin <ecr-url>
   docker build -t customerapi .
   docker tag customerapi:latest <ecr-url>/customerapi:latest
   docker push <ecr-url>/customerapi:latest
   ```

2. **Provision infrastructure** using your IaC tool of choice (CloudFormation / Terraform):
   - RDS PostgreSQL instance (private subnet)
   - ElastiCache Redis cluster (private subnet)
   - ECS Cluster + Task Definition + Service
   - ALB with HTTPS listener (ACM certificate)
   - Security groups: ALB → ECS → RDS/Redis only

3. **Store secrets** in AWS Secrets Manager:
   - `BasicAuth__Password`
   - `Encryption__Key`
   - `ConnectionStrings__DefaultConnection`

4. **Configure Task Definition** to inject secrets as environment variables via `secrets` block — never put real credentials in task definition JSON directly.

5. **Disable auto-migration** in production (`Database__AutoMigrate=false`). Run migrations as a one-off ECS task or as a step in the CI/CD pipeline before deploying the new service version:
   ```bash
   dotnet ef database update --connection "<prod-connection-string>"
   ```

### Scaling

ECS Service Auto Scaling responds to:
- CPU utilisation > 70% → scale out
- Request count per target → scale out
- Multi-AZ deployment for high availability
