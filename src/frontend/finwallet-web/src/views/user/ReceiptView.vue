<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useTransactionStore } from '../../stores/transaction';
import { useQuasar } from 'quasar';

const route = useRoute();
const router = useRouter();
const transactionStore = useTransactionStore();
const $q = useQuasar();

const txId = route.params.id as string;
const receipt = ref<any | null>(null);
const loading = ref(false);

const loadReceipt = async () => {
  loading.value = true;
  try {
    receipt.value = await transactionStore.fetchReceiptByTransaction(txId);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load receipt details';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const handlePrint = () => {
  window.print();
};

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString();
};

onMounted(() => {
  loadReceipt();
});
</script>

<template>
  <q-page class="q-pa-lg flex flex-center print-pagebg">
    <div class="w-full max-w-md print-no-margin">
      
      <!-- Back Btn (hidden on print) -->
      <div class="row items-center justify-between q-mb-md no-print">
        <q-btn
          flat
          dense
          color="grey-5"
          icon="arrow_back"
          label="Back to Transaction"
          no-caps
          @click="router.push(`/transactions/${txId}`)"
        />
        <q-btn
          flat
          dense
          color="secondary"
          icon="print"
          label="Print"
          no-caps
          @click="handlePrint"
        />
      </div>

      <!-- Receipt Layout -->
      <q-card class="glass-card q-pa-xl receipt-card relative text-white" v-if="receipt">
        
        <!-- Watermark / Stamp -->
        <div class="absolute-center stamp-watermark">
          PAID
        </div>

        <q-card-section class="q-pa-none text-center">
          <!-- Company Name -->
          <div class="text-weight-bolder text-subtitle1 font-mono text-indigo-4 letter-spacing-1 q-mb-xs">
            FINWALLET PLATFORM
          </div>
          <div class="text-caption text-grey-5 font-mono">OFFICIAL TRANSACTION RECEIPT</div>
          
          <q-separator class="bg-grey-9 q-my-lg" />

          <!-- Receipt Details -->
          <div class="q-mb-md">
            <div class="text-caption text-grey-5">Receipt Number</div>
            <div class="text-weight-bold text-subtitle2 font-mono text-white">
              {{ receipt.receiptNumber }}
            </div>
          </div>

          <div class="q-mb-lg row justify-between text-left text-body2">
            <div class="col-12 q-mb-sm">
              <span class="text-grey-5 block">Transaction Date</span>
              <span class="text-weight-bold text-white">{{ formatDate(receipt.issuedAt) }}</span>
            </div>

            <div class="col-12 q-mb-sm" v-if="receipt.fromWalletNumber">
              <span class="text-grey-5 block">Sender Wallet</span>
              <span class="text-weight-bold text-white font-mono">{{ receipt.fromWalletNumber }}</span>
            </div>

            <div class="col-12 q-mb-sm" v-if="receipt.toWalletNumber">
              <span class="text-grey-5 block">Recipient Wallet</span>
              <span class="text-weight-bold text-white font-mono">{{ receipt.toWalletNumber }}</span>
            </div>

            <div class="col-12 q-mb-sm">
              <span class="text-grey-5 block">Initiated By</span>
              <span class="text-weight-bold text-white">{{ receipt.initiatedByFullName }}</span>
            </div>

            <div class="col-12 q-mb-sm" v-if="receipt.description">
              <span class="text-grey-5 block">Description / Memo</span>
              <span class="text-grey-4 text-italic">"{{ receipt.description }}"</span>
            </div>
          </div>

          <q-separator class="bg-grey-9 q-my-lg" />

          <!-- Total Amount -->
          <div class="q-py-md bg-grey-10-dim rounded-borders">
            <div class="text-caption text-grey-5 text-uppercase letter-spacing-1">Total Amount</div>
            <div class="text-h3 text-weight-bolder text-emerald-4">
              {{ receipt.amount.toFixed(3) }}
              <span class="text-subtitle1 text-grey-4 text-weight-medium">LYD</span>
            </div>
          </div>

          <div class="q-mt-xl text-caption text-grey-6 font-mono border-top q-pt-md">
            FinWallet Digital receipt. Secure and Verified.
          </div>

        </q-card-section>
      </q-card>

      <!-- Loading skeleton -->
      <q-card class="glass-card q-pa-lg text-center" v-else>
        <q-spinner color="primary" size="48px" class="q-my-xl" />
        <div class="text-grey-5">Generating Receipt...</div>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.max-w-md {
  max-width: 440px;
}
.receipt-card {
  border: 1px dashed rgba(255, 255, 255, 0.15) !important;
  border-radius: 12px !important;
  background: rgba(15, 23, 42, 0.7) !important;
}
.block {
  display: block;
}
.bg-grey-10-dim {
  background: rgba(30, 41, 59, 0.4);
}
.text-emerald-4 {
  color: #10B981;
}

.stamp-watermark {
  position: absolute;
  font-size: 80px;
  font-weight: 900;
  color: rgba(16, 185, 129, 0.08);
  border: 8px solid rgba(16, 185, 129, 0.08);
  padding: 10px 30px;
  transform: rotate(-15deg);
  pointer-events: none;
  border-radius: 20px;
  letter-spacing: 4px;
}

/* Print CSS overrides */
@media print {
  .no-print {
    display: none !important;
  }
  
  body, .print-pagebg {
    background: white !important;
    color: black !important;
  }
  
  .receipt-card {
    background: white !important;
    color: black !important;
    border: 1px solid #ddd !important;
    box-shadow: none !important;
    backdrop-filter: none !important;
    margin: 0 auto;
  }

  .text-white {
    color: black !important;
  }
  .text-grey-5 {
    color: #666 !important;
  }
  .text-grey-4 {
    color: #333 !important;
  }
  .text-grey-6 {
    color: #999 !important;
  }
  .text-indigo-4 {
    color: #4F46E5 !important;
  }
  .text-emerald-4 {
    color: #10B981 !important;
  }
  .stamp-watermark {
    color: rgba(16, 185, 129, 0.04) !important;
    border-color: rgba(16, 185, 129, 0.04) !important;
  }
  .bg-grey-10-dim {
    background: #f8f9fa !important;
    border: 1px solid #eee;
  }
  .print-no-margin {
    max-width: 100% !important;
    width: 100% !important;
    margin: 0 !important;
    padding: 0 !important;
  }
}
</style>
