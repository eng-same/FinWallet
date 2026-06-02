# 01. Project Scope & Objectives

## 1. Project Objective
**FinWallet** is an academic digital wallet simulation platform built for a university software development course. The primary goal of the system is to demonstrate best practices in modern web application engineering, specifically focusing on clean code architecture, database integrity, and asynchronous transaction handling.

The system is a fully self-contained simulation: it does **not** process real money, interact with real banking infrastructure, or handle real personal identifiable information (PII). All account balances, transactions, and banking system interactions are mock representations of reality.

---

## 2. Target Audience & Academic Value
FinWallet is designed to serve as a comprehensive codebase demonstrating:
- **Clean Architecture / Domain-Driven Design (DDD)** principles in .NET 10.
- **Modular Monolithic** structures that establish clean layer boundaries before microservice splitting.
- **Event-Driven Integration** using MassTransit and RabbitMQ for simulated asynchronous external communications.
- **Relational Schema Separation** in PostgreSQL/SQLite for enterprise-level data segregation.
- **State-of-the-Art SPA Frontend** development using Vue 3, TypeScript, and Quasar Framework.

---

## 3. User Roles
The system defines two primary user roles with distinct permissions:

### A. Customer (User)
The customer represents the primary consumer of the wallet. Their capabilities include:
- **Self-Registration & Authentication**: Safe sign-up and sign-in generating a secure JWT token.
- **Automatic Wallet Provisioning**: Instantly receive a default active digital wallet (in Libyan Dinar, `LYD`) upon registration.
- **Simulated Top-up Request**: Request simulated balance top-ups from a mock external bank.
- **Intra-system Transfer**: Send funds synchronously to other users' wallets by looking up their wallet numbers.
- **Transaction History & Ledger Activity**: View detailed histories of inbound and outbound transactions.
- **Digital Receipt Verification**: Review detailed digital receipts containing cryptographic transaction reference IDs.
- **Profile Management**: View and verify profile settings.

### B. System Administrator (Admin)
The administrator is responsible for back-office monitoring and compliance control. Their capabilities include:
- **Platform Analytics Dashboard**: View aggregate data charts representing total system transaction volume, active/frozen wallet ratios, registration counts, and today's transaction velocities.
- **User Directory Management**: View all registered users, their verification details, and active statuses.
- **Compliance Control (Freeze/Unfreeze)**: Permanently or temporarily freeze specific wallets due to simulated suspicious activity, preventing them from sending/receiving funds.
- **Transaction Ledger Auditing**: Monitor all transaction details and inspect underlying database entries.
- **Security & Audit Logs**: Trace a chronological list of sensitive administrative actions (e.g., auth events, freeze/unfreeze state shifts, IP/UserAgent capture).

---

## 4. Functional Scope
The following table outlines the system's core functional components:

| Component | Scope Description | Critical Business Rules |
|---|---|---|
| **Identity System** | User accounts, credentials hashing, JWT token-based session management. | - Strict password requirements.<br>- Duplicate emails are forbidden. |
| **Wallet Management** | Wallet creation, balance tracking, status transitions. | - One wallet per user.<br>- Wallet status can be Active, Frozen, or Closed.<br>- Wallet balance cannot go negative under any circumstance. |
| **Simulated Top-Up** | Asynchronous external bank simulation. | - Communicates via RabbitMQ message queues.<br>- Decisions (Accept/Reject) are processed with a 2s delay to simulate external networks. |
| **Peer-to-Peer Transfer** | Synchronous intra-system ledger updates. | - Must be atomic (debit sender and credit receiver in one transaction).<br>- Block self-transfers.<br>- Block transfers if sender wallet is frozen or has insufficient funds. |
| **Audit Trails** | Chronological logs of user actions. | - Captures User ID, Action, Target Entity, Description, IP Address, and browser User Agent. |
