# Digital Wallet Backend

A backend API for a digital wallet platform designed to handle wallet balances, financial transactions, and user authentication.

This project demonstrates backend architecture for financial systems including authentication, transaction processing, and wallet management.

---

## Tech Stack

- C#
- ASP.NET Web API
- Entity Framework
- PostgreSQL
- JWT Authentication

---

## Core Features

- User registration and authentication
- JWT-secured API endpoints
- Wallet balance management
- Transaction processing
- Transaction history
- Database migrations with Entity Framework

---

## Architecture

The system follows a layered architecture:

Controllers  
→ Handle API requests

Services  
→ Business logic for wallets and transactions

Models  
→ Database entities

Data / Migrations  
→ Database schema management

---

## Example Endpoints

POST /api/auth/register  
POST /api/auth/login  
GET /api/wallet  
POST /api/transactions/transfer  
GET /api/transactions

---

## Running the Project

1. Clone repository
git clone https://github.com/kupelaphiri/DigitalWalletBackend

2. Configure database connection in `appsettings.json`

3. Run migrations

4. Start the API

---

## Future Improvements

- Integration with mobile money providers
- Fraud detection checks
- Transaction analytics
- Multi-currency support
