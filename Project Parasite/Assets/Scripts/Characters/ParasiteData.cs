using UnityEngine;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	int _parasiteHealth;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			_parasiteHealth = Mathf.Clamp(value, 0, STARTING_HEALTH);
			UiManager.Instance.UpdateHealthObject(_parasiteHealth);
			if (value <= 0 && !hasHandledDeath) {
				deathHandler();
				hasHandledDeath = true;
			}
		}
	}
	
	#endregion

    #region [Private Variables]
    
    bool hasHandledDeath;

	DeathHandler deathHandler;
    
    #endregion

    #region [Public Methods]

    public ParasiteData(DeathHandler deathHandler = null) {
		if (deathHandler == null) {
			deathHandler = DefaultDeathHandler;
		}
		this.deathHandler = deathHandler;
		hasHandledDeath = false;
        ParasiteHealth = STARTING_HEALTH;
    }
	
	public delegate void DeathHandler();
    
	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}
    
    #endregion

	#region [Private Methods]

	void DefaultDeathHandler() {
		EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
	}
	
	#endregion

}
