using System;
            using System.Collections;
            using UnityEngine;
            using Random = UnityEngine.Random;
            
            [RequireComponent(typeof(Rigidbody))]
            public class CowLogic : AgentLogic
            {
                #region Static Variables
            
                private static readonly float _boxPoints = 2.0f;
                private static readonly float _piratePoints = -100.0f;
                private static readonly float _health = 100.0f;
            
                #endregion
            
                [Header("Cow Variables")]
                public float startHealth = 100.0f;
            
                private Coroutine gasSphereCoroutine;
            
                private void Start()
                {
                    startHealth = _health;
                }
            
                private void OnTriggerEnter(Collider other)
                {
                    if (!other.gameObject.tag.Equals("Box")) return;
                    points += _boxPoints;
                    Destroy(other.gameObject);
                }
            
                private void OnCollisionEnter(Collision other)
                {
                    if (other.gameObject.tag.Equals("Enemy"))
                    {
                        points += _piratePoints;
                    }
            
                    if (other.gameObject.tag.Equals("GasSphere"))
                    {
                        if (gasSphereCoroutine == null)
                        {
                            gasSphereCoroutine = StartCoroutine(ReduceHealthOverTime());
                        }
                    }
                }
            
                private void OnCollisionExit(Collision other)
                {
                    if (other.gameObject.tag.Equals("GasSphere"))
                    {
                        if (gasSphereCoroutine != null)
                        {
                            StopCoroutine(gasSphereCoroutine);
                            gasSphereCoroutine = null;
                        }
                    }
                }
            
                private IEnumerator ReduceHealthOverTime()
                {
                    while (true)
                    {
                        startHealth -= 0.5f;
                        yield return new WaitForSeconds(1.0f);
                    }
                }
            }