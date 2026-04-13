using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Rappresenta una risorsa fisica (es. un cubo di legno/pietra) a terra
    /// che può essere raccolta da un villager.
    /// </summary>
    public class ResourcePickup : MonoBehaviour
    {
        public string resourceType = "Wood";
        public int amount = 1;
        
        private bool _isCollected = false;
        private VillagerController _claimer; // Il villager che ha "prenotato" la risorsa

        public void Initialize(string type, int qty)
        {
            resourceType = type;
            amount = qty;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = 1f;
                rb.angularDamping = 1f;
                rb.AddForce(Vector3.up * 2f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Restituisce true se la risorsa non è ancora stata prenotata da nessuno.
        /// </summary>
        public bool CanBeClaimed()
        {
            return !_isCollected && _claimer == null;
        }

        /// <summary>
        /// Un villager prenota questa risorsa.
        /// </summary>
        public bool Claim(VillagerController villager)
        {
            if (!CanBeClaimed()) return false;
            _claimer = villager;
            return true;
        }

        public void Unclaim()
        {
            _claimer = null;
        }

        public bool Collect(VillagerController villager)
        {
            if (_isCollected) return false;
            
            // Permette la raccolta solo se il villager è il claimer o se nessuno ha prenotato
            if (_claimer != null && _claimer != villager) return false;

            _isCollected = true;

            if (resourceType == "Wood")
                villager.ReceiveResource("Wood", amount);
            else if (resourceType == "Stone")
                villager.ReceiveResource("Stone", amount);

            if (FloatingTextSpawner.Instance != null)
            {
                Color c = resourceType == "Stone" ? Color.gray : new Color(0.6f, 0.4f, 0.2f);
                FloatingTextSpawner.Instance.Spawn($"Picked up {resourceType}", transform.position + Vector3.up, c);
            }

            Destroy(gameObject);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndCollect(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckAndCollect(collision.gameObject);
        }

        private void CheckAndCollect(GameObject obj)
        {
            if (_isCollected) return;
            var villager = obj.GetComponent<VillagerController>();
            if (villager != null)
            {
                Collect(villager);
            }
        }
    }
}
