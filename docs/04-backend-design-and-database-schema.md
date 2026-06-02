# 04. Database Schema & Backend Design

This document details the database schema configuration and the MediatR request/response designs in the backend.

---

## 1. Database Schema Segregation
To mirror enterprise systems, FinWallet segregates its database tables into distinct schemas within the database:

| Schema Name | Target Domain | Table Name | Purpose |
|---|---|---|---|
| **`identity`** | Authentication & Users | `Users`<br>`Roles`<br>`UserRoles`<br>`UserClaims`<br>`UserLogins`<br>`UserTokens`<br>`RoleClaims` | Extends ASP.NET Core Identity. Tracks user names, contacts, roles, and hashed credentials. |
| **`wallet`** | Ledger Core & Finances | `Wallets`<br>`Transactions`<br>`LedgerEntries`<br>`Receipts` | Houses double-entry ledger inputs, client wallet balances, and receipt references. |
| **`integration`**| Outer Communications | `BankTopUpRequests` | Stores correlating IDs and outcomes for external bank top-up requests. |
| **`audit`** | History & Logging | `WalletStatusHistories`<br>`AuditLogs` | Stores state transition histories (e.g. frozen reason) and technical audit logs. |

---

## 2. Table Column Configurations & Constraints

### A. Table: `wallet.Wallets`
Tracks user wallet balance and compliance status.
- `Id`: `uuid`, Primary Key
- `UserId`: `uuid`, Unique Index, Foreign Key to `identity.Users` (Cascade delete)
- `WalletNumber`: `varchar(50)`, Unique Index, Required. Unique account string.
- `Balance`: `numeric(18, 3)`, Required. Precision matching Libyan Dinars (`LYD`).
- `Currency`: `varchar(10)`, Required. (Hardcoded to `LYD`).
- `Status`: `varchar(20)`, Required. (Values: `Active`, `Frozen`, `Closed`).
- `FrozenReason`: `varchar(500)`, Nullable.
- `CreatedAt`/`UpdatedAt`: `timestamp with time zone`
- **Database Check Constraint**: `CK_Wallet_Balance_Min` checks `"Balance" >= 0`. Prevents negative wallet balances.

### B. Table: `wallet.Transactions`
Tracks all historical financial activities.
- `Id`: `uuid`, Primary Key
- `ReferenceNumber`: `varchar(50)`, Unique Index. System-generated string.
- `Type`: `varchar(20)`, Required. (Values: `TopUp`, `Transfer`, `Refund`).
- `Status`: `varchar(30)`, Required. (Values: `Created`, `PendingBankApproval`, `Completed`, `Rejected`, `Failed`, `Cancelled`).
- `FromWalletId`/`ToWalletId`: `uuid`, Foreign Key to `wallet.Wallets` (Restrict delete).
- `InitiatedByUserId`: `uuid`, Foreign Key to `identity.Users` (Restrict delete).
- `Amount`: `numeric(18,3)`
- `Description`/`RejectionReason`: `varchar(500)`
- `BankReference`: `varchar(100)`
- `CorrelationId`: `uuid`
- `CreatedAt`/`CompletedAt`: `timestamp with time zone`
- **Database Check Constraint**: `CK_Transaction_Amount_Min` checks `"Amount" > 0`.

### C. Table: `wallet.LedgerEntries`
Enforces the double-entry accounting model. Every peer-to-peer transfer creates two entries: a debit for the sender and a credit for the receiver.
- `Id`: `uuid`, Primary Key
- `TransactionId`: `uuid`, Foreign Key to `wallet.Transactions` (Cascade delete).
- `WalletId`: `uuid`, Foreign Key to `wallet.Wallets` (Cascade delete).
- `EntryType`: `varchar(20)`, Required. (Values: `Debit`, `Credit`).
- `Amount`: `numeric(18,3)`
- `BalanceBefore`/`BalanceAfter`: `numeric(18,3)`
- `Currency`: `varchar(10)`
- `CreatedAt`: `timestamp with time zone`
- **Database Check Constraint**: `CK_LedgerEntry_Amount_Min` checks `"Amount" > 0`.

