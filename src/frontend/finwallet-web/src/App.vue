<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from './stores/auth';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';
import { useI18n } from 'vue-i18n';

const authStore = useAuthStore();
const router = useRouter();
const $q = useQuasar();
const { locale } = useI18n();

const leftDrawerOpen = ref(false);

const toggleLeftDrawer = () => {
  leftDrawerOpen.value = !leftDrawerOpen.value;
};

const handleLogout = () => {
  authStore.logout();
  router.push('/login');
  $q.notify({
    type: 'positive',
    message: 'Logged out successfully'
  });
};

const navigateTo = (path: string) => {
  router.push(path);
};

const toggleLanguage = () => {
  const newLocale = locale.value === 'en' ? 'ar' : 'en';
  locale.value = newLocale;
  localStorage.setItem('finwallet_locale', newLocale);
  updateLayoutDirection(newLocale);
};

const updateLayoutDirection = (lang: string) => {
  if (lang === 'ar') {
    document.documentElement.setAttribute('dir', 'rtl');
  } else {
    document.documentElement.setAttribute('dir', 'ltr');
  }
};

// Nav items based on role (using label keys instead of hardcoded labels)
const userNavs = [
  { labelKey: 'menu.dashboard', icon: 'dashboard', path: '/dashboard' },
  { labelKey: 'menu.myWallet', icon: 'account_balance_wallet', path: '/wallet' },
  { labelKey: 'menu.topUp', icon: 'add_card', path: '/wallet/top-up' },
  { labelKey: 'menu.sendMoney', icon: 'send', path: '/wallet/send' },
  { labelKey: 'menu.history', icon: 'history', path: '/transactions' },
  { labelKey: 'menu.profile', icon: 'person', path: '/profile' }
];

const adminNavs = [
  { labelKey: 'menu.dashboard', icon: 'analytics', path: '/admin/dashboard' },
  { labelKey: 'menu.manageUsers', icon: 'people', path: '/admin/users' },
  { labelKey: 'menu.manageWallets', icon: 'wallet', path: '/admin/wallets' },
  { labelKey: 'menu.systemTransactions', icon: 'list_alt', path: '/admin/transactions' },
  { labelKey: 'menu.auditLogs', icon: 'security', path: '/admin/audit-logs' }
];

onMounted(() => {
  // Set premium dark mode by default
  $q.dark.set(true);
  
  // Apply stored language layout direction
  updateLayoutDirection(locale.value);
});
</script>

