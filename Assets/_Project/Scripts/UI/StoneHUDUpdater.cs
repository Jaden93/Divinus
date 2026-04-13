using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    public class StoneHUDUpdater : MonoBehaviour
    {
        private Text _woodText;
        private Text _stoneText;

        void Start()
        {
            RefreshReferences();
        }

        public void RefreshReferences()
        {
            var woodRow = GameObject.Find("WoodRow");
            var stoneRow = GameObject.Find("StoneRow");
            
            if (woodRow != null) _woodText = woodRow.transform.Find("ValueText")?.GetComponent<Text>();
            if (stoneRow != null) _stoneText = stoneRow.transform.Find("ValueText")?.GetComponent<Text>();
        }

        void Update()
        {
            if (ResourceManager.Instance == null) return;
            
            if (_woodText == null || _stoneText == null) RefreshReferences();

            if (_woodText != null) 
                _woodText.text = ResourceManager.Instance.wood.count.ToString();
            
            if (_stoneText != null) 
                _stoneText.text = ResourceManager.Instance.stone.count.ToString();
        }
    }
}
