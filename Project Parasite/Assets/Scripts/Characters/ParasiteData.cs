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
			if (value <= 0 && !hasSentGameOver) {
                EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
				hasSentGameOver = false;
			}
		}
	}
	
	#endregion

    #region [Private Variables]
    
    bool hasSentGameOver;
    
    #endregion

    #region [Public Methods]

    public ParasiteData() {
		hasSentGameOver = false;
        ParasiteHealth = STARTING_HEALTH;
    }
    
	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}
    
    #endregion

}
