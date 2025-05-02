using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasSphere : MonoBehaviour
{
    private HashSet<CowLogic> cowsInSphere = new HashSet<CowLogic>();
    private Coroutine damageCoroutine;
    
    
    [Header("Gas Sphere Variables")]
    public float damagePerSecond = 20f;

    private void Start()
    {
        CheckCowsInSphere();
    }

    public void CheckCowsInSphere()
    {
        float radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var collider in colliders)
        {
            CowLogic cow = collider.GetComponentInChildren<CowLogic>();
            if (cow != null && !cowsInSphere.Contains(cow))
            {
                cowsInSphere.Add(cow);
                if (damageCoroutine == null)
                {
                    damageCoroutine = StartCoroutine(DamageCowsOverTime());
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CowLogic cow = other.GetComponent<CowLogic>();
        if (cow != null)
        {
            cowsInSphere.Add(cow);
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageCowsOverTime());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CowLogic cow = other.GetComponent<CowLogic>();
        if (cow != null)
        {
            cowsInSphere.Remove(cow);
            if (cowsInSphere.Count == 0 && damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageCowsOverTime()
    {
        while (cowsInSphere.Count > 0)
        {
            foreach (var cow in new List<CowLogic>(cowsInSphere))
            {
                if (cow != null)
                {
                    cow.TakeDamage(damagePerSecond);
                    if (cow.GetPoints() <= 0)
                    {
                        Debug.Log($"{cow.name} is dying in the gas zone!");
                    }
                }
            }

            yield return new WaitForSeconds(1.0f);
        }
    }
}
