using UnityEngine;

namespace DigiPhant
{
    // Opt-in navigation; body controls retain their own calibration and mappings.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DigiPhantController))]
    public class DigiPhantDriverNavigation : MonoBehaviour
    {
        public bool enableDriverNavigation;
        public bool usePalmSizeSteering = true;
        public float palmSizeDeadZone = .07f, palmSizeRange = .3f;
        float leftScaleNeutral, rightScaleNeutral, lostSince = -1, recoveredSince = -1;
        float liveDepthDelta, liveSizeDelta, liveTurn;
        bool waitingForNeutral;
        public const float RecoverySeconds = 20f;
        [Range(1, 4)] public int driverSlot = 1;
        [Range(.1f, 1)] public float maximumForward = .6f;
        [Min(.05f)] public float raisedHandRange = .65f;
        [Min(.01f)] public float handDeadZone = .12f;
        [Min(.05f)] public float steeringRange = .45f;
        [Min(.01f)] public float depthDeadZone = .10f;
        [Range(.1f, 1)] public float maximumReverse = .6f;
        [Min(.1f)] public float gearHoldSeconds = .35f;
        public const float CalibrationSeconds = 8f;
        public bool IsCalibrated { get; private set; }
        public bool IsArmed { get; private set; }
        public bool CalibrationPending => deadline >= 0;
        public string Status { get; private set; } = "Calibrate with hands low in view, both PALMS facing camera.";
        DigiPhantController controller;
        float leftNeutral, rightNeutral, deadline = -1;
        int calibratedSlot, gear;
        float leftDepthNeutral, rightDepthNeutral, leftPalmSign, rightPalmSign, gearSince;
        void OnEnable() { controller = GetComponent<DigiPhantController>(); ResetCalibration(); }
        void OnDisable() { ResetCalibration(); }
        public void ResetCalibration()
        {
            IsCalibrated = IsArmed = false;
            deadline = -1;
            gear = 0; lostSince = recoveredSince = -1; liveTurn = liveDepthDelta = liveSizeDelta = 0; waitingForNeutral = false;
            Status = "Calibrate with hands low in view, both PALMS facing camera.";
            GetComponent<DigiPhantLocomotion>()?.StopMotion();
        }
        public void BeginCalibration(float now)
        {
            ResetCalibration();
            deadline = now + CalibrationSeconds;
            Status = "Driver: hands low in view, PALMS to camera, equal depth. Hold still.";
        }
        public bool InputsVisible(float now)
        {
            if (!controller) controller = GetComponent<DigiPhantController>();
            return controller.IsMovementVisible(driverSlot, Movement.LeftHandHeight, now) &&
                controller.IsMovementVisible(driverSlot, Movement.RightHandHeight, now);
        }
        public bool CalibrateAt(float now)
        {
            deadline = -1;
            if (!ReadHands(now, out float left, out float right) ||
                !controller.TryReadDriverHands(driverSlot, now, out float ld, out float rd, out float lp, out float rp) ||
                Mathf.Abs(lp) < .35f || Mathf.Abs(rp) < .35f)
            {
                IsCalibrated = IsArmed = false;
                Status = "Calibration failed: " + controller.DriverCalibrationProblem(driverSlot, now) + " Try again when both hands are clear.";
                return false;
            }
            if (usePalmSizeSteering && !controller.TryReadHandScale(driverSlot, now, out leftScaleNeutral, out rightScaleNeutral))
            {
                IsCalibrated = IsArmed = false;
                Status = "Calibration failed: palm size unavailable. Show both open hands; restart Camera tracking after this update.";
                return false;
            }
            lostSince = recoveredSince = -1; waitingForNeutral = false;
            leftDepthNeutral = ld; rightDepthNeutral = rd;
            leftPalmSign = Mathf.Sign(lp); rightPalmSign = Mathf.Sign(rp);
            gear = 0;
            leftNeutral = left;
            rightNeutral = right;
            calibratedSlot = driverSlot;
            IsCalibrated = IsArmed = true;
            Status = "Ready: palms = forward; backs = reverse; left hand forward = left; hands down = stop.";
            return true;
        }
        bool ReadHands(float now, out float left, out float right)
        {
            if (!controller) controller = GetComponent<DigiPhantController>();
            bool l = controller.TryReadRawMovement(driverSlot, Movement.LeftHandHeight, now, out left);
            bool r = controller.TryReadRawMovement(driverSlot, Movement.RightHandHeight, now, out right);
            return l && r;
        }
        public bool TryRead(float now, out float forward, out float steering)
        {
            forward = steering = 0; liveTurn = 0;
            if (!controller) controller = GetComponent<DigiPhantController>();
            if (!enableDriverNavigation || !isActiveAndEnabled) return false;
            if (controller.CalibrationPending)
            {
                ResetCalibration();
                Status = "Body calibration in progress. Calibrate driver afterward.";
                return false;
            }
            if (CalibrationPending)
            {
                if (now >= deadline) CalibrateAt(now);
                return false; // Never move on the calibration frame.
            }
            if (!IsCalibrated) return false; // Preserve the specific failure until the next attempt.
            if (driverSlot != calibratedSlot)
            {
                IsCalibrated = IsArmed = false;
                Status = "Driver changed. Calibrate the selected driver first.";
                return false;
            }
            bool visible = ReadHands(now, out float left, out float right) &&
                controller.TryReadDriverHands(driverSlot, now, out _, out _, out _, out _);
            float ls = 0, rs = 0;
            if (usePalmSizeSteering) visible &= controller.TryReadHandScale(driverSlot, now, out ls, out rs);
            if (!visible)
            {
                gear = 0; IsArmed = false; recoveredSince = -1;
                liveDepthDelta = liveSizeDelta = 0;
                waitingForNeutral = true;
                if (lostSince < 0) lostSince = now;
                if (now - lostSince >= RecoverySeconds)
                {
                    IsCalibrated = false;
                    Status = "Tracking missing for 20s. Check P label and recalibrate.";
                }
                else Status = "Stopped: tracking interrupted. Calibration retained for 20s; return to your P position.";
                return false;
            }
            if (lostSince >= 0)
            {
                if (now-lostSince >= RecoverySeconds)
                { IsCalibrated = IsArmed = false; Status = "Tracking gap exceeded 20s. Recalibrate driver."; return false; }
                lostSince = -1; // The detection gap has ended; allow time to read the recovery instruction.
            }
            if (waitingForNeutral)
            {
                if (left-leftNeutral > handDeadZone || right-rightNeutral > handDeadZone)
                { recoveredSince = -1; Status = "Tracking recovered: lower both hands to resume."; return false; }
                if (recoveredSince < 0) recoveredSince = now;
                if (now-recoveredSince < .4f) { Status = "Hold both hands lowered to resume."; return false; }
                waitingForNeutral = false; recoveredSince = -1; IsArmed = true; gear = 0;
                Status = "Ready again. Raise both hands to move.";
                return false; // Never move on the rearming frame.
            }
            controller.TryReadDriverHands(driverSlot, now, out float ld, out float rd, out float lp, out float rp);
            liveDepthDelta = (rd-rightDepthNeutral)-(ld-leftDepthNeutral);
            liveSizeDelta = usePalmSizeSteering ? Mathf.Log(rs/rightScaleNeutral)-Mathf.Log(ls/leftScaleNeutral) : 0;
            left -= leftNeutral; right -= rightNeutral;
            if (left <= handDeadZone && right <= handDeadZone)
            { gear = 0; liveTurn = 0; Status = "Stopped: hands lowered."; return true; }
            float facingLeft = lp * leftPalmSign, facingRight = rp * rightPalmSign;
            int requestedGear = facingLeft > .35f && facingRight > .35f ? 1 :
                facingLeft < -.35f && facingRight < -.35f ? -1 : 0;
            if (requestedGear == 0)
            {
                gear = 0;
                Status = "Stopped: both palms for forward, both backs for reverse.";
                return false;
            }
            if (gear != requestedGear)
            {
                gear = requestedGear;
                gearSince = now;
                Status = "Stopped: hold hand direction briefly to engage.";
                return false;
            }
            if (now - gearSince < gearHoldSeconds) return false;
            float amount = Mathf.InverseLerp(handDeadZone, handDeadZone + raisedHandRange, Mathf.Min(left,right));
            forward = amount * (gear > 0 ? maximumForward : -maximumReverse);
            float depthDifference = usePalmSizeSteering ? liveSizeDelta : liveDepthDelta;
            float dead = usePalmSizeSteering ? palmSizeDeadZone : depthDeadZone;
            float range = usePalmSizeSteering ? palmSizeRange : steeringRange;
            steering = Mathf.Abs(depthDifference) <= dead ? 0 : Mathf.Sign(depthDifference) *
                Mathf.InverseLerp(dead, dead + range, Mathf.Abs(depthDifference));
            liveTurn = steering;
            Status = gear > 0 ? "Forward: raise hands for speed; push left/right hand to turn." : "Reverse: raise hands for speed; hands down to stop.";
            return true;
        }

