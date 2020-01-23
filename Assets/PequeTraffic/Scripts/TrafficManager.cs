using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic {
    public class TrafficManager : MonoBehaviour
    {
        public static TrafficManager Instance;

        public Dictionary<int, VehicleNavigation> vehicles;
        public List<CharacterNavigationController> pedestrians;

        private SensorsJob sensorsJob;
        private bool sensors = false;

        private void Awake() {
            if (TrafficManager.Instance != null) {
                // There must only exist one TrafficManager
                Destroy(this);
                return;
            }

            vehicles = new Dictionary<int, VehicleNavigation>();
            pedestrians = new List<CharacterNavigationController>();
            sensorsJob = GetComponent<SensorsJob>();

            Instance = this;
        }

        private void Update() {
            if (sensors && sensorsJob.ready) {
                sensorsJob.start();
            }
        }

        public void add (VehicleNavigation vehicle) {
            vehicles.Add(vehicle.GetInstanceID(), vehicle);

            if (!sensors) {
                sensors = true;
                sensorsJob.start();
            }
        }

        public void delete (VehicleNavigation vehicle) {
            vehicles.Remove(vehicle.GetInstanceID());
        }

        public void add(CharacterNavigationController pedestrian) {
            pedestrians.Add(pedestrian);
        }

        public void delete(CharacterNavigationController pedestrian) {
            pedestrians.Remove(pedestrian);
        }
    }
}