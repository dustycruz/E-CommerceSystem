# E-CommerceSystem

Microservices e-commerce application built with .NET 10, ASP.NET Core Web API, ASP.NET Core MVC, Entity Framework Core 10, SQL Server LocalDB, RabbitMQ, and Swagger/OpenAPI.

This project demonstrates a database-per-service architecture and an asynchronous order-processing saga. The Storefront provides the web interface, the Order Service coordinates checkout, the Catalog Service provides product and pricing information, the Inventory Service manages stock reservations, and the Payment Service processes test payments.

> This project runs locally without Docker.

> The commands below assume the repository is located at `C:\dev\E-CommerceSystem`. Replace this path if your clone is located elsewhere.


## Components and ports

| Component | Responsibility | Port |
|---|---|---:|
| Storefront | ASP.NET Core MVC web application, cart, checkout, order tracking, and service-status page | 5000 |
| Catalog Service | Product catalog, prices, product CRUD, and catalog validation | 5001 |
| Order Service | Order creation, catalog validation, saga orchestration, and transactional outbox publishing | 5002 |
| Inventory Service | Stock levels, reservations, commits, releases, and RabbitMQ consumers | 5003 |
| Payment Service | Test charges, refunds, payment CRUD, and payment idempotency | 5004 |
| RabbitMQ | Asynchronous Order–Inventory saga messaging | 5672 |
| RabbitMQ Management UI | Optional broker administration dashboard | 15672 |

## Communication flow

- The Storefront calls Catalog for products and prices.
- The Storefront calls Inventory for stock hints.
- The Storefront calls Order for checkout and order tracking.
- The Storefront checks all four backend services from its Service Status page.
- Order calls Catalog over HTTP to validate products and obtain prices.
- Order calls Payment over HTTP to charge or refund payments.
- Order publishes saga events through RabbitMQ.
- Inventory consumes order and release-stock events and publishes reservation results.
- Order consumes inventory results and continues the saga.

## Database-per-service

Each service owns a separate SQL Server LocalDB database:

- `ECommerceCatalogDb`
- `ECommerceInventoryDb`
- `ECommerceOrderDb`
- `ECommercePaymentDb`

Services do not directly access another service's database.

## Prerequisites

Install the following on Windows:

1. Windows 10 or Windows 11
2. .NET SDK 10.0.100 or a compatible .NET 10 SDK
3. SQL Server Express LocalDB
4. SQL Server command-line tools, including `sqlcmd`
5. RabbitMQ Server
6. Erlang/OTP, if required by the RabbitMQ installer
7. Git

Verify the tools:

```cmd
dotnet --version
sqllocaldb info MSSQLLocalDB
sqlcmd -?
```

The repository's `global.json` targets .NET SDK `10.0.100`.

If `dotnet ef` is unavailable, install it:

```cmd
dotnet tool install --global dotnet-ef
```

## Clone and build

```cmd
git clone https://github.com/dustycruz/E-CommerceSystem.git
cd E-CommerceSystem
dotnet restore
dotnet build
```

## Start SQL Server LocalDB

```cmd
sqllocaldb start MSSQLLocalDB
sqllocaldb info MSSQLLocalDB
```

The services use:

```text
(localdb)\MSSQLLocalDB
```

## Create or update databases

In Development, each database-owning service automatically applies EF Core migrations during startup.

Migrations can also be applied manually:

```cmd
cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.CatalogService
dotnet ef database update

cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.InventoryService
dotnet ef database update

cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.OrderService
dotnet ef database update

cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.PaymentService
dotnet ef database update
```

Catalog and Inventory migrations create the development seed data.

## Start RabbitMQ

RabbitMQ is required by the Order and Inventory services for the asynchronous part of the order saga.

The local Development configuration uses:

```text
Host: localhost
AMQP port: 5672
Username: guest
Password: guest
Virtual host: /
```

If RabbitMQ is installed as a Windows service, start it using Windows Services or run:

```cmd
net start RabbitMQ
```

Verify the broker:

```cmd
rabbitmq-diagnostics ping
```

Expected result:

```text
Ping succeeded
```

If `rabbitmq-diagnostics` is not recognized, run it from RabbitMQ's `sbin` directory or add that directory to the Windows `PATH`.

The RabbitMQ Management UI is available at:

```text
http://localhost:15672
```

