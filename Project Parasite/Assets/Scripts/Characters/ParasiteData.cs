using UnityEngine;
using UnityEngine.UI;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	public static GameObject RegainedHealthPrefab;
	public bool isVamparasite {get; private set;}

	#endregion

    #region [Private Variables]

	int MaxHealth {
		get {
			return isVamparasite ? 175 : 100;
		}
	}
	Vector2 HEALTH_ALERT_OFFSET = new Vector2(0, 0.5f);
	const int HEAL_AMOUNT_DEFAULT = 5;
	const int HEAL_AMOUNT_VAMPARASITE = 25;

	int _parasiteHealth = STARTING_HEALTH;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			bool isTakingDamage = value < _parasiteHealth;
			int oldParasiteHealth = _parasiteHealth;
			_parasiteHealth = Mathf.Clamp(value, 0, MaxHealth);
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
		isVamparasite = false;
        ParasiteHealth = STARTING_HEALTH;
    }
	
	public delegate void DeathHandler(CharacterSpawner spawner);
    
	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}

	public int GetParasiteHealth() {
		return ParasiteHealth;
	}

	public void SetVamparasite() {
		isVamparasite = true;
		Parasite parasite = (Parasite)owner.GetCharacter();
		if (parasite != null) {
			parasite.ChangeToVampColour();
		}
	}

	public void RegainHealthOnKill() {
		ParasiteRegainHealth(isVamparasite ? HEAL_AMOUNT_VAMPARASITE : HEAL_AMOUNT_DEFAULT);
	}
    
    #endregion

	#region [Private Methods]

	void DefaultDeathHandler(CharacterSpawner unused) {
		EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
	}

	void SpawnHealthRegainedAlert(int amountRegained) {
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
    
	void ParasiteRegainHealth(int health) {
		ParasiteHealth += health;
	}
	
	#endregion

}
