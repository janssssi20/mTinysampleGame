using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class RobotController : MonoBehaviour
{
    public float moveDistance = 1f;
    public float moveSpeed = 2f;

    [Header("Walkable Tiles")]
    public LayerMask walkableLayer;

    private bool isExecuting;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void RunProgram()
    {
        if (!isExecuting)
        {
            StartCoroutine(ExecuteCommands());
        }
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

            yield return new WaitForSeconds(0.2f);
        }

        isExecuting = false;
    }

    IEnumerator MoveForward()
    {
        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + (Vector2)transform.up * moveDistance;

        // ✅ CHECK IF TARGET TILE IS WALKABLE
        Collider2D tile = Physics2D.OverlapPoint(targetPos, walkableLayer);

        if (tile == null)
        {
            Debug.Log("Blocked: not a valid tile!");
            yield break;
        }

        // Smooth movement
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;

            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);
    }

    IEnumerator TurnLeft()
    {
        yield return RotateRobot(90f);
    }

    IEnumerator TurnRight()
    {
        yield return RotateRobot(-90f);
    }

    IEnumerator RotateRobot(float angle)
    {
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0, 0, angle);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;

            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        transform.rotation = targetRot;
    }

    public void StopProgram()
    {
        StopAllCoroutines();
        isExecuting = false;
    }

    private void LateUpdate()
    {
        // keep robot on 2D plane
        Vector3 p = transform.position;
        if (p.z != 0)
        {
            p.z = 0;
            transform.position = p;
        }
    }
}