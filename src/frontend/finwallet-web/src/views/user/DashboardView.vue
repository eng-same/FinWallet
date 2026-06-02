<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useAuthStore } from '../../stores/auth';
import { useWalletStore } from '../../stores/wallet';
import { useTransactionStore } from '../../stores/transaction';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const authStore = useAuthStore();
const walletStore = useWalletStore();
const transactionStore = useTransactionStore();
const router = useRouter();
const $q = useQuasar();

const loading = ref(false);

const loadDashboardData = async () => {
  loading.value = true;
  try {
    await walletStore.fetchMyWallet();
    await transactionStore.fetchMyRecent(5);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load dashboard data';
    $q.notify({
      type: 'negative',
      message: errorMsg
    });
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

onMounted(() => {
  loadDashboardData();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <!-- Header Greetings -->
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">
          {{ $t('dashboard.welcome', { name: authStore.user?.fullName }) }}
        </h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('dashboard.subtitle') }}</p>
      </div>
      <q-btn
        flat
        round
        color="white"
        icon="refresh"
        @click="loadDashboardData"
        :loading="loading"
      >
        <q-tooltip>{{ $t('common.refresh') }}</q-tooltip>
      </q-btn>
    </div>

    <!-- Frozen Wallet Alert -->
    <q-banner
      v-if="walletStore.wallet?.status === 'Frozen'"
      rounded
      class="q-mb-lg glass-card text-white bg-red-10-dim"
      inline-actions
    >
      <template v-slot:avatar>
        <q-icon name="warning" color="negative" size="md" />
      </template>
      <div class="text-weight-bold text-subtitle1">{{ $t('dashboard.frozenAlert') }}</div>
      <div class="text-caption text-grey-3">
        {{ $t('dashboard.frozenReason', { reason: walletStore.wallet?.frozenReason || 'Under administrative review' }) }}
      </div>
    </q-banner>

    <!-- Stat Grid -->
    <div class="row q-col-gutter-lg q-mb-xl">
      
      <!-- Wallet Info Card -->
      <div class="col-12 col-md-6">
        <q-card class="glass-card hover-scale relative overflow-hidden" style="min-height: 200px;">
          <!-- Glowing circle background effect -->
          <div class="absolute-right bg-primary opacity-20" style="width: 150px; height: 150px; border-radius: 50%; top: -30px; right: -30px; filter: blur(40px);"></div>
          
          <q-card-section class="q-pa-lg">
            <div class="row items-center justify-between q-mb-md">
              <span class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1">
                {{ $t('dashboard.activeBalance') }}
              </span>
              <q-icon name="wallet" size="28px" color="indigo-3" />
            </div>

            <div class="text-h3 text-white text-weight-bolder q-mb-sm">
              {{ walletStore.wallet?.balance?.toFixed(3) || '0.000' }}
              <span class="text-h6 text-indigo-3 font-sans text-weight-medium">{{ $t('common.lyd') }}</span>
            </div>

            <div class="row items-center justify-between q-mt-lg border-top border-indigo q-pt-md">
              <div>
                <div class="text-caption text-grey-5">{{ $t('dashboard.walletNumber') }}</div>
                <div class="text-weight-bold font-mono text-subtitle2 text-white">
                  {{ walletStore.wallet?.walletNumber || $t('dashboard.generating') }}
                </div>
              </div>
              <div>
                <div class="text-caption text-grey-5 text-right">{{ $t('common.status') }}</div>
                <q-badge
                  :color="walletStore.wallet?.status === 'Active' ? 'secondary' : 'negative'"
                  class="text-bold text-capitalize"
                >
                  {{ walletStore.wallet?.status || 'Active' }}
                </q-badge>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Quick Actions Card -->
      <div class="col-12 col-md-6">
        <q-card class="glass-card fit">
          <q-card-section class="q-pa-lg">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-lg">
              {{ $t('dashboard.quickActions') }}
            </div>

            <div class="row q-col-gutter-md justify-center">
              <div class="col-6">
                <q-btn
                  stack
                  color="indigo-10"
                  text-color="white"
                  class="w-full q-py-lg glass-card hover-scale text-weight-bold"
                  no-caps
                  icon="add_card"
                  :label="$t('menu.topUp')"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/top-up')"
                />
              </div>
              <div class="col-6">
                <q-btn
                  stack
                  color="indigo-10"
                  text-color="white"
                  class="w-full q-py-lg glass-card hover-scale text-weight-bold"
                  no-caps
                  icon="send"
                  :label="$t('menu.sendMoney')"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/send')"
                />
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

    </div>

    <!-- Recent Transactions Table -->
    <q-card class="glass-card">
      <q-card-section class="row items-center justify-between q-pa-lg">
        <div class="text-subtitle1 text-weight-bold text-white">
          {{ $t('dashboard.recentTransactions') }}
        </div>
        <q-btn
          flat
          dense
          color="primary"
          :label="$t('dashboard.viewAll')"
          no-caps
          class="text-weight-bold"
          @click="router.push('/transactions')"
        />
      </q-card-section>

      <q-separator class="bg-grey-9" />

      <q-card-section class="q-pa-none">
        <q-list class="q-py-sm" v-if="transactionStore.recentTransactions.length > 0">
          <template v-for="(tx, idx) in transactionStore.recentTransactions" :key="tx.id">
            <q-item clickable @click="router.push(`/transactions/${tx.id}`)" class="q-py-md hover-scale">
              <q-item-section avatar>
                <q-avatar
                  :color="tx.type === 'TopUp' ? 'indigo-10' : 'grey-10'"
                  :text-color="tx.type === 'TopUp' ? 'indigo-3' : 'grey-3'"
                  size="48px"
                >
                  <q-icon :name="tx.type === 'TopUp' ? 'add' : 'call_made'" />
                </q-avatar>
              </q-item-section>

              <q-item-section>
                <q-item-label class="text-weight-bold text-white">{{ tx.type }}</q-item-label>
                <q-item-label caption class="text-grey-5 font-mono">{{ tx.referenceNumber }}</q-item-label>
              </q-item-section>

              <q-item-section side class="text-right">
                <div class="text-subtitle1 text-weight-bolder text-white">
                  {{ tx.type === 'TopUp' ? '+' : '-' }}{{ tx.amount.toFixed(3) }}
                  <span class="text-caption text-grey-5 font-sans">{{ $t('common.lyd') }}</span>
                </div>
                <q-badge :color="getStatusColor(tx.status)" class="text-bold text-caption q-mt-xs">
                  {{ getStatusLabel(tx.status) }}
                </q-badge>
              </q-item-section>
            </q-item>
            <q-separator class="bg-grey-9" v-if="Number(idx) < transactionStore.recentTransactions.length - 1" />
          </template>
        </q-list>

        <!-- No data state -->
        <div class="text-center q-pa-xl" v-else>
          <q-icon name="info" size="64px" color="grey-7" class="q-mb-md" />
          <div class="text-subtitle1 text-grey-5">{{ $t('dashboard.noTransactions') }}</div>
          <q-btn
            outline
            color="primary"
            :label="$t('dashboard.makeFirst')"
            no-caps
            class="q-mt-md rounded-borders hover-scale"
            :disabled="walletStore.wallet?.status !== 'Active'"
            @click="router.push('/wallet/send')"
          />
        </div>
      </q-card-section>
    </q-card>

  </q-page>
</template>

<style scoped>
.border-top {
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}
.opacity-20 {
  opacity: 0.2;
}
.w-full {
  width: 100%;
}
</style>
