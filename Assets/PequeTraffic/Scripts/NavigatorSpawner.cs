using System.Collections;
using UnityEngine;

namespace Peque.Traffic
{ 
    public class NavigatorSpawner : MonoBehaviour
    {
        public enum Direction
        {
            Both = 2,
            Normal = 0,
            Inverse = 1,
        }
        public GameObject[] prefabs;
        public int numberToSpawn = 5;
        public Direction allowedDirection = Direction.Both;

        void Start() {
            StartCoroutine(spawn());
        }

        IEnumerator spawn () {
            int count = 0;

            while (count < numberToSpawn) {
                GameObject obj = Instantiate(prefabs[Random.Range(0, prefabs.Length)]);
                Transform child = transform.GetChild(Random.Range(0, transform.childCount -1));
                
                obj.transform.position = child.position;

                int direction;

                if (allowedDirection == Direction.Both) {
                    direction = Mathf.RoundToInt(Random.Range(0f, 1f));
                } else {
                    direction = (int)allowedDirection;
                }

                obj.GetComponent<WaypointNavigator>().init(direction, child.GetComponent<Waypoint>());

                yield return new WaitForEndOfFrame();
                count++;
            }
        }
    }
}