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
        class="q-mb-md fade-in-down"
        @click="router.push('/dashboard')"
      />

      <!-- Transfer Card -->
      <q-card class="glass-card q-pa-lg send-card relative overflow-hidden">

        <!-- Decorative glow orb -->
        <div class="glow-orb" aria-hidden="true" />

        <q-card-section class="q-pa-none" style="position: relative; z-index: 1;">

          <!-- Step Indicator -->
          <div class="step-indicator q-mb-lg fade-in-up" style="animation-delay: 0.05s;">
            <div class="step-track">
              <div class="step-item" :class="{ 'step-active': true }">
                <div class="step-dot" />
                <span class="step-label">Recipient</span>
              </div>
              <div class="step-line" :class="{ 'step-line-active': !!recipientName }" />
              <div class="step-item" :class="{ 'step-active': !!recipientName }">
                <div class="step-dot" />
                <span class="step-label">Amount</span>
              </div>
              <div class="step-line" :class="{ 'step-line-active': !!recipientName && !!amount && amount > 0 }" />
              <div class="step-item" :class="{ 'step-active': !!recipientName && !!amount && amount > 0 }">
                <div class="step-dot" />
                <span class="step-label">Confirm</span>
              </div>
            </div>
          </div>

          <!-- Title area -->
          <div class="row items-center q-gutter-md q-mb-xl fade-in-up" style="animation-delay: 0.1s;">
            <div class="avatar-ring-wrap">
              <q-avatar color="indigo-10" text-color="indigo-3" size="48px" class="send-avatar">
                <q-icon name="send" size="24px" />
              </q-avatar>
            </div>
            <div>
              <h5 class="text-h5 text-white text-weight-bold q-my-none" style="font-family: 'Outfit', sans-serif;">{{ $t('send.title') }}</h5>
              <p class="text-caption text-grey-5 q-my-none letter-spacing-1">{{ $t('send.subtitle') }}</p>
            </div>
          </div>

          <q-form @submit.prevent="handleSendMoneySubmit" class="q-gutter-md">
            
            <!-- Recipient Wallet -->
            <div class="fade-in-up" style="animation-delay: 0.18s;">
              <q-input
                v-model="recipientWallet"
                type="text"
                :label="$t('send.recipientNum')"
                :placeholder="'FW-2026-000002'"
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
            </div>

            <!-- Connection line indicator -->
            <q-slide-transition>
              <div v-if="recipientName" class="connection-line-wrap">
                <div class="connection-dots">
                  <span /><span /><span />
                </div>
              </div>
            </q-slide-transition>

            <!-- Recipient Fullname display card -->
            <q-slide-transition>
              <div v-if="recipientName" class="recipient-confirmed-card scale-in">
                <div class="row items-center justify-between no-wrap">
                  <div class="row items-center q-gutter-sm">
                    <q-icon name="check_circle" color="green-4" size="22px" />
                    <div>
                      <span class="text-grey-4 text-caption block letter-spacing-1" style="font-size: 0.68rem;">
                        {{ locale === 'en' ? 'RECIPIENT CONFIRMED' : 'تم تأكيد الحساب المستلم' }}
                      </span>
                      <span class="text-white text-subtitle1 text-weight-medium">{{ recipientName }}</span>
                    </div>
                  </div>
                  <q-avatar size="36px" color="green-10" text-color="green-3" class="q-ml-sm">
                    <q-icon name="person" size="20px" />
                  </q-avatar>
                </div>
              </div>
            </q-slide-transition>

            <!-- Amount Input -->
            <div class="fade-in-up" style="animation-delay: 0.26s;">
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
            </div>

            <!-- Description -->
            <div class="fade-in-up" style="animation-delay: 0.34s;">
              <q-input
                v-model="description"
                type="textarea"
                :label="$t('send.memo')"
                label-color="indigo-3"
                dark
                outlined
                rows="3"
                :placeholder="$t('send.memo')"
              >
                <template v-slot:prepend>
                  <q-icon name="description" color="indigo-4" />
                </template>
              </q-input>
            </div>

            <!-- Submit Button -->
            <div class="fade-in-up" style="animation-delay: 0.42s;">
              <q-btn
                type="submit"
                class="w-full q-py-md rounded-borders text-bold submit-btn"
                :class="{ 'submit-btn--disabled': !recipientName }"
                size="large"
                :loading="loading"
                :disabled="!recipientName"
                :label="locale === 'en' ? 'Send Funds' : 'إرسال الأموال'"
                icon="send"
                no-caps
                unelevated
              />
            </div>

          </q-form>
        </q-card-section>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
/* ── Layout ─────────────────────────────────────── */
.w-full {
  width: 100%;
}
.max-w-lg {
  max-width: 560px;
}
.relative {
  position: relative;
}
.overflow-hidden {
  overflow: hidden;
}

/* ── Send Card ──────────────────────────────────── */
.send-card {
  border-radius: 20px !important;
}

/* ── Decorative Glow Orb ────────────────────────── */
.glow-orb {
  position: absolute;
  top: -60px;
  right: -60px;
  width: 200px;
  height: 200px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(139, 92, 246, 0.18) 0%, rgba(99, 60, 200, 0.06) 50%, transparent 70%);
  pointer-events: none;
  z-index: 0;
  filter: blur(8px);
}