The Management UI must be enabled in RabbitMQ. If `guest` / `guest` does not work, use the credentials configured on your computer.

## Start the services

Open a separate Command Prompt window for each application.

### Catalog Service

```cmd
cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.CatalogService
dotnet run
```

Runs at:

```text
http://localhost:5001
```

### Order Service

```cmd
cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.OrderService
dotnet run
```

Runs at:

```text
http://localhost:5002
```

### Inventory Service

```cmd
cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.InventoryService
dotnet run
```

Runs at:

```text
http://localhost:5003
```

### Payment Service

```cmd
cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.PaymentService
dotnet run
```

Runs at:

```text
http://localhost:5004
```

### Storefront

```cmd
cd /d C:\dev\E-CommerceSystem\web\ECommerceSystem.Storefront
dotnet run
```

Runs at:

```text
http://localhost:5000
```

Open the Storefront at:

```text
http://localhost:5000
```

## Start all five applications

From the repository root:

```cmd
start "Catalog Service" cmd /k "cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.CatalogService && dotnet run"
start "Order Service" cmd /k "cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.OrderService && dotnet run"
start "Inventory Service" cmd /k "cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.InventoryService && dotnet run"
start "Payment Service" cmd /k "cd /d C:\dev\E-CommerceSystem\src\ECommerceSystem.PaymentService && dotnet run"
start "Storefront" cmd /k "cd /d C:\dev\E-CommerceSystem\web\ECommerceSystem.Storefront && dotnet run"
```

Wait for all applications to start before testing an order.

## Verify the services

Use the Windows system copy of `curl.exe`:

```cmd
"%SystemRoot%\System32\curl.exe" http://localhost:5001/
"%SystemRoot%\System32\curl.exe" http://localhost:5002/
"%SystemRoot%\System32\curl.exe" http://localhost:5003/
"%SystemRoot%\System32\curl.exe" http://localhost:5004/
```

Each backend should return JSON containing:

```json
{
  "status": "running"
}
```

Verify the Storefront:

```cmd
"%SystemRoot%\System32\curl.exe" -s -o NUL -w "Storefront HTTP: %{http_code}\n" http://localhost:5000/
```

Expected result:

```text
Storefront HTTP: 200
```

Check the ports:

```cmd
netstat -ano | findstr "LISTENING" | findstr ":5000 :5001 :5002 :5003 :5004"
```

## Verify the databases

From the repository root:

```cmd
database\verify-all.cmd
```

If the default LocalDB name does not work, retrieve the current pipe:

```cmd
sqllocaldb start MSSQLLocalDB
sqllocaldb info MSSQLLocalDB
```

Copy the exact value shown after `Instance pipe name:`:

```cmd
database\verify-all.cmd "CURRENT_INSTANCE_PIPE_NAME"
```

Example format:

```cmd
database\verify-all.cmd "np:\\.\pipe\LOCALDB#XXXXXXXX\tsql\query"
```

The pipe can change after LocalDB restarts. Always use the current value.

The scripts check database existence, migrations, seed data, indexes, money columns, rowversion columns, foreign keys, idempotency, card-data storage rules, overselling protection, and outbox state.

A successful run reports `PASS:` lines and no `FAIL:` lines.

## Verify RabbitMQ queues

If `rabbitmqctl.bat` is in the Windows `PATH`:

```cmd
rabbitmqctl.bat list_queues name
```

If it is not recognized, run it from RabbitMQ's `sbin` directory or use the full path. For example:

```cmd
"C:\Program Files\RabbitMQ Server\rabbitmq_server-4.3.6\sbin\rabbitmqctl.bat" list_queues name
```

The versioned directory may differ on another computer.

Core queues:

```text
inventory.order-placed
inventory.order-placed.dlq
inventory.release-stock
inventory.release-stock.dlq
order.inventory-results
order.inventory-results.dlq
```

The `.dlq` queues are dead-letter queues.

The shared code also defines an optional `notification.all` topology, but there is no Notification Service in the current solution and it is not required for the core saga.

## Use the Storefront

1. Open `http://localhost:5000`.
2. Add a product to the cart.
3. Continue to checkout.
4. Enter a name and valid email address.
5. Use the successful test card:

   ```text
   4242424242424242
   ```

6. Submit the order.
7. Track the order until it reaches `Confirmed`.

### Normal order flow

