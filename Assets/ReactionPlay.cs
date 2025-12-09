using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ReactionPlay
/// - Responsibility: Orchestrate reaction sequence (body anim + facial blendshapes + lipsync).
/// - Pattern: Controller over Animator + AudioSource + SkinnedMeshRenderer.
/// </summary>
public class ReactionPlay : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private Button playButton;

    [Header("Audio Setup")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private float lipSyncSensitivity = 100.0f;

    [Header("Face Configuration")]
    [SerializeField] private SkinnedMeshRenderer faceMesh;

    [Header("Animator Setup")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private string loopParamName = "LoopStart";

    // Animator state identifiers (easily swappable -> Open/Closed principle)
    [SerializeField] private string smileStateName = "Smile";
    [SerializeField] private string sadStateName = "Sad";

    [Header("Blendshape Names")]
    [SerializeField] private string jawOpenName = "Xana_female_Bsps.jawOpen";
    [SerializeField]
    private string[] smileBlendShapes =
        { "Xana_female_Bsps.mouthSmileLeft", "Xana_female_Bsps.mouthSmileRight" };
    [SerializeField]
    private string[] sadBlendShapes =
        { "Xana_female_Bsps.mouthFrownLeft", "Xana_female_Bsps.mouthFrownRight" };

    [Header("Intensity Settings")]
    [Range(100, 200)]
    [SerializeField] private float maxSmileIntensity = 150f;
    [SerializeField] private float maxSadIntensity = 100f;
    [SerializeField] private float expressionChangeSpeed = 15.0f;

    private float _currentSmileWeight;
    private float _currentSadWeight;
    private float _currentLipOpenValue;

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClick);

        if (bodyAnimator != null)
            bodyAnimator.SetBool(loopParamName, false);

        ForceResetAll();
    }

    private void Update()
    {
        UpdateExpressionByState();
        UpdateLipSync();
    }

    /// <summary>
    /// Map animator state to facial expression weights.
    /// </summary>
    private void UpdateExpressionByState()
    {
        if (bodyAnimator == null)
            return;

        AnimatorStateInfo stateInfo = bodyAnimator.GetCurrentAnimatorStateInfo(0);

        float targetSmile = 0f;
        float targetSad = 0f;

        if (stateInfo.IsName(smileStateName))
        {
            targetSmile = maxSmileIntensity;
        }
        else if (stateInfo.IsName(sadStateName))
        {
            targetSad = maxSadIntensity;
        }

        _currentSmileWeight = Mathf.Lerp(_currentSmileWeight, targetSmile, Time.deltaTime * expressionChangeSpeed);
        _currentSadWeight = Mathf.Lerp(_currentSadWeight, targetSad, Time.deltaTime * expressionChangeSpeed);

        foreach (string name in smileBlendShapes)
            SetBlendShapeWeight(name, _currentSmileWeight);

        foreach (string name in sadBlendShapes)
            SetBlendShapeWeight(name, _currentSadWeight);
    }

    /// <summary>
    /// Drive jaw blendshape from audio volume (simple lipsync).
    /// </summary>
    private void UpdateLipSync()
    {
        if (voiceAudioSource == null)
            return;

        if (voiceAudioSource.isPlaying)
        {
            float currentVol = GetAudioVolume();
            _currentLipOpenValue = Mathf.Lerp(
                _currentLipOpenValue,
                currentVol * lipSyncSensitivity,
                Time.deltaTime * 20f
            );

            float finalWeight = Mathf.Clamp(_currentLipOpenValue, 0, 100);
            SetBlendShapeWeight(jawOpenName, finalWeight);
        }
        else if (_currentLipOpenValue > 0.1f)
        {
            _currentLipOpenValue = Mathf.Lerp(_currentLipOpenValue, 0, Time.deltaTime * 10f);
            SetBlendShapeWeight(jawOpenName, _currentLipOpenValue);
        }
    }

    /// <summary>
    /// UI command: start full reaction sequence.
    /// </summary>
    private void OnPlayButtonClick()
    {
        if (playButton != null)
            playButton.interactable = false;

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.Play();
        }

        StopCoroutine(nameof(EnableButtonAfterAudio));
        StartCoroutine(nameof(EnableButtonAfterAudio));

        if (bodyAnimator != null)
        {
            bodyAnimator.SetBool(loopParamName, true);
            bodyAnimator.Play(smileStateName, 0, 0f);
        }
    }

    private IEnumerator EnableButtonAfterAudio()
    {
        if (voiceAudioSource != null)
        {
            while (voiceAudioSource.isPlaying)
                yield return null;
        }

        if (playButton != null)
            playButton.interactable = true;
    }

    #region Helpers (SRP: isolate low-level operations)

    private float GetAudioVolume()
    {
        float[] data = new float[256];
        voiceAudioSource.GetOutputData(data, 0);
        float sum = 0f;

        foreach (float s in data)
            sum += Mathf.Abs(s);

        return sum / data.Length;
    }

    private void SetBlendShapeWeight(string name, float weight)
    {
        if (faceMesh == null || faceMesh.sharedMesh == null)
            return;

        int index = faceMesh.sharedMesh.GetBlendShapeIndex(name);
        if (index != -1)
            faceMesh.SetBlendShapeWeight(index, weight);
    }

    private void ForceResetAll()
    {
        _currentSmileWeight = 0f;
        _currentSadWeight = 0f;
        _currentLipOpenValue = 0f;

        foreach (string name in smileBlendShapes)
            SetBlendShapeWeight(name, 0f);

        foreach (string name in sadBlendShapes)
            SetBlendShapeWeight(name, 0f);

        SetBlendShapeWeight(jawOpenName, 0f);
    }

    #endregion
}
