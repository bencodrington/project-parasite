using UnityEngine;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	int _parasiteHealth;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			bool isTakingDamage = false;
			if (value < _parasiteHealth) {
				isTakingDamage = true;
			}
			_parasiteHealth = Mathf.Clamp(value, 0, STARTING_HEALTH);
			UiManager.Instance.UpdateHealthObject(_parasiteHealth, isTakingDamage);
			if (value <= 0 && !hasHandledDeath) {
				deathHandler(owner);
				hasHandledDeath = true;
			}
		}
	}
	
	#endregion

    #region [Private Variables]
    
    bool hasHandledDeath;

	CharacterSpawner owner;
	DeathHandler deathHandler;
    
    #endregion

    #region [Public Methods]

    public ParasiteData(CharacterSpawner owner, DeathHandler deathHandler = null) {
		this.owner = owner;
		if (deathHandler == null) {
			deathHandler = DefaultDeathHandler;
		}
		this.deathHandler = deathHandler;
		hasHandledDeath = false;
        ParasiteHealth = STARTING_HEALTH;
    }
	
	public delegate void DeathHandler(CharacterSpawner spawner);
    
	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}
    
    #endregion

	#region [Private Methods]

	void DefaultDeathHandler(CharacterSpawner unused) {
		EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
	}
	
	#endregion

}
