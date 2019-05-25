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
        interact
    }
    
    #endregion

    protected Dictionary<Key, bool> oldState;
    protected Dictionary<Key, bool> state;

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
        return state[key];
    }

    public bool isJustPressed(Key key) {
        return state[key] && !oldState[key];
    }

    public bool isJustReleased(Key key) {
        return !state[key] && oldState[key];
    }

    public bool ShouldCameraFollowOwner() {
        return shouldCameraFollowOwner;
    }

    public virtual void SetOwner(Character owner) {
        this.owner = owner;
    }
    
    #endregion

    protected Dictionary<Key, bool> NewState() {
        Dictionary<Key, bool> newState = new Dictionary<Key, bool>();
        foreach(Key key in (Key[])Enum.GetValues(typeof(Key)))  {
            newState.Add(key, false);
        }
        return newState;
    }
}
