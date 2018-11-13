using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour {

	public MenuItemSet startingMenuItemSet;
	private MenuItemSet menuItemSet;
	private List<GameObject> menuItems;

	// Use this for initialization
	void Start () {
		menuItems = new List<GameObject>();
		TransitionToNewMenuItemSet(startingMenuItemSet);
	}

	void InitializeMenuItems() {
		foreach (GameObject menuItemPrefab in menuItemSet.menuItemPrefabs) {
			menuItems.Add(Instantiate(menuItemPrefab, Vector3.zero, Quaternion.identity, transform));
		}
	}

	public void DeleteMenuItems() {
		foreach (GameObject menuItem in menuItems) {
			Destroy(menuItem);
		}
		menuItems.Clear();
	}

	public void TransitionToNewMenuItemSet(MenuItemSet newMenuItemSet) {
		DeleteMenuItems();
		menuItemSet = newMenuItemSet;
		InitializeMenuItems();
	}
}
