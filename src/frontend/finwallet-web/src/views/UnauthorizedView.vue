<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';

const router = useRouter();
const authStore = useAuthStore();

const goHome = () => {
  if (authStore.isAuthenticated) {
    if (authStore.isAdmin) {
      router.push('/admin/dashboard');
    } else {
      router.push('/dashboard');
    }
  } else {
    router.push('/');
  }
};
</script>

<template>
  <q-page class="flex flex-center text-center q-pa-lg">
    <div>
      <q-icon name="gpp_bad" size="120px" color="negative" class="q-mb-md" />
      <h3 class="text-h3 text-white text-weight-bolder q-mt-none q-mb-sm">{{ $t('errors.unauthorizedTitle') }}</h3>
      <p class="text-subtitle1 text-grey-5 q-mb-xl">
        {{ $t('errors.unauthorizedDesc') }}
      </p>
      <q-btn color="primary" label="Go to Dashboard" size="lg" no-caps class="q-px-xl hover-scale" @click="goHome" />
    </div>
  </q-page>
</template>