1. Order validates products and prices through Catalog.
2. Order is saved as `Pending`.
3. An order event is saved to the transactional outbox.
4. The outbox publisher sends the event through RabbitMQ.
5. Inventory reserves stock.
6. Payment charges the test card over HTTP.
7. Inventory commits the reservation.
8. The order becomes `Confirmed`.

Declined test card:

```text
4000000000000002
```

The checkout page also provides a failure-simulation option. When enabled, the saga compensates after payment by:

1. Refunding the payment
2. Releasing the stock
3. Cancelling the order

This application is for test data only. Do not use real payment-card information.

## Service failure behavior

The Storefront Service Status page checks the Catalog, Order, Inventory, and Payment services.

- If Catalog is unavailable, product data may not load and checkout can return an error.
- If Inventory is unavailable, stock hints may be unavailable. Messages already published to RabbitMQ remain available for processing when Inventory reconnects.
- If Payment is unavailable during payment processing, the Order Service retries the saga message through its RabbitMQ consumer retry mechanism. If retries are exhausted, it releases stock and cancels the order.
- If Order is unavailable, checkout and order tracking cannot complete, but the Storefront cart remains available.

## Swagger/OpenAPI

| Service | Swagger URL |
|---|---|
| Catalog | http://localhost:5001/swagger |
| Orders | http://localhost:5002/swagger |
| Inventory | http://localhost:5003/swagger |
| Payments | http://localhost:5004/swagger |

OpenAPI contracts are located in:

```text
contracts/
```

- `contracts/catalog.yaml`
- `contracts/orders.yaml`
- `contracts/inventory.yaml`
- `contracts/payments.yaml`

### API base paths

| Service | Base path |
|---|---|
| Catalog | `/catalog/v1` |
| Orders | `/orders/v1` |
| Inventory | `/inventory/v1` |
| Payments | `/payments/v1` |

## Project structure

```text
E-CommerceSystem/
├── contracts/                         # OpenAPI contracts
├── database/                          # SQL migration and verification scripts
├── postman/                           # Currently reserved for Postman files
├── src/
│   ├── ECommerceSystem.CatalogService/
│   ├── ECommerceSystem.InventoryService/
│   ├── ECommerceSystem.OrderService/
│   ├── ECommerceSystem.PaymentService/
│   └── ECommerceSystem.Shared/
├── tests/                             # Currently reserved for automated tests
├── web/
│   └── ECommerceSystem.Storefront/
├── E-CommerceSystem.slnx
├── global.json
└── README.md
```

## Stopping the application

Stop the .NET services:

```cmd
taskkill /F /IM dotnet.exe
```

Stop RabbitMQ if it is running as a Windows service:

```cmd
net stop RabbitMQ
```

Stop LocalDB:

```cmd
sqllocaldb stop MSSQLLocalDB
```

Use `net stop RabbitMQ` only when RabbitMQ is installed as a Windows service.

## Troubleshooting

### Curl reports `This app can't run on your PC`

```cmd
"%SystemRoot%\System32\curl.exe" --version
```

### A port is already in use

```cmd
netstat -ano | findstr ":5000"
tasklist /FI "PID eq PROCESS_ID"
```

Replace the port and process ID as appropriate.

### LocalDB pipe is invalid

```cmd
sqllocaldb start MSSQLLocalDB
sqllocaldb info MSSQLLocalDB
database\verify-all.cmd "CURRENT_INSTANCE_PIPE_NAME"
```

### RabbitMQ is unavailable

```cmd
rabbitmq-diagnostics ping
net start RabbitMQ
```

If either command is not recognized, run it from RabbitMQ's `sbin` directory or add that directory to `PATH`.

## Contributors

> Confirm that these responsibilities match your group's actual contributions before submitting.

| Member | GitHub | Responsibilities |
|---|---|---|
| Dusty Cruz | [dustycruz](https://github.com/dustycruz) | Order Service, saga orchestration, RabbitMQ workflow, and service integration |
| Laurence Mirandilla | [LaurenceMirandilla](https://github.com/LaurenceMirandilla) | Catalog Service, Inventory Service, product catalog, stock management, and reservations |
| CJ Bello | [CJ-Bello](https://github.com/CJ-Bello) | Payment Service, payment database, idempotent charges, and refunds |

All members contributed to integration testing, documentation, debugging, and project review.
