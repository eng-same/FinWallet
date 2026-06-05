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
  { labelKey: 'menu.auditLogs', icon: 'security', path: '/admin/audit-logs' },
  { labelKey: 'menu.profile', icon: 'person', path: '/profile' }
];

onMounted(() => {
  // Set premium dark mode by default
  $q.dark.set(true);
  
  // Apply stored language layout direction
  updateLayoutDirection(locale.value);
});
</script>

<template>
  <q-layout view="hHh Lpr fFf" class="bg-dark text-white font-sans premium-layout">
    
    <!-- Header -->
    <q-header v-if="authStore.isAuthenticated" class="glass-header text-white" :class="{'drawer-open': leftDrawerOpen}">
      <q-toolbar class="q-py-sm">
        <q-btn flat dense round icon="menu_open" aria-label="Menu" @click="toggleLeftDrawer" class="menu-btn" :class="{'rotate-180': !leftDrawerOpen}" />
        <q-toolbar-title class="text-weight-bold letter-spacing-1 flex items-center q-pl-md">
          <q-icon name="account_balance" color="indigo-4" size="28px" class="q-mr-sm logo-icon" />
          <span class="brand-text">FinWallet</span>
          <span class="text-caption text-indigo-3 font-mono q-ml-sm badge-platform">Platform</span>
        </q-toolbar-title>

        <q-space />

        <div class="row items-center q-gutter-x-sm">
          
          <!-- Language Toggle Button -->
          <q-btn
            flat
            dense
            no-caps
            icon="language"
            :label="locale === 'en' ? 'EN' : 'AR'"
            @click="toggleLanguage"
            class="text-weight-bold q-px-sm lang-btn"
          >
            <q-tooltip class="bg-indigo-10 text-white shadow-4">{{ locale === 'en' ? 'Switch to Arabic' : 'التغيير إلى الإنجليزية' }}</q-tooltip>
          </q-btn>

          <div class="header-divider hidden sm:block"></div>

          <q-badge color="indigo-6" text-color="white" class="role-badge q-px-sm q-py-xs text-bold shadow-3" v-if="authStore.role">
            <q-icon :name="authStore.isAdmin ? 'admin_panel_settings' : 'verified_user'" size="14px" class="q-mr-xs" />
            {{ authStore.role }}
          </q-badge>
          
          <div class="gt-xs text-subtitle2 text-weight-medium q-px-sm user-name-display">
            {{ authStore.user?.fullName }}
          </div>
          
          <q-btn flat round dense icon="logout" @click="handleLogout" class="logout-btn q-ml-xs" color="grey-4">
            <q-tooltip class="bg-red-10 text-white shadow-4">{{ $t('menu.logout') }}</q-tooltip>
          </q-btn>
        </div>
      </q-toolbar>
    </q-header>

    <!-- Navigation Drawer -->
    <q-drawer
      v-model="leftDrawerOpen"
      v-if="authStore.isAuthenticated"
      show-if-above
      class="sidebar-glass text-grey-3"
      :width="280"
      :side="locale === 'ar' ? 'right' : 'left'"
    >
      <q-scroll-area class="fit drawer-scroll-area">
        <div class="sidebar-profile-section q-pa-lg text-center relative-position overflow-hidden">
          <div class="sidebar-glow-orb"></div>
          
          <div class="avatar-container q-mx-auto q-mb-md">
            <q-avatar size="80px" class="profile-avatar shadow-10 text-white font-sans text-h4 text-weight-bold">
              {{ authStore.user?.fullName ? authStore.user.fullName.charAt(0).toUpperCase() : '' }}
              <q-icon v-if="!authStore.user?.fullName" name="person" size="48px" color="white" />
            </q-avatar>
            <div class="avatar-ring-animated"></div>
          </div>
          
          <div class="text-h6 text-weight-bolder text-white q-mt-sm tracking-wide">{{ authStore.user?.fullName }}</div>
          <div class="text-caption text-indigo-3 font-mono">{{ authStore.user?.email }}</div>
        </div>

        <q-separator class="drawer-separator q-mx-md" />

        <q-list class="q-pa-md nav-list">
          <!-- User Nav Link list -->
          <template v-if="!authStore.isAdmin">
            <q-item-label header class="nav-section-title text-indigo-3">
              {{ $t('menu.navigation') }}
            </q-item-label>
            <q-item
              v-for="nav in userNavs"
              :key="nav.path"
              clickable
              v-ripple
              :active="router.currentRoute.value.path === nav.path"
              active-class="nav-item-active user-active"
              @click="navigateTo(nav.path)"
              class="nav-item rounded-borders q-mb-sm"
            >
              <q-item-section avatar class="nav-icon-section">
                <q-icon :name="nav.icon" class="nav-icon" />
              </q-item-section>
              <q-item-section>
                <q-item-label class="text-weight-medium tracking-wide">{{ $t(nav.labelKey) }}</q-item-label>
              </q-item-section>
              <div class="active-indicator"></div>
            </q-item>
          </template>

          <!-- Admin Nav Link list -->
          <template v-if="authStore.isAdmin">
            <q-item-label header class="nav-section-title text-red-4">
              {{ $t('menu.adminPanel') }}
            </q-item-label>
            <q-item
              v-for="nav in adminNavs"
              :key="nav.path"
              clickable
              v-ripple
              :active="router.currentRoute.value.path === nav.path"
              active-class="nav-item-active admin-active"
              @click="navigateTo(nav.path)"
              class="nav-item rounded-borders q-mb-sm admin-item"
            >
              <q-item-section avatar class="nav-icon-section">
                <q-icon :name="nav.icon" class="nav-icon" />
              </q-item-section>
              <q-item-section>
                <q-item-label class="text-weight-medium tracking-wide">{{ $t(nav.labelKey) }}</q-item-label>
              </q-item-section>
              <div class="active-indicator"></div>
            </q-item>
          </template>

        </q-list>
      </q-scroll-area>
    </q-drawer>

    <!-- Page Content Container -->
    <q-page-container class="page-container-bg">
      <router-view v-slot="{ Component }">
        <transition name="fade-page" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </q-page-container>

  </q-layout>
