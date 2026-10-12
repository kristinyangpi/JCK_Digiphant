# 三人固定区域操作

打开 `Assets/DigiPhant/Scenes/DigiPhant.unity`。镜像摄像头画面从左到右：**P1 DRIVER / P2 EAT / WATER / P3 LOLLIPOP + FLAP**。当前场景保留粉色大象、金色耳环、原摄像机及 Savannah 课程。

## 开始

1. 按 Play，选择 Camera、3 people、Seated / upper body，启动摄像头预览。
2. P1 肩膀和双手位于左侧；P2 肩膀和脸位于中间；P3 肩膀位于右侧。按肩膀中心分配区域，P2 脸也必须位于中间。
3. P1 点击 Calibrate driver，双掌朝摄像头、双手低位且完整可见，保持 8 秒直到 Ready。不需要全身 neutral 校准。
4. P3 准备打印 `StudentTracking/lollipop.png`。识别的是这张参考图，不是任意棒棒糖或颜色卡。

区域边界有缓冲带；同一区域出现两个人时该区域暂停。角色属于区域，不是人脸身份。空区域不会阻塞其他角色。一个人也可保持 3 people 模式，依次站入各区检查对应输入；P3 功能仅在三人分区模式启用。

## P1：导航

- 双手一起抬高加速，降低减速；两手回到校准高度或以下停止。
- 左手向摄像头前伸左转，右手前伸右转。左右指自己的左右，转向相对大象朝向。
- 双掌朝摄像头前进；双手翻成手背朝摄像头后退。方向保持约 0.35 秒后换挡；一掌一背或识别不清时停车。
- 转向默认使用手掌画面大小变化估计前伸，并非真实深度测量。可关闭 Steering: apparent palm size 切回 Pose 深度，切换后重新校准。
- 追踪丢失立即停车，校准不限时保留。双手重新识别、朝向一致并稳定 0.5 秒后自动恢复，无需手动重设。
- 改人数、模式、Driver slot、重新分配、重启追踪或 Play，以及 Return to starting position，仍需重新校准。
- 步行设置为 2.4、倒车 1.2、转向 60 度/秒；手势最大输入为 0.6。木头可由 P2 张嘴吃掉；仍没有推木头或碰撞阻挡功能。

若身体 visible 但不能驾驶，查看左右 wrist、depth、palm、facing，以及 Size delta / Turn。身体可见不等于双手数据有效。

## P2：吃西瓜、喝水、喷水与吃木头

P1 开到喂食灌木或水盆附近的西瓜旁。P2 张嘴抓取，张大嘴保持约 0.3 秒吃下；闭嘴放下。持有西瓜时 P1 可继续驾驶，吃的动画期间停车，结束后恢复且保留驾驶校准。吃掉的西瓜本轮不再生，重新进入 Play 可重置。

P2 在中间区域正视镜头、保持头部稳定 2 秒，自动设置头部中立姿态。无需重设 P1 校准；普通追踪丢失后保留头部中立姿态，重启追踪会重新设置。

- **喝水：** P1 将大象头部开到水坑中心 3 单位以内，P2 低头至少 7 度、保持约 0.3 秒。动画完成后储存一次喷水用水。
- **喷水：** 先回到中立姿态约 0.3 秒，再抬头至少 10 度、保持约 0.3 秒。喷水消耗储水；没有水时只抬象鼻。水落到有效道路／平台时长花，水坑和空中不长花。
- **吃木头：** P1 朝向木头（约 60 度范围内），让静止象鼻尖靠到 2.5 单位以内。P2 张嘴达到约 40%、保持 0.3 秒，自动吃两口，木头缩短并出现木屑。本轮不再生，重进 Play 重置；附近可抓取或已持有的西瓜优先。

两次头部动作之间回正约 0.3 秒才能再次触发。喝水、喷水、吃木头期间暂时停车，完成后恢复且不清除 P1 校准。飞行中不能开始这些动作；P2 与 P3 共用象鼻互斥锁。吃木头不挪动导航根节点，已有点位不变。


西瓜防穿模修复（`291324e`）：地面上的西瓜会避开大象动画中的腿部和躯干；持有时与象鼻末端保持偏移，放下时重新检查安全位置。导航手势、速度和转向设置未改变。

## P3：棒棒糖、奖励和飞行

1. P1 驶近灌木或水盆旁的棒棒糖，确保在象鼻可达范围。若显示 Walk closer，继续靠近。
2. P3 把完整参考卡放在镜像右侧区域内，保持清晰、平稳约半秒；肩膀中心也需留在 P3 区域。卡片伸入 P1/P2 区不会用于识别。
3. 大象伸鼻、卷向嘴边并吃下糖。第一次吃下后出现生日帽；第二次吃下后耳朵变大，并解锁飞行。
4. 两次触发之间移开卡片至少约 0.8 秒，等动作结束、驶近下一颗糖，再展示卡片。
5. 解锁后，P3 双臂向两侧伸展，同时上下挥动。静止展开或只动一只手不会触发持续飞行。肩膀、肘部和手腕尽量完整出镜。
6. 飞行仅垂直升降，最高距起始地面 2.2 单位。停止挥臂或失去追踪后缓慢落地；落地并收回展开的双臂后，P1 可继续驾驶。

P2 持瓜或吃瓜时，P3 不能抢用象鼻；P3 吃糖期间也不接受 P2 抓取。吃糖及飞行暂停水平移动，但不清除 P1 校准。飞行期间不能开始新的抓取。漂浮路线保留原交互点位和地面高度；终点新增彩带与彩纸庆祝，没有导入完整舞蹈；喝水、喷水和吃木头现由 P2 控制。

