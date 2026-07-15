export interface CpuInfo { cpuNum: number; total: number; used: number; sys: number; free: number }
export interface MemInfo { total: number; used: number; free: number; usage: number }
export interface JvmInfo { total: number; max: number; used: number; free: number; usage: number; name: string; version: string; vendor?: string; startTime: number; runTime: number; home: string; inputArgs: string; nonheapTotal?: number; nonheapUsed?: number; nonheapMax?: number }
export interface SysInfo { computerName: string; computerIp: string; userName?: string; osName: string; osArch: string; osVersion?: string; userDir: string; userHome?: string }
export interface SysFileInfo { dirName: string; sysTypeName: string; typeName: string; total: number; free: number; used: number; usage: number }
export interface ServerInfo { cpu: CpuInfo; mem: MemInfo; jvm: JvmInfo; sys: SysInfo; sysFiles: SysFileInfo[] }