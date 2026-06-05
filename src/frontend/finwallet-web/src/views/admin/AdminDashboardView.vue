<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useAdminStore } from '../../stores/admin';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const adminStore = useAdminStore();
const router = useRouter();
const $q = useQuasar();

const loading = ref(false);

const loadDashboard = async () => {
  loading.value = true;
  try {
    await adminStore.fetchDashboardStats();
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load admin stats';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const getStatusColor = (status: string) => {
  switch (status.toLowerCase()) {
    case 'completed': return 'positive';
    case 'pendingbankapproval': return 'accent';
    case 'rejected': return 'negative';
    default: return 'grey';
  }
};

const getStatusLabel = (status: string) => {
  if (status.toLowerCase() === 'pendingbankapproval') return 'Pending Bank';
  return status;
};

// ApexCharts options
const chartOptions = computed(() => ({
  chart: {
    type: 'donut' as const,
    foreColor: '#94A3B8',
  },
  labels: ['Active Wallets', 'Frozen Wallets'],
  colors: ['#10B981', '#EF4444'],
  theme: {
    mode: 'dark' as const
  },
  legend: {
    position: 'bottom' as const
  },
  responsive: [{
    breakpoint: 480,
    options: {
      chart: {
        width: 200
      },
      legend: {
        position: 'bottom'
      }
    }
  }]
}));

const chartSeries = computed(() => {
  if (!adminStore.dashboardStats) return [0, 0];
  return [
    adminStore.dashboardStats.activeWallets,
    adminStore.dashboardStats.frozenWallets
  ];
});

onMounted(() => {
  loadDashboard();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <!-- Title -->
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">{{ $t('adminDashboard.title') }}</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('adminDashboard.subtitle') }}</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadDashboard" :loading="loading" />
    </div>

    <!-- Stat Grid -->
    <div class="row q-col-gutter-lg q-mb-xl" v-if="adminStore.dashboardStats">
      
      <!-- Total Users -->
      <div class="col-6 col-md-3">
        <q-card class="glass-card hover-scale">
          <q-card-section class="q-pa-md text-center">
            <q-icon name="people" size="32px" color="primary" class="q-mb-sm" />
            <div class="text-caption text-grey-5 text-uppercase letter-spacing-1 font-mono">{{ $t('adminDashboard.totalUsers') }}</div>
            <div class="text-h4 text-weight-bolder text-white q-mt-xs">
              {{ adminStore.dashboardStats.totalUsers }}
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Total Wallets -->
      <div class="col-6 col-md-3">
        <q-card class="glass-card hover-scale">
          <q-card-section class="q-pa-md text-center">
            <q-icon name="wallet" size="32px" color="emerald-4" class="q-mb-sm" />
            <div class="text-caption text-grey-5 text-uppercase letter-spacing-1 font-mono">{{ $t('adminDashboard.totalWallets') }}</div>
            <div class="text-h4 text-weight-bolder text-white q-mt-xs">
              {{ adminStore.dashboardStats.totalWallets }}
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Total Volume -->
      <div class="col-6 col-md-3">
        <q-card class="glass-card hover-scale">
          <q-card-section class="q-pa-md text-center">
            <q-icon name="payments" size="32px" color="secondary" class="q-mb-sm" />
            <div class="text-caption text-grey-5 text-uppercase letter-spacing-1 font-mono">{{ $t('adminDashboard.totalVolume') }}</div>
            <div class="text-h5 text-weight-bolder text-white q-mt-sm">
              {{ adminStore.dashboardStats.totalTransactionVolume.toFixed(3) }}
              <span class="text-caption text-indigo-3">LYD</span>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Today Volume -->
      <div class="col-6 col-md-3">
        <q-card class="glass-card hover-scale">
          <q-card-section class="q-pa-md text-center">
            <q-icon name="trending_up" size="32px" color="accent" class="q-mb-sm" />
            <div class="text-caption text-grey-5 text-uppercase letter-spacing-1 font-mono">{{ $t('adminDashboard.todayVolume') }}</div>
            <div class="text-h5 text-weight-bolder text-white q-mt-sm">
              {{ adminStore.dashboardStats.todayTransactionVolume.toFixed(3) }}
              <span class="text-caption text-indigo-3">LYD</span>
            </div>
          </q-card-section>
        </q-card>
      </div>

    </div>

    <!-- Charts & Metrics Row -->
    <div class="row q-col-gutter-lg q-mb-xl" v-if="adminStore.dashboardStats">
      
      <!-- Wallet Chart Card -->
      <div class="col-12 col-md-4">
        <q-card class="glass-card fit q-pa-md">
          <q-card-section class="q-pa-none">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-lg">
              {{ $t('adminDashboard.walletStatusRatio') }}
            </div>
            <div class="row justify-center">
              <apexchart
                width="280"
                type="donut"
                :options="chartOptions"
                :series="chartSeries"
              />
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Recent System Transactions Card -->
      <div class="col-12 col-md-8">
        <q-card class="glass-card fit">
          <q-card-section class="row items-center justify-between q-pa-md">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1">
              {{ $t('adminDashboard.globalActivity') }}
            </div>
            <q-btn
              flat
              dense
              color="primary"
              label="View All"
              no-caps
              class="text-weight-bold"
              @click="router.push('/admin/transactions')"
            />
          </q-card-section>

          <q-separator class="bg-grey-9" />

          <q-card-section class="q-pa-none">
            <q-list class="q-py-sm" v-if="adminStore.dashboardStats.recentTransactions.length > 0">
              <template v-for="(tx, idx) in adminStore.dashboardStats.recentTransactions" :key="tx.id">
                <q-item clickable @click="router.push(`/admin/transactions/${tx.id}`)" class="q-py-md hover-scale">
                  
                  <q-item-section>
                    <q-item-label class="text-weight-bold text-white row items-center">
                      {{ tx.type }}
                      <q-badge :color="getStatusColor(tx.status)" class="text-bold text-caption q-ml-sm">
                        {{ getStatusLabel(tx.status) }}
                      </q-badge>
                    </q-item-label>
                    <q-item-label caption class="text-grey-5 font-mono">{{ tx.referenceNumber }}</q-item-label>
                  </q-item-section>

                  <q-item-section class="gt-xs text-grey-4">
                    <div v-if="tx.fromWalletNumber" class="text-caption">
                      {{ $t('common.from') }}: <span class="font-mono text-indigo-3">{{ tx.fromWalletNumber }}</span>
                    </div>
                    <div v-if="tx.toWalletNumber" class="text-caption">
                      {{ $t('common.to') }}: <span class="font-mono text-emerald-4">{{ tx.toWalletNumber }}</span>
                    </div>
                  </q-item-section>

                  <q-item-section side class="text-right text-subtitle1 text-weight-bold text-white">
                    {{ tx.amount.toFixed(3) }}
                    <span class="text-caption text-grey-5 font-sans">LYD</span>
                  </q-item-section>
                </q-item>
                <q-separator class="bg-grey-9" v-if="Number(idx) < adminStore.dashboardStats.recentTransactions.length - 1" />
              </template>
            </q-list>
            <div class="text-center q-pa-xl text-grey-5" v-else>
              {{ $t('adminDashboard.noTx') }}
            </div>
          </q-card-section>
        </q-card>
      </div>

    </div>

    <!-- Loading spinner -->
    <div class="text-center q-pa-xl" v-else>
      <q-spinner color="primary" size="48px" class="q-my-xl" />
      <div class="text-grey-5">{{ $t('adminDashboard.loading') }}</div>
    </div>

  </q-page>
</template>

<style scoped>
.emerald-4 {
  color: #10B981;
}
.w-full {
  width: 100%;
}
</style>
