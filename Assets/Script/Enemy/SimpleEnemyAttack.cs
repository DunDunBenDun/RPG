using UnityEngine;

[DisallowMultipleComponent]
public class SimpleEnemyAttack : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private bool faceTarget = true;

    private float nextAttackTime;

    private void Awake()
    {
        if (target == null)
        {
            GameObject kazuko = GameObject.Find("Kazuko");
            if (kazuko != null)
            {
                target = kazuko.transform;
            }
        }
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        if (faceTarget)
        {
            FaceTarget();
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange)
        {
            Attack(distance);
            nextAttackTime = Time.time + attackInterval;
        }
    }

    public void Configure(Transform newTarget, float newAttackRange, float newAttackInterval)
    {
        target = newTarget;
        attackRange = newAttackRange;
        attackInterval = newAttackInterval;
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Attack(float distance)
    {
        Debug.Log($"{name} attacks {target.name}. Distance: {distance:F2}");
    }
}