<template>
  <q-layout view="hHh lpR fFf" class="bg-dark text-white font-sans">
    
    <!-- Header -->
    <q-header elevated v-if="authStore.isAuthenticated" class="bg-indigo-9 text-white">
      <q-toolbar>
        <q-btn flat dense round icon="menu" aria-label="Menu" @click="toggleLeftDrawer" />
        <q-toolbar-title class="text-weight-bold letter-spacing-1">
          FinWallet <span class="text-caption text-indigo-3 font-mono">Simulation</span>
        </q-toolbar-title>

        <q-space />

        <div class="row items-center q-gutter-md">
          
          <!-- Language Toggle Button -->
          <q-btn
            flat
            dense
            no-caps
            icon="language"
            :label="locale === 'en' ? 'EN' : 'AR'"
            @click="toggleLanguage"
            class="text-weight-bold q-px-sm"
          >
            <q-tooltip>{{ locale === 'en' ? 'Switch to Arabic' : 'التغيير إلى الإنجليزية' }}</q-tooltip>
          </q-btn>

          <q-badge color="secondary" text-color="white" class="text-bold" v-if="authStore.role">
            {{ authStore.role }}
          </q-badge>
          <div class="gt-xs text-subtitle2 text-weight-medium">
            {{ authStore.user?.fullName }}
          </div>
          <q-btn flat round dense icon="logout" @click="handleLogout">
            <q-tooltip>{{ $t('menu.logout') }}</q-tooltip>
          </q-btn>
        </div>
      </q-toolbar>
    </q-header>

    <!-- Navigation Drawer -->
    <q-drawer
      v-model="leftDrawerOpen"
      v-if="authStore.isAuthenticated"
      show-if-above
      bordered
      class="bg-grey-10 text-grey-3"
      :width="260"
      :side="locale === 'ar' ? 'right' : 'left'"
    >
      <q-scroll-area class="fit q-pa-sm">
        <div class="q-pa-md text-center border-bottom border-indigo">
          <q-avatar size="72px" color="indigo-10" class="shadow-5 q-mb-sm">
            <q-icon name="account_balance" size="36px" color="white" />
          </q-avatar>
          <div class="text-subtitle1 text-weight-bold text-white">{{ authStore.user?.fullName }}</div>
          <div class="text-caption text-grey-5">{{ authStore.user?.email }}</div>
        </div>

        <q-list class="q-mt-md">
          <q-item-label header class="text-weight-bold text-indigo-3 text-uppercase font-mono text-caption">
            {{ $t('menu.navigation') }}
          </q-item-label>

          <!-- User Nav Link list -->
          <template v-if="authStore.isUser || authStore.role === 'Admin'">
            <q-item
              v-for="nav in userNavs"
              :key="nav.path"
              clickable
              v-ripple
              :active="router.currentRoute.value.path === nav.path"
              active-class="text-indigo-4 bg-indigo-10-dim"
              @click="navigateTo(nav.path)"
              class="rounded-borders q-my-xs"
            >
              <q-item-section avatar>
                <q-icon :name="nav.icon" />
              </q-item-section>
              <q-item-section>
                <q-item-label class="text-weight-medium">{{ $t(nav.labelKey) }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>

          <q-separator class="q-my-md bg-grey-9" v-if="authStore.isAdmin" />

          <!-- Admin Nav Link list -->
          <template v-if="authStore.isAdmin">
            <q-item-label header class="text-weight-bold text-red-3 text-uppercase font-mono text-caption">
              {{ $t('menu.adminPanel') }}
            </q-item-label>
            <q-item
              v-for="nav in adminNavs"
              :key="nav.path"
              clickable
              v-ripple
              :active="router.currentRoute.value.path === nav.path"
              active-class="text-red-4 bg-red-10-dim"
              @click="navigateTo(nav.path)"
              class="rounded-borders q-my-xs"
            >
              <q-item-section avatar>
                <q-icon :name="nav.icon" color="red-4" />
              </q-item-section>
              <q-item-section>
                <q-item-label class="text-weight-medium text-red-2">{{ $t(nav.labelKey) }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>

        </q-list>
      </q-scroll-area>
    </q-drawer>

    <!-- Page Content Container -->
    <q-page-container>
      <router-view />
    </q-page-container>

  </q-layout>
</template>

<style>
/* Custom Global Styles for Premium Glassmorphism Look */
body {
  background-color: #0B0F19 !important;
}

/* English (LTR) Typography */
html:not([dir="rtl"]) body,
html:not([dir="rtl"]) .font-sans,
html:not([dir="rtl"]) .q-field,
html:not([dir="rtl"]) .q-btn,
html:not([dir="rtl"]) .q-item {
  font-family: 'Inter', -apple-system, sans-serif !important;
}

html:not([dir="rtl"]) h1,
html:not([dir="rtl"]) h2,
html:not([dir="rtl"]) h3,
html:not([dir="rtl"]) h4,
html:not([dir="rtl"]) h5,
html:not([dir="rtl"]) h6,
html:not([dir="rtl"]) .text-h1,
html:not([dir="rtl"]) .text-h2,
html:not([dir="rtl"]) .text-h3,
html:not([dir="rtl"]) .text-h4,
html:not([dir="rtl"]) .text-h5,
html:not([dir="rtl"]) .text-h6,
html:not([dir="rtl"]) .q-toolbar-title {
  font-family: 'Outfit', -apple-system, sans-serif !important;
}

/* Arabic (RTL) Typography */
html[dir="rtl"] body,
html[dir="rtl"] .font-sans,
html[dir="rtl"] h1,
html[dir="rtl"] h2,
html[dir="rtl"] h3,
html[dir="rtl"] h4,
html[dir="rtl"] h5,
html[dir="rtl"] h6,
html[dir="rtl"] .text-h1,
html[dir="rtl"] .text-h2,
html[dir="rtl"] .text-h3,
html[dir="rtl"] .text-h4,
html[dir="rtl"] .text-h5,
html[dir="rtl"] .text-h6,
html[dir="rtl"] .q-btn,
html[dir="rtl"] .q-item,
html[dir="rtl"] .q-field,
html[dir="rtl"] .q-toolbar-title,
html[dir="rtl"] .q-tooltip {
  font-family: 'Cairo', 'Segoe UI', Tahoma, sans-serif !important;
}

.border-bottom {
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.border-indigo {
  border-bottom: 1px solid rgba(79, 70, 229, 0.2);
}

/* Glassmorphism card utility */
.glass-card {
  background: rgba(30, 41, 59, 0.45) !important;
  backdrop-filter: blur(16px) saturate(120%) !important;
  -webkit-backdrop-filter: blur(16px) saturate(120%) !important;
  border: 1px solid rgba(255, 255, 255, 0.08) !important;
  border-radius: 16px !important;
}

.glass-card-dim {
  background: rgba(15, 23, 42, 0.6) !important;
  backdrop-filter: blur(10px) !important;
  border: 1px solid rgba(255, 255, 255, 0.05) !important;
  border-radius: 12px !important;
}

.bg-indigo-10-dim {
  background: rgba(79, 70, 229, 0.15) !important;
}

.bg-red-10-dim {
  background: rgba(239, 68, 68, 0.12) !important;
}

/* Animations */
.hover-scale {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.hover-scale:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px rgba(0, 0, 0, 0.3) !important;
}

/* Spacings and Lettering */
.letter-spacing-1 {
  letter-spacing: 1px;
}
</style>
