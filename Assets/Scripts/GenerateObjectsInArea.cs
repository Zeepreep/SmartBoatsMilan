using UnityEngine;
    using Random = UnityEngine.Random;
    using System.Collections.Generic;
    
    /// <summary>
    /// Script to generate objects in multiple given areas.
    /// </summary>
    [ExecuteInEditMode]
    public class GenerateObjectsInArea : MonoBehaviour
    {
        [Header("Objects")]
        [SerializeField, Tooltip("Areas to be used where the objects will be created.")]
        private BoxCollider[] areas; // Changed to an array of BoxColliders
        [SerializeField, Tooltip("Possible objects to be created in the areas.")]
        private GameObject[] gameObjectToBeCreated;
    
        [SerializeField, Tooltip("Number of objects to be created.")]
        private uint count;
    
        [Space(10)]
        [Header("Variation")]
        [SerializeField]
        private Vector3 randomRotationMinimal;
        [SerializeField]
        private Vector3 randomRotationMaximal;
    
        /// <summary>
        /// Remove all children objects. Uses DestroyImmediate.
        /// </summary>
        public void RemoveChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; --i)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    
        /// <summary>
        /// Destroy all objects in the areas (that belong to this script) and create them again.
        /// The list of newly created objects is returned.
        /// </summary>
        /// <returns></returns>
       public List<GameObject> RegenerateObjects()
        {
            // Remove all existing children
            for (var i = transform.childCount - 1; i >= 0; --i)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        
            // Calculate total volume of all areas
            float totalVolume = 0f;
            var areaVolumes = new float[areas.Length];
            for (int i = 0; i < areas.Length; i++)
            {
                var area = areas[i];
                areaVolumes[i] = area.bounds.size.x * area.bounds.size.y * area.bounds.size.z;
                totalVolume += areaVolumes[i];
            }
        
            // Generate objects proportionally across areas
            var newObjects = new List<GameObject>();
            for (int i = 0; i < areas.Length; i++)
            {
                var area = areas[i];
                int objectsInArea = Mathf.RoundToInt((areaVolumes[i] / totalVolume) * count);
        
                for (int j = 0; j < objectsInArea; j++)
                {
                    var created = Instantiate(
                        gameObjectToBeCreated[Random.Range(0, gameObjectToBeCreated.Length)],
                        GetRandomPositionInWorldBounds(area),
                        GetRandomRotation()
                    );
                    created.transform.parent = transform;
                    newObjects.Add(created);
                }
            }
        
            return newObjects;
        }
    
        /// <summary>
        /// Gets a random position delimited by the bounds of the selected area.
        /// </summary>
        /// <param name="area">The area to get the random position from.</param>
        /// <returns>Returns a random position in the bounds of the area.</returns>
        private Vector3 GetRandomPositionInWorldBounds(BoxCollider area)
        {
            var randomPoint = new Vector3(
                Random.Range(area.bounds.min.x, area.bounds.max.x),
                transform.position.y,
                Random.Range(area.bounds.min.z, area.bounds.max.z)
            );
            return randomPoint;
        }
    
        /// <summary>
        /// Gets a random rotation (Quaternion) using the randomRotationMinimal and randomRotationMaximal.
        /// </summary>
        /// <returns>Returns a random rotation.</returns>
        private Quaternion GetRandomRotation()
        {
            return Quaternion.Euler(Random.Range(randomRotationMinimal.x, randomRotationMaximal.x),
                Random.Range(randomRotationMinimal.y, randomRotationMaximal.y),
                Random.Range(randomRotationMinimal.z, randomRotationMaximal.z));
        }
    }