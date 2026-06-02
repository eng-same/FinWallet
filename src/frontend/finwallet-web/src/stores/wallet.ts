import { defineStore } from 'pinia';
import apiClient from '../api/client';
import { useAuthStore } from './auth';

export interface WalletState {
  wallet: any | null;
  loading: boolean;
}

export const useWalletStore = defineStore('wallet', {
  state: (): WalletState => ({
    wallet: null,
    loading: false,
  }),
  actions: {
    async fetchMyWallet() {
      this.loading = true;
      try {
        const response = await apiClient.get('/api/wallets/my-wallet');
        this.wallet = response.data;
        
        // Sync with auth store
        const authStore = useAuthStore();
        authStore.wallet = this.wallet;
        localStorage.setItem('finwallet_wallet', JSON.stringify(this.wallet));
        
        return this.wallet;
      } catch (error) {
        throw error;
      } finally {
        this.loading = false;
      }
    },
    async lookupWallet(walletNumber: string) {
      try {
        const response = await apiClient.get(`/api/wallets/lookup/${walletNumber}`);
        return response.data; // { walletNumber, fullName }
      } catch (error) {
        throw error;
      }
    },
    async requestTopUp(amount: number, description: string) {
      try {
        const response = await apiClient.post('/api/wallets/top-up', {
          amount,
          currency: 'LYD',
          description,
        });
        return response.data; // Pending transaction
      } catch (error) {
        throw error;
      }
    },
    async sendMoney(recipientWalletNumber: string, amount: number, description: string) {
      try {
        const response = await apiClient.post('/api/transfers/send', {
          toWalletNumber: recipientWalletNumber,
          amount,
          currency: 'LYD',
          description,
        });
        
        // Re-fetch our wallet to sync balance
        await this.fetchMyWallet();
        
        return response.data; // Completed transaction
      } catch (error) {
        throw error;
      }
    },
  },
});
