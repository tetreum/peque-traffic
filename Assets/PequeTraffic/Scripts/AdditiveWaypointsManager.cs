using System.Collections.Generic;
using UnityEngine;

namespace Peque.Traffic
{
    public class AdditiveWaypointsManager : MonoBehaviour {
        public static AdditiveWaypointsManager Instance;

        [HideInInspector]
        public List<WaypointData> waypoints = new List<WaypointData>();

        private void Awake() {
            Instance = this;
        }

        public List<WaypointData> getByPath(int path) {
            List<WaypointData> list = new List<WaypointData>();

            foreach (var entry in waypoints) {
                if (entry.pathId == path) {
                    list.Add(entry);
                }
            }
            return list;
        }

        public WaypointData getByName (string name) {
            foreach (var entry in waypoints) {
                if (entry.name == name) {
                    return entry;
                }
            }
            return null;
        }
    }
}