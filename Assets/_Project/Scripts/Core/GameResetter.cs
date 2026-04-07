using UnityEngine;
using UnityEngine.SceneManagement;

namespace DivinePrototype
{
    /// <summary>
    /// Reset rapido del loop: ricarica la scena attiva.
    /// In editor: click destro sul componente → "Reset Game".
    /// In gioco: chiama ResetGame() da qualunque sistema.
    /// </summary>
    public class GameResetter : MonoBehaviour
    {
        [ContextMenu("Reset Game")]
        public void ResetGame()
        {
            Debug.Log("[GameResetter] Reset loop.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
