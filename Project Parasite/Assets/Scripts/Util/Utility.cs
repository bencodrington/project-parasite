using UnityEngine;

public static class Utility {
	public static int GetLayerMask(CharacterType type) {
		switch (type) {
			case CharacterType.Hunter: return 1 << LayerMask.NameToLayer("Hunters");
			case CharacterType.NPC: return 1 << LayerMask.NameToLayer("NPCs");
			case CharacterType.Parasite: return 1 << LayerMask.NameToLayer("Parasites");
			default: return -1;
		}
	}

	// Valid keys: [ "character", "obstacle", "energyCenter", "potentialParasite" ]
	public static int GetLayerMask(string key) {
		switch (key) {
			case "character": return 	GetLayerMask(CharacterType.Hunter) +
										GetLayerMask(CharacterType.NPC) +
										GetLayerMask(CharacterType.Parasite);
			case "potentialParasite":	return 	GetLayerMask(CharacterType.NPC) +
												GetLayerMask(CharacterType.Parasite);
			case "obstacle": return 1 << LayerMask.NameToLayer("Obstacles");
			case "energyCenter": return 1 << LayerMask.NameToLayer("EnergyCenters");
			case "energyBeam": return 1 << LayerMask.NameToLayer("EnergyBeams");
			case "npcPathObstacle": return	GetLayerMask("obstacle") +
											GetLayerMask("energyCenter") +
											GetLayerMask("energyBeam");
			default: return -1;
		}
	}

	public static Vector2 ProjectOntoRay2D(Vector2 point, Ray2D ray) {
		// Projection of vector b onto vector a is 	c = ( (a . b) / ||a||^2 ) * a
		// 	Assuming a is unit length, this can be simplified to	 c = (a . b) * a

		// Convert "a", or ray's direction, to a vector of unit length
		Vector2 unitDirection = ray.direction.normalized;
		// Convert "b" (the vector from (0, 0) to the point) to a vector with the same origin as the ray
		Vector2 pointWithRaysOrigin = point - ray.origin;
		// Calculate (a . b) to get the scalar multiple of how many unit a's are necessary to get to c
		float proportion = Vector2.Dot(unitDirection, pointWithRaysOrigin);
		// Multiply by the unit direction to get the displacement from the origin of a, to c
		Vector2 displacement = proportion * unitDirection;
		// Return c in world space
		return ray.origin + displacement;
	}

	public static Vector2 GetMousePos() {
		return Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}

	public static float GetAngleToMouse(Vector2 point) {
		return Vector2.SignedAngle(Vector2.right, GetMousePos() - point);
	}

	public enum Directions {
		Up,
		Down,
		Left,
		Right,
		Null
	}
}
