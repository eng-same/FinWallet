<script setup lang="ts">
import { ref } from 'vue';
import { useWalletStore } from '../../stores/wallet';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';
import { useI18n } from 'vue-i18n';

const walletStore = useWalletStore();
const router = useRouter();
const $q = useQuasar();
const { locale } = useI18n();

const amount = ref<number | null>(null);
const description = ref('');
const loading = ref(false);

const handleTopUpSubmit = async () => {
  if (!amount.value || amount.value <= 0) {
    $q.notify({
      type: 'warning',
      message: locale.value === 'en' ? 'Please enter a valid amount greater than 0' : 'يرجى إدخال مبلغ صالح أكبر من 0'
    });
    return;
  }

  loading.value = true;
  try {
    const transaction = await walletStore.requestTopUp(amount.value, description.value);

    // Prompt user about the asynchronous RabbitMQ workflow (Localized dialog text)
    const dialogTitle = locale.value === 'en' ? 'Top-Up Initialized' : 'تم تهيئة عملية الشحن';
    const dialogMessage = locale.value === 'en' 
      ? `Your request for ${amount.value.toFixed(3)} LYD is sent to the Mock Bank Core via RabbitMQ. Since bank processing is asynchronous, the transaction is marked as PendingBankApproval. You can inspect the final status (Accepted or Rejected) shortly!`
      : `تم إرسال طلبك بقيمة ${amount.value.toFixed(3)} د.ل إلى نظام البنك عبر خدمة RabbitMQ. وبما أن المعالجة البنكية غير متزامنة، فقد تم وضع علامة "قيد موافقة البنك" على المعاملة. يمكنك التحقق من الحالة النهائية قريباً!`;
    const dialogTrack = locale.value === 'en' ? 'Track Transaction' : 'متابعة المعاملة';

    $q.dialog({
      title: dialogTitle,
      message: dialogMessage,
      ok: {
        label: dialogTrack,
        color: 'primary'
      },
      dark: true,
      persistent: true
    }).onOk(() => {
      router.push(`/transactions/${transaction.id}`);
    });
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to submit top-up request';
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
      
      <!-- Go Back Btn -->
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

      <!-- Form Card -->
      <q-card class="glass-card q-pa-lg">
        <q-card-section class="q-pa-none">
          <div class="row items-center q-gutter-md q-mb-xl">
            <q-avatar color="indigo-10" text-color="indigo-3" size="48px">
              <q-icon name="add_card" size="28px" />
            </q-avatar>
            <div>
              <h5 class="text-h5 text-white text-weight-bold q-my-none">{{ $t('topup.title') }}</h5>
              <p class="text-caption text-grey-5 q-my-none">{{ $t('topup.subtitle') }}</p>
            </div>
          </div>

          <q-form @submit.prevent="handleTopUpSubmit" class="q-gutter-md">
            
            <q-input
              v-model.number="amount"
              type="number"
              step="0.001"
              :label="$t('topup.amount')"
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

            <q-input
              v-model="description"
              type="textarea"
              :label="locale === 'en' ? 'Description (Optional)' : 'الوصف (اختياري)'"
              label-color="indigo-3"
              dark
              outlined
              rows="3"
              :placeholder="locale === 'en' ? 'Provide a simulated bank top-up memo' : 'أدخل بياناً لعملية شحن الرصيد'"
            >
              <template v-slot:prepend>
                <q-icon name="description" color="indigo-4" />
              </template>
            </q-input>

            <!-- Alert describing messaging flow -->
            <div class="q-pa-md bg-grey-10-dim rounded-borders border-left border-indigo text-caption text-grey-4 q-mb-md">
              <div class="text-bold text-white q-mb-xs">
                {{ locale === 'en' ? 'Simulated Banking System Workflow:' : 'مسار نظام معالجة المصرف المحاكي:' }}
              </div>
              <span v-if="locale === 'en'">
                Your top-up will be published to the <span class="text-weight-bold text-indigo-3 font-mono">finwallet.bank.topup.requests</span> RabbitMQ queue.
                The Mock Bank Worker will process the request randomly (70% Accept / 30% Reject) and return its response.
              </span>
              <span v-else>
                سيتم إرسال طلب الشحن إلى طابور RabbitMQ المسمى <span class="text-weight-bold text-indigo-3 font-mono">finwallet.bank.topup.requests</span>.
                سيقوم معالج البنك المحاكي باتخاذ القرار عشوائياً (70% قبول / 30% رفض) وإرجاع النتيجة.
              </span>
            </div>

            <q-btn
              type="submit"
              color="primary"
              class="w-full q-py-sm rounded-borders text-bold hover-scale"
              size="large"
              :loading="loading"
              :label="locale === 'en' ? 'Initialize Top-Up' : 'بدء عملية شحن الرصيد'"
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
.bg-grey-10-dim {
  background: rgba(30, 41, 59, 0.3);
}
.border-left {
  border-left: 3px solid #4F46E5;
}
</style>
