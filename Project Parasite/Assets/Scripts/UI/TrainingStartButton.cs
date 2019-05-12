using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingStartButton : MonoBehaviour
{
    public enum CharacterTrainingOptions {
        Parasite,
        Hunter
    }

    public CharacterTrainingOptions characterType;

    public void OnClick() {
        CharacterType type = characterType == CharacterTrainingOptions.Parasite ?
                    CharacterType.Parasite :
                    CharacterType.Hunter;
        MatchManager.Instance.StartTutorial(type);
    }
}