/* ── Step Indicator ─────────────────────────────── */
.step-indicator {
  padding-bottom: 4px;
}
.step-track {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0;
}
.step-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  opacity: 0.35;
  transition: opacity 0.35s ease;
}
.step-item.step-active {
  opacity: 1;
}
.step-dot {
  width: 9px;
  height: 9px;
  border-radius: 50%;
  background: rgba(165, 148, 249, 0.35);
  border: 1.5px solid rgba(165, 148, 249, 0.5);
  transition: background 0.35s ease, border-color 0.35s ease, box-shadow 0.35s ease;
}
.step-active .step-dot {
  background: #a78bfa;
  border-color: #a78bfa;
  box-shadow: 0 0 8px rgba(167, 139, 250, 0.45);
}
.step-label {
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 0.62rem;
  color: rgba(209, 213, 219, 0.7);
  letter-spacing: 0.5px;
  text-transform: uppercase;
  white-space: nowrap;
}
.step-active .step-label {
  color: #c4b5fd;
}
.step-line {
  width: 52px;
  height: 1.5px;
  background: rgba(165, 148, 249, 0.18);
  margin: 0 6px;
  margin-bottom: 18px;
  border-radius: 2px;
  transition: background 0.4s ease;
}
.step-line-active {
  background: rgba(167, 139, 250, 0.55);
}

/* ── Avatar Ring Pulse ──────────────────────────── */
.avatar-ring-wrap {
  position: relative;
  display: inline-flex;
}
.avatar-ring-wrap::before {
  content: '';
  position: absolute;
  inset: -5px;
  border-radius: 50%;
  border: 2px solid rgba(129, 140, 248, 0.3);
  animation: pulse-ring 2.5s ease-in-out infinite;
}
@keyframes pulse-ring {
  0%, 100% { transform: scale(1); opacity: 0.5; }
  50% { transform: scale(1.12); opacity: 0; }
}

/* ── Connection Dots ────────────────────────────── */
.connection-line-wrap {
  display: flex;
  justify-content: flex-start;
  padding-left: 20px;
  margin-top: -6px;
  margin-bottom: -6px;
}
.connection-dots {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}
.connection-dots span {
  display: block;
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: rgba(16, 185, 129, 0.5);
}

/* ── Recipient Confirmed Card ───────────────────── */
.recipient-confirmed-card {
  padding: 14px 18px;
  background: rgba(16, 185, 129, 0.1);
  border-left: 3px solid #10B981;
  border-radius: 12px;
  box-shadow: 0 0 20px rgba(16, 185, 129, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.04);
  backdrop-filter: blur(4px);
}
.block {
  display: block;
}

/* ── Scale-in entrance ──────────────────────────── */
.scale-in {
  animation: scaleIn 0.35s cubic-bezier(0.34, 1.56, 0.64, 1) both;
}
@keyframes scaleIn {
  from { transform: scale(0.88); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

/* ── Input Focus Glow ───────────────────────────── */
.send-card :deep(.q-field--outlined .q-field__control) {
  transition: box-shadow 0.3s ease, border-color 0.3s ease;
  border-radius: 10px;
}
.send-card :deep(.q-field--outlined.q-field--focused .q-field__control) {
  box-shadow: 0 0 0 2px rgba(129, 140, 248, 0.2), 0 0 16px rgba(129, 140, 248, 0.08);
}
.send-card :deep(.q-field--outlined .q-field__control::after) {
  transition: border-color 0.3s ease;
}

/* ── Submit Button ──────────────────────────────── */
.submit-btn {
  background: linear-gradient(135deg, #6366f1 0%, #7c3aed 50%, #8b5cf6 100%) !important;
  color: #fff !important;
  border-radius: 12px !important;
  font-size: 1.05rem;
  letter-spacing: 0.5px;
  position: relative;
  overflow: hidden;
  transition: filter 0.3s ease, transform 0.2s ease, box-shadow 0.3s ease;
  box-shadow: 0 4px 20px rgba(99, 102, 241, 0.3);
}
.submit-btn:hover:not(.submit-btn--disabled) {
  filter: brightness(1.1);
  transform: translateY(-1px);
  box-shadow: 0 6px 28px rgba(99, 102, 241, 0.4);
}
.submit-btn::after {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 60%;
  height: 100%;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.12),
    transparent
  );
  transition: none;
  pointer-events: none;
}
.submit-btn:hover:not(.submit-btn--disabled)::after {
  animation: btn-shine 0.7s ease forwards;
}
@keyframes btn-shine {
  from { left: -100%; }
  to { left: 130%; }
}
.submit-btn--disabled {
  background: linear-gradient(135deg, rgba(99, 102, 241, 0.25) 0%, rgba(124, 58, 237, 0.2) 100%) !important;
  color: rgba(255, 255, 255, 0.35) !important;
  box-shadow: none;
  cursor: not-allowed;
}

/* ── Fade-in-up animation ───────────────────────── */
.fade-in-up {
  animation: fadeInUp 0.5s ease both;
}
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(18px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ── Fade-in-down animation (back button) ────────── */
.fade-in-down {
  animation: fadeInDown 0.45s ease both;
}
@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-14px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
