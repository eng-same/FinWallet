<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAdminStore } from '../../stores/admin';
import { useQuasar } from 'quasar';

const adminStore = useAdminStore();
const $q = useQuasar();

const searchVal = ref('');

const columns = [
  { name: 'createdAt', label: 'Timestamp', field: 'createdAt', align: 'left' as const, sortable: true },
  { name: 'userEmail', label: 'Actor', field: 'userEmail', align: 'left' as const, sortable: true },
  { name: 'action', label: 'Action', field: 'action', align: 'center' as const, sortable: true },
  { name: 'description', label: 'Description', field: 'description', align: 'left' as const },
  { name: 'ipAddress', label: 'IP Address', field: 'ipAddress', align: 'center' as const },
  { name: 'actions', label: 'Details', field: 'actions', align: 'center' as const }
];

const loadAuditLogs = async () => {
  try {
    await adminStore.fetchAuditLogs(searchVal.value || undefined);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load audit logs';
    $q.notify({ type: 'negative', message: errorMsg });
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString();
};

const getActionColor = (action: string) => {
  const act = action.toUpperCase();
  if (act.includes('AUTH') || act.includes('LOGIN')) return 'indigo-8';
  if (act.includes('FREEZE')) return 'red-8';
  if (act.includes('UNFREEZE')) return 'green-8';
  if (act.includes('SEND') || act.includes('TRANSFER')) return 'amber-9';
  if (act.includes('TOPUP') || act.includes('TOP_UP')) return 'teal-8';
  if (act.includes('CREATE')) return 'cyan-8';
  return 'grey-8';
};

onMounted(() => {
  loadAuditLogs();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">Security & Audit Logs</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">Trace and review administrative actions and platform event records</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadAuditLogs" />
    </div>

    <!-- Search / Filter -->
    <q-card class="glass-card q-mb-xl q-pa-md">
      <div class="row items-center justify-between q-col-gutter-md">
        <div class="col-12 col-sm-6">
          <q-input
            v-model="searchVal"
            label="Search by Action, Description or Actor Email"
            label-color="indigo-3"
            dark
            outlined
            dense
            debounce="500"
            @update:model-value="loadAuditLogs"
          >
            <template v-slot:prepend>
              <q-icon name="search" color="indigo-4" />
            </template>
          </q-input>
        </div>
        <div class="col-12 col-sm-2 text-right">
          <q-btn
            color="primary"
            label="Clear"
            no-caps
            class="w-full q-py-sm"
            @click="() => { searchVal = ''; loadAuditLogs(); }"
          />
        </div>
      </div>
    </q-card>

    <!-- Audit Logs Table -->
    <q-card class="glass-card">
      <q-card-section class="q-pa-none">
        <q-table
          :rows="adminStore.auditLogs"
          :columns="columns"
          row-key="id"
          dark
          flat
          class="bg-transparent"
          :loading="adminStore.loading"
          :rows-per-page-options="[10, 20, 50]"
        >
          <!-- Custom Timestamp -->
          <template v-slot:body-cell-createdAt="props">
            <q-td :props="props" class="text-grey-4">
              {{ formatDate(props.row.createdAt) }}
            </q-td>
          </template>

          <!-- Custom Actor -->
          <template v-slot:body-cell-userEmail="props">
            <q-td :props="props">
              <span v-if="props.row.userEmail === 'System'" class="text-grey-5 text-italic">System</span>
              <span v-else class="text-indigo-2 text-weight-bold font-mono text-caption">{{ props.row.userEmail }}</span>
            </q-td>
          </template>

          <!-- Custom Action -->
          <template v-slot:body-cell-action="props">
            <q-td :props="props">
              <q-badge :color="getActionColor(props.row.action)" class="text-bold text-uppercase q-py-xs q-px-sm">
                {{ props.row.action }}
              </q-badge>
            </q-td>
          </template>

          <!-- Custom Description -->
          <template v-slot:body-cell-description="props">
            <q-td :props="props" class="text-grey-3 text-weight-medium">
              {{ props.row.description }}
            </q-td>
          </template>

          <!-- Custom IP Address -->
          <template v-slot:body-cell-ipAddress="props">
            <q-td :props="props" class="font-mono text-caption text-grey-4">
              {{ props.row.ipAddress || 'N/A' }}
            </q-td>
          </template>

          <!-- Row Details Expand Action -->
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <q-btn
                flat
                round
                dense
                :icon="props.expand ? 'keyboard_arrow_up' : 'keyboard_arrow_down'"
                color="indigo-3"
                @click="props.expand = !props.expand"
              >
                <q-tooltip>Toggle Technical Details</q-tooltip>
              </q-btn>
            </q-td>
          </template>

          <!-- Expanded Row Template -->
          <template v-slot:body="props">
            <q-tr :props="props">
              <q-td v-for="col in props.cols" :key="col.name" :props="props">
                <template v-if="col.name === 'createdAt'">
                  <span class="text-grey-4">{{ formatDate(props.row.createdAt) }}</span>
                </template>
                <template v-else-if="col.name === 'userEmail'">
                  <span v-if="props.row.userEmail === 'System'" class="text-grey-5 text-italic font-mono text-caption">System</span>
                  <span v-else class="text-indigo-2 text-weight-bold font-mono text-caption">{{ props.row.userEmail }}</span>
                </template>
                <template v-else-if="col.name === 'action'">
                  <q-badge :color="getActionColor(props.row.action)" class="text-bold text-uppercase q-py-xs q-px-sm">
                    {{ props.row.action }}
                  </q-badge>
                </template>
                <template v-else-if="col.name === 'description'">
                  <span class="text-grey-3 text-weight-medium">{{ props.row.description }}</span>
                </template>
                <template v-else-if="col.name === 'ipAddress'">
                  <span class="font-mono text-caption text-grey-4">{{ props.row.ipAddress || 'N/A' }}</span>
                </template>
                <template v-else-if="col.name === 'actions'">
                  <q-btn
                    flat
                    round
                    dense
                    :icon="props.expand ? 'keyboard_arrow_up' : 'keyboard_arrow_down'"
                    color="indigo-3"
                    @click="props.expand = !props.expand"
                  />
                </template>
              </q-td>
            </q-tr>
            <q-tr v-show="props.expand" :props="props" class="bg-grey-10">
              <q-td colspan="100%">
                <div class="q-pa-md text-left text-caption text-grey-4">
                  <div class="row q-col-gutter-md">
                    <div class="col-12 col-md-4">
                      <div class="text-weight-bold text-indigo-3 q-mb-xs">Log Reference ID:</div>
                      <div class="font-mono text-white">{{ props.row.id }}</div>
                    </div>
                    <div class="col-12 col-md-4">
                      <div class="text-weight-bold text-indigo-3 q-mb-xs">Affected Entity Name:</div>
                      <div class="font-mono text-white">{{ props.row.entityName || 'None' }}</div>
                    </div>
                    <div class="col-12 col-md-4">
                      <div class="text-weight-bold text-indigo-3 q-mb-xs">Affected Entity ID:</div>
                      <div class="font-mono text-white">{{ props.row.entityId || 'None' }}</div>
                    </div>
                    <div class="col-12">
                      <div class="text-weight-bold text-indigo-3 q-mb-xs">Client User Agent:</div>
                      <div class="text-white">{{ props.row.userAgent || 'Unknown / System initiated' }}</div>
                    </div>
                  </div>
                </div>
              </q-td>
            </q-tr>
          </template>

          <!-- No records state -->
          <template v-slot:no-data>
            <div class="w-full text-center q-pa-xl">
              <q-icon name="security" size="64px" color="grey-7" class="q-mb-md" />
              <div class="text-subtitle1 text-grey-5">No security or audit logs found matching criteria.</div>
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
.font-mono {
  font-family: monospace;
}
</style>
