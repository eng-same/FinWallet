<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <q-page class="flex flex-center text-center q-pa-lg">
    <div>
      <q-icon name="explore_off" size="120px" color="primary" class="q-mb-md" />
      <h3 class="text-h3 text-white text-weight-bolder q-mt-none q-mb-sm">Page Not Found</h3>
      <p class="text-subtitle1 text-grey-5 q-mb-xl">
        The page you are looking for does not exist or has been moved.
      </p>
      <q-btn color="primary" label="Back to Safety" size="lg" no-caps class="q-px-xl hover-scale" @click="router.push('/')" />
    </div>
  </q-page>
</template>
