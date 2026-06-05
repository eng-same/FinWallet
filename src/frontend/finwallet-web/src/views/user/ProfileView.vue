<script setup lang="ts">
import { useAuthStore } from '../../stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const formatDate = (dateStr: string) => {
  if (!dateStr) return '';
  return new Date(dateStr).toLocaleString();
};
</script>

<template>
  <q-page class="q-pa-lg flex flex-center">
    <div class="w-full max-w-md">
      
      <q-btn
        flat
        dense
        color="grey-5"
        icon="arrow_back"
        label="Back to Dashboard"
        no-caps
        class="q-mb-md"
        @click="router.push('/dashboard')"
      />

      <q-card class="glass-card q-pa-lg text-center">
        <q-card-section>
          
          <q-avatar size="80px" color="indigo-10" class="q-mb-md shadow-5">
            <q-icon name="person" size="40px" color="white" />
          </q-avatar>
          
          <h5 class="text-h5 text-white text-weight-bold q-my-none">
            {{ authStore.user?.fullName }}
          </h5>
          <div class="text-caption text-grey-5 font-mono q-mt-xs">{{ authStore.user?.email }}</div>
          
          <q-separator class="bg-grey-9 q-my-lg" />

          <div class="text-left text-body2 q-gutter-y-md">
            
            <div class="row justify-between">
              <span class="text-grey-5">{{ $t('profile.userId') }}</span>
              <span class="font-mono text-white text-caption">{{ authStore.user?.id }}</span>
            </div>

            <div class="row justify-between">
              <span class="text-grey-5">{{ $t('profile.phoneNumber') }}</span>
              <span class="text-white text-weight-bold">{{ authStore.user?.phoneNumber }}</span>
            </div>

            <div class="row justify-between">
              <span class="text-grey-5">{{ $t('profile.accessRole') }}</span>
              <q-badge color="secondary" text-color="white" class="text-bold">
                {{ authStore.role }}
              </q-badge>
            </div>

            <div class="row justify-between">
              <span class="text-grey-5">{{ $t('profile.accountCreated') }}</span>
              <span class="text-white text-weight-bold">{{ formatDate(authStore.user?.createdAt || '') }}</span>
            </div>

            <div class="row justify-between" v-if="authStore.user?.lastLoginAt">
              <span class="text-grey-5">{{ $t('profile.lastSignIn') }}</span>
              <span class="text-white text-weight-bold">{{ formatDate(authStore.user?.lastLoginAt || '') }}</span>
            </div>

          </div>

        </q-card-section>
      </q-card>

    </div>
  </q-page>
</template>

<style scoped>
.w-full {
  width: 100%;
}
.max-w-md {
  max-width: 440px;
}
</style>
