<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAdminStore } from '../../stores/admin';
import { useQuasar } from 'quasar';

const route = useRoute();
const router = useRouter();
const adminStore = useAdminStore();
const $q = useQuasar();

const txId = route.params.id as string;
const txDetails = ref<any | null>(null);
const loading = ref(false);

const loadDetails = async () => {
  loading.value = true;
  try {
    txDetails.value = await adminStore.fetchTransactionDetails(txId);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load transaction details';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const getStatusColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'completed': return 'positive';
    case 'pendingbankapproval': return 'accent';
    case 'rejected': return 'negative';
    default: return 'grey';
  }
};

const getStatusLabel = (status: string) => {
  if (status?.toLowerCase() === 'pendingbankapproval') return 'Pending Bank Approval';
  return status;
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString();
};

onMounted(() => {
  loadDetails();
});
</script>

<template>
  <q-page class="q-pa-lg flex flex-center">
    <div class="w-full max-w-2xl">
      
      <!-- Back Btn -->
      <div class="row items-center justify-between q-mb-md">
        <q-btn
          flat
          dense
          color="grey-5"
          icon="arrow_back"
          label="Back to Registry"
          no-caps
          @click="router.push('/admin/transactions')"
        />
        <q-btn
          flat
          dense
          color="white"
          icon="sync"
          :label="$t('common.refresh')"
          no-caps
          :loading="loading"
          @click="loadDetails"
          v-if="txDetails?.transaction?.status === 'PendingBankApproval'"
        />
      </div>

      <!-- Detail Card -->
      <q-card class="glass-card q-pa-lg" v-if="txDetails">
        <q-card-section class="q-pa-none">
          
          <div class="text-center q-mb-lg border-bottom q-pb-lg">
            <q-avatar
              :color="txDetails.transaction.type === 'TopUp' ? 'indigo-10' : 'grey-10'"
              :text-color="txDetails.transaction.type === 'TopUp' ? 'indigo-3' : 'grey-3'"
              size="64px"
              class="q-mb-md shadow-3"
            >
              <q-icon :name="txDetails.transaction.type === 'TopUp' ? 'add' : 'call_made'" size="32px" />
            </q-avatar>
            <h5 class="text-h5 text-white text-weight-bold q-my-none">
              {{ txDetails.transaction.type }} Transaction
            </h5>
            <div class="text-caption text-grey-5 font-mono q-mt-xs">{{ txDetails.transaction.referenceNumber }}</div>
            
            <q-badge
              :color="getStatusColor(txDetails.transaction.status)"
              class="text-bold text-subtitle2 q-mt-sm q-py-xs q-px-md text-capitalize"
            >
              {{ getStatusLabel(txDetails.transaction.status) }}
            </q-badge>
          </div>

          <!-- Grid details -->
          <div class="row q-col-gutter-y-md q-mb-lg text-body2">
            <div class="col-6">
              <span class="text-grey-5 block">{{ $t('common.amount') }}</span>
              <span class="text-subtitle1 text-weight-bolder text-white font-sans">
                {{ txDetails.transaction.amount.toFixed(3) }} {{ txDetails.transaction.currency }}
              </span>
            </div>
            
            <div class="col-6">
              <span class="text-grey-5 block">{{ $t('common.date') }}</span>
              <span class="text-weight-bold text-white">{{ formatDate(txDetails.transaction.createdAt) }}</span>
            </div>

            <div class="col-6" v-if="txDetails.transaction.fromWalletNumber">
              <span class="text-grey-5 block">{{ $t('common.from') }} {{ $t('common.wallet') }}</span>
              <span class="text-weight-bold text-white font-mono">{{ txDetails.transaction.fromWalletNumber }}</span>
            </div>

            <div class="col-6" v-if="txDetails.transaction.toWalletNumber">
              <span class="text-grey-5 block">{{ $t('common.to') }} {{ $t('common.wallet') }}</span>
              <span class="text-weight-bold text-white font-mono">{{ txDetails.transaction.toWalletNumber }}</span>
            </div>

            <div class="col-6">
              <span class="text-grey-5 block">{{ $t('adminTx.initiator') }}</span>
              <span class="text-weight-bold text-white">{{ txDetails.transaction.initiatedByFullName }}</span>
            </div>

            <div class="col-6" v-if="txDetails.transaction.bankReference">
              <span class="text-grey-5 block">{{ $t('adminTx.bankRef') }}</span>
              <span class="text-weight-bold text-indigo-3 font-mono">{{ txDetails.transaction.bankReference }}</span>
            </div>

            <div class="col-12" v-if="txDetails.transaction.rejectionReason">
              <span class="text-grey-5 block">{{ $t('adminTx.rejectionReason') }}</span>
              <q-banner dense rounded class="bg-red-10-dim text-red-2 text-bold border-left-red q-mt-xs">
                {{ txDetails.transaction.rejectionReason }}
              </q-banner>
            </div>

            <div class="col-12" v-if="txDetails.transaction.description">
              <span class="text-grey-5 block">{{ $t('send.memo') }}</span>
              <span class="text-grey-3">{{ txDetails.transaction.description }}</span>
            </div>
          </div>

          <!-- Academic Double Ledger Entries -->
          <div class="q-mt-md">
            <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-md">
              Double-Ledger Audit Entries
            </div>

            <q-list class="q-pa-none rounded-borders bg-grey-10-dim" v-if="txDetails.ledgerEntries.length > 0">
              <template v-for="(le, index) in txDetails.ledgerEntries" :key="le.id">
                <q-item class="q-py-md">
                  <q-item-section avatar>
                    <q-avatar
                      :color="le.entryType === 'Credit' ? 'emerald-10' : 'red-10'"
                      :text-color="le.entryType === 'Credit' ? 'emerald-3' : 'red-3'"
                      size="40px"
                    >
                      <q-icon :name="le.entryType === 'Credit' ? 'arrow_downward' : 'arrow_upward'" />
                    </q-avatar>
                  </q-item-section>

                  <q-item-section>
                    <q-item-label class="text-weight-bold text-white row items-center">
                      {{ $t('common.wallet') }}: <span class="font-mono text-indigo-3 q-ml-xs">{{ le.walletNumber }}</span>
                    </q-item-label>
                    <q-item-label caption class="text-grey-5">
                      Before: {{ le.balanceBefore.toFixed(3) }} | After: {{ le.balanceAfter.toFixed(3) }} {{ le.currency }}
                    </q-item-label>
                  </q-item-section>

                  <q-item-section side class="text-right text-subtitle1 text-weight-bold text-white">
                    <span :class="le.entryType === 'Credit' ? 'text-emerald-4' : 'text-red-4'">
                      {{ le.entryType === 'Credit' ? '+' : '-' }}{{ le.amount.toFixed(3) }}
                    </span>
                    <div class="text-caption text-grey-5 font-mono">{{ le.entryType }}</div>
                  </q-item-section>
                </q-item>
                <q-separator class="bg-grey-9" v-if="Number(index) < txDetails.ledgerEntries.length - 1" />
              </template>
            </q-list>

            <div class="text-caption text-grey-5 text-center q-pa-md" v-else>
              Ledger entries are only created upon completed balance-changing transactions.
            </div>
          </div>

        </q-card-section>
      </q-card>

      <!-- Skeleton loading -->
      <q-card class="glass-card q-pa-lg text-center" v-else>
        <q-spinner color="primary" size="48px" class="q-my-xl" />
        <div class="text-grey-5">Loading transaction details...</div>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.max-w-2xl {
  max-width: 600px;
}
.block {
  display: block;
}
.bg-grey-10-dim {
  background: rgba(30, 41, 59, 0.25);
}
.text-emerald-4 {
  color: #34D399;
}
.text-red-4 {
  color: #F87171;
}
.border-left-red {
  border-left: 3px solid #EF4444;
}
.border-bottom {
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}
</style>
