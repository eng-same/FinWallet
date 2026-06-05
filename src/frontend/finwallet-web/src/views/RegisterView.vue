<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const authStore = useAuthStore();
const router = useRouter();
const $q = useQuasar();

const fullName = ref('');
const email = ref('');
const phoneNumber = ref('');
const password = ref('');
const confirmPassword = ref('');
const showPassword = ref(false);
const loading = ref(false);

const handleRegister = async () => {
  if (!fullName.value || !email.value || !phoneNumber.value || !password.value || !confirmPassword.value) {
    $q.notify({
      type: 'warning',
      message: 'Please fill in all fields'
    });
    return;
  }

  if (password.value !== confirmPassword.value) {
    $q.notify({
      type: 'negative',
      message: 'Passwords do not match'
    });
    return;
  }

  loading.value = true;
  try {
    const response = await authStore.register({
      fullName: fullName.value,
      email: email.value,
      phoneNumber: phoneNumber.value,
      password: password.value,
      confirmPassword: confirmPassword.value
    });

    $q.notify({
      type: 'positive',
      message: `Account created! Welcome, ${response.user.fullName}. Your wallet is ready.`
    });

    router.push('/dashboard');
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Registration failed';
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
      
      <!-- Title -->
      <div class="text-center q-mb-md">
        <q-avatar size="64px" color="indigo-10" class="q-mb-md shadow-5 hover-scale" @click="router.push('/')">
          <q-icon name="account_balance_wallet" size="32px" color="white" />
        </q-avatar>
        <h2 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">Create Account</h2>
        <p class="text-subtitle2 text-grey-5">Get a digital wallet automatically</p>
      </div>

      <!-- Card Form -->
      <q-card class="glass-card q-pa-lg hover-scale">
        <q-card-section class="q-pa-none">
          <q-form @submit.prevent="handleRegister" class="q-gutter-sm">
            
            <q-input
              v-model="fullName"
              type="text"
              label="Full Name"
              label-color="indigo-3"
              dark
              outlined
              required
            >
              <template v-slot:prepend>
                <q-icon name="person" color="indigo-4" />
              </template>
            </q-input>

            <q-input
              v-model="email"
              type="email"
              label="Email Address"
              label-color="indigo-3"
              dark
              outlined
              required
            >
              <template v-slot:prepend>
                <q-icon name="email" color="indigo-4" />
              </template>
            </q-input>

            <q-input
              v-model="phoneNumber"
              type="tel"
              label="Phone Number (10 digits)"
              placeholder="e.g. 0912345678"
              label-color="indigo-3"
              dark
              outlined
              mask="##########"
              required
            >
              <template v-slot:prepend>
                <q-icon name="phone" color="indigo-4" />
              </template>
            </q-input>

            <q-input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              label="Password"
              label-color="indigo-3"
              dark
              outlined
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

            <q-input
              v-model="confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              label="Confirm Password"
              label-color="indigo-3"
              dark
              outlined
              class="q-mb-md"
              required
            >
              <template v-slot:prepend>
                <q-icon name="lock_reset" color="indigo-4" />
              </template>
            </q-input>

            <q-btn
              type="submit"
              color="primary"
              class="w-full q-py-sm rounded-borders text-bold hover-scale"
              size="large"
              :loading="loading"
              label="Register"
              no-caps
            />

          </q-form>
        </q-card-section>
      </q-card>

      <!-- Footer Link -->
      <div class="text-center q-mt-lg">
        <span class="text-grey-5">Already have an account? </span>
        <q-btn flat dense no-caps color="primary" class="text-bold" label="Sign in here" @click="router.push('/login')" />
      </div>

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
