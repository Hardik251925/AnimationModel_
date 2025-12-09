using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
public class SimpleFirebaseLogin : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject loginPanel;
    public Button loginButton;
    public TMP_Text statusTextTMP;

    [Header("Game Objects")]
    public GameObject characterObject;
    public GameObject reactionButton;

    private FirebaseAuth auth;
    private FirebaseApp app; // App ka reference rakhna zaroori hai
    private bool isFirebaseReady = false;

    // Logging for Debug
    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (statusTextTMP) statusTextTMP.text = "ERR: " + logString;
        }
    }

    void Start()
    {
        // UI Reset
        if (loginPanel) loginPanel.SetActive(true);
        if (characterObject) characterObject.SetActive(false);
        if (reactionButton) reactionButton.SetActive(false);
        if (loginButton) loginButton.interactable = false;

        UpdateStatus("Bypassing Config File...");

        // 1. DIRECT APP CREATION (Bina Dependencies Check kiye try karte hain)
        // Ye "Try-Catch" block sabse zaroori hai
        try
        {
            var options = new AppOptions
            {
                AppId = "1:789026766226:android:0a70ea8c82044d1bd8554b",
                ApiKey = "AIzaSyA2GOb6-I35GH4QNrRaKYX_rM5fX5gUEM", // <--- ISKO JSON SE CONFIRM KAR LENA
                ProjectId = "charanim-bb98b"
            };

            // MAGIC LINE: Hum "MyAuthApp" naam se app bana rahe hain.
            // Jab naam dete hain, to ye google-services.json ko ignore kar deta hai.
            app = FirebaseApp.Create(options, "MyAuthApp");

            // Is naye app se Auth nikalo
            auth = FirebaseAuth.GetAuth(app);

            auth.SignOut();
            isFirebaseReady = true;

            if (loginButton) loginButton.interactable = true;
            UpdateStatus("Ready (Bypassed File). Login Now.");
        }
        catch (System.Exception e)
        {
            UpdateStatus("Setup Failed: " + e.Message);
            Debug.LogError(e);
        }
    }

    public void OnLoginClicked()
    {
        if (!isFirebaseReady) return;

        UpdateStatus("Signing in...");
        loginButton.interactable = false;

        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    UpdateStatus("Login Failed.");
                    loginButton.interactable = true;
                    if (task.Exception != null) Debug.LogError(task.Exception);
                });
                return;
            }

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (loginPanel) loginPanel.SetActive(false);
                if (characterObject) characterObject.SetActive(true);
                if (reactionButton) reactionButton.SetActive(true);
            });
        });
    }

    void UpdateStatus(string msg)
    {
        if (statusTextTMP) statusTextTMP.text = msg;
    }
}

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly System.Collections.Generic.Queue<System.Action> _executionQueue = new System.Collections.Generic.Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            GameObject obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue) { _executionQueue.Enqueue(action); }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0) _executionQueue.Dequeue().Invoke();
        }
    }
}

// MainThreadDispatcher helper class neeche wahi purani wali rahegi...