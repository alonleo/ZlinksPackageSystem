<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { companyApi } from '@/api/company'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Check } from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const saving = ref(false)
const formRef = ref()

const isNew = computed(() => route.params.id === 'new')
const companyId = computed(() => isNew.value ? null : Number(route.params.id))

const form = ref({
  companyName: '',
  platform: '',
  account: '',
  password: '',
  remark: '',
})

const rules = {
  companyName: [{ required: true, message: '请输入公司名称', trigger: 'blur' }],
}

const fetchData = async () => {
  if (!companyId.value) return
  loading.value = true
  try {
    const response = await companyApi.getById(companyId.value)
    const c = response.data
    form.value = {
      companyName: c.companyName || '',
      platform: c.platform || '',
      account: c.account || '',
      password: c.password || '',
      remark: c.remark || '',
    }
  } catch (error) {
    ElMessage.error('获取公司信息失败')
  } finally {
    loading.value = false
  }
}

const getErrorMessage = (error: any): string => {
  return error?.response?.data?.message || error?.message || '操作失败'
}

const handleSave = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      saving.value = true
      try {
        if (isNew.value) {
          await companyApi.create(form.value)
          ElMessage.success('创建成功')
        } else {
          await companyApi.update(companyId.value!, form.value)
          ElMessage.success('更新成功')
        }
        router.push({ name: 'companies' })
      } catch (error: any) {
        ElMessage.error(getErrorMessage(error))
      } finally {
        saving.value = false
      }
    }
  })
}

const handleBack = () => router.push({ name: 'companies' })

onMounted(() => {
  if (!isNew.value) fetchData()
})
</script>

<template>
  <div class="company-detail-container">
    <el-card class="box-card" v-loading="loading">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-button :icon="ArrowLeft" @click="handleBack">返回</el-button>
            <span>{{ isNew ? '新增公司' : '编辑公司' }}</span>
          </div>
          <el-button type="primary" :icon="Check" :loading="saving" @click="handleSave">保存</el-button>
        </div>
      </template>

      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" class="company-form">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="公司名称" prop="companyName">
              <el-input v-model="form.companyName" placeholder="请输入公司名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="平台">
              <el-input v-model="form.platform" placeholder="如 荣耀/VIVO/华为" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="账号">
              <el-input v-model="form.account" placeholder="请输入账号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="密码">
              <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注信息">
          <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<style scoped>
.company-detail-container { padding: 20px; }
.card-header { display: flex; justify-content: space-between; align-items: center; }
.header-left { display: flex; align-items: center; gap: 15px; }
.header-left span { font-size: 16px; font-weight: bold; }
.company-form { max-width: 800px; }
</style>
