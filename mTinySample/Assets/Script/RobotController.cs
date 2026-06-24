using UnityEngine;
using System.Collections;

public class RobotController : MonoBehaviour
{
    public float moveDistance = 1f;
    public float moveSpeed = 2f;

    private bool isExecuting;

    public void RunProgram()
    {
        if (!isExecuting)
            StartCoroutine(ExecuteCommands());
    }

    IEnumerator ExecuteCommands()
    {
        isExecuting = true;

        foreach (Command cmd in CommandManager.Instance.commandList)
        {
            switch (cmd.commandType)
            {
                case CommandType.Forward:
                    yield return MoveForward();
                    break;

                case CommandType.TurnLeft:
                    yield return TurnLeft();
                    break;

                case CommandType.TurnRight:
                    yield return TurnRight();
                    break;
            }

            yield return new WaitForSeconds(0.3f);
        }

        isExecuting = false;
    }

    IEnumerator MoveForward()
    {
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + (Vector2)transform.up * moveDistance;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;

            transform.position = Vector2.Lerp(startPos, targetPos, t);

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator TurnLeft()
    {
        Quaternion startRot =
            transform.rotation;

        Quaternion targetRot =
            transform.rotation *
            Quaternion.Euler(0, 0, 90);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;

            transform.rotation =
                Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }
    }

    IEnumerator TurnRight()
    {
        Quaternion startRot =
            transform.rotation;

        Quaternion targetRot =
            transform.rotation *
            Quaternion.Euler(0, 0, -90);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;

            transform.rotation =
                Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }
    }

    public void StopProgram()
    {
        StopAllCoroutines();
        isExecuting = false;
    }
}