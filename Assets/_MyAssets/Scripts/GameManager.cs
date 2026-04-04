using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public GameObject player, titleScreenCamera, titleScreenUI, playerUI;
    public FirstPersonController playerController;
    public Cinemachine.CinemachineBrain cinemachineBrain;
    public bool isDebugMode = false;

    public static GameManager Instance { get; private set; }

    public DialogueTrigger tutorialSonarDialogueTrigger;

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
        else
        {
            player.SetActive(true);
            playerUI.SetActive(true);
            titleScreenCamera.SetActive(false);
            titleScreenUI.SetActive(false);
            playerController.canLook = true;
            playerController.canMove = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (isDebugMode)
        {
            if(Keyboard.current[Key.Digit1].wasPressedThisFrame)
            {
                //Set up scene for tutorial sonar minigame
                tutorialSonarDialogueTrigger.TriggerDialogue();
            }
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
