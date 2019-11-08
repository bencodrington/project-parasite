using UnityEngine;
using UnityEngine.UI;

public class ParasiteData
{
	#region [Public Variables]
	
	public const int STARTING_HEALTH = 100;

	public static GameObject RegainedHealthPrefab;
	public static GameObject LostHealthPrefab;
	public bool isVamparasite {get; private set;}

	#endregion

    #region [Private Variables]

	int MaxHealth {
		get {
			return isVamparasite ? 175 : 100;
		}
	}
	Vector2 HEALTH_ALERT_OFFSET = new Vector2(0, 0.75f);
	const int HEAL_AMOUNT_DEFAULT = 5;
	const int HEAL_AMOUNT_VAMPARASITE = 25;

	int _parasiteHealth = STARTING_HEALTH;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			int oldParasiteHealth = _parasiteHealth;
			_parasiteHealth = Mathf.Clamp(value, 0, MaxHealth);
			if (_parasiteHealth == oldParasiteHealth) { return; }
			OnHealthChanged(_parasiteHealth - oldParasiteHealth);
			UiManager.Instance.UpdateHealthObject(_parasiteHealth, _parasiteHealth < oldParasiteHealth, _parasiteHealth > oldParasiteHealth);
			if (value <= 0 && !hasHandledDeath) {
				deathHandler(owner);
				hasHandledDeath = true;
			}
		}
	}
	
    bool hasHandledDeath;

	CharacterSpawner owner;
	DeathHandler deathHandler;
	// CLEANUP: update name to reflect that it's more generic now
	PlatformCalledAlert lostHealthAlert;
	int lostHealthAlertValue;
    
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
		try {
			Parasite parasite = (Parasite)owner.GetCharacter();
			if (parasite != null) {
				parasite.ChangeToVampColour();
			}
		} catch {}
		GameObject mutationAlertPrefab = Resources.Load("MutationAlert") as GameObject;
		GameObject.Instantiate(mutationAlertPrefab,
					owner.GetCharacter().transform.position + (Vector3)HEALTH_ALERT_OFFSET,
					Quaternion.identity);
		UiManager.Instance.ActivateMutation(0);
	}

	public void RegainHealthOnKill() {
		ParasiteRegainHealth(isVamparasite ? HEAL_AMOUNT_VAMPARASITE : HEAL_AMOUNT_DEFAULT);
	}
    
    #endregion

	#region [Private Methods]

	void DefaultDeathHandler(CharacterSpawner unused) {
		EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
	}

	PlatformCalledAlert SpawnHealthChangedAlert(int difference) {
		if (difference == 0) { return null; }
		if (RegainedHealthPrefab == null) {
			// This is the first one to be spawned, so find the prefab
			RegainedHealthPrefab = Resources.Load("RegainedHealthAlert") as GameObject;
		}
		if (LostHealthPrefab == null) {
			// This is the first one to be spawned, so find the prefab
			LostHealthPrefab = Resources.Load("LostHealthAlert") as GameObject;
		}
		GameObject healthAlertObject = GameObject.Instantiate(difference > 0 ? RegainedHealthPrefab : LostHealthPrefab,
					owner.GetCharacter().transform.position + (Vector3)HEALTH_ALERT_OFFSET,
					Quaternion.identity);
		// And set this parasite as it's parent in the hierarchy
		healthAlertObject.transform.SetParent(owner.GetCharacter().transform);
		PlatformCalledAlert healthAlert = healthAlertObject.GetComponentInChildren<PlatformCalledAlert>();
		// Show a '+' if the parasite is regaining health
		healthAlertObject.GetComponentInChildren<Text>().text = (difference > 0 ? "+" : "") + difference + " HP";
		return healthAlert;
	}
    
	void ParasiteRegainHealth(int health) {
		ParasiteHealth += health;
	}

	void OnHealthChanged(int difference) {
		if (difference > 0) {
			SpawnHealthChangedAlert(difference);
		} else if (difference < 0) {
			if (lostHealthAlert == null) {
				lostHealthAlert = SpawnHealthChangedAlert(difference);
				lostHealthAlertValue = difference;
			} else {
				lostHealthAlertValue += difference;
				lostHealthAlert.text.text = (lostHealthAlertValue) + " HP";
				lostHealthAlert.Restart();
			}
		}
	}
	
	#endregion

}
