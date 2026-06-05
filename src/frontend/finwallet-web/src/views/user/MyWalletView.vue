<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import { useWalletStore } from '../../stores/wallet';
import { useTransactionStore } from '../../stores/transaction';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const walletStore = useWalletStore();
const transactionStore = useTransactionStore();
const router = useRouter();
const $q = useQuasar();

const loading = ref(false);

const loadWalletDetails = async () => {
  loading.value = true;
  try {
    await walletStore.fetchMyWallet();
    await transactionStore.fetchMyRecent(5);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load wallet details';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const topUpVolume = computed(() => {
  return transactionStore.recentTransactions
    .filter(t => t.type === 'TopUp' && t.status === 'Completed')
    .reduce((sum, t) => sum + t.amount, 0);
});

const transferVolume = computed(() => {
  return transactionStore.recentTransactions
    .filter(t => t.type === 'Transfer' && t.status === 'Completed')
    .reduce((sum, t) => sum + t.amount, 0);
});

const netFlow = computed(() => topUpVolume.value - transferVolume.value);

const netFlowPercent = computed(() => {
  const total = topUpVolume.value + transferVolume.value;
  if (total === 0) return 50;
  return Math.round((topUpVolume.value / total) * 100);
});

onMounted(() => {
  loadWalletDetails();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <!-- Header -->
    <div class="row items-center justify-between q-mb-xl wallet-header">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">{{ $t('wallet.title') }}</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('wallet.subtitle') }}</p>
      </div>
      <q-btn
        flat
        round
        color="white"
        icon="refresh"
        @click="loadWalletDetails"
        :loading="loading"
        class="refresh-btn"
      >
        <q-tooltip>{{ $t('common.refresh') }}</q-tooltip>
      </q-btn>
    </div>

    <div class="row q-col-gutter-lg">
      
      <!-- Left side: Wallet Card -->
      <div class="col-12 col-md-7 fade-in-up" style="animation-delay: 0.05s;">
        <q-card class="wallet-hero-card relative overflow-hidden">
          <!-- Decorative glow orbs -->
          <div class="glow-orb glow-orb-primary"></div>
          <div class="glow-orb glow-orb-accent"></div>

          <!-- Card grid pattern overlay -->
          <div class="card-pattern"></div>
          
          <q-card-section class="q-pa-xl relative-position" style="z-index: 1;">
            <!-- Top row: label + status -->
            <div class="row items-center justify-between q-mb-lg">
              <div class="row items-center no-wrap">
                <div class="chip-icon q-mr-sm">
                  <q-icon name="credit_card" size="18px" color="amber-6" />
                </div>
                <span class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1">
                  {{ $t('wallet.spec') }}
                </span>
              </div>
              <q-badge
                :color="walletStore.wallet?.status === 'Active' ? 'secondary' : 'negative'"
                class="text-bold text-subtitle2 text-capitalize q-py-xs q-px-md status-badge"
              >
                <q-icon :name="walletStore.wallet?.status === 'Active' ? 'check_circle' : 'error'" size="14px" class="q-mr-xs" />
                {{ walletStore.wallet?.status || 'Active' }}
              </q-badge>
            </div>

            <!-- Wallet Number -->
            <div class="q-mb-xl">
              <div class="text-caption text-grey-5 q-mb-xs">{{ $t('dashboard.walletNumber') }}</div>
              <div class="text-h5 font-mono text-weight-bolder text-white wallet-number">
                {{ walletStore.wallet?.walletNumber || 'FW-000000000000' }}
              </div>
            </div>

            <!-- Balance -->
            <div class="q-mb-lg">
              <span class="text-caption text-grey-5 block q-mb-xs">{{ $t('wallet.availableBalance') }}</span>
              <div class="balance-display">
                <span class="text-h3 text-white text-weight-bolder">
                  {{ walletStore.wallet?.balance?.toFixed(3) || '0.000' }}
                </span>
                <span class="text-subtitle1 text-indigo-3 text-weight-medium q-ml-sm">{{ $t('common.lyd') }}</span>
              </div>
            </div>

            <q-separator class="separator-glow q-my-lg" />

            <!-- Bottom info row -->
            <div class="row q-col-gutter-md">
              <div class="col-6">
                <div class="text-caption text-grey-5">{{ $t('wallet.currencyUnit') }}</div>
                <div class="text-subtitle1 text-white text-weight-bold">{{ $t('wallet.libyanDinar') }}</div>
              </div>
              <div class="col-6">
                <div class="text-caption text-grey-5">{{ $t('wallet.precisionLimit') }}</div>
                <div class="text-subtitle1 text-white text-weight-bold">{{ $t('wallet.decimalPlaces') }}</div>
              </div>
            </div>

          </q-card-section>
        </q-card>
      </div>

      <!-- Right side: Quick actions & volume metrics -->
      <div class="col-12 col-md-5">

        <!-- Quick Actions -->
        <div class="fade-in-up" style="animation-delay: 0.15s;">
          <q-card class="glass-card q-mb-lg">
            <q-card-section class="q-pa-lg">
              <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-lg">
                {{ $t('wallet.manageBalance') }}
              </div>

              <div class="column q-gutter-md">
                <q-btn
                  unelevated
                  color="primary"
                  no-caps
                  class="w-full q-py-md action-btn text-weight-bold"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/top-up')"
                >
                  <div class="row items-center no-wrap q-gutter-sm">
                    <q-avatar size="32px" color="white" text-color="primary" class="action-icon-avatar">
                      <q-icon name="add_card" size="18px" />
                    </q-avatar>
                    <span>{{ $t('wallet.bankTopUp') }}</span>
                  </div>
                </q-btn>
                <q-btn
                  outline
                  color="indigo-3"
                  no-caps
                  class="w-full q-py-md action-btn text-weight-bold"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/send')"
                >
                  <div class="row items-center no-wrap q-gutter-sm">
                    <q-avatar size="32px" color="indigo-10" text-color="indigo-3" class="action-icon-avatar">
                      <q-icon name="send" size="18px" />
                    </q-avatar>
                    <span>{{ $t('wallet.p2pTransfer') }}</span>
                  </div>
                </q-btn>
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Volume Metrics -->
        <div class="fade-in-up" style="animation-delay: 0.25s;">
          <q-card class="glass-card">
            <q-card-section class="q-pa-lg">
              <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-lg">
                {{ $t('wallet.recentVolumes') }}
              </div>
              
              <div class="row q-col-gutter-md q-mb-md">
                <div class="col-6 text-center volume-cell">
                  <q-icon name="trending_up" size="22px" color="green-4" class="q-mb-xs" />
                  <div class="text-caption text-grey-5">{{ $t('wallet.topUpVolume') }}</div>
                  <div class="text-subtitle1 text-weight-bolder text-green-4">
                    +{{ topUpVolume.toFixed(3) }} <span class="text-caption">{{ $t('common.lyd') }}</span>
                  </div>
                </div>
                <div class="col-6 text-center volume-cell">
                  <q-icon name="trending_down" size="22px" color="red-4" class="q-mb-xs" />
                  <div class="text-caption text-grey-5">{{ $t('wallet.transferVolume') }}</div>
                  <div class="text-subtitle1 text-weight-bolder text-red-4">
                    -{{ transferVolume.toFixed(3) }} <span class="text-caption">{{ $t('common.lyd') }}</span>
                  </div>
                </div>
              </div>

              <!-- Net Flow Indicator -->
              <div class="net-flow-section q-mb-md">
                <div class="row items-center justify-between q-mb-xs">
                  <span class="text-caption text-grey-5">{{ $t('common.netFlow') }}</span>
                  <span class="text-caption text-weight-bold" :class="netFlow >= 0 ? 'text-green-4' : 'text-red-4'">
                    {{ netFlow >= 0 ? '+' : '' }}{{ netFlow.toFixed(3) }} {{ $t('common.lyd') }}
                  </span>
                </div>
                <div class="net-flow-bar">
                  <div
                    class="net-flow-fill"
                    :style="{ width: netFlowPercent + '%' }"
                    :class="netFlow >= 0 ? 'fill-positive' : 'fill-negative'"
                  ></div>
                </div>
              </div>

              <q-separator class="bg-grey-9 q-my-md" />
              
              <div class="text-center">
                <q-btn
                  flat
                  color="indigo-3"
                  :label="$t('wallet.viewAllTx')"
                  no-caps
                  class="text-weight-bold"
                  icon="history"
                  @click="router.push('/transactions')"
                />
              </div>
            </q-card-section>
          </q-card>
        </div>

      </div>

    </div>

  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.block {
  display: block;
}

/* ─── Wallet Hero Card ─── */
.wallet-hero-card {
  background: linear-gradient(135deg, rgba(30, 41, 72, 0.7) 0%, rgba(20, 28, 58, 0.85) 100%) !important;
  backdrop-filter: blur(20px) saturate(130%) !important;
  -webkit-backdrop-filter: blur(20px) saturate(130%) !important;
  border: 1px solid rgba(99, 102, 241, 0.2) !important;
  border-radius: 20px !important;
  overflow: hidden;
}

/* Decorative glow orbs */
.glow-orb {
  position: absolute;
  border-radius: 50%;
  pointer-events: none;
  filter: blur(60px);
}
.glow-orb-primary {
  width: 220px;
  height: 220px;
  top: -60px;
  right: -40px;
  background: rgba(99, 102, 241, 0.25);
  animation: float-glow 6s ease-in-out infinite;
}
.glow-orb-accent {
  width: 140px;
  height: 140px;
  bottom: -30px;
  left: 20px;
  background: rgba(139, 92, 246, 0.15);
  animation: float-glow 8s ease-in-out infinite reverse;
}

/* Subtle pattern overlay */
.card-pattern {
  position: absolute;
  inset: 0;
  opacity: 0.03;
  background-image: 
    radial-gradient(circle at 25% 25%, white 1px, transparent 1px),
    radial-gradient(circle at 75% 75%, white 1px, transparent 1px);
  background-size: 24px 24px;
  pointer-events: none;
}

/* Chip icon (credit card chip look) */
.chip-icon {
  width: 32px;
  height: 24px;
  border-radius: 4px;
  background: linear-gradient(135deg, rgba(251, 191, 36, 0.2), rgba(245, 158, 11, 0.35));
  border: 1px solid rgba(251, 191, 36, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Wallet number letter spacing */
.wallet-number {
  letter-spacing: 2px;
}

/* Balance animated shine */
.balance-display {
  display: flex;
  align-items: baseline;
}

/* Glowing separator */
.separator-glow {
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(99, 102, 241, 0.3), transparent) !important;
  border: none;
}

/* Status badge glow */
.status-badge {
  border-radius: 20px;
}

/* ─── Quick Action Buttons ─── */
.action-btn {
  border-radius: 14px !important;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.action-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(99, 102, 241, 0.25) !important;
}
.action-icon-avatar {
  transition: transform 0.25s ease;
}
.action-btn:hover .action-icon-avatar {
  transform: scale(1.1);
}

/* ─── Volume Cells ─── */
.volume-cell {
  padding: 12px 8px;
  border-radius: 12px;
  transition: background 0.2s ease;
}
.volume-cell:hover {
  background: rgba(255, 255, 255, 0.04);
}

/* ─── Net Flow Bar ─── */
.net-flow-section {
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.03);
  border-radius: 12px;
}
.net-flow-bar {
  height: 6px;
  border-radius: 3px;
  background: rgba(255, 255, 255, 0.06);
  overflow: hidden;
}
.net-flow-fill {
  height: 100%;
  border-radius: 3px;
  transition: width 0.8s cubic-bezier(0.22, 1, 0.36, 1);
}
.fill-positive {
  background: linear-gradient(90deg, #22c55e, #4ade80);
}
.fill-negative {
  background: linear-gradient(90deg, #ef4444, #f87171);
}

/* ─── Header ─── */
.wallet-header {
  animation: fade-in-down 0.4s ease-out;
}

.refresh-btn {
  transition: transform 0.3s ease;
}
.refresh-btn:hover {
  transform: rotate(90deg);
}

/* ─── Animations ─── */
@keyframes float-glow {
  0%, 100% { transform: translate(0, 0) scale(1); }
  50% { transform: translate(-10px, 10px) scale(1.08); }
}

@keyframes fade-in-down {
  from {
    opacity: 0;
    transform: translateY(-12px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.fade-in-up {
  animation: fade-in-up 0.5s ease-out both;
}

@keyframes fade-in-up {
  from {
    opacity: 0;
    transform: translateY(16px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
