# 07. Academic Presentation & Demo Script

Use this step-by-step script to demonstrate **FinWallet**'s features to evaluators.

---

## 1. Preparation & Setup
Ensure the stack is running cleanly inside Docker:
1. Start containers:
   ```bash
   docker compose up --build -d
   ```
2. Confirm the services are healthy.
3. Open your browser to the Web client URL: `http://localhost:9000`.

---

## 2. Step-by-Step Demo Script

### Phase 1: Administrator Initial Review
*Purpose: Introduce the system from an administrative perspective.*
1. **Action**: On the Login screen, enter the administrator credentials:
   - **Email**: `admin@finwallet.local`
   - **Password**: `Admin@123456`
2. **Action**: Click **Login**.
3. **Showcase**:
   - Point out the **Admin Control Panel** sidebar.
   - Point out the **Dashboard** charts showing system metrics, active users, total volume, and recent transactions.
   - Explain that this dashboard utilizes ApexCharts and queries administrative endpoints mapped directly to modular monolith query handlers.

---

### Phase 2: Customer Onboarding
*Purpose: Demonstrate user self-registration and automatic wallet provisioning.*
1. **Action**: Click the logout icon in the top right.
2. **Action**: Click **Sign Up** on the Landing Page.
3. **Action**: Fill out the registration form:
   - **Full Name**: `Fatima Al-Senussi`
   - **Email**: `fatima@finwallet.local`
   - **Phone**: `+218912345678`
   - **Password**: `Fatima@123456`
4. **Action**: Submit.
5. **Showcase**:
   - Point out that Fatima is immediately redirected to her **Customer Dashboard**.
   - Show the newly generated **Wallet Number** (e.g. `FW-123456789012`) and the current balance of `0.000 LYD`.
   - Explain that the wallet creation was executed atomically in the database during registration inside a single database transaction.

---

### Phase 3: Simulated Asynchronous Top-Up
*Purpose: Demonstrate external integration via message queuing.*
1. **Action**: Click **Top-Up Wallet** in the menu.
2. **Action**: Input values:
   - **Select Bank**: `National Commercial Bank`
   - **Card Number**: `1234567890123456`
   - **Amount**: `250.000`
3. **Action**: Click **Authorize Top-Up**.
4. **Showcase**:
   - Point out the popup indicating the top-up request is pending bank approval.
   - Navigate to the **Transaction History** page. Show that the status is `Pending Bank`.
   - Wait 2 seconds. Press the **Refresh** button in the header.
   - Point out the state transition to `Completed` (or `Rejected`). Assuming accepted, show that the balance has updated to `250.000 LYD`.
   - **Technical explanation**: Explain that this request published a `BankTopUpRequested` event to RabbitMQ. The `MockBankCore` worker consumed it, simulated processing latency, and published `BankTopUpProcessed`. The API consumer then updated the balance.

---

### Phase 4: Peer-to-Peer Fund Transfer
*Purpose: Demonstrate atomic ledger compliance.*
1. **Action**: Click **Send Money** in the menu.
2. **Action**: We need a target wallet number. We can register another account, or use the pre-seeded user's wallet. Let's look up the wallet:
   - Enter a target wallet number (e.g., you can lookup `FW-584930294857` or register another user in a separate incognito tab to get their wallet number).
3. **Action**: Click **Verify Account**. Show that the recipient's name is dynamically fetched and displayed for validation.
4. **Action**: Enter **Amount**: `80.000` and Description `Project Share`. Click **Send Funds**.
5. **Showcase**:
   - Point out the green confirmation checkmark page.
   - Click **View Digital Receipt**. Point out the Receipt Number and the unique Transaction reference code.
   - Navigate to **My Wallet** to show the debit entry in the ledger. Explain that two ledger entries were written inside a single database transaction, ensuring no funds were lost.

---

### Phase 5: Compliance Controls (Freeze & Block)
*Purpose: Show administrative authority and security.*
1. **Action**: Log out Fatima, and log back in as `admin@finwallet.local`.
2. **Action**: Go to **Manage Users** and click on `Fatima Al-Senussi`.
3. **Action**: Review Fatima's user details page, wallet status, and recent transactions.
4. **Action**: Click **Freeze Wallet**. Input reason: "Suspicious high-velocity top-up check". Click **Confirm**.
5. **Action**: Log out and log back in as Fatima.
6. **Showcase**:
   - Point out the red **FROZEN** badge on Fatima's dashboard.
   - Click **Send Money** and attempt to transfer funds.
   - Show the error banner indicating: `Your wallet is currently frozen. Transfers are blocked.`
   - Point out that this validates business rules enforcement at both the API level and database level.

---

### Phase 6: System Audit Trail Verification
*Purpose: Show compliance trails.*
1. **Action**: Log out Fatima, and log back in as the Administrator.
2. **Action**: Go to **Audit Logs** in the admin menu.
3. **Action**: Use the search input to filter by `Fatima`.
4. **Showcase**:
   - Show the recorded history of: User Registration, Wallet Provisioning, Top-Up request, transfer actions, and the admin **Wallet Freeze** action.
   - Click **Toggle Technical Details** on the Freeze action.
   - Point out the captured technical details: The IP address, User Agent string, Log Reference ID, and the exact Wallet ID that was frozen.
   - Conclude by highlighting how the system provides full accountability and security across all layers.
