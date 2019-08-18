using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarField : MonoBehaviour
{
    #region [Public Variables]
    
    public Sprite[] starSprites;
    public Color[] starColours;
    public Vector2 mapMinCoordinates;
    public Vector2 mapMaxCoordinates;

    [Range(1, 1000)]
    public int numberOfStars;

    [Range(0, 10)]
    public float starSpeedPerSecond;
    
    #endregion

    #region [Private Variables]
    
    GameObject[] stars;
    int[] starDistances;
    Vector2 distanceBetweenStars;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    // Start is called before the first frame update
    void Start() {
        SpawnStars();
    }

    // Update is called once per frame
    void Update() {
        float xSpeed;
        for (int i = 0; i < numberOfStars; i++) {
            xSpeed = SpeedMultiplierFromZDistance(starDistances[i])
                * -starSpeedPerSecond
                * Time.deltaTime;
            stars[i].transform.position += new Vector3(xSpeed, 0, 0);
            if (stars[i].transform.position.x < mapMinCoordinates.x) {
                ResetStar(i);
            }
        }
    }
    
    #endregion

    #region [Private Methods]
    
    void SpawnStars() {
        distanceBetweenStars = (mapMaxCoordinates - mapMinCoordinates) / numberOfStars;
        stars = new GameObject[numberOfStars];
        starDistances = new int[numberOfStars];
        Vector2 starPosition;
        float size;
        for (int i = 0; i < numberOfStars; i++) {
            starPosition = new Vector2(
                mapMinCoordinates.x + i * distanceBetweenStars.x,
                GetRandomStarYCoordinate()
                );
            starDistances[i] = Random.Range(1, 100);
            size = (100 - starDistances[i]) / 100f; // The farther the star is, the smaller it is
            stars[i] = SpawnStar(starPosition, size);
        }
    }

    GameObject SpawnStar(Vector2 starPosition, float size) {
        GameObject star;
        SpriteRenderer starSpriteRenderer;
        star = new GameObject();
        starSpriteRenderer = star.AddComponent<SpriteRenderer>();
        starSpriteRenderer.sprite = Utility.GetRandom(starSprites);
        starSpriteRenderer.color = Utility.GetRandom(starColours);
        starSpriteRenderer.sortingLayerName = "Background";
        starSpriteRenderer.sortingOrder = -1000;
        star.transform.position = starPosition;
        star.transform.localScale = Vector2.one * size;
        star.transform.Rotate(0, 0, Random.Range(0, 360));
        star.transform.SetParent(transform);
        return star;
    }

    float SpeedMultiplierFromZDistance(int zDistance) {
        return (100 - zDistance) / 100f; // The farther the star is, the slower it is
    }

    float GetRandomStarYCoordinate() {
        return mapMinCoordinates.y + Random.Range(0, numberOfStars) * distanceBetweenStars.y;
    }

    void ResetStar(int starIndex) {
        Transform star = stars[starIndex].transform;
        star.position = new Vector2(mapMaxCoordinates.x, GetRandomStarYCoordinate());
    }
    
    #endregion

}
