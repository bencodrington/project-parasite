﻿using UnityEngine;

public static class Utility {
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
}