# 三人固定区域操作（当前版本）
镜像摄像头画面从左到右：**P1 DRIVER / P2 EAT / P3 UNUSED**。

1. 进入 `Assets/DigiPhant/Scenes/DigiPhant.unity`，按 Play，选择 Camera、3 people、Seated / upper body。
2. P1肩膀和双手留在左侧区域；P2肩膀和脸在中间区域；P3在右侧，暂不驱动任何动作。分区按肩膀中心判定，P2嘴部还需位于中间区域。
3. 仅P1点击 Calibrate driver，8秒内保持双掌朝摄像头、双手低位且完整可见。不用全身neutral校准。
4. P1继续使用抬手控制速度、左右手向镜头前伸转向、掌心/手背切换前进后退。丢失后稳定0.5秒自动恢复。
5. P1将象鼻带到西瓜附近（水平距离3.5单位内）。P2张嘴抓取，张大嘴保持约0.3秒吃下；闭嘴放下。西瓜位于喂食灌木及水盆附近。
6. 进食动画期间自动停车，结束后恢复P1驾驶。吃掉的西瓜本轮不再生，重新进入Play可重置。

区域边界设有缓冲带；同一区域有两个人时该区域暂停。角色属于区域，不是人脸身份：不要互换区域。P2单独露脸、肩膀不在画面中时不会分配角色。仅一个人测试也可保持3人模式：站左侧试驾驶、站中间试P2嘴部输入，空区域不会阻塞其余角色。

来源：celenashen04-boop/JCK_digiphant 的 digiphant-integration 分支，移植 Watermelon/ClipOverlay 并适配独立P2输入；未导入喝水、喷水或原项目设置。
验证：41项Python测试通过、Face Landmarker加载通过、Unity P2_EATING_VALIDATION_PASSED。场景备份 `Logs/DigiPhant-before-P2-eating-20261009-105411.unity`。真人三人测试尚未完成。

---
下面保留此前导航技术说明；以上角色分区说明优先。

# 单人驾驶：三人出镜

更新：2026-10-08。场景：Assets/DigiPhant/Scenes/DigiPhant.unity。
备份：Assets/DigiPhant/Scenes/DigiPhant_BeforeDriverNavigation.unity。

## 开始
1. Unity 按 Play，选择 3 人、Seated / upper body、Camera。
2. 三人并排，保持肩膀和双手可见，等待预览 P1/P2/P3 标签。不要交叉换位。
3. 勾选 Enable locomotion 和 Single-driver navigation。Driver slot 选择自己的 P 标签，默认 P1（镜像预览中通常显示在最右侧，请以 P 标签为准）。
4. 若其他人的身体动作也需要校准，先做 Set neutral pose (10 seconds)。再校准驾驶员。
5. 点击 Calibrate driver (8 seconds)。双手放在较低但摄像头清楚可见的位置，肘部自然弯曲，两只手掌心朝摄像头、手指张开、前后距离接近；保持到 Ready。

## 操作
- 双手一起抬高：加速；降低：减速；双手回到校准时高度或以下：立即停止移动和转向。
- 左手相对校准位置向摄像头前伸：左转；右手前伸：右转。另一手保持原位置。左右是自己的左右，转向是大象的朝向，不是屏幕边缘。
- 双手掌心朝摄像头：前进；两只手同时翻成手背朝摄像头：后退。
- 换挡会先停车，保持朝向约 0.35 秒后才移动。一掌一背、手侧对摄像头或手部识别不清时停止。
- 原地转向：一只手保持低位，让速度为零，另一只手稍抬起；用前后距离差转向。两个手都要清晰可见且朝向一致。
- 默认最高前进速度约 2.22 单位/秒，最高倒车速度约 0.72 单位/秒。Inspector 中 Maximum Forward/Maximum Reverse 可以调节。
- 木头按用户选择绕行，不添加碰撞、推动、跳跃或自动通关。移动半径从 15 调到 30，可覆盖完整课程。

## 停车和恢复
追踪丢失立即停车，并在当前运行期间保留校准，不再超时清除。双手重新识别且朝向一致、稳定0.5秒后自动恢复，无需点按钮或放低双手。单人模式可重新接回画面内唯一的人；三人仍按P位置匹配，不要交叉换位。
改变人数、模式、Driver slot，重新分配人员或重启追踪会清除相关校准。Return to starting position 也需要重新校准。
保持固定站位：现有人员跟踪依据位置，不是人脸身份识别，不能保证交叉换位后仍识别原驾驶员。

