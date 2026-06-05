<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAdminStore } from '../../stores/admin';
import { useQuasar } from 'quasar';

const route = useRoute();
const router = useRouter();
const adminStore = useAdminStore();
const $q = useQuasar();
import { useI18n } from 'vue-i18n';
const { t } = useI18n();

const walletId = route.params.id as string;
const wDetails = ref<any | null>(null);
const loading = ref(false);

const loadDetails = async () => {
  loading.value = true;
  try {
    wDetails.value = await adminStore.fetchWalletDetails(walletId);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load wallet details';
    $q.notify({ type: 'negative', message: errorMsg });
  } finally {
    loading.value = false;
  }
};

const handleFreeze = () => {
  if (!wDetails.value?.wallet) return;
  
  $q.dialog({
    title: 'Freeze Wallet',
    message: 'Enter the reason for freezing this wallet:',
    prompt: {
      model: '',
      type: 'text',
      required: true,
      label: t('adminWallets.statusTransition')
    },
    ok: {
      label: 'Freeze',
      color: 'negative'
    },
    cancel: true,
    dark: true
  }).onOk(async (reason) => {
    try {
      await adminStore.freezeWallet(wDetails.value.wallet.id, reason);
      $q.notify({ type: 'positive', message: 'Wallet frozen successfully.' });
      loadDetails();
    } catch (error: any) {
      const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to freeze wallet';
      $q.notify({ type: 'negative', message: errorMsg });
    }
  });
};

const handleUnfreeze = () => {
  if (!wDetails.value?.wallet) return;
  
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
      await adminStore.unfreezeWallet(wDetails.value.wallet.id, reason);
      $q.notify({ type: 'positive', message: 'Wallet unfrozen successfully.' });
      loadDetails();
    } catch (error: any) {
      const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to unfreeze wallet';
      $q.notify({ type: 'negative', message: errorMsg });
    }
  });
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
        label="Back to Wallets"
        no-caps
        class="q-mb-md"
        @click="router.push('/admin/wallets')"
      />

      <div v-if="wDetails">
        <div class="row q-col-gutter-lg">
          
          <!-- Wallet Info Card -->
          <div class="col-12 col-md-5">
            <q-card class="glass-card fit q-pa-md text-center">
              <q-card-section class="q-pa-none">
                <q-avatar size="80px" color="indigo-10" class="q-mb-md">
                  <q-icon name="account_balance_wallet" color="white" />
                </q-avatar>

                <h5 class="text-h5 text-white text-weight-bold q-my-none font-mono">
                  {{ wDetails.wallet.walletNumber }}
                </h5>
                <div class="text-caption text-grey-5 q-mb-md">Owner: {{ wDetails.owner.fullName }}</div>

                <div class="text-h6 text-white text-weight-bolder q-my-md">
                  {{ wDetails.wallet.balance.toFixed(3) }}
                  <span class="text-caption text-indigo-3 font-sans">LYD</span>
                </div>

                <div class="text-left text-body2 q-gutter-y-sm border-top border-indigo q-pt-md">
                  <div>{{ $t('adminUsers.ownerEmail') }}: <span class="text-white font-mono text-caption">{{ wDetails.owner.email }}</span></div>
                  <div>
                    Status: 
                    <q-badge :color="wDetails.wallet.status === 'Active' ? 'secondary' : 'negative'" class="text-bold">
                      {{ wDetails.wallet.status }}
                    </q-badge>
                  </div>
                  <div v-if="wDetails.wallet.status === 'Frozen'" class="text-red-3 font-italic text-bold q-mt-xs">
                    "{{ wDetails.wallet.frozenReason }}"
                  </div>
                  <div>{{ $t('adminUsers.created') }}: <span class="text-grey-4">{{ formatDate(wDetails.wallet.createdAt) }}</span></div>
                </div>

                <q-separator class="bg-grey-9 q-my-lg" />

                <!-- Action Controls -->
                <div class="q-gutter-y-sm">
                  <q-btn
                    color="negative"
                    class="w-full q-py-sm rounded-borders text-bold"
                    label="Freeze Wallet"
                    icon="lock"
                    no-caps
                    v-if="wDetails.wallet.status !== 'Frozen'"
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

              </q-card-section>
            </q-card>
          </div>

          <!-- Status Transition History Card -->
          <div class="col-12 col-md-7">
            <q-card class="glass-card fit">
              <q-card-section class="q-pa-md">
                <div class="text-subtitle2 text-indigo-3 font-mono text-uppercase text-weight-bold letter-spacing-1">
                  {{ $t('adminWallets.statusTransition') }}
                </div>
              </q-card-section>

              <q-separator class="bg-grey-9" />

              <q-card-section class="q-pa-none">
                <q-list class="q-py-xs" v-if="wDetails.statusHistories.length > 0">
                  <template v-for="(h, idx) in wDetails.statusHistories" :key="h.id">
                    <q-item class="q-py-md">
                      <q-item-section avatar>
                        <q-avatar
                          :color="h.newStatus === 'Active' ? 'emerald-10' : 'red-10'"
                          :text-color="h.newStatus === 'Active' ? 'emerald-3' : 'red-3'"
                          size="36px"
                        >
                          <q-icon :name="h.newStatus === 'Active' ? 'check' : 'lock'" />
                        </q-avatar>
                      </q-item-section>

                      <q-item-section>
                        <q-item-label class="text-weight-bold text-white">
                          {{ h.oldStatus }} → {{ h.newStatus }}
                        </q-item-label>
                        <q-item-label class="text-grey-4 text-caption text-bold">
                          Reason: "{{ h.reason }}"
                        </q-item-label>
                        <q-item-label caption class="text-grey-5">
                          By: {{ h.changedByFullName }} | {{ formatDate(h.changedAt) }}
                        </q-item-label>
                      </q-item-section>
                    </q-item>
                    <q-separator class="bg-grey-9" v-if="Number(idx) < wDetails.statusHistories.length - 1" />
                  </template>
                </q-list>
                <div class="text-center q-pa-xl text-grey-5" v-else>
                  No status histories recorded for this wallet.
                </div>
              </q-card-section>
            </q-card>
          </div>

        </div>
      </div>

      <!-- Loader -->
      <q-card class="glass-card q-pa-lg text-center" v-else>
        <q-spinner color="primary" size="48px" class="q-my-xl" />
        <div class="text-grey-5">Loading wallet logs...</div>
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
</style>
