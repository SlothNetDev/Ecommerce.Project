# Ecommerce.Project

## Overview

This repository contains the backend for an e-commerce application implemented in C#. The API and business logic are located under the Ecommerce.Api project.

> This repository is backend-only.

## Features

- Product management
- User authentication and authorization
- Order processing
- Shopping cart handling

## Requirements

- .NET SDK (compatible with the project, e.g. .NET 8 or the version used in the solution)
- A database (configured in appsettings or environment variables)

## Quick start

1. Clone the repository:

   git clone https://github.com/SlothNetDev/Ecommerce.Project.git

2. Change to the API project folder:

   cd Ecommerce.Project/Ecommerce.Api

3. Restore and build:

   dotnet restore
   dotnet build

4. Configure the database and any secrets in appsettings.json or via environment variables. Common settings:

   - Connection strings
   - JWT or auth settings

5. Run the API:

   dotnet run

## Database and Migrations

If the project uses EF Core, apply migrations (if any) before running:

   dotnet ef database update

Adjust the command to the project and tools used in the solution.

## Tests

If there are test projects in the solution, run them with:

   dotnet test

## Contributing

Contributions are welcome. Please open issues or pull requests and include a short description of the change.

## License

No license is specified. Add a LICENSE file if you want to provide one.
