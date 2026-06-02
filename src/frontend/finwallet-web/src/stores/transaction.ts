import { defineStore } from 'pinia';
import apiClient from '../api/client';

export interface TransactionState {
  transactions: any[];
  recentTransactions: any[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  loading: boolean;
}

export const useTransactionStore = defineStore('transaction', {
  state: (): TransactionState => ({
    transactions: [],
    recentTransactions: [],
    totalCount: 0,
    page: 1,
    pageSize: 10,
    totalPages: 0,
    loading: false,
  }),
  actions: {
    async fetchMyTransactions(filters: any = {}) {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/transactions/my-transactions', {
          params: {
            page: this.page,
            pageSize: this.pageSize,
            ...filters,
          },
        });
        const data = response.data;
        this.transactions = data.items;
        this.totalCount = data.totalCount;
        this.totalPages = data.totalPages;
        return data;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async fetchMyRecent(count = 5) {
      try {
        const response = await apiClient.get('/api/transactions/my-recent', {
          params: { count },
        });
        this.recentTransactions = response.data;
        return this.recentTransactions;
      } catch (error) {
        throw error;
      }
    },
    async fetchTransactionDetails(transactionId: string) {
      try {
        const response = await apiClient.get(`/api/transactions/${transactionId}`);
        return response.data; // { transaction, ledgerEntries, receipt }
      } catch (error) {
        throw error;
      }
    },
    async fetchReceiptByTransaction(transactionId: string) {
      try {
        const response = await apiClient.get(`/api/receipts/by-transaction/${transactionId}`);
        return response.data; // receipt details
      } catch (error) {
        throw error;
      }
    },
  },
});
