using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{
    public class SemaphoreManager : MonoBehaviour
    {
        public static SemaphoreManager Instance;

        [HideInInspector]
        public List<Semaphore> semaphores = new List<Semaphore>();
        private void Awake() {
            Instance = this;
        }

        private void Start() {
            StartCoroutine(changeStatus());
        }

        IEnumerator changeStatus() {
            while (true) {
                foreach (var semaphore in semaphores) {
                    semaphore.remainingSeconds--;

                    if (semaphore.remainingSeconds == 0) {
                        semaphore.changeStatus();
                    }
                }

                yield return new WaitForSeconds(1);
            }
        }
    }
}