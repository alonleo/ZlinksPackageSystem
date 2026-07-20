<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { authApi } from '@/api/auth'
import { useUserStore } from '@/stores/user'

interface Props {
  modelValue: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'updated'): void
}>()

const userStore = useUserStore()

const formRef = ref()
const currentPassword = ref('')
const newUsername = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const verified = ref(false)
const verifying = ref(false)
const saving = ref(false)

const rules = computed(() => ({
  currentPassword: [
    { required: true, message: '请输入当前密码', trigger: 'blur' },
  ],
  newPassword: [
    {
      validator: (_: any, value: string, cb: (err?: Error) => void) => {
        if (value && value.length < 6) {
          cb(new Error('新密码至少 6 位'))
        } else {
          cb()
        }
      },
      trigger: 'blur',
    },
  ],
  confirmPassword: [
    {
      validator: (_: any, value: string, cb: (err?: Error) => void) => {
        if (newPassword.value && value !== newPassword.value) {
          cb(new Error('两次输入的密码不一致'))
        } else {
          cb()
        }
      },
      trigger: 'blur',
    },
  ],
}))

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v),
})

watch(visible, (v) => {
  if (!v) {
    resetForm()
  } else {
    newUsername.value = userStore.currentUser?.username || ''
  }
})

const resetForm = () => {
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  verified.value = false
  formRef.value?.clearValidate()
}

const handleVerify = async () => {
  if (!currentPassword.value) {
    ElMessage.warning('请输入当前密码')
    return
  }
  verifying.value = true
  try {
    await authApi.changePassword({
      oldPassword: currentPassword.value,
    })
    verified.value = true
    ElMessage.success('密码验证成功,请修改账号信息')
  } catch (e: any) {
    ElMessage.error(e?.message || '当前密码错误')
    verified.value = false
  } finally {
    verifying.value = false
  }
}

const handleSubmit = async () => {
  await formRef.value?.validate()
  if (!verified.value) {
    ElMessage.warning('请先验证当前密码')
    return
  }
  if (!newPassword.value && newUsername.value === userStore.currentUser?.username) {
    ElMessage.warning('未做任何修改')
    return
  }
  saving.value = true
  try {
    await authApi.changePassword({
      oldPassword: currentPassword.value,
      newPassword: newPassword.value || undefined,
      newUsername: newUsername.value !== userStore.currentUser?.username ? newUsername.value : undefined,
    })
    ElMessage.success('账号信息修改成功')
    await userStore.fetchUserInfo()
    emit('updated')
    visible.value = false
  } catch (e: any) {
    ElMessage.error(e?.message || '修改失败')
  } finally {
    saving.value = false
  }
}

const handleCancel = () => {
  visible.value = false
}
</script>

<template>
  <el-dialog
    v-model="visible"
    title="修改账号信息"
    width="480px"
    :close-on-click-modal="false"
    destroy-on-close
  >
    <el-form
      ref="formRef"
      :model="{ currentPassword, newUsername, newPassword, confirmPassword }"
      :rules="rules"
      label-width="100px"
      label-position="right"
    >
      <el-form-item label="当前密码" prop="currentPassword">
        <el-input
          v-model="currentPassword"
          type="password"
          show-password
          placeholder="请输入当前密码"
          :disabled="verified"
        >
          <template #append>
            <el-button
              :loading="verifying"
              :disabled="verified || !currentPassword"
              @click="handleVerify"
            >
              {{ verified ? '已验证' : '验证密码' }}
            </el-button>
          </template>
        </el-input>
      </el-form-item>

      <el-form-item label="用户名" prop="newUsername">
        <el-input
          v-model="newUsername"
          placeholder="请输入新用户名(留空则不修改)"
          :disabled="!verified"
        />
      </el-form-item>

      <el-form-item label="新密码" prop="newPassword">
        <el-input
          v-model="newPassword"
          type="password"
          show-password
          placeholder="留空则不修改密码"
          :disabled="!verified"
        />
      </el-form-item>

      <el-form-item label="确认密码" prop="confirmPassword">
        <el-input
          v-model="confirmPassword"
          type="password"
          show-password
          placeholder="再次输入新密码"
          :disabled="!verified"
        />
      </el-form-item>

      <el-alert
        v-if="!verified"
        type="info"
        :closable="false"
        show-icon
        title="请先验证当前密码,验证通过后才能修改账号信息"
      />
    </el-form>
    <template #footer>
      <el-button @click="handleCancel">取消</el-button>
      <el-button
        type="primary"
        :loading="saving"
        :disabled="!verified"
        @click="handleSubmit"
      >
        保存修改
      </el-button>
    </template>
  </el-dialog>
</template>