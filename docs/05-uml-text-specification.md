# 05. UML Technical Specifications

This document outlines the system diagrams of **FinWallet** using standard UML notation written in Mermaid.

---

## 1. Use Case Diagram
Describes the actions that Customers and Administrators can perform on the platform.

```mermaid
left-to-right-direction
actor Customer
actor Administrator

rectangle FinWalletSystem {
    Customer --> (Register & Log In)
    Customer --> (View Wallet Balance)
    Customer --> (Request Asynchronous Top-Up)
    Customer --> (Transfer Funds to Peer)
    Customer --> (View Transaction History)
    Customer --> (Print Digital Receipt)

    (Request Asynchronous Top-Up) .-> (Simulate External Bank Processing) : <<include>>
    (Transfer Funds to Peer) .-> (Debit Sender & Credit Receiver) : <<include>>

    Administrator --> (View Global Analytics Dashboard)
    Administrator --> (Search Users Directory)
    Administrator --> (Freeze / Unfreeze Wallets)
    Administrator --> (Audit System Transactions)
    Administrator --> (View Security Audit Logs)
}
```

---

## 2. Activity Diagram: P2P Fund Transfer
Details the logic and checks performed when transferring money between two accounts.

```mermaid
stateDiagram-v2
    [*] --> VerifyRecipient : Request P2P Transfer (Target, Amount)
    
    state VerifyRecipient {
        [*] --> CheckExists
        CheckExists --> RecipientValid : Exists
        CheckExists --> RecipientInvalid : Not Found
    }
    
    RecipientInvalid --> ErrorReturn : Show "Wallet Not Found"
    
    RecipientValid --> CheckSenderStatus
    
    state CheckSenderStatus {
        [*] --> VerifyFreeze
        VerifyFreeze --> SenderActive : Active
        VerifyFreeze --> SenderFrozen : Frozen
    }
    
    SenderFrozen --> ErrorReturn : Show "Wallet is Frozen"
    
    SenderActive --> CheckFunds
    
    state CheckFunds {
        [*] --> VerifyBalance
        VerifyBalance --> BalanceSufficient : Balance >= Amount
        VerifyBalance --> BalanceInsufficient : Balance < Amount
    }
    
    BalanceInsufficient --> ErrorReturn : Show "Insufficient Funds"
    
    BalanceSufficient --> ExecuteTransfer : Open Database Transaction
    
    state ExecuteTransfer {
        [*] --> DeductSender : Subtract from Sender Balance
        DeductSender --> CreditReceiver : Add to Receiver Balance
        CreditReceiver --> WriteLedgers : Create Debit & Credit Ledger Entries
        WriteLedgers --> UpdateTransaction : Mark Transaction as 'Completed'
        UpdateTransaction --> GenerateReceipt : Generate Receipt Record
        GenerateReceipt --> LogAudit : Add Audit Trail Entry
    }
    
    ExecuteTransfer --> CommitTransaction
    CommitTransaction --> ShowReceipt : Commit Success
    ShowReceipt --> [*]
    
    ErrorReturn --> [*]
```

---

## 3. Sequence Diagram: Top-Up Process
Shows the chronological message exchange for an asynchronous top-up.

```mermaid
sequenceDiagram
    autonumber
    actor Customer as User Browser
    participant API as FinWallet.Api
    participant DB as Db Context
    participant Broker as RabbitMQ Broker
    participant Bank as MockBankCore.Worker

    Customer->>API: POST /api/wallets/top-up { Card, Amount }
    activate API
    API->>DB: Create 'PendingBankApproval' Transaction
    API->>DB: Save Integration Request
    API->>Broker: Publish BankTopUpRequested (Event)
    API-->>Customer: 222 Accepted (CorrelationId)
    deactivate API
    
    Note over Customer: Dashboard displays transaction as 'Pending'

    activate Broker
    Broker->>Bank: Dispatch BankTopUpRequested
    deactivate Broker
    activate Bank
    Note over Bank: Simulate bank delay (2 seconds)
    Note over Bank: Random decision (70% Accepted / 30% Rejected)
    Bank->>Broker: Publish BankTopUpProcessed (Decision, Ref)
    deactivate Bank
    
    activate Broker
    Broker->>API: Deliver BankTopUpProcessed to Response Consumer
    deactivate Broker
    activate API
    API->>DB: Load original Transaction & Request
    alt Decision is Accepted
        API->>DB: Add credit amount to Wallet Balance
        API->>DB: Create 'Credit' LedgerEntry
        API->>DB: Update Transaction to 'Completed'
    else Decision is Rejected
        API->>DB: Update Transaction to 'Rejected' (Rejection Reason)
    end
    API->>DB: Update Integration Request status
    API->>DB: Record AuditLog
    API->>DB: Save Changes
    API-->>Broker: Acknowledge Message
    deactivate API
    
    Note over Customer: Dashboard refreshes to show 'Completed' or 'Rejected'
```

---

## 4. Class Diagram
Represents the core models and their relationships inside the domain layer.

