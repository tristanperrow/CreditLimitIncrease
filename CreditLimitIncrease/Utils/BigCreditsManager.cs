using BreakInfinity;
using UnityEngine;

namespace CreditLimitIncrease.Utils
{
    /// <summary>
    /// Static utilities class for common functions and properties to be used within your mod code
    /// </summary>
    public class BigCreditsManager
    {
        public static BigCreditsManager Instance { get; private set; } // Singleton

        public PlayerGarden gardenInstance;
        public BigDouble credits = 0;

        public Animator anim;
        public ParticleSystem particles;

        public BigCreditsManager(int credits) {
            /*
            if (Instance != null)
                return;
            */
            Instance = this;
            if (credits < 0)
                this.credits = int.MaxValue;
            else
                this.credits = credits;
        }

        public void AddCredits(BigDouble amount)
        {
            // Get Upgrades
            amount += amount * 0.1f * UpgradesData.instance.GetUpgrade_Int(2);
            amount += amount * StatusManager.instance.GetEffectState_Float(6);
            if (UnityEngine.Random.value < ArtifactsManager.instance.GetArtifactEfficiency(11))
            {
                amount *= 2;
            }
            // Produce Output
            credits += amount;
            gardenInstance.onPickupCreditsEvent.Invoke();
            gardenInstance.UpdateCreditsText();
            gardenInstance.cellsAnim.Play("CellCounter_Pop", -1, 0f);
            gardenInstance.creditsParticles.Play();
        }

        public void RemoveCredits(BigDouble amount)
        {
            credits -= amount;
            gardenInstance.UpdateCreditsText();
            gardenInstance.cellsAnim.Play("CellCounter_Pop", -1, 0f);
        }

        public void UpdateCreditsText()
        {
            gardenInstance.onPickupCreditsEvent.Invoke();
            gardenInstance.UpdateCreditsText();
            gardenInstance.cellsAnim.Play("CellCounter_Pop", -1, 0f);
            gardenInstance.creditsParticles.Play();
        }
    }
}
