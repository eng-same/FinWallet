import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const routes: Array<RouteRecordRaw> = [
  // Public
  {
    path: '/',
    name: 'Landing',
    component: () => import('../views/LandingView.vue'),
    meta: { public: true }
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/LoginView.vue'),
    meta: { public: true, guestOnly: true }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/RegisterView.vue'),
    meta: { public: true, guestOnly: true }
  },
  {
    path: '/unauthorized',
    name: 'Unauthorized',
    component: () => import('../views/UnauthorizedView.vue'),
    meta: { public: true }
  },
  {
    path: '/server-error',
    name: 'ServerError',
    component: () => import('../views/ServerErrorView.vue'),
    meta: { public: true }
  },

  // User Routes
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: () => import('../views/user/DashboardView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/wallet',
    name: 'MyWallet',
    component: () => import('../views/user/MyWalletView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/wallet/top-up',
    name: 'TopUp',
    component: () => import('../views/user/TopUpView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/wallet/send',
    name: 'SendMoney',
    component: () => import('../views/user/SendMoneyView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/transactions',
    name: 'TransactionHistory',
    component: () => import('../views/user/TransactionHistoryView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/transactions/:id',
    name: 'TransactionDetails',
    component: () => import('../views/user/TransactionDetailsView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/transactions/:id/receipt',
    name: 'Receipt',
    component: () => import('../views/user/ReceiptView.vue'),
    meta: { requiresAuth: true, role: 'User' }
  },
  {
    path: '/profile',
    name: 'Profile',
    component: () => import('../views/user/ProfileView.vue'),
    meta: { requiresAuth: true }
  },

  // Admin Routes
  {
    path: '/admin/dashboard',
    name: 'AdminDashboard',
    component: () => import('../views/admin/AdminDashboardView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/users',
    name: 'AdminUsers',
    component: () => import('../views/admin/UsersManagementView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/users/:id',
    name: 'AdminUserDetails',
    component: () => import('../views/admin/UserDetailsView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/wallets',
    name: 'AdminWallets',
    component: () => import('../views/admin/WalletsManagementView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/wallets/:id',
    name: 'AdminWalletDetails',
    component: () => import('../views/admin/WalletDetailsView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/transactions',
    name: 'AdminTransactions',
    component: () => import('../views/admin/TransactionsManagementView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/transactions/:id',
    name: 'AdminTransactionDetails',
    component: () => import('../views/admin/AdminTransactionDetailsView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },
  {
    path: '/admin/audit-logs',
    name: 'AdminAuditLogs',
    component: () => import('../views/admin/AuditLogsView.vue'),
    meta: { requiresAuth: true, role: 'Admin' }
  },

  // Catch all (404)
  {
    path: '/:catchAll(.*)*',
    name: 'NotFound',
    component: () => import('../views/NotFoundView.vue'),
    meta: { public: true }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore();

  // Redirect guest-only pages (login/register) to respective dashboards if already authenticated
  if (to.meta.guestOnly && authStore.isAuthenticated) {
    if (authStore.isAdmin) {
      return next('/admin/dashboard');
    }
    return next('/dashboard');
  }

  // Require Auth Guard
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return next('/login');
  }

  // Role Authorization Guard
  if (to.meta.role && authStore.isAuthenticated) {
    const requiredRole = to.meta.role as string;
    const userRole = authStore.role;

    if (requiredRole === 'Admin' && userRole !== 'Admin') {
      return next('/unauthorized');
    }

    if (requiredRole === 'User' && userRole !== 'User') {
      // Admin should be allowed to view user dashboard if they navigate there, or we redirect them back
      if (userRole === 'Admin') {
        // Allow Admin to view User pages (very standard for diagnostic purposes)
        return next();
      }
      return next('/unauthorized');
    }
  }

  next();
});

export default router;
