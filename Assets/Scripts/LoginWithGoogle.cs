using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using Google;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google Configuration")]
    [SerializeField]
    private string webClientId = "585714368332-1kntkjkt83j09pjqs2ll002fe1b7cnb0.apps.googleusercontent.com";

    [Header("UI Objects (Login Screen)")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TextMeshProUGUI errorText; // Yahan error dikhega

    [Header("Game Objects (After Login)")]
    [SerializeField] private GameObject femaleObject;
    [SerializeField] private GameObject playReactionBtn;

    private FirebaseAuth _auth;
    private GoogleSignInConfiguration _configuration;
    private bool _isGoogleSignInInitialized;

    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue) { _executionQueue.Enqueue(action); }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0) _executionQueue.Dequeue().Invoke();
        }
    }

    private void Start()
    {
        SetGameContentActive(false);
        SetLoginPanelActive(true);
        SetErrorMessage("");

        InitFirebase();
    }

    private void InitFirebase()
    {
        _auth = FirebaseAuth.DefaultInstance;
        if (_auth.CurrentUser != null)
        {
            Debug.Log("Auto-Login Success: " + _auth.CurrentUser.Email);
            ShowGameContent();
        }
    }

    public void Login()
    {
        SetErrorMessage("Connecting to Google...");
        Debug.Log("Login Started...");

        EnsureGoogleConfig();

        Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

        signIn.ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                
                Debug.LogWarning("Google Login Cancelled by User");
                Enqueue(() => SetErrorMessage("Login Cancelled"));
                return;
            }

            if (task.IsFaulted)
            {
                //SetLoginPanelActive(false);
                //SetErrorMessage("");
                //SetGameContentActive(true);
                // Yahan asli error pakda jayega
                string errorMsg = "Google Error: " + task.Exception.Flatten().InnerExceptions[0].Message;
                Debug.LogError(errorMsg);

                Enqueue(() => SetErrorMessage(errorMsg)); // Screen par error dikhao
                return;
            }

            // Google Success Check
            if (task.Result == null || string.IsNullOrEmpty(task.Result.IdToken))
            {
                //SetLoginPanelActive(false);
                //SetErrorMessage("");
                //SetGameContentActive(true);
                Debug.LogError("Google Success but ID Token is NULL!");
                Enqueue(() => SetErrorMessage("Google ID Token missing!"));
                return;
            }

            Debug.Log("Google Success! Token received. Connecting to Firebase...");
            Enqueue(() => SetErrorMessage("Verifying with Firebase..."));

            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);

            _auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
            {
                if (authTask.IsCanceled)
                {
                    Debug.LogWarning("Firebase Auth Cancelled");
                    Enqueue(() => SetErrorMessage("Firebase Auth Cancelled"));
                    return;
                }

                if (authTask.IsFaulted)
                {
                    //SetLoginPanelActive(false);
                    //SetErrorMessage("");
                    //SetGameContentActive(true);
                    string fbError = "Firebase Error: " + authTask.Exception.Flatten().InnerExceptions[0].Message;
                    Debug.LogError(fbError);
                    Enqueue(() => SetErrorMessage(fbError));
                    return;
                }

                // Final Success
                Debug.Log("Firebase Login Success! User: " + authTask.Result.DisplayName);
                Enqueue(ShowGameContent);
            });
        });
    }

    private void EnsureGoogleConfig()
    {
        if (_isGoogleSignInInitialized) return;

        _configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true
        };

        GoogleSignIn.Configuration = _configuration;
        _isGoogleSignInInitialized = true;
    }

    private void ShowGameContent()
    {
        SetLoginPanelActive(false);
        SetErrorMessage("");
        SetGameContentActive(true);
    }

    public void SignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        _auth.SignOut();
        SetLoginPanelActive(true);
        SetGameContentActive(false);
    }

    // --- UI Helpers ---
    private void SetLoginPanelActive(bool isActive) { if (loginPanel) loginPanel.SetActive(isActive); }
    private void SetGameContentActive(bool isActive)
    {
        if (femaleObject) femaleObject.SetActive(isActive);
        if (playReactionBtn) playReactionBtn.SetActive(isActive);
    }
    private void SetErrorMessage(string message) { if (errorText) errorText.text = message; }
}