## 漂浮糖果路线与终点庆祝

原路线换为糖果色路面、云岛平台和桥式支撑；原大地面、Stage 和网格隐藏。西瓜、棒棒糖、水坑、木头及终点的原有交互位置和地面高度不变，P1/P2/P3 操作不变。周围保留 Yangpi 的城堡、云、瀑布和气球装饰。

沿原路线从来路方向穿过终点彩带，会出现一次彩纸庆祝和 YOU FINISHED!。可以步行通过，无需飞行；庆祝不会接管操作、锁定移动或清除 P1 校准。离开起点超过 6 单位后，回到起点 2 单位内会自动重置庆祝，便于重跑。未导入完整终点舞蹈。

漂浮外观没有新增边缘限制或坠落物理，仍使用原有导航范围和地面高度；大象可能走出可见道路。木头可绕行，也可靠近后由 P2 张嘴吃掉。

美术来自 Yangpi 的 Candy Wonderland；终点 `CourseFinishCelebration` 参考其 `RibbonFinish` 的彩带与彩纸思路，改为现有路线的定向步行触发。场景和 `FloatingCourse` 资源已保存，安装及润色工具仅用于本地维护，不随仓库共享，也不应重复运行。

改动前备份：`Logs/DigiPhant-before-floating-route-20261011-094319.unity`。预览：`Logs/FloatingCourse-game.png`、`Logs/FloatingCourse-overview.png`、`Logs/FloatingCourse-finish.png`。Logs 文件仅保存在本地。

## 验证与来源

本次 P2 完整交互：61 项 Python 测试通过；`Logs/P2CourseActions.log` 以退出码 0 完成，包含 `P2_COURSE_ALL_VALIDATION_OK`，覆盖喝水、喷水、木头及既有西瓜／P3／恢复检查。尚未完成真人三人全程验证。修改前备份：`Logs/DigiPhant-before-P2-course-actions-20261011-145015.unity`。

此前漂浮路线检查：`Logs/FloatingCoursePolish.log` 包含 `FLOATING_POLISH_AND_FINISH_RESET_OK`、`WATERMELON_ALL_CHECKS_PASSED` 和 `FLOATING_FINAL_ALL_CHECKS_PASSED`，Unity 退出码为 0。覆盖终点方向／范围、快速穿越、返回起点后再次庆祝及已有交互回归。以下背景记录是此前版本的验证历史；尚未完成真人三人全程测试。

起点可见性修复已通过：`Logs/BackdropVisibilityVerified.log` 包含 `BACKDROP_VISIBILITY_OK`，Unity 退出码为 0。仅调整城堡和三朵云的位置，原路线、控制设置及摄像机变换保持不变。修复前备份：`Logs/DigiPhant-before-visible-backdrop-20261010-215419.unity`。可见性维护工具仅保留在本地，不随仓库共享。

背景最终检查已通过：`Logs/CandyBackdropVerified.log` 退出码为 0，包含 `CANDY_BACKDROP_VALIDATION_OK`、`WATERMELON_ALL_CHECKS_PASSED` 和 `CANDY_BACKDROP_ALL_CHECKS_PASSED`。确认原路线网格、已有物体变换和控制配置不变，新增背景只含装饰，并通过西瓜及 P1/P2/P3 回归检查。这不代表已完成真人摄像头测试。

西瓜修复验证已通过：`Logs/WatermelonCollisionValidation.log` 包含 `WATERMELON_ALL_CHECKS_PASSED`，Unity 退出码为 0。检查覆盖动画腿部／躯干避让、持有间距、放下，以及 P1、P2、P3 功能回归；仍需在实际摄像头操作中确认视觉效果。

52 项 Python 测试已通过。Unity 完整检查通过，`Logs/P3AllValidationRetry.log` 退出码为 0，包含 `P3_VALIDATION_OK`、`P2_EATING_VALIDATION_PASSED`、`AUTO_RECOVERY_VALIDATION_OK` 和 `P3_ALL_VALIDATION_OK`。额外的实际移动计算检查确认：飞行抬升移动根节点、摄像机跟随、P1 校准保留、丢失追踪后落地，滑块模式忽略飞行输入。尚未完成真人三人摄像头测试，现场仍需验证卡片清晰度、区域遮挡、挥臂空间和恢复表现。

场景备份：`Logs/DigiPhant-before-P3-20261009-175954.unity`；此前 P2 备份：`Logs/DigiPhant-before-P2-eating-20261009-105411.unity`。Logs 是本地备份，不随 Git 上传。

P2 来源：celenashen04-boop/JCK_digiphant 的 digiphant-integration 分支。P3 来源：kristinyangpi/JCK_Digiphant 的 main，提交 `66065793cfe581f3648e31c7e16ac499ad48d623`。识别使用同一摄像头的 P3 区域和 OpenCV 参考图匹配；不需要另外启动摄像头程序或 TensorFlow 检测器。

定制追踪代码、模型及安装说明在 `StudentTracking`。Unity 优先使用其中的 Python 环境，也兼容现有 starter 虚拟环境。自动检查可从 DigiPhant 菜单运行；不要把逻辑测试通过等同于真人交互已验证。


最终 P2 验证：Logs/P2FinalValidation.log，P2_HEAD_INPUT_VALIDATION_OK、P2_COURSE_VISUAL_CHECKS_OK、P2_FINAL_VALIDATION_OK，退出码 0。包含默认距离下的木头交互及原有交互回归。预览 P2-drink.png、P2-spray.png、P2-log-midbite.png 位于 Logs；尚未验证真人三人同时操作。
