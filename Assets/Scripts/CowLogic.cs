using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CowLogic : AgentLogic
{
    #region Static Variables

    private static readonly float _boxPoints = 2.0f;
    private static readonly float _gasBoxPoints = 4.0f;

    #endregion

    [Header("Cow Variables")] public float startHealth = 100.0f;

    private float _health;
    private Coroutine gasSphereCoroutine;
    private Coroutine gasZoneCoroutine;

    private void Start()
    {
        _health = startHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Box"))
        {
            points += _boxPoints;
            Destroy(other.gameObject);
        }
        else if (other.gameObject.tag.Equals("GasBox"))
        {
            points += _gasBoxPoints;
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("GasSphere"))
        {
            EnterGasZone(); // Notify AgentLogic
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("GasSphere"))
        {
            if (gasSphereCoroutine != null)
            {
                StopCoroutine(gasSphereCoroutine);
                gasSphereCoroutine = null;
            }
            ExitGasZone(); // Notify AgentLogic
        }
    }


    public void TakeDamage(float damage)
    {
        _health -= damage;
        Debug.Log($"{gameObject.name} health: {_health}");
        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }
    
    public float GetHealth()
    {
        return _health;
    }
}
