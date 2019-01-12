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
			AddNewItem(menuItemPrefab);
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

	public GameObject AddNewItem(GameObject itemPrefab) {
		// Instantiate item
		GameObject item = Instantiate(itemPrefab);
		// Set it as a child of the menu 
		item.transform.SetParent(transform);
		// Add it to the list of items to remove on transition
		menuItems.Add(item);
		return item;
	}

	public GameObject AddNewItemAtIndex(GameObject itemPrefab, int index) {
		GameObject item = AddNewItem(itemPrefab);
		item.transform.SetSiblingIndex(index);
		return item;
	}

	public void RemoveMenuItem(GameObject item) {
		menuItems.Remove(item);
	}
}
