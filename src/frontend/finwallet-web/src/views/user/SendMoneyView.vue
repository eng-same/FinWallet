<script setup lang="ts">
import { ref, watch } from 'vue';
import { useWalletStore } from '../../stores/wallet';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';
import { useI18n } from 'vue-i18n';

const walletStore = useWalletStore();
const router = useRouter();
const $q = useQuasar();
const { locale } = useI18n();

const recipientWallet = ref('');
const recipientName = ref<string | null>(null);
const lookupLoading = ref(false);

const amount = ref<number | null>(null);
const description = ref('');
const loading = ref(false);

const performLookup = async () => {
  if (!recipientWallet.value || recipientWallet.value.trim().length < 8) {
    recipientName.value = null;
    return;
  }

  lookupLoading.value = true;
  try {
    const result = await walletStore.lookupWallet(recipientWallet.value.trim());
    recipientName.value = result.fullName;
  } catch (error: any) {
    recipientName.value = null;
    const errorMsg = error.errors?.[0]?.message || error.message || (locale.value === 'en' ? 'Recipient not found' : 'لم يتم العثور على المستلم');
    $q.notify({
      type: 'negative',
      message: errorMsg,
      position: 'top-right'
    });
  } finally {
    lookupLoading.value = false;
  }
};

// Auto lookup when user finishes typing the wallet format
watch(recipientWallet, (newVal) => {
  if (newVal.length >= 14) {
    performLookup();
  } else {
    recipientName.value = null;
  }
});

const handleSendMoneySubmit = async () => {
  if (!recipientWallet.value) {
    $q.notify({ 
      type: 'warning', 
      message: locale.value === 'en' ? 'Recipient wallet is required' : 'رقم محفظة المستلم مطلوب' 
    });
    return;
  }
  if (!recipientName.value) {
    $q.notify({ 
      type: 'warning', 
      message: locale.value === 'en' ? 'Please verify the recipient before sending' : 'يرجى التحقق من المستلم قبل الإرسال' 
    });
    return;
  }
  if (!amount.value || amount.value <= 0) {
    $q.notify({ 
      type: 'warning', 
      message: locale.value === 'en' ? 'Amount must be greater than 0' : 'يجب أن يكون المبلغ أكبر من 0' 
    });
    return;
  }

  loading.value = true;
  try {
    const tx = await walletStore.sendMoney(recipientWallet.value.trim(), amount.value, description.value);
    
    $q.notify({
      type: 'positive',
      message: locale.value === 'en' 
        ? `Sent ${amount.value.toFixed(3)} LYD to ${recipientName.value}!` 
        : `تم إرسال ${amount.value.toFixed(3)} د.ل إلى ${recipientName.value}!`
    });

    // Route to receipt
    router.push(`/transactions/${tx.id}/receipt`);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || (locale.value === 'en' ? 'Transfer failed' : 'فشلت عملية التحويل');
    $q.notify({
      type: 'negative',
      message: errorMsg
    });
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <q-page class="q-pa-lg flex flex-center">
    <div class="w-full max-w-lg">
      
      <!-- Back Btn -->
      <q-btn
        flat
        dense
        color="grey-5"
        icon="arrow_back"
        :label="locale === 'en' ? 'Back to Dashboard' : 'الرجوع للوحة القيادة'"
        no-caps
        class="q-mb-md"
        @click="router.push('/dashboard')"
      />

      <!-- Transfer Card -->
      <q-card class="glass-card q-pa-lg">
        <q-card-section class="q-pa-none">
          <div class="row items-center q-gutter-md q-mb-xl">
            <q-avatar color="indigo-10" text-color="indigo-3" size="48px">
              <q-icon name="send" size="24px" />
            </q-avatar>
            <div>
              <h5 class="text-h5 text-white text-weight-bold q-my-none">{{ $t('send.title') }}</h5>
              <p class="text-caption text-grey-5 q-my-none">{{ $t('send.subtitle') }}</p>
            </div>
          </div>

          <q-form @submit.prevent="handleSendMoneySubmit" class="q-gutter-md">
            
            <!-- Recipient Wallet -->
            <q-input
              v-model="recipientWallet"
              type="text"
              :label="$t('send.recipientNum')"
              placeholder="e.g. FW-2026-000002"
              label-color="indigo-3"
              dark
              outlined
              required
              @blur="performLookup"
            >
              <template v-slot:prepend>
                <q-icon name="account_box" color="indigo-4" />
              </template>
              <template v-slot:append>
                <q-spinner-oval color="primary" size="20px" v-if="lookupLoading" />
                <q-btn flat round dense icon="search" color="indigo-4" @click="performLookup" v-else />
              </template>
            </q-input>

            <!-- Recipient Fullname display card -->
            <q-slide-transition>
              <div v-if="recipientName" class="q-pa-md bg-emerald-10-dim rounded-borders border-left-emerald text-subtitle2 text-emerald-2 row items-center justify-between">
                <div class="row items-center q-gutter-sm">
                  <q-icon name="check_circle" color="emerald-4" size="20px" />
                  <div>
                    <span class="text-grey-4 text-caption block">
                      {{ locale === 'en' ? 'RECIPIENT CONFIRMED' : 'تم تأكيد الحساب المستلم' }}
                    </span>
                    {{ recipientName }}
                  </div>
                </div>
              </div>
            </q-slide-transition>

            <!-- Amount Input -->
            <q-input
              v-model.number="amount"
              type="number"
              step="0.001"
              :label="$t('send.enterAmount')"
              label-color="indigo-3"
              dark
              outlined
              required
              :rules="[ val => val && val > 0 || (locale === 'en' ? 'Amount must be greater than 0' : 'يجب أن يكون المبلغ أكبر من 0') ]"
            >
              <template v-slot:prepend>
                <q-icon name="payments" color="indigo-4" />
              </template>
            </q-input>

            <!-- Description -->
            <q-input
              v-model="description"
              type="textarea"
              :label="$t('send.memo')"
              label-color="indigo-3"
              dark
              outlined
              rows="3"
              :placeholder="locale === 'en' ? 'Enter transfer details or memo' : 'أدخل بياناً لعملية التحويل'"
            >
              <template v-slot:prepend>
                <q-icon name="description" color="indigo-4" />
              </template>
            </q-input>

            <q-btn
              type="submit"
              color="primary"
              class="w-full q-py-sm rounded-borders text-bold hover-scale"
              size="large"
              :loading="loading"
              :disabled="!recipientName"
              :label="locale === 'en' ? 'Send Funds' : 'إرسال الأموال'"
              no-caps
            />

          </q-form>
        </q-card-section>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.max-w-lg {
  max-width: 500px;
}
.bg-emerald-10-dim {
  background: rgba(16, 185, 129, 0.12) !important;
}
.border-left-emerald {
  border-left: 3px solid #10B981;
}
.text-emerald-2 {
  color: #D1FAE5;
}
.block {
  display: block;
}
</style>
