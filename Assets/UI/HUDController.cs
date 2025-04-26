using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Charge Meter")]
    
    [SerializeField] private Image chargeMeterFill;
    
    [Header("Speedometer")]
    
    [SerializeField] private TMP_Text speedometerText;
    
    [Header("Debug")]

    [SerializeField] private TMP_Text debugText;

    [Header("Pause Menu")] 
    
    [SerializeField] private GameObject pauseMenu;
    
    [SerializeField] private GameObject resumeArrow;

    [SerializeField] private GameObject quitArrow;

    [Header("Dependencies")] 
    
    [SerializeField] private PlayerCharacterController player;
    
    //-------------------------------------------------------------------------

    private InputAction _pauseAction;
    private InputAction _navigateAction;
    private InputAction _submitAction;

    private bool _quitSelected;

    public bool isPaused;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        chargeMeterFill.type = Image.Type.Filled;
        chargeMeterFill.fillMethod = Image.FillMethod.Vertical;
        
        _pauseAction = InputSystem.actions.FindAction("Pause");
        _navigateAction = InputSystem.actions.FindAction("Navigate");
        _submitAction = InputSystem.actions.FindAction("Submit");
    }

    private void Update()
    {
        bool pause = _pauseAction.triggered;

        if (pause)
        {
            TogglePause();
        }

        bool navigateUp = _navigateAction.ReadValue<Vector2>().y > 0f;
        bool navigateDown = _navigateAction.ReadValue<Vector2>().y < 0f;

        if (navigateUp)
        {
            SelectResume(true);
        } 
        else if (navigateDown)
        {
            SelectResume(false);
        }

        if (_submitAction.triggered && isPaused)
        {
            if (_quitSelected)
            {
                Debug.Log("Quit");
                Application.Quit();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void SelectResume(bool resume)
    {
        resumeArrow.SetActive(resume);
        quitArrow.SetActive(!resume);
        _quitSelected = !resume;
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenu.SetActive(isPaused);
        SelectResume(true);
    }

    public void DebugDisplay(string text)
    {
        debugText.text = text;
    }

    public void UpdateChargeDisplay(float charge)
    {
        chargeMeterFill.fillAmount = charge;
    }

    public void UpdateSpeedometerText(string text)
    {
        speedometerText.text = text;
    }
}
