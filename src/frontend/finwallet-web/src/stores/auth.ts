import { defineStore } from 'pinia';
import apiClient from '../api/client';

export interface User {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface Wallet {
  id: string;
  userId: string;
  walletNumber: string;
  balance: number;
  currency: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  frozenAt: string | null;
  frozenReason: string | null;
}

export interface AuthState {
  token: string | null;
  user: User | null;
  role: string | null;
  wallet: Wallet | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('finwallet_token'),
    user: JSON.parse(localStorage.getItem('finwallet_user') || 'null'),
    role: localStorage.getItem('finwallet_role'),
    wallet: JSON.parse(localStorage.getItem('finwallet_wallet') || 'null'),
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    isAdmin: (state) => state.role === 'Admin',
    isUser: (state) => state.role === 'User',
  },
  actions: {
    async login(credentials: any) {
      try {
        const response = await apiClient.post('/api/auth/login', credentials);
        const data = response.data;
        this.setAuth(data);
        return data;
      } catch (error) {
        throw error;
      }
    },
    async register(userData: any) {
      try {
        const response = await apiClient.post('/api/auth/register', userData);
        const data = response.data;
        this.setAuth(data);
        return data;
      } catch (error) {
        throw error;
      }
    },
    setAuth(data: any) {
      this.token = data.token;
      this.user = data.user;
      this.role = data.role;
      this.wallet = data.wallet || null;

      localStorage.setItem('finwallet_token', data.token);
      localStorage.setItem('finwallet_user', JSON.stringify(data.user));
      localStorage.setItem('finwallet_role', data.role);
      if (data.wallet) {
        localStorage.setItem('finwallet_wallet', JSON.stringify(data.wallet));
      } else {
        localStorage.removeItem('finwallet_wallet');
      }
    },
    async refreshMe() {
      if (!this.token) return;
      try {
        const response = await apiClient.get('/api/auth/me');
        this.user = response.data;
        localStorage.setItem('finwallet_user', JSON.stringify(this.user));
      } catch (error) {
        this.logout();
      }
    },
    logout() {
      this.token = null;
      this.user = null;
      this.role = null;
      this.wallet = null;

      localStorage.removeItem('finwallet_token');
      localStorage.removeItem('finwallet_user');
      localStorage.removeItem('finwallet_role');
      localStorage.removeItem('finwallet_wallet');
    },
  },
});
