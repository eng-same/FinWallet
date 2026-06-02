# 03. Frontend Pages & API Contracts

This document explains the authentication mechanism, UI page routing structure, and details the REST API endpoints.

---

## 1. Authentication Strategy
FinWallet uses **JWT Bearer Token** authentication:
- Upon a successful POST to `/api/auth/login` or `/api/auth/register`, the backend returns an access token along with basic user details and their role.
- The Vue client saves the token, user profile, and role in the browser's `localStorage` via Pinia stores.
- An Axios request interceptor automatically attaches the JWT token to the `Authorization` header (`Bearer <token>`) for all subsequent API requests.
- A response interceptor checks for `401 Unauthorized` responses. If a session expires, it clears local storage and redirects the user to the login screen.

---

## 2. Frontend Page Routing

The application defines distinct views for Customers and Admins:

| Route Path | Page Name | Access Rule | Purpose |
|---|---|---|---|
| `/` | Landing | Public | Public landing page introducing the system. |
| `/login` | Login | Guest Only | Form to authenticate existing accounts. |
| `/register` | Register | Guest Only | Form to create a customer account. |
| `/unauthorized` | Unauthorized | Public | Access denied warning page. |
| `/server-error` | ServerError | Public | Database/API offline warning page. |
| `/dashboard` | Dashboard | User Role | Main customer dashboard. Shows balance and links. |
| `/wallet` | MyWallet | User Role | Displays detailed wallet ledger and balance. |
| `/wallet/top-up` | TopUp | User Role | Form to execute simulated card top-ups. |
| `/wallet/send` | SendMoney | User Role | Forms to search receiver and transfer funds. |
| `/transactions`| TransactionHistory | User Role | Chronological history of user transactions. |
| `/transactions/:id`| TransactionDetails | User Role | In-depth transaction context view. |
| `/transactions/:id/receipt`| Receipt | User Role | Cryptographically referenced printable receipt. |
| `/profile` | Profile | Auth User | Check user profile and role details. |
| `/admin/dashboard`| AdminDashboard | Admin Role | Graphical ApexCharts metrics of the system. |
| `/admin/users` | AdminUsers | Admin Role | Searchable directory list of users. |
| `/admin/users/:id`| AdminUserDetails | Admin Role | Freeze/unfreeze owner wallets & view user details. |
| `/admin/wallets` | AdminWallets | Admin Role | Directory grid of all user wallets and balances. |
| `/admin/wallets/:id`| AdminWalletDetails | Admin Role | Inspect status log histories of a specific wallet. |
| `/admin/transactions`| AdminTransactions | Admin Role | Filterable view of all transactions on the system. |
| `/admin/transactions/:id`| AdminTransactionDetails| Admin Role | Deep-dive audit of system transaction details. |
| `/admin/audit-logs`| AdminAuditLogs | Admin Role | Searchable logs of security and admin actions. |

---

## 3. Backend API Contract Envelopes

All responses return a uniform JSON format:

### Success Envelope
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... }
}
```

### Error Envelope
```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "message": "'Amount' must be greater than 0."
    }
  ]
}
```

---

## 4. API Endpoints List

### 1. Authentication Module (`/api/auth`)
- **`POST /api/auth/register`**: Creates a new customer account, provisions their wallet, and returns their JWT.
- **`POST /api/auth/login`**: Authenticates credentials and returns a JWT token.
- **`GET /api/auth/me`**: Fetches the authenticated user profile details.

### 2. Wallet Operations Module (`/api/wallets`)
- **`GET /api/wallets/my`**: Retrieves the active user's wallet details.
- **`POST /api/wallets/top-up`**: Publishes a top-up request event to RabbitMQ. Returns `202 Accepted` with a correlation ID.
- **`GET /api/wallets/lookup`**: Look up user wallets by their wallet number before sending money.

### 3. P2P Transfers Module (`/api/transfers`)
- **`POST /api/transfers`**: Performs a synchronous peer-to-peer transfer. Returns transaction details.

### 4. Transactions & Receipts Modules
- **`GET /api/transactions`**: Fetches transaction histories for the logged-in customer.
- **`GET /api/transactions/{id}`**: Retrieves specific transaction details.
- **`GET /api/receipts/{transactionId}`**: Generates and retrieves printable receipt details.

### 5. Administrative Controls Module (`/api/admin`)
- **`GET /api/admin/dashboard`**: Returns global statistics (total volume, transaction velocities, chart metrics).
- **`GET /api/admin/users`**: List all users. Supports searching by keyword.
- **`GET /api/admin/users/{id}`**: Detail audit card for a specific user, their wallet, and recent activity.
- **`GET /api/admin/wallets`**: List all wallets. Supports searching by wallet number.
- **`GET /api/admin/wallets/{id}`**: Detailed audit card for a wallet and its history logs.
- **`PUT /api/admin/wallets/{id}/freeze`**: Mark a wallet as frozen with a reasoning comment.
- **`PUT /api/admin/wallets/{id}/unfreeze`**: Unfreeze a wallet to resume transfers.
- **`GET /api/admin/transactions`**: View system transactions. Supports status and type filters.
- **`GET /api/admin/transactions/{id}`**: Deep administrative audit view of a single transaction.
- **`GET /api/admin/audit-logs`**: Searchable chronological list of administrative actions.