        public void DrawControls(float width)
        {
            var label = new GUIStyle(GUI.skin.label) { wordWrap = true };
            bool selected = GUILayout.Toggle(enableDriverNavigation, "Single-driver navigation");
            if (selected != enableDriverNavigation) { enableDriverNavigation = selected; ResetCalibration(); }
            if (!enableDriverNavigation) return;
            GUILayout.Label("Driver slot (match the camera P label)", label, GUILayout.Width(width));
            int count = Mathf.Clamp(controller.performerCount, 1, 4);
            var names = new string[count];
            for (int i = 0; i < count; i++) names[i] = "P" + (i + 1);
            int choice = GUILayout.Toolbar(driverSlot - 1, names) + 1;
            if (choice != driverSlot) { driverSlot = choice; ResetCalibration(); }
            GUILayout.Label("Raise hands: speed. Left hand toward camera: left; right hand: right. Both palms to camera: forward; both hand backs: reverse. Hold direction briefly. Both hands down: stop.", label, GUILayout.Width(width));
            bool sizeMode = GUILayout.Toggle(usePalmSizeSteering, "Steering: apparent palm size (off = pose depth)");
            if (sizeMode != usePalmSizeSteering) { usePalmSizeSteering = sizeMode; ResetCalibration(); }
            GUILayout.Label("Size is a camera-distance proxy: keep palms flat, extend one hand. Not measured 3D depth.", label, GUILayout.Width(width));
            GUILayout.Label(Status, label, GUILayout.Width(width));
            GUILayout.Label($"Depth delta {liveDepthDelta:F3} | Size delta {liveSizeDelta:F3} | Turn {liveTurn:F2}", label, GUILayout.Width(width));
            if (CalibrationPending)
                GUILayout.Label("Calibrating in " + Mathf.Max(0, Mathf.CeilToInt(deadline - Time.realtimeSinceStartup)) + "s", label);
            GUILayout.Label(controller.DriverHandDiagnostics(driverSlot, Time.realtimeSinceStartup), label, GUILayout.Width(width));
            if (GUILayout.Button("Calibrate driver (8 seconds)")) BeginCalibration(Time.realtimeSinceStartup);
        }
    }
}
