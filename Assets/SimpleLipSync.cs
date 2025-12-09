using UnityEngine;

/// <summary>
/// SimpleLipSync
/// - Responsibility: Drive mouth/blendshape or jaw bone based on audio volume.
/// - Pattern: Strategy-style fallback (primary: blendshape, secondary: jaw bone).
/// </summary>
public class SimpleLipSync : MonoBehaviour
{
    [Header("Audio Setup")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private float sensitivity = 10.0f;
    [SerializeField] private float smoothness = 10.0f;

    [Header("Option 1: Blendshapes (Recommended)")]
    [SerializeField] private SkinnedMeshRenderer characterFaceMesh;
    [SerializeField] private string mouthOpenBlendShape = "mouthOpen";

    [Header("Option 2: Jaw Bone (Fallback)")]
    [SerializeField] private Transform jawBone;
    [SerializeField] private Vector3 jawClosedRotation;
    [SerializeField] private Vector3 jawOpenRotation = new Vector3(20, 0, 0);

    private int _blendShapeIndex = -1;
    private float _currentVolume;

    private void Start()
    {
        InitBlendShape();
        InitJawBone();
    }

    private void Update()
    {
        float targetVolume = GetTargetVolume();
        _currentVolume = Mathf.Lerp(_currentVolume, targetVolume, Time.deltaTime * smoothness);
        ApplyLipSync(_currentVolume);
    }

    /// <summary>
    /// Resolve which control strategy is available (blendshape index).
    /// </summary>
    private void InitBlendShape()
    {
        if (characterFaceMesh == null || characterFaceMesh.sharedMesh == null)
            return;

        _blendShapeIndex = characterFaceMesh.sharedMesh.GetBlendShapeIndex(mouthOpenBlendShape);
    }

    /// <summary>
    /// Cache initial jaw rotation for interpolation.
    /// </summary>
    private void InitJawBone()
    {
        if (jawBone == null)
            return;

        jawClosedRotation = jawBone.localEulerAngles;
    }

    /// <summary>
    /// Compute target "volume" value from audio (0 when idle).
    /// </summary>
    private float GetTargetVolume()
    {
        if (voiceAudioSource == null || !voiceAudioSource.isPlaying)
            return 0f;

        float[] data = new float[256];
        voiceAudioSource.GetOutputData(data, 0);

        float sum = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            sum += Mathf.Abs(data[i]);
        }

        float averageVolume = sum / data.Length;
        return averageVolume * sensitivity;
    }

    /// <summary>
    /// Apply calculated volume to either blendshape or jaw bone.
    /// </summary>
    private void ApplyLipSync(float volume)
    {
        // Strategy 1: Blendshape
        if (characterFaceMesh != null && _blendShapeIndex != -1)
        {
            float weight = Mathf.Clamp(volume * 100f, 0f, 100f);
            characterFaceMesh.SetBlendShapeWeight(_blendShapeIndex, weight);
            return;
        }

        // Strategy 2: Jaw bone
        if (jawBone != null)
        {
            float t = Mathf.Clamp01(volume);
            jawBone.localEulerAngles =
                Vector3.Lerp(jawClosedRotation, jawClosedRotation + jawOpenRotation, t);
        }
    }
}
