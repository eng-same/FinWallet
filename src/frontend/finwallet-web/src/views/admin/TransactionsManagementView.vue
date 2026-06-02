<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAdminStore } from '../../stores/admin';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const adminStore = useAdminStore();
const router = useRouter();
const $q = useQuasar();

const typeFilter = ref('');
const statusFilter = ref('');
const searchQuery = ref('');

const columns = [
  { name: 'referenceNumber', label: 'Reference Number', field: 'referenceNumber', align: 'left' as const, sortable: true },
  { name: 'type', label: 'Type', field: 'type', align: 'center' as const },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right' as const, sortable: true },
  { name: 'status', label: 'Status', field: 'status', align: 'center' as const },
  { name: 'createdAt', label: 'Date', field: 'createdAt', align: 'center' as const, sortable: true },
];

const loadTransactions = async () => {
  try {
    await adminStore.fetchTransactions({
      type: typeFilter.value || undefined,
      status: statusFilter.value || undefined,
      search: searchQuery.value || undefined,
    });
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load system transactions';
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
  router.push(`/admin/transactions/${row.id}`);
};

onMounted(() => {
  loadTransactions();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">Global Ledger Activity</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">Inspect and audit all transaction flows in the platform</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadTransactions" />
    </div>

    <!-- Filters -->
    <q-card class="glass-card q-mb-xl q-pa-md">
      <div class="row q-col-gutter-md items-center">
        
        <div class="col-12 col-sm-4">
          <q-input
            v-model="searchQuery"
            label="Search Ref / Wallet"
            label-color="indigo-3"
            dark
            outlined
            dense
            debounce="500"
            @update:model-value="loadTransactions"
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
            label="Type"
            label-color="indigo-3"
            dark
            outlined
            dense
            @update:model-value="loadTransactions"
          />
        </div>

        <div class="col-6 col-sm-3">
          <q-select
            v-model="statusFilter"
            :options="['', 'Created', 'PendingBankApproval', 'Completed', 'Rejected', 'Failed']"
            label="Status"
            label-color="indigo-3"
            dark
            outlined
            dense
            @update:model-value="loadTransactions"
          />
        </div>

        <div class="col-12 col-sm-2 text-right">
          <q-btn
            color="primary"
            label="Reset"
            no-caps
            class="w-full q-py-sm"
            @click="() => { typeFilter = ''; statusFilter = ''; searchQuery = ''; loadTransactions(); }"
          />
        </div>

      </div>
    </q-card>

    <!-- Table -->
    <q-card class="glass-card">
      <q-card-section class="q-pa-none">
        <q-table
          :rows="adminStore.transactions"
          :columns="columns"
          row-key="id"
          dark
          flat
          class="bg-transparent"
          :loading="adminStore.loading"
          @row-click="onRowClick"
          :rows-per-page-options="[10, 20, 50]"
        >
          <!-- Custom Type -->
          <template v-slot:body-cell-type="props">
            <q-td :props="props">
              <q-badge
                :color="props.row.type === 'TopUp' ? 'indigo-10' : 'grey-10'"
                :text-color="props.row.type === 'TopUp' ? 'indigo-3' : 'grey-3'"
                class="text-weight-bold text-subtitle2 text-capitalize"
              >
                {{ props.row.type }}
              </q-badge>
            </q-td>
          </template>

          <!-- Custom Amount -->
          <template v-slot:body-cell-amount="props">
            <q-td :props="props" class="text-weight-bold text-subtitle1 text-white">
              {{ props.row.amount.toFixed(3) }}
              <span class="text-caption text-grey-5 font-sans">LYD</span>
            </q-td>
          </template>

          <!-- Custom Status -->
          <template v-slot:body-cell-status="props">
            <q-td :props="props">
              <q-badge :color="getStatusColor(props.row.status)" class="text-bold">
                {{ getStatusLabel(props.row.status) }}
              </q-badge>
            </q-td>
          </template>

          <!-- Custom Date -->
          <template v-slot:body-cell-createdAt="props">
            <q-td :props="props" class="text-grey-4">
              {{ formatDate(props.row.createdAt) }}
            </q-td>
          </template>

          <!-- No records state -->
          <template v-slot:no-data>
            <div class="w-full text-center q-pa-xl">
              <q-icon name="list" size="64px" color="grey-7" class="q-mb-md" />
              <div class="text-subtitle1 text-grey-5">No platform transaction logs found.</div>
            </div>
          </template>
        </q-table>
      </q-card-section>
    </q-card>

  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
</style>
