# ISO 11820 不燃性试验系统

基于 **.NET 8 WinForms** 的 ISO 11820 建筑材料不燃性试验测控系统，实现炉温控制、数据采集、曲线实时显示、报告生成全流程自动化。

## 功能特性

- **用户认证** — 管理员 / 试验员双角色登录
- **五通道温度采集** — 炉温 1 (TF1)、炉温 2 (TF2)、表面温 (TS)、中心温 (TC)、校准温 (TCal)
- **五状态机控制** — 空闲 → 升温 → 就绪 → 记录中 → 完成
- **实时温度曲线** — 多通道折线图，支持鼠标拖拽平移、滚轮缩放、右键框选放大
- **悬停提示** — 鼠标靠近曲线即显示曲线名、时间、温度
- **曲线显隐切换** — 左侧复选框独立控制每条曲线
- **温漂自动计算** — MathNet.Numerics 线性回归，实时评估炉温稳定性
- **试验记录查询** — 按样品编号、操作员、日期范围筛选，支持删除
- **报告导出** — Excel (EPPlus)、CSV、PDF (PDFsharp + OxyPlot 嵌入式温度曲线图)
- **自适应布局** — TableLayoutPanel 百分比布局，窗口缩放自动适配

## 技术栈

| 分类 | 技术 |
|---|---|
| 运行时 | .NET 8 (Windows Forms) |
| 数据库 | SQLite (Microsoft.Data.Sqlite) |
| 图表 | OxyPlot 2.1 |
| Excel | EPPlus 7.1 |
| PDF | PDFsharp-MigraDoc 6.1 |
| 数值计算 | MathNet.Numerics 5.0 |
| 日志 | Serilog 4.0 |

## 项目结构

```
ISO/
├── Program.cs                  # 入口，初始化配置/日志/数据库
├── appsettings.json            # 数据库路径、仿真参数、报告配置
├── ISO11820.csproj             # 项目文件
│
├── Core/
│   └── TestController.cs       # 五状态机：Idle→Preparing→Ready→Recording→Complete
│
├── Data/
│   ├── DbInitializer.cs        # 建表 + 种子数据
│   └── DbHelper.cs             # 数据库 CRUD（试验记录、产品、操作员、校准）
│
├── Forms/
│   ├── LoginForm.cs            # 登录窗口
│   ├── MainForm.cs             # 主界面：温度面板、曲线图、按钮、日志、查询、校准
│   ├── NewTestForm.cs          # 新建试验对话框
│   ├── TestRecordForm.cs       # 试验记录保存对话框
│   └── SettingsForm.cs         # 系统设置
│
├── Global/
│   └── AppContext.cs           # 全局上下文：DI 容器、服务实例
│
├── Helpers/
│   └── ThemeColors.cs          # 亮色主题色板
│
├── Models/
│   ├── TestMaster.cs           # 试验主记录实体
│   ├── TemperatureData.cs      # 温度数据实体
│   ├── ProductMaster.cs        # 产品/样品实体
│   ├── Operator.cs             # 操作员实体
│   ├── Apparatus.cs            # 设备信息实体
│   ├── CalibrationRecord.cs    # 校准记录实体
│   ├── SimulationConfig.cs     # 仿真参数配置
│   └── ...
│
└── Services/
    ├── DaqWorker.cs            # 数据采集定时器（800ms 周期），秒检测 + 数据广播
    ├── SensorSimulator.cs      # 五通道温度仿真（升温/稳定/降温 三阶段）
    ├── DriftCalculator.cs      # 温漂计算：线性回归斜率 °C/10min
    └── ExportService.cs        # 报告导出：CSV / Excel / PDF
```

## 数据库

SQLite 数据库 `Data/ISO11820.db`，6 张表：

| 表名 | 说明 |
|---|---|
| `operators` | 操作员（admin / experimenter） |
| `productmaster` | 样品信息 |
| `testmaster` | 试验主记录（温升、失重率、时长等） |
| `temperaturedata` | 温度逐秒数据 |
| `apparatus` | 设备信息 |
| `sensors` | 传感器配置 |
| `CalibrationRecords` | 校准记录 |

## 状态机流程

```
Idle ──[开始升温]──→ Preparing ──[温度稳定]──→ Ready
  ↑                      │                         │
  │                      ↓                         │
  └──[停止升温]───  ←──  ┘         ┌──[开始记录]───┘
                                    │
                                    ↓
                              Recording ──[手动/自动停止]──→ Complete
```

- **Preparing**：炉温从室温升至 750±3°C，计算温漂，达标后自动切 Ready
- **Ready**：等待用户点击"开始记录"
- **Recording**：计时归零，逐秒采集温度；满足终止条件或到达目标时长自动结束
- **Complete**：填写试验后数据，保存入库并导出报告

## 快速开始

### 环境要求
- .NET 8 SDK
- Windows 10/11

### 运行

```bash
cd ISO
dotnet run
```

或在 VS Code 中按 **F5** 启动调试。

### 登录

| 用户名 | 密码 | 角色 |
|---|---|---|
| admin | 123456 | 管理员 |
| experimenter | 123456 | 试验员 |

### 典型操作流程

1. 登录 → 2. 新建试验（填写样品信息）→ 3. 自动升温 → 4. 就绪后点击"开始记录" → 5. 记录完成 → 6. 填写试验后数据 → 7. 保存 → 8. 自动导出报告并跳转查询

## 配置

`appsettings.json` 关键配置项：

```jsonc
{
  "Simulation": {
    "EnableSimulation": true,       // 仿真模式（无需硬件）
    "InitialFurnaceTemp": 25.0,     // 初始炉温 °C
    "TargetFurnaceTemp": 750.0,     // 目标炉温 °C
    "HeatingRatePerSecond": 20.0    // 升温速率 °C/s
  },
  "Report": {
    "EnablePdfExport": true         // 是否生成 PDF 报告
  }
}
```

## 图表交互

| 操作 | 效果 |
|---|---|
| 鼠标左键拖拽 | 平移图表 |
| 鼠标滚轮 | 缩放 |
| 鼠标右键框选 | 矩形放大 |
| 鼠标悬停曲线 | 显示曲线名 + 时间 + 温度 |
| 左侧复选框 | 切换曲线显隐 |