### D. Table: `wallet.Receipts`
Holds printable evidence of completed transactions.
- `Id`: `uuid`, Primary Key
- `TransactionId`: `uuid`, Unique, Foreign Key to `wallet.Transactions` (Cascade delete).
- `ReceiptNumber`: `varchar(50)`, Unique.
- `IssuedAt`: `timestamp with time zone`

### E. Table: `integration.BankTopUpRequests`
Ensures idempotency and traces message states with RabbitMQ.
- `Id`: `uuid`, Primary Key
- `TransactionId`: `uuid`, Foreign Key to `wallet.Transactions` (Cascade delete).
- `CorrelationId`: `uuid`, Unique Index.
- `RequestNumber`: `varchar(50)`, Unique Index.
- `Status`: `varchar(20)` (Values: `Pending`, `Accepted`, `Rejected`, `Failed`).
- `Amount`: `numeric(18,3)`
- `Currency`: `varchar(10)`
- `BankReference`: `varchar(100)`
- `RejectionReason`: `varchar(500)`
- `RequestedAt`/`ProcessedAt`: `timestamp with time zone`

### F. Table: `audit.WalletStatusHistories`
Chronologically stores administrative wallet freeze/unfreeze transitions.
- `Id`: `uuid`, Primary Key
- `WalletId`: `uuid`, Foreign Key to `wallet.Wallets` (Cascade delete).
- `OldStatus`/`NewStatus`: `varchar(20)`
- `Reason`: `varchar(500)`, Required.
- `ChangedByUserId`: `uuid`, Foreign Key to `identity.Users` (Cascade delete).
- `ChangedAt`: `timestamp with time zone`

### G. Table: `audit.AuditLogs`
Stores system security and audit trails.
- `Id`: `uuid`, Primary Key
- `UserId`: `uuid`, Foreign Key to `identity.Users` (Set null on delete).
- `Action`: `varchar(100)`, Required.
- `EntityName`: `varchar(100)`
- `EntityId`: `varchar(50)`
- `Description`: `varchar(1000)`
- `IpAddress`: `varchar(45)` (Supports IPv4 and IPv6 lengths).
- `UserAgent`: `varchar(500)`
- `CreatedAt`: `timestamp with time zone`

---

## 3. CQRS Implementation (MediatR Actions)

The system splits reads and writes using the Command Query Responsibility Segregation (CQRS) pattern:

### A. Commands (State Changes)
- **`RegisterUserCommand`**: Handled by registering a user, assigning the `User` role, generating an active wallet with a unique 12-digit number, logging the audit event, and returning access tokens.
- **`LoginCommand`**: Matches password against Hash, updates `LastLoginAt`, records IP/UserAgent audit logs, and returns access tokens.
- **`RequestWalletTopUpCommand`**: Saves a `Pending` transaction and `BankTopUpRequest`, then publishes a `BankTopUpRequested` integration event via MassTransit.
- **`SendMoneyCommand`**: Executes a P2P transfer between two wallets within a database transaction context.
- **`FreezeWalletCommand`**: Modifies a wallet state to `Frozen`, documents details in `WalletStatusHistory`, and creates an audit log.
- **`UnfreezeWalletCommand`**: Restores a wallet state to `Active`, documents details in `WalletStatusHistory`, and creates an audit log.

### B. Queries (State Reads)
- **`GetMyWalletQuery`**: Returns the active customer's balance and status.
- **`LookupWalletQuery`**: Confirms recipient wallet presence and returns recipient user's full name.
- **`GetMyTransactionsQuery`**: Chronological pageable transaction lists for the current user.
- **`GetTransactionDetailsQuery`**: Specific details for a single transaction.
- **`GetReceiptByTransactionQuery`**: Printable layout details for a completed transaction receipt.
- **`GetAdminDashboardQuery`**: Aggregates metrics (active/frozen ratios, volume calculations) and retrieves recent transaction activity.
- **`GetAdminUsersQuery`**: Searchable grid of system registrants.
- **`GetAdminWalletsQuery`**: Searchable grid of wallets.
- **`GetAdminAuditLogsQuery`**: System security trail list.
