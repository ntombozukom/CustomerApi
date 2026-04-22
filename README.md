# CustomerApi
A production-oriented RESTful Customer Management API built with **.NET 9 Clean Architecture**, designed to demonstrate real-world backend engineering principles including security, scalability, caching, containerisation, and cloud deployment readiness.

This project focuses on **production-style architecture and trade-off awareness**, not just CRUD implementation.

---
## Architecture Overview
The system follows **Clean Architecture principles**:

- **Domain Layer** → Core business entities and rules
- **Application Layer** → Business logic, validation, and service orchestration
- **Infrastructure Layer** → EF Core, Redis caching, encryption, external services
- **API Layer** → Controllers, middleware, request/response handling

### Design Principles
- Separation of concerns for maintainability and testability
- Stateless API design for horizontal scaling
- Infrastructure abstraction (database, cache, encryption)
- Secure handling of PII data
- Cloud-native container-first deployment model

---
## Key Features
- Full CRUD operations for Customer entity
- Paging and filtering (FirstName)
- Email uniqueness enforcement
- Proper HTTP status code mapping
- Global exception handling with RFC 7807 Problem Details

---
## Security
### Authentication
- HTTP Basic Authentication implemented via custom middleware

> Note: Basic Auth is used for assessment purposes. In production systems, this would typically be replaced with OAuth2 / OpenID Connect (e.g. Azure AD, Auth0).

---
### PII Encryption Strategy
Sensitive fields are protected using:

- AES-256 encryption via EF Core value converters:
  - FirstName
  - LastName
  - Email

---
## Caching Strategy (Redis)
Redis is used to improve read performance and reduce database load.

### Cached Data
- `GetCustomerById` results cached per customer ID
- TTL-based expiration policy
- Cache invalidation on update and delete operations

### Trade-offs
- Optimised for read performance
- Accepts eventual consistency for cached data

---
## Error Handling
- Global exception handling middleware
- RFC 7807 Problem Details responses

---
## Observability
### Implemented
- Structured logging (ILogger)
- CloudWatch-compatible logs via container stdout/stderr
- Centralised exception logging

### Future Improvements
- OpenTelemetry distributed tracing
- Metrics (latency, throughput, error rates)
- Dashboards (CloudWatch / Grafana)

---
## Scalability
Designed for horizontal scaling:
- Stateless API design
- Docker containerisation
- AWS ECS Fargate compatibility
- Application Load Balancer (ALB) support

### Scaling Strategy (AWS)
- ECS Service Auto Scaling:
  - CPU utilisation thresholds
  - Request count per target
- Multi-AZ deployment support
- Database scaling via RDS configuration

---
## Database Design
- PostgreSQL via EF Core
- Indexed hashed fields for performance:
  - FirstNameHash
  - LastNameHash
  - EmailHash (unique index)

### Migration Strategy
- Automatic migrations on startup (development only)
- Production systems should use CI/CD-controlled migrations

---
## Testing
- 17+ unit tests using xUnit, Moq, FluentAssertions

### Coverage
- Service layer business logic
- CRUD operations
- Validation rules
- Duplicate handling
- Not-found scenarios
- Paging and filtering

### Strategy
- Unit tests for isolated business rules
- Mocked dependencies for external concerns

---
## Containerisation
- Multi-stage Docker build
- Docker Compose includes:
  - API
  - PostgreSQL
  - Redis

### Goal
- One-command local environment setup
- Production parity between local and cloud environments

---
## API Documentation
- Swagger / OpenAPI (Swashbuckle)
- Interactive API testing
- Authentication documented in UI
- Full request/response schemas

---
## Cloud Deployment (AWS Reference Architecture)
Designed for deployment on **AWS ECS Fargate (serverless containers)**
| Component          | AWS Service              |
|--------------------|--------------------------|
| Compute            | ECS Fargate              |
| Container Registry | Amazon ECR               |
| Database           | Amazon RDS PostgreSQL    |
| Cache              | Amazon ElastiCache Redis |
| Load Balancer      | Application Load Balancer|
| Secrets            | AWS Secrets Manager      |
| Logging            | CloudWatch               |

---
## Running the Application
```bash
docker compose up --build