using UnityEngine;
using UnityEngine.SceneManagement;

namespace Peque.Traffic.Demo { 
    public class LoadAdditiveScene : MonoBehaviour
    {
        private void Start() {
            SceneManager.LoadSceneAsync("AddedScene", LoadSceneMode.Additive);
        }
    }
}