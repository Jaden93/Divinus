using UnityEngine;

namespace DivinePrototype
{
    public enum PersonalityTrait
    {
        Standard,
        Courageous, // Approaches events, initiates burial
        Cowardly,   // Flees immediately from smines/death
        Altruistic, // High priority for burial and helping others
        Selfish,    // Ignores social events, prioritizes self
        Devout      // Gains more loyalty from benevolent acts, loses more from cruel ones
    }

    [System.Serializable]
    public class PersonalityData
    {
        public PersonalityTrait primaryTrait;
        public float courageMultiplier = 1.0f;
        public float altruismMultiplier = 1.0f;
        public float loyaltySensitivity = 1.0f;

        public PersonalityData(PersonalityTrait trait)
        {
            primaryTrait = trait;
            switch (trait)
            {
                case PersonalityTrait.Courageous:
                    courageMultiplier = 2.0f;
                    altruismMultiplier = 1.2f;
                    break;
                case PersonalityTrait.Cowardly:
                    courageMultiplier = 0.3f;
                    altruismMultiplier = 0.8f;
                    break;
                case PersonalityTrait.Altruistic:
                    altruismMultiplier = 2.5f;
                    courageMultiplier = 1.2f;
                    break;
                case PersonalityTrait.Selfish:
                    altruismMultiplier = 0.1f;
                    courageMultiplier = 0.8f;
                    break;
                case PersonalityTrait.Devout:
                    loyaltySensitivity = 2.0f;
                    break;
            }
        }
    }
}
