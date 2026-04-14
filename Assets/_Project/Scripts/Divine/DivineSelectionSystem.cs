using UnityEngine;

namespace DivinePrototype
{
    public class DivineSelectionSystem : MonoBehaviour
    {
        public static DivineSelectionSystem Instance { get; private set; }

        [Header("Selection Effects")]
        public Light selectionLight;
        public Vector3 lightOffset = new Vector3(0, 5, 0);

        private VillagerController _selectedVillager;

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

            if (selectionLight != null)
            {
                selectionLight.enabled = false;
            }
        }

        public void SelectVillager(VillagerController villager)
        {
            if (_selectedVillager == villager) return;

            if (_selectedVillager != null)
            {
                Deselect();
            }

            _selectedVillager = villager;
            
            if (_selectedVillager != null)
            {
                _selectedVillager.SetSocialState(VillagerController.VillagerState.Selected);
                _selectedVillager.PauseWork();
                
                if (selectionLight != null)
                {
                    selectionLight.enabled = true;
                    selectionLight.transform.SetParent(_selectedVillager.transform);
                    selectionLight.transform.localPosition = lightOffset;
                    selectionLight.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }

                if (OverheadMenuUI.Instance != null)
                {
                    OverheadMenuUI.Instance.Show(_selectedVillager);
                }
            }
        }

        public void Deselect()
        {
            if (_selectedVillager != null)
            {
                _selectedVillager.ResumeWork();
                _selectedVillager = null;
            }

            if (selectionLight != null)
            {
                selectionLight.enabled = false;
                selectionLight.transform.SetParent(this.transform);
            }

            if (OverheadMenuUI.Instance != null)
            {
                OverheadMenuUI.Instance.Hide();
            }
        }
    }
}
