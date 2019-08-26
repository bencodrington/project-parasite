using UnityEngine;
using UnityEngine.UI;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	public static GameObject RegainedHealthPrefab;

	#endregion

    #region [Private Variables]

	const int MAX_HEALTH = 100;
	Vector2 HEALTH_ALERT_OFFSET = new Vector2(0, 0.5f);

	int _parasiteHealth = STARTING_HEALTH;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			bool isTakingDamage = value < _parasiteHealth;
			int oldParasiteHealth = _parasiteHealth;
			_parasiteHealth = Mathf.Clamp(value, 0, MAX_HEALTH);
			bool isRegainingHealth = _parasiteHealth > oldParasiteHealth;
			if (isRegainingHealth) {
				SpawnHealthRegainedAlert(_parasiteHealth - oldParasiteHealth);
			}
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

	public void SpawnHealthRegainedAlert(int amountRegained) {
		if (amountRegained <= 0) { return; }
		if (RegainedHealthPrefab == null) {
			// This is the first one to be spawned, so find the prefab
			RegainedHealthPrefab = Resources.Load("RegainedHealthAlert") as GameObject;
		}
		GameObject regainedHealthAlert = GameObject.Instantiate(RegainedHealthPrefab,
					owner.GetCharacter().transform.position + (Vector3)HEALTH_ALERT_OFFSET,
					Quaternion.identity);
		// And set this parasite as it's parent in the hierarchy
		regainedHealthAlert.transform.SetParent(owner.GetCharacter().transform);
		regainedHealthAlert.GetComponentInChildren<Text>().text = "+" + amountRegained;
	}
	
	#endregion

}
