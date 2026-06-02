<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <q-page class="flex flex-center text-center q-pa-lg">
    <div>
      <q-icon name="error_outline" size="120px" color="warning" class="q-mb-md" />
      <h3 class="text-h3 text-white text-weight-bolder q-mt-none q-mb-sm">Server Error</h3>
      <p class="text-subtitle1 text-grey-5 q-mb-xl">
        Something went wrong on the server. Please try again later.
      </p>
      <q-btn color="primary" label="Try Again" size="lg" no-caps class="q-px-xl hover-scale" @click="router.push('/')" />
    </div>
  </q-page>
</template>