```mermaid
classDiagram
    class ApplicationUser {
        +Guid Id
        +string FullName
        +string Email
        +bool IsActive
        +DateTimeOffset CreatedAt
        +DateTimeOffset? LastLoginAt
        +Wallet Wallet
    }

    class Wallet {
        +Guid Id
        +Guid UserId
        +string WalletNumber
        +decimal Balance
        +string Currency
        +WalletStatus Status
        +string FrozenReason
        +DateTimeOffset CreatedAt
        +DateTimeOffset UpdatedAt
        +ApplicationUser User
    }

    class Transaction {
        +Guid Id
        +string ReferenceNumber
        +TransactionType Type
        +TransactionStatus Status
        +Guid? FromWalletId
        +Guid? ToWalletId
        +Guid InitiatedByUserId
        +decimal Amount
        +string Currency
        +string Description
        +string RejectionReason
        +string BankReference
        +Guid? CorrelationId
        +DateTimeOffset CreatedAt
        +DateTimeOffset? CompletedAt
        +Wallet FromWallet
        +Wallet ToWallet
        +ApplicationUser InitiatedByUser
    }

    class LedgerEntry {
        +Guid Id
        +Guid TransactionId
        +Guid WalletId
        +LedgerEntryType EntryType
        +decimal Amount
        +decimal BalanceBefore
        +decimal BalanceAfter
        +string Currency
        +DateTimeOffset CreatedAt
        +Transaction Transaction
        +Wallet Wallet
    }

    class Receipt {
        +Guid Id
        +Guid TransactionId
        +string ReceiptNumber
        +DateTimeOffset IssuedAt
        +Transaction Transaction
    }

    class BankTopUpRequest {
        +Guid Id
        +Guid TransactionId
        +Guid CorrelationId
        +string RequestNumber
        +BankRequestStatus Status
        +decimal Amount
        +string Currency
        +string BankReference
        +string RejectionReason
        +DateTimeOffset RequestedAt
        +DateTimeOffset? ProcessedAt
        +Transaction Transaction
    }

    class WalletStatusHistory {
        +Guid Id
        +Guid WalletId
        +WalletStatus OldStatus
        +WalletStatus NewStatus
        +string Reason
        +Guid ChangedByUserId
        +DateTimeOffset ChangedAt
        +Wallet Wallet
        +ApplicationUser ChangedByUser
    }

    class AuditLog {
        +Guid Id
        +Guid? UserId
        +string Action
        +string EntityName
        +string EntityId
        +string Description
        +string IpAddress
        +string UserAgent
        +DateTimeOffset CreatedAt
        +ApplicationUser User
    }

    ApplicationUser "1" -- "1" Wallet : owns
    Wallet "1" -- "0..*" LedgerEntry : records
    Transaction "1" -- "0..*" LedgerEntry : matches
    Transaction "1" -- "0..1" Receipt : has
    Transaction "1" -- "0..1" BankTopUpRequest : correlates
    Wallet "1" -- "0..*" WalletStatusHistory : logs
    ApplicationUser "1" -- "0..*" WalletStatusHistory : performs
    ApplicationUser "1" -- "0..*" AuditLog : triggers
```

---

## 5. State Machine Diagram: Wallet & Transactions

### Wallet Status Transitions
```mermaid
stateDiagram-v2
    [*] --> Active : Registration (Automatic Provisioning)
    Active --> Frozen : Admin Action (Suspend Account)
    Frozen --> Active : Admin Action (Lift Hold)
    Active --> Closed : Admin Action (Terminate Account)
    Frozen --> Closed : Admin Action (Terminate Account)
    Closed --> [*]
```

### Transaction Status Transitions
```mermaid
stateDiagram-v2
    [*] --> Created : Initiate Command
    Created --> Completed : Direct P2P Transfer (Sync)
    
    Created --> PendingBankApproval : Top-Up Request (Async)
    PendingBankApproval --> Completed : Bank Approved (Processed = Accept)
    PendingBankApproval --> Rejected : Bank Denied (Processed = Reject)
    PendingBankApproval --> Failed : Network Timeout / Error
    
    Completed --> [*]
    Rejected --> [*]
    Failed --> [*]
```

---

## 6. Component Diagram
Shows how different technical layers and services fit together.

```mermaid
graph LR
    subgraph FrontendSPA [Vue Client App]
        UI[Views / Components] --> Stores[Pinia Stores]
        Stores --> Axios[Axios API Client]
    end

    subgraph BackendAPI [ASP.NET Core Web API]
        Controllers[API Controllers] --> MediatR[MediatR Mediator]
        MediatR --> Handlers[Command / Query Handlers]
        Handlers --> EF[Entity Framework Core]
        Handlers --> MT_API[MassTransit Event Publisher]
    end

    subgraph MessageBroker [RabbitMQ Service]
        Queue[rabbitmq-queue]
    end

    subgraph BackgroundWorker [Mock Bank Worker Service]
        MT_Wrk[MassTransit Event Consumer] --> Simulation[Mock Processing Logic]
    end

    subgraph DatabaseEngine [PostgreSQL Server]
        Schema_Id[identity schema]
        Schema_Wl[wallet schema]
        Schema_In[integration schema]
        Schema_Au[audit schema]
    end

    Axios -->|REST API over HTTP| Controllers
    MT_API -->|Publish Events| Queue
    Queue -->|Deliver Events| MT_Wrk
    Simulation -->|Publish Decisions| Queue
    Queue -->|Deliver Decisions| MT_API
    EF -->|Read / Write SQL| DatabaseEngine
```

---

## 7. Deployment Diagram
Illustrates the physical container topology configured in Docker Compose.

```mermaid
graph TD
    subgraph ExternalClient [User Desktop Environment]
        Browser[Web Browser]
    end

    subgraph DockerHost [Docker Compose Network: finwallet-network]
        Frontend[finwallet-frontend container<br/>Nginx Server | Port 80]
        API[finwallet-api container<br/>.NET 10 | Port 8080]
        Worker[finwallet-mock-bank-core container<br/>.NET 10 Worker]
        Rabbit[finwallet-rabbitmq container<br/>RabbitMQ Management | Ports 5672, 15672]
        DB[finwallet-postgres container<br/>PostgreSQL 16 | Port 5432]
    end

    Browser -->|Port 9000| Frontend
    Browser -->|Port 8081| API
    Browser -->|Port 15672| Rabbit

    API -->|Port 5432| DB
    API -->|Port 5672| Rabbit
    Worker -->|Port 5672| Rabbit
```
