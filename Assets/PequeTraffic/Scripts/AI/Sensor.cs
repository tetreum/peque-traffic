using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Peque.Traffic {

    public class Sensor : MonoBehaviour
    {
        public enum Element
        {
            None = 0,
            Vehicle = 1,
            Person = 2,
        }
        public Element detectedElementType {
            get {
                if (detectedElement == null) {
                    return Element.None;
                }

                return getElementType(detectedElement);
            }
        }
        public Transform detectedElement;

        /*
        public Element detectedElementType {
            get {
                if (collisions.Count == 0) {
                    return Element.None;
                }

                return getElementType(collisions.First().Value);
            }
        }
        
        public Transform detectedElement {
            get {
                return collisions.First().Value;
            }
        }

        private Dictionary<int, Transform> collisions = new Dictionary<int, Transform>();

        private void OnDisable() {
            // clean collisions
            collisions = new Dictionary<int, Transform>();
        }
        
        private void OnTriggerStay(Collider other) {
            if (!enabled) {
                return;
            }
            int id = other.transform.root.GetInstanceID();

            if (collisions.ContainsKey(id) || getElementType(other.transform.root) == Element.None) {
                return;
            }

            collisions.Add(id, other.transform.root);
        }

        private void OnTriggerExit(Collider other) {
            if (!enabled) {
                return;
            }
            try {
                collisions.Remove(other.transform.root.GetInstanceID());
            } catch (System.Exception) {}
        }
        */
        private Element getElementType(Transform root) {
            switch (root.tag) {
                case "Vehicle":
                    return Element.Vehicle;
                case "Person":
                    return Element.Person;
            }

            return Element.None;
        }
    }
}