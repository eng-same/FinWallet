<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const authStore = useAuthStore();
const router = useRouter();
const $q = useQuasar();

const email = ref('');
const password = ref('');
const showPassword = ref(false);
const loading = ref(false);

const handleLogin = async () => {
  if (!email.value || !password.value) {
    $q.notify({
      type: 'warning',
      message: 'Please fill in all fields'
    });
    return;
  }

  loading.value = true;
  try {
    const response = await authStore.login({
      email: email.value,
      password: password.value
    });

    $q.notify({
      type: 'positive',
      message: `Welcome back, ${response.user.fullName}!`
    });

    if (response.role === 'Admin') {
      router.push('/admin/dashboard');
    } else {
      router.push('/dashboard');
    }
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Login failed';
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
  <q-page class="flex flex-center bg-radial-dark q-pa-lg">
    <div class="w-full max-w-md">
      
      <!-- Logo Title -->
      <div class="text-center q-mb-xl">
        <q-avatar size="64px" color="indigo-10" class="q-mb-md shadow-5 hover-scale" @click="router.push('/')">
          <q-icon name="account_balance_wallet" size="32px" color="white" />
        </q-avatar>
        <h2 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">Sign In</h2>
        <p class="text-subtitle2 text-grey-5">Access your digital wallet system</p>
      </div>

      <!-- Login Form Card -->
      <q-card class="glass-card q-pa-lg hover-scale">
        <q-card-section class="q-pa-none">
          <q-form @submit.prevent="handleLogin" class="q-gutter-md">
            
            <q-input
              v-model="email"
              type="email"
              label="Email Address"
              label-color="indigo-3"
              dark
              outlined
              class="q-mb-md"
              required
            >
              <template v-slot:prepend>
                <q-icon name="email" color="indigo-4" />
              </template>
            </q-input>

            <q-input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              label="Password"
              label-color="indigo-3"
              dark
              outlined
              class="q-mb-lg"
              required
            >
              <template v-slot:prepend>
                <q-icon name="lock" color="indigo-4" />
              </template>
              <template v-slot:append>
                <q-icon
                  :name="showPassword ? 'visibility_off' : 'visibility'"
                  class="cursor-pointer"
                  color="indigo-4"
                  @click="showPassword = !showPassword"
                />
              </template>
            </q-input>

            <q-btn
              type="submit"
              color="primary"
              class="w-full q-py-sm rounded-borders text-bold hover-scale"
              size="large"
              :loading="loading"
              label="Login"
              no-caps
            />

          </q-form>
        </q-card-section>
      </q-card>

      <!-- Extra link -->
      <div class="text-center q-mt-xl">
        <span class="text-grey-5">Don't have an account? </span>
        <q-btn flat dense no-caps color="primary" class="text-bold" label="Register here" @click="router.push('/register')" />
      </div>

      <!-- Quick Demo Users Box -->
      <q-card class="glass-card-dim q-mt-xl q-pa-md">
        <div class="text-weight-bold text-indigo-3 text-caption q-mb-sm font-mono text-uppercase">
          Demo Credentials
        </div>
        <div class="row q-col-gutter-sm text-caption">
          <div class="col-12">
            <span class="text-grey-4 text-weight-bold">Administrator:</span>
            <div class="text-grey-5 font-mono">admin@finwallet.local / Admin@123456</div>
          </div>
        </div>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.bg-radial-dark {
  background: radial-gradient(circle at center, #1E293B 0%, #0F172A 70%, #090D16 100%) !important;
}

.w-full {
  width: 100%;
}

.max-w-md {
  max-width: 420px;
}
</style>
