<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAdminStore } from '../../stores/admin';
import { useQuasar } from 'quasar';

const route = useRoute();
const router = useRouter();
const adminStore = useAdminStore();
const $q = useQuasar();

const userId = route.params.id as string;
const userDetails = ref<any | null>(null);
const loading = ref(false);

const loadDetails = async () => {
  loading.value = true;
  try {
    userDetails.value = await adminStore.fetchUserDetails(userId);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load user details';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const handleFreeze = () => {
  if (!userDetails.value?.wallet) return;
  
  $q.dialog({
    title: 'Freeze Wallet',
    message: 'Enter the reason for freezing this wallet:',
    prompt: {
      model: '',
      type: 'text',
      required: true,
      label: 'Freeze Reason'
    },
    ok: {
      label: 'Freeze',
      color: 'negative'
    },
    cancel: true,
    dark: true
  }).onOk(async (reason) => {
    try {
      await adminStore.freezeWallet(userDetails.value.wallet.id, reason);
      $q.notify({
        type: 'positive',
        message: 'Wallet frozen successfully.'
      });
      loadDetails();
    } catch (error: any) {
      const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to freeze wallet';
      $q.notify({ type: 'negative', message: errorMsg });
    }
  });
};

const handleUnfreeze = () => {
  if (!userDetails.value?.wallet) return;
  
  $q.dialog({
    title: 'Unfreeze Wallet',
    message: 'Enter the reason for unfreezing this wallet:',
    prompt: {
      model: '',
      type: 'text',
      required: true,
      label: 'Unfreeze Reason'
    },
    ok: {
      label: 'Unfreeze',
      color: 'secondary'
    },
    cancel: true,
    dark: true
  }).onOk(async (reason) => {
    try {
      await adminStore.unfreezeWallet(userDetails.value.wallet.id, reason);
      $q.notify({
        type: 'positive',
        message: 'Wallet unfrozen successfully.'
      });
      loadDetails();
    } catch (error: any) {
      const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to unfreeze wallet';
      $q.notify({ type: 'negative', message: errorMsg });
    }
  });
};

const getStatusColor = (status: string) => {
  switch (status.toLowerCase()) {
    case 'completed': return 'positive';
    case 'pendingbankapproval': return 'accent';
    case 'rejected': return 'negative';
    default: return 'grey';
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString();
};

onMounted(() => {
  loadDetails();
});
</script>

<template>
  <q-page class="q-pa-lg flex flex-center">
    <div class="w-full max-w-3xl">
      
      <!-- Back Btn -->
      <q-btn
        flat
        dense
        color="grey-5"
        icon="arrow_back"
        label="Back to Users"
        no-caps
        class="q-mb-md"
        @click="router.push('/admin/users')"
      />

      <!-- Content -->
      <div v-if="userDetails">
        
        <div class="row q-col-gutter-lg">
          
          <!-- User Details and Actions Card -->
          <div class="col-12 col-md-5">
            <q-card class="glass-card fit q-pa-md text-center">
              <q-card-section class="q-pa-none">
                <q-avatar size="80px" color="indigo-10" class="q-mb-md">
                  <q-icon name="person" color="white" />
                </q-avatar>

                <h5 class="text-h5 text-white text-weight-bold q-my-none">{{ userDetails.user.fullName }}</h5>
                <div class="text-caption text-grey-5 font-mono q-mb-md">{{ userDetails.user.email }}</div>

                <div class="text-left text-body2 q-gutter-y-sm border-top border-indigo q-pt-md">
                  <div>Phone: <span class="text-white text-weight-bold">{{ userDetails.user.phoneNumber }}</span></div>
                  <div>Account: <span class="text-white">{{ userDetails.user.isActive ? 'Active' : 'Deactivated' }}</span></div>
                  <div>Registered: <span class="text-grey-4">{{ formatDate(userDetails.user.createdAt) }}</span></div>
                  <div v-if="userDetails.user.lastLoginAt">Last login: <span class="text-grey-4">{{ formatDate(userDetails.user.lastLoginAt) }}</span></div>
                </div>

                <q-separator class="bg-grey-9 q-my-lg" />

                <!-- Action Controls for wallet freeze -->
                <div class="q-gutter-y-sm" v-if="userDetails.wallet">
                  <q-btn
                    color="negative"
                    class="w-full q-py-sm rounded-borders text-bold"
                    label="Freeze Wallet"
                    icon="lock"
                    no-caps
                    v-if="userDetails.wallet.status !== 'Frozen'"
                    @click="handleFreeze"
                  />
                  <q-btn
                    color="secondary"
                    class="w-full q-py-sm rounded-borders text-bold"
                    label="Unfreeze Wallet"
                    icon="lock_open"
                    no-caps
                    v-else
                    @click="handleUnfreeze"
                  />
                </div>
                
                <div class="text-caption text-grey-5" v-else>
                  This user has no associated wallet.
                </div>

              </q-card-section>
            </q-card>
          </div>

          <!-- Wallet Status and Recent Activities Card -->
          <div class="col-12 col-md-7">
            <div class="q-gutter-y-lg">
              
              <!-- Wallet Details Banner -->
              <q-card class="glass-card" v-if="userDetails.wallet">
                <q-card-section class="q-pa-lg">
                  <div class="text-caption text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1 q-mb-xs">
                    Wallet Telemetry
                  </div>

                  <div class="row items-center justify-between">
                    <div>
                      <div class="text-caption text-grey-5">Wallet Number</div>
                      <div class="text-subtitle1 font-mono text-white text-weight-bold">{{ userDetails.wallet.walletNumber }}</div>
                    </div>
                    <div class="text-right">
                      <div class="text-caption text-grey-5">Balance</div>
                      <div class="text-h6 text-white text-weight-bolder">
                        {{ userDetails.wallet.balance.toFixed(3) }}
                        <span class="text-caption text-indigo-3">LYD</span>
                      </div>
                    </div>
                  </div>

                  <div class="q-mt-md border-top border-indigo q-pt-md row justify-between items-center">
                    <div>
                      <span class="text-caption text-grey-5 mr-xs">Status: </span>
                      <q-badge :color="userDetails.wallet.status === 'Active' ? 'secondary' : 'negative'" class="text-bold">
                        {{ userDetails.wallet.status }}
                      </q-badge>
                    </div>
                    <div v-if="userDetails.wallet.status === 'Frozen'" class="text-caption text-red-2 text-right font-italic">
                      "{{ userDetails.wallet.frozenReason }}"
                    </div>
                  </div>
                </q-card-section>
              </q-card>

              <!-- Recent transactions card -->
              <q-card class="glass-card">
                <q-card-section class="q-pa-md">
                  <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1">
                    Recent Wallet Activities
                  </div>
                </q-card-section>
                
                <q-separator class="bg-grey-9" />
                
                <q-card-section class="q-pa-none">
                  <q-list class="q-py-xs" v-if="userDetails.recentTransactions.length > 0">
                    <template v-for="(tx, index) in userDetails.recentTransactions" :key="tx.id">
                      <q-item clickable @click="router.push(`/admin/transactions/${tx.id}`)" class="q-py-md hover-scale">
                        <q-item-section avatar>
                          <q-icon :name="tx.type === 'TopUp' ? 'add' : 'call_made'" color="grey-4" />
                        </q-item-section>

                        <q-item-section>
                          <q-item-label class="text-weight-bold text-white">{{ tx.type }}</q-item-label>
                          <q-item-label caption class="text-grey-5 font-mono">{{ tx.referenceNumber }}</q-item-label>
                        </q-item-section>

                        <q-item-section side class="text-right">
                          <div class="text-subtitle2 text-weight-bolder text-white">
                            {{ tx.amount.toFixed(3) }} LYD
                          </div>
                          <q-badge :color="getStatusColor(tx.status)" class="text-bold q-mt-xs text-caption">
                            {{ tx.status }}
                          </q-badge>
                        </q-item-section>
                      </q-item>
                      <q-separator class="bg-grey-9" v-if="Number(index) < userDetails.recentTransactions.length - 1" />
                    </template>
                  </q-list>
                  <div class="text-center q-pa-xl text-grey-5" v-else>
                    No transaction logs for this wallet.
                  </div>
                </q-card-section>
              </q-card>

            </div>
          </div>

        </div>

      </div>

      <!-- Skeleton loading -->
      <q-card class="glass-card q-pa-lg text-center" v-else>
        <q-spinner color="primary" size="48px" class="q-my-xl" />
        <div class="text-grey-5">Loading user details...</div>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.max-w-3xl {
  max-width: 800px;
}
.border-top {
  border-top: 1px solid rgba(255, 255, 255, 0.08);
}
.bg-red-10-dim {
  background: rgba(239, 68, 68, 0.1) !important;
}
.mr-xs {
  margin-right: 4px;
}
</style>
