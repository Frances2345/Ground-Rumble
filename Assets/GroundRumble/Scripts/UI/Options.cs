using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Options : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    [Header("Menú de Pausa")]
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    [Header("Pantalla de Muerte")]
    public Image blackFadePanel;
    public float slowMotionFactor = 0.2f;
    public float slowMotionDuration = 1.5f;
    public float fadeDuration = 2f;
    public string gameOverSceneName = "GameOver";

    private bool isDead = false;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.PauseGame.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.PauseGame.performed -= OnPausePerformed;
        inputActions.Player.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (blackFadePanel != null)
        {
            Color c = blackFadePanel.color;
            c.a = 0f;
            blackFadePanel.color = c;
            blackFadePanel.gameObject.SetActive(false);
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ResetScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TriggerDeathSequence()
    {
        if (isDead) return;
        isDead = true;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;

        yield return new WaitForSecondsRealtime(slowMotionDuration);

        if (blackFadePanel != null)
        {
            blackFadePanel.gameObject.SetActive(true);
            float elapsedTime = 0f;
            Color panelColor = blackFadePanel.color;

            panelColor.r = 0f; panelColor.g = 0f; panelColor.b = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                panelColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
                blackFadePanel.color = panelColor;
                yield return null;
            }

            panelColor.a = 1f;
            blackFadePanel.color = panelColor;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(gameOverSceneName);
    }
}