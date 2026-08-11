using System.Collections;
using UnityEngine;

public class GoalZone : MonoBehaviour
{
    private CommandManager commandManager;

    private void Start()
    {
        commandManager = FindObjectOfType<CommandManager>();

        if (commandManager == null)
        {
            Debug.LogError("CommandManager not found!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(
            $"Goal Triggered By: {other.name} | Tag: {other.tag}"
        );

        if (other.CompareTag("Robot"))
        {
            Debug.Log("YOU WIN!");

            if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.ReachFinish();
                }

            //StartCoroutine(finishPanelShow());
        }
    }

    public IEnumerator finishPanelShow()
    {
        yield return new WaitForSeconds(1f);

        if (commandManager != null && commandManager.finishPanel != null)
        {
            commandManager.finishPanel.SetActive(true);
        }
    }
}