# Rupali Account Management System (হিসাব ব্যবস্থাপনা)

A professional, production-grade Account Management System built with **C# 12**, **.NET 8 MAUI (Windows Desktop)**, and **PostgreSQL** using **Entity Framework Core 8**. Designed specifically for printing press, packaging, and paper converting businesses in Bangladesh.

---

## 🏛️ Clean Architecture Structure

```
AccountManagement.slnx
│
├── src/
│   ├── AccountManagement.Domain/          # Pure Domain Entities, Enums, Financial Calculator, BDT Currency Formatter
│   ├── AccountManagement.Application/     # Use Cases, DTOs, Service Interfaces, FluentValidation Rules
│   ├── AccountManagement.Infrastructure/  # EF Core DbContext, PostgreSQL (Npgsql), Database Seeder, Backup Engine
│   └── AccountManagement.App/             # .NET MAUI Windows Desktop Shell, MVVM ViewModels, XAML Views
│
└── tests/
    ├── AccountManagement.Domain.UnitTests/      # Automated tests for financial formulas, BDT formatting & conversions
    └── AccountManagement.Application.UnitTests/ # Automated tests for validators & ledger transaction workflows
```

---

## ⚙️ Core Modules & Features

### 1. 📊 Executive Financial Dashboard (`DashboardPage`)
- Real-time **Today's Revenue**, **Transactions Count**, and **Direct Production Costs**.
- **Total Outstanding Customer Due** across all active clients.
- **Current Month Revenue, Direct Costs, Overhead Expenses, Gross Profit, and Net Profit**.
- Live recent transactions stream with status badges (Active, Completed, Cancelled).

### 2. 📖 Daily & Monthly Ledger (`LedgerPage`)
- Comprehensive transaction data grid: Date, Job Number, Customer, Work Description, Total Bill, Paid Amount, Balance Due, Status.
- Multi-criteria filtering by Date Range (From/To), Customer, Status, and Search keywords.
- Paginated table navigation with record counts.
- **Audit-Safe Cancellation**: Prompts for cancellation reason, marks status as `Cancelled`, records audit trail, and automatically reverts the customer's outstanding balance.

### 3. 📝 New Job Order Entry (`NewTransactionPage`)
- Fast customer selection and date picker.
- **Raw Materials / Paper Items Breakdown**:
  - Material selection (Art Paper, Offset, Auto Carbon, Art Card, Colour Card, Sticker, Ledger Paper).
  - GSM, Paper Size, Quantity (Reams/Sheets/Packets), Unit Rate.
  - Automatic rate snapshotting: Historical rates used in a transaction are permanently locked and will never silently change when future monthly rates are modified.
- **Production & Finishing Costs Breakdown**:
  - Itemized cost entries for Master Plates, Offset Printing, Lamination, Binding, Die-cutting, Transport, etc.
- **Live Billing Calculations**: Subtotal, Discount, Total Bill Amount in BDT (৳), Advance Payment received (Cash, bKash, Nagad, Bank, Cheque), and Remaining Due.

### 4. 🏷️ Monthly Rate Management (`RateManagerPage`)
- Rate periods with effective dates (e.g. October 2026, November 2026).
- Material-specific pricing with GSM and Size specifications.
- Quantity tiers and measurement units (Ream, Sheet, Packet, Dosta, Kg).

### 5. 👥 Customers & Accounts (`CustomerManagerPage`)
- Customer directory with contact details and running balances.
- Fast client addition with initial opening balance support.

### 6. 💰 Payments & Overhead Expenses (`PaymentExpensePage`)
- **Customer Collections**: Record partial or full invoice settlements with payment method and transaction reference numbers.
- **Overhead Expenses**: Record factory rent, electricity/power, machine maintenance, ink/chemicals, staff wages, and refreshments.

### 7. 🛡️ Backup & Recovery (`BackupRestorePage`)
- One-click verified database backup creation to a local directory (default `C:\RupaliAccountBackups`).
- Generates a complete data bundle with SHA-256 checksum verification (`.sha256`).
- Transactional restoration that validates file integrity before repopulating tables.

---

## 🚀 Setup & Execution Guide

### Prerequisites
1. **.NET 8 SDK** installed on Windows.
2. **PostgreSQL 14+** running locally (or remote server).

### Database Configuration & Automatic Offline Fallback
The system supports both high-concurrency **PostgreSQL** (for networked multi-user setups) and **SQLite** (for automatic single-user offline desktop execution):

[`src/AccountManagement.App/appsettings.json`](file:///src/AccountManagement.App/appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=RupaliAccountDb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD;"
  }
}
```

*Note: If PostgreSQL is not active or reachable on port 5432, the system automatically and seamlessly falls back to a local SQLite database (`rupali_account_offline.db`) in the application directory. On first startup, the database is auto-migrated and seeded with standard Bangladeshi printing press paper categories (Art Paper, Offset, Auto Carbon, Swedish Board, etc.), paper sizes, and default system settings.*

### Running the Application
From PowerShell in the project root:
```powershell
dotnet run --project src/AccountManagement.App/AccountManagement.App.csproj -f net8.0-windows10.0.19041.0
```

### Running Automated Tests
```powershell
dotnet test tests/AccountManagement.Domain.UnitTests/AccountManagement.Domain.UnitTests.csproj
dotnet test tests/AccountManagement.Application.UnitTests/AccountManagement.Application.UnitTests.csproj
```

---

## 🔒 Security & Financial Integrity
- **Decimal Precision**: All currency calculations use 18-digit precision with 2 decimal places (`MIDPOINT_AWAY_FROM_ZERO`) to eliminate binary floating-point rounding errors.
- **Rate Immutability**: Rates are snapshotted onto each `TransactionItem` at creation time.
- **Audit Trails**: Non-destructive cancellations with logged reasons and timestamped records.
- **Bilingual Support**: Standard Bangladeshi Taka notation (`৳ 1,25,000.00` and `৳ ১,২৫,০০০.০০`).