</template>

<style>
/* Custom Global Styles for Premium Glassmorphism Look */
body {
  background-color: #0B0F19 !important;
  background-image: radial-gradient(circle at top right, rgba(30, 27, 75, 0.4), transparent 40%),
                    radial-gradient(circle at bottom left, rgba(17, 24, 39, 0.8), #0B0F19 60%);
  background-attachment: fixed;
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
.tracking-wide {
  letter-spacing: 0.025em;
}

/* --- PREMIUM NAVBAR STYLES --- */
.glass-header {
  background: rgba(11, 15, 25, 0.75) !important;
  backdrop-filter: blur(16px) saturate(150%) !important;
  -webkit-backdrop-filter: blur(16px) saturate(150%) !important;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  box-shadow: 0 4px 30px rgba(0, 0, 0, 0.1);
  transition: background 0.3s ease;
}

.brand-text {
  background: linear-gradient(to right, #818cf8, #c084fc);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
  font-weight: 800;
  letter-spacing: -0.5px;
}

.badge-platform {
  background: rgba(79, 70, 229, 0.15);
  padding: 2px 6px;
  border-radius: 6px;
  border: 1px solid rgba(79, 70, 229, 0.3);
  font-size: 0.65rem;
  vertical-align: middle;
}

.logo-icon {
  filter: drop-shadow(0 0 8px rgba(129, 140, 248, 0.4));
}

.menu-btn {
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #94a3b8;
}
.menu-btn:hover {
  color: #fff;
  background: rgba(255, 255, 255, 0.05);
}
.rotate-180 {
  transform: rotate(180deg);
}

.lang-btn {
  border-radius: 8px;
  color: #cbd5e1;
  transition: all 0.2s ease;
}
.lang-btn:hover {
  background: rgba(79, 70, 229, 0.15);
  color: #fff;
}

.header-divider {
  width: 1px;
  height: 24px;
  background: rgba(255, 255, 255, 0.1);
  margin: 0 8px;
}

.role-badge {
  border-radius: 8px;
  background: linear-gradient(135deg, #4f46e5, #6366f1) !important;
}

.user-name-display {
  color: #e2e8f0;
}

.logout-btn {
  transition: all 0.2s ease;
}
.logout-btn:hover {
  color: #ef4444 !important;
  background: rgba(239, 68, 68, 0.1);
  transform: rotate(5deg) scale(1.05);
}

/* --- PREMIUM SIDEBAR STYLES --- */
.sidebar-glass {
  background: rgba(15, 23, 42, 0.6) !important;
  backdrop-filter: blur(20px) saturate(150%) !important;
  -webkit-backdrop-filter: blur(20px) saturate(150%) !important;
  border-right: 1px solid rgba(255, 255, 255, 0.05);
  box-shadow: 4px 0 24px rgba(0, 0, 0, 0.2);
}
html[dir="rtl"] .sidebar-glass {
  border-right: none;
  border-left: 1px solid rgba(255, 255, 255, 0.05);
  box-shadow: -4px 0 24px rgba(0, 0, 0, 0.2);
}

.drawer-scroll-area {
  height: 100%;
}

.sidebar-profile-section {
  padding-top: 40px !important;
  padding-bottom: 30px !important;
  background: linear-gradient(180deg, rgba(30, 27, 75, 0.2) 0%, transparent 100%);
}

.sidebar-glow-orb {
  position: absolute;
  top: -40px;
  left: 50%;
  transform: translateX(-50%);
  width: 120px;
  height: 120px;
  background: radial-gradient(circle, rgba(99, 102, 241, 0.3) 0%, transparent 70%);
  filter: blur(20px);
  pointer-events: none;
}

.avatar-container {
  position: relative;
  width: 80px;
  height: 80px;
}

.profile-avatar {
  background: linear-gradient(135deg, #1e1b4b, #312e81) !important;
  border: 2px solid rgba(129, 140, 248, 0.3);
  position: relative;
  z-index: 2;
}

.avatar-ring-animated {
  position: absolute;
  inset: -6px;
  border-radius: 50%;
  border: 1px solid rgba(129, 140, 248, 0.5);
  animation: ripple 2.5s infinite cubic-bezier(0.2, 0.8, 0.2, 1);
  z-index: 1;
}

@keyframes ripple {
  0% { transform: scale(0.9); opacity: 1; }
  100% { transform: scale(1.3); opacity: 0; }
}

.drawer-separator {
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent) !important;
  height: 1px;
  border: none;
}

.nav-list {
  padding-top: 20px;
}

.nav-section-title {
  font-size: 0.7rem !important;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  font-weight: 700;
  margin-top: 8px;
  margin-bottom: 8px;
  padding-left: 12px;
  font-family: 'Outfit', sans-serif;
  opacity: 0.8;
}

.nav-item {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border: 1px solid transparent;
  color: #94a3b8;
  position: relative;
  overflow: hidden;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.03);
  color: #e2e8f0;
  transform: translateX(4px);
}
html[dir="rtl"] .nav-item:hover {
  transform: translateX(-4px);
}

.nav-icon-section {
  min-width: 44px;
  padding-right: 12px;
}
html[dir="rtl"] .nav-icon-section {
  padding-right: 0;
  padding-left: 12px;
}

.nav-icon {
  font-size: 22px;
  transition: transform 0.3s ease, color 0.3s ease;
}

.nav-item:hover .nav-icon {
  transform: scale(1.1);
  color: #fff;
}

.active-indicator {
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%) scaleY(0);
  width: 4px;
  height: 24px;
  border-radius: 0 4px 4px 0;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
html[dir="rtl"] .active-indicator {
  left: auto;
  right: 0;
  border-radius: 4px 0 0 4px;
}

.nav-item-active.user-active {
  background: linear-gradient(90deg, rgba(79, 70, 229, 0.15), rgba(79, 70, 229, 0.05)) !important;
  border: 1px solid rgba(79, 70, 229, 0.2);
  color: #fff !important;
}
html[dir="rtl"] .nav-item-active.user-active {
  background: linear-gradient(270deg, rgba(79, 70, 229, 0.15), rgba(79, 70, 229, 0.05)) !important;
}

.nav-item-active.user-active .nav-icon {
  color: #818cf8;
}

.nav-item-active.user-active .active-indicator {
  background: #818cf8;
  transform: translateY(-50%) scaleY(1);
  box-shadow: 0 0 8px #818cf8;
}

.nav-item.admin-item:hover {
  background: rgba(239, 68, 68, 0.05);
}

.nav-item-active.admin-active {
  background: linear-gradient(90deg, rgba(239, 68, 68, 0.15), rgba(239, 68, 68, 0.05)) !important;
  border: 1px solid rgba(239, 68, 68, 0.2);
  color: #fff !important;
}
html[dir="rtl"] .nav-item-active.admin-active {
  background: linear-gradient(270deg, rgba(239, 68, 68, 0.15), rgba(239, 68, 68, 0.05)) !important;
}

.nav-item-active.admin-active .nav-icon {
  color: #f87171;
}

.nav-item-active.admin-active .active-indicator {
  background: #f87171;
  transform: translateY(-50%) scaleY(1);
  box-shadow: 0 0 8px #f87171;
}

/* Page transition */
.fade-page-enter-active,
.fade-page-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-page-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-page-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
