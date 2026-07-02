<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { gameApi } from '@/api/game'
import type { Game } from '@/types/game'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Check } from '@element-plus/icons-vue'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const saving = ref(false)
const formRef = ref()

const isNew = computed(() => route.params.id === 'new')
const gameId = computed(() => isNew.value ? null : Number(route.params.id))

const form = ref<Partial<Game>>({
  gameName: '',
  gameDirection: '',
  source: '',
  gitUrl: '',
  priority: 0,
  tags: '',
  projectType: '',
  manager: '',
  whiteBranch: '',
  status: 'active',
  retentionRecord: '',
  androidFolderName: '',
  remark: '',
})

const rules = {
  gameName: [
    { required: true, message: '请输入游戏名称', trigger: 'blur' },
  ],
  gameDirection: [
    { required: true, message: '请选择游戏方向', trigger: 'change' },
  ],
}

const directionOptions = [
  { label: '横屏', value: 'horizontal' },
  { label: '竖屏', value: 'vertical' },
]

const statusOptions = [
  { label: '进行中', value: 'active' },
  { label: '已完成', value: 'completed' },
  { label: '已暂停', value: 'paused' },
]

const fetchGameData = async () => {
  if (!gameId.value) return
  
  loading.value = true
  try {
    const response = await gameApi.getById(gameId.value)
    form.value = response.data
  } catch (error) {
    ElMessage.error('获取游戏信息失败')
  } finally {
    loading.value = false
  }
}

const handleSave = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      saving.value = true
      try {
        if (isNew.value) {
          await gameApi.create(form.value)
          ElMessage.success('创建成功')
        } else {
          await gameApi.update(gameId.value!, form.value)
          ElMessage.success('更新成功')
        }
        router.push({ name: 'games' })
      } catch (error) {
        ElMessage.error(isNew.value ? '创建失败' : '更新失败')
      } finally {
        saving.value = false
      }
    }
  })
}

const handleBack = () => {
  router.push({ name: 'games' })
}

onMounted(() => {
  if (!isNew.value) {
    fetchGameData()
  }
})
</script>

<template>
  <div class="game-detail-container">
    <el-card class="box-card" v-loading="loading">
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-button :icon="ArrowLeft" @click="handleBack">返回</el-button>
            <span>{{ isNew ? '新增游戏' : '编辑游戏' }}</span>
          </div>
          <el-button type="primary" :icon="Check" :loading="saving" @click="handleSave">
            保存
          </el-button>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="120px"
        class="game-form"
      >
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="游戏名称" prop="gameName">
              <el-input v-model="form.gameName" placeholder="请输入游戏名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="游戏方向" prop="gameDirection">
              <el-select v-model="form.gameDirection" placeholder="请选择游戏方向">
                <el-option
                  v-for="item in directionOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="来源">
              <el-input v-model="form.source" placeholder="请输入来源" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Git地址">
              <el-input v-model="form.gitUrl" placeholder="请输入Git地址" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="优先级">
              <el-input-number v-model="form.priority" :min="0" :max="100" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="标签">
              <el-input v-model="form.tags" placeholder="请输入标签，多个用逗号分隔" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="项目工程类型">
              <el-input v-model="form.projectType" placeholder="请输入项目工程类型" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="负责人">
              <el-input v-model="form.manager" placeholder="请输入负责人" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="白包分支">
              <el-input v-model="form.whiteBranch" placeholder="请输入白包分支" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态">
              <el-select v-model="form.status" placeholder="请选择状态">
                <el-option
                  v-for="item in statusOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="安卓文件夹名称">
              <el-input v-model="form.androidFolderName" placeholder="请输入安卓文件夹名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="游戏留存记录">
              <el-input v-model="form.retentionRecord" placeholder="请输入游戏留存记录" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注信息">
          <el-input
            v-model="form.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入备注信息"
          />
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<style scoped>
.game-detail-container {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 15px;
}

.header-left span {
  font-size: 16px;
  font-weight: bold;
}

.game-form {
  max-width: 1000px;
}
</style>