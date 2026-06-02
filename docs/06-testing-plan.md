# 06. Testing & Quality Assurance Plan

This document details the testing strategies, test suite cases, and validation processes of **FinWallet**.

---

## 1. Testing Strategy

FinWallet ensures stability and security by using a multi-tiered testing plan:

1. **Unit & Integration Testing (Automated)**:
   - Covers critical financial business rules in `FinWallet.Tests` using **xUnit**, **FluentAssertions**, and **NSubstitute** (for mocking message publishers).
   - Simulates EF Core interactions using an in-memory database to verify multi-table mutations, transaction atomicity, ledger balances, and database exceptions.
2. **Asynchronous Flow Verification (Integration)**:
   - Validates RabbitMQ message exchanges between `FinWallet.Api` and `FinWallet.MockBankCore.Worker` by testing consumer triggers.
3. **Manual Functional Testing (User Journey)**:
   - Validates client-side logic, routing guards, response styling, and error handlers directly in the browser.

---

## 2. Automated Test Suite Cases (`FinWallet.Tests`)

The backend contains automated tests verifying the following business boundaries:

| Test Case Name | Target Action | Verification & Assertions |
|---|---|---|
| `SendMoneyCommand_ShouldFail_WhenBalanceIsInsufficient` | Peer-to-Peer Transfer | Checks that a transfer is blocked and throws a `FinWalletException` with code `INSUFFICIENT_BALANCE` if the sender's balance is lower than the transfer amount. |
| `SendMoneyCommand_ShouldFail_WhenTransferToSelf` | Peer-to-Peer Transfer | Checks that a user cannot transfer money to their own wallet. Throws `CANNOT_TRANSFER_TO_SELF`. |
| `SendMoneyCommand_ShouldFail_WhenSenderWalletIsFrozen` | Peer-to-Peer Transfer | Checks that frozen wallets are blocked from initiating transfers. Throws `WALLET_FROZEN`. |
| `SendMoneyCommand_ShouldSucceed_WhenBalancesAndLedgersAreValid` | Peer-to-Peer Transfer | Checks that a valid transfer completes correctly: <br>- Deducts amount from sender, adds to receiver.<br>- Generates two Ledger Entries (one Debit, one Credit).<br>- Ledger balances before/after are recorded correctly.<br>- Generates a printable `Receipt` with a unique receipt number prefix. |
| `RequestWalletTopUpCommand_ShouldCreatePendingTransaction` | Simulated Top-up | Checks that requesting a top-up:<br>- Creates a transaction with status `PendingBankApproval`.<br>- Records a `Pending` integration request.<br>- Publishes the `BankTopUpRequested` integration event to RabbitMQ using MassTransit. |

---

## 3. Manual E2E Testing Scenarios (Walkthroughs)

To verify the system end-to-end, execute the following manual tests:

### Scenario A: User Onboarding and Identity Security
- **Steps**:
  1. Click **Sign Up** from the Landing Page.
  2. Input invalid fields (e.g., matching password mismatch, weak credentials) and assert error banners.
  3. Enter valid credentials and submit.
- **Expected Results**:
  - Automatically redirects to the Customer Dashboard.
  - The wallet is automatically generated with a zero balance.
  - Check the database: A user record appears in `identity.Users` and a wallet appears in `wallet.Wallets`.

### Scenario B: Asynchronous Top-Up Execution
- **Steps**:
  1. Navigate to the **Top-Up** page.
  2. Input a valid card number and an amount of `100.000 LYD`.
  3. Submit the request.
- **Expected Results**:
  - Banners alert that the request is sent to the bank.
  - The transaction history lists a new top-up as `Pending Bank`.
  - After 2 seconds, the record updates to `Completed` (70% probability) or `Rejected` (30% probability) based on the worker's decision.
  - If approved, the wallet balance increases by `100.000 LYD`.

### Scenario C: P2P Money Transfer Integrity
- **Steps**:
  1. Create a second user account to receive funds, or look up their wallet number.
  2. In User 1, navigate to **Send Money**.
  3. Input User 2's wallet number to look up the owner. Assert that their full name is retrieved.
  4. Enter `40.000 LYD` and click transfer.
- **Expected Results**:
  - The transaction completes, showing a green checkmark page.
  - The user can view and print a cryptographic transaction receipt.
  - User 1's balance drops, while User 2's balance increases.
  - Check the database: Two ledger entries (Debit and Credit) are saved for this transaction.

### Scenario D: Compliance and Wallet Freezing
- **Steps**:
  1. Log in as Admin (`admin@finwallet.local` / `Admin@123456`).
  2. Go to **Manage Users** and find User 1.
  3. Click **Freeze Wallet** and enter "Fraudulent behavior detection".
  4. Log out and log back in as User 1.
  5. Try to transfer money to User 2.
- **Expected Results**:
  - The Admin dashboard registers the frozen count increment.
  - User 1's dashboard displays a `Frozen` status badge.
  - User 1's transfer requests fail with a warning message.

---

## 4. Running the Tests

To run the automated test suite, execute the following command in your terminal:

```powershell
dotnet test src/backend/FinWallet.slnx
```
This runs all xUnit integration and unit tests and outputs the results.
