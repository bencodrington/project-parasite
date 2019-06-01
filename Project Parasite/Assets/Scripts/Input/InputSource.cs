using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputSource
{
    #region [Public Variables]

    public enum Key {
        right,
        left,
        up,
        down,
        action1,
        action2,
        interact,
        jump
    }
    
    #endregion

    protected struct InputState {
        public Dictionary<Key, bool> keyState;
        public Vector2 mousePosition;
    }

    protected InputState oldState;
    protected InputState state;

    protected bool shouldCameraFollowOwner = false;

    protected Character owner;

    #region [Public Methods]

    public InputSource() {
        // Initialize state so that oldState isn't assigned a null reference 
        state = NewState();
    }
    
    public virtual void UpdateInputState() {
        // Set old input state to point to current input state
        oldState = state;
        // Get current input state
        state = NewState();
    }

    public bool isDown(Key key) {
        return state.keyState[key];
    }

    public bool isJustPressed(Key key) {
        return state.keyState[key] && !oldState.keyState[key];
    }

    public bool isJustReleased(Key key) {
        return !state.keyState[key] && oldState.keyState[key];
    }

    public bool ShouldCameraFollowOwner() {
        return shouldCameraFollowOwner;
    }

    public virtual void SetOwner(Character owner) {
        this.owner = owner;
    }

    public Vector2 getMousePosition() {
        return state.mousePosition;
    }
    
    #endregion

    protected InputState NewState() {
        InputState newState = new InputState();
        newState.keyState = new Dictionary<Key, bool>();
        foreach(Key key in (Key[])Enum.GetValues(typeof(Key)))  {
            newState.keyState.Add(key, false);
        }
        newState.mousePosition = Vector2.zero;
        return newState;
    }
}
