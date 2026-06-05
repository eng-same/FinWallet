<script setup lang="ts">
import { ref } from 'vue';
import { useWalletStore } from '../../stores/wallet';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';
import { useI18n } from 'vue-i18n';

const walletStore = useWalletStore();
const router = useRouter();
const $q = useQuasar();
const { t, locale } = useI18n();

const amount = ref<number | null>(null);
const description = ref('');
const loading = ref(false);

// Payment method selection state
const selectedMethod = ref<'Card' | 'Bank' | 'onePay' | 'LyPay'>('Card');

// Payment method inputs
const cardNumber = ref('');
const bankName = ref('NCB');
const accountNumber = ref('');
const onePayId = ref('');
const lyPayIban = ref('');

interface PaymentMethod {
  id: 'Card' | 'Bank' | 'onePay' | 'LyPay';
  icon?: string;
  imgSrc?: string;
  imgExists?: boolean;
  name: string;
  labelKey: string;
}

const methods = ref<PaymentMethod[]>([
  { id: 'Card', icon: 'credit_card', name: 'Card', labelKey: 'topup.methodCard' },
  { id: 'Bank', icon: 'account_balance', name: 'Bank', labelKey: 'topup.methodBank' },
  { id: 'onePay', imgSrc: '/OnePaylogo.jpg', imgExists: true, name: 'onePay', labelKey: 'topup.methodOnePay' },
  { id: 'LyPay', imgSrc: '/LypayLogo.jpg', imgExists: true, name: 'LyPay', labelKey: 'topup.methodLyPay' }
]);

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
    if (selectedMethod.value === 'Card') {
      const transaction = await walletStore.requestTopUp(amount.value, description.value);

      const dialogTitle = locale.value === 'en' ? 'Top-Up Initialized' : 'تم تهيئة عملية الشحن';
      const dialogMessage = locale.value === 'en' 
        ? `Your request for ${amount.value.toFixed(3)} LYD is sent to the Bank Core via RabbitMQ. Since bank processing is asynchronous, the transaction is marked as PendingBankApproval. You can inspect the final status (Accepted or Rejected) shortly!`
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
    } else {
      // Frontend-only top-up for other methods
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      const dialogTitle = t('topup.successTitleTopup');
      const dialogMessage = t('topup.successMessageTopup');
      const dialogOk = locale.value === 'en' ? 'Go to Dashboard' : 'الذهاب للوحة القيادة';

      $q.dialog({
        title: dialogTitle,
        message: dialogMessage,
        ok: {
          label: dialogOk,
          color: 'secondary'
        },
        dark: true,
        persistent: true
      }).onOk(() => {
        router.push('/dashboard');
      });
    }
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

          <!-- Payment Method Selector -->
          <div class="q-mb-lg">
            <div class="text-caption text-indigo-3 font-mono text-uppercase text-weight-bold q-mb-sm">
              {{ locale === 'en' ? 'Payment Method' : 'طريقة الدفع' }}
            </div>
            <div class="row q-col-gutter-sm">
              <div class="col-6 col-sm-3" v-for="method in methods" :key="method.id">
                <q-card 
                  class="payment-method-card text-center cursor-pointer hover-scale border-transition"
                  :class="{ 'active-method': selectedMethod === method.id }"
                  @click="selectedMethod = method.id"
                >
                  <q-card-section class="q-pa-sm">
                    <!-- Icon -->
                    <q-icon :name="method.icon" size="24px" v-if="method.icon" :color="selectedMethod === method.id ? 'primary' : 'grey-5'" class="q-mb-xs" />
                    <!-- Placeholder image with fallback -->
                    <div class="logo-container row justify-center items-center" v-else style="height: 28px;">
                      <img :src="method.imgSrc" class="payment-logo" v-if="method.imgExists" @error="method.imgExists = false" :alt="method.name" />
                      <div class="payment-logo-fallback text-weight-bolder" :class="selectedMethod === method.id ? 'text-primary' : 'text-grey-5'" v-else>
                        {{ method.name }}
                      </div>
                    </div>
                    <div class="text-caption font-size-11 text-weight-bold q-mt-xs">{{ $t(method.labelKey) }}</div>
                  </q-card-section>
                </q-card>
              </div>
            </div>
          </div>

          <q-form @submit.prevent="handleTopUpSubmit" class="q-gutter-md">
            
            <!-- Dynamic Fields by selected method -->

            <!-- CARD PAYMENT METHOD -->
            <template v-if="selectedMethod === 'Card'">
              <q-select
                v-model="bankName"
                :options="['NCB', 'Wahda', 'Sahary', 'Commerce & Development']"
                :label="$t('topup.selectBank')"
                label-color="indigo-3"
                dark
                outlined
                required
              />

              <q-input
                v-model="cardNumber"
                type="text"
                :label="$t('topup.cardNumber')"
                label-color="indigo-3"
                dark
                outlined
                required
                mask="#### #### #### ####"
                unmasked-value
                :rules="[ val => (typeof val === 'string' && val.length === 16) || (locale === 'en' ? 'Card number must be 16 digits' : 'رقم البطاقة يجب أن يكون 16 خانة') ]"
              >
                <template v-slot:prepend>
                  <q-icon name="credit_card" color="indigo-4" />
                </template>
              </q-input>
            </template>

            <!-- BANK TRANSFER METHOD -->
            <template v-else-if="selectedMethod === 'Bank'">
              <q-select
                v-model="bankName"
                :options="['NCB', 'Wahda', 'Sahary', 'Commerce & Development']"
                :label="$t('topup.selectBank')"
                label-color="indigo-3"
                dark
                outlined
                required
              />

              <q-input
                v-model="accountNumber"
                type="text"
                :label="$t('topup.accountNumber')"
                :placeholder="$t('topup.bankNumberHint')"
                label-color="indigo-3"
                dark
                outlined
                required
                mask="###############"
                unmasked-value
                :rules="[ val => (typeof val === 'string' && val.length === 15) || (locale === 'en' ? 'Account number must be 15 digits' : 'رقم الحساب يجب أن يكون 15 خانة') ]"
              >
                <template v-slot:prepend>
                  <q-icon name="account_balance" color="indigo-4" />
                </template>
              </q-input>
            </template>

            <!-- ONEPAY METHOD -->
            <template v-else-if="selectedMethod === 'onePay'">
              <q-input
                v-model="onePayId"
                type="text"
                :label="$t('topup.onePayId')"
                :placeholder="$t('topup.onePayHint')"
                label-color="indigo-3"
                dark
                outlined
                required
                :rules="[ val => (typeof val === 'string' && val.trim().length >= 5) || (locale === 'en' ? 'Account number must be at least 5 characters' : 'رقم الحساب يجب أن يكون 5 خانات على الأقل') ]"
              >
                <template v-slot:prepend>
                  <q-icon name="account_box" color="indigo-4" />
                </template>
              </q-input>
            </template>

            <!-- LYPAY METHOD -->
            <template v-else-if="selectedMethod === 'LyPay'">
              <q-input
                v-model="lyPayIban"
                type="text"
                :label="$t('topup.lyPayIban')"
                :placeholder="$t('topup.lyPayHint')"
                label-color="indigo-3"
                dark
                outlined
                required
                @update:model-value="val => { if (typeof val === 'string') lyPayIban = val.toUpperCase(); }"
                :rules="[ val => (typeof val === 'string' && /^LY\d{23}$/.test(val.trim().toUpperCase())) || (locale === 'en' ? 'IBAN must start with LY followed by 23 digits' : 'الآيبان يجب أن يبدأ بـ LY متبوعة بـ 23 رقماً') ]"
              >
                <template v-slot:prepend>
                  <q-icon name="vpn_key" color="indigo-4" />
                </template>
              </q-input>
            </template>

            <!-- COMMON FIELDS -->
            <q-input
              v-model.number="amount"
              type="number"
              step="0.001"
              :label="$t('topup.amount')"
              label-color="indigo-3"
              dark
              outlined
              required
              :rules="[ val => (val !== null && val !== undefined && Number(val) > 0) || (locale === 'en' ? 'Amount must be greater than 0' : 'يجب أن يكون المبلغ أكبر من 0') ]"
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
              :placeholder="locale === 'en' ? 'Provide a bank top-up memo' : 'أدخل بياناً لعملية شحن الرصيد'"
            >
              <template v-slot:prepend>
                <q-icon name="description" color="indigo-4" />
              </template>
            </q-input>

            <!-- Alert describing messaging flow (Shows only for RabbitMQ Card method) -->
            <div v-if="selectedMethod === 'Card'" class="q-pa-md bg-grey-10-dim rounded-borders border-left border-indigo text-caption text-grey-4 q-mb-md">
              <div class="text-bold text-white q-mb-xs">
                {{ locale === 'en' ? 'Banking System Workflow:' : 'مسار نظام معالجة المصرف:' }}
              </div>
              <span v-if="locale === 'en'">
                Your top-up will be published to the <span class="text-weight-bold text-indigo-3 font-mono">finwallet.bank.topup.requests</span> RabbitMQ queue.
                The Bank Worker will process the request and return its response.
              </span>
              <span v-else>
                سيتم إرسال طلب الشحن إلى طابور RabbitMQ المسمى <span class="text-weight-bold text-indigo-3 font-mono">finwallet.bank.topup.requests</span>.
                سيقوم معالج البنك المحاكي باتخاذ القرار عشوائياً (70% قبول / 30% رفض) وإرجاع النتيجة.
              </span>
            </div>

            <!-- Information block describing flow for other methods -->
            <div v-else class="q-pa-md bg-grey-10-dim rounded-borders border-left-amber text-caption text-grey-4 q-mb-md">
              <div class="text-bold text-white q-mb-xs">
                {{ locale === 'en' ? 'Frontend Processing Mode:' : 'وضع معالجة الواجهة الأمامية:' }}
              </div>
              <div class="text-caption text-grey-7" v-if="locale === 'en'">
                This is a secure integration. The transaction details will be processed locally on the client-side for presentation purposes without modifying the backend database.
              </div>
              <span v-else>
                هذا الخيار عبارة عن محاكاة فقط. سيتم تنفيذ المعاملة افتراضياً على جانب العميل لأغراض العرض التقديمي دون تعديل قاعدة البيانات الفعلية.
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
.border-left-amber {
  border-left: 3px solid #F59E0B;
}

/* Payment Method Card Styles */
.payment-method-card {
  background: rgba(30, 41, 59, 0.25) !important;
  border: 1px solid rgba(255, 255, 255, 0.05) !important;
  border-radius: 12px !important;
  transition: all 0.3s ease;
}
.payment-method-card:hover {
  border-color: rgba(79, 70, 229, 0.3) !important;
  background: rgba(30, 41, 59, 0.45) !important;
}
.active-method {
  border: 2px solid #4F46E5 !important;
  background: rgba(79, 70, 229, 0.1) !important;
  box-shadow: 0 0 10px rgba(79, 70, 229, 0.3) !important;
}
.font-size-11 {
  font-size: 11px;
}
.border-transition {
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

/* Payment Logo fallback Styles */
.payment-logo {
  max-height: 24px;
  max-width: 100%;
  object-fit: contain;
}
.payment-logo-fallback {
  font-size: 12px;
  letter-spacing: 0.5px;
}
</style>
