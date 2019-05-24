using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput
{
    #region [Public Variables]

    public enum InputKey {
        right,
        left,
        up,
        down,
        action1,
        action2,
        interact
    }

    #endregion

    #region [Private Variables]
    
    Dictionary<InputKey, bool> oldState;
    Dictionary<InputKey, bool> state;
    
    #endregion

    #region [Public Methods]

    public PlayerInput() {
        // Initialize state so that oldState isn't assigned a null reference 
        state = NewPlayerInputState();
    }

    public void UpdateInputState() {
        /// Set old input state to point to current input state
        oldState = state;
        /// Get current input state
        state = NewPlayerInputState();
		// Movement
		state[InputKey.right] = Input.GetKey(KeyCode.D);
		state[InputKey.left] = Input.GetKey(KeyCode.A);
		state[InputKey.up] = Input.GetKey(KeyCode.W);
		state[InputKey.down] = Input.GetKey(KeyCode.S);
		state[InputKey.action1] = Input.GetMouseButton(0);
		state[InputKey.action2] = Input.GetMouseButton(1);
		state[InputKey.interact] = Input.GetKey(KeyCode.E);
    }

    public bool isDown(InputKey key) {
        return state[key];
    }

    public bool isJustPressed(InputKey key) {
        return state[key] && !oldState[key];
    }

    public bool isJustReleased(InputKey key) {
        return !state[key] && oldState[key];
    }

    #endregion

    #region [Private Methods]
    
    Dictionary<InputKey, bool> NewPlayerInputState() {
        Dictionary<InputKey, bool> newState = new Dictionary<InputKey, bool>();
        foreach(InputKey key in (KeyCode[])Enum.GetValues(typeof(InputKey)))  {
            newState.Add(key, false);
        }
        return newState;
    }
    
    #endregion

}
