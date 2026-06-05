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

onMounted(() => {
  loadWalletDetails();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">{{ $t('wallet.title') }}</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('wallet.subtitle') }}</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadWalletDetails" :loading="loading" />
    </div>

    <div class="row q-col-gutter-lg">
      
      <!-- Left side: Wallet Details -->
      <div class="col-12 col-md-7">
        <q-card class="glass-card q-mb-lg relative overflow-hidden">
          <div class="absolute-right bg-indigo-9 opacity-20" style="width: 200px; height: 200px; border-radius: 50%; top: -50px; right: -50px; filter: blur(50px);"></div>
          
          <q-card-section class="q-pa-lg">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold q-mb-md">
              {{ $t('wallet.spec') }}
            </div>

            <div class="row items-center justify-between q-mb-xl">
              <div>
                <div class="text-caption text-grey-5">{{ $t('dashboard.walletNumber') }}</div>
                <div class="text-h5 font-mono text-weight-bolder text-white">
                  {{ walletStore.wallet?.walletNumber || 'FW-000000000000' }}
                </div>
              </div>
              <q-badge
                :color="walletStore.wallet?.status === 'Active' ? 'secondary' : 'negative'"
                class="text-bold text-subtitle2 text-capitalize q-py-xs q-px-md"
              >
                {{ walletStore.wallet?.status || 'Active' }}
              </q-badge>
            </div>

            <div class="q-mb-md">
              <span class="text-caption text-grey-5 block">{{ $t('wallet.availableBalance') }}</span>
              <span class="text-h3 text-white text-weight-bolder">
                {{ walletStore.wallet?.balance?.toFixed(3) || '0.000' }}
                <span class="text-subtitle1 text-indigo-3 text-weight-medium">{{ $t('common.lyd') }}</span>
              </span>
            </div>

            <q-separator class="bg-grey-9 q-my-lg" />

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

        <!-- Academic Ledger Information -->
        <q-card class="glass-card bg-indigo-10-dim border-indigo">
          <q-card-section class="q-pa-lg">
            <div class="row items-center q-mb-md">
              <q-icon name="school" size="28px" color="indigo-4" class="q-mr-sm" />
              <div class="text-subtitle1 text-weight-bold text-white">{{ $t('wallet.ledgerConcept') }}</div>
            </div>
            <p class="text-body2 text-grey-4 leading-relaxed">
              {{ $t('wallet.ledgerDescription') }}
            </p>
            <ul class="text-caption text-grey-5 q-mt-md q-pl-md leading-relaxed">
              <li>{{ $t('wallet.debitPoint') }}</li>
              <li>{{ $t('wallet.creditPoint') }}</li>
              <li>{{ $t('wallet.balanceCheckPoint') }}</li>
              <li>{{ $t('wallet.constraintPoint') }}</li>
            </ul>
          </q-card-section>
        </q-card>

      </div>

      <!-- Right side: Quick actions & volume metrics -->
      <div class="col-12 col-md-5">
        <q-card class="glass-card q-mb-lg">
          <q-card-section class="q-pa-lg">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold q-mb-lg">
              {{ $t('wallet.manageBalance') }}
            </div>

            <div class="row q-col-gutter-md">
              <div class="col-12">
                <q-btn
                  color="primary"
                  icon="add_card"
                  :label="$t('wallet.bankTopUp')"
                  no-caps
                  class="w-full q-py-md hover-scale text-weight-bold"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/top-up')"
                />
              </div>
              <div class="col-12">
                <q-btn
                  outline
                  color="indigo-3"
                  icon="send"
                  :label="$t('wallet.p2pTransfer')"
                  no-caps
                  class="w-full q-py-md hover-scale text-weight-bold"
                  :disabled="walletStore.wallet?.status !== 'Active'"
                  @click="router.push('/wallet/send')"
                />
              </div>
            </div>
          </q-card-section>
        </q-card>

        <q-card class="glass-card">
          <q-card-section class="q-pa-lg">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold q-mb-lg">
              {{ $t('wallet.recentVolumes') }}
            </div>
            
            <div class="row q-col-gutter-md">
              <div class="col-6 text-center border-right border-grey-9">
                <div class="text-caption text-grey-5">{{ $t('wallet.topUpVolume') }}</div>
                <div class="text-subtitle1 text-weight-bolder text-green-4">
                  +{{ topUpVolume.toFixed(3) }} <span class="text-caption">{{ $t('common.lyd') }}</span>
                </div>
              </div>
              <div class="col-6 text-center">
                <div class="text-caption text-grey-5">{{ $t('wallet.transferVolume') }}</div>
                <div class="text-subtitle1 text-weight-bolder text-red-4">
                  -{{ transferVolume.toFixed(3) }} <span class="text-caption">{{ $t('common.lyd') }}</span>
                </div>
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

  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.leading-relaxed {
  line-height: 1.6;
}
.border-right {
  border-right: 1px solid rgba(255, 255, 255, 0.08);
}
.block {
  display: block;
}
</style>
