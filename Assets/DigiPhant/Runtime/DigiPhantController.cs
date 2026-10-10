using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace DigiPhant
{
    public enum Movement { LeftHandHeight, RightHandHeight, LeftFootLift, RightFootLift, Lean, ArmSpread }
    public enum InputMode { TestSliders, Camera }

    [Serializable] public class PerformerFrame
    {
        public int slot;
        public float mouthOpen, mouthConfidence;
        public float[] values;
        public float[] confidence;
        public float[] flap;
        public float[] handDepth, handDepthConfidence, palmFacing, palmConfidence, handScale, handScaleConfidence;
    }
    [Serializable] public class PoseFrame
    {
        public int version;
        public string trackingEpoch;
        public int performerCount;
        public bool upperBodyOnly;
        public PerformerFrame[] people;
    }
    [Serializable] public class BoneControl
    {
        public string label;
        [Range(1, 4)] public int performer = 1;
        public Movement movement;
        public Transform[] bones;
        [Tooltip("Rotation axis in each bone's local coordinates.")]
        public Vector3 localAxis = Vector3.forward;
        [Tooltip("Maximum rotation per bone for an input of 1. Negative values invert direction.")]
        public float degrees = 20;
        [Range(0, 3)] public float sensitivity = 1;
        [Range(0, 20)] public float smoothing = 8;
        [Range(-1, 1)] public float testValue;
        [Tooltip("How much of this gesture is added over locomotion animation. Zero lets the animation own this joint.")]
        [Range(0, 1)] public float locomotionWeight = 1;
        [NonSerialized] public float current;
    }

    public class DigiPhantController : MonoBehaviour
    {
        public InputMode inputMode = InputMode.TestSliders;
        [HideInInspector] public int performerCount = 3;
        public bool upperBodyOnly;
        public BoneControl[] controls = Array.Empty<BoneControl>();
        [Tooltip("Local camera bridge port. Change outside Play mode.")]
        public int port = 5055;
        [Range(0, 1)] public float minimumConfidence = .5f;
        [Min(.1f)] public float trackingTimeout = .5f;
        public bool showControls = true;
        public bool showCameraPreview = true;
        public string Status { get; private set; } = "Test sliders ready";
        public int ReceivedFrames { get; private set; }
        float lastPacketAt = -1, lastAcceptedAt = -1, nextRejectWarning;
        int assignedBodies;
        string trackingDetail = "Waiting for camera packets";
        bool packetRejected;
        public string TrackingStatus {
            get {
                float age = lastPacketAt < 0 ? -1 : Time.realtimeSinceStartup - lastPacketAt;
                string detail = trackingDetail;
                if (!packetRejected && lastAcceptedAt >= 0 && Time.realtimeSinceStartup - lastAcceptedAt > trackingTimeout)
                    detail = "Stale frame: tracking timed out";
                return detail + " | accepted " + ReceivedFrames + " | last packet " + (age < 0 ? "none" : age.ToString("F1") + "s ago");
            }
        }
        bool RejectPacket(string reason, float now) {
            packetRejected = true;
            trackingDetail = reason;
            if (now >= nextRejectWarning) {
                Debug.LogWarning("DigiPhant tracking: " + reason);
                nextRejectWarning = now + 1;
            }
            return false;
        }
        public bool IsCalibrated { get; private set; }
        float calibrationDeadline = -1;
        public bool CalibrationPending => calibrationDeadline >= 0;
        readonly float[,] values = new float[4, 6];
        readonly float[,] flap = new float[4, 4];
        readonly float[,] confidence = new float[4, 6];
        readonly float[,] neutral = new float[4, 6];
        readonly float[,] handScale = new float[4, 2], handScaleConfidence = new float[4, 2];
        readonly float[,] handDepth = new float[4, 2], handDepthConfidence = new float[4, 2];
        readonly float[,] palmFacing = new float[4, 2], palmConfidence = new float[4, 2];
        readonly float[] mouthOpen = new float[4], mouthConfidence = new float[4];
        string trackingEpoch;
        readonly float[] lastSeen = { -1000, -1000, -1000, -1000 };
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        UdpClient socket;
        Vector2 scroll;
        float nextCountRequest;
        int previousCount;
        InputMode previousMode;
        bool previousUpperBodyOnly;

        void OnEnable()
        {
            if (Application.isPlaying && !Application.isBatchMode && showCameraPreview && GetComponent<DigiPhantCameraPreview>() == null)
                gameObject.AddComponent<DigiPhantCameraPreview>();
            if (Application.isPlaying && !Application.isBatchMode && GetComponent<DigiPhantRecording>() == null)
                gameObject.AddComponent<DigiPhantRecording>();
            previousCount = performerCount;
            previousUpperBodyOnly = upperBodyOnly;
            ClearTracking();
            nextCountRequest = 0;
            ReceivedFrames = 0;
            Status = "Test sliders ready";
            CaptureRestPose();
            previousMode = inputMode;
            if (inputMode == InputMode.Camera) StartReceiver();
        }
        public void SetPerformerCount(int count)
        {
            performerCount = previousCount = Mathf.Clamp(count, 1, 4);
            foreach (var c in controls)
            {
                if (GetComponent<P2EatingRole>()?.RoleEnabled == true) continue;
                switch (c.label)
                {
                    case "Front left leg": case "Front right leg": c.performer = 1; break;
                    case "Rear left leg": case "Rear right leg": case "Tail sway":
                        c.performer = Mathf.Min(2, performerCount); break;
                    case "Head turn": case "Left ear": case "Right ear":
                        c.performer = Mathf.Min(3, performerCount); break;
                    case "Trunk curl": c.performer = performerCount; break;
                    default: c.performer = Mathf.Clamp(c.performer, 1, performerCount); break;
                }
            }
            GetComponent<DigiPhantLocomotion>()?.ClampPerformers(performerCount);
            ClearTracking();
            ResetControls();
            nextCountRequest = 0;
            Status = "Group size changed. Set neutral pose again in Camera mode.";
        }
        public void SetUpperBodyOnly(bool enabled)
        {
            upperBodyOnly = previousUpperBodyOnly = enabled;
            ClearTracking();
            ResetControls();
            nextCountRequest = 0;
            Status = "Movement mode changed. Wait for camera, then set neutral pose.";
        }
        public void CaptureRestPose()
        {
            rest.Clear();
            foreach (var c in controls)
                if (c.bones != null)
                    foreach (var bone in c.bones)
                        if (bone != null && !rest.ContainsKey(bone)) rest.Add(bone, bone.localRotation);
        }
        void StartReceiver()
        {
            StopReceiver();
            try
            {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                socket.Client.Blocking = false;
                Status = "Waiting for local camera bridge";
            }
            catch (Exception e) when (e is SocketException || e is ArgumentOutOfRangeException)
            { StopReceiver(); Status = "Cannot open camera port: " + e.Message; }
        }
        void StopReceiver() { socket?.Close(); socket = null; }
        public void SetInputMode(InputMode mode)
        {
            inputMode = previousMode = mode;
            ClearTracking();
            if (mode == InputMode.Camera) StartReceiver();
            else { StopReceiver(); Status = "Test sliders ready"; }
        }
        void ClearTracking()
        {
            GetComponent<DigiPhantDriverNavigation>()?.ResetCalibration();
            calibrationDeadline = -1;
            GetComponent<DigiPhantLocomotion>()?.StopMotion();
            IsCalibrated = false;
            Array.Clear(confidence, 0, confidence.Length);
            Array.Clear(neutral, 0, neutral.Length);
            ClearHandValidity();
            for (int i = 0; i < 4; i++) lastSeen[i] = -1000;
        }
        void ClearHandValidity()
        {
            Array.Clear(flap, 0, flap.Length);
            Array.Clear(mouthOpen, 0, mouthOpen.Length);
            Array.Clear(mouthConfidence, 0, mouthConfidence.Length);
            Array.Clear(handScaleConfidence, 0, handScaleConfidence.Length);
            Array.Clear(handDepthConfidence, 0, handDepthConfidence.Length);
            Array.Clear(palmConfidence, 0, palmConfidence.Length);
        }
        public bool AcceptPacket(string json, float now)
        {
            lastPacketAt = now;
            bool valid = AcceptPacketChecked(json, now);
            if (!valid)
            {
                Array.Clear(confidence, 0, confidence.Length);
                ClearHandValidity();
            }
            return valid;
        }
        static bool ValidPair(float[] values, float[] confidence)
        {
            if (values == null && confidence == null) return true;
            if (values == null || confidence == null || values.Length != 2 || confidence.Length != 2) return false;
            for (int i = 0; i < 2; i++)
                if (!Finite(values[i]) || !Finite(confidence[i]) || confidence[i] < 0 || confidence[i] > 1) return false;
            return true;
        }
        static bool ValidFlap(float[] f) {
            if (f == null || f.Length == 0) return true;
            return f.Length == 4 && Array.TrueForAll(f, Finite) && f[2] >= 0 && f[2] <= 1 && f[3] >= 0 && f[3] <= 1;
        }
        public bool TryReadFlap(int slot, float now, out float left, out float right, out bool sideways) {
            left = right = 0; sideways = false;
            if (!IsTracked(slot, now)) return false;
            int i = slot - 1;
            if (flap[i,3] < minimumConfidence) return false;
            left = flap[i,0]; right = flap[i,1]; sideways = flap[i,2] >= .5f;
            return true;
        }
        bool AcceptPacketChecked(string json, float now)
        {
            PoseFrame frame;
            try { frame = JsonUtility.FromJson<PoseFrame>(json); }
            catch (ArgumentException) { return RejectPacket("Invalid packet JSON", now); }
            if (frame == null || frame.version != 1 || frame.people == null || frame.people.Length > 4) return RejectPacket("Invalid packet version or people array", now);
            if (frame.performerCount != 0 && frame.performerCount != performerCount)
            {
                Array.Clear(confidence, 0, confidence.Length);
                Status = "Updating camera group size...";
                return RejectPacket("People count mismatch: camera " + frame.performerCount + ", Unity " + performerCount, now);
            }
            if (frame.upperBodyOnly != upperBodyOnly)
            {
                Array.Clear(confidence, 0, confidence.Length);
                Status = "Updating camera movement mode...";
                return RejectPacket("Movement mode mismatch: camera upper-body=" + frame.upperBodyOnly + ", Unity=" + upperBodyOnly, now);
            }
            var slots = new HashSet<int>();
            foreach (var p in frame.people)
            {
                if (p == null || p.slot < 1 || p.slot > 4 || !slots.Add(p.slot) ||
                    p.values == null || p.confidence == null || p.values.Length != 6 || p.confidence.Length != 6) return RejectPacket("Invalid performer slot or feature arrays", now);
                if (!ValidFlap(p.flap)) return RejectPacket("Invalid flap array", now);
                if (!Finite(p.mouthOpen) || p.mouthOpen < 0 || p.mouthOpen > 3 || !Finite(p.mouthConfidence) || p.mouthConfidence < 0 || p.mouthConfidence > 1) return RejectPacket("Invalid mouth value or confidence", now);
                if (!ValidPair(p.handDepth, p.handDepthConfidence) || !ValidPair(p.palmFacing, p.palmConfidence) || !ValidPair(p.handScale, p.handScaleConfidence)) return RejectPacket("Invalid hand arrays or confidence", now);
                for (int k = 0; k < 6; k++)
                    if (!Finite(p.values[k]) || !Finite(p.confidence[k]) || p.confidence[k] < 0 || p.confidence[k] > 1) return RejectPacket("Invalid body value or confidence for P" + p.slot, now);
            }
            if (!string.IsNullOrEmpty(frame.trackingEpoch) && frame.trackingEpoch != trackingEpoch)
            {
                ClearTracking();
                trackingEpoch = frame.trackingEpoch;
            }
            Array.Clear(confidence, 0, confidence.Length);
            ClearHandValidity();
            foreach (var p in frame.people)
            {
                int i = p.slot - 1;
                lastSeen[i] = now;
                if (p.flap != null && p.flap.Length == 4) for (int k = 0; k < 4; k++) flap[i,k] = p.flap[k];
                mouthOpen[i] = p.mouthOpen; mouthConfidence[i] = p.mouthConfidence;
                for (int k = 0; k < 6; k++)
                { values[i, k] = Mathf.Clamp(p.values[k], -3, 3); confidence[i, k] = p.confidence[k]; }
                for (int k = 0; k < 2; k++)
                {
                    if (p.handScale != null) { handScale[i,k] = p.handScale[k]; handScaleConfidence[i,k] = p.handScaleConfidence[k]; }
                    if (p.handDepth != null) { handDepth[i,k] = p.handDepth[k]; handDepthConfidence[i,k] = p.handDepthConfidence[k]; }
                    if (p.palmFacing != null) { palmFacing[i,k] = Mathf.Clamp(p.palmFacing[k], -1, 1); palmConfidence[i,k] = p.palmConfidence[k]; }
                }
            }
            ReceivedFrames++;
            lastAcceptedAt = now;
            packetRejected = false;
            assignedBodies = frame.people.Length;
            bool anyTracked = false;
            for (int slot = 1; slot <= performerCount; slot++) anyTracked |= IsTracked(slot, now);
            trackingDetail = assignedBodies == 0 ? "No assigned bodies: check people count / visibility" :
                !anyTracked ? "Assigned bodies below confidence threshold" : assignedBodies + " assigned bodies";
            return true;
        }
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public void PollCamera(float now)
        {
            if (socket == null) return;
            try
            {
                if (now >= nextCountRequest)
                {
                    var request = Encoding.UTF8.GetBytes("{\"version\":1,\"performerCount\":" + performerCount + ",\"upperBodyOnly\":" + (upperBodyOnly ? "true" : "false") + "}");
                    socket.Send(request, request.Length, new IPEndPoint(IPAddress.Loopback, port + 1));
                    nextCountRequest = now + .5f;
                }
                // Bound per-frame work; use the newest datagram from this batch.
                byte[] latest = null;
                IPEndPoint sender = new IPEndPoint(IPAddress.Loopback, 0);
                for (int i = 0; i < 64 && socket.Available > 0; i++) latest = socket.Receive(ref sender);
                if (latest != null) {
                    if (latest.Length < 16000) AcceptPacket(Encoding.UTF8.GetString(latest), now);
                    else { lastPacketAt = now; Array.Clear(confidence, 0, confidence.Length); ClearHandValidity(); RejectPacket("Camera packet too large", now); }
                }
            }
            catch (SocketException e) { Status = "Camera connection: " + e.Message; }
        }
        public bool IsTracked(int slot, float now)
        {
            int i = slot - 1;
            if (i < 0 || i >= 4 || now - lastSeen[i] > trackingTimeout) return false;
            for (int k = 0; k < 6; k++) if (confidence[i, k] >= minimumConfidence) return true;
            return false;
        }
        public void ReassignPeople()
        {
            ClearTracking();
            if (socket == null) return;
            try
            {
                var request = Encoding.UTF8.GetBytes("{\"version\":1,\"performerCount\":" + performerCount + ",\"upperBodyOnly\":" + (upperBodyOnly ? "true" : "false") + ",\"reset\":true}");
                socket.Send(request, request.Length, new IPEndPoint(IPAddress.Loopback, port + 1));
                Status = "Reassigning people. Set neutral pose when visible.";
            }
            catch (SocketException e) { Status = e.Message; }
        }
        public void InvalidateCalibration()
        {
            GetComponent<DigiPhantDriverNavigation>()?.ResetCalibration();
            calibrationDeadline = -1;
            IsCalibrated = false;
            GetComponent<DigiPhantLocomotion>()?.StopMotion();
        }
        public bool IsMovementVisible(int performer, Movement movement, float now)
        {
            int i = performer - 1, k = (int)movement;
            return i >= 0 && i < performerCount && k >= 0 && k < 6 &&
                IsTracked(performer, now) && confidence[i, k] >= minimumConfidence;
        }
        // Read-only diagnostics use the same freshness and confidence gates as navigation.
        public string DriverCalibrationProblem(int performer, float now)
        {
            int i = performer - 1;
            if (i < 0 || i >= performerCount || i >= 4) return "Select a valid driver slot.";
            if (now - lastSeen[i] > trackingTimeout) return "Driver data is stale; check the P label.";
            for (int k = 0; k < 2; k++)
            {
                string hand = k == 0 ? "Left hand" : "Right hand";
                if (confidence[i,k] < minimumConfidence) return hand + ": wrist not clear enough.";
                if (handDepthConfidence[i,k] < minimumConfidence) return hand + ": depth tracking unavailable or unclear.";
                if (palmConfidence[i,k] < minimumConfidence) return hand + ": palm not detected or not matched to driver.";
                if (Mathf.Abs(palmFacing[i,k]) < .35f) return hand + ": too edge-on; face your open palm toward camera.";
            }
            return "";
        }
        public string DriverHandDiagnostics(int performer, float now)
        {
            int i = performer - 1;
            if (i < 0 || i >= performerCount || i >= 4) return "No valid driver selected.";
            if (now - lastSeen[i] > trackingTimeout) return "Hand data stale; waiting for driver.";
            string Hand(int k) => (k == 0 ? "Left" : "Right") +
                " wrist " + confidence[i,k].ToString("F2") +
                " | depth " + handDepth[i,k].ToString("F2") + " (conf " + handDepthConfidence[i,k].ToString("F2") + ")" +
                " | size " + handScale[i,k].ToString("F2") + " (conf " + handScaleConfidence[i,k].ToString("F2") + ")" +
                " | palm " + palmConfidence[i,k].ToString("F2") +
                " | facing " + Mathf.Abs(palmFacing[i,k]).ToString("F2");
            return Hand(0) + "\n" + Hand(1) + "\nConfidence needs " + minimumConfidence.ToString("F2") +
                "; facing needs 0.35. Left/right are your own hands.";
        }
        public bool TryReadHandScale(int performer, float now, out float left, out float right)
        {
            left = right = 0;
            if (!IsTracked(performer, now)) return false;
            int i = performer - 1;
            if (handScaleConfidence[i,0] < minimumConfidence || handScaleConfidence[i,1] < minimumConfidence ||
                handScale[i,0] <= .001f || handScale[i,1] <= .001f) return false;
            left = handScale[i,0]; right = handScale[i,1]; return true;
        }
        public bool TryReadDriverHands(int performer, float now, out float leftDepth, out float rightDepth,
            out float leftPalm, out float rightPalm)
        {
            leftDepth = rightDepth = leftPalm = rightPalm = 0;
            if (!IsMovementVisible(performer, Movement.LeftHandHeight, now) ||
                !IsMovementVisible(performer, Movement.RightHandHeight, now)) return false;
            int i = performer - 1;
            for (int k = 0; k < 2; k++)
                if (handDepthConfidence[i,k] < minimumConfidence || palmConfidence[i,k] < minimumConfidence) return false;
            leftDepth = handDepth[i,0]; rightDepth = handDepth[i,1];
            leftPalm = palmFacing[i,0]; rightPalm = palmFacing[i,1];
            return true;
        }
        // For independently calibrated navigation only; preserves body-control neutral poses.
        public bool TryReadRawMovement(int performer, Movement movement, float now, out float value)
        {
            value = 0;
            if (!IsMovementVisible(performer, movement, now)) return false;
            value = values[performer - 1, (int)movement];
            return true;
        }
        public bool TryReadMovement(int performer, Movement movement, float now, out float value)
        {
            value = 0;
            if (!IsCalibrated || !IsMovementVisible(performer, movement, now)) return false;
            value = values[performer - 1, (int)movement] - neutral[performer - 1, (int)movement];
            return true;
        }
        public void BeginCalibrationCountdown(float now)
        {
            if (inputMode != InputMode.Camera) return;
            InvalidateCalibration();
            ResetControls();
            calibrationDeadline = now + 10;
            Status = "Get into your neutral pose and hold still.";
        }
        public void CancelCalibrationCountdown()
        {
            calibrationDeadline = -1;
            Status = "Calibration cancelled. Set neutral pose when ready.";
        }
        public void UpdateCalibrationCountdown(float now)
        {
            if (!CalibrationPending || now < calibrationDeadline) return;
            calibrationDeadline = -1;
            CalibrateAt(now);
        }
        public void Calibrate() => CalibrateAt(Time.realtimeSinceStartup);
        public bool CalibrateAt(float now)
        {
            calibrationDeadline = -1;
            // Require every mapped input, so occluded landmarks cannot silently become neutral.
            foreach (var c in controls)
            {
                int i = c.performer - 1, k = (int)c.movement;
                if (i < 0 || i >= performerCount || k < 0 || k >= 6 ||
                    !IsTracked(c.performer, now) || confidence[i, k] < minimumConfidence)
                { Status = "Calibration needs all assigned movements visible"; return false; }
            }
            var locomotion = GetComponent<DigiPhantLocomotion>();
            if (locomotion != null && locomotion.isActiveAndEnabled && !locomotion.InputsVisible(now))
            { Status = "Calibration needs speed and steering inputs visible"; return false; }
            Array.Copy(values, neutral, values.Length);
            IsCalibrated = true;
            Status = "Neutral pose saved";
            return true;
        }
        public bool TryReadMouthOpen(int slot, float now, out float value)
        {
            value = 0;
            int i = slot - 1;
            if (i < 0 || i >= performerCount || i >= 4 || now - lastSeen[i] > trackingTimeout ||
                mouthConfidence[i] <= 0 || mouthConfidence[i] < minimumConfidence) return false;
            value = mouthOpen[i]; return true;
        }
        public void ResetTrunkToRest(BoneControl trunk)
        {
            if (trunk?.bones == null) return;
            foreach (var bone in trunk.bones)
                if (bone && rest.TryGetValue(bone, out var rotation)) bone.localRotation = rotation;
        }
        public void ResetControls()
        {
            foreach (var c in controls) { c.testValue = 0; c.current = 0; }
            foreach (var pair in rest) if (pair.Key != null) pair.Key.localRotation = pair.Value;
        }
        public void ApplyControls(float now, float dt)
        {
            foreach (var pair in rest) if (pair.Key != null) pair.Key.localRotation = pair.Value;
            var locomotion = GetComponent<DigiPhantLocomotion>();
            bool animated = locomotion != null && locomotion.isActiveAndEnabled && locomotion.Evaluate(now, dt);
            var eatingRole = GetComponent<P2EatingRole>();
            bool fixedRoles = inputMode == InputMode.Camera && eatingRole && eatingRole.RoleEnabled;
            foreach (var c in controls)
            {
                float target = 0;
                int i = c.performer - 1, k = (int)c.movement;
                if (fixedRoles)
                {
                    bool mouthVisible = TryReadMouthOpen(2, now, out float mouth);
                    target = c.label == "Trunk curl" && mouthVisible ? Mathf.Clamp01(mouth) : 0;
                    if (target == 0) c.current = 0;
                }
                else if (i >= 0 && i < performerCount && k >= 0 && k < 6)
                {
                    if (inputMode == InputMode.TestSliders) target = c.testValue;
                    else if (IsCalibrated && IsTracked(c.performer, now) && confidence[i, k] >= minimumConfidence)
                        target = values[i, k] - neutral[i, k];
                }
                target = Mathf.Clamp(target * (fixedRoles ? 1 : c.sensitivity), -1, 1);
                c.current = c.smoothing <= 0 ? target : Mathf.Lerp(c.current, target, 1 - Mathf.Exp(-c.smoothing * dt));
                if (c.bones == null || c.localAxis.sqrMagnitude < .0001f) continue;
                foreach (var bone in c.bones)
                    if (bone != null && rest.ContainsKey(bone))
                        bone.localRotation *= Quaternion.AngleAxis(c.current * c.degrees * (animated ? c.locomotionWeight : 1), c.localAxis.normalized);
            }
        }
        void LateUpdate()
        {
            if (previousUpperBodyOnly != upperBodyOnly) SetUpperBodyOnly(upperBodyOnly);
            if (previousCount != performerCount) SetPerformerCount(performerCount);
            if (previousMode != inputMode) SetInputMode(inputMode);
            if (inputMode == InputMode.Camera) PollCamera(Time.realtimeSinceStartup);
            UpdateCalibrationCountdown(Time.realtimeSinceStartup);
            ApplyControls(Time.realtimeSinceStartup, Time.deltaTime);
            var stageCamera = Camera.main;
            if (stageCamera != null)
            {
                float left = showControls ? (Mathf.Min(350, Screen.width * .32f) + 20) / Mathf.Max(1, Screen.width) : 0;
                stageCamera.rect = new Rect(left, 0, 1 - left, 1);
            }
        }
        void OnDisable() { calibrationDeadline = -1; StopReceiver(); GetComponent<DigiPhantLocomotion>()?.StopAnimation(); ResetControls(); }
        void OnGUI()
        {
            if (!showControls) return;
            float width = Mathf.Min(350, Screen.width * .32f);
            GUI.DrawTexture(new Rect(0, 0, width + 20, Screen.height), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            var labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            void Label(string text) => GUILayout.Label(text, labelStyle, GUILayout.Width(width - 32));
            GUILayout.BeginArea(new Rect(10, 10, width, Screen.height - 20), GUI.skin.box);
            GetComponent<DigiPhantCameraPreview>()?.DrawInline(width - 20);
            GetComponent<DigiPhantRecording>()?.DrawControls(width - 32);
            scroll = GUILayout.BeginScrollView(scroll);
            Label("DIGIPHANT | collective digital twin");
            Label("Choose your group size");
            int count = GUILayout.Toolbar(performerCount - 1, new[] { "1", "2", "3", "4" }) + 1;
            if (count != performerCount) SetPerformerCount(count);
            bool upper = GUILayout.Toolbar(upperBodyOnly ? 1 : 0, new[] { "Full body", "Seated / upper body" }) == 1;
            if (upper != upperBodyOnly) SetUpperBodyOnly(upper);
            if (upperBodyOnly)
            {
                var navigation = GetComponent<DigiPhantDriverNavigation>();
                if (navigation && navigation.isActiveAndEnabled && navigation.enableDriverNavigation)
                    Label(GetComponent<P2EatingRole>()?.RoleEnabled == true ? (GetComponent<StudentWork.LollipopPickup>() ? "P1 drives. P2 opens their mouth to eat watermelon. P3 shows the lollipop card; after two candies, flaps to fly." : "P1 drives with hands. P2 opens their mouth to pick up and eat. P3 is unassigned.") : "Keep shoulders and wrists visible. Driver hand controls below operate travel; other body mappings are unchanged.");
                else Label("Keep shoulders and hands visible. Raise hands for legs; tilt shoulders for steering, head and tail. Solo: left hand also drives travel; right hand curls trunk.");
            }
            int mode = GUILayout.Toolbar((int)inputMode, new[] { "Test sliders", "Camera" });
            if (mode != (int)inputMode) SetInputMode((InputMode)mode);
            Label(Status);
            GetComponent<DigiPhantLocomotion>()?.DrawControls(width - 32);
            if (inputMode == InputMode.Camera)
            {
                for (int i = 1; i <= performerCount; i++)
                    Label("Performer " + i + (IsTracked(i, Time.realtimeSinceStartup) ? " · visible" : " · waiting / lost"));
                Label(TrackingStatus);
                bool fixedEatingRoles = GetComponent<P2EatingRole>()?.RoleEnabled == true;
                if (fixedEatingRoles) Label("P2 mouth: open to grab, open wider to eat, close to drop. Body neutral is not required.");
                if (!fixedEatingRoles && !IsCalibrated) Label("Set neutral pose for body gestures. Driver navigation has its own calibration above.");
                if (CalibrationPending)
                {
                    int seconds = Mathf.Max(0, Mathf.CeilToInt(calibrationDeadline - Time.realtimeSinceStartup));
                    Label("Setting neutral pose in " + seconds + " seconds — hold your resting pose.");
                    if (GUILayout.Button("Cancel countdown")) CancelCalibrationCountdown();
                }
                else if (!fixedEatingRoles && GUILayout.Button("Set neutral pose (10 seconds)"))
                    BeginCalibrationCountdown(Time.realtimeSinceStartup);
                if (GUILayout.Button("Reassign people")) ReassignPeople();
                if (!fixedEatingRoles) Label("Hold a comfortable pose, then set neutral.");
            }
            else
            {
                Label("Move a slider to test its elephant part.");
                foreach (var c in controls)
                {
                    Label(c.label + "  · P" + c.performer);
                    c.testValue = GUILayout.HorizontalSlider(c.testValue, -1, 1);
                }
                if (GUILayout.Button("Reset body sliders")) ResetControls();
            }
            GUILayout.Space(8);
            Label("To adapt: stop Play, select DigiPhant Controls,\nthen edit Controls in the Inspector.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
