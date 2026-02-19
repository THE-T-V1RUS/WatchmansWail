using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player, titleScreenCamera, titleScreenUI, playerUI;
    public Cinemachine.CinemachineBrain cinemachineBrain;
    public bool isDebugMode = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if(!isDebugMode)
        {
            player.SetActive(false);
            playerUI.SetActive(false);
            titleScreenCamera.SetActive(true);
            titleScreenUI.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    public void SetCameraTransitionSpeed(float speed)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Time = speed;
        }
    }

    public void SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style style)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Style = style;
        }
    }

    // Wrapper methods for Unity Events (which can't use complex enum types)
    public void SetCameraTransitionStyle_EaseInOut()
    {
        SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style.EaseInOut);
    }

    public void SetCameraTransitionStyle_EaseIn()
    {
        SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style.EaseIn);
    }

    public void SetCameraTransitionStyle_EaseOut()
    {
        SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style.EaseOut);
    }

    public void SetCameraTransitionStyle_Linear()
    {
        SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style.Linear);
    }

    public void SetCameraTransitionStyle_Cut()
    {
        SetCameraTransitionType(Cinemachine.CinemachineBlendDefinition.Style.Cut);
    }
}
