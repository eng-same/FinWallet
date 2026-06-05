<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAdminStore } from '../../stores/admin';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const adminStore = useAdminStore();
const router = useRouter();
const $q = useQuasar();

const searchVal = ref('');

const loadUsers = async () => {
  try {
    await adminStore.fetchUsers(searchVal.value || undefined);
  } catch (error: any) {
    const errorMsg = error.errors?.[0]?.message || error.message || 'Failed to load users';
    $q.notify({ type: 'negative', message: errorMsg });
  }
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString();
};

onMounted(() => {
  loadUsers();
});
</script>

<template>
  <q-page class="q-pa-lg">
    
    <div class="row items-center justify-between q-mb-xl">
      <div>
        <h4 class="text-h4 text-white text-weight-bolder q-mt-none q-mb-xs">{{ $t('adminUsers.title') }}</h4>
        <p class="text-subtitle1 text-grey-5 text-weight-light">{{ $t('adminUsers.subtitle') }}</p>
      </div>
      <q-btn flat round color="white" icon="refresh" @click="loadUsers" />
    </div>

    <!-- Search Input -->
    <q-card class="glass-card q-mb-xl q-pa-md">
      <div class="row items-center">
        <div class="col-12 col-sm-6">
          <q-input
            v-model="searchVal"
            :label="$t('adminUsers.search')"
            label-color="indigo-3"
            dark
            outlined
            dense
            debounce="500"
            @update:model-value="loadUsers"
          >
            <template v-slot:prepend>
              <q-icon name="search" color="indigo-4" />
            </template>
          </q-input>
        </div>
      </div>
    </q-card>

    <!-- Users Grid -->
    <div class="row q-col-gutter-lg" v-if="!adminStore.loading">
      <div class="col-12 col-sm-6 col-md-4" v-for="user in adminStore.users" :key="user.id">
        <q-card class="glass-card hover-scale cursor-pointer" @click="router.push(`/admin/users/${user.id}`)">
          <q-card-section class="q-pa-md text-center">
            
            <q-avatar size="64px" color="indigo-10" class="q-mb-sm">
              <q-icon name="person" color="white" />
            </q-avatar>

            <div class="text-subtitle1 text-weight-bold text-white leading-tight">
              {{ user.fullName }}
            </div>
            
            <div class="text-caption text-grey-5 font-mono q-mb-md">
              {{ user.email }}
            </div>

            <q-badge :color="user.isActive ? 'secondary' : 'negative'" class="text-bold text-capitalize q-mb-md">
              {{ user.isActive ? 'Active User' : 'Deactivated' }}
            </q-badge>

            <q-separator class="bg-grey-9 q-my-sm" />

            <div class="text-left text-caption text-grey-5">
              <div>{{ $t('common.phone') }}: <span class="text-grey-3 text-weight-bold">{{ user.phoneNumber }}</span></div>
              <div>{{ $t('adminUsers.registered') }}: <span class="text-grey-3">{{ formatDate(user.createdAt) }}</span></div>
            </div>

          </q-card-section>
        </q-card>
      </div>

      <!-- No users state -->
      <div class="col-12 text-center q-pa-xl text-grey-5" v-if="adminStore.users.length === 0">
        <q-icon name="group_off" size="64px" class="q-mb-md" />
        <div>{{ $t('adminUsers.noUsers') }}</div>
      </div>
    </div>

    <!-- Loader -->
    <div class="text-center q-pa-xl" v-else>
      <q-spinner color="primary" size="48px" />
      <div class="text-grey-5 q-mt-md">{{ $t('adminUsers.loading') }}</div>
    </div>

  </q-page>
</template>

<style scoped>
.cursor-pointer {
  cursor: pointer;
}
.leading-tight {
  line-height: 1.25;
}
</style>
