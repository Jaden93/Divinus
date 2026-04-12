using UnityEngine;
using UnityEngine.SceneManagement;

namespace DivinePrototype
{
    public class SceneRefreshButton : MonoBehaviour
    {
        public void RefreshScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
