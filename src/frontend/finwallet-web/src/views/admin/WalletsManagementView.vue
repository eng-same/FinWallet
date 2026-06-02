<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAdminStore } from '../../stores/admin';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const adminStore = useAdminStore();
const router = useRouter();
const $q = useQuasar();

const searchVal = ref('');

const loadWallets = async () => {
  try {
    await adminStore.fetchWallets(searchVal.value || undefined);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load wallets';
    $q.notify({ type: 'negative', message: errorMsg });
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString();
};

onMounted(() => {
  loadWallets();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">Wallet Registry</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">Monitor wallet balances, owners, and freeze logs</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadWallets" />
    </div>

    <!-- Search Input -->
    <q-card class="glass-card q-mb-xl q-pa-md">
      <div class="row items-center">
        <div class="col-12 col-sm-6">
          <q-input
            v-model="searchVal"
            label="Search by Wallet Number / Owner Name"
            label-color="indigo-3"
            dark
            outlined
            dense
            debounce="500"
            @update:model-value="loadWallets"
          >
            <template v-slot:prepend>
              <q-icon name="search" color="indigo-4" />
            </template>
          </q-input>
        </div>
      </div>
    </q-card>

    <!-- Wallets Grid -->
    <div class="row q-col-gutter-lg" v-if="!adminStore.loading">
      <div class="col-12 col-sm-6 col-md-4" v-for="w in adminStore.wallets" :key="w.id">
        <q-card class="glass-card hover-scale cursor-pointer" @click="router.push(`/admin/wallets/${w.id}`)">
          <q-card-section class="q-pa-md">
            
            <div class="row items-center justify-between q-mb-md">
              <span class="text-subtitle2 font-mono text-indigo-3 text-weight-bold">
                {{ w.walletNumber }}
              </span>
              <q-badge :color="w.status === 'Active' ? 'secondary' : 'negative'" class="text-bold">
                {{ w.status }}
              </q-badge>
            </div>

            <!-- Balance -->
            <div class="text-h5 text-weight-bolder text-white q-mb-md text-center">
              {{ w.balance.toFixed(3) }}
              <span class="text-caption text-grey-4 font-sans">LYD</span>
            </div>

            <q-separator class="bg-grey-9 q-my-md" />

            <div class="text-caption text-grey-5 text-left">
              <div>Owner ID: <span class="text-grey-3 font-mono">{{ w.userId.substring(0,8) }}...</span></div>
              <div>Created: <span class="text-grey-3">{{ formatDate(w.createdAt) }}</span></div>
              <div v-if="w.status === 'Frozen'" class="text-red-3 font-italic q-mt-xs text-bold">
                "{{ w.frozenReason }}"
              </div>
            </div>

          </q-card-section>
        </q-card>
      </div>

      <!-- No wallets state -->
      <div class="col-12 text-center q-pa-xl text-grey-5" v-if="adminStore.wallets.length === 0">
        <q-icon name="wallet" size="64px" class="q-mb-md" />
        <div>No matching wallets found in the system.</div>
      </div>
    </div>

    <!-- Loader -->
    <div class="text-center q-pa-xl" v-else>
      <q-spinner color="primary" size="48px" />
      <div class="text-grey-5 q-mt-md">Querying wallets...</div>
    </div>

  </q-page>
</template>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}
</style>
