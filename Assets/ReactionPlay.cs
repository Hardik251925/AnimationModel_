using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ReactionPlay : MonoBehaviour
{
    [Header("🧪 TESTING TOOL")]
    [Range(0, 1)] public float testSadness = 0f;

    [Header("UI & Audio")]
    [SerializeField] private Button playButton;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private float lipSyncSensitivity = 100.0f;

    [Header("Face Setup")]
    [SerializeField] private SkinnedMeshRenderer faceMesh;

    [Header("Animator Setup")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private string loopParamName = "LoopStart";
    [SerializeField] private string smileStateName = "Smile";
    [SerializeField] private string sadStateName = "Sad";

    [Header("Bone Control")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;
    [SerializeField] private Transform spine;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;

    [Header("Sad Pose Adjustments")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 spineOffset = new Vector3(4f, 0f, 0f);
    [SerializeField] private Vector3 leftArmOffset = new Vector3(-9.91f, 0f, -10.7f);
    [SerializeField] private Vector3 rightArmOffset = new Vector3(-6.96f, 0f, 20f);

    [Header("Blendshape Names")]
    [SerializeField] private string jawOpenName = "Xana_female_Bsps.jawOpen";

    // SMILE: Brows UP honi chahiye
    [SerializeField]
    private string[] smileBlendShapes = {
        "Xana_female_Bsps.mouthSmileLeft",
        "Xana_female_Bsps.mouthSmileRight",
        "Xana_female_Bsps.browOuterUpLeft",
        "Xana_female_Bsps.browOuterUpRight"
    };

    // SAD: Brows DOWN honi chahiye
    [SerializeField]
    private string[] sadBlendShapes = {
        "Xana_female_Bsps.mouthFrownLeft",
        "Xana_female_Bsps.mouthFrownRight",
        "Xana_female_Bsps.browDownLeft",
        "Xana_female_Bsps.browDownRight",
        "Xana_female_Bsps.browInnerUp"
    };

    [Header("Eye Blink Setup")]
    [SerializeField] private string[] blinkBlendShapes = { "Xana_female_Bsps.eyeBlinkLeft", "Xana_female_Bsps.eyeBlinkRight" };

    [Header("Timing & Speed")]
    [Range(100, 200)][SerializeField] private float maxSmileIntensity = 150f;
    [SerializeField] private float maxSadIntensity = 100f;

    // Face bohot TEZ badlega (Brows turant niche aayenge)
    [SerializeField] private float faceChangeSpeed = 25.0f;

    // Body SLOW badlegi (Gardan baad mein jhukegi)
    [SerializeField] private float bodyMoveSpeed = 2.0f;

    private float _currentSmileWeight, _currentSadWeight, _currentLipOpenValue, _currentBoneSadness;
    private float _blinkTimer, _nextBlinkTime;
    private bool _isBlinking;

    private void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClick);
        if (bodyAnimator != null) bodyAnimator.SetBool(loopParamName, false);
        _nextBlinkTime = Random.Range(2f, 4f);
        ForceResetAll();
    }

    private void Update()
    {
        UpdateExpressionByState();
        UpdateLipSync();
        HandleBlinking();
    }

    private void LateUpdate()
    {
        // Test Slider ya Calculated Weight
        float finalWeight = Mathf.Max(_currentBoneSadness, testSadness);

        if (finalWeight > 0.001f)
        {
            if (headBone != null) headBone.Rotate(headOffset * finalWeight);
            if (leftShoulder != null) leftShoulder.Rotate(shoulderOffset * finalWeight);
            if (rightShoulder != null)
            {
                Vector3 rightShOff = new Vector3(-shoulderOffset.x, -shoulderOffset.y, shoulderOffset.z);
                rightShoulder.Rotate(rightShOff * finalWeight);
            }
            if (leftArm != null) leftArm.Rotate(leftArmOffset * finalWeight);
            if (rightArm != null) rightArm.Rotate(rightArmOffset * finalWeight);
            if (spine != null) spine.Rotate(spineOffset * finalWeight);
        }
    }

    private void HandleBlinking()
    {
        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= _nextBlinkTime && !_isBlinking)
        {
            StartCoroutine(BlinkRoutine());
            _blinkTimer = 0f;
            _nextBlinkTime = Random.Range(2.0f, 5.0f);
        }
    }

    private IEnumerator BlinkRoutine()
    {
        _isBlinking = true;
        float t = 0;
        while (t < 100) { t += Time.deltaTime * 1500; foreach (string name in blinkBlendShapes) SetBlendShapeWeight(name, t); yield return null; }
        while (t > 0) { t -= Time.deltaTime * 1500; foreach (string name in blinkBlendShapes) SetBlendShapeWeight(name, t); yield return null; }
        foreach (string name in blinkBlendShapes) SetBlendShapeWeight(name, 0);
        _isBlinking = false;
    }

    private void UpdateExpressionByState()
    {
        if (bodyAnimator == null) return;
        AnimatorStateInfo stateInfo = bodyAnimator.GetCurrentAnimatorStateInfo(0);

        float targetSmile = 0f;
        float targetSad = 0f;
        float targetBoneSadness = 0f;

        // --- STEP 1: Determine Targets ---
        if (stateInfo.IsName(smileStateName))
        {
            targetSmile = maxSmileIntensity;
            // Smile mein Body turant seedhi honi chahiye
            targetBoneSadness = 0f;
        }
        else if (stateInfo.IsName(sadStateName))
        {
            targetSad = maxSadIntensity;
            // Sad mein Body tabhi jhukegi jab logic allow karega (Step 3 dekho)
            targetBoneSadness = 1f;
        }

        // --- STEP 2: Move Face FAST ---
        _currentSmileWeight = Mathf.Lerp(_currentSmileWeight, targetSmile, Time.deltaTime * faceChangeSpeed);
        _currentSadWeight = Mathf.Lerp(_currentSadWeight, targetSad, Time.deltaTime * faceChangeSpeed);

        // --- STEP 3: Body Delay Logic (THE FIX) ---

        // Agar hum SAD state mein hain
        if (targetBoneSadness > 0.5f)
        {
            // Check karo: Kya Chehra poori tarah Sad ho chuka hai? (80% Sadness aa gayi?)
            // Jab tak chehra sad nahi hota, body wait karegi (0 rahegi)
            if (_currentSadWeight < (maxSadIntensity * 0.8f))
            {
                targetBoneSadness = 0f; // ROKO! Abhi gardan mat jhukao
            }
        }
        else
        {
            // Agar Smile par wapis ja rahe hain, to turant body seedhi karo (No delay)
            // Body jaldi seedhi hogi taaki "Happy" energetic lage
            targetBoneSadness = 0f;
        }

        // Body ko move karo (Slowly)
        _currentBoneSadness = Mathf.Lerp(_currentBoneSadness, targetBoneSadness, Time.deltaTime * bodyMoveSpeed);

        // Test Slider Override
        if (testSadness > 0)
        {
            _currentSadWeight = Mathf.Lerp(0, maxSadIntensity, testSadness);
            _currentSmileWeight = 0;
        }

        foreach (string name in smileBlendShapes) SetBlendShapeWeight(name, _currentSmileWeight);
        foreach (string name in sadBlendShapes) SetBlendShapeWeight(name, _currentSadWeight);
    }

    private void UpdateLipSync()
    {
        if (voiceAudioSource == null || !voiceAudioSource.isPlaying)
        {
            if (_currentLipOpenValue > 0.1f) { _currentLipOpenValue = Mathf.Lerp(_currentLipOpenValue, 0, Time.deltaTime * 10f); SetBlendShapeWeight(jawOpenName, _currentLipOpenValue); }
            return;
        }
        float currentVol = GetAudioVolume();
        _currentLipOpenValue = Mathf.Lerp(_currentLipOpenValue, currentVol * lipSyncSensitivity, Time.deltaTime * 20f);
        SetBlendShapeWeight(jawOpenName, Mathf.Clamp(_currentLipOpenValue, 0, 100));
    }

    private void OnPlayButtonClick()
    {
        if (playButton != null) playButton.interactable = false;
        if (voiceAudioSource != null) { voiceAudioSource.Stop(); voiceAudioSource.Play(); }
        StopCoroutine("EnableButtonAfterAudio"); StartCoroutine("EnableButtonAfterAudio");
        if (bodyAnimator != null)
        {
            bodyAnimator.SetBool(loopParamName, true);
            bodyAnimator.Play(smileStateName, 0, 0f);
        }
    }

    private IEnumerator EnableButtonAfterAudio()
    {
        if (voiceAudioSource != null) while (voiceAudioSource.isPlaying) yield return null;
        if (playButton != null) playButton.interactable = true;
    }

    private float GetAudioVolume() { float[] data = new float[256]; voiceAudioSource.GetOutputData(data, 0); float sum = 0f; foreach (float s in data) sum += Mathf.Abs(s); return sum / 256; }
    private void SetBlendShapeWeight(string name, float weight) { if (faceMesh == null) return; int index = faceMesh.sharedMesh.GetBlendShapeIndex(name); if (index != -1) faceMesh.SetBlendShapeWeight(index, weight); }
    private void ForceResetAll() { _currentSmileWeight = 0f; _currentSadWeight = 0f; _currentLipOpenValue = 0f; _currentBoneSadness = 0f; SetBlendShapeWeight(jawOpenName, 0f); }
}