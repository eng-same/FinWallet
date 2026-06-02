import { defineStore } from 'pinia';
import apiClient from '../api/client';

export interface AdminState {
  dashboardStats: any | null;
  users: any[];
  wallets: any[];
  transactions: any[];
  auditLogs: any[];
  loading: boolean;
}

export const useAdminStore = defineStore('admin', {
  state: (): AdminState => ({
    dashboardStats: null,
    users: [],
    wallets: [],
    transactions: [],
    auditLogs: [],
    loading: false,
  }),
  actions: {
    async fetchDashboardStats() {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/admin/dashboard');
        this.dashboardStats = response.data;
        return this.dashboardStats;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async fetchUsers(search?: string) {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/admin/users', {
          params: { search },
        });
        this.users = response.data;
        return this.users;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async fetchUserDetails(userId: string) {
      try {
        const response = await apiClient.get(`/api/admin/users/${userId}`);
        return response.data; // { user, wallet, recentTransactions }
      } catch (error) {
        throw error;
      }
    },
    async fetchWallets(search?: string) {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/admin/wallets', {
          params: { search },
        });
        this.wallets = response.data;
        return this.wallets;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async fetchWalletDetails(walletId: string) {
      try {
        const response = await apiClient.get(`/api/admin/wallets/${walletId}`);
        return response.data; // { wallet, owner, statusHistories }
      } catch (error) {
        throw error;
      }
    },
    async freezeWallet(walletId: string, reason: string) {
      try {
        const response = await apiClient.put(`/api/admin/wallets/${walletId}/freeze`, { reason });
        return response.data;
      } catch (error) {
        throw error;
      }
    },
    async unfreezeWallet(walletId: string, reason: string) {
      try {
        const response = await apiClient.put(`/api/admin/wallets/${walletId}/unfreeze`, { reason });
        return response.data;
      } catch (error) {
        throw error;
      }
    },
    async fetchTransactions(filters: any = {}) {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/admin/transactions', {
          params: filters,
        });
        this.transactions = response.data;
        return this.transactions;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async fetchTransactionDetails(transactionId: string) {
      try {
        const response = await apiClient.get(`/api/admin/transactions/${transactionId}`);
        return response.data;
      } catch (error) {
        throw error;
      }
    },
    async fetchAuditLogs(search?: string) {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/admin/audit-logs', {
          params: { search },
        });
        this.auditLogs = response.data;
        return this.auditLogs;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
  },
});
