# MarketCore - Event-Driven Microservices Marketplace

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Messaging-FF6600)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-336791)
![Redis](https://img.shields.io/badge/Redis-Caching-DC382D)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-Search-005571)

**MarketCore** is a scalable, modular multi-tenant marketplace backend platform built with modern **.NET** technologies. It demonstrates a fully decoupled **Microservices Architecture** using **RabbitMQ** for asynchronous communication, **Redis** for caching, **Elasticsearch** for advanced catalog filtering, and **YARP** as a unified API Gateway.

---

## Architecture & Design

The system follows a strict **Event-Driven Architecture** combined with the **Database per Service** pattern. 

###  Key Architectural Patterns
* **Database per Service** 
* **Event-Driven**
* **Distributed Caching (Redis)** 
* **Full-Text Search (Elasticsearch)**
* **API Gateway (YARP)**

### Building Blocks

* **Core.Messaging**: A robust Event Bus abstraction built on top of the native RabbitMQ Client. It implements standard enterprise patterns including:
  * **Convention-Based Routing**: Uses custom attributes (`[MessageKey]`) to bind routing keys directly to message contracts.
  * **Auto-Discovery**: Uses Reflection and Assembly Scanning to automatically register consumers and handlers.
  * **Resilience**: Includes automated connection retries and dead-letter handling strategies.

* **Core.Domain**: A shared library containing the Result pattern and standardized Domain Errors for consistent error handling across services.
---

## Tech Stack

* **Framework:** ASP.NET Core Web API (.NET 9)
* **Database:** PostgreSQL (Entity Framework Core)
* **Messaging:** RabbitMQ (RabbitMQ.Client / Custom Event Bus)
* **Caching:** Redis (Distributed Caching)
* **Search Engine:** Elasticsearch (Full-text Search, Suggestions, & Faceting)
* **Gateway**: YARP (Yet Another Reverse Proxy)
* **Validation:** FluentValidation
* **Testing:** xUnit, Moq, FluentAssertions, InMemoryDB
* **Containerization:** Docker & Docker Compose

---

## Services Overview

| Service | Responsibility |
|------|---------------|
| **API Gateway** |	Unified entry point (YARP) handling routing, rate limiting, and health checks |
| **Auth Service** | Authentication, authorization, JWT handling |
| **Customer Service** | Customer profiles, preferences, identity data |
| **Store Service** | Store management and ownership |
| **Catalog Service** | Manages products, categories, pricing, and catalog organization |
| **Search Service** | Full-text search and indexing (Elasticsearch) |
| **Cart Service** | Shopping cart lifecycle (Redis-backed) |
| **Order Service** | Order creation, tracking, and lifecycle |
| **Payment Service** | Payment processing and status tracking |
| **Warehouse Service** | Inventory stock levels and reservation |
| **Notification Service** | Email / event-based notifications |

---

## Getting Started

### Prerequisites

Ensure the following tools are installed on your machine:

- .NET SDK 9.0+
- Docker Desktop

---

### Infrastructure Setup

MarketCore relies on several infrastructure services that are provisioned using Docker Compose.

1. Start all required containers:
```bash
docker-compose up -d
```
This spins up Postgres, RabbitMQ, Redis, Elasticsearch, and Kibana.

2. Apply Database Migrations
   
Navigate to each service that uses a database and run:
e.g,
```bash
dotnet ef database update --project src/Services/Auth/Auth.API
dotnet ef database update --project src/Services/Catalog/Catalog.API
# Repeat for other services...
```

3. Run Unit Tests
```bash
dotnet test
```

4. Access the Application:
   
   **API Gateway (Single Entry Point): http://localhost:5000**
   
   Direct Service Access (Dev Mode):
   * Catalog: http://localhost:5004
   * Cart: http://localhost:5005
   * Search: http://localhost:5008

   Kibana: http://localhost:5601    
   RabbitMQ: http://localhost:15672

