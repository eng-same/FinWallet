<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue';
import { useTransactionStore } from '../../stores/transaction';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const transactionStore = useTransactionStore();
const router = useRouter();
const $q = useQuasar();
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

const typeFilter = ref('');
const statusFilter = ref('');
const searchQuery = ref('');

const columns = computed(() => [
  { name: 'referenceNumber', label: t('adminTx.referenceNumber'), field: 'referenceNumber', align: 'left' as const, sortable: true },
  { name: 'type', label: t('common.type'), field: 'type', align: 'center' as const },
  { name: 'amount', label: t('common.amount'), field: 'amount', align: 'right' as const, sortable: true },
  { name: 'status', label: t('common.status'), field: 'status', align: 'center' as const },
  { name: 'createdAt', label: t('common.date'), field: 'createdAt', align: 'center' as const, sortable: true }
]);

const loadHistory = async () => {
  try {
    await transactionStore.fetchMyTransactions({
      type: typeFilter.value || undefined,
      status: statusFilter.value || undefined,
      search: searchQuery.value || undefined,
    });
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load transaction history';
    $q.notify({ type: 'negative', message: errorMsg });
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

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString();
};

const onRowClick = (_evt: any, row: any) => {
  router.push(`/transactions/${row.id}`);
};

// Reload when page changes in the store
watch(() => transactionStore.page, () => {
  loadHistory();
});

onMounted(() => {
  transactionStore.page = 1;
  loadHistory();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">{{ $t('history.title') }}</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('history.subtitle') }}</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadHistory" />
    </div>

    <!-- Filters Card -->
    <q-card class="glass-card q-mb-xl q-pa-md">
      <div class="row q-col-gutter-md items-center">
        
        <div class="col-12 col-sm-4">
          <q-input
            v-model="searchQuery"
            label="Search Reference / Memo"
            label-color="indigo-3"
            dark
            outlined
            dense
            debounce="500"
            @update:model-value="loadHistory"
          >
            <template v-slot:prepend>
              <q-icon name="search" color="indigo-4" />
            </template>
          </q-input>
        </div>

        <div class="col-6 col-sm-3">
          <q-select
            v-model="typeFilter"
            :options="['', 'TopUp', 'Transfer']"
            :label="$t('common.type')"
            label-color="indigo-3"
            dark
            outlined
            dense
            emit-value
            map-options
            @update:model-value="loadHistory"
          />
        </div>

        <div class="col-6 col-sm-3">
          <q-select
            v-model="statusFilter"
            :options="['', 'Created', 'PendingBankApproval', 'Completed', 'Rejected', 'Failed']"
            :label="$t('common.status')"
            label-color="indigo-3"
            dark
            outlined
            dense
            emit-value
            map-options
            @update:model-value="loadHistory"
          />
        </div>

        <div class="col-12 col-sm-2 text-right">
          <q-btn
            color="primary"
            label="Reset Filters"
            no-caps
            class="w-full q-py-sm"
            @click="() => { typeFilter = ''; statusFilter = ''; searchQuery = ''; loadHistory(); }"
          />
        </div>

      </div>
    </q-card>

    <!-- Table Card -->
    <q-card class="glass-card">
      <q-card-section class="q-pa-none">
        <q-table
          :rows="transactionStore.transactions"
          :columns="columns"
          row-key="id"
          dark
          flat
          class="bg-transparent"
          binary-state-sort
          :loading="transactionStore.loading"
          @row-click="onRowClick"
          hide-pagination
        >
          <!-- Custom Type mapping -->
          <template v-slot:body-cell-type="props">
            <q-td :props="props">
              <q-badge
                :color="props.row.type === 'TopUp' ? 'indigo-10' : 'grey-10'"
                :text-color="props.row.type === 'TopUp' ? 'indigo-3' : 'grey-3'"
                class="text-weight-bold text-subtitle2"
              >
                {{ props.row.type }}
              </q-badge>
            </q-td>
          </template>

          <!-- Custom Amount mapping -->
          <template v-slot:body-cell-amount="props">
            <q-td :props="props" class="text-weight-bold text-subtitle1 text-white">
              {{ props.row.type === 'TopUp' ? '+' : '-' }}{{ props.row.amount.toFixed(3) }}
              <span class="text-caption text-grey-5 font-sans">LYD</span>
            </q-td>
          </template>

          <!-- Custom Status mapping -->
          <template v-slot:body-cell-status="props">
            <q-td :props="props">
              <q-badge :color="getStatusColor(props.row.status)" class="text-bold">
                {{ getStatusLabel(props.row.status) }}
              </q-badge>
            </q-td>
          </template>

          <!-- Custom Date mapping -->
          <template v-slot:body-cell-createdAt="props">
            <q-td :props="props" class="text-grey-4">
              {{ formatDate(props.row.createdAt) }}
            </q-td>
          </template>

          <!-- No records state -->
          <template v-slot:no-data>
            <div class="w-full text-center q-pa-xl">
              <q-icon name="list" size="64px" color="grey-7" class="q-mb-md" />
              <div class="text-subtitle1 text-grey-5">No transactions matched your search criteria.</div>
            </div>
          </template>
        </q-table>
      </q-card-section>

      <q-separator class="bg-grey-9" />

      <!-- Custom Pagination -->
      <q-card-section class="row items-center justify-between q-pa-md">
        <div class="text-caption text-grey-5">
          Total: {{ transactionStore.totalCount }} items
        </div>
        <q-pagination
          v-model="transactionStore.page"
          :max="transactionStore.totalPages"
          :max-pages="6"
          direction-links
          boundary-links
          color="primary"
          dark
        />
      </q-card-section>
    </q-card>

  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.cursor-pointer {
  cursor: pointer;
}
</style>
