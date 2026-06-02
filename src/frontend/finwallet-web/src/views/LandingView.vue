<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const router = useRouter();
const authStore = useAuthStore();

const navigateToDashboard = () => {
  if (authStore.isAuthenticated) {
    if (authStore.isAdmin) {
      router.push('/admin/dashboard');
    } else {
      router.push('/dashboard');
    }
  } else {
    router.push('/login');
  }
};
</script>

<template>
  <q-page class="flex flex-center bg-radial-indigo text-white q-pa-lg">
    <div class="max-w-4xl text-center">
      
      <!-- Premium Hero Badge -->
      <q-badge outline color="indigo-4" class="q-py-xs q-px-md text-subtitle2 rounded-borders text-bold q-mb-lg letter-spacing-1 hover-scale animate-fade">
        <q-icon name="stars" class="q-mr-xs" />
        ACADEMIC FINTECH PROJECT 2026
      </q-badge>

      <!-- Main Heading -->
      <h1 class="text-h2 md:text-h1 text-weight-bolder leading-tight q-mt-none q-mb-md animate-fade-up">
        Experience Simulated Banking with
        <span class="text-transparent bg-clip-text bg-gradient-indigo">FinWallet</span>
      </h1>

      <!-- Description -->
      <p class="text-subtitle1 text-grey-4 max-w-2xl mx-auto q-mb-xl text-weight-light animate-fade-up" style="line-height: 1.8;">
        A high-fidelity academic fintech simulation platform. Register user accounts, manage digital wallets, top up balances with mock bank cores, perform instant peer-to-peer transfers, and monitor platform metrics.
      </p>

      <!-- CTA Buttons -->
      <div class="row justify-center q-gutter-md q-mb-xl animate-fade-up">
        <q-btn
          elevated
          color="primary"
          size="lg"
          class="q-px-xl rounded-borders text-bold hover-scale"
          label="Launch App"
          no-caps
          @click="navigateToDashboard"
        />
        <q-btn
          outline
          color="white"
          size="lg"
          class="q-px-xl rounded-borders text-bold hover-scale"
          label="Register Account"
          no-caps
          v-if="!authStore.isAuthenticated"
          @click="router.push('/register')"
        />
      </div>

      <!-- Feature Grid -->
      <div class="row q-col-gutter-lg justify-center q-mt-lg animate-fade-up">
        
        <div class="col-12 col-md-4">
          <q-card class="glass-card hover-scale fit q-pa-sm">
            <q-card-section>
              <q-avatar color="indigo-10" text-color="indigo-3" class="q-mb-md">
                <q-icon name="account_balance_wallet" size="28px" />
              </q-avatar>
              <div class="text-h6 text-white text-weight-bold q-mb-sm">Simulated Wallet</div>
              <div class="text-body2 text-grey-5">
                Every user receives an active wallet upon registration. View transactions and download digital receipts instantly.
              </div>
            </q-card-section>
          </q-card>
        </div>

        <div class="col-12 col-md-4">
          <q-card class="glass-card hover-scale fit q-pa-sm">
            <q-card-section>
              <q-avatar color="emerald-10" text-color="emerald-3" class="q-mb-md">
                <q-icon name="swap_horiz" size="28px" />
              </q-avatar>
              <div class="text-h6 text-white text-weight-bold q-mb-sm">Instant Transfers</div>
              <div class="text-body2 text-grey-5">
                Transfer LYD balances peer-to-peer synchronously with transaction auditing, atomic updates, and double ledger logging.
              </div>
            </q-card-section>
          </q-card>
        </div>

        <div class="col-12 col-md-4">
          <q-card class="glass-card hover-scale fit q-pa-sm">
            <q-card-section>
              <q-avatar color="amber-10" text-color="amber-3" class="q-mb-md">
                <q-icon name="settings_input_component" size="28px" />
              </q-avatar>
              <div class="text-h6 text-white text-weight-bold q-mb-sm">Mock Bank Core</div>
              <div class="text-body2 text-grey-5">
                Initiate balance top-ups processed via RabbitMQ. External core accepts/rejects top-ups asynchronously.
              </div>
            </q-card-section>
          </q-card>
        </div>

      </div>

    </div>
  </q-page>
</template>

<style scoped>
/* Gradient Background */
.bg-radial-indigo {
  background: radial-gradient(circle at top, #1E1B4B 0%, #0F172A 70%, #090D16 100%) !important;
}

.max-w-4xl {
  max-width: 900px;
}

.max-w-2xl {
  max-width: 650px;
}

.mx-auto {
  margin-left: auto;
  margin-right: auto;
}

/* Gradient text effect */
.bg-gradient-indigo {
  background: linear-gradient(135deg, #818CF8 0%, #4F46E5 100%);
}

.text-transparent {
  color: transparent;
}

.bg-clip-text {
  -webkit-background-clip: text;
  background-clip: text;
}

/* Keyframes and Animations */
.animate-fade {
  animation: fadeIn 0.8s ease-out;
}

.animate-fade-up {
  animation: fadeInUp 0.8s ease-out both;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