## 实现与验证
沿用已有 MediaPipe Pose；新增官方 MediaPipe Hand Landmarker 模型：DigiPhantStarter/Tracking/hand_landmarker.task。
来源：https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task
手腕前后距离来自 Pose 世界关键点，相对各自肩膀、按肩宽归一化；手掌朝向由手部关键点平面法线估计，并在驾驶员校准时确定掌心方向。
手部匹配会考虑所有检测到的人，拒绝竞争接近的匹配和重复归属。导航只读选定驾驶员；其他角色的身体控制映射保留。
Python Tracking 测试 19/19 通过；现有 .venv 的 MediaPipe 0.10.35 可加载新手部模型。
Unity 编译及 DRIVER_NAVIGATION_VALIDATION_OK 通过，日志 Logs/DriverNavigationValidation.log。
验证覆盖：左右深度转向、前后换挡等待及停车、P2/P3 输入隔离、双手放下停止、驾驶员丢失、缺少手数据、身份重置、全身校准重置、旧滑块模式。
首次安装测试因测试用 JSON 将缺失数组写为空数组而失败，测试现已改成真实旧版数据的省略字段格式，重测通过。
尚未完成三人真人摄像头测试；单目深度和掌心方向都是估计值，需实测灵敏度、光照、遮挡和帧率。

## 镜像与追踪诊断更新
摄像头预览默认镜像，骨架同步翻转而文字保持可读；原始推理、身体左右与驾驶控制不翻转。Tracking 行显示接受帧数、包龄和具体拒收原因。启动同步上半身模式，有限置信度规范到0–1；保留0.5秒超时与导航停车逻辑。20项Python测试通过。摄像头短测检测到1人，单帧0.05–0.10秒，当时未检测到手；绿色骨架但Unity lost的原始情况尚未重现。

## 驾驶员校准诊断更新
驾驶员校准时间从 3 秒延长到 8 秒。单人测试请选择 1 person、Camera 和 Driver P1；如需 Set neutral pose，等它结束后再做驾驶员校准。身体显示 visible 只说明身体追踪有效，不表示双手和驾驶员校准已经成功。

校准失败原因现在会持续保留，不会立即被通用的“先校准”提示覆盖。查看每只手的实时诊断：wrist（手腕）、depth（前后深度）、palm（手部识别置信度）和 facing（掌心朝向）。双手完整出镜、手指展开、掌心朝摄像头，放在较低但清楚可见的位置，点击 Calibrate driver 后保持 8 秒；以 Ready 状态确认成功。若失败，记录持续显示的原因及左右手诊断数值，再调整对应手的位置或朝向。

目前尚未确定实际摄像头校准失败的根因。这次未降低识别安全阈值，仍保留缺少可靠手部数据时停车的规则。此轮改动已通过代码审阅、Unity 编译及 CALIBRATION_DIAGNOSTICS_SMOKE_OK 检查；日志为 Logs/CalibrationDiagnosticsSmoke.txt。检查覆盖 8 秒校准、校准完成当帧不移动、失败提示保留及每只手的数据门槛，未修改场景。真人摄像头校准仍需复测。
## 2026-10-08 导航更新
- 默认使用手掌画面大小变化估计前伸：左手靠近镜头变大向左，右手变大向右。保持手掌平整；这是视觉代理信号，不是真实距离测量。可关闭“Steering: apparent palm size”切回原Pose深度，切换后重新校准。
- 面板显示实际Depth delta、Size delta、Turn，以及每只手的数值/置信度。
- 自动恢复：追踪丢失后不限时保留校准，信号稳定0.5秒后自动继续，不要求放低双手。重新分配P编号或重启追踪仍需要校准。
- 步行速度1.8→2.4；转弯和倒车速度不变。
- 定制追踪代码和模型在StudentTracking，Unity优先使用其中的Python环境，也兼容已有starter虚拟环境。
- 24项Python测试通过；Unity RECOVERY_STEERING_CHECK_OK验证大小转向、倒车、20秒保留、恢复确认、超时及epoch重置。真人三人操作仍需复测。

## 自动恢复验证
26项Python测试通过。Unity AUTO_RECOVERY_VALIDATION_OK覆盖：丢失即时停止、超过一分钟仍保留校准、抬手状态下自动恢复、稳定计时被中断时重置、手掌朝向不一致不恢复、恢复后换挡等待、显式身份重置失效。仅开场需要校准；改变P编号、重启追踪或停止/重新开始Play仍会清除校准。
单人模式检测到多个人时暂停，避免选择旁人；三人模式仍是位置匹配，不是人脸身份识别，不能保证交叉换位或换人后自动正确识别。
手动运行逻辑测试：DigiPhant > Validate Automatic Recovery。测试不修改场景。真人摄像头测试仍需确认。
