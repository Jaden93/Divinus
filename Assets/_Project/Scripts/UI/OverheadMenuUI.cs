using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    public class OverheadMenuUI : MonoBehaviour
    {
        public static OverheadMenuUI Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject container;
        public Text thoughtText;
        public Button infoButton;
        public Button powerButton;
        public Button closeButton;

        [Header("Offset")]
        public Vector3 offset = new Vector3(0, 3.0f, 0);

        private VillagerController _currentTarget;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (container != null)
            {
                container.SetActive(false);
            }
        }

        private void Update()
        {
            if (_currentTarget != null && container != null && container.activeSelf)
            {
                transform.position = _currentTarget.transform.position + offset;
                
                // Billboard to camera (ignore X rotation to keep vertical)
                if (Camera.main != null)
                {
                    transform.rotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
                }
            }
        }

        public void Show(VillagerController villager)
        {
            _currentTarget = villager;
            if (thoughtText != null)
            {
                thoughtText.text = villager.GetCurrentThought();
                thoughtText.raycastTarget = false; // Non blocca il Dio
            }
            
            if (container != null)
            {
                container.SetActive(true);
                
                // Pulisce le immagini placeholder (i quadrati bianchi)
                var images = container.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    // Se non ha uno sprite, lo rendiamo invisibile o trasparente
                    if (img.sprite == null)
                    {
                        img.color = new Color(1, 1, 1, 0.1f); 
                    }
                    
                    // Se non è un bottone, non deve bloccare il raycast
                    if (img.GetComponent<Button>() == null)
                    {
                        img.raycastTarget = false;
                    }
                }
            }
        }

        public void Hide()
        {
            _currentTarget = null;
            if (container != null)
            {
                container.SetActive(false);
            }
        }

        // Placeholder methods for buttons (to be linked in Editor)
        public void OnInfoClick() { Debug.Log($"Info for {(_currentTarget != null ? _currentTarget.name : "None")}"); }
        public void OnPowerClick() { Debug.Log($"Divine Power on {(_currentTarget != null ? _currentTarget.name : "None")}"); }
        public void OnCloseClick() { DivineSelectionSystem.Instance.Deselect(); }
    }
}
