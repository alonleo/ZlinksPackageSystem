export interface SysJob {
  jobId?: number
  jobName: string
  jobGroup: string
  invokeTarget: string
  cronExpression: string
  misfirePolicy: string
  concurrent: string
  status: string
  createBy?: string
  createTime?: string
  updateBy?: string
  updateTime?: string
  remark?: string
}

export interface SysJobLog {
  jobLogId?: number
  jobName: string
  jobGroup: string
  invokeTarget: string
  cronExpression: string
  startTime: string
  endTime: string | null
  status: string
  jobMessage: string
  exceptionInfo: string | null
}

export interface JobQuery {
  current?: number
  size?: number
  jobName?: string
  jobGroup?: string
  status?: string
}