using UnityEngine;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	#endregion

    #region [Private Variables]

	const int MAX_HEALTH = 150;

	int _parasiteHealth;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			bool isTakingDamage = value < _parasiteHealth;
			bool isRegainingHealth = value > _parasiteHealth;
			_parasiteHealth = Mathf.Clamp(value, 0, MAX_HEALTH);
			UiManager.Instance.UpdateHealthObject(_parasiteHealth, isTakingDamage, isRegainingHealth);
			if (value <= 0 && !hasHandledDeath) {
				deathHandler(owner);
				hasHandledDeath = true;
			}
		}
	}
	
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
    
	public void ParasiteRegainHealth(int health) {
		ParasiteHealth += health;
	}

	public int GetParasiteHealth() {
		return ParasiteHealth;
	}
    
    #endregion

	#region [Private Methods]

	void DefaultDeathHandler(CharacterSpawner unused) {
		EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
	}
	
	#endregion

}